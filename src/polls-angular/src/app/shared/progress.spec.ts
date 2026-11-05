import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Progress } from './progress';

@Component({
  imports: [Progress],
  template: `<app-progress [done]="done()" [total]="total()">{{ done() }} / {{ total() }} odpowiedzi</app-progress>`,
})
class Host {
  readonly done = signal(0);
  readonly total = signal(0);
}

describe('Progress', () => {
  async function render(done: number, total: number): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.done.set(done);
    fixture.componentInstance.total.set(total);
    await fixture.whenStable();
    return fixture.nativeElement.querySelector('app-progress') as HTMLElement;
  }

  it('marks an incomplete count amber with a "?" icon', async () => {
    const el = await render(2, 5);

    expect(el.classList).toContain('unanswered');
    expect(el.querySelector('mat-icon')?.textContent).toBe('help');
    expect(el.textContent).toContain('2 / 5 odpowiedzi');
  });

  it('marks a complete count with a check', async () => {
    const el = await render(5, 5);

    expect(el.classList).toContain('complete');
    expect(el.querySelector('mat-icon')?.textContent).toBe('check_circle');
  });

  it('shows plain muted text when there is nothing to count', async () => {
    const el = await render(0, 0);

    expect(el.classList).toContain('muted');
    expect(el.querySelector('mat-icon')).toBeNull();
  });
});
