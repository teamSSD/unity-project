import { expect, test } from '@playwright/test';
import { writeFile } from 'node:fs/promises';

const baseUrl = process.env.E2E_WEBGL_URL;

test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.status === testInfo.expectedStatus) return;

  // WebGL canvas는 Playwright의 접근성 스냅샷에 내부 상태가 거의 남지 않는다.
  // 실패 시 브리지 관측값을 함께 보존해야 실제 입력 경로의 막힌 지점을 재현할 수 있다.
  const bridgeState = await page.evaluate(() => ({
    events: window.AftertasteE2E?.events ?? [],
    targetMap: [...(window.AftertasteE2E?.events ?? [])].reverse()
      .find(event => event.type === 'target-map')?.targets ?? [],
  })).catch(error => ({ pageUnavailable: String(error) }));
  await writeFile(testInfo.outputPath('e2e-failure-diagnostic.json'), JSON.stringify({
    errors: testInfo.errors,
    bridgeState,
  }, null, 2));
});

function assertLocalE2EOrigin(url) {
  if (!url) throw new Error('E2E_WEBGL_URL is required.');
  const parsed = new URL(url);
  if (parsed.protocol !== 'http:' || !['localhost', '127.0.0.1'].includes(parsed.hostname) || !/^81\d\d$/.test(parsed.port))
    throw new Error('World-navigation E2E only accepts an isolated localhost port from 8100 through 8199.');
}

async function command(page, value) {
  await page.evaluate(commandValue => window.AftertasteE2E.command(commandValue), value);
}

async function waitForBridge(page) {
  await expect.poll(
    () => page.evaluate(() => window.AftertasteE2E?.events?.some(event => event.type === 'snapshot' && event.label === 'bridge-ready') ?? false),
    { timeout: 30_000, message: 'the WebGL E2E bridge must be live before sending commands' },
  ).toBeTruthy();
}

async function eventsSince(page, index) {
  return page.evaluate(start => window.AftertasteE2E.events.slice(start), index);
}

async function targetMap(page) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await command(page, { action: 'targets' });
  await expect.poll(
    () => eventsSince(page, start).then(events => events.find(event => event.type === 'target-map') ?? null),
    { timeout: 10_000, message: 'missing E2E target map' },
  ).not.toBeNull();
  return (await eventsSince(page, start)).find(event => event.type === 'target-map').targets;
}

async function target(page, id) {
  await expect.poll(
    async () => (await targetMap(page)).find(candidate => candidate.id === id) ?? null,
    { timeout: 20_000, message: `missing target ${id}` },
  ).not.toBeNull();
  return (await targetMap(page)).find(candidate => candidate.id === id);
}

async function targetMatching(page, predicate, label) {
  await expect.poll(
    async () => (await targetMap(page)).find(predicate) ?? null,
    { timeout: 20_000, message: `missing target ${label}` },
  ).not.toBeNull();
  return (await targetMap(page)).find(predicate);
}

async function visibleTargetMatching(page, predicate, label) {
  await expect.poll(
    async () => (await targetMap(page)).find(candidate => predicate(candidate) && candidate.visible) ?? null,
    { timeout: 20_000, message: `missing visible target ${label}` },
  ).not.toBeNull();
  return (await targetMap(page)).find(candidate => predicate(candidate) && candidate.visible);
}

async function panUntilVisible(page, predicate, label) {
  const initial = await targetMatching(page, predicate, label);
  if (initial.visible) return initial;

  // Cooking camera is intentionally horizontal-only. The macro observes a target's
  // screen X then uses the same left/right keyboard input as a player to bring it
  // into view; it never writes a camera/world transform through the bridge.
  const direction = initial.x < 0 ? 'ArrowLeft' : 'ArrowRight';
  await page.keyboard.down(direction);
  try {
    return await visibleTargetMatching(page, predicate, label);
  } finally {
    await page.keyboard.up(direction);
  }
}

async function clickTarget(page, canvas, id) {
  const item = await target(page, id);
  expect(item.visible, `${id} must be visible`).toBeTruthy();
  expect(item.interactable, `${id} must accept pointer input`).toBeTruthy();
  const box = await canvas.boundingBox();
  expect(box).not.toBeNull();
  await page.mouse.click(
    box.x + box.width * (item.x + item.width / 2),
    box.y + box.height * (item.y + item.height / 2),
  );
}

