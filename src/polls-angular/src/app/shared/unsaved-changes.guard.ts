import { inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CanDeactivateFn } from '@angular/router';
import { SessionStore } from '../core/session/session.store';
import { confirm } from './confirm-dialog';

export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * D46: leaving a screen with unsaved input (answers, poll editor) asks first – also when switching or leaving
 * the user, since the shell navigates to /users before it changes the session.
 */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) =>
  // after a 401 the session is gone and nothing can be saved any more: don't ask
  !inject(SessionStore).isUser() ||
  !component.hasUnsavedChanges() ||
  confirm(inject(MatDialog), {
    title: 'Niezapisane zmiany',
    message: 'Masz niezapisane zmiany. Opuścić stronę?',
    confirm: 'Opuść',
  });

/**
 * D46: closing or reloading the tab (or typing another URL) with unsaved input lets the browser ask.
 * The browser shows its own text; it can't be changed. Same rules as the guard: no prompt once the session is gone.
 * Bind it on the screen: `host: { '(window:beforeunload)': 'onBeforeUnload($event)' }`.
 */
export function warnBeforeUnload(event: BeforeUnloadEvent, component: HasUnsavedChanges, session: SessionStore): void {
  if (session.isUser() && component.hasUnsavedChanges()) {
    event.preventDefault();
    event.returnValue = true; // older browsers that ignore preventDefault() here
  }
}
