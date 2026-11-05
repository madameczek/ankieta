import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

/** Poll routes need a current user; visitors go to the user list (D11). */
export const userGuard: CanActivateFn = () =>
  inject(SessionStore).isUser() ? true : inject(Router).createUrlTree(['/users']);
