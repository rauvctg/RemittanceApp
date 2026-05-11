import { test, expect, Page } from '@playwright/test';

/**
 * E2E coverage for the user-facing acceptance criteria of US-001.
 * See docs/USER_STORY.md for the full AC list.
 *
 * These tests hit the real Angular app on http://localhost:4200, which in turn
 * hits the real ASP.NET API on http://localhost:5170 and the real SQL Server
 * database. Use `npm run e2e:headed` to watch them run live.
 */

const APP_URL = 'http://localhost:4200';
const DEMO_EMAIL = 'demo@remittance.test';
const DEMO_PASSWORD = 'Demo123!';

async function login(page: Page, email = DEMO_EMAIL, password = DEMO_PASSWORD) {
  await page.goto(`${APP_URL}/login`);
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
}

test.describe('AC-06 · routes are protected', () => {
  test('anonymous visitor cannot reach /remittances', async ({ page, context }) => {
    await context.clearCookies();
    await page.goto(`${APP_URL}/remittances`);
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  });
});

test.describe('AC-03 · invalid credentials show an inline error', () => {
  test('shows error message and stays on /login', async ({ page }) => {
    await login(page, 'wrong@user.com', 'badpass');
    await expect(page.getByText('Invalid email or password.')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });
});

test.describe('AC-02 + AC-08 · login and list', () => {
  test('demo user can log in and see their remittances table', async ({ page }) => {
    await login(page);

    await expect(page).toHaveURL(/\/remittances$/);
    await expect(page.getByRole('heading', { name: 'My remittances' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Demo User/ })).toBeVisible();

    // The seeded data should be visible
    await expect(page.getByRole('cell', { name: 'Maria Useche' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Carlos Perez' })).toBeVisible();
  });
});

test.describe('AC-09 + AC-11 · create and delete a remittance', () => {
  test('user creates a new remittance, sees it in the list, then deletes it', async ({ page }) => {
    const beneficiary = `E2E Test ${Date.now()}`;

    await login(page);

    // Go to the form
    await page.getByRole('button', { name: 'New remittance' }).click();
    await expect(page).toHaveURL(/\/remittances\/new$/);

    // AC-12: Save button starts disabled (form invalid)
    const saveBtn = page.getByRole('button', { name: 'Create' });
    await expect(saveBtn).toBeDisabled();

    // Sender (the form has two "ID number" fields, scope by index)
    await page.getByLabel('Sender name').fill('E2E Sender');
    await page.getByLabel('ID number').nth(0).fill('1001234567');

    // Beneficiary
    await page.getByLabel('Beneficiary name').fill(beneficiary);
    await page.getByLabel('ID number').nth(1).fill('1009876543');

    // Transfer details
    await page.getByLabel('Amount').fill('42.50');

    // Now the button becomes enabled
    await expect(saveBtn).toBeEnabled();
    await saveBtn.click();

    // Back at the list, the new row is there
    await expect(page).toHaveURL(/\/remittances$/);
    const newRow = page.getByRole('row', { name: new RegExp(beneficiary) });
    await expect(newRow).toBeVisible();
    await expect(newRow).toContainText('42.50');

    // Delete it (AC-11)
    await newRow.getByRole('button').last().click();
    await page.getByRole('button', { name: 'Delete' }).click();

    await expect(page.getByRole('cell', { name: beneficiary })).toHaveCount(0);
  });
});

test.describe('AC-07 · logout clears the session', () => {
  test('after sign out the user is back at /login', async ({ page }) => {
    await login(page);
    await expect(page).toHaveURL(/\/remittances$/);

    await page.getByRole('button', { name: /Demo User/ }).click();
    await page.getByRole('menuitem', { name: 'Sign out' }).click();

    await expect(page).toHaveURL(/\/login$/);

    // Going back to a protected route still bounces to /login (no leftover token)
    await page.goto(`${APP_URL}/remittances`);
    await expect(page).toHaveURL(/\/login$/);
  });
});
