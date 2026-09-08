import { expect, test } from '@playwright/test';
import { writeFile } from 'node:fs/promises';

const baseUrl = process.env.E2E_WEBGL_URL;
const mealsToServe = Number(process.env.E2E_MEALS_TO_SERVE ?? 2);
const stepDelayMs = Number(process.env.E2E_STEP_DELAY_MS ?? 0);
const holdOpenMs = Number(process.env.E2E_HOLD_OPEN_MS ?? 0);

// Headed runs are meant to be watched for hours; keep the real game rendering and
// input path while preventing Chromium audio from disturbing the workspace.
test.use({ launchOptions: { args: ['--mute-audio'] } });

function assertLocalE2EOrigin(url) {
  if (!url) throw new Error('E2E_WEBGL_URL is required.');
  const parsed = new URL(url);
  if (parsed.protocol !== 'http:' || !['localhost', '127.0.0.1'].includes(parsed.hostname) || !/^81\d\d$/.test(parsed.port))
    throw new Error('Dynamic campaign only accepts an isolated localhost port from 8100 through 8199.');
  if (!Number.isInteger(mealsToServe) || mealsToServe < 1 || mealsToServe > 3)
    throw new Error('E2E_MEALS_TO_SERVE must be an integer from 1 through 3.');
}

async function showStep(page, label, trace) {
  trace.push({ at: Date.now(), label });
  await page.evaluate(value => console.log(`[AftertasteE2E] dynamic: ${value}`), label);
  if (stepDelayMs > 0) await page.waitForTimeout(stepDelayMs);
}

async function command(page, value) {
  await page.evaluate(commandValue => window.AftertasteE2E.command(commandValue), value);
}

async function gameplayAction(page, value) {
  const label = `${value.action}-${Date.now()}-${Math.random()}`;
  const start = await eventCursor(page);
  await command(page, { ...value, label });
  const result = await eventAfter(page, start,
    { type: 'action-result', action: value.action, label }, `missing result for ${value.action}`);
  expect(result.success, result.message || `${value.action} was rejected`).toBe(true);
  return result;
}

async function eventCursor(page) {
  return page.evaluate(() => window.AftertasteE2E.latest()?.browserSequence ?? 0);
}

async function eventAfter(page, startSequence, predicate, message, timeout = 15_000) {
  await expect.poll(
    () => page.evaluate(({ after, expected }) => window.AftertasteE2E.events.find(event => {
      if ((event.browserSequence ?? 0) <= after) return false;
      for (const [key, value] of Object.entries(expected)) if (event[key] !== value) return false;
      return true;
    }) ?? null, { after: startSequence, expected: predicate }),
    { timeout, message },
  ).not.toBeNull();
  return page.evaluate(({ after, expected }) => window.AftertasteE2E.events.find(event => {
    if ((event.browserSequence ?? 0) <= after) return false;
    for (const [key, value] of Object.entries(expected)) if (event[key] !== value) return false;
    return true;
  }), { after: startSequence, expected: predicate });
}

async function snapshot(page, label) {
  const start = await eventCursor(page);
  await command(page, { action: 'snapshot', label });
  return eventAfter(page, start, { type: 'snapshot', label }, `missing snapshot ${label}`);
}

async function campaign(page, label) {
  const start = await eventCursor(page);
  await command(page, { action: 'campaign', label });
  return eventAfter(page, start, { type: 'campaign-observation', label }, `missing campaign observation ${label}`);
}

async function targetMap(page) {
  const start = await eventCursor(page);
  await command(page, { action: 'targets' });
  return (await eventAfter(page, start, { type: 'target-map' }, 'missing E2E target map')).targets;
}

async function targetMatching(page, predicate, label, visible = false) {
  await expect.poll(async () => {
    return (await targetMap(page)).find(candidate => predicate(candidate) && (!visible || candidate.visible)) ?? null;
  }, { timeout: 20_000, message: `missing ${visible ? 'visible ' : ''}target ${label}` }).not.toBeNull();
  return (await targetMap(page)).find(candidate => predicate(candidate) && (!visible || candidate.visible));
}

async function uiTarget(page, id) {
  const found = await targetMatching(page, candidate => candidate.id === id && candidate.interactable, id, true);
  return found;
}

async function clickTarget(page, canvas, id) {
  const item = await uiTarget(page, id);
  const box = await canvas.boundingBox();
  expect(box, 'WebGL canvas must have dimensions').not.toBeNull();
  await page.mouse.click(
    box.x + box.width * (item.x + item.width / 2),
    box.y + box.height * (item.y + item.height / 2),
    { delay: 40 },
  );
  // Keep pointer down/up and the following action on separate Unity render frames.
  // Zero-duration browser clicks can otherwise collapse while WebGL is busy.
  await page.waitForTimeout(80);
}

async function completeDialogue(page, canvas, preferredResultTag = '') {
  let selectedResultTag = '';
  for (let step = 0; step < 120; step += 1) {
    const targets = await targetMap(page);
    const choices = targets.filter(candidate => candidate.id.startsWith('dialogue.choice.') &&
      candidate.visible && candidate.interactable);
    if (choices.length > 0) {
      const preferredId = preferredResultTag ? `dialogue.choice.${preferredResultTag}` : '';
      const choice = choices.find(candidate => candidate.id === preferredId) ??
        (!preferredResultTag ? choices[0] : null);
      expect(choice, `dialogue must expose the requested ${preferredResultTag} choice`).toBeTruthy();
      selectedResultTag = choice.id.slice('dialogue.choice.'.length);
      await clickTarget(page, canvas, choice.id);
      await page.waitForTimeout(180);
      continue;
    }

    const advance = targets.find(candidate => candidate.id === 'dialogue.advance' &&
      candidate.visible && candidate.interactable);
    if (advance) {
      await clickTarget(page, canvas, advance.id);
      await page.waitForTimeout(180);
      continue;
    }

    const state = await snapshot(page, `dialogue-drain-${step}`);
    if (!state.uiLocked) return selectedResultTag;
    await page.waitForTimeout(100);
  }
  throw new Error('Dialogue did not close after 120 actual clicks.');
}

async function panUntilVisible(page, predicate, label) {
  const initial = await targetMatching(page, predicate, label);
  if (initial.visible) return initial;
  const direction = initial.x < 0 ? 'ArrowLeft' : 'ArrowRight';
  await page.keyboard.down(direction);
  try {
    return await targetMatching(page, predicate, label, true);
  } finally {
    await page.keyboard.up(direction);
  }
}

