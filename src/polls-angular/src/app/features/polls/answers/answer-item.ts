import { TextFieldModule } from '@angular/cdk/text-field';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';
import { Limits, PollQuestion, SavedAnswer } from '../../../core/api/api.models';
import { isScreenError, toApiError } from '../../../core/api/api-error';
import { messageFor } from '../../../core/api/error-messages';
import { SaveBar, SaveState } from '../../../shared/save-bar';
import { PollContext } from '../poll-context';

/** One question with the user's answer; saved on its own (D29). Empty text deletes the answer (D31). */
@Component({
  selector: 'app-answer-item',
  imports: [TextFieldModule, MatFormFieldModule, MatInputModule, SaveBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h3 [id]="headingId()">{{ index() }}. {{ question().question }}</h3>
    <mat-form-field class="full-width" subscriptSizing="dynamic">
      <mat-label>Twoja odpowiedź</mat-label>
      <textarea
        matInput
        cdkTextareaAutosize
        cdkAutosizeMinRows="1"
        [attr.aria-describedby]="headingId()"
        [attr.maxlength]="maxLength"
        [value]="text()"
        (input)="text.set($any($event.target).value)"
      ></textarea>
      <mat-hint align="end">{{ text().length }} / {{ maxLength }}</mat-hint>
    </mat-form-field>
    <app-save-bar
      [state]="state()"
      [error]="error()"
      [dirty]="dirty()"
      [savedAt]="savedAt()"
      emptyLabel="Brak odpowiedzi"
      [describedBy]="headingId()"
      (save)="save()"
    />
  `,
  styles: `
    :host {
      display: block;
      margin-bottom: 24px;
    }
    h3 {
      font: var(--mat-sys-title-medium);
      white-space: pre-line;
    }
  `,
})
export class AnswerItem {
  private readonly http = inject(HttpClient);
  private readonly poll = inject(PollContext);

  readonly question = input.required<PollQuestion>();
  readonly index = input.required<number>();

  protected readonly maxLength = Limits.answer;
  protected readonly headingId = computed(() => `question-${this.question().id}`);

  /** Last saved text; follows the shared poll (patched after each save). */
  private readonly saved = linkedSignal(() => this.question().myAnswer ?? '');
  protected readonly savedAt = linkedSignal(() => this.question().myAnswerModifiedAt);
  protected readonly text = linkedSignal(() => this.saved());
  protected readonly state = signal<SaveState>('idle');
  protected readonly error = signal<string | null>(null);

  readonly dirty = computed(() => this.text() !== this.saved());

  protected async save(): Promise<void> {
    if (this.state() === 'saving') {
      return; // a fast second click before the button disabled
    }
    this.state.set('saving');
    const text = this.text();
    const questionId = this.question().id;
    try {
      const url = this.poll.url(`/questions/${questionId}/answer`);
      const response = await firstValueFrom(this.http.put<SavedAnswer | null>(url, { answer: text }));
      const cleared = !text.trim();
      this.saved.set(cleared ? '' : text);
      this.text.set(cleared ? '' : text);
      this.savedAt.set(response?.modifiedAt ?? null);
      this.state.set('idle');
      // keep the shared poll in step, so returning to this tab shows the saved answer
      this.poll.patchQuestion(questionId, {
        myAnswer: cleared ? null : text,
        myAnswerModifiedAt: response?.modifiedAt ?? null,
      });
    } catch (e) {
      const apiError = toApiError(e);
      if (isScreenError(apiError)) {
        this.error.set(messageFor(apiError.fieldCode('answer') ?? apiError.code));
        this.state.set('error');
      } else {
        // shown by the interceptor; the status falls back to "Niezapisane zmiany"
        this.state.set('idle');
      }
    }
  }
}
