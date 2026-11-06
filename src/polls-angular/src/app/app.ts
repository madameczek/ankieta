import { CdkScrollable } from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, ElementRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { SessionStore } from './core/session/session.store';

const pathOf = (url: string) => url.split(/[?#]/)[0];
const pollOf = (url: string) => /^\/polls\/(\d+)(\/|$)/.exec(pathOf(url))?.[1];

/**
 * Whether a navigation shows a new screen: then the content area starts at the top and gets focus.
 * Switching tabs inside one poll (or a query change only) keeps the position.
 */
export function isNewScreen(previousUrl: string | null, url: string): boolean {
  if (previousUrl === null) {
    return true;
  }
  if (pathOf(previousUrl) === pathOf(url)) {
    return false;
  }
  const poll = pollOf(url);
  return poll === undefined || poll !== pollOf(previousUrl);
}

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
          <!-- accessible name starts with the visible label (WCAG 2.5.3) -->
          <button mat-button class="user" [matMenuTriggerFor]="userMenu" [attr.aria-label]="user.name + ' – menu użytkownika'">
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
    <!-- only this area scrolls: the scrollbar starts below the header and shows only when needed;
         tabindex -1: focused on each new screen, so keyboard scrolling works and screen readers notice the change -->
    <main #content class="content" cdkScrollable tabindex="-1">
      <div class="page">
        <router-outlet />
      </div>
    </main>
  `,
  styles: `
    /* Full-height column: fixed header + scrolling content (the document itself never scrolls).
       On phones the address bar therefore stays visible – accepted for a fixed header. */
    :host {
      display: flex;
      flex-direction: column;
      height: 100vh; /* browsers without dvh */
      height: 100dvh;
    }
    /* Both reserve the scrollbar's width, so header and page columns stay aligned with or without a scrollbar.
       overflow: hidden makes the toolbar a scroll container – scrollbar-gutter only applies to those. */
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
    .content:focus {
      outline: none;
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
    }
    /* Long names end with "…" (the limit sits on the text itself, the button label does not shrink) */
    .user-name {
      display: block;
      max-width: min(40vw, 320px);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .guest {
      font: var(--mat-sys-body-medium);
    }
    /* Narrow screens: icon only (the name stays in the button's accessible name and the menu);
       a visitor sees only "Wybierz użytkownika" */
    @media (max-width: 599px) {
      .user-name,
      .guest {
        display: none;
      }
      .user {
        min-width: 48px;
      }
    }
    /* Print the whole page, not only the visible part of the scroll area */
    @media print {
      :host {
        display: block;
        height: auto;
      }
      .content {
        overflow: visible;
      }
      mat-toolbar {
        box-shadow: none;
      }
    }
  `,
})
export class App {
  protected readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly content = viewChild<ElementRef<HTMLElement>>('content');

  constructor() {
    // Scrolling moved from the window to <main>: the router's own scroll handling (window only) can't do this
    let previousUrl: string | null = null;
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => {
        const main = this.content()?.nativeElement;
        if (main && isNewScreen(previousUrl, e.urlAfterRedirects)) {
          main.scrollTo({ top: 0 });
          main.focus({ preventScroll: true });
        }
        previousUrl = e.urlAfterRedirects;
      });
  }

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