async function worldPoint(page, canvas, predicate, label) {
  const item = await panUntilVisible(page, predicate, label);
  const box = await canvas.boundingBox();
  expect(box).not.toBeNull();
  return {
    x: box.x + box.width * item.x,
    y: box.y + box.height * item.y,
  };
}

async function dragWorldTarget(page, canvas, sourcePrefix, destinationId) {
  const from = await worldPoint(page, canvas, candidate => candidate.id.startsWith(sourcePrefix), sourcePrefix);
  await dragToWorldTargetWithCamera(page, canvas, from, candidate => candidate.id === destinationId, destinationId, 14);
}

async function dragWorldPoints(page, canvas, sourceId, destinationId, steps = 14) {
  const from = await worldPoint(page, canvas, candidate => candidate.id === sourceId, sourceId);
  await dragToWorldTargetWithCamera(page, canvas, from, candidate => candidate.id === destinationId, destinationId, steps);
}

async function dragToWorldTargetWithCamera(page, canvas, from, destinationPredicate, destinationLabel, steps) {
  // Source를 누른 상태에서 도구가 화면 밖이면 카메라만 실제 키보드 입력으로 이동한다.
  // FoodBehavior는 포인터의 현재 월드 좌표를 따라가므로, 사용자가 드래그 중 화면을
  // 스크롤해 반대편 조리대에 놓는 것과 같은 경로가 된다.
  await page.mouse.move(from.x, from.y);
  await page.waitForTimeout(60);
  await page.mouse.down();
  try {
    await page.waitForTimeout(60);
    const destination = await panUntilVisible(page, destinationPredicate, destinationLabel);
    const box = await canvas.boundingBox();
    expect(box).not.toBeNull();
    const to = { x: box.x + box.width * destination.x, y: box.y + box.height * destination.y };
    const count = Math.max(1, steps);
    for (let step = 1; step <= count; step += 1) {
      const fraction = step / count;
      await page.mouse.move(from.x + (to.x - from.x) * fraction, from.y + (to.y - from.y) * fraction);
      await page.waitForTimeout(35);
    }
    await page.waitForTimeout(360);
  } finally {
    await page.mouse.up();
  }
  await page.waitForTimeout(250);
}

async function clickWorldTarget(page, canvas, id) {
  const point = await worldPoint(page, canvas, candidate => candidate.id === id, id);
  // ClickStateUtil은 MouseDown 프레임과 MouseUp 프레임을 분리해 자체 click을
  // 판정한다. DOM의 즉시 click은 두 이벤트가 한 WebGL frame에 합쳐질 수 있으므로
  // 실제 짧은 클릭처럼 press를 유지한다.
  await page.mouse.move(point.x, point.y);
  await page.waitForTimeout(60);
  await page.mouse.down();
  await page.waitForTimeout(80);
  await page.mouse.up();
  await page.waitForTimeout(100);
}

async function buyShopItem(page, canvas, foodId) {
  for (let refreshAttempt = 0; refreshAttempt < 10; refreshAttempt += 1) {
    const itemId = `shop.item.${foodId}`;
    const item = (await targetMap(page)).find(candidate => candidate.id === itemId && candidate.visible && candidate.interactable);
    if (item) {
      const before = await snapshot(page, `before-buy-${foodId}`);
      await clickTarget(page, canvas, itemId);
      await clickTarget(page, canvas, 'shop.detail.plus');
      await clickTarget(page, canvas, 'shop.detail.buy');
      await expect.poll(async () => {
        const state = await snapshot(page, `after-buy-${foodId}`);
        return state.money < before.money ? state : null;
      }, { timeout: 10_000, message: `buying ${foodId} must deduct live money` }).not.toBeNull();
      return;
    }
    const refresh = (await targetMap(page)).find(candidate => candidate.id === 'shop.refresh');
    if (!refresh?.visible || !refresh.interactable) {
      const state = await snapshot(page, `refresh-unavailable-${foodId}`);
      throw new Error(`Shop refresh unavailable for ${foodId}: money=${state.money}, visible=${refresh?.visible}, interactable=${refresh?.interactable}`);
    }
    await clickTarget(page, canvas, 'shop.refresh');
    await page.waitForTimeout(300);
  }
  throw new Error(`Could not find ${foodId} in ten real shop lineups.`);
}

async function snapshot(page, label) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await command(page, { action: 'snapshot', label });
  await expect.poll(
    () => eventsSince(page, start).then(events => events.find(event => event.type === 'snapshot' && event.label === label) ?? null),
    { timeout: 10_000, message: `missing snapshot ${label}` },
  ).not.toBeNull();
  return (await eventsSince(page, start)).find(event => event.type === 'snapshot' && event.label === label);
}

