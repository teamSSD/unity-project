import { expect, test } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import { createServer } from 'node:http';

const baseUrl = process.env.E2E_WEBGL_URL;
const mealsToServe = Number(process.env.E2E_MEALS_TO_SERVE ?? 2);
const stepDelayMs = Number(process.env.E2E_STEP_DELAY_MS ?? 0);
const holdOpenMs = Number(process.env.E2E_HOLD_OPEN_MS ?? 0);
const maxCampaignDays = Number(process.env.E2E_MAX_CAMPAIGN_DAYS ?? 60);
const campaignTimeoutMs = Number(process.env.E2E_CAMPAIGN_TIMEOUT_MS ?? 3_600_000);
const liveObserverPort = Number(process.env.E2E_LIVE_OBSERVER_PORT ?? 0);

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
  if (!Number.isInteger(maxCampaignDays) || maxCampaignDays < 1 || maxCampaignDays > 365)
    throw new Error('E2E_MAX_CAMPAIGN_DAYS must be an integer from 1 through 365.');
  if (!Number.isInteger(campaignTimeoutMs) || campaignTimeoutMs < 60_000)
    throw new Error('E2E_CAMPAIGN_TIMEOUT_MS must be an integer of at least 60000.');
  if (liveObserverPort !== 0 &&
      (!Number.isInteger(liveObserverPort) || liveObserverPort < 8100 || liveObserverPort > 8199 ||
       liveObserverPort === Number(parsed.port)))
    throw new Error('E2E_LIVE_OBSERVER_PORT must be a different localhost port from 8100 through 8199.');
}

async function startLiveObserver(page) {
  if (liveObserverPort === 0) return null;

  let latestFrame = null;
  let latestFrameAt = 0;
  let latestGameState = null;
  let captureTimer = null;
  let stopped = false;
  const server = createServer((request, response) => {
    response.setHeader('Cache-Control', 'no-store, no-cache, must-revalidate');
    if (request.url?.startsWith('/frame.jpg')) {
      if (!latestFrame) {
        response.writeHead(503, { 'Content-Type': 'text/plain; charset=utf-8', 'Retry-After': '1' });
        response.end('Waiting for the first WebGL frame.');
        return;
      }
      response.writeHead(200, {
        'Content-Type': 'image/jpeg',
        'Content-Length': latestFrame.length,
        'X-Frame-Captured-At': String(latestFrameAt),
      });
      response.end(latestFrame);
      return;
    }
    if (request.url?.startsWith('/status.json')) {
      response.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
      response.end(JSON.stringify({ ready: Boolean(latestFrame), capturedAt: latestFrameAt, game: latestGameState }));
      return;
    }
    if (request.url !== '/' && !request.url?.startsWith('/?')) {
      response.writeHead(404).end();
      return;
    }

    response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    response.end(`<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Aftertaste E2E Live Observer</title><style>
html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#111;color:#fff;font-family:system-ui,sans-serif}
#frame{display:block;width:100%;height:100%;object-fit:contain;pointer-events:none;user-select:none}
#status{position:fixed;left:12px;top:12px;padding:6px 10px;border-radius:6px;background:#000b;font-size:13px}
</style></head><body><img id="frame" alt="자동 E2E 실시간 화면"><div id="status">연결 중…</div><script>
const frame=document.getElementById('frame');const status=document.getElementById('status');
async function refresh(){const now=Date.now();frame.src='/frame.jpg?t='+now;try{const data=await fetch('/status.json?t='+now).then(r=>r.json());const game=data.game;const progress=game?' · '+game.scene+'/'+game.phase+' · '+game.day+'일차 · 퀘스트 '+game.completedQuests+'/'+game.totalQuests+(game.activeMinigame?' · '+game.activeMinigame:''):'';status.textContent=data.ready?'자동 E2E · 입력 격리됨'+progress+' · '+new Date(data.capturedAt).toLocaleTimeString():'첫 화면 대기 중…';}catch{status.textContent='중계 재연결 중…';}}
setInterval(refresh,500);refresh();
</script></body></html>`);
  });

  await new Promise((resolve, reject) => {
    const onError = error => reject(error);
    server.once('error', onError);
    server.listen(liveObserverPort, '127.0.0.1', () => {
      server.off('error', onError);
      resolve();
    });
  });

  const capture = async () => {
    if (stopped || page.isClosed()) return;
    try {
      latestFrame = await page.screenshot({ type: 'jpeg', quality: 72, timeout: 5_000 });
      latestFrameAt = Date.now();
      const observedGameState = await page.evaluate(() => {
        const events = window.AftertasteE2E?.events ?? [];
        const state = [...events].reverse().find(event => event.type === 'snapshot');
        const campaign = [...events].reverse().find(event => event.type === 'campaign-observation');
        if (!state && !campaign) return null;
        const quests = campaign?.questNpcs ?? [];
        const stages = new Map(quests.filter(quest => quest.groupId)
          .map(quest => [quest.groupId, quest.stage]));
        return {
          scene: state?.scene ?? campaign?.scene ?? '',
          phase: state?.phase ?? campaign?.phase ?? '',
          day: state?.day ?? campaign?.day ?? 0,
          activeMinigame: state?.activeMinigame ?? campaign?.minigame?.name ?? '',
          completedQuests: [...stages.values()].filter(stage => stage === 'Completed').length,
          totalQuests: stages.size,
        };
      });
      if (observedGameState) {
        if (observedGameState.totalQuests === 0 && latestGameState?.totalQuests > 0) {
          observedGameState.completedQuests = latestGameState.completedQuests;
          observedGameState.totalQuests = latestGameState.totalQuests;
        }
        latestGameState = observedGameState;
      }
    } catch {
      // Navigation and shutdown can invalidate a frame; the previous frame stays visible.
    } finally {
      if (!stopped) captureTimer = setTimeout(capture, 500);
    }
  };
  void capture();
  process.stdout.write(`Read-only E2E observer: http://127.0.0.1:${liveObserverPort}/\n`);

  return {
    async stop() {
      stopped = true;
      if (captureTimer) clearTimeout(captureTimer);
      await new Promise(resolve => server.close(resolve));
    },
  };
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
  const observation = await eventAfter(page, start,
    { type: 'campaign-observation', label }, `missing campaign observation ${label}`);
  const bindings = new Map((observation.minigameBindings ?? [])
    .map(binding => [binding.recipeId, binding.runtimeName]));
  for (const plan of observation.cookingPlans ?? [])
    for (const step of plan.steps ?? []) step.runtimeMinigameName = bindings.get(step.recipeId) ?? '';
  return observation;
}

