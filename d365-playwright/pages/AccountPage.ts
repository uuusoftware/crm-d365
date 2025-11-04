import { Page } from '@playwright/test';

export class AccountPage {
  constructor(private page: Page) {}

  async setName(name: string) {
    await this.page.fill('input[data-id="name.fieldControl-text-box-input"]', name);
  }

  async save() {
    await this.page.getByRole('menuitem', { name: 'Save (CTRL+S)' }).click();
  }
}