import { test, expect } from '@playwright/test';

const APP_URL = 'http://localhost:4200';

test.describe('Public tracking page', () => {
  test('anonymous visitor can look up a remittance by tracking code', async ({ page, context }) => {
    await context.clearCookies();

    await page.goto(`${APP_URL}/track`);
    await expect(page.getByLabel('Tracking code')).toBeVisible();

    await page.getByLabel('Tracking code').fill('RM-DEMO01');
    await page.getByRole('button', { name: 'Check status' }).click();

    await expect(page.getByText('RM-DEMO01')).toBeVisible();
    await expect(page.getByText('Maria Useche')).toBeVisible();
    await expect(page.getByText('Completed')).toBeVisible();
  });

  test('shows error for an unknown tracking code', async ({ page, context }) => {
    await context.clearCookies();

    await page.goto(`${APP_URL}/track`);
    await page.getByLabel('Tracking code').fill('RM-NOPE99');
    await page.getByRole('button', { name: 'Check status' }).click();

    await expect(page.getByText('No remittance found with that tracking code.')).toBeVisible();
  });
});
