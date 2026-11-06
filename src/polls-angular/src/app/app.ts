import { CdkScrollable } from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { SessionStore } from './core/session/session.store';

/** Shell: fixed toolbar with the current user (or "Gość") and the routed screen in its own scroll area. */
@Component({
  selector: 'app-root',
  imports: [CdkScrollable, RouterOutlet, RouterLink, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-toolbar>
      <div class="bar">
        <a class="brand" routerLink="/">Ankiety</a>
        <span class="spacer"></span>
        @if (session.user(); as user) {
          <button mat-button class="user" [matMenuTriggerFor]="userMenu" [attr.aria-label]="'Menu użytkownika – ' + user.name">
            <mat-icon>account_circle</mat-icon>
            <span class="user-name">{{ user.name }}</span>
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
      </div>
    </mat-toolbar>
    <!-- only this area scrolls: the scrollbar starts below the header and shows only when needed -->
    <main class="content" cdkScrollable>
      <div class="page">
        <router-outlet />
      </div>
    </main>
  `,
  styles: `
    /* Full-height column: fixed header + scrolling content (the document itself never scrolls) */
    :host {
      display: flex;
      flex-direction: column;
      height: 100dvh;
    }
    /* both reserve the scrollbar's width, so header and page columns stay aligned with or without a scrollbar */
    mat-toolbar {
      flex: none;
      padding: 0;
      overflow: hidden;
      scrollbar-gutter: stable;
      position: relative;
      z-index: 1;
      box-shadow: var(--mat-sys-level1);
    }
    /* Header content in the same 960 px column as the page (.page in styles.scss) */
    .bar {
      display: flex;
      align-items: center;
      gap: 8px;
      width: 100%;
      max-width: 960px;
      min-width: 0;
      margin: 0 auto;
      padding: 0 16px;
      box-sizing: border-box;
    }
    .content {
      flex: 1;
      min-height: 0;
      overflow: auto;
      scrollbar-gutter: stable;
    }
    .brand {
      flex: none;
      color: inherit;
      text-decoration: none;
      font: var(--mat-sys-title-large);
    }
    .spacer {
      flex: 1;
    }
    .user {
      min-width: 0;
      max-width: 50%;
    }
    .user-name {
      display: block;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .guest {
      color: var(--mat-sys-on-primary-container);
      font: var(--mat-sys-body-medium);
    }
    /* Narrow screens: icon only (the name stays in the button's accessible name and the menu) */
    @media (max-width: 599px) {
      .user-name,
      .guest {
        display: none;
      }
      .user {
        min-width: 48px;
      }
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