async function waitForScene(page, scene, label) {
  await expect.poll(async () => {
    const state = await snapshot(page, label);
    return state.scene === scene ? state : null;
  }, { timeout: 20_000, message: `expected ${scene} scene` }).not.toBeNull();
}

async function waitForWorldInput(page, label) {
  await expect.poll(async () => {
    const state = await snapshot(page, label);
    return state.uiLocked === false ? state : null;
  }, { timeout: 10_000, message: 'world interaction must wait for the game UI lock to clear' }).not.toBeNull();
}

async function finishFireMinigame(page) {
  await expect.poll(async () => (await snapshot(page, 'fire-started')).activeMinigame,
    { timeout: 10_000, message: 'clicking T001 must start its real fire minigame' }).toBe('FireMiniGame');
  await page.keyboard.down('Space');
  await page.waitForTimeout(3_300);
  await page.keyboard.up('Space');
  await expect.poll(async () => (await snapshot(page, 'fire-finished')).activeMinigame,
    { timeout: 10_000, message: 'the real fire minigame must finish' }).toBe('');
}

async function finishSliceMinigame(page, canvas) {
  await expect.poll(async () => (await snapshot(page, 'slice-started')).activeMinigame,
    { timeout: 10_000, message: 'clicking T004 must start its real slice minigame' }).toBe('SliceMiniGame');
  for (let slice = 0; slice < 6; slice += 1)
    await dragWorldPoints(page, canvas, 'world.cooking.minigame.slice.start', 'world.cooking.minigame.slice.end', 24);
  await expect.poll(async () => (await snapshot(page, 'slice-finished')).activeMinigame,
    { timeout: 10_000, message: 'six real guided mouse cuts must finish the slice minigame' }).toBe('');
}

test('actual WebGL keyboard interaction opens the farm after a travel-only teleport', async ({ page }) => {
  test.setTimeout(90_000);
  assertLocalE2EOrigin(baseUrl);
  const pageErrors = [];
  page.on('pageerror', error => pageErrors.push(error.message));

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible({ timeout: 30_000 });
  await waitForBridge(page);

  await clickTarget(page, canvas, 'start.new-game');
  await waitForScene(page, 'Mall', 'new-game-ready');
  await target(page, 'world.farm-path');

  // 이동 거리만 생략한다. 도착 뒤의 상호작용과 씬 로드는 실제 브라우저 키 입력과 게임 코드다.
  const travelStart = await page.evaluate(() => window.AftertasteE2E.events.length);
  await command(page, { action: 'teleport', targetId: 'farm-path' });
  await expect.poll(
    () => eventsSince(page, travelStart).then(events => events.find(event => event.type === 'bridge-log' && event.message === 'Teleported player to farm-path') ?? null),
    { timeout: 5_000, message: 'travel-only teleport must be acknowledged' },
  ).not.toBeNull();
  await page.keyboard.press('Space');
  await waitForScene(page, 'Garden', 'farm-opened');
  await target(page, 'world.farm.tile.0');

  const allEvents = await page.evaluate(() => window.AftertasteE2E.events);
  const unityErrors = allEvents.filter(event => event.type === 'unity-log' && ['Error', 'Exception', 'Assert'].includes(event.level));
  expect({ pageErrors, unityErrors }).toEqual({ pageErrors: [], unityErrors: [] });
});

test('actual WebGL keyboard interaction opens the first delivery NPC dialogue', async ({ page }) => {
  test.setTimeout(90_000);
  assertLocalE2EOrigin(baseUrl);

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible({ timeout: 30_000 });
  await waitForBridge(page);

  await clickTarget(page, canvas, 'start.new-game');
  await waitForScene(page, 'Mall', 'new-game-ready-npc');
  await target(page, 'world.npc.npc_nimo');

  await command(page, { action: 'teleport', targetId: 'npc.npc_nimo' });
  await page.keyboard.press('Space');
  const advance = await target(page, 'dialogue.advance');
  expect(advance.visible, 'NPC dialogue must be visible after actual Space input').toBeTruthy();
});

