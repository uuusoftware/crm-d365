import { test, expect } from '@playwright/test';
import { clickIfVisible } from '../utils/utils';
import { AccountPage } from '../pages/AccountPage';
import * as dotenv from 'dotenv';
dotenv.config();

test('Test Duplicated Records Error Message', async ({ page, browserName }) => {
  test.skip(browserName === 'webkit', 'Skipping on WebKit');

  const account = new AccountPage(page);

  await page.goto('https://cmdynamicsdev.crm3.dynamics.com/main.aspx?appid=25ab0e88-05d6-ef11-a732-000d3af4f5b6');
  await page.getByRole('textbox', { name: 'someone@example.com' }).click();
  await page.getByRole('textbox', { name: 'someone@example.com' }).fill('longviewsystems@CSSAca.onmicrosoft.com');
  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('textbox', { name: 'Enter the password for' }).click();
  const CRM_PASSWORD = process.env.CRM_PASSWORD;
  if (!CRM_PASSWORD) throw new Error('CRM_PASSWORD is not set. Add it to .env or your environment variables.');
  await page.getByRole('textbox', { name: 'Enter the password for' }).fill(CRM_PASSWORD);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.pause();
  // Unpause after authentication
  await page.getByRole('button', { name: 'Yes' }).click();
  await page.getByText('Contacts').click();
  await page.getByRole('menuitem', { name: 'New', exact: true }).click();
  await page.getByRole('button', { name: 'Search records for Account' }).click();
  await page.getByText('Allison Company').click();
  await page.getByRole('textbox', { name: 'First Name' }).click();
  await page.getByRole('textbox', { name: 'First Name' }).fill('Charles');
  await page.getByRole('textbox', { name: 'First Name' }).press('Tab');
  await page.getByRole('textbox', { name: 'Last Name' }).fill('J');
  await page.getByRole('textbox', { name: 'Last Name' }).press('Tab');
  await page.getByRole('button', { name: 'Search records for Account' }).press('Tab');
  await page.getByRole('textbox', { name: 'Email' }).fill('cj@gmail.com');
  await account.save();
  // TODO: find what is wrong with condition:
  // const ignoreButton = page.getByRole('button', { name: 'Ignore and save' });
  // await clickIfVisible(ignoreButton);
  await page.getByRole('button', { name: 'Ignore and save' }).click();
  await expect(page.getByText('Contacts must be unique for each Account')).toBeVisible();
});