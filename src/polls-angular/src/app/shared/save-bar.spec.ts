import { registerLocaleData } from '@angular/common';
import localePl from '@angular/common/locales/pl';
import { Component, LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SaveBar } from './save-bar';

registerLocaleData(localePl);

describe('SaveBar', () => {
  let fixture: ComponentFixture<SaveBar>;

  async function render(inputs: Record<string, unknown>): Promise<HTMLElement> {
    TestBed.configureTestingModule({ providers: [{ provide: LOCALE_ID, useValue: 'pl' }] });
    fixture = TestBed.createComponent(SaveBar);
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  const button = (el: HTMLElement) => el.querySelector('button') as HTMLButtonElement;
  const status = (el: HTMLElement) => el.querySelector('.status') as HTMLElement;

  it('disables "Zapisz" while nothing changed', async () => {
    const el = await render({ dirty: false, savedAt: '2026-11-05T10:00:00Z' });

    expect(button(el).disabled).toBe(true);
    expect(status(el).textContent).toContain('Zapisano');
  });

  it('enables "Zapisz" and says so when there are unsaved changes', async () => {
    const el = await render({ dirty: true });

    expect(button(el).disabled).toBe(false);
    expect(status(el).textContent).toContain('Niezapisane zmiany');
  });

  it('keeps "Zapisz" disabled for invalid input', async () => {
    const el = await render({ dirty: true, invalid: true });

    expect(button(el).disabled).toBe(true);
  });

  it('highlights the empty state with a "?" icon', async () => {
    const el = await render({ dirty: false, emptyLabel: 'Brak odpowiedzi' });

    expect(status(el).classList).toContain('unanswered');
    expect(status(el).querySelector('mat-icon')?.textContent).toBe('help');
    expect(status(el).textContent).toContain('Brak odpowiedzi');
  });

  it('shows the error instead of the save state', async () => {
    const el = await render({ dirty: true, state: 'error', error: 'Odpowiedź jest za długa.' });

    expect(status(el).classList).toContain('error-text');
    expect(status(el).textContent).toContain('Odpowiedź jest za długa.');
  });

  it('emits save on click outside a form', async () => {
    const el = await render({ dirty: true });
    const saved = vi.fn();
    fixture.componentInstance.save.subscribe(saved);

    button(el).click();

    expect(saved).toHaveBeenCalledOnce();
  });

  it('disables "Zapisz" while saving', async () => {
    const el = await render({ dirty: true, state: 'saving' });

    expect(button(el).disabled).toBe(true);
    expect(status(el).textContent).toContain('Zapisywanie');
  });

  it('hides the empty label once something is saved or typed', async () => {
    let el = await render({ dirty: false, emptyLabel: 'Brak odpowiedzi', savedAt: '2026-11-05T10:00:00Z' });
    expect(status(el).textContent).not.toContain('Brak odpowiedzi');

    TestBed.resetTestingModule();
    el = await render({ dirty: true, emptyLabel: 'Brak odpowiedzi' });
    expect(status(el).textContent).not.toContain('Brak odpowiedzi');
    expect(status(el).textContent).toContain('Niezapisane zmiany');
  });

  it('links the button to what it saves for screen readers', async () => {
    const el = await render({ dirty: true, describedBy: 'question-7' });

    expect(button(el).getAttribute('aria-describedby')).toBe('question-7');
  });
});

@Component({
  imports: [SaveBar],
  template: `
    <form (submit)="$event.preventDefault(); submitted = submitted + 1">
      <input name="title" />
      <app-save-bar [dirty]="dirty" [submit]="true" (save)="emitted = emitted + 1" />
    </form>
  `,
})
class FormHost {
  dirty = true;
  submitted = 0;
  emitted = 0;
}

describe('SaveBar inside a form', () => {
  it('submits the form once and never emits save', async () => {
    const fixture = TestBed.createComponent(FormHost);
    await fixture.whenStable();
    const host = fixture.componentInstance;
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;

    button.click();

    expect(button.type).toBe('submit');
    expect(host.submitted).toBe(1);
    expect(host.emitted).toBe(0);
  });

  it('does not submit while nothing changed (the button is disabled)', async () => {
    const fixture = TestBed.createComponent(FormHost);
    fixture.componentInstance.dirty = false;
    await fixture.whenStable();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;

    button.click();

    expect(button.disabled).toBe(true);
    expect(fixture.componentInstance.submitted).toBe(0);
  });
});
