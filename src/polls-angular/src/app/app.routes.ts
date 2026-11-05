import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { SessionStore } from './core/session/session.store';
import { userGuard } from './core/session/user.guard';
import { unsavedChangesGuard } from './shared/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: () => (inject(SessionStore).isUser() ? '/polls' : '/users'),
  },
  {
    path: 'users',
    title: 'Użytkownicy',
    loadComponent: () => import('./features/users/user-list').then((m) => m.UserList),
  },
  {
    path: 'polls',
    title: 'Moje ankiety',
    canActivate: [userGuard],
    loadComponent: () => import('./features/polls/poll-list').then((m) => m.PollList),
  },
  {
    // D45: owner screens are tabs under the poll; a participant only sees the answers
    path: 'polls/:pollId',
    title: 'Ankieta',
    canActivate: [userGuard],
    loadComponent: () => import('./features/polls/poll-layout').then((m) => m.PollLayout),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'answers' },
      {
        path: 'answers',
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () => import('./features/polls/answers/answers').then((m) => m.Answers),
      },
      {
        path: 'edit',
        loadComponent: () => import('./features/polls/editor/editor').then((m) => m.Editor),
      },
      {
        path: 'participants',
        loadComponent: () => import('./features/polls/participants/participants').then((m) => m.Participants),
      },
      {
        path: 'results',
        loadComponent: () => import('./features/polls/results/results').then((m) => m.Results),
      },
    ],
  },
  {
    path: '**',
    title: 'Nie znaleziono',
    loadComponent: () => import('./features/not-found').then((m) => m.NotFound),
  },
];
