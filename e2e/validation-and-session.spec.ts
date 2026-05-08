import { test, expect, Page } from '@playwright/test';

const APP_URL = 'http://localhost:4200';
const DEMO_EMAIL = 'demo@remittance.test';
const DEMO_PASSWORD = 'Demo123!';

async function login(page: Page) {
  await page.goto(`${APP_URL}/login`);
  await page.getByLabel('Email').fill(DEMO_EMAIL);
  await page.getByLabel('Password').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/remittances$/);
}

test.describe('Dynamic ID validation per document type', () => {
  test('switching ID type updates the hint and rejects invalid input', async ({ page }) => {
    await login(page);
    await page.getByRole('button', { name: 'New remittance' }).click();

    // CC selected by default → numeric, 6-10 digits
    const senderIdNumber = page.getByLabel('ID number').nth(0);
    await senderIdNumber.fill('abc');
    await senderIdNumber.blur();
    await expect(page.getByText(/Invalid format/).first()).toBeVisible();

    // Switch sender ID type to Passport → alphanumeric allowed
    await page.getByLabel('ID type*').first().click();
    await page.getByRole('option', { name: 'Passport' }).click();
    await senderIdNumber.fill('AB123456');
    await senderIdNumber.blur();
    await expect(page.getByText(/Invalid format/)).toHaveCount(0);
  });
});

test.describe('Session persistence', () => {
  test('reloading the page keeps the user logged in', async ({ page }) => {
    await login(page);
    await page.reload();
    await expect(page).toHaveURL(/\/remittances$/);
    await expect(page.getByRole('heading', { name: 'My remittances' })).toBeVisible();
  });
});
