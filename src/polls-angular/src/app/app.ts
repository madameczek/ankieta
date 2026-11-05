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
    await this.session.leave();
    await this.router.navigate(['/users']);
  }
}
