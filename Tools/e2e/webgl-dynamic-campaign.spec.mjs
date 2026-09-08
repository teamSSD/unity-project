import { expect, test } from '@playwright/test';
import { writeFile } from 'node:fs/promises';

const baseUrl = process.env.E2E_WEBGL_URL;
const mealsToServe = Number(process.env.E2E_MEALS_TO_SERVE ?? 1);
const stepDelayMs = Number(process.env.E2E_STEP_DELAY_MS ?? 0);
const holdOpenMs = Number(process.env.E2E_HOLD_OPEN_MS ?? 0);

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

async function eventAfter(page, start, predicate, message, timeout = 15_000) {
  await expect.poll(
    () => page.evaluate(({ index, expected }) => window.AftertasteE2E.events.slice(index).find(event => {
      for (const [key, value] of Object.entries(expected)) if (event[key] !== value) return false;
      return true;
    }) ?? null, { index: start, expected: predicate }),
    { timeout, message },
  ).not.toBeNull();
  return page.evaluate(({ index, expected }) => window.AftertasteE2E.events.slice(index).find(event => {
    for (const [key, value] of Object.entries(expected)) if (event[key] !== value) return false;
    return true;
  }), { index: start, expected: predicate });
}

async function snapshot(page, label) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await command(page, { action: 'snapshot', label });
  return eventAfter(page, start, { type: 'snapshot', label }, `missing snapshot ${label}`);
}

async function campaign(page, label) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await command(page, { action: 'campaign', label });
  return eventAfter(page, start, { type: 'campaign-observation', label }, `missing campaign observation ${label}`);
}

async function targetMap(page) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
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
  );
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
    await page.keyboard.down('Space');
    await page.waitForTimeout(3_300);
    await page.keyboard.up('Space');
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
      if (!produced.has(inputFoodId)) {
        await dragFoodToTool(page, canvas, inputFoodId, step.toolId);
        continue;
      }

      const state = await campaign(page, `locate-${inputFoodId}-for-${step.outputFoodId}`);
      const destination = state.tools.find(tool => tool.toolId === step.toolId);
      if (destination?.ingredientFoodIds.includes(inputFoodId))
        continue;
      const source = state.tools.find(tool => tool.resultFoodId === inputFoodId);
      if (!source)
        throw new Error(`Produced input ${inputFoodId} is not present on any live cooking tool.`);
      if (source.toolId !== step.toolId)
        await dragToolToTool(page, canvas, source.toolId, step.toolId);
    }

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

async function waitForState(page, expected, label, timeout = 25_000) {
  await expect.poll(async () => {
    const state = await snapshot(page, label);
    return Object.entries(expected).every(([key, value]) => state[key] === value) ? state : null;
  }, { timeout, message: `expected ${JSON.stringify(expected)}` }).not.toBeNull();
  return snapshot(page, `${label}-confirmed`);
}

function inventoryQuantity(observation, foodId) {
  return observation.inventory.find(item => item.foodId === foodId)?.quantity ?? 0;
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
  const before = inventoryQuantity(await campaign(page, `${labelPrefix}-before-${foodId}`), foodId);
  await clickTarget(page, canvas, `shop.item.${foodId}`);
  await clickTarget(page, canvas, 'shop.detail.plus');
  await clickTarget(page, canvas, 'shop.detail.buy');
  await expect.poll(async () => inventoryQuantity(await campaign(page, `${labelPrefix}-after-${foodId}`), foodId),
    { timeout: 10_000, message: `actual shop purchase must add ${foodId}` }).toBe(before + 1);
}

