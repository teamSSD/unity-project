import { expect, test } from '@playwright/test';
import { readFile, writeFile } from 'node:fs/promises';

const baseUrl = process.env.E2E_WEBGL_URL;
const planPath = process.env.E2E_PLAY_PLAN;
const stepDelayMs = Number(process.env.E2E_STEP_DELAY_MS ?? 0);
const holdOpenMs = Number(process.env.E2E_HOLD_OPEN_MS ?? 0);

async function showStep(page, label) {
  if (stepDelayMs <= 0) return;
  await page.evaluate(value => console.log(`[AftertasteE2E] macro: ${value}`), label);
  await page.waitForTimeout(stepDelayMs);
}

async function expectBrowserAudioPlayed(page, directory, filename) {
  await expect.poll(
    () => page.evaluate(({ dir, name }) => window.__aftertasteBrowserAudioPlayCalls?.some(src => src.endsWith(`/StreamingAssets/Audio/${dir}/${name}`)) ?? false, { dir: directory, name: filename }),
    { timeout: 10_000, message: `${filename} must be requested through browser audio` }
  ).toBeTruthy();
}

function assertIsolatedLocalE2EOrigin(url) {
  if (!url) throw new Error('E2E_WEBGL_URL is required.');
  const parsed = new URL(url);
  if (parsed.protocol !== 'http:' || !['localhost', '127.0.0.1'].includes(parsed.hostname) || !/^81\d\d$/.test(parsed.port))
    throw new Error('Policy pilot only accepts an isolated localhost port from 8100 through 8199.');
}

async function firstPlannedMenuIds() {
  if (!planPath) throw new Error('E2E_PLAY_PLAN is required; generate it with Tools/Simulation/Export Baseline Playtest Plan.');
  const lines = (await readFile(planPath, 'utf8')).trim().split('\n');
  const menu = lines.map(JSON.parse).find(event => event.type === 'MenuSelected');
  if (!menu?.mainIds?.length) throw new Error(`No MenuSelected decision in ${planPath}`);
  return menu.mainIds;
}

async function getTargets(page) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await page.evaluate(() => window.AftertasteE2E.command({ action: 'targets' }));
  await expect.poll(
    () => page.evaluate(index => window.AftertasteE2E.events.slice(index).find(event => event.type === 'target-map') ?? null, start),
    { timeout: 10_000 }
  ).not.toBeNull();
  return page.evaluate(index => window.AftertasteE2E.events.slice(index).find(event => event.type === 'target-map').targets, start);
}

async function clickTarget(page, canvas, id) {
  await expect.poll(async () => (await getTargets(page)).find(target => target.id === id) ?? null,
    { timeout: 10_000, message: `missing E2E target ${id}` }).not.toBeNull();
  const target = (await getTargets(page)).find(candidate => candidate.id === id);
  expect(target.visible, `${id} must be on-screen before actual pointer input`).toBeTruthy();
  expect(target.interactable, `${id} must be interactable before actual pointer input`).toBeTruthy();
  const box = await canvas.boundingBox();
  expect(box).not.toBeNull();
  await page.mouse.click(box.x + box.width * (target.x + target.width / 2), box.y + box.height * (target.y + target.height / 2));
}

async function snapshot(page, label) {
  const start = await page.evaluate(() => window.AftertasteE2E.events.length);
  await page.evaluate(value => window.AftertasteE2E.command({ action: 'snapshot', label: value }), label);
  await expect.poll(
    () => page.evaluate(index => window.AftertasteE2E.events.slice(index).find(event => event.type === 'snapshot' && event.label === 'policy-pilot') ?? null, start),
    { timeout: 10_000 }
  ).not.toBeNull();
  return page.evaluate(index => window.AftertasteE2E.events.slice(index).find(event => event.type === 'snapshot' && event.label === 'policy-pilot'), start);
}

