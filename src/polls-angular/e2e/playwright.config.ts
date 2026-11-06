import { defineConfig, devices } from '@playwright/test';
import { apiConnectionString, settings } from './settings';

const apiUrl = `http://localhost:${settings.apiPort}`;
const webUrl = `http://localhost:${settings.webPort}`;

/**
 * E2E smoke (C2, D49): Playwright starts the API and `ng serve` against the test database from e2e/.env,
 * global setup migrates (and resets) that database, then the main flow runs in Chromium.
 */
export default defineConfig({
  testDir: '.',
  testMatch: '*.e2e.ts',
  outputDir: 'test-results',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list']],
  globalSetup: './db-setup.ts',
  use: {
    baseURL: webUrl,
    locale: 'pl-PL',
    timezoneId: 'Europe/Warsaw',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      // API on its own port, own environment name: no user secrets, no appsettings.Development.json
      command: 'dotnet run --project ../../polls/polls.Api --no-launch-profile',
      url: `${apiUrl}/api/health/live`,
      timeout: 180_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
      env: {
        ASPNETCORE_ENVIRONMENT: 'E2E',
        ASPNETCORE_URLS: apiUrl,
        ConnectionStrings__Polls: apiConnectionString,
        Session__SecureCookie: 'false',
        // the test creates users, polls, questions and a participant in seconds; reruns must not hit 429
        RateLimiting__Create__PermitLimit: '1000',
        RateLimiting__Global__PermitLimit: '100000',
        // no mail and no Graylog from test runs
        EmailApi__Url: '',
        Graylog__Address: '',
      },
    },
    {
      command: `npx ng serve --port ${settings.webPort} --proxy-config e2e/proxy.e2e.mjs`,
      cwd: '..',
      url: webUrl,
      timeout: 180_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
      env: { E2E_API_PORT: String(settings.apiPort) },
    },
  ],
});
