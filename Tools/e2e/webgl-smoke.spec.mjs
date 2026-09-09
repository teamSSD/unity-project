import { expect, test } from '@playwright/test';

const baseUrl = process.env.E2E_WEBGL_URL;
const holdOpenMs = Number(process.env.E2E_HOLD_OPEN_MS ?? 0);

function assertIsolatedLocalE2EOrigin(url) {
  if (!url) throw new Error('E2E_WEBGL_URL is required; never run this against a general development origin.');
  const parsed = new URL(url);
  const loopback = parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1';
  const dedicatedPort = /^81\d\d$/.test(parsed.port);
  if (parsed.protocol !== 'http:' || !loopback || !dedicatedPort) {
    throw new Error(`Refusing unsafe E2E URL '${url}'. Use an isolated localhost port from 8100 through 8199.`);
  }
}

async function getUiTargets(page) {
  const before = await page.evaluate(() => window.AftertasteE2E.events.length);
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'targets' }));
  await expect.poll(
    () => page.evaluate(start => window.AftertasteE2E.events.slice(start).find(event => event.type === 'target-map') ?? null, before),
    { timeout: 10_000 }
  ).not.toBeNull();
  return page.evaluate(start => window.AftertasteE2E.events.slice(start).find(event => event.type === 'target-map').targets, before);
}

async function clickTarget(page, canvas, id) {
  await waitForTarget(page, id);
  const targets = await getUiTargets(page);
  const target = targets.find(candidate => candidate.id === id);
  expect(target.visible, `${id} must be visible before input`).toBeTruthy();
  expect(target.interactable, `${id} must be interactable before input`).toBeTruthy();
  const box = await canvas.boundingBox();
  expect(box, 'Unity Canvas bounds should be available').not.toBeNull();
  await page.mouse.click(
    box.x + box.width * (target.x + target.width / 2),
    box.y + box.height * (target.y + target.height / 2)
  );
}

async function waitForTarget(page, id) {
  await expect.poll(
    async () => (await getUiTargets(page)).find(candidate => candidate.id === id) ?? null,
    { timeout: 10_000, message: `registered E2E UI target '${id}' is required` }
  ).not.toBeNull();
}

test('fresh WebGL build boots, emits E2E events, and accepts real tutorial input', async ({ page }, testInfo) => {
  assertIsolatedLocalE2EOrigin(baseUrl);
  const consoleEntries = [];
  page.on('console', message => consoleEntries.push({ type: message.type(), text: message.text() }));
  page.on('pageerror', error => consoleEntries.push({ type: 'pageerror', text: error.message }));

  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible({ timeout: 30_000 });

  // E2E 전용 빌드 표식과 Unity 부팅 이벤트를 먼저 확인한다.
  await expect.poll(
    () => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0),
    { timeout: 30_000 }
  ).toBeGreaterThan(0);
  await expect(page.evaluate(() => typeof window.AftertasteE2E?.command)).resolves.toBe('function');

  // splash/bridge-ready만으로는 게임 UI가 준비된 것이 아니다.
  // 첫 증적은 실제 New Game 버튼이 표시되고 눌릴 수 있는 뒤에 남긴다.
  await waitForTarget(page, 'start.new-game');
  const bootScreenshot = await page.screenshot({ path: testInfo.outputPath('01-boot.png') });
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'snapshot', label: 'before-new-game-click' }));

  // Unity가 제공한 관측 좌표를 사용하되, 실행은 실제 Canvas 마우스 입력이다.
  // 브리지는 버튼을 호출하거나 게임 상태를 변경할 수 없다.
  await clickTarget(page, canvas, 'start.new-game');
  await expect.poll(async () => {
    await page.evaluate(() => window.AftertasteE2E.command({ action: 'snapshot', label: 'after-new-game-click' }));
    await page.waitForTimeout(100);
    return page.evaluate(() => [...window.AftertasteE2E.events].reverse()
      .find(event => event.type === 'snapshot' && event.label === 'after-new-game-click')?.scene ?? '');
  }, {
    timeout: 20_000,
    message: 'actual New Game input must finish the Managers → Mall transition',
  }).toBe('Mall');
  const afterClickScreenshot = await page.screenshot({ path: testInfo.outputPath('02-new-game-click.png') });
  await page.keyboard.press('Space');
  await page.waitForTimeout(800);
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'snapshot', label: 'after-space-input' }));
  const afterSpaceScreenshot = await page.screenshot({ path: testInfo.outputPath('03-space-input.png') });

  const events = await page.evaluate(() => window.AftertasteE2E.events);
  const errors = consoleEntries.filter(entry => entry.type === 'error' || entry.type === 'pageerror');
  await testInfo.attach('e2e-events.json', {
    body: JSON.stringify(events, null, 2),
    contentType: 'application/json',
  });
  await testInfo.attach('browser-console.json', {
    body: JSON.stringify(consoleEntries, null, 2),
    contentType: 'application/json',
  });

  expect(events.some(event => event.type === 'snapshot')).toBeTruthy();
  const snapshot = label => [...events].reverse()
    .find(event => event.type === 'snapshot' && event.label === label);
  const beforeClick = snapshot('before-new-game-click');
  const afterClick = snapshot('after-new-game-click');
  const afterSpace = snapshot('after-space-input');
  expect(beforeClick, 'pre-input snapshot is required').toBeTruthy();
  expect(afterClick, 'post-click snapshot is required').toBeTruthy();
  expect(afterSpace, 'post-Space snapshot is required').toBeTruthy();
  expect(afterClick.browserReceivedAt, 'click observation must arrive after pre-input state').toBeGreaterThan(beforeClick.browserReceivedAt);
  expect(afterSpace.browserReceivedAt, 'Space observation must arrive after click observation').toBeGreaterThan(afterClick.browserReceivedAt);
  expect(afterClick.scene, 'actual New Game input must leave the title scene').not.toBe(beforeClick.scene);
  expect(afterClick.scene, 'actual New Game input must load the mall scene').toBe('Mall');
  expect(afterClickScreenshot.equals(bootScreenshot), 'New Game canvas click must change the rendered game state').toBeFalsy();
  expect(afterSpaceScreenshot.equals(afterClickScreenshot), 'Space must advance the tutorial after the canvas is focused').toBeFalsy();
  expect(errors, JSON.stringify(errors, null, 2)).toEqual([]);

  // 사람이 실제 실행 장면을 보려는 경우에만 열린 창을 잠시 유지한다.
  if (holdOpenMs > 0) await page.waitForTimeout(holdOpenMs);
});