test('actual WebGL shop exposes its live product IDs after entering the shop scene', async ({ page }) => {
  test.setTimeout(90_000);
  assertLocalE2EOrigin(baseUrl);
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible({ timeout: 30_000 });
  await waitForBridge(page);

  await clickTarget(page, canvas, 'start.new-game');
  await waitForScene(page, 'Mall', 'new-game-ready-shop-entry');
  await target(page, 'world.scene.Shop');
  await command(page, { action: 'teleport', targetId: 'scene.Shop' });
  await page.waitForTimeout(350);
  await page.keyboard.press('Space');
  await waitForScene(page, 'Shop', 'shop-scene-open');
  await waitForWorldInput(page, 'shop-world-ready');
  await target(page, 'world.shop.Item');
  await command(page, { action: 'teleport', targetId: 'shop.Item' });
  await page.waitForTimeout(350);
  await page.keyboard.press('Space');
  await expect.poll(
    async () => (await targetMap(page)).find(candidate => candidate.id.startsWith('shop.item.') && candidate.visible && candidate.interactable) ?? null,
    { timeout: 20_000, message: 'a live shop product must become actionable through real pointer input' },
  ).not.toBeNull();
  // I009는 시작 상점의 실제 Special 재료다. 행 순서가 아니라 식별자로 선택하고,
  // 실제 상세 패널의 구매 버튼으로 잔액/인벤토리 변경까지 검증한다.
  await buyShopItem(page, canvas, 'I009');
});

