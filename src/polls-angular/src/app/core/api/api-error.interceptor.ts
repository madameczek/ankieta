import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Notifier } from '../notifier';
import { SessionStore } from '../session/session.store';
import { toApiError } from './api-error';

/**
 * Turns failed responses into ApiError. Errors the screens can't do anything about are shown here
 * (401 → back to the user list, 403, 429, 5xx, network); the rest (400, 404, 409, 503) are handled by the screen.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const notifier = inject(Notifier);

  return next(request).pipe(
    catchError((response: unknown) => {
      const error = toApiError(response);

      if (error.status === 401) {
        session.clear();
        void router.navigate(['/users']);
        notifier.error(error.code);
      } else if (error.status === 403 || error.status === 429 || error.status === 0 || isServerError(error.status)) {
        notifier.error(error.code);
      }

      return throwError(() => error);
    }),
  );
};

/** 5xx except 503: a 503 is the lockout threshold (D43), shown by the create dialogs. */
function isServerError(status: number): boolean {
  return status >= 500 && status !== 503;
}
