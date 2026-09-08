import { expect, test } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.E2E_WEBGL_URL;
const planPath = process.env.E2E_PLAY_PLAN;
const requestedDays = Number(process.env.E2E_SURVIVAL_DAYS ?? 3);
const stepDelayMs = Number(process.env.E2E_STEP_DELAY_MS ?? 0);
const holdOpenMs = Number(process.env.E2E_HOLD_OPEN_MS ?? 0);

function assertIsolatedLocalE2EOrigin(url) {
  if (!url) throw new Error('E2E_WEBGL_URL is required.');
  const parsed = new URL(url);
  if (parsed.protocol !== 'http:' || !['localhost', '127.0.0.1'].includes(parsed.hostname) || !/^81\d\d$/.test(parsed.port))
    throw new Error('Survival macro only accepts an isolated localhost port from 8100 through 8199.');
  if (!Number.isInteger(requestedDays) || requestedDays < 1 || requestedDays > 7)
    throw new Error('E2E_SURVIVAL_DAYS must be an integer from 1 through 7.');
}

async function plannedMenusByDay() {
  if (!planPath) throw new Error('E2E_PLAY_PLAN is required; export the baseline plan first.');
  const events = (await readFile(planPath, 'utf8')).trim().split('\n').map(JSON.parse);
  const menus = new Map(events
    .filter(event => event.type === 'MenuSelected' && Number.isInteger(event.day))
    .map(event => [event.day, event.mainIds]));
  if (!menus.get(0)?.length) throw new Error(`No initial MenuSelected event in ${planPath}`);
  return menus;
}

async function waitForEvent(page, start, predicate, message) {
  await expect.poll(
    () => page.evaluate(({ index, expected }) => window.AftertasteE2E.events.slice(index).find(event => {
      for (const [key, value] of Object.entries(expected)) if (event[key] !== value) return false;
      return true;
    }) ?? null, { index: start, expected: predicate }),
    { timeout: 20_000, message },
  ).not.toBeNull();
  return page.evaluate(({ index, expected }) => window.AftertasteE2E.events.slice(index).find(event => {
    for (const [key, value] of Object.entries(expected)) if (event[key] !== value) return false;
    return true;
  }), { index: start, expected: predicate });
}

async function snapshot(page, label) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await page.evaluate(value => window.AftertasteE2E.command({ action: 'snapshot', label: value }), label);
  return waitForEvent(page, start, { type: 'snapshot', label }, `missing snapshot ${label}`);
}

async function targets(page) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'targets' }));
  const map = await waitForEvent(page, start, { type: 'target-map' }, 'missing E2E target map');
  return map.targets;
}

async function target(page, id) {
  await expect.poll(async () => {
    const item = (await targets(page)).find(candidate => candidate.id === id);
    return item?.visible && item?.interactable ? item : null;
  }, { timeout: 20_000, message: `missing visible E2E target ${id}` }).not.toBeNull();
  const found = (await targets(page)).find(item => item.id === id);
  expect(found.visible, `${id} must be visible`).toBeTruthy();
  expect(found.interactable, `${id} must accept actual browser input`).toBeTruthy();
  return found;
}

async function clickTarget(page, canvas, id) {
  const item = await target(page, id);
  const box = await canvas.boundingBox();
  expect(box, 'WebGL canvas must have dimensions').not.toBeNull();
  const point = {
    x: box.x + box.width * (item.x + item.width / 2),
    y: box.y + box.height * (item.y + item.height / 2),
  };
  await page.evaluate(value => console.log(`[AftertasteE2E] click ${value.id} at ${value.x.toFixed(1)},${value.y.toFixed(1)}`), { id, ...point });
  await page.mouse.click(point.x, point.y);
}

async function walkToWorldTarget(page, targetId) {
  for (let attempt = 0; attempt < 28; attempt += 1) {
    const map = await targets(page);
    const player = map.find(item => item.id === 'world.player');
    const destination = map.find(item => item.id === targetId);
    expect(player, 'world.player observation is required for real movement').toBeTruthy();
    expect(destination, `${targetId} observation is required for real movement`).toBeTruthy();
    const delta = destination.x - player.x;
    if (Math.abs(delta) < 0.055) return;
    await page.keyboard.down(delta > 0 ? 'ArrowRight' : 'ArrowLeft');
    await page.waitForTimeout(220);
    await page.keyboard.up(delta > 0 ? 'ArrowRight' : 'ArrowLeft');
  }
  throw new Error(`Could not walk close enough to ${targetId} using actual keyboard input.`);
}

