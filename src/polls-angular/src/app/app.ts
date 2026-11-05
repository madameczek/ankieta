import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { SessionStore } from './core/session/session.store';

/** Shell: toolbar with the current user (or "Gość") and the routed screen. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-toolbar>
      <a class="brand" routerLink="/">Ankiety</a>
      <span class="spacer"></span>
      @if (session.user(); as user) {
        <button mat-button [matMenuTriggerFor]="userMenu" aria-label="Menu użytkownika">
          <mat-icon>account_circle</mat-icon>
          {{ user.name }}
        </button>
        <mat-menu #userMenu="matMenu">
          <a mat-menu-item routerLink="/users">
            <mat-icon>switch_account</mat-icon>
            Zmień użytkownika
          </a>
          <button mat-menu-item (click)="leave()">
            <mat-icon>logout</mat-icon>
            Wyjdź
          </button>
        </mat-menu>
      } @else {
        <span class="guest">Gość</span>
        <a mat-button routerLink="/users">Wybierz użytkownika</a>
      }
    </mat-toolbar>
    <main class="page">
      <router-outlet />
    </main>
  `,
  styles: `
    .brand {
      color: inherit;
      text-decoration: none;
      font: var(--mat-sys-title-large);
    }
    .spacer {
      flex: 1;
    }
    .guest {
      margin-right: 8px;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
    }
  `,
})
export class App {
  protected readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  protected async leave(): Promise<void> {
    // Navigate first: the screen's unsaved-changes guard can still ask while the user is signed in (D46)
    const onUsers = this.router.url.split(/[?#]/)[0] === '/users';
    try {
      if (!onUsers && !(await this.router.navigate(['/users']))) {
        return; // cancelled in the unsaved-changes dialog, or superseded: stay signed in
      }
    } catch {
      return; // navigation failed (e.g. a chunk did not load): stay signed in
    }
    try {
      await this.session.leave();
    } catch {
      // the interceptor showed the error; leave locally anyway (the cookie expires or is replaced later)
      this.session.clear();
    }
  }
}