async function exerciseOffscreenShopPurchase(page, canvas) {
  const targets = await targetMap(page);
  const viewport = targets.find(candidate => candidate.id === 'shop.scroll.viewport');
  expect(viewport, 'shop viewport must be observable').toBeTruthy();
  const offscreen = targets.find(candidate => candidate.id.startsWith('shop.item.') &&
    !isInsideViewport(candidate, viewport));
  expect(offscreen, 'a 20-row live shop must expose an initially off-viewport item').toBeTruthy();

  const foodId = offscreen.id.slice('shop.item.'.length);
  const scrolled = await scrollShopItemIntoView(page, canvas, offscreen.id);
  expect(scrolled.dragCount, 'off-viewport shop coverage must use real pointer drag').toBeGreaterThan(0);
  expect(scrolled.final && isInsideViewport(scrolled.final, viewport),
    `${foodId} must enter the actual viewport after dragging`).toBeTruthy();
  await purchaseVisibleShopItem(page, canvas, foodId, 'offscreen-drag');
  return { foodId, dragCount: scrolled.dragCount, initialY: scrolled.initial.y, finalY: scrolled.final.y };
}

async function buyOne(page, canvas, foodId) {
  for (let refresh = 0; refresh < 12; refresh += 1) {
    const itemId = `shop.item.${foodId}`;
    let item = (await targetMap(page)).find(candidate => candidate.id === itemId);
    if (item) {
      // WebGL normalizes a wheel event before ScrollRect applies scrollSensitivity,
      // so even a large browser delta only moves this list a few pixels. Dragging the
      // real viewport is both user-equivalent and independent of list/content height.
      const scrolled = await scrollShopItemIntoView(page, canvas, itemId);
      item = scrolled.final;
      expect(item?.visible && item?.interactable,
        `${foodId} exists in the live lineup and must become visible through real scrolling`).toBeTruthy();
      await purchaseVisibleShopItem(page, canvas, foodId, 'restock');
      return;
    }
    await clickTarget(page, canvas, 'shop.refresh');
    await page.waitForTimeout(300);
  }
  throw new Error(`Live shop did not offer required ingredient ${foodId} after 12 refreshes.`);
}

async function teleportAndInteract(page, targetId) {
  await command(page, { action: 'teleport', targetId });
  await page.waitForTimeout(350);
  await page.keyboard.press('Space');
}

