import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { messageFor } from '../../core/api/error-messages';
import { PollContext } from './poll-context';

/** D45: header + router tabs; the owner sees all four tabs, a participant only "Moje odpowiedzi". */
@Component({
  selector: 'app-poll-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatTabsModule, MatButtonModule, MatProgressBarModule],
  providers: [PollContext],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (poll.detail.isLoading() && !poll.detail.hasValue()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (poll.errorCode(); as code) {
      <div class="empty-state" role="alert">
        <p>{{ message(code) }}</p>
        <a mat-stroked-button routerLink="/polls">Wróć do listy ankiet</a>
      </div>
    } @else if (poll.detail.hasValue()) {
      <div class="page-header">
        <h1>{{ poll.detail.value().title }}</h1>
        <span class="role-badge" [class.owner]="poll.isOwner()">{{ poll.isOwner() ? 'Właściciel' : 'Uczestnik' }}</span>
      </div>
      @if (poll.detail.value().description; as description) {
        <p class="description">{{ description }}</p>
      }

      @if (poll.isOwner()) {
        <nav mat-tab-nav-bar [tabPanel]="panel" aria-label="Sekcje ankiety">
          @for (tab of tabs; track tab.path) {
            <a mat-tab-link [routerLink]="tab.path" routerLinkActive #active="routerLinkActive" [active]="active.isActive">
              {{ tab.label }}
            </a>
          }
        </nav>
      }
      <mat-tab-nav-panel #panel>
        <div class="tab-content">
          <router-outlet />
        </div>
      </mat-tab-nav-panel>
    }
  `,
  styles: `
    .description {
      white-space: pre-line;
      margin-top: -8px;
    }
    .tab-content {
      padding-top: 16px;
    }
  `,
})
export class PollLayout {
  protected readonly poll = inject(PollContext);

  protected readonly tabs = [
    { path: 'answers', label: 'Moje odpowiedzi' },
    { path: 'edit', label: 'Edycja' },
    { path: 'participants', label: 'Uczestnicy' },
    { path: 'results', label: 'Wyniki' },
  ];

  protected message(code: string): string {
    return messageFor(code);
  }
}