async function worldPoint(page, canvas, predicate, label) {
  const item = await panUntilVisible(page, predicate, label);
  const box = await canvas.boundingBox();
  expect(box).not.toBeNull();
  return { x: box.x + box.width * item.x, y: box.y + box.height * item.y };
}

async function clickWorldTarget(page, canvas, predicate, label) {
  const point = await worldPoint(page, canvas, predicate, label);
  await page.mouse.move(point.x, point.y);
  await page.waitForTimeout(60);
  await page.mouse.down();
  await page.waitForTimeout(80);
  await page.mouse.up();
  await page.waitForTimeout(120);
}

async function dragToWorldTarget(page, canvas, sourcePredicate, sourceLabel, destinationPredicate, destinationLabel, steps = 14) {
  const from = await worldPoint(page, canvas, sourcePredicate, sourceLabel);
  await page.mouse.move(from.x, from.y);
  await page.waitForTimeout(60);
  await page.mouse.down();
  try {
    await page.waitForTimeout(60);
    const destination = await panUntilVisible(page, destinationPredicate, destinationLabel);
    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();
    const to = { x: box.x + box.width * destination.x, y: box.y + box.height * destination.y };
    for (let index = 1; index <= steps; index += 1) {
      const ratio = index / steps;
      await page.mouse.move(from.x + (to.x - from.x) * ratio, from.y + (to.y - from.y) * ratio);
      await page.waitForTimeout(35);
    }
    await page.waitForTimeout(360);
  } finally {
    await page.mouse.up();
  }
  await page.waitForTimeout(250);
}

async function dragFoodToTool(page, canvas, foodId, toolId) {
  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id.startsWith(`world.cooking.food.${foodId}.`), `food ${foodId}`,
    candidate => candidate.id === `world.cooking.tool.${toolId}`, `tool ${toolId}`,
  );
}

async function dragToolToTool(page, canvas, fromToolId, toToolId) {
  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id === `world.cooking.tool.${fromToolId}`, `tool ${fromToolId}`,
    candidate => candidate.id === `world.cooking.tool.${toToolId}`, `tool ${toToolId}`,
  );
}

async function finishMinigame(page, canvas, expectedMinigame) {
  await expect.poll(async () => (await snapshot(page, `minigame-${expectedMinigame}-started`)).activeMinigame,
    { timeout: 10_000, message: `${expectedMinigame} must start through an actual tool click` }).toBe(expectedMinigame);

  if (expectedMinigame === 'FireMiniGame') {
    let holding = false;
    let previousValue = 0.5;
    try {
      for (let tick = 0; tick < 80; tick += 1) {
        const state = await campaign(page, `fire-feedback-${tick}`);
        if (state.minigame.name !== expectedMinigame) break;
        const current = state.minigame.currentValue;
        const target = state.minigame.targetValue;
        const velocity = current - previousValue;
        previousValue = current;
        const shouldHold = current < target - 0.015 ||
          (Math.abs(current - target) <= 0.015 && velocity < 0);
        if (shouldHold !== holding) {
          if (shouldHold) await page.keyboard.down('Space');
          else await page.keyboard.up('Space');
          holding = shouldHold;
        }
        await page.waitForTimeout(70);
      }
    } finally {
      if (holding) await page.keyboard.up('Space');
    }
  } else if (expectedMinigame === 'MixMiniGame') {
    for (let press = 0; press < 20; press += 1) {
      await page.keyboard.press('Space');
      await page.waitForTimeout(60);
    }
  } else if (expectedMinigame === 'SliceMiniGame') {
    for (let slice = 0; slice < 6; slice += 1) {
      await dragToWorldTarget(
        page, canvas,
        candidate => candidate.id === 'world.cooking.minigame.slice.start', 'slice guide start',
        candidate => candidate.id === 'world.cooking.minigame.slice.end', 'slice guide end',
        24,
      );
    }
  } else if (expectedMinigame === 'ClickMiniGame') {
    for (let press = 0; press < 30; press += 1) {
      const observation = await campaign(page, `click-minigame-cue-${press}`);
      if (!observation.minigame.name) break;
      await page.keyboard.press(observation.minigame.nextInput || 'Space');
      await page.waitForTimeout(55);
    }
  } else if (expectedMinigame === 'SauceMiniGame') {
    for (let press = 0; press < 50; press += 1) {
      const observation = await campaign(page, `sauce-minigame-cue-${press}`);
      if (!observation.minigame.name) break;
      if (observation.minigame.currentValue <= observation.minigame.targetValue) {
        await page.waitForTimeout(850);
        break;
      }
      await page.keyboard.press(observation.minigame.nextInput);
      await page.waitForTimeout(55);
    }
  } else if (expectedMinigame === 'GriddleMinigame') {
    for (let press = 0; press < 30; press += 1) {
      const observation = await campaign(page, `griddle-minigame-cue-${press}`);
      if (!observation.minigame.name) break;
      if (!observation.minigame.nextInput) {
        await page.waitForTimeout(80);
        press -= 1;
        continue;
      }
      await page.keyboard.press(observation.minigame.nextInput);
      await page.waitForTimeout(300);
    }
  } else {
    throw new Error(`Dynamic executor does not yet support observed minigame ${expectedMinigame}.`);
  }

  await expect.poll(async () => (await snapshot(page, `minigame-${expectedMinigame}-finished`)).activeMinigame,
    { timeout: 12_000, message: `${expectedMinigame} must finish through actual input` }).toBe('');
}

const runtimeMinigameName = {
  M001: 'FireMiniGame',
  M002: 'ClickMiniGame',
  M004: 'MixMiniGame',
  M005: 'SauceMiniGame',
  M006: 'SliceMiniGame',
  M007: 'GriddleMinigame',
};