test('state-driven macro operates and persists a full live day using actual WebGL input', async ({ page }, testInfo) => {
  test.setTimeout(360_000);
  assertLocalE2EOrigin(baseUrl);
  const trace = [];
  const pageErrors = [];
  const consoleEntries = [];
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
    await showStep(page, `selected ${plan.targetFoodId} from ${initial.unlockedMainFoodIds.join(',')}`, trace);

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
      const latest = await campaign(page, `meal-${meal}-replan`);
      const livePlan = latest.cookingPlans.find(candidate => candidate.targetFoodId === plan.targetFoodId);
      const finalToolId = await executeRecipePlan(page, canvas, livePlan, trace);
      const bentoTargetId = await createBento(page, canvas);
      await dragToWorldTarget(
        page, canvas,
        candidate => candidate.id === `world.cooking.tool.${finalToolId}`, `finished ${plan.targetFoodId}`,
        candidate => candidate.id === bentoTargetId, 'spawned bento',
      );
      const packed = await campaign(page, `meal-${meal}-packed`);
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
      const after = await expect.poll(async () => {
        const state = await snapshot(page, `meal-${meal}-served`);
        return state.money > before.money ? state.money : null;
      }, { timeout: 12_000, message: 'serving the real customer must increase live money' }).not.toBeNull();
      const afterState = await snapshot(page, `meal-${meal}-served-state`);
      observedMealMinutes = Math.max(observedMealMinutes,
        (afterState.hour * 60 + afterState.minute) - (before.hour * 60 + before.minute));
      servedMeals += 1;
      await showStep(page, `served meal ${meal + 1}/${mealsToServe}`, trace);
    }

    expect(servedMeals, 'the policy must complete at least one real sale').toBeGreaterThan(0);
    await clickTarget(page, canvas, 'cooking.early-end');
    await clickTarget(page, canvas, 'confirm.yes');
    await waitForState(page, { scene: 'Mall', phase: 'Afternoon', day: 0 }, 'afternoon-ready');
    await page.waitForTimeout(900);

    // Restock the same catalog-derived recipe for the next day. Random shop lineups
    // are observed on every refresh; the driver never assumes a row or product ID.
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

    const desiredCrafts = Math.min(2, servedMeals);
    for (const requirement of plan.ingredients) {
      let available = inventoryQuantity(await campaign(page, `stock-${requirement.foodId}`), requirement.foodId);
      const desired = requirement.quantity * desiredCrafts;
      while (available < desired) {
        await buyOne(page, canvas, requirement.foodId);
        available += 1;
      }
    }
    const restocked = await campaign(page, 'restock-complete');
    for (const requirement of plan.ingredients)
      expect(inventoryQuantity(restocked, requirement.foodId)).toBeGreaterThanOrEqual(requirement.quantity * desiredCrafts);
    await clickTarget(page, canvas, 'shop.close');

    await teleportAndInteract(page, 'scene.Mall');
    await waitForState(page, { scene: 'Mall', phase: 'Afternoon', day: 0 }, 'mall-after-shopping');
    await teleportAndInteract(page, 'go-home');
    await clickTarget(page, canvas, 'confirm.yes');
    await waitForState(page, { scene: 'Mall', phase: 'Evening', day: 0 }, 'evening-ready');
    await page.waitForTimeout(900);
    await clickTarget(page, canvas, 'phase.rest');
    await waitForState(page, { scene: 'Mall', phase: 'Night', day: 0 }, 'night-ready');
    await page.waitForTimeout(900);
    await clickTarget(page, canvas, 'phase.rest');
    // Settlement registers its continue button as soon as PassDay/save finishes,
    // which can be one frame earlier than the persistent loading overlay releases
    // input. Wait for the real loading lock to clear before sending the click.
    await waitForState(page,
      { scene: 'Settlement', phase: 'Preparation', day: 1, uiLocked: false },
      'settlement-ready');
    await clickTarget(page, canvas, 'settlement.continue');
    const dayOne = await waitForState(page, { scene: 'Mall', phase: 'Preparation', day: 1 }, 'day-one-ready');
    const dayOneInventory = await campaign(page, 'day-one-inventory');
    await page.screenshot({ path: testInfo.outputPath('dynamic-campaign-day-one.png') });

    // Reload the real WebGL page and continue from IndexedDB. This catches save data
    // that looked correct in memory but was never durably flushed.
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.locator('#unity-canvas')).toBeVisible({ timeout: 30_000 });
    await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0), { timeout: 30_000 }).toBeGreaterThan(0);
    await clickTarget(page, canvas, 'start.continue');
    const restored = await waitForState(page, { scene: 'Mall', phase: 'Preparation', day: 1 }, 'save-restored');
    const restoredInventory = await campaign(page, 'save-restored-inventory');
    expect(restored.money).toBe(dayOne.money);
    expect(restored.stamina).toBe(dayOne.stamina);
    expect(restoredInventory.inventory).toEqual(dayOneInventory.inventory);
    await showStep(page, `restored day ${restored.day} with ${restored.money}G`, trace);

    const finalState = restored;
    expect(finalState.stamina).toBe(100);
    expect(finalState.money).toBeGreaterThan(0);
    await page.screenshot({ path: testInfo.outputPath('dynamic-campaign-complete.png') });

    const events = await page.evaluate(() => window.AftertasteE2E.events);
    const unityErrors = events.filter(event => event.type === 'unity-log' && ['Error', 'Exception', 'Assert'].includes(event.level));
    const relevantConsoleErrors = consoleEntries.filter(entry => entry.type === 'error');
    expect({ pageErrors, relevantConsoleErrors, unityErrors })
      .toEqual({ pageErrors: [], relevantConsoleErrors: [], unityErrors: [] });
  } finally {
    const events = await page.evaluate(() => window.AftertasteE2E?.events ?? []).catch(() => []);
    const report = { kind: 'state-driven-actual-input-campaign', maxMeals: mealsToServe, trace, pageErrors, consoleEntries, events };
    await writeFile(testInfo.outputPath('dynamic-campaign-report.json'), JSON.stringify(report, null, 2));
    await testInfo.attach('dynamic-campaign-report.json', { body: JSON.stringify(report, null, 2), contentType: 'application/json' });
    if (holdOpenMs > 0) await page.waitForTimeout(holdOpenMs);
  }
});
