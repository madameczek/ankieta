import { registerLocaleData } from '@angular/common';
import localePl from '@angular/common/locales/pl';
import { LOCALE_ID } from '@angular/core';
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

    expect(status(el).classList).toContain('empty');
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

  it('submits the form instead of emitting when used inside one', async () => {
    const el = await render({ dirty: true, submit: true });
    const saved = vi.fn();
    fixture.componentInstance.save.subscribe(saved);

    button(el).click();

    expect(button(el).type).toBe('submit');
    expect(saved).not.toHaveBeenCalled();
  });
});
