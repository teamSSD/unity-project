import { expect, test } from '@playwright/test';

const baseUrl = process.env.E2E_WEBGL_URL;

function assertIsolatedLocalE2EOrigin(url) {
  if (!url) throw new Error('E2E_WEBGL_URL is required; never run this against a general development origin.');
  const parsed = new URL(url);
  const loopback = parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1';
  const dedicatedPort = /^81\d\d$/.test(parsed.port);
  if (parsed.protocol !== 'http:' || !loopback || !dedicatedPort) {
    throw new Error(`Refusing unsafe E2E URL '${url}'. Use an isolated localhost port from 8100 through 8199.`);
  }
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

  const bootScreenshot = await page.screenshot({ path: testInfo.outputPath('01-boot.png') });
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'snapshot', label: 'before-new-game-click' }));

  // 실제 Canvas 좌표와 브라우저 키 입력이다. 브리지 command로 이동/튜토리얼을 우회하지 않는다.
  const box = await canvas.boundingBox();
  expect(box, 'Unity Canvas bounds should be available').not.toBeNull();
  await page.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.38);
  await page.waitForTimeout(500);
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'snapshot', label: 'after-new-game-click' }));
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
  const snapshot = label => events.find(event => event.type === 'snapshot' && event.label === label);
  const beforeClick = snapshot('before-new-game-click');
  const afterClick = snapshot('after-new-game-click');
  const afterSpace = snapshot('after-space-input');
  expect(beforeClick, 'pre-input snapshot is required').toBeTruthy();
  expect(afterClick, 'post-click snapshot is required').toBeTruthy();
  expect(afterSpace, 'post-Space snapshot is required').toBeTruthy();
  expect(afterClick.browserReceivedAt, 'click observation must arrive after pre-input state').toBeGreaterThan(beforeClick.browserReceivedAt);
  expect(afterSpace.browserReceivedAt, 'Space observation must arrive after click observation').toBeGreaterThan(afterClick.browserReceivedAt);
  expect(afterClickScreenshot.equals(bootScreenshot), 'New Game canvas click must change the rendered game state').toBeFalsy();
  expect(afterSpaceScreenshot.equals(afterClickScreenshot), 'Space must advance the tutorial after the canvas is focused').toBeFalsy();
  expect(errors, JSON.stringify(errors, null, 2)).toEqual([]);
});
