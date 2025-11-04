import { test } from '@playwright/test';
import { chromium } from 'playwright'; // import chromium explicitly

test('login to D365', async () => {
  // Launch persistent context manually
  const context = await chromium.launchPersistentContext('./d365-session', {
    headless: false,
  });

  const page = await context.newPage();
  await page.goto(' https://cmdynamicsdev.crm3.dynamics.com/main.aspx?appid=25ab0e88-05d6-ef11-a732-000d3af4f5b6&forceUCI=1&pagetype=entityrecord&etn=contact&id=83c26eb5-09e3-ef11-be21-6045bdf9b4af');

  console.log('Login manually if needed');

  // Keep the browser open until you close manually
  await page.waitForTimeout(60000); 
});