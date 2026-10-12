import { expect, test } from '@playwright/test';

test('an executive reads the summary, filters it, then explores the VAS sections', async ({ page }) => {
  const consoleErrors: string[] = [];
  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });

  await page.goto('/');

  // The home route is the Executive Summary, with the menu the backend returned.
  await expect(page).toHaveURL(/\/executive-summary$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Executive Summary' })).toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Reporting areas' }).getByRole('link')).toHaveCount(6);
  await expect(page.getByText('Mock data')).toBeVisible();

  // Six KPI cards, each with a value.
  const cards = page.getByRole('article');
  await expect(cards).toHaveCount(6);
  await expect(page.getByRole('article', { name: 'Applications received' })).toContainText(/\d/);

  // A filter goes into the URL, so the view can be shared and survives a refresh.
  await page.getByLabel('Country').selectOption('AE');
  await expect(page).toHaveURL(/country=AE/);
  await page.reload();
  await expect(page.getByLabel('Country')).toHaveValue('AE');

  // The filter travels with the user to the VAS report.
  await page.getByRole('link', { name: 'VAS' }).click();
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

  // A planned area shows its placeholder.
  await page.getByRole('link', { name: /Finance/ }).click();
  await expect(page.getByRole('heading', { name: 'Finance is coming soon' })).toBeVisible();

  expect(consoleErrors).toEqual([]);
});

test('an unknown address shows the not-found page inside the shell', async ({ page }) => {
  await page.goto('/no-such-page');

  await expect(page.getByRole('heading', { name: 'Page not found' })).toBeVisible();
  await page.getByRole('link', { name: 'Go to Executive Summary' }).click();
  await expect(page).toHaveURL(/\/executive-summary$/);
});
