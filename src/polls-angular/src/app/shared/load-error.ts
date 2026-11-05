import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { isScreenError, toApiError } from '../core/api/api-error';
import { messageFor } from '../core/api/error-messages';

/**
 * Error state of a list (D9 of ui.md: loading/empty/error). Shows the API message only for errors the screen owns;
 * otherwise a neutral text, because the interceptor has already shown the details.
 */
@Component({
  selector: 'app-load-error',
  imports: [MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state" role="alert">
      <p>{{ message() }}</p>
      <button mat-stroked-button (click)="retry.emit()">Spróbuj ponownie</button>
    </div>
  `,
})
export class LoadError {
  readonly error = input.required<unknown>();
  readonly retry = output();

  protected readonly message = computed(() => {
    const error = toApiError(this.error());
    return isScreenError(error) ? messageFor(error.code) : 'Nie udało się wczytać danych.';
  });
}
