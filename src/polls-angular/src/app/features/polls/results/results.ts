import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { PollResults } from '../../../core/api/api.models';
import { toApiError } from '../../../core/api/api-error';
import { messageFor } from '../../../core/api/error-messages';
import { PollContext } from '../poll-context';

/** Owner only (D33): answers per question, current participants only. */
@Component({
  selector: 'app-results',
  imports: [DatePipe, MatCardModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (results.isLoading() && !results.hasValue()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (results.error()) {
      <p class="error-text" role="alert">{{ errorMessage() }}</p>
    } @else if (results.hasValue()) {
      @for (question of results.value().questions; track question.id; let i = $index) {
        <mat-card appearance="outlined" class="question">
          <mat-card-header>
            <mat-card-title>{{ i + 1 }}. {{ question.question }}</mat-card-title>
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
              <p class="muted">Brak odpowiedzi.</p>
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
  private readonly poll = inject(PollContext);

  protected readonly results = httpResource<PollResults>(() => this.poll.url('/results'));

  protected errorMessage(): string {
    return messageFor(toApiError(this.results.error()).code);
  }
}
