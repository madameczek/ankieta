import { inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CanDeactivateFn } from '@angular/router';
import { confirm } from './confirm-dialog';

export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/** D46: leaving a screen with unsaved answers asks first. */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) =>
  !component.hasUnsavedChanges() ||
  confirm(inject(MatDialog), {
    title: 'Niezapisane zmiany',
    message: 'Masz niezapisane odpowiedzi. Opuścić stronę?',
    confirm: 'Opuść',
  });
