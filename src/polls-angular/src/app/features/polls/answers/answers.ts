import { ChangeDetectionStrategy, Component, inject, viewChildren } from '@angular/core';
import { HasUnsavedChanges } from '../../../shared/unsaved-changes.guard';
import { PollContext } from '../poll-context';
import { AnswerItem } from './answer-item';

/** "Moje odpowiedzi": every question with the user's own answer (D24). */
@Component({
  selector: 'app-answers',
  imports: [AnswerItem],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (poll.detail.hasValue()) {
      @for (question of poll.detail.value().questions; track question.id; let i = $index) {
        <app-answer-item [pollId]="poll.pollId()" [question]="question" [index]="i + 1" />
      } @empty {
        <p class="empty-state">
          {{ poll.isOwner() ? 'Dodaj pytania w zakładce „Edycja”.' : 'Właściciel nie dodał jeszcze pytań.' }}
        </p>
      }
    }
  `,
})
export class Answers implements HasUnsavedChanges {
  protected readonly poll = inject(PollContext);
  private readonly items = viewChildren(AnswerItem);

  hasUnsavedChanges(): boolean {
    return this.items().some((item) => item.dirty());
  }
}
