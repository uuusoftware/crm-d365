// helpers.ts
import { Locator } from '@playwright/test';

/**
 * Clicks the given locator only if it is visible.
 * @param locator - Playwright Locator object
 */
export async function clickIfVisible(locator: Locator) {
  if (await locator.isVisible()) {
    await locator.click();
  }
}