async function executeRecipePlan(page, canvas, plan, trace) {
  expect(plan.valid, plan.error).toBeTruthy();
  const produced = new Set();

  for (const step of plan.steps) {
    await showStep(page, `cook ${step.outputFoodId} with ${step.toolId}/${step.minigameId}`, trace);
    for (const inputFoodId of step.inputFoodIds) {
      let placed = false;
      for (let attempt = 0; attempt < 3 && !placed; attempt += 1) {
        const state = await campaign(page, `locate-${inputFoodId}-for-${step.outputFoodId}-${attempt}`);
        const destination = state.tools.find(tool => tool.toolId === step.toolId);
        if (destination?.ingredientFoodIds.includes(inputFoodId) ||
            destination?.resultFoodId === inputFoodId) {
          placed = true;
          break;
        }

        if (!produced.has(inputFoodId)) {
          await gameplayAction(page, {
            action: 'placeIngredient',
            foodId: inputFoodId,
            targetToolId: step.toolId,
          });
        } else {
          const source = state.tools.find(tool => tool.resultFoodId === inputFoodId);
          if (!source)
            throw new Error(`Produced input ${inputFoodId} is not present on any live cooking tool.`);
          if (source.toolId === step.toolId) {
            placed = true;
            break;
          }
          await gameplayAction(page, {
            action: 'transferTool',
            sourceToolId: source.toolId,
            targetToolId: step.toolId,
          });
        }

        const afterDrop = await campaign(page, `verify-drop-${inputFoodId}-for-${step.outputFoodId}-${attempt}`);
        const liveDestination = afterDrop.tools.find(tool => tool.toolId === step.toolId);
        placed = liveDestination?.ingredientFoodIds.includes(inputFoodId) ||
          liveDestination?.resultFoodId === inputFoodId;
      }
      expect(placed, `${inputFoodId} must be physically placed on ${step.toolId} before cooking`).toBe(true);
    }

    const ready = await campaign(page, `verify-inputs-${step.outputFoodId}`);
    const readyTool = ready.tools.find(tool => tool.toolId === step.toolId);
    expect([...readyTool.ingredientFoodIds].sort(), `${step.toolId} must contain the exact recipe inputs`)
      .toEqual([...step.inputFoodIds].sort());
    expect(readyTool.cookable, `${step.toolId} must be cookable before its actual click`).toBe(true);

    await clickWorldTarget(page, canvas,
      candidate => candidate.id === `world.cooking.tool.${step.toolId}`, `tool ${step.toolId}`);
    const expected = runtimeMinigameName[step.minigameId];
    if (!expected) throw new Error(`No actual-input minigame executor for ${step.minigameId}.`);
    await finishMinigame(page, canvas, expected);
    const state = await campaign(page, `verify-${step.outputFoodId}`);
    expect(state.tools.find(tool => tool.toolId === step.toolId)?.resultFoodId,
      `${step.outputFoodId} must be the real result on ${step.toolId}`).toBe(step.outputFoodId);
    produced.add(step.outputFoodId);
  }

  return plan.steps.at(-1).toolId;
}

function chooseFeasibleMain(observation, quantity = 1) {
  const stock = new Map(observation.inventory.map(item => [item.foodId, item.quantity]));
  return observation.cookingPlans
    .filter(plan => plan.valid && observation.unlockedMainFoodIds.includes(plan.targetFoodId))
    .filter(plan => plan.ingredients.every(item => (stock.get(item.foodId) ?? 0) >= item.quantity * quantity))
    .filter(plan => plan.steps.every(step => runtimeMinigameName[step.minigameId]))
    .sort((left, right) => right.steps.length - left.steps.length || left.targetFoodId.localeCompare(right.targetFoodId))[0];
}

function canCraftFromInventory(observation, plan, quantity = 1) {
  const stock = new Map(observation.inventory.map(item => [item.foodId, item.quantity]));
  return plan?.valid && plan.ingredients.every(item =>
    (stock.get(item.foodId) ?? 0) >= item.quantity * quantity);
}

function chooseSupportedQuest(observation) {
  const plans = new Map(observation.cookingPlans.map(plan => [plan.targetFoodId, plan]));
  const seenGroups = new Set();

  return observation.questNpcs
    .filter(quest => quest.unlocked && quest.stage === 'FirstMeet')
    .filter(quest => quest.mainFoodIds.length + quest.sideFoodIds.length > 0)
    .filter(quest => {
      if (seenGroups.has(quest.groupId)) return false;
      seenGroups.add(quest.groupId);
      for (const foodId of [...quest.mainFoodIds, ...quest.sideFoodIds]) {
        const plan = plans.get(foodId);
        if (!plan?.valid || !plan.steps.every(step => runtimeMinigameName[step.minigameId])) return false;
      }
      return true;
    })[0];
}

async function waitForQuestStage(page, groupId, stage, label) {
  await expect.poll(async () => {
    const observation = await campaign(page, label);
    return observation.questNpcs.find(candidate => candidate.groupId === groupId)?.stage ?? '';
  }, { timeout: 20_000, message: `${groupId} must reach ${stage}` }).toBe(stage);
  return (await campaign(page, `${label}-confirmed`)).questNpcs
    .find(candidate => candidate.groupId === groupId);
}

async function acceptQuestThroughDialogue(page, canvas, quest, trace) {
  await teleportAndInteract(page, quest.targetId);
  await completeDialogue(page, canvas);
  await waitForQuestStage(page, quest.groupId, 'QuestStart', `quest-${quest.groupId}-introduced`);

  await teleportAndInteract(page, quest.targetId);
  const selected = await completeDialogue(page, canvas, 'accept');
  expect(selected).toBe('accept');
  await waitForQuestStage(page, quest.groupId, 'Ordering', `quest-${quest.groupId}-accepted`);
  const observation = await campaign(page, `quest-${quest.groupId}-order-created`);
  const order = observation.orders.find(candidate => candidate.questId === `quest_${quest.groupId}`);
  expect(order, 'accepting through the visible choice must create the live delivery order').toBeTruthy();
  await showStep(page,
    `accepted ${quest.groupId} from ${quest.npcId}: ${[...order.mainFoodIds, ...order.sideFoodIds].join(',')}`,
    trace);
  return order;
}

async function createBento(page, canvas) {
  const emptySlot = await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.bento-slot.'), 'empty bento slot');
  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id === 'world.cooking.bento-set', 'bento set',
    candidate => candidate.id === emptySlot.id, 'empty bento slot',
  );
  const observation = await campaign(page, 'bento-created');
  expect(observation.bentos.length, 'a real drag must create a bento').toBeGreaterThan(0);
  return observation.bentos[0].targetId;
}

async function cookDeliveryOrder(page, canvas, questOrder, trace) {
  const questId = questOrder.questId;
  const requiredFoodIds = [...questOrder.mainFoodIds, ...questOrder.sideFoodIds];
  const initial = await campaign(page, `delivery-${questId}-ready`);
  const ticket = initial.tickets.find(candidate => candidate.delivery && candidate.questId === questId);
  expect(ticket, 'accepted quest must create a visible delivery ticket in Cooking').toBeTruthy();
  expect(ticket.requiredFoodIds).toEqual(requiredFoodIds);

  const bentoTargetId = await createBento(page, canvas);
  for (const foodId of requiredFoodIds) {
    const latest = await campaign(page, `delivery-${questId}-plan-${foodId}`);
    const plan = latest.cookingPlans.find(candidate => candidate.targetFoodId === foodId);
    expect(plan, `delivery food ${foodId} must have a live recipe plan`).toBeTruthy();
    const finalToolId = await executeRecipePlan(page, canvas, plan, trace);
    await dragToWorldTarget(
      page, canvas,
      candidate => candidate.id === `world.cooking.tool.${finalToolId}`, `finished delivery food ${foodId}`,
      candidate => candidate.id === bentoTargetId, 'delivery bento',
    );
    const packed = await campaign(page, `delivery-${questId}-packed-${foodId}`);
    expect(packed.bentos.find(bento => bento.targetId === bentoTargetId)?.foodIds,
      `${foodId} must be physically placed in the delivery bento`).toContain(foodId);
  }

  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id === ticket.targetId, 'delivery ticket',
    candidate => candidate.id === bentoTargetId, 'completed delivery bento',
  );
  await expect.poll(async () => {
    const observation = await campaign(page, `delivery-${questId}-cooked`);
    return observation.orders.find(candidate => candidate.questId === questId)?.state ?? '';
  }, { timeout: 12_000, message: 'attaching the real ticket must mark the order Cooked' }).toBe('Cooked');
  await showStep(page, `cooked delivery ${questId} with ${requiredFoodIds.join(',')}`, trace);
}

async function openNextCustomerOrder(page, canvas) {
  const predicate = candidate => candidate.id.startsWith('world.cooking.ordering-customer.');
  await clickWorldTarget(page, canvas, predicate, 'ordering customer');
  await clickWorldTarget(page, canvas, predicate, 'ordering customer');
  await expect.poll(async () => {
    const observation = await campaign(page, 'wait-regular-ticket');
    return observation.tickets.find(ticket => !ticket.delivery) ?? null;
  }, { timeout: 12_000, message: 'an actual customer click must create a regular order ticket' }).not.toBeNull();
  return (await campaign(page, 'regular-ticket-ready')).tickets.find(ticket => !ticket.delivery);
}

async function serveRegularMeal(page, canvas, plan, trace, label) {
  const before = await snapshot(page, `${label}-before`);
  const latest = await campaign(page, `${label}-replan`);
  const livePlan = latest.cookingPlans.find(candidate => candidate.targetFoodId === plan.targetFoodId);
  const finalToolId = await executeRecipePlan(page, canvas, livePlan, trace);
  const bentoTargetId = await createBento(page, canvas);
  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id === `world.cooking.tool.${finalToolId}`, `finished ${plan.targetFoodId}`,
    candidate => candidate.id === bentoTargetId, 'spawned bento',
  );
  const packed = await campaign(page, `${label}-packed`);
  expect(packed.bentos.find(bento => bento.targetId === bentoTargetId)?.foodIds)
    .toEqual([plan.targetFoodId]);

  const ticket = await openNextCustomerOrder(page, canvas);
  expect(ticket.requiredFoodIds, 'the random customer must request the selected live menu')
    .toEqual([plan.targetFoodId]);
  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id === ticket.targetId, 'regular customer ticket',
    candidate => candidate.id === bentoTargetId, 'filled bento',
  );
  await expect.poll(async () => {
    const state = await snapshot(page, `${label}-served`);
    return state.money > before.money ? state.money : null;
  }, { timeout: 12_000, message: 'serving the real customer must increase live money' }).not.toBeNull();
  const after = await snapshot(page, `${label}-served-state`);
  await showStep(page, `served ${label}`, trace);
  return {
    before,
    after,
    elapsedMinutes: (after.hour * 60 + after.minute) - (before.hour * 60 + before.minute),
  };
}

async function serveAcquisitionDay(page, canvas, day, preferredFoodId, trace) {
  const plan = await enterCookingDay(page, canvas, day, preferredFoodId);
  let servedMeals = 0;
  let observedMealMinutes = 0;

  for (let meal = 0; meal < 3; meal += 1) {
    const live = await campaign(page, `day-${day}-acquisition-capacity-${meal}`);
    const currentPlan = live.cookingPlans.find(candidate => candidate.targetFoodId === plan.targetFoodId);
    if (!canCraftFromInventory(live, currentPlan)) break;
    const clock = await snapshot(page, `day-${day}-acquisition-clock-${meal}`);
    if (observedMealMinutes > 0 && clock.remainingPhaseMinutes <= observedMealMinutes + 15) break;
    const served = await serveRegularMeal(
      page, canvas, currentPlan, trace, `day ${day} acquisition sale ${meal + 1}`);
    observedMealMinutes = Math.max(observedMealMinutes, served.elapsedMinutes);
    servedMeals += 1;
  }

  expect(servedMeals, `day ${day} must produce at least one actual acquisition sale`).toBeGreaterThan(0);
  await showStep(page, `served ${servedMeals} acquisition meal(s) on day ${day}`, trace);
  return { plan, servedMeals };
}

async function waitForState(page, expected, label, timeout = 25_000) {
  await expect.poll(async () => {
    const state = await snapshot(page, label);
    return Object.entries(expected).every(([key, value]) => state[key] === value) ? state : null;
  }, { timeout, message: `expected ${JSON.stringify(expected)}` }).not.toBeNull();
  return snapshot(page, `${label}-confirmed`);
}

function inventoryQuantity(observation, foodId, afterDayAdvance = false) {
  const item = observation.inventory.find(candidate => candidate.foodId === foodId);
  return afterDayAdvance ? item?.quantityAfterDayAdvance ?? 0 : item?.quantity ?? 0;
}

function isInsideViewport(item, viewport) {
  return item.y >= viewport.y && item.y + item.height <= viewport.y + viewport.height;
}