async function waitForScene(page, scene, phase, day, label) {
  await expect.poll(async () => {
    const state = await snapshot(page, label);
    return state.scene === scene && state.phase === phase && state.day === day ? state : null;
  }, { timeout: 25_000, message: `expected ${scene}/${phase}/day ${day}` }).not.toBeNull();
}

async function waitForTransitionInput(page) {
  // ProgressService는 blackout의 midAction에서 먼저 phase를 바꾼다. 그 프레임에는
  // Loading 잠금/검은 오버레이가 아직 입력을 가로챈다. 다음 실제 버튼 입력 전 페이드가
  // 끝날 때까지 기다려, 사람이 화면을 보고 누르는 순서를 재현한다.
  // 새 페이즈 선택창 자체도 UI 잠금을 보유하므로 스냅샷의 uiLocked만으로는
  // Loading 잠금 해제를 구분할 수 없다. 현재 Blackout의 최대 지속 시간보다 긴
  // 짧은 관찰 대기를 둔다.
  await page.waitForTimeout(900);
}

async function waitForWorldInteraction(page, label) {
  // 정산에서 Mall로 돌아올 때 Loading lock이 마지막 프레임까지 남아 있을 수 있다.
  // 플레이어 상호작용은 게임 코드도 이 lock이 풀린 뒤에만 받으므로, E2E 역시 상태를
  // 바꾸지 않고 같은 입력 가능 시점만 관찰해 기다린다.
  await expect.poll(async () => {
    const state = await snapshot(page, label);
    return state.uiLocked === false;
  }, { timeout: 10_000, message: 'Mall world interaction must become input-ready' }).toBeTruthy();
}

async function showStep(page, label) {
  if (stepDelayMs <= 0) return;
  await page.evaluate(value => console.log(`[AftertasteE2E] survival: ${value}`), label);
  await page.waitForTimeout(stepDelayMs);
}

