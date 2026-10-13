import { expect, test } from '@playwright/test';
import { installFakeBackend, TEST_ACCESS_TOKEN } from './fakeBackend';

test('an executive signs in, reads the summary, filters it, then explores the VAS sections', async ({ page }) => {
  const apiCalls = await installFakeBackend(page);

  const consoleErrors: string[] = [];
  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });

  // Opening the app asks for a sign-in first; only then is the dashboard shown.
  await page.goto('/');

  // The home screen greets the user and shows the areas the backend returned on the carousel.
  await expect(page.getByRole('heading', { level: 1, name: 'Welcome, Test' })).toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Reporting areas' }).getByRole('link')).toHaveCount(6);

  // The card at the front opens its report.
  await page.getByRole('link', { name: /^Executive Summary/ }).click();
  await expect(page).toHaveURL(/\/executive-summary$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Executive Summary' })).toBeVisible();
  await expect(page.getByText('Mock data')).toBeVisible();

  // Six KPI cards, each with a value.
  const cards = page.getByRole('article');
  await expect(cards).toHaveCount(6);
  await expect(page.getByRole('article', { name: 'Applications received' })).toContainText(/\d/);

  // A filter goes into the URL, so the view can be shared and survives a refresh.
  await page.getByLabel('Country').selectOption('AE');
  await expect(page).toHaveURL(/country=AE/);
  await expect
    .poll(() => apiCalls.some((call) => call.path === '/executive-summary' && call.query.get('countryCode') === 'AE'))
    .toBe(true);
  await page.reload();
  await expect(page.getByLabel('Country')).toHaveValue('AE');

  // The filter travels with the user, by way of the home screen, to the VAS report.
  await page.getByRole('link', { name: 'All areas' }).click();
  await expect(page).toHaveURL(/\/\?country=AE$/);
  // A card behind comes to the front on the first click and opens on the next.
  await page.getByRole('link', { name: /^VAS/ }).click();
  await expect(page).toHaveURL(/\/\?country=AE$/);
  await page.getByRole('link', { name: /^VAS/ }).click();
  await expect(page).toHaveURL(/\/vas\?.*country=AE/);
  await expect(page.getByRole('heading', { level: 1, name: 'VAS' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'By country' })).toBeVisible();

  // Each of the six sections opens.
  for (const [tab, heading] of [
    ['Outcomes', 'Outcome mix'],
    ['Processing', 'Stage funnel'],
    ['Revenue', 'Revenue over time'],
    ['Visa types', 'Performance by visa type'],
    ['Centres', 'Centre performance'],
  ] as const) {
    await page.getByRole('tab', { name: tab }).click();
    await expect(page.getByRole('heading', { name: heading })).toBeVisible();
  }

  // The centre table sorts and pages.
  await page.getByRole('button', { name: 'Sort Received descending' }).click();
  await expect(page.getByRole('table', { name: 'Centre performance' }).getByRole('row').nth(1)).toContainText('Dubai Centre');
  await page.getByRole('button', { name: 'Next page' }).click();
  await expect(page.getByText('Page 2 of 2')).toBeVisible();

  // A planned area is behind the ring until the carousel is turned to it; it shows its placeholder.
  await page.getByRole('link', { name: 'All areas' }).click();
  await page.getByRole('button', { name: 'Show Finance' }).click();
  await page.getByRole('link', { name: /^Finance/ }).click();
  await expect(page.getByRole('heading', { name: 'Finance is coming soon' })).toBeVisible();

  // Every report came from the API, and every call carried the token from the sign-in.
  expect(apiCalls.length).toBeGreaterThan(10);
  expect(apiCalls.every((call) => call.authorization === `Bearer ${TEST_ACCESS_TOKEN}`)).toBe(true);

  expect(consoleErrors).toEqual([]);
});

test('an unknown address shows the not-found page inside the shell', async ({ page }) => {
  await installFakeBackend(page);

  // Sign-in returns the user to the address they asked for.
  await page.goto('/no-such-page');

  await expect(page.getByRole('heading', { name: 'Page not found' })).toBeVisible();
  await page.getByRole('link', { name: 'Go to Executive Summary' }).click();
  await expect(page).toHaveURL(/\/executive-summary$/);
});
