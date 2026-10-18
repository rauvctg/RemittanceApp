import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config — Remittance Manager E2E.
 * Reuses the API + Angular dev server already running on the developer's machine
 * (see README "Run locally"). If they aren't up, start them in two terminals:
 *   1) dotnet run --project src/RemittanceManager.Api
 *   2) cd frontend && npm start
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  fullyParallel: false,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10_000,
    /**
     * `slowMo` adds a delay (ms) between each Playwright action so a human
     * can follow the run live. Set to 0 in CI; 600-1000 ms is comfortable
     * for demos. Toggle via env var: `$env:PW_SLOWMO=1200; npm run e2e:headed`
     */
    launchOptions: {
      slowMo: Number(process.env['PW_SLOWMO'] ?? 600)
    }
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
});
