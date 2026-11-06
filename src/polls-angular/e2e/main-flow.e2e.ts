import { Page, expect, test } from '@playwright/test';

/**
 * Main flow (docs/ui.md, D48): create user → create poll → add questions and a participant → answer as both
 * users → the owner sees the counters and the results. Names are unique per run, so it also works without a reset.
 */
const run = Date.now().toString(36);
const owner = `Ala ${run}`;
const participant = `Ola ${run}`;
const title = `Wycieczka ${run}`;
const q1 = 'Dokąd jedziemy?';
const q2 = 'Ile kilometrów?';

/** "X / Y odpowiedzi" – the DOM text also holds the screen-reader "z" next to the visual slash. */
const counter = (done: number, total: number) => new RegExp(`${done}\\s*/\\s*z?\\s*${total} odpowiedzi`);

/** The toolbar's user button; its accessible name is "<name> – menu użytkownika". */
const userButton = (page: Page) => page.getByRole('button', { name: /menu użytkownika/i });

async function createUser(page: Page, name: string): Promise<void> {
  await page.goto('/users');
  await page.getByRole('button', { name: 'Nowy użytkownik' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Nazwa').fill(name);
  await dialog.getByRole('button', { name: 'Utwórz' }).click();
  await expect(page).toHaveURL(/\/polls$/);
  await expect(userButton(page)).toHaveAccessibleName(`${name} – menu użytkownika`);
}

async function becomeUser(page: Page, name: string): Promise<void> {
  await userButton(page).click();
  await page.getByRole('menuitem', { name: 'Zmień użytkownika' }).click();
  await expect(page).toHaveURL(/\/users$/);
  await page.getByLabel('Szukaj').fill(name);
  await page.getByRole('row', { name }).getByRole('button', { name: 'Wejdź w rolę' }).click();
  await expect(page).toHaveURL(/\/polls$/);
  await expect(userButton(page)).toHaveAccessibleName(`${name} – menu użytkownika`);
}

async function answer(page: Page, question: string, text: string): Promise<void> {
  const item = page.locator('app-answer-item').filter({ hasText: question });
  await item.getByLabel('Twoja odpowiedź').fill(text);
  await item.getByRole('button', { name: 'Zapisz' }).click();
  await expect(item.locator('app-save-bar')).toContainText('Zapisano');
}

test('owner creates a poll, both users answer, the owner sees the results', async ({ page }) => {
  await test.step('create the participant and the owner', async () => {
    await createUser(page, participant);
    await createUser(page, owner);
  });

  await test.step('owner creates a poll with two questions', async () => {
    await page.getByRole('button', { name: 'Nowa ankieta' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Tytuł').fill(title);
    await dialog.getByRole('button', { name: 'Utwórz' }).click();
    await expect(page).toHaveURL(/\/polls\/\d+\/edit$/);
    await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible();

    for (const question of [q1, q2]) {
      await page.getByLabel('Nowe pytanie').fill(question);
      await page.getByRole('button', { name: 'Dodaj pytanie' }).click();
      await expect(page.locator('.question').filter({ hasText: question })).toBeVisible();
    }
  });

  await test.step('owner adds the participant', async () => {
    await page.getByRole('tab', { name: 'Uczestnicy' }).click();
    await page.getByLabel('Dodaj uczestnika').fill(participant);
    await page.getByRole('option', { name: participant }).click();
    const row = page.locator('mat-list-item').filter({ hasText: participant });
    await expect(row).toContainText(counter(0, 2));
  });

  await test.step('owner answers the first question', async () => {
    await page.getByRole('tab', { name: 'Moje odpowiedzi' }).click();
    await answer(page, q1, 'W Bieszczady');
    // the second one stays unanswered
    await expect(page.locator('app-answer-item').filter({ hasText: q2 })).toContainText('Brak odpowiedzi');
  });

  await test.step('participant sees the poll and answers both questions', async () => {
    await becomeUser(page, participant);
    const listRow = page.getByRole('link', { name: new RegExp(title) });
    await expect(listRow).toContainText('Uczestnik');
    await expect(listRow).toContainText(counter(0, 2));
    await listRow.click();

    // a participant only sees the answers, no owner tabs (D45)
    await expect(page.getByRole('tab', { name: 'Edycja' })).toHaveCount(0);
    await answer(page, q1, 'Na Mazury');
    await answer(page, q2, 'Około 60');
  });

  await test.step('owner sees counters and results', async () => {
    await becomeUser(page, owner);
    await expect(page.getByRole('link', { name: new RegExp(title) })).toContainText(counter(1, 2));
    await page.getByRole('link', { name: new RegExp(title) }).click();

    await page.getByRole('tab', { name: 'Uczestnicy' }).click();
    await expect(page.locator('mat-list-item').filter({ hasText: participant })).toContainText(counter(2, 2));

    await page.getByRole('tab', { name: 'Wyniki' }).click();
    const first = page.locator('mat-card').filter({ hasText: q1 });
    await expect(first).toContainText('Odpowiedziało 2 z 2 uczestników');
    await expect(first).toContainText('W Bieszczady');
    await expect(first).toContainText('Na Mazury');
    const second = page.locator('mat-card').filter({ hasText: q2 });
    await expect(second).toContainText('Odpowiedziało 1 z 2 uczestników');
    await expect(second).toContainText('Około 60');
  });

  await test.step('layout: aligned header, no sideways scroll on a phone, new screens start at the top', async () => {
    const left = (selector: string) =>
      page.locator(selector).first().evaluate((e) => Math.round(e.getBoundingClientRect().left));
    await page.setViewportSize({ width: 1280, height: 800 });
    expect(await left('.brand')).toBe(await left('.page-header h1'));

    await page.setViewportSize({ width: 375, height: 400 });
    await expect(userButton(page)).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);

    const content = page.locator('main.content');
    await content.evaluate((e) => e.scrollTo({ top: 200 }));
    expect(await content.evaluate((e) => e.scrollTop)).toBeGreaterThan(0);
    await page.getByRole('link', { name: 'Ankiety' }).click();
    await expect(page).toHaveURL(/\/polls$/);
    expect(await content.evaluate((e) => e.scrollTop)).toBe(0);

    await page.goto('/users');
    await expect(page.getByRole('table')).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  });
});
