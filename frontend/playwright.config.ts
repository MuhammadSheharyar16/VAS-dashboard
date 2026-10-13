import { defineConfig, devices } from '@playwright/test';

const PORT = 5179;

/**
 * The integration tests drive the assembled app in a real browser. The app is the real one; the
 * tests answer for Entra ID and the API at the network (tests/integration/fakeBackend.ts).
 */
export default defineConfig({
  testDir: './tests/integration',
  // The dev server compiles each module the first time a browser asks for it. With several
  // browsers starting at once that first page can take longer than the default five seconds.
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? 'github' : 'list',
  use: {
    baseURL: `http://localhost:${PORT}`,
    trace: 'on-first-retry',
    // The front card of the home carousel floats for ever, and a click waits for its target to stand
    // still. The tests ask for reduced motion, which the app honours by stopping decorative movement.
    contextOptions: { reducedMotion: 'reduce' },
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: `npx vite --port ${PORT} --strictPort`,
    url: `http://localhost:${PORT}`,
    reuseExistingServer: !process.env.CI,
    env: { VITE_API_BASE_URL: '/api/v1' },
  },
});