async function targetMap(page) {
  const start = await eventCursor(page);
  await command(page, { action: 'targets' });
  return (await eventAfter(page, start, { type: 'target-map' }, 'missing E2E target map')).targets;
}

async function targetMatching(page, predicate, label, visible = false, timeout = 20_000) {
  await expect.poll(async () => {
    return (await targetMap(page)).find(candidate => predicate(candidate) && (!visible || candidate.visible)) ?? null;
  }, { timeout, message: `missing ${visible ? 'visible ' : ''}target ${label}` }).not.toBeNull();
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

async function scrollBentoTargetIntoView(page, canvas, slotIndex, category, foodId) {
  const itemId = `bento.slot${slotIndex}.${category}.${foodId}`;
  const visibleItem = async () => (await targetMap(page))
    .find(candidate => candidate.id === itemId && candidate.visible && candidate.interactable) ?? null;
  if (await visibleItem()) return;

  // A newly opened selection starts at the left, but the fallback sweep also handles
  // a ScrollRect position retained while the modal stays alive.
  for (const [direction, attempts] of [['next', 6], ['previous', 12], ['next', 6]]) {
    for (let attempt = 0; attempt < attempts; attempt += 1) {
      if (await visibleItem()) return;
      await clickTarget(page, canvas, `bento.slot${slotIndex}.${category}.${direction}`);
    }
  }
  expect(await visibleItem(), `${itemId} must become clickable through actual carousel arrows`).toBeTruthy();
}

async function setBentoFoodSelected(page, canvas, slotIndex, category, foodId, selected, label) {
  const isSelected = observation => {
    const slot = observation.selectedMenus?.find(candidate => candidate.slotIndex === slotIndex);
    return category === 'main'
      ? slot?.mainFoodId === foodId
      : (slot?.sideFoodIds ?? []).includes(foodId);
  };
  for (let attempt = 0; attempt < 3; attempt += 1) {
    const before = await campaign(page, `${label}-before-${attempt}`);
    if (isSelected(before) === selected) return;
    await scrollBentoTargetIntoView(page, canvas, slotIndex, category, foodId);
    await clickTarget(page, canvas, `bento.slot${slotIndex}.${category}.${foodId}`);
  }
  expect(isSelected(await campaign(page, `${label}-failed`)), label).toBe(selected);
}

async function choosePhaseAction(page, canvas, targetId, day, phase) {
  for (let attempt = 0; attempt < 3; attempt += 1) {
    await clickTarget(page, canvas, targetId);
    await page.waitForTimeout(180);
    const state = await snapshot(page, `day-${day}-${phase}-${targetId}-selected-${attempt}`);
    if (!state.uiLocked) return state;
  }
  expect((await snapshot(page, `day-${day}-${phase}-${targetId}-selection-failed`)).uiLocked,
    `${targetId} must close the phase selector through actual input`).toBe(false);
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

async function panUntilVisible(page, predicate, label, timeout = 20_000) {
  const initial = await targetMatching(page, predicate, label, false, timeout);
  if (initial.visible) return initial;
  const direction = initial.x < 0 ? 'ArrowLeft' : 'ArrowRight';
  await page.keyboard.down(direction);
  try {
    return await targetMatching(page, predicate, label, true, timeout);
  } finally {
    await page.keyboard.up(direction);
  }
}

async function worldPoint(page, canvas, predicate, label, timeout = 20_000) {
  const item = await panUntilVisible(page, predicate, label, timeout);
  const box = await canvas.boundingBox();
  expect(box).not.toBeNull();
  return { x: box.x + box.width * item.x, y: box.y + box.height * item.y };
}

async function clickWorldTarget(page, canvas, predicate, label, timeout = 20_000) {
  const point = await worldPoint(page, canvas, predicate, label, timeout);
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
    for (let attempt = 0; attempt < 24; attempt += 1) {
      const before = await campaign(page, `slice-minigame-cue-${attempt}`);
      if (before.minigame.name !== expectedMinigame) break;
      if (before.minigame.currentValue >= before.minigame.targetValue) break;
      const previousSlice = before.minigame.currentValue;
      await dragToWorldTarget(
        page, canvas,
        candidate => candidate.id === 'world.cooking.minigame.slice.start', 'slice guide start',
        candidate => candidate.id === 'world.cooking.minigame.slice.end', 'slice guide end',
        24,
      );
      await expect.poll(async () => {
        const after = await campaign(page, `slice-minigame-result-${attempt}`);
        return after.minigame.name !== expectedMinigame || after.minigame.currentValue > previousSlice;
      }, { timeout: 4_000, message: 'each real slice drag must advance the live slice counter' }).toBe(true);
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

async function startMinigame(page, canvas, toolId, expectedMinigame) {
  for (let attempt = 0; attempt < 3; attempt += 1) {
    await clickWorldTarget(page, canvas,
      candidate => candidate.id === `world.cooking.tool.${toolId}`, `tool ${toolId}`);
    for (let poll = 0; poll < 6; poll += 1) {
      const active = (await snapshot(page,
        `minigame-${expectedMinigame}-start-attempt-${attempt}-${poll}`)).activeMinigame;
      if (active === expectedMinigame) return;
      if (active) throw new Error(`${toolId} started unexpected minigame ${active}`);
      await page.waitForTimeout(120);
    }
  }
  expect((await snapshot(page, `minigame-${expectedMinigame}-start-failed`)).activeMinigame,
    `${expectedMinigame} must start through a retried actual tool click`).toBe(expectedMinigame);
}

const supportedRuntimeMinigames = new Set([
  'FireMiniGame',
  'ClickMiniGame',
  'MixMiniGame',
  'SauceMiniGame',
  'SliceMiniGame',
  'GriddleMinigame',
]);

// Menu/shop planning happens in scenes that do not own MiniGameManager, so an
// empty runtimeName means "not observed yet", not "unsupported". Execution in
// Cooking remains strict once the live prefab binding is available.
function isPotentiallySupportedStep(step) {
  return !step.runtimeMinigameName || supportedRuntimeMinigames.has(step.runtimeMinigameName);
}

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

    const expected = step.runtimeMinigameName;
    if (!supportedRuntimeMinigames.has(expected))
      throw new Error(`No actual-input executor for observed ${step.recipeId}/${step.minigameId}: ${expected || 'unbound'}.`);
    await startMinigame(page, canvas, step.toolId, expected);
    await finishMinigame(page, canvas, expected);
    const state = await campaign(page, `verify-${step.outputFoodId}`);
    expect(state.tools.find(tool => tool.toolId === step.toolId)?.resultFoodId,
      `${step.outputFoodId} must be the real result on ${step.toolId}`).toBe(step.outputFoodId);
    produced.add(step.outputFoodId);
  }

  return plan.steps.at(-1).toolId;
}

function chooseFeasibleMain(observation, quantity = 1, reservedRequirements = new Map()) {
  const stock = new Map(observation.inventory.map(item => [item.foodId, item.quantity]));
  return observation.cookingPlans
    .filter(plan => plan.valid && observation.unlockedMainFoodIds.includes(plan.targetFoodId))
    .filter(plan => plan.ingredients.every(item =>
      (stock.get(item.foodId) ?? 0) - Math.min(
        stock.get(item.foodId) ?? 0,
        reservedRequirements.get(item.foodId) ?? 0,
      ) >= item.quantity * quantity))
    .filter(plan => plan.steps.every(isPotentiallySupportedStep))
    .sort((left, right) => right.steps.length - left.steps.length || left.targetFoodId.localeCompare(right.targetFoodId))[0];
}

function canCraftFromInventory(observation, plan, quantity = 1, reservedRequirements = new Map()) {
  const stock = new Map(observation.inventory.map(item => [item.foodId, item.quantity]));
  return plan?.valid && plan.ingredients.every(item =>
    (stock.get(item.foodId) ?? 0) - Math.min(
      stock.get(item.foodId) ?? 0,
      reservedRequirements.get(item.foodId) ?? 0,
    ) >= item.quantity * quantity);
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
        if (!plan?.valid || !plan.steps.every(isPotentiallySupportedStep)) return false;
      }
      return true;
    })[0];
}