async function walkToWorldTarget(page, targetId) {
  for (let attempt = 0; attempt < 24; attempt += 1) {
    const targets = await getTargets(page);
    const player = targets.find(target => target.id === 'world.player');
    const destination = targets.find(target => target.id === targetId);
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

test('BaselinePolicy menu decision is enacted through actual WebGL pointer input', async ({ page }, testInfo) => {
  assertIsolatedLocalE2EOrigin(baseUrl);
  const plannedMainIds = await firstPlannedMenuIds();
  const pageErrors = [];
  const consoleEntries = [];
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('console', message => consoleEntries.push({ type: message.type(), text: message.text() }));
  await page.addInitScript(() => {
    window.__aftertasteBrowserAudioPlayCalls = [];
    window.__aftertasteBrowserAudioMediaErrors = [];
    const originalPlay = HTMLMediaElement.prototype.play;
    HTMLMediaElement.prototype.play = function (...args) {
      const source = this.currentSrc || this.src;
      if (source.includes('/StreamingAssets/Audio/')) {
        window.__aftertasteBrowserAudioPlayCalls.push(source);
        this.addEventListener('error', () => window.__aftertasteBrowserAudioMediaErrors.push(this.currentSrc || this.src), { once: true });
      }
      return originalPlay.apply(this, args);
    };
  });

  try {
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
    const canvas = page.locator('#unity-canvas');
    await expect(canvas).toBeVisible({ timeout: 30_000 });
    await expect.poll(() => page.evaluate(() => window.AftertasteE2E?.events?.length ?? 0), { timeout: 30_000 }).toBeGreaterThan(0);
    await expect(page.evaluate(() => typeof window.AftertasteE2E?.command)).resolves.toBe('function');
    await clickTarget(page, canvas, 'start.new-game');
    await expectBrowserAudioPlayed(page, 'UI', 'sfx_ui_button_click.mp3');
    await showStep(page, 'New Game clicked — campaign profile is loading');

    // E2E New Game은 튜토리얼 완료·고정 시드 프로필로 시작한다.
    // 메뉴 선택과 이동은 아래 실제 pointer/keyboard input이다.
    await expect.poll(async () => (await snapshot(page, 'policy-pilot')).scene,
      { timeout: 15_000, message: 'New Game must finish loading the campaign Mall' }).toBe('Mall');
    const profile = await snapshot(page, 'policy-pilot');
    expect(profile.campaignProfile).toBeTruthy();
    expect(profile.immutableSeed, 'long-run policy plan seed must be applied to the actual game').toBe(42);
    expect(profile.scene).toBe('Mall');
    await expectBrowserAudioPlayed(page, 'BGM', 'bgm_mall_theme.mp3');

    await walkToWorldTarget(page, 'world.go-home');
    await showStep(page, 'Arrived at home interaction');
    await page.keyboard.press('Space');
    await expectBrowserAudioPlayed(page, 'UI', 'sfx_ui_book.mp3');
    await showStep(page, 'Opened menu selection');
    for (const [slot, mainId] of plannedMainIds.entries())
    {
      await clickTarget(page, canvas, `bento.slot${slot}.main.${mainId}`);
      await showStep(page, `Selected planned menu ${mainId} for slot ${slot + 1}`);
    }
    const afterSelection = await snapshot(page, 'policy-pilot');
    expect(afterSelection.selectedMenuCount, `all planned menus ${plannedMainIds.join(', ')} must be selected`).toBe(plannedMainIds.length);

    const events = await page.evaluate(() => window.AftertasteE2E.events);
    const consoleErrors = consoleEntries.filter(entry => entry.type === 'error');
    const unityErrors = events.filter(event => event.type === 'unity-log' && ['Error', 'Exception', 'Assert'].includes(event.level));
    const browserAudioMediaErrors = await page.evaluate(() => window.__aftertasteBrowserAudioMediaErrors ?? []);
    if (holdOpenMs > 0) await page.waitForTimeout(holdOpenMs);
    expect({ pageErrors, consoleErrors, unityErrors, browserAudioMediaErrors }).toEqual({ pageErrors: [], consoleErrors: [], unityErrors: [], browserAudioMediaErrors: [] });
  } finally {
    const events = await page.evaluate(() => window.AftertasteE2E?.events ?? []).catch(() => []);
    const decision = { policy: 'BaselinePolicy', plannedMainIds, planPath };
    const browserAudioPlayCalls = await page.evaluate(() => window.__aftertasteBrowserAudioPlayCalls ?? []).catch(() => []);
    const browserAudioMediaErrors = await page.evaluate(() => window.__aftertasteBrowserAudioMediaErrors ?? []).catch(() => []);
    const diagnostics = { pageErrors, consoleEntries, events, browserAudioPlayCalls, browserAudioMediaErrors };
    // Playwright reporter 설정과 무관하게 실패 폴더에 원본 진단을 남긴다.
    await writeFile(testInfo.outputPath('policy-decision.json'), JSON.stringify(decision, null, 2));
    await writeFile(testInfo.outputPath('browser-console.json'), JSON.stringify(diagnostics, null, 2));
    await testInfo.attach('policy-decision.json', {
      body: JSON.stringify(decision, null, 2),
      contentType: 'application/json',
    });
    await testInfo.attach('browser-console.json', {
      body: JSON.stringify(diagnostics, null, 2),
      contentType: 'application/json',
    });
  }
});
