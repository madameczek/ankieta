import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';
import { Created, Limits } from '../../core/api/api.models';
import { ApiError, toApiError } from '../../core/api/api-error';
import { messageFor } from '../../core/api/error-messages';

/** Creates a poll owned by the current user. Closes with the new poll id. */
@Component({
  selector: 'app-create-poll-dialog',
  imports: [MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Nowa ankieta</h2>
    <form (submit)="$event.preventDefault(); save()">
      <mat-dialog-content>
        <mat-form-field class="full-width">
          <mat-label>Tytuł</mat-label>
          <input
            matInput
            cdkFocusInitial
            required
            [attr.maxlength]="limits.title"
            [value]="title()"
            (input)="title.set($any($event.target).value)"
            autocomplete="off"
          />
        </mat-form-field>
        <mat-form-field class="full-width">
          <mat-label>Opis (opcjonalnie)</mat-label>
          <textarea
            matInput
            rows="3"
            [attr.maxlength]="limits.description"
            [value]="description()"
            (input)="description.set($any($event.target).value)"
          ></textarea>
        </mat-form-field>
        @if (error(); as message) {
          <p class="error-text" role="alert">{{ message }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Anuluj</button>
        <button mat-flat-button type="submit" [disabled]="saving() || !title().trim()">Utwórz</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class CreatePollDialog {
  private readonly http = inject(HttpClient);
  private readonly dialogRef = inject<MatDialogRef<CreatePollDialog, number>>(MatDialogRef);

  protected readonly limits = Limits;
  protected readonly title = signal('');
  protected readonly description = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async save(): Promise<void> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const created = await firstValueFrom(
        this.http.post<Created>('/api/polls', { title: this.title(), description: this.description() || null }),
      );
      this.dialogRef.close(created.id);
    } catch (e) {
      this.error.set(firstMessage(toApiError(e)));
    } finally {
      this.saving.set(false);
    }
  }
}

/** The first field error if there is one, else the error itself (e.g. limit.polls_reached). */
export function firstMessage(error: ApiError): string {
  const firstField = Object.values(error.fieldErrors)[0]?.[0];
  return messageFor(firstField ?? error.code);
}