function questGroupState(observation) {
  const groups = new Map();
  for (const quest of observation.questNpcs) {
    if (!quest.groupId) continue;
    const previous = groups.get(quest.groupId);
    if (previous)
      expect(quest.stage, `all live NPCs in ${quest.groupId} must share one quest stage`).toBe(previous.stage);
    else
      groups.set(quest.groupId, quest);
  }
  return groups;
}

function assertCompleteQuestGraph(observation) {
  const configured = [...new Set(observation.configuredQuestGroupIds ?? [])].sort();
  const live = [...questGroupState(observation).keys()].sort();
  expect(configured.length, 'the live quest menu catalog must contain delivery quests').toBeGreaterThan(0);
  expect(live, 'every configured delivery quest must have a live NPC in the Mall').toEqual(configured);
  return configured;
}

function completedQuestGroups(observation, configuredQuestGroupIds) {
  const groups = questGroupState(observation);
  return configuredQuestGroupIds.filter(groupId => groups.get(groupId)?.stage === 'Completed');
}

function campaignBlocker(observation, configuredQuestGroupIds) {
  const plans = new Map(observation.cookingPlans.map(plan => [plan.targetFoodId, plan]));
  const groups = questGroupState(observation);
  return configuredQuestGroupIds.map(groupId => {
    const quest = groups.get(groupId);
    const unsupportedFoods = [...(quest?.mainFoodIds ?? []), ...(quest?.sideFoodIds ?? [])]
      .filter(foodId => {
        const plan = plans.get(foodId);
        return !plan?.valid || !plan.steps.every(isPotentiallySupportedStep);
      });
    return {
      groupId,
      stage: quest?.stage ?? 'MissingNpc',
      unlocked: quest?.unlocked ?? false,
      prerequisiteGroupId: quest?.prerequisiteGroupId ?? '',
      unsupportedFoods,
    };
  });
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
  await clickWorldTarget(page, canvas, predicate, 'ordering customer', 75_000);
  await clickWorldTarget(page, canvas, predicate, 'ordering customer', 75_000);
  await expect.poll(async () => {
    const observation = await campaign(page, 'wait-regular-ticket');
    return observation.tickets.find(ticket => !ticket.delivery) ?? null;
  }, { timeout: 12_000, message: 'an actual customer click must create a regular order ticket' }).not.toBeNull();
  return (await campaign(page, 'regular-ticket-ready')).tickets.find(ticket => !ticket.delivery);
}

