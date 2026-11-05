import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * "X of Y" counter (D35): the text comes from the content, the state from the numbers.
 * Incomplete → amber with "?" (like "Brak odpowiedzi"), complete → ✓, nothing to count (total 0) → plain muted text.
 */
@Component({
  selector: 'app-progress',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.unanswered]': "state() === 'incomplete'",
    '[class.complete]': "state() === 'complete'",
    '[class.muted]': "state() === 'none'",
  },
  template: `
    @if (state() !== 'none') {
      <mat-icon aria-hidden="true">{{ state() === 'complete' ? 'check_circle' : 'help' }}</mat-icon>
    }
    <ng-content />
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      vertical-align: middle;
    }
    mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }
    :host(.complete) {
      color: var(--mat-sys-on-surface-variant);
    }
    :host(.complete) mat-icon {
      color: var(--mat-sys-primary);
    }
  `,
})
export class Progress {
  readonly done = input.required<number>();
  readonly total = input.required<number>();

  protected readonly state = computed(() =>
    this.total() <= 0 ? 'none' : this.done() >= this.total() ? 'complete' : 'incomplete',
  );
}
