import { test, expect } from '@playwright/test';

const APP_URL = 'http://localhost:4200';

test.describe('User can register and is auto-logged in', () => {
  test('new user signs up and lands on /remittances', async ({ page }) => {
    const unique = Date.now();
    const email = `e2e_${unique}@test.local`;
    const password = 'Pass123!';
    const name = `E2E User ${unique}`;

    await page.goto(`${APP_URL}/register`);
    await expect(page.getByLabel('Display name')).toBeVisible();

    await page.getByLabel('Display name').fill(name);
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Password').fill(password);

    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page).toHaveURL(/\/remittances$/);
    await expect(page.getByRole('button', { name: new RegExp(name) })).toBeVisible();
    await expect(page.getByText(/No remittances yet/)).toBeVisible();
  });
});