async function serveRegularMeal(page, canvas, reservedRequirements, trace, label) {
  const before = await snapshot(page, `${label}-before`);
  const ticket = await openNextCustomerOrder(page, canvas);
  expect(ticket.requiredFoodIds, 'a regular customer must request one complete selected menu')
    .toHaveLength(1);
  const requestedFoodId = ticket.requiredFoodIds[0];
  const latest = await campaign(page, `${label}-ticket-plan`);
  expect(latest.customerSalesMainFoodIds, 'the customer order must come from the live Cooking menu')
    .toContain(requestedFoodId);
  const livePlan = latest.cookingPlans.find(candidate => candidate.targetFoodId === requestedFoodId);
  expect(livePlan, `the live customer order ${requestedFoodId} must have a recipe plan`).toBeTruthy();
  expect(canCraftFromInventory(latest, livePlan, 1, reservedRequirements),
    `the selected customer menu ${requestedFoodId} must be craftable without consuming quest reserves`)
    .toBe(true);
  const finalToolId = await executeRecipePlan(page, canvas, livePlan, trace);
  const bentoTargetId = await createBento(page, canvas);
  await dragToWorldTarget(
    page, canvas,
    candidate => candidate.id === `world.cooking.tool.${finalToolId}`, `finished ${requestedFoodId}`,
    candidate => candidate.id === bentoTargetId, 'spawned bento',
  );
  const packed = await campaign(page, `${label}-packed`);
  expect(packed.bentos.find(bento => bento.targetId === bentoTargetId)?.foodIds)
    .toEqual([requestedFoodId]);
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
  await showStep(page, `served ${label}: actual order ${requestedFoodId}`, trace);
  return {
    before,
    after,
    foodId: requestedFoodId,
    elapsedMinutes: (after.hour * 60 + after.minute) - (before.hour * 60 + before.minute),
  };
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
  const itemId = `shop.item.${foodId}`;
  let item = (await targetMap(page)).find(candidate => candidate.id === itemId);
  if (!item) return false;

  const offerState = await campaign(page, `offer-${foodId}`);
  const offer = offerState.shopItems.find(candidate => candidate.foodId === foodId);
  const reserve = offerState.managementFee * 2;
  if (!offer?.canBuyOne || offer.remainingStock <= 0 || offerState.money - offer.unitPrice < reserve)
    return false;

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

async function refreshLineupOnceIfRational(page, canvas, requirements, trace) {
  const state = await campaign(page, 'refresh-policy');
  const unmet = unmetRequirements(state, requirements);
  const reserve = state.managementFee * 2;
  const refresh = (await targetMap(page)).find(candidate => candidate.id === 'shop.refresh');
  const shouldRefresh = unmet.length > 0 && state.refreshCount === 0 && state.canRefresh &&
    state.money - state.refreshCost >= reserve && refresh?.visible && refresh?.interactable;
  if (!shouldRefresh) return false;

  const beforeMoney = state.money;
  await clickTarget(page, canvas, refresh.id);
  await expect.poll(async () => (await campaign(page, 'refresh-policy-confirmed')).refreshCount,
    { timeout: 5_000, message: 'one bounded real shop refresh must complete' }).toBe(1);
  const after = await campaign(page, 'refresh-policy-after');
  expect(after.money).toBe(beforeMoney - state.refreshCost);
  await showStep(page,
    `used one bounded refresh (${state.refreshCost}G); reserve ${reserve}G`,
    trace);
  return true;
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
  const deferredUpgrades = [];
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
      const reserve = observation.managementFee * 2;
      if (beforeMoney - live.nextCost < reserve) {
        deferredUpgrades.push({ storageType, cost: live.nextCost, money: beforeMoney, reserve });
        await showStep(page,
          `deferred ${storageType} upgrade (${live.nextCost}G); ${beforeMoney}G cash must preserve ${reserve}G`,
          trace);
        break;
      }
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
  return deferredUpgrades;
}

async function teleportAndInteract(page, targetId) {
  const start = await eventCursor(page);
  await command(page, { action: 'teleport', targetId });
  await eventAfter(page, start,
    { type: 'bridge-log', message: `Teleported player to ${targetId}` },
    `teleport to ${targetId} did not reach an interaction-ready physics frame`,
    5_000);
  await page.keyboard.press('Space');
}

function unmetRequirements(observation, requirements) {
  return [...requirements]
    .filter(([foodId, desired]) => inventoryQuantity(observation, foodId, true) < desired)
    .map(([foodId]) => foodId);
}

function chooseOperationalStockPlan(observation, reservedRequirements = new Map(), servings = 3) {
  const storageTypeByFood = new Map(
    observation.ingredientStorage.map(item => [item.foodId, item.storageType]),
  );
  const storageByType = new Map(observation.storage.map(item => [item.storageType, item]));

  return observation.cookingPlans
    .filter(plan => plan.valid && observation.unlockedMainFoodIds.includes(plan.targetFoodId))
    .filter(plan => plan.steps.every(isPotentiallySupportedStep))
    .map(plan => {
      // Operational ingredients are inserted first so limited cash buys the stock
      // that can generate tomorrow's revenue before unrelated quest ingredients.
      // Shared ingredients still include the full quest reserve.
      const requirements = new Map();
      const newTypes = new Map();
      let missingUnits = 0;
      for (const ingredient of plan.ingredients) {
        const desired = (reservedRequirements.get(ingredient.foodId) ?? 0) +
          ingredient.quantity * servings;
        requirements.set(ingredient.foodId, desired);
        const have = inventoryQuantity(observation, ingredient.foodId, true);
        missingUnits += Math.max(0, desired - have);
        if (have > 0) continue;
        const storageType = storageTypeByFood.get(ingredient.foodId);
        if (!newTypes.has(storageType)) newTypes.set(storageType, new Set());
        newTypes.get(storageType).add(ingredient.foodId);
      }
      for (const [foodId, desired] of reservedRequirements)
        if (!requirements.has(foodId)) requirements.set(foodId, desired);
      const fits = [...newTypes].every(([storageType, foodIds]) => {
        const storage = storageByType.get(storageType);
        return storage && storage.used + foodIds.size <= storage.capacity;
      });
      return { plan, requirements, missingUnits, fits };
    })
    .filter(candidate => candidate.fits)
    .sort((left, right) => left.missingUnits - right.missingUnits ||
      left.plan.steps.length - right.plan.steps.length ||
      left.plan.targetFoodId.localeCompare(right.plan.targetFoodId))[0] ?? null;
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

async function buyRequirementsWithBoundedRefresh(page, canvas, requirements, trace, allowRefresh = true) {
  let acquisition = await buyAvailableRequirements(page, canvas, requirements);
  if (allowRefresh && acquisition.unmet.length > 0 &&
      await refreshLineupOnceIfRational(page, canvas, requirements, trace))
    acquisition = await buyAvailableRequirements(page, canvas, requirements);
  return acquisition;
}

async function enterCookingDay(page, canvas, day, preferredFoodId = '', reservedRequirements = new Map()) {
  const observation = await campaign(page, `day-${day}-choose-live-menu`);
  const preferred = observation.cookingPlans.find(candidate => candidate.targetFoodId === preferredFoodId);
  const stock = new Map(observation.inventory.map(item => [item.foodId, item.quantity]));
  const isSupportedMain = plan => plan?.valid &&
    observation.unlockedMainFoodIds.includes(plan.targetFoodId) &&
    plan.steps.every(isPotentiallySupportedStep);
  const preferredIsFeasible = isSupportedMain(preferred) &&
    preferred.ingredients.every(item =>
      (stock.get(item.foodId) ?? 0) - Math.min(
        stock.get(item.foodId) ?? 0,
        reservedRequirements.get(item.foodId) ?? 0,
      ) >= item.quantity);
  const fallback = observation.cookingPlans
    .filter(plan => plan.valid && observation.unlockedMainFoodIds.includes(plan.targetFoodId))
    .filter(plan => plan.steps.every(isPotentiallySupportedStep))
    .sort((left, right) => left.targetFoodId.localeCompare(right.targetFoodId))[0];
  const livePlan = preferredIsFeasible ? preferred : chooseFeasibleMain(observation, 1, reservedRequirements) ??
    (isSupportedMain(preferred) ? preferred : fallback);
  expect(livePlan, `day ${day} must have a selectable main menu supported by the macro`).toBeTruthy();
  await teleportAndInteract(page, 'go-home');

  let menuState = await campaign(page, `day-${day}-menu-before-normalize`);
  for (const selected of menuState.selectedMenus ?? []) {
    for (const sideFoodId of selected.sideFoodIds ?? [])
      await setBentoFoodSelected(page, canvas, selected.slotIndex, 'side', sideFoodId, false,
        `day ${day} clears slot ${selected.slotIndex} side ${sideFoodId}`);
    if (selected.slotIndex > 0 && selected.mainFoodId)
      await setBentoFoodSelected(page, canvas, selected.slotIndex, 'main', selected.mainFoodId, false,
        `day ${day} clears slot ${selected.slotIndex} main ${selected.mainFoodId}`);
  }
  menuState = await campaign(page, `day-${day}-menu-normalized`);
  const slotZero = menuState.selectedMenus?.find(candidate => candidate.slotIndex === 0);
  if (slotZero?.mainFoodId !== livePlan.targetFoodId)
    await setBentoFoodSelected(page, canvas, 0, 'main', livePlan.targetFoodId, true,
      `day ${day} selects ${livePlan.targetFoodId}`);
  await expect.poll(async () => {
    const selected = (await campaign(page, `day-${day}-menu-selected`)).selectedMenus ?? [];
    return selected.filter(candidate => candidate.mainFoodId)
      .map(candidate => `${candidate.slotIndex}:${candidate.mainFoodId}`);
  }, { timeout: 10_000, message: `day ${day} must select only ${livePlan.targetFoodId} in slot 0` })
    .toEqual([`0:${livePlan.targetFoodId}`]);
  await clickTarget(page, canvas, 'bento.confirm');
  await waitForState(page, { scene: 'Cooking', day }, `day-${day}-cooking-ready`);
  await expect.poll(async () => (await snapshot(page, `day-${day}-cooking-input-ready`)).uiLocked,
    { timeout: 10_000 }).toBe(false);
  await expect.poll(async () => (await campaign(page, `day-${day}-live-sales-menu`)).customerSalesMainFoodIds,
    { timeout: 10_000, message: `Cooking must capture only the selected ${livePlan.targetFoodId} menu` })
    .toEqual([livePlan.targetFoodId]);
  return livePlan;
}

async function openPhaseShop(page, canvas, day, phase) {
  await choosePhaseAction(page, canvas, 'phase.shopping', day, phase);
  await teleportAndInteract(page, 'scene.Shop');
  await waitForState(page, { scene: 'Shop', phase, day }, `day-${day}-${phase}-shop-ready`);
  await expect.poll(async () => (await snapshot(page, `day-${day}-${phase}-shop-input-ready`)).uiLocked,
    { timeout: 10_000 }).toBe(false);
  await teleportAndInteract(page, 'shop.Item');
  await targetMatching(page, candidate => candidate.id.startsWith('shop.item.'), 'live shop item', true);
}

async function leaveShop(page, canvas, day, phase) {
  await clickTarget(page, canvas, 'shop.close');
  await teleportAndInteract(page, 'scene.Mall');
  await waitForState(page, { scene: 'Mall', phase, day }, `day-${day}-${phase}-mall-after-shopping`);
}

async function visitAndHarvestFarm(page, day, phase, trace) {
  await teleportAndInteract(page, 'farm-path');
  await waitForState(page, { scene: 'Garden', phase, day }, `day-${day}-${phase}-garden-ready`);
  await expect.poll(async () => (await snapshot(page, `day-${day}-${phase}-garden-input-ready`)).uiLocked,
    { timeout: 10_000 }).toBe(false);

  let garden = await campaign(page, `day-${day}-${phase}-farm-observation`);
  expect(garden.farmTiles.length, 'Garden must expose its live farm tiles').toBeGreaterThan(0);
  const harvested = [];
  for (const tile of garden.farmTiles.filter(candidate => !candidate.locked && candidate.harvestable)) {
    const before = inventoryQuantity(garden, tile.cropId);
    for (let attempt = 0; attempt < 3; attempt += 1) {
      await teleportAndInteract(page, tile.targetId);
      garden = await campaign(page, `day-${day}-${phase}-${tile.targetId}-harvested-${attempt}`);
      if (inventoryQuantity(garden, tile.cropId) > before) break;
    }
    expect(inventoryQuantity(garden, tile.cropId), `${tile.targetId} must add its actual crop`)
      .toBeGreaterThan(before);
    harvested.push(tile.cropId);
  }
  await showStep(page,
    harvested.length > 0
      ? `harvested ${harvested.join(',')} during ${phase}`
      : `checked live farm during ${phase}; no crop ready`,
    trace);
  await teleportAndInteract(page, 'scene.Mall');
  await waitForState(page, { scene: 'Mall', phase, day }, `day-${day}-${phase}-mall-after-farm`);
  return harvested;
}

async function finishPhase(page, canvas, day, phase) {
  await teleportAndInteract(page, 'go-home');
  await clickTarget(page, canvas, 'confirm.yes');
  if (phase !== 'Night') {
    const nextPhase = phase === 'Afternoon' ? 'Evening' : 'Night';
    return waitForState(page, { scene: 'Mall', phase: nextPhase, day },
      `day-${day}-${nextPhase}-ready`);
  }
  await waitForState(page,
    { scene: 'Settlement', phase: 'Preparation', day: day + 1, uiLocked: false },
    `day-${day + 1}-settlement-ready`);
  await clickTarget(page, canvas, 'settlement.continue');
  return waitForState(page,
    { scene: 'Mall', phase: 'Preparation', day: day + 1 },
    `day-${day + 1}-ready`);
}

async function advanceAfternoonToNextDay(page, canvas, day, trace = []) {
  // Delivery leaves the campaign in Mall free-roam after choosing Shopping.
  await visitAndHarvestFarm(page, day, 'Afternoon', trace);
  await finishPhase(page, canvas, day, 'Afternoon');
  for (const phase of ['Evening', 'Night']) {
    await choosePhaseAction(page, canvas, 'phase.shopping', day, phase);
    await visitAndHarvestFarm(page, day, phase, trace);
    const state = await campaign(page, `day-${day}-${phase}-delivery-day-before-pass`);
    expect(state.money).toBeGreaterThanOrEqual(state.managementFee);
    await finishPhase(page, canvas, day, phase);
  }
  return snapshot(page, `day-${day + 1}-delivery-day-advanced`);
}

async function operateShoppingPhase(page, canvas, day, phase, requirements, trace, exerciseOffscreen = false) {
  await openPhaseShop(page, canvas, day, phase);
  let offscreenPurchase = null;
  if (exerciseOffscreen) {
    offscreenPurchase = await exerciseOffscreenShopPurchase(page, canvas);
    await showStep(page,
      `dragged offscreen ${offscreenPurchase.foodId} from ${offscreenPurchase.initialY.toFixed(3)} to ${offscreenPurchase.finalY.toFixed(3)} in ${offscreenPurchase.dragCount} gesture(s)`,
      trace);
  }
  await ensureStorageCapacity(page, canvas, requirements, trace);
  const operationalPlan = chooseOperationalStockPlan(
    await campaign(page, `day-${day}-${phase}-operational-plan`),
    requirements,
  );
  const purchaseRequirements = operationalPlan?.requirements ?? requirements;
  if (operationalPlan) await showStep(page,
    `restock ${operationalPlan.plan.targetFoodId} for up to three real sales while preserving quest stock`,
    trace);
  const acquisition = await buyRequirementsWithBoundedRefresh(
    page,
    canvas,
    purchaseRequirements,
    trace,
    phase === 'Night',
  );
  const questState = await campaign(page, `day-${day}-${phase}-quest-stock-after-purchases`);
  const questAcquisition = { state: questState, unmet: unmetRequirements(questState, requirements) };
  await showStep(page,
    questAcquisition.unmet.length === 0
      ? `acquired all required ingredients during day ${day} ${phase}`
      : `day ${day} ${phase} still lacks ${questAcquisition.unmet.join(',')}`,
    trace);
  await leaveShop(page, canvas, day, phase);
  const harvested = await visitAndHarvestFarm(page, day, phase, trace);
  const state = await campaign(page, `day-${day}-${phase}-operations-complete`);
  return {
    acquisition: { state, unmet: unmetRequirements(state, requirements) },
    offscreenPurchase,
    harvested,
    operationalFoodId: operationalPlan?.plan.targetFoodId ?? '',
  };
}

async function operateWorkDayToShop(
  page,
  canvas,
  day,
  preferredFoodId,
  trace,
  { exactSales = 0, reservedRequirements = new Map() } = {},
) {
  expect(day, 'campaign exceeded its maximum live-day safety bound').toBeLessThanOrEqual(maxCampaignDays);
  const plan = await enterCookingDay(page, canvas, day, preferredFoodId, reservedRequirements);
  let servedMeals = 0;
  let observedMealMinutes = 0;
  const saleLimit = exactSales > 0 ? exactSales : 3;

  for (let meal = 0; meal < saleLimit; meal += 1) {
    const live = await campaign(page, `day-${day}-work-capacity-${meal}`);
    const currentPlan = live.cookingPlans.find(candidate => candidate.targetFoodId === plan.targetFoodId);
    if (!canCraftFromInventory(live, currentPlan, 1, reservedRequirements)) break;
    const clock = await snapshot(page, `day-${day}-work-clock-${meal}`);
    if (observedMealMinutes > 0 && clock.remainingPhaseMinutes <= observedMealMinutes + 15) break;
    const served = await serveRegularMeal(
      page,
      canvas,
      reservedRequirements,
      trace,
      `day ${day} campaign sale ${meal + 1}`,
    );
    observedMealMinutes = Math.max(observedMealMinutes, served.elapsedMinutes);
    servedMeals += 1;
  }

  if (exactSales > 0)
    expect(servedMeals, `day ${day} must complete every configured real sale`).toBe(exactSales);
  await showStep(page,
    servedMeals > 0
      ? `operated day ${day} with ${servedMeals} regular sale(s)`
      : `opened day ${day} without consuming quest-reserved stock`,
    trace);

  await clickTarget(page, canvas, 'cooking.early-end');
  await clickTarget(page, canvas, 'confirm.yes');
  await waitForState(page,
    { scene: 'Mall', phase: 'Afternoon', day },
    `day-${day}-campaign-afternoon`);
  return { plan, servedMeals };
}

async function acquireQuestIngredients(
  page,
  canvas,
  questOrder,
  startDay,
  preferredFoodId,
  trace,
  { exactInitialSales = 0, exerciseOffscreen = false } = {},
) {
  let day = startDay;
  const planningState = await campaign(page, `quest-${questOrder.questId}-restock-plan`);
  const foodCraftCounts = new Map();
  for (const foodId of [...questOrder.mainFoodIds, ...questOrder.sideFoodIds])
    foodCraftCounts.set(foodId, (foodCraftCounts.get(foodId) ?? 0) + 1);
  const requirements = aggregateIngredientRequirements(planningState, foodCraftCounts);
  let acquisition = null;
  let firstAttempt = true;
  let offscreenPurchase = null;
  let regularSales = 0;

  do {
    const work = await operateWorkDayToShop(page, canvas, day, preferredFoodId, trace, {
      exactSales: firstAttempt ? exactInitialSales : 0,
      reservedRequirements: requirements,
    });
    regularSales += work.servedMeals;
    preferredFoodId = work.plan.targetFoodId;

    for (const phase of ['Afternoon', 'Evening', 'Night']) {
      const operations = await operateShoppingPhase(
        page,
        canvas,
        day,
        phase,
        requirements,
        trace,
        firstAttempt && exerciseOffscreen && phase === 'Afternoon',
      );
      acquisition = operations.acquisition;
      if (operations.operationalFoodId) preferredFoodId = operations.operationalFoodId;
      if (operations.offscreenPurchase) offscreenPurchase = operations.offscreenPurchase;
      const beforePass = await campaign(page, `day-${day}-${phase}-before-pass`);
      expect(beforePass.money, `${phase} must retain one management fee before phase completion`)
        .toBeGreaterThanOrEqual(beforePass.managementFee);
      await finishPhase(page, canvas, day, phase);
    }
    day += 1;
    firstAttempt = false;
  } while (acquisition.unmet.length > 0 && day <= maxCampaignDays);

  expect(acquisition.unmet,
    `${questOrder.questId} ingredients must become affordable and available by live day ${maxCampaignDays}`)
    .toEqual([]);
  return { deliveryDay: day, preferredFoodId, requirements, offscreenPurchase, regularSales };
}

async function reloadCampaignCheckpoint(
  page,
  canvas,
  expectedState,
  expectedInventory,
  expectedCompletedGroups,
  expectedDeliveredOrderIds,
  diagnostics,
  trace,
) {
  const beforeReload = await page.evaluate(() => ({
    events: window.AftertasteE2E.events,
    errors: window.AftertasteE2E.errors,
    errorCount: window.AftertasteE2E.errorCount(),
  }));
  diagnostics.events.push(...beforeReload.events);
  diagnostics.unityErrors.push(...beforeReload.errors);
  diagnostics.unityErrorCount += beforeReload.errorCount;

  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(canvas).toBeVisible({ timeout: 30_000 });
  await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0),
    { timeout: 30_000 }).toBeGreaterThan(0);
  await clickTarget(page, canvas, 'start.continue');
  const restored = await waitForState(page,
    { scene: 'Mall', phase: 'Preparation', day: expectedState.day },
    `day-${expectedState.day}-save-restored`);
  const restoredCampaign = await campaign(page, `day-${expectedState.day}-save-restored-campaign`);
  expect(restored.money).toBe(expectedState.money);
  expect(restored.stamina).toBe(expectedState.stamina);
  expect(restored.immutableSeed).toBe(expectedState.immutableSeed);
  expect(restoredCampaign.inventory).toEqual(expectedInventory.inventory);
  for (const groupId of expectedCompletedGroups)
    expect(questGroupState(restoredCampaign).get(groupId)?.stage).toBe('Completed');
  for (const questId of expectedDeliveredOrderIds)
    expect(restoredCampaign.orders.find(order => order.questId === questId)?.state).toBe('Delivered');
  await showStep(page,
    `restored day ${restored.day}: ${expectedCompletedGroups.length} quest group(s) persisted`,
    trace);
  return { state: restored, campaign: restoredCampaign };
}

