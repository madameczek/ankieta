import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { messageFor } from './api/error-messages';

/** Short messages at the bottom of the screen. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  info(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
  }

  error(code: string): void {
    this.snackBar.open(messageFor(code), 'OK', { duration: 5000 });
  }
}
