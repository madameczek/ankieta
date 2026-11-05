import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { PollListItem } from '../../core/api/api.models';
import { toApiError } from '../../core/api/api-error';
import { messageFor } from '../../core/api/error-messages';
import { CreatePollDialog } from './create-poll-dialog';

/** D35/D50: owned and participating polls in one list, newest change first. */
@Component({
  selector: 'app-poll-list',
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatListModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-header">
      <h1>Moje ankiety</h1>
      <button mat-flat-button (click)="createPoll()">
        <mat-icon>add</mat-icon>
        Nowa ankieta
      </button>
    </div>

    @if (polls.isLoading()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (polls.error()) {
      <p class="error-text" role="alert">{{ errorMessage() }}</p>
    } @else if (polls.hasValue() && polls.value().length === 0) {
      <div class="empty-state">
        <p>Nie uczestniczysz jeszcze w żadnej ankiecie.</p>
        <button mat-stroked-button (click)="createPoll()">Utwórz ankietę</button>
      </div>
    } @else if (polls.hasValue()) {
      <mat-nav-list>
        @for (poll of polls.value(); track poll.id) {
          <a mat-list-item [routerLink]="['/polls', poll.id]">
            <span matListItemTitle>{{ poll.title }}</span>
            <span matListItemLine class="muted">
              <span class="role-badge" [class.owner]="poll.role === 'owner'">
                {{ poll.role === 'owner' ? 'Właściciel' : 'Uczestnik' }}
              </span>
              · zmieniona {{ poll.modifiedAt | date: 'dd.MM.yyyy HH:mm' }}
            </span>
          </a>
        }
      </mat-nav-list>
    }
  `,
})
export class PollList {
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly polls = httpResource<PollListItem[]>(() => '/api/polls');

  protected errorMessage(): string {
    return messageFor(toApiError(this.polls.error()).code);
  }

  protected createPoll(): void {
    this.dialog
      .open<CreatePollDialog, void, number>(CreatePollDialog, { width: '480px' })
      .afterClosed()
      .subscribe((pollId) => {
        if (pollId) {
          void this.router.navigate(['/polls', pollId, 'edit']);
        }
      });
  }
}
