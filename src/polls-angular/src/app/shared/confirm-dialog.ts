import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';

export interface ConfirmData {
  title: string;
  message: string;
  confirm: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>{{ data.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button class="cancel" [mat-dialog-close]="false">Anuluj</button>
      <button mat-flat-button class="confirm" [class.danger]="data.danger" [mat-dialog-close]="true">
        {{ data.confirm }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmData>(MAT_DIALOG_DATA);
}

/** Opens the confirm dialog; emits true only when confirmed. */
export function confirm(dialog: MatDialog, data: ConfirmData): Observable<boolean> {
  return dialog
    .open<ConfirmDialog, ConfirmData, boolean>(ConfirmDialog, {
      data,
      width: '420px',
      // destructive actions focus "Anuluj", so Enter doesn't delete
      autoFocus: data.danger ? 'button.cancel' : 'button.confirm',
    })
    .afterClosed()
    .pipe(map((result) => result === true));
}
