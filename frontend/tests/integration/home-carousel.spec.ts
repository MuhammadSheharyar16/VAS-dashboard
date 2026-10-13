import { expect, test } from '@playwright/test';
import { installFakeBackend } from './fakeBackend';

// The rest of the suite asks for reduced motion. This test runs with motion on, because the ring
// only leans toward the pointer, and only turns to a card the mouse rests on, when motion is allowed.
test.use({ contextOptions: { reducedMotion: 'no-preference' } });

test('the mouse turns the home carousel, and the front card still opens on a click', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 900 });
  await installFakeBackend(page);
  await page.goto('/');
  await page.getByRole('heading', { level: 1 }).waitFor();
  await page.waitForTimeout(1500);

  const current = (area: string) => page.getByRole('button', { name: `Show ${area}` });
  const centreOf = async (name: RegExp) => {
    const box = await page.getByRole('link', { name }).boundingBox();
    return { x: box!.x + box!.width / 2, y: box!.y + box!.height / 2 };
  };

  // Resting on a card behind brings it to the front.
  await page.mouse.move(800, 150);
  const vas = await centreOf(/^VAS/);
  await page.mouse.move(vas.x, vas.y);
  await page.waitForTimeout(800);
  await expect(current('VAS')).toHaveAttribute('aria-current', 'true');

  // A mouse that then stands still does not keep the ring spinning.
  await page.waitForTimeout(2200);
  await expect(current('VAS')).toHaveAttribute('aria-current', 'true');

  // Moving on over the next card behind brings that one forward too. Each move here is a single
  // jump: a slow, stepped move would count as resting on a card more than once.
  const operations = await centreOf(/^Operations/);
  await page.mouse.move(operations.x + 50, operations.y + 60);
  await page.waitForTimeout(800);
  await expect(current('Operations')).toHaveAttribute('aria-current', 'true');

  // The front card opens on a click, with the ring leaning in 3D under the pointer.
  // The card is measured once it has finished travelling to the front.
  await page.waitForTimeout(1200);
  const front = await centreOf(/^Operations/);
  await page.mouse.move(front.x, front.y);
  await page.waitForTimeout(600);
  await expect(current('Operations')).toHaveAttribute('aria-current', 'true');
  await page.mouse.click(front.x, front.y);
  await expect(page).toHaveURL(/\/operations$/);
});