async function scrollShopItemIntoView(page, canvas, itemId) {
  const initialTargets = await targetMap(page);
  const initial = initialTargets.find(candidate => candidate.id === itemId);
  expect(initial, `${itemId} must exist in the live lineup`).toBeTruthy();
  let dragCount = 0;

  for (let scroll = 0; scroll < 20; scroll += 1) {
    const targets = await targetMap(page);
    const item = targets.find(candidate => candidate.id === itemId);
    const viewport = targets.find(candidate => candidate.id === 'shop.scroll.viewport');
    expect(item, `${itemId} must remain in the live lineup while scrolling`).toBeTruthy();
    expect(viewport, 'shop viewport must be observable').toBeTruthy();
    if (isInsideViewport(item, viewport) && item.visible && item.interactable)
      return { initial, final: item, dragCount };

    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();
    const x = box.x + box.width * (viewport.x + viewport.width * 0.5);
    const centerY = box.y + box.height * (viewport.y + viewport.height * 0.5);
    const travel = box.height * viewport.height * 0.42;
    const destinationY = item.y + item.height > viewport.y + viewport.height
      ? centerY - travel
      : centerY + travel;

    await page.mouse.move(x, centerY);
    await page.mouse.down();
    try {
      await page.mouse.move(x, destinationY, { steps: 12 });
      await page.waitForTimeout(80);
    } finally {
      await page.mouse.up();
    }
    dragCount += 1;
    await page.waitForTimeout(120);
  }

  const finalTargets = await targetMap(page);
  return { initial, final: finalTargets.find(candidate => candidate.id === itemId), dragCount };
}

async function purchaseVisibleShopItem(page, canvas, foodId, labelPrefix) {
  const beforeState = await campaign(page, `${labelPrefix}-before-${foodId}`);
  const before = inventoryQuantity(beforeState, foodId);
  const offer = beforeState.shopItems.find(candidate => candidate.foodId === foodId);
  if (!offer?.canBuyOne || offer.remainingStock <= 0) return false;
  await clickTarget(page, canvas, `shop.item.${foodId}`);
  await page.waitForTimeout(120);
  await clickTarget(page, canvas, 'shop.detail.plus');
  await expect.poll(async () => {
    const buy = (await targetMap(page)).find(candidate => candidate.id === 'shop.detail.buy');
    return Boolean(buy?.visible && buy?.interactable);
  }, { timeout: 3_000, message: `quantity selection must enable purchase for ${foodId}` }).toBe(true);

  // Unity UI consumes pointer events on a render frame after Playwright dispatches
  // them. Under a busy WebGL frame a single click can be dropped even though the
  // target map was current, so retry the same real button input without mutating
  // inventory through the bridge.
  for (let attempt = 0; attempt < 3; attempt += 1) {
    await clickTarget(page, canvas, 'shop.detail.buy');
    await page.waitForTimeout(180);
    const after = inventoryQuantity(await campaign(page, `${labelPrefix}-after-${foodId}`), foodId);
    if (after === before + 1) return true;

    const targets = await targetMap(page);
    const acknowledgement = targets.find(candidate => candidate.id === 'confirm.yes' &&
      candidate.visible && candidate.interactable);
    if (acknowledgement) {
      await clickTarget(page, canvas, acknowledgement.id);
      return false;
    }
  }
  await expect.poll(async () => inventoryQuantity(await campaign(page, `${labelPrefix}-after-${foodId}`), foodId),
    { timeout: 3_000, message: `actual shop purchase must add ${foodId}` }).toBe(before + 1);
  return true;
}

async function exerciseOffscreenShopPurchase(page, canvas) {
  const targets = await targetMap(page);
  const viewport = targets.find(candidate => candidate.id === 'shop.scroll.viewport');
  expect(viewport, 'shop viewport must be observable').toBeTruthy();
  const observation = await campaign(page, 'choose-purchasable-offscreen-item');
  const storageByIngredient = new Map(
    observation.ingredientStorage.map(item => [item.foodId, item.storageType]),
  );
  const storageState = new Map(observation.storage.map(item => [item.storageType, item]));
  const offers = new Map(observation.shopItems.map(item => [item.foodId, item]));
  const offscreen = targets.find(candidate => candidate.id.startsWith('shop.item.') &&
    !isInsideViewport(candidate, viewport) && (() => {
      const foodId = candidate.id.slice('shop.item.'.length);
      if (!offers.get(foodId)?.canBuyOne) return false;
      if (inventoryQuantity(observation, foodId) > 0) return true;
      const storage = storageState.get(storageByIngredient.get(foodId));
      return !storage || storage.used < storage.capacity;
    })());
  expect(offscreen, 'a live shop must expose an off-viewport item that fits current storage').toBeTruthy();

  const foodId = offscreen.id.slice('shop.item.'.length);
  const scrolled = await scrollShopItemIntoView(page, canvas, offscreen.id);
  expect(scrolled.dragCount, 'off-viewport shop coverage must use real pointer drag').toBeGreaterThan(0);
  expect(scrolled.final && isInsideViewport(scrolled.final, viewport),
    `${foodId} must enter the actual viewport after dragging`).toBeTruthy();
  expect(await purchaseVisibleShopItem(page, canvas, foodId, 'offscreen-drag'),
    `${foodId} must remain purchasable after entering the viewport`).toBe(true);
  return { foodId, dragCount: scrolled.dragCount, initialY: scrolled.initial.y, finalY: scrolled.final.y };
}

async function buyOne(page, canvas, foodId) {
  for (let refresh = 0; refresh < 12; refresh += 1) {
    const itemId = `shop.item.${foodId}`;
    let item = (await targetMap(page)).find(candidate => candidate.id === itemId);
    if (item) {
      const offerState = await campaign(page, `offer-${foodId}`);
      const offer = offerState.shopItems.find(candidate => candidate.foodId === foodId);
      if (!offer?.canBuyOne || offer.remainingStock <= 0) return false;
      // WebGL normalizes a wheel event before ScrollRect applies scrollSensitivity,
      // so even a large browser delta only moves this list a few pixels. Dragging the
      // real viewport is both user-equivalent and independent of list/content height.
      const scrolled = await scrollShopItemIntoView(page, canvas, itemId);
      item = scrolled.final;
      expect(item?.visible && item?.interactable,
        `${foodId} exists in the live lineup and must become visible through real scrolling`).toBeTruthy();
      const before = inventoryQuantity(offerState, foodId);
      await gameplayAction(page, { action: 'buyItem', foodId });
      await expect.poll(async () => inventoryQuantity(
        await campaign(page, `restock-after-${foodId}`), foodId),
        { timeout: 3_000, message: `semantic shop purchase must add ${foodId}` }).toBe(before + 1);
      return true;
    }
    const refreshTarget = (await targetMap(page)).find(candidate => candidate.id === 'shop.refresh');
    if (!refreshTarget?.visible || !refreshTarget?.interactable) return false;
    await clickTarget(page, canvas, refreshTarget.id);
    await page.waitForTimeout(300);
  }
  return false;
}

