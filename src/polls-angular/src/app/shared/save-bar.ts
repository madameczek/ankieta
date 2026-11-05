import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type SaveState = 'idle' | 'saving' | 'error';

/**
 * Status line + "Zapisz" for a form saved on its own (answers, poll fields).
 * The button is enabled only with unsaved changes; the status tells saving / error / unsaved / saved / empty.
 */
@Component({
  selector: 'app-save-bar',
  imports: [DatePipe, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="status"
      [class.error-text]="state() === 'error'"
      [class.muted]="state() !== 'error' && !showEmpty()"
      [class.empty]="showEmpty()"
      aria-live="polite"
    >
      @switch (true) {
        @case (state() === 'saving') { Zapisywanie… }
        @case (state() === 'error') { {{ error() }} }
        @case (dirty()) { Niezapisane zmiany }
        @case (!!savedAt()) { Zapisano {{ savedAt() | date: 'dd.MM.yyyy HH:mm' }} }
        @case (showEmpty()) {
          <mat-icon aria-hidden="true">help</mat-icon>
          {{ emptyLabel() }}
        }
        @default { Zapisano }
      }
    </span>
    <button
      mat-stroked-button
      [type]="submit() ? 'submit' : 'button'"
      (click)="submit() || save.emit()"
      [disabled]="!dirty() || invalid() || state() === 'saving'"
    >
      Zapisz
    </button>
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      margin-top: 8px;
    }
    .status {
      display: inline-flex;
      align-items: center;
      gap: 4px;
    }
    .empty {
      color: var(--mat-sys-tertiary);
      font-weight: 500;
    }
  `,
})
export class SaveBar {
  readonly state = input<SaveState>('idle');
  readonly error = input<string | null>(null);
  readonly dirty = input.required<boolean>();
  readonly savedAt = input<string | null>(null);
  /** Shown (with a "?" icon) when nothing was ever saved, e.g. an unanswered question; empty = plain "Zapisano". */
  readonly emptyLabel = input('');
  /** Input is not valid (e.g. a required field is empty): the button stays disabled. */
  readonly invalid = input(false);
  /** Inside a form: the button submits the form instead of emitting `save`. */
  readonly submit = input(false);

  readonly save = output<void>();

  protected showEmpty(): boolean {
    return !!this.emptyLabel() && !this.dirty() && !this.savedAt() && this.state() === 'idle';
  }
}
