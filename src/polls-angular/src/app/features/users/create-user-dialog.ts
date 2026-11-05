import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Limits, User } from '../../core/api/api.models';
import { toApiError } from '../../core/api/api-error';
import { messageFor } from '../../core/api/error-messages';
import { SessionStore } from '../../core/session/session.store';

/** Creates a user; on success the new user is current (D10). Closes with the user. */
@Component({
  selector: 'app-create-user-dialog',
  imports: [MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Nowy użytkownik</h2>
    <form (submit)="$event.preventDefault(); save()">
      <mat-dialog-content>
        <mat-form-field class="full-width">
          <mat-label>Nazwa</mat-label>
          <input
            matInput
            cdkFocusInitial
            [value]="name()"
            (input)="name.set($any($event.target).value)"
            [attr.maxlength]="maxLength"
            autocomplete="off"
            required
          />
          <mat-hint align="end">{{ name().length }} / {{ maxLength }}</mat-hint>
        </mat-form-field>
        @if (fieldError()) {
          <p class="error-text" role="alert">{{ fieldError() }}</p>
        }
        @if (formError()) {
          <p class="error-text" role="alert">{{ formError() }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Anuluj</button>
        <button mat-flat-button type="submit" [disabled]="saving() || !name().trim()">Utwórz</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class CreateUserDialog {
  private readonly session = inject(SessionStore);
  private readonly dialogRef = inject<MatDialogRef<CreateUserDialog, User>>(MatDialogRef);

  protected readonly maxLength = Limits.userName;
  protected readonly name = signal('');
  protected readonly saving = signal(false);
  protected readonly fieldError = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);

  protected async save(): Promise<void> {
    this.saving.set(true);
    this.fieldError.set(null);
    this.formError.set(null);
    try {
      this.dialogRef.close(await this.session.createUser(this.name().trim()));
    } catch (e) {
      const error = toApiError(e);
      const field = error.fieldCode('name');
      if (field || error.code === 'user.name_taken') {
        this.fieldError.set(messageFor(field ?? error.code));
      } else {
        // e.g. limit.users_reached (503); 429/5xx are also shown by the interceptor
        this.formError.set(messageFor(error.code));
      }
    } finally {
      this.saving.set(false);
    }
  }
}