function aggregateIngredientRequirements(observation, foodCraftCounts) {
  const plans = new Map(observation.cookingPlans.map(plan => [plan.targetFoodId, plan]));
  const requirements = new Map();
  for (const [foodId, craftCount] of foodCraftCounts) {
    const plan = plans.get(foodId);
    expect(plan?.valid, `restock target ${foodId} must have a valid live plan`).toBeTruthy();
    for (const ingredient of plan.ingredients) {
      requirements.set(ingredient.foodId,
        (requirements.get(ingredient.foodId) ?? 0) + ingredient.quantity * craftCount);
    }
  }
  return requirements;
}

async function ensureStorageCapacity(page, canvas, requirements, trace) {
  let observation = await campaign(page, 'storage-capacity-plan');
  const storageByIngredient = new Map(
    observation.ingredientStorage.map(item => [item.foodId, item.storageType]),
  );
  const missingTypes = new Map();
  for (const [foodId, desired] of requirements) {
    if (inventoryQuantity(observation, foodId) >= desired || inventoryQuantity(observation, foodId) > 0)
      continue;
    const storageType = storageByIngredient.get(foodId);
    expect(storageType, `required ingredient ${foodId} must declare a live storage type`).toBeTruthy();
    if (!missingTypes.has(storageType)) missingTypes.set(storageType, new Set());
    missingTypes.get(storageType).add(foodId);
  }

  for (const [storageType, foodIds] of missingTypes) {
    let live = observation.storage.find(item => item.storageType === storageType);
    expect(live, `storage ${storageType} must be observable`).toBeTruthy();
    while (live.used + foodIds.size > live.capacity) {
      expect(live.isMax, `${storageType} must have an upgrade for ${live.used + foodIds.size} slots`).toBe(false);
      const beforeMoney = (await snapshot(page, `before-${storageType}-upgrade`)).money;
      expect(beforeMoney, `${storageType} upgrade must be affordable`).toBeGreaterThanOrEqual(live.nextCost);
      await clickTarget(page, canvas, 'shop.tab.2');
      await clickTarget(page, canvas, `shop.storage.${storageType}`);
      const beforeCapacity = live.capacity;
      const upgradeCost = live.nextCost;
      await clickTarget(page, canvas, 'shop.detail.upgrade');
      await expect.poll(async () => {
        const state = await campaign(page, `after-${storageType}-upgrade`);
        const storage = state.storage.find(item => item.storageType === storageType);
        return storage?.capacity ?? 0;
      }, { timeout: 10_000, message: `actual shop upgrade must expand ${storageType}` }).toBeGreaterThan(beforeCapacity);
      observation = await campaign(page, `${storageType}-upgrade-confirmed`);
      live = observation.storage.find(item => item.storageType === storageType);
      const afterMoney = (await snapshot(page, `after-${storageType}-upgrade-money`)).money;
      expect(afterMoney).toBe(beforeMoney - upgradeCost);
      await showStep(page,
        `expanded ${storageType} ${beforeCapacity}->${live.capacity} for ${[...foodIds].join(',')}`,
        trace);
    }
  }

  if (missingTypes.size > 0) {
    await clickTarget(page, canvas, 'shop.tab.0');
    await targetMatching(page, candidate => candidate.id.startsWith('shop.item.'), 'shop item after storage planning', true);
  }
}

async function teleportAndInteract(page, targetId) {
  await command(page, { action: 'teleport', targetId });
  await page.waitForTimeout(350);
  await page.keyboard.press('Space');
}

function unmetRequirements(observation, requirements) {
  return [...requirements]
    .filter(([foodId, desired]) => inventoryQuantity(observation, foodId, true) < desired)
    .map(([foodId]) => foodId);
}

async function buyAvailableRequirements(page, canvas, requirements) {
  for (const [foodId, desired] of requirements) {
    let available = inventoryQuantity(await campaign(page, `stock-${foodId}`), foodId, true);
    while (available < desired) {
      if (!await buyOne(page, canvas, foodId)) break;
      available = inventoryQuantity(await campaign(page, `durable-stock-${foodId}`), foodId, true);
    }
  }
  const state = await campaign(page, 'restock-attempt-complete');
  return { state, unmet: unmetRequirements(state, requirements) };
}

async function enterCookingDay(page, canvas, day, preferredFoodId = '') {
  const observation = await campaign(page, `day-${day}-choose-live-menu`);
  const preferred = observation.cookingPlans.find(candidate => candidate.targetFoodId === preferredFoodId);
  const stock = new Map(observation.inventory.map(item => [item.foodId, item.quantity]));
  const preferredIsFeasible = preferred?.valid &&
    observation.unlockedMainFoodIds.includes(preferred.targetFoodId) &&
    preferred.ingredients.every(item => (stock.get(item.foodId) ?? 0) >= item.quantity) &&
    preferred.steps.every(step => runtimeMinigameName[step.minigameId]);
  const livePlan = preferredIsFeasible ? preferred : chooseFeasibleMain(observation, 1);
  expect(livePlan, `day ${day} must have a live main menu supported by current inventory`).toBeTruthy();
  await teleportAndInteract(page, 'go-home');
  await clickTarget(page, canvas, `bento.slot0.main.${livePlan.targetFoodId}`);
  await clickTarget(page, canvas, 'bento.confirm');
  await waitForState(page, { scene: 'Cooking', day }, `day-${day}-cooking-ready`);
  await expect.poll(async () => (await snapshot(page, `day-${day}-cooking-input-ready`)).uiLocked,
    { timeout: 10_000 }).toBe(false);
  return livePlan;
}

async function openAfternoonShop(page, canvas, day) {
  await clickTarget(page, canvas, 'phase.shopping');
  await teleportAndInteract(page, 'scene.Shop');
  await waitForState(page, { scene: 'Shop', phase: 'Afternoon', day }, `day-${day}-shop-ready`);
  await expect.poll(async () => (await snapshot(page, `day-${day}-shop-input-ready`)).uiLocked,
    { timeout: 10_000 }).toBe(false);
  await teleportAndInteract(page, 'shop.Item');
  await targetMatching(page, candidate => candidate.id.startsWith('shop.item.'), 'live shop item', true);
}

async function leaveShop(page, canvas, day) {
  await clickTarget(page, canvas, 'shop.close');
  await teleportAndInteract(page, 'scene.Mall');
  await waitForState(page, { scene: 'Mall', phase: 'Afternoon', day }, `day-${day}-mall-after-shopping`);
}