test('actual WebGL macro survives multiple game days using the baseline policy menus', async ({ page }, testInfo) => {
  test.setTimeout(180_000);
  assertIsolatedLocalE2EOrigin(baseUrl);
  const menusByDay = await plannedMenusByDay();
  const pageErrors = [];
  const consoleEntries = [];
  const trace = [];
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('console', message => consoleEntries.push({ type: message.type(), text: message.text() }));

  try {
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
    const canvas = page.locator('#unity-canvas');
    await expect(canvas).toBeVisible({ timeout: 30_000 });
    await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0), { timeout: 30_000 }).toBeGreaterThan(0);

    // 최초 로드 뒤의 실제 창 크기 변경을 재현한다. E2E target의 정규화 좌표로 누른
    // 브라우저 포인터가 Unity 버튼을 계속 누르는지는 아래 scene 전환으로 검증된다.
    await page.setViewportSize({ width: 1280, height: 820 });
    await expect.poll(async () => {
      const box = await canvas.boundingBox();
      return box && Math.abs(box.width / box.height - 16 / 9) < 0.01;
    }, { timeout: 10_000, message: 'WebGL canvas must remain 16:9 after browser resize' }).toBeTruthy();
    await clickTarget(page, canvas, 'start.new-game');
    await waitForScene(page, 'Mall', 'Preparation', 0, 'new-game-ready');

    for (let day = 0; day < requestedDays; day += 1) {
      // 첫 날은 실제 캐릭터를 걸어서 집 상호작용에 도착한다. 다음 날부터는 게임이 제공하는
      // 동일한 Go Home UI를 실제 포인터로 누른다. 어느 경우도 상태/시간을 E2E가 변경하지 않는다.
      if (day === 0) {
        await walkToWorldTarget(page, 'world.go-home');
        await page.keyboard.press('Space');
      } else {
        // 정산에서 복귀한 플레이어는 이미 집 앞 trigger 안에 배치된다. 실제 UI에는
        // 별도 Go Home 버튼이 없고, 화면 안내와 동일하게 Space로 상호작용한다.
        await waitForWorldInteraction(page, `day-${day}-go-home-ready`);
        await page.keyboard.press('Space');
      }
      await showStep(page, `${day}일차 메뉴 선택 화면`);

      // 새 준비 페이즈마다 게임이 도시락을 비운다. 따라서 정책이 정한 그 날의
      // 메뉴를 매일 실제 카드 클릭으로 다시 넣어야 한다.
      const mainIds = menusByDay.get(day);
      if (!mainIds?.length) throw new Error(`No MenuSelected policy for day ${day} in ${planPath}`);
      for (const [slot, mainId] of mainIds.entries())
        await clickTarget(page, canvas, `bento.slot${slot}.main.${mainId}`);
      await clickTarget(page, canvas, 'bento.confirm');
      await waitForScene(page, 'Cooking', 'Morning', day, `day-${day}-cooking-open`);
      await page.screenshot({ path: testInfo.outputPath(`day-${day}-cooking-before-early-end.png`) });

      // 조기 종료도 게임이 원래 제공하는 버튼과 ConfirmModal을 클릭한다. CustomerManager가
      // 실제로 종료하고 SubSceneController가 다음 페이즈/씬을 전환한다.
      await clickTarget(page, canvas, 'cooking.early-end');
      await clickTarget(page, canvas, 'confirm.yes');
      await waitForScene(page, 'Mall', 'Afternoon', day, `day-${day}-afternoon`);

      await waitForTransitionInput(page);
      await page.screenshot({ path: testInfo.outputPath(`day-${day}-afternoon-actions.png`) });
      await clickTarget(page, canvas, 'phase.rest');
      await waitForScene(page, 'Mall', 'Evening', day, `day-${day}-evening`);
      await waitForTransitionInput(page);
      await clickTarget(page, canvas, 'phase.rest');
      await waitForScene(page, 'Mall', 'Night', day, `day-${day}-night`);
      await waitForTransitionInput(page);
      await clickTarget(page, canvas, 'phase.rest');
      // SettlementController는 화면을 연 직후 실제 SaveRoutine에서 PassDay를 수행한다.
      // 따라서 정산 화면 관찰 시점의 Day는 이미 다음 날이다.
      await waitForScene(page, 'Settlement', 'Preparation', day + 1, `day-${day}-settlement`);
      await page.screenshot({ path: testInfo.outputPath(`day-${day}-settlement-ready.png`) });
      await clickTarget(page, canvas, 'settlement.continue');
      await waitForScene(page, 'Mall', 'Preparation', day + 1, `day-${day + 1}-ready`);

      const state = await snapshot(page, `day-${day}-complete`);
      expect(state.stamina, `day ${day} must resume with a valid stamina value`).toBeGreaterThanOrEqual(0);
      expect(state.money, `day ${day} must retain a live economy value`).toBeGreaterThanOrEqual(0);
      trace.push(state);
      await page.screenshot({ path: testInfo.outputPath(`day-${day + 1}-mall.png`) });
    }

    const events = await page.evaluate(() => window.AftertasteE2E.events);
    const unityErrors = events.filter(event => event.type === 'unity-log' && ['Error', 'Exception', 'Assert'].includes(event.level));
    const consoleErrors = consoleEntries.filter(entry => entry.type === 'error');
    expect({ pageErrors, consoleErrors, unityErrors }).toEqual({ pageErrors: [], consoleErrors: [], unityErrors: [] });
  } finally {
    const events = await page.evaluate(() => window.AftertasteE2E?.events ?? []).catch(() => []);
    const report = {
      kind: 'actual-webgl-survival-macro',
      policy: 'BaselinePolicy',
      requestedDays,
      planPath,
      plannedMainIdsByDay: Object.fromEntries(menusByDay),
      completedDays: trace.length,
      snapshots: trace,
      pageErrors,
      consoleEntries,
      unityErrors: events.filter(event => event.type === 'unity-log' && ['Error', 'Exception', 'Assert'].includes(event.level)),
      lastTargetMap: [...events].reverse().find(event => event.type === 'target-map')?.targets ?? [],
    };
    await mkdir(path.dirname(testInfo.outputPath('survival-report.json')), { recursive: true });
    await writeFile(testInfo.outputPath('survival-report.json'), JSON.stringify(report, null, 2));
    await testInfo.attach('survival-report.json', { body: JSON.stringify(report, null, 2), contentType: 'application/json' });
    if (holdOpenMs > 0) await page.waitForTimeout(holdOpenMs);
  }
});
