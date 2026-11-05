import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { PollResults } from '../../../core/api/api.models';
import { LoadError } from '../../../shared/load-error';
import { Progress } from '../../../shared/progress';
import { OwnerOnly } from '../owner-only';
import { PollContext } from '../poll-context';

/** "Odpowiedziało X z Y uczestników" – "z 1 uczestnika" for a single participant (Polish genitive). */
export function answeredLabel(answered: number, participants: number): string {
  return `Odpowiedziało ${answered} z ${participants} ${participants === 1 ? 'uczestnika' : 'uczestników'}`;
}

/** Owner only (D33): answers per question, current participants only. */
@Component({
  selector: 'app-results',
  imports: [DatePipe, MatCardModule, MatIconModule, MatProgressBarModule, LoadError, OwnerOnly, Progress],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!poll.isOwner()) {
      <app-owner-only />
    }
    @if (results.isLoading() && !results.hasValue()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (results.error()) {
      <app-load-error [error]="results.error()" (retry)="results.reload()" />
    } @else if (results.hasValue()) {
      @for (question of results.value().questions; track question.id; let i = $index) {
        <mat-card appearance="outlined" class="question">
          <mat-card-header>
            <mat-card-title>{{ i + 1 }}. {{ question.question }}</mat-card-title>
            <mat-card-subtitle>
              <app-progress [done]="question.answers.length" [total]="results.value().participantCount">
                {{ answeredLabel(question.answers.length, results.value().participantCount) }}
              </app-progress>
            </mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            @for (answer of question.answers; track answer.userId) {
              <div class="answer">
                <div class="meta">
                  <strong>{{ answer.name }}</strong>
                  <span class="muted">{{ answer.modifiedAt | date: 'dd.MM.yyyy HH:mm' }}</span>
                </div>
                <p>{{ answer.answer }}</p>
              </div>
            } @empty {
              <p class="unanswered"><mat-icon aria-hidden="true">help</mat-icon> Brak odpowiedzi</p>
            }
          </mat-card-content>
        </mat-card>
      } @empty {
        <p class="empty-state">Ankieta nie ma jeszcze pytań.</p>
      }
    }
  `,
  styles: `
    .question {
      margin-bottom: 16px;
    }
    mat-card-title {
      white-space: pre-line;
    }
    mat-card-subtitle {
      margin-top: 4px;
    }
    .answer {
      padding: 8px 0;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .answer:last-child {
      border-bottom: none;
    }
    .meta {
      display: flex;
      gap: 8px;
      align-items: baseline;
      flex-wrap: wrap;
    }
    .answer p {
      white-space: pre-line;
      margin: 4px 0 0;
    }
  `,
})
export class Results {
  protected readonly poll = inject(PollContext);

  // owner only: no request for a participant who opened the URL directly
  protected readonly answeredLabel = answeredLabel;

  protected readonly results = httpResource<PollResults>(() => (this.poll.isOwner() ? this.poll.url('/results') : undefined));
}
