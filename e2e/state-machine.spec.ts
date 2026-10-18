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

test.describe('State machine and field locking', () => {
  test('Pending → Sent locks business fields and disables delete', async ({ page }) => {
    const beneficiary = `SM ${Date.now()}`;

    await login(page);

    // Create a Pending remittance
    await page.getByRole('button', { name: 'New remittance' }).click();
    await page.getByLabel('Sender name').fill('SM Sender');
    await page.getByLabel('ID number').nth(0).fill('1001234567');
    await page.getByLabel('Beneficiary name').fill(beneficiary);
    await page.getByLabel('ID number').nth(1).fill('1009876543');
    await page.getByLabel('Amount').fill('15.00');
    await page.getByRole('button', { name: 'Create' }).click();
    await expect(page).toHaveURL(/\/remittances$/);

    const newRow = page.getByRole('row', { name: new RegExp(beneficiary) });
    await expect(newRow).toBeVisible();

    // Open edit (first action button in the row)
    await newRow.getByRole('button').first().click();
    await expect(page).toHaveURL(/\/remittances\/[^/]+\/edit$/);

    // Change Status to Sent
    await page.getByLabel('Status').click();
    await page.getByRole('option', { name: 'Sent' }).click();
    await page.getByRole('button', { name: 'Save changes' }).click();

    // Back at the list
    await expect(page).toHaveURL(/\/remittances$/);
    const sentRow = page.getByRole('row', { name: new RegExp(beneficiary) });
    await expect(sentRow).toContainText('Sent');

    // Delete button must be disabled with the locked tooltip
    const deleteBtn = sentRow.getByRole('button').last();
    await expect(deleteBtn).toBeDisabled();

    // Re-open edit and confirm the locked banner appears
    await sentRow.getByRole('button').first().click();
    await expect(page.getByText(/Only the status can be changed/)).toBeVisible();

    // Sender name should be disabled (locked)
    await expect(page.getByLabel('Sender name*')).toBeDisabled();

    // Cleanup: move to Cancelled so we can delete
    await page.getByLabel('Status').click();
    await page.getByRole('option', { name: 'Cancelled' }).click();
    await page.getByRole('button', { name: 'Save changes' }).click();
    await expect(page).toHaveURL(/\/remittances$/);
    const cancelledRow = page.getByRole('row', { name: new RegExp(beneficiary) });
    await cancelledRow.getByRole('button').last().click();
    await page.getByRole('button', { name: 'Delete' }).click();
    await expect(page.getByRole('cell', { name: beneficiary })).toHaveCount(0);
  });
});