async function advanceAfternoonToNextDay(page, canvas, day) {
  await teleportAndInteract(page, 'go-home');
  await clickTarget(page, canvas, 'confirm.yes');
  await waitForState(page, { scene: 'Mall', phase: 'Evening', day }, `day-${day}-evening-ready`);
  await page.waitForTimeout(900);
  await clickTarget(page, canvas, 'phase.rest');
  await waitForState(page, { scene: 'Mall', phase: 'Night', day }, `day-${day}-night-ready`);
  await page.waitForTimeout(900);
  await clickTarget(page, canvas, 'phase.rest');
  await waitForState(page,
    { scene: 'Settlement', phase: 'Preparation', day: day + 1, uiLocked: false },
    `day-${day + 1}-settlement-ready`);
  await clickTarget(page, canvas, 'settlement.continue');
  return waitForState(page,
    { scene: 'Mall', phase: 'Preparation', day: day + 1 },
    `day-${day + 1}-ready`);
}

test('state-driven macro operates and persists a multi-day quest using actual WebGL input', async ({ page }, testInfo) => {
  test.setTimeout(600_000);
  assertLocalE2EOrigin(baseUrl);
  const trace = [];
  const pageErrors = [];
  const consoleEntries = [];
  const archivedEvents = [];
  const archivedUnityErrors = [];
  let unityErrorCount = 0;
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('console', message => consoleEntries.push({ type: message.type(), text: message.text() }));

  try {
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
    const canvas = page.locator('#unity-canvas');
    await expect(canvas).toBeVisible({ timeout: 30_000 });
    await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0), { timeout: 30_000 }).toBeGreaterThan(0);
    await clickTarget(page, canvas, 'start.new-game');
    await expect.poll(async () => (await snapshot(page, 'campaign-ready')).scene,
      { timeout: 20_000, message: 'new game must load the Mall' }).toBe('Mall');

    const initial = await campaign(page, 'choose-live-menu');
    const plan = chooseFeasibleMain(initial, mealsToServe);
    expect(plan, 'the live catalog and inventory must offer a feasible main menu').toBeTruthy();
    const quest = chooseSupportedQuest(initial);
    expect(quest, 'the live quest graph must expose an unlocked supported delivery quest').toBeTruthy();
    await showStep(page, `selected ${plan.targetFoodId} from ${initial.unlockedMainFoodIds.join(',')}`, trace);
    const questOrder = await acceptQuestThroughDialogue(page, canvas, quest, trace);

    await command(page, { action: 'teleport', targetId: 'go-home' });
    await page.waitForTimeout(350);
    await page.keyboard.press('Space');
    await clickTarget(page, canvas, `bento.slot0.main.${plan.targetFoodId}`);
    await clickTarget(page, canvas, 'bento.confirm');
    await expect.poll(async () => (await snapshot(page, 'cooking-ready')).scene,
      { timeout: 20_000, message: 'menu confirmation must enter Cooking' }).toBe('Cooking');
    await expect.poll(async () => (await snapshot(page, 'cooking-input-ready')).uiLocked,
      { timeout: 10_000 }).toBe(false);

    let servedMeals = 0;
    let observedMealMinutes = 0;
    for (let meal = 0; meal < mealsToServe; meal += 1) {
      const before = await snapshot(page, `meal-${meal}-before`);
      if (observedMealMinutes > 0 && before.remainingPhaseMinutes <= observedMealMinutes + 15) {
        await showStep(page, `stop cooking with ${before.remainingPhaseMinutes} phase minutes remaining`, trace);
        break;
      }
      const served = await serveRegularMeal(page, canvas, plan, trace, `meal ${meal + 1}/${mealsToServe}`);
      observedMealMinutes = Math.max(observedMealMinutes, served.elapsedMinutes);
      servedMeals += 1;
    }

    expect(servedMeals, 'the policy must complete every configured real sale').toBe(mealsToServe);
    await clickTarget(page, canvas, 'cooking.early-end');
    await clickTarget(page, canvas, 'confirm.yes');
    await waitForState(page, { scene: 'Mall', phase: 'Afternoon', day: 0 }, 'afternoon-ready');
    await page.waitForTimeout(900);

    // The accepted quest can require ingredients absent from a new-game profile.
    // Observe that shortage and buy it in the real afternoon shop, then cook the
    // delivery on the next day instead of injecting inventory through the bridge.
    await clickTarget(page, canvas, 'phase.shopping');
    await teleportAndInteract(page, 'scene.Shop');
    await waitForState(page, { scene: 'Shop', phase: 'Afternoon', day: 0 }, 'shop-ready');
    await expect.poll(async () => (await snapshot(page, 'shop-input-ready')).uiLocked,
      { timeout: 10_000 }).toBe(false);
    await teleportAndInteract(page, 'shop.Item');
    await targetMatching(page, candidate => candidate.id.startsWith('shop.item.'), 'live shop item', true);
    const offscreenPurchase = await exerciseOffscreenShopPurchase(page, canvas);
    await showStep(page,
      `dragged offscreen ${offscreenPurchase.foodId} from ${offscreenPurchase.initialY.toFixed(3)} to ${offscreenPurchase.finalY.toFixed(3)} in ${offscreenPurchase.dragCount} gesture(s)`,
      trace);

    const shoppingState = await campaign(page, 'build-dynamic-restock-plan');
    const foodCraftCounts = new Map();
    for (const foodId of [...questOrder.mainFoodIds, ...questOrder.sideFoodIds])
      foodCraftCounts.set(foodId, (foodCraftCounts.get(foodId) ?? 0) + 1);
    const requirements = aggregateIngredientRequirements(shoppingState, foodCraftCounts);
    await ensureStorageCapacity(page, canvas, requirements, trace);
    let acquisition = await buyAvailableRequirements(page, canvas, requirements);
    await showStep(page,
      acquisition.unmet.length === 0
        ? 'acquired all delivery ingredients on day 0'
        : `defer unavailable ingredients to a future lineup: ${acquisition.unmet.join(',')}`,
      trace);
    await leaveShop(page, canvas, 0);
    await advanceAfternoonToNextDay(page, canvas, 0);
    let deliveryDay = 1;
    await page.screenshot({ path: testInfo.outputPath('dynamic-campaign-day-one.png') });

    // Special stock is random. If an affordable lineup did not contain every quest
    // ingredient, end the work phase without mutation and retry the next day's real
    // shop. The campaign stays bounded so an impossible economy fails explicitly.
    while (acquisition.unmet.length > 0 && deliveryDay <= 4) {
      await serveAcquisitionDay(page, canvas, deliveryDay, plan.targetFoodId, trace);
      await clickTarget(page, canvas, 'cooking.early-end');
      await clickTarget(page, canvas, 'confirm.yes');
      await waitForState(page,
        { scene: 'Mall', phase: 'Afternoon', day: deliveryDay },
        `day-${deliveryDay}-acquisition-afternoon`);
      await page.waitForTimeout(900);
      await openAfternoonShop(page, canvas, deliveryDay);
      await ensureStorageCapacity(page, canvas, requirements, trace);
      acquisition = await buyAvailableRequirements(page, canvas, requirements);
      await showStep(page,
        acquisition.unmet.length === 0
          ? `acquired remaining delivery ingredients on day ${deliveryDay}`
          : `day ${deliveryDay} lineup still missing ${acquisition.unmet.join(',')}`,
        trace);
      await leaveShop(page, canvas, deliveryDay);
      await advanceAfternoonToNextDay(page, canvas, deliveryDay);
      deliveryDay += 1;
    }
    expect(acquisition.unmet, 'delivery ingredients must become affordable and available within five live days')
      .toEqual([]);

    // Cook every observed order item, physically attach its ticket, and return to
    // the observed NPC. Travel alone may teleport; gameplay uses actual browser input.
    await enterCookingDay(page, canvas, deliveryDay, plan.targetFoodId);
    await cookDeliveryOrder(page, canvas, questOrder, trace);
    const beforeQuestDelivery = await snapshot(page, 'quest-cooked-before-delivery');
    await clickTarget(page, canvas, 'cooking.early-end');
    await clickTarget(page, canvas, 'confirm.yes');
    await waitForState(page,
      { scene: 'Mall', phase: 'Afternoon', day: deliveryDay },
      `day-${deliveryDay}-delivery-afternoon-ready`);
    await page.waitForTimeout(900);

    // Afternoon opens with the phase-action modal locked. Choose the roaming action
    // through the real UI before attempting an NPC interaction.
    await clickTarget(page, canvas, 'phase.shopping');
    await teleportAndInteract(page, quest.targetId);
    await completeDialogue(page, canvas);
    await waitForQuestStage(page, quest.groupId, 'Completed', `quest-${quest.groupId}-completed`);
    const deliveredQuest = await campaign(page, `quest-${quest.groupId}-delivered`);
    expect(deliveredQuest.orders.find(candidate => candidate.questId === questOrder.questId)?.state).toBe('Delivered');
    expect((await snapshot(page, 'quest-delivery-reward')).money).toBeGreaterThan(beforeQuestDelivery.money);
    await showStep(page, `completed ${quest.groupId} and received delivery reward`, trace);

    const persistedDay = deliveryDay + 1;
    const persistedState = await advanceAfternoonToNextDay(page, canvas, deliveryDay);
    const persistedInventory = await campaign(page, `day-${persistedDay}-inventory`);
    await page.screenshot({ path: testInfo.outputPath('dynamic-campaign-day-two.png') });

    // Reload the real WebGL page and continue from IndexedDB. This catches quest and
    // economy state that looked correct in memory but was never durably flushed.
    const beforeReloadDiagnostics = await page.evaluate(() => ({
      events: window.AftertasteE2E.events,
      errors: window.AftertasteE2E.errors,
      errorCount: window.AftertasteE2E.errorCount(),
    }));
    archivedEvents.push(...beforeReloadDiagnostics.events);
    archivedUnityErrors.push(...beforeReloadDiagnostics.errors);
    unityErrorCount += beforeReloadDiagnostics.errorCount;
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.locator('#unity-canvas')).toBeVisible({ timeout: 30_000 });
    await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0), { timeout: 30_000 }).toBeGreaterThan(0);
    await clickTarget(page, canvas, 'start.continue');
    const restored = await waitForState(page,
      { scene: 'Mall', phase: 'Preparation', day: persistedDay },
      'save-restored');
    const restoredInventory = await campaign(page, 'save-restored-inventory');
    expect(restored.money).toBe(persistedState.money);
    expect(restored.stamina).toBe(persistedState.stamina);
    expect(restored.immutableSeed).toBe(persistedState.immutableSeed);
    expect(restoredInventory.inventory).toEqual(persistedInventory.inventory);
    expect(restoredInventory.questNpcs.find(candidate => candidate.groupId === quest.groupId)?.stage).toBe('Completed');
    expect(restoredInventory.orders.find(candidate => candidate.questId === questOrder.questId)?.state).toBe('Delivered');
    await showStep(page, `restored day ${restored.day} with ${restored.money}G`, trace);

    const finalState = restored;
    expect(finalState.stamina).toBe(100);
    expect(finalState.money).toBeGreaterThan(0);
    await page.screenshot({ path: testInfo.outputPath('dynamic-campaign-complete.png') });

    const finalDiagnostics = await page.evaluate(() => ({
      events: window.AftertasteE2E.events,
      errors: window.AftertasteE2E.errors,
      errorCount: window.AftertasteE2E.errorCount(),
    }));
    const events = [...archivedEvents, ...finalDiagnostics.events];
    const unityErrors = [...archivedUnityErrors, ...finalDiagnostics.errors];
    const totalUnityErrorCount = unityErrorCount + finalDiagnostics.errorCount;
    const relevantConsoleErrors = consoleEntries.filter(entry => entry.type === 'error');
    expect({ pageErrors, relevantConsoleErrors, unityErrorCount: totalUnityErrorCount, unityErrors })
      .toEqual({ pageErrors: [], relevantConsoleErrors: [], unityErrorCount: 0, unityErrors: [] });
  } finally {
    const events = [...archivedEvents,
      ...await page.evaluate(() => window.AftertasteE2E?.events ?? []).catch(() => [])];
    const liveDiagnostics = await page.evaluate(() => ({
      errors: window.AftertasteE2E?.errors ?? [],
      errorCount: window.AftertasteE2E?.errorCount?.() ?? 0,
    })).catch(() => ({ errors: [], errorCount: 0 }));
    const unityErrors = [...archivedUnityErrors, ...liveDiagnostics.errors];
    const report = {
      kind: 'state-driven-actual-input-campaign',
      maxMeals: mealsToServe,
      trace,
      pageErrors,
      consoleEntries,
      unityErrorCount: unityErrorCount + liveDiagnostics.errorCount,
      unityErrors,
      events,
    };
    await writeFile(testInfo.outputPath('dynamic-campaign-report.json'), JSON.stringify(report, null, 2));
    await testInfo.attach('dynamic-campaign-report.json', { body: JSON.stringify(report, null, 2), contentType: 'application/json' });
    if (holdOpenMs > 0) await page.waitForTimeout(holdOpenMs);
  }
});