test('actual WebGL input cooks and packs the first delivery main through real minigames', async ({ page }) => {
  // 실제 월드 카메라 이동·드래그와 세 미니게임을 모두 거치므로 브라우저/기기별
  // frame rate 변동을 수용한다. 임의 상태 변경 없이 실제 입력만 보내는 장기 경로다.
  test.setTimeout(180_000);
  assertLocalE2EOrigin(baseUrl);

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible({ timeout: 30_000 });
  await waitForBridge(page);

  await clickTarget(page, canvas, 'start.new-game');
  await waitForScene(page, 'Mall', 'new-game-ready-quest');
  await expect.poll(async () => {
    const state = await snapshot(page, 'new-game-inventory-ready');
    return state.inventoryItemCount > 0 ? state : null;
  }, { timeout: 10_000, message: 'a new game must retain its default cooking inventory before entering the quest' }).not.toBeNull();
  // power_room_pair은 campaign의 선행 퀘스트가 없는 첫 배달 퀘스트다.
  await command(page, { action: 'teleport', targetId: 'npc.npc_getoro' });

  let accepted = false;
  for (let dialogueAttempt = 0; dialogueAttempt < 3 && !accepted; dialogueAttempt += 1) {
    await page.keyboard.press('Space');
    for (let input = 0; input < 30 && !accepted; input += 1) {
      const accept = (await targetMap(page)).find(candidate => candidate.id === 'dialogue.choice.accept');
      if (accept?.visible && accept.interactable) {
        await clickTarget(page, canvas, 'dialogue.choice.accept');
        accepted = true;
        break;
      }
      await page.keyboard.press('Space');
      await page.waitForTimeout(190);
    }
    // FirstMeet은 선택지 없이 끝난다. 같은 NPC에게 다시 실제 Space 입력을 보낸 뒤 QuestStart를 진행한다.
    if (!accepted) await page.waitForTimeout(300);
  }

  expect(accepted, 'the real quest dialogue must expose and accept its accept choice').toBeTruthy();
  for (let input = 0; input < 12; input += 1) {
    await page.keyboard.press('Space');
    await page.waitForTimeout(190);
  }
  await expect.poll(async () => {
    const state = await snapshot(page, 'quest-accepted');
    return state.activeOrderCount > 0 ? state : null;
  }, { timeout: 15_000, message: 'accepting the dialogue must create a live delivery order' }).not.toBeNull();

  // 주문으로 해금된 메뉴는 실제 메뉴 선택 UI에서 골라 조리 씬으로 들어간다.
  await command(page, { action: 'teleport', targetId: 'go-home' });
  await page.waitForTimeout(350);
  await page.keyboard.press('Space');
  await clickTarget(page, canvas, 'bento.slot0.main.I044');
  await clickTarget(page, canvas, 'bento.confirm');
  await waitForScene(page, 'Cooking', 'quest-cooking-open');
  // 씬 이름이 바뀐 직후에는 로딩/메뉴 선택 lock이 한 프레임 남아 있을 수 있다.
  // 실제 드래그를 보내기 전에 게임이 입력을 받을 준비가 된 상태를 확인한다.
  await waitForWorldInput(page, 'cooking-world-ready');
  await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.food.I007.'), 'raw ingredient I007');
  await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.food.I019.'), 'raw ingredient I019');
  await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.food.I017.'), 'raw ingredient I017');
  await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.food.I009.'), 'raw ingredient I009');
  await target(page, 'world.cooking.tool.T001');
  await target(page, 'world.cooking.tool.T003');
  await target(page, 'world.cooking.tool.T004');

  // I042: raw I017 → T001 → Fire. 결과는 T001에 남아 최종 I044의 첫 입력이 된다.
  await dragWorldTarget(page, canvas, 'world.cooking.food.I017.', 'world.cooking.tool.T001');
  await clickWorldTarget(page, canvas, 'world.cooking.tool.T001');
  await finishFireMinigame(page);

  // I040: raw I009 → T004 → Slice. 결과를 실제 도구 드래그로 T001에 합친다.
  let t004Ready = false;
  for (let attempt = 0; attempt < 3 && !t004Ready; attempt += 1) {
    await dragWorldTarget(page, canvas, 'world.cooking.food.I009.', 'world.cooking.tool.T004');
    const state = await snapshot(page, `t004-received-I009-attempt-${attempt + 1}`);
    t004Ready = state.t004IngredientCount === 1 && state.t004Cookable;
  }
  expect(t004Ready, 'real drag must put I009 into T004 before the slice minigame starts').toBeTruthy();
  await clickWorldTarget(page, canvas, 'world.cooking.tool.T004');
  await finishSliceMinigame(page, canvas);
  await dragWorldTarget(page, canvas, 'world.cooking.tool.T004', 'world.cooking.tool.T001');

  // I043: raw I007 + I019 → T003 → Mix. 이것도 T001로 실제 전이한다.
  const beforeMixing = await snapshot(page, 'before-real-mixing');
  await dragWorldTarget(page, canvas, 'world.cooking.food.I007.', 'world.cooking.tool.T003');
  await dragWorldTarget(page, canvas, 'world.cooking.food.I019.', 'world.cooking.tool.T003');
  await clickWorldTarget(page, canvas, 'world.cooking.tool.T003');
  await expect.poll(async () => (await snapshot(page, 'mixing-started')).activeMinigame,
    { timeout: 10_000, message: 'dropping the recipe inputs and clicking T003 must start a real minigame' }).toBe('MixMiniGame');
  for (let press = 0; press < 20; press += 1) {
    await page.keyboard.press('Space');
    await page.waitForTimeout(60);
  }
  await expect.poll(async () => {
    const state = await snapshot(page, 'mixing-finished');
    return state.activeMinigame === '' && state.stamina < beforeMixing.stamina ? state : null;
  }, { timeout: 10_000, message: 'the real Mix minigame must finish and consume stamina' }).not.toBeNull();
  await dragWorldTarget(page, canvas, 'world.cooking.tool.T003', 'world.cooking.tool.T001');

  // 세 중간 결과를 T001에서 다시 굽는다. I044 완성 후 도시락도 실제로 자리와 음식 모두 드래그한다.
  await clickWorldTarget(page, canvas, 'world.cooking.tool.T001');
  await finishFireMinigame(page);
  const bentoSlot = await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.bento-slot.'), 'empty bento slot');
  await dragWorldTarget(page, canvas, 'world.cooking.bento-set', bentoSlot.id);
  const bento = await targetMatching(page, candidate => candidate.id.startsWith('world.cooking.bento.'), 'spawned bento');
  let bentoHasMain = false;
  for (let attempt = 0; attempt < 3 && !bentoHasMain; attempt += 1) {
    await dragWorldTarget(page, canvas, 'world.cooking.tool.T001', bento.id);
    bentoHasMain = (await snapshot(page, `bento-received-main-attempt-${attempt + 1}`)).bentoFoodCount === 1;
  }
  expect(bentoHasMain, 'the finished main must enter the spawned bento through a real drag').toBeTruthy();

  let orderCooked = false;
  for (let attempt = 0; attempt < 3 && !orderCooked; attempt += 1) {
    await dragWorldTarget(page, canvas, 'world.cooking.delivery-ticket.quest_power_room_pair', bento.id);
    await page.waitForTimeout(1_800);
    orderCooked = (await snapshot(page, `delivery-bento-packed-attempt-${attempt + 1}`)).firstOrderState === 'Cooked';
  }
  expect(orderCooked, 'attaching the delivery ticket must mark the exact live order as cooked').toBeTruthy();
});
