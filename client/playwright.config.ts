import { defineConfig, devices } from '@playwright/test'

// Smoke tests against a running dev stack with demo data (docker compose up).
//   pnpm e2e                      → http://localhost:5173
//   E2E_BASE_URL=http://… pnpm e2e
// They only read data, so they can run against the same database repeatedly.
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  reporter: process.env.CI ? 'github' : 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    trace: 'retain-on-failure',
    launchOptions: process.env.PW_CHROMIUM_PATH ? { executablePath: process.env.PW_CHROMIUM_PATH } : {},
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