test('state-driven macro completes and persists every configured quest using actual WebGL input', async ({ page }, testInfo) => {
  test.setTimeout(campaignTimeoutMs);
  assertLocalE2EOrigin(baseUrl);
  const trace = [];
  const pageErrors = [];
  const consoleEntries = [];
  const diagnostics = { events: [], unityErrors: [], unityErrorCount: 0 };
  const questRuns = [];
  const deliveredOrderIds = [];
  let configuredQuestGroupIds = [];
  let finalState = null;
  let finalCampaign = null;
  let liveObserver = null;
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('console', message => consoleEntries.push({ type: message.type(), text: message.text() }));

  try {
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
    const canvas = page.locator('#unity-canvas');
    await expect(canvas).toBeVisible({ timeout: 30_000 });
    liveObserver = await startLiveObserver(page);
    await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0), { timeout: 30_000 }).toBeGreaterThan(0);
    await clickTarget(page, canvas, 'start.new-game');
    await expect.poll(async () => (await snapshot(page, 'campaign-ready')).scene,
      { timeout: 20_000, message: 'new game must load the Mall' }).toBe('Mall');

    let currentCampaign = await campaign(page, 'campaign-graph-ready');
    configuredQuestGroupIds = assertCompleteQuestGraph(currentCampaign);
    const initialPlan = chooseFeasibleMain(currentCampaign, mealsToServe);
    expect(initialPlan, 'the live catalog and inventory must offer a feasible opening menu').toBeTruthy();
    let preferredFoodId = initialPlan.targetFoodId;
    let currentDay = 0;
    await showStep(page,
      `discovered ${configuredQuestGroupIds.length} quest group(s); opening with ${preferredFoodId}`,
      trace);

    while (completedQuestGroups(currentCampaign, configuredQuestGroupIds).length < configuredQuestGroupIds.length) {
      const completedBefore = completedQuestGroups(currentCampaign, configuredQuestGroupIds);
      const quest = chooseSupportedQuest(currentCampaign);
      if (!quest)
        throw new Error(`No actionable quest remains: ${JSON.stringify(campaignBlocker(currentCampaign, configuredQuestGroupIds))}`);

      const questStartDay = currentDay;
      await showStep(page,
        `selected unlocked quest ${quest.groupId} from ${quest.npcId} on day ${currentDay}`,
        trace);
      const questOrder = await acceptQuestThroughDialogue(page, canvas, quest, trace);
      const acquisition = await acquireQuestIngredients(
        page,
        canvas,
        questOrder,
        currentDay,
        preferredFoodId,
        trace,
        {
          exactInitialSales: questRuns.length === 0 ? mealsToServe : 0,
          exerciseOffscreen: questRuns.length === 0,
        },
      );
      currentDay = acquisition.deliveryDay;
      preferredFoodId = acquisition.preferredFoodId;

      // Travel may use the semantic teleport hook; cooking, packing, dialogue, and
      // delivery still pass through the real runtime UI and domain services.
      await enterCookingDay(page, canvas, currentDay, preferredFoodId);
      await cookDeliveryOrder(page, canvas, questOrder, trace);
      const beforeQuestDelivery = await snapshot(page, `quest-${quest.groupId}-before-delivery`);
      await clickTarget(page, canvas, 'cooking.early-end');
      await clickTarget(page, canvas, 'confirm.yes');
      await waitForState(page,
        { scene: 'Mall', phase: 'Afternoon', day: currentDay },
        `day-${currentDay}-${quest.groupId}-delivery-afternoon`);
      await page.waitForTimeout(900);
      await choosePhaseAction(page, canvas, 'phase.shopping', currentDay, 'Afternoon');
      await teleportAndInteract(page, `npc.${questOrder.npcId}`);
      await completeDialogue(page, canvas);
      await waitForQuestStage(page, quest.groupId, 'Completed', `quest-${quest.groupId}-completed`);
      const deliveredQuest = await campaign(page, `quest-${quest.groupId}-delivered`);
      expect(deliveredQuest.orders.find(order => order.questId === questOrder.questId)?.state).toBe('Delivered');
      expect((await snapshot(page, `quest-${quest.groupId}-reward`)).money)
        .toBeGreaterThan(beforeQuestDelivery.money);
      const completedAfter = completedQuestGroups(deliveredQuest, configuredQuestGroupIds);
      expect(completedAfter.length, `${quest.groupId} must advance the completed campaign`).toBe(completedBefore.length + 1);
      expect(completedAfter).toContain(quest.groupId);
      deliveredOrderIds.push(questOrder.questId);
      await showStep(page,
        `completed ${quest.groupId} (${completedAfter.length}/${configuredQuestGroupIds.length})`,
        trace);

      const persistedState = await advanceAfternoonToNextDay(page, canvas, currentDay, trace);
      currentDay += 1;
      const persistedCampaign = await campaign(page, `day-${currentDay}-${quest.groupId}-persisted`);
      await page.screenshot({
        path: testInfo.outputPath(`quest-${completedAfter.length}-${quest.groupId}-complete.png`),
      });
      const restored = await reloadCampaignCheckpoint(
        page,
        canvas,
        persistedState,
        persistedCampaign,
        completedAfter,
        deliveredOrderIds,
        diagnostics,
        trace,
      );
      currentCampaign = restored.campaign;
      finalState = restored.state;
      finalCampaign = restored.campaign;
      questRuns.push({
        groupId: quest.groupId,
        npcId: questOrder.npcId,
        questId: questOrder.questId,
        startDay: questStartDay,
        deliveryDay: acquisition.deliveryDay,
        restoredDay: restored.state.day,
        requiredFoodIds: [...questOrder.mainFoodIds, ...questOrder.sideFoodIds],
        requiredIngredients: Object.fromEntries(acquisition.requirements),
        regularSales: acquisition.regularSales,
      });
      expect(currentDay, 'campaign exceeded its maximum live-day safety bound').toBeLessThanOrEqual(maxCampaignDays);
    }

    expect(finalCampaign, 'the full campaign must produce a restored final observation').toBeTruthy();
    expect(completedQuestGroups(finalCampaign, configuredQuestGroupIds)).toEqual(configuredQuestGroupIds);
    for (const questId of deliveredOrderIds)
      expect(finalCampaign.orders.find(order => order.questId === questId)?.state).toBe('Delivered');
    expect(deliveredOrderIds.length).toBe(configuredQuestGroupIds.length);
    expect(finalState.stamina).toBe(100);
    expect(finalState.money).toBeGreaterThan(0);
    await page.screenshot({ path: testInfo.outputPath('dynamic-campaign-complete.png') });

    const finalDiagnostics = await page.evaluate(() => ({
      events: window.AftertasteE2E.events,
      errors: window.AftertasteE2E.errors,
      errorCount: window.AftertasteE2E.errorCount(),
    }));
    const unityErrors = [...diagnostics.unityErrors, ...finalDiagnostics.errors];
    const totalUnityErrorCount = diagnostics.unityErrorCount + finalDiagnostics.errorCount;
    const relevantConsoleErrors = consoleEntries.filter(entry => entry.type === 'error');
    expect({ pageErrors, relevantConsoleErrors, unityErrorCount: totalUnityErrorCount, unityErrors })
      .toEqual({ pageErrors: [], relevantConsoleErrors: [], unityErrorCount: 0, unityErrors: [] });
  } finally {
    const events = [...diagnostics.events,
      ...await page.evaluate(() => window.AftertasteE2E?.events ?? []).catch(() => [])];
    const liveDiagnostics = await page.evaluate(() => ({
      errors: window.AftertasteE2E?.errors ?? [],
      errorCount: window.AftertasteE2E?.errorCount?.() ?? 0,
    })).catch(() => ({ errors: [], errorCount: 0 }));
    const unityErrors = [...diagnostics.unityErrors, ...liveDiagnostics.errors];
    const report = {
      kind: 'state-driven-full-quest-actual-input-campaign',
      maxMeals: mealsToServe,
      maxCampaignDays,
      configuredQuestGroupIds,
      questRuns,
      deliveredOrderIds,
      finalState,
      finalQuestStages: finalCampaign
        ? Object.fromEntries([...questGroupState(finalCampaign)].map(([groupId, quest]) => [groupId, quest.stage]))
        : {},
      trace,
      pageErrors,
      consoleEntries,
      unityErrorCount: diagnostics.unityErrorCount + liveDiagnostics.errorCount,
      unityErrors,
      events,
    };
    try {
      await writeFile(testInfo.outputPath('dynamic-campaign-report.json'), JSON.stringify(report, null, 2));
      await testInfo.attach('dynamic-campaign-report.json', { body: JSON.stringify(report, null, 2), contentType: 'application/json' });
      if (holdOpenMs > 0) await page.waitForTimeout(holdOpenMs);
    } finally {
      await liveObserver?.stop();
    }
  }
});
