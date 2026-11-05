import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { messageFor } from '../../core/api/error-messages';
import { LoadError } from '../../shared/load-error';
import { PollContext } from './poll-context';

/** D45: header + router tabs; the owner sees all four tabs, a participant only "Moje odpowiedzi". */
@Component({
  selector: 'app-poll-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatTabsModule, MatButtonModule, MatProgressBarModule, LoadError],
  providers: [PollContext],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (poll.detail.isLoading() && !poll.current()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (notFound()) {
      <!-- D41: not a participant, deleted, or never existed -->
      <div class="empty-state" role="alert">
        <p>{{ notFoundMessage }}</p>
        <a mat-stroked-button routerLink="/polls">Wróć do listy ankiet</a>
      </div>
    } @else if (poll.current(); as current) {
      <div class="page-header">
        <h1>{{ current.title }}</h1>
        <span class="role-badge" [class.owner]="poll.isOwner()">{{ poll.isOwner() ? 'Właściciel' : 'Uczestnik' }}</span>
      </div>
      @if (current.description; as description) {
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
    } @else if (poll.detail.error()) {
      <!-- first load failed for another reason (the interceptor showed the details) -->
      <app-load-error [error]="poll.detail.error()" (retry)="poll.detail.reload()" />
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

  protected readonly notFoundMessage = messageFor('poll.not_found');
  /** Only a 404 replaces the page; a failed reload keeps the loaded poll and the tabs' unsaved input. */
  protected readonly notFound = computed(() => this.poll.errorCode() === 'poll.not_found');

  protected readonly tabs = [
    { path: 'answers', label: 'Moje odpowiedzi' },
    { path: 'edit', label: 'Edycja' },
    { path: 'participants', label: 'Uczestnicy' },
    { path: 'results', label: 'Wyniki' },
  ];
}
