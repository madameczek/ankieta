import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';
import { Limits, PollQuestion, SavedAnswer } from '../../../core/api/api.models';
import { toApiError } from '../../../core/api/api-error';
import { messageFor } from '../../../core/api/error-messages';

type SaveState = 'idle' | 'saving' | 'error';

/** One question with the user's answer; saved on its own (D29). Empty text deletes the answer (D31). */
@Component({
  selector: 'app-answer-item',
  imports: [DatePipe, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h3>{{ index() }}. {{ question().question }}</h3>
    <mat-form-field class="full-width" subscriptSizing="dynamic">
      <mat-label>Twoja odpowiedź</mat-label>
      <textarea
        matInput
        rows="3"
        [attr.maxlength]="maxLength"
        [value]="text()"
        (input)="text.set($any($event.target).value)"
      ></textarea>
      <mat-hint align="end">{{ text().length }} / {{ maxLength }}</mat-hint>
    </mat-form-field>
    <div class="status">
      <span [class.error-text]="state() === 'error'" [class.muted]="state() !== 'error'" aria-live="polite">
        @switch (true) {
          @case (state() === 'saving') { Zapisywanie… }
          @case (state() === 'error') { {{ error() }} }
          @case (dirty()) { Niezapisane zmiany }
          @case (!!savedAt()) { Zapisano {{ savedAt() | date: 'dd.MM.yyyy HH:mm' }} }
          @default { Brak odpowiedzi }
        }
      </span>
      <button mat-stroked-button (click)="save()" [disabled]="!dirty() || state() === 'saving'">Zapisz</button>
    </div>
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
    .status {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      margin-top: 8px;
    }
  `,
})
export class AnswerItem {
  private readonly http = inject(HttpClient);

  readonly pollId = input.required<number>();
  readonly question = input.required<PollQuestion>();
  readonly index = input.required<number>();

  protected readonly maxLength = Limits.answer;

  /** Last saved text; reset when the question is reloaded. */
  private readonly saved = linkedSignal(() => this.question().myAnswer ?? '');
  protected readonly savedAt = linkedSignal(() => this.question().myAnswerModifiedAt);
  protected readonly text = linkedSignal(() => this.saved());
  protected readonly state = signal<SaveState>('idle');
  protected readonly error = signal<string | null>(null);

  readonly dirty = computed(() => this.text() !== this.saved());

  protected async save(): Promise<void> {
    this.state.set('saving');
    const text = this.text();
    try {
      const url = `/api/polls/${this.pollId()}/questions/${this.question().id}/answer`;
      const response = await firstValueFrom(this.http.put<SavedAnswer | null>(url, { answer: text }));
      const cleared = !text.trim();
      this.saved.set(cleared ? '' : text);
      this.text.set(cleared ? '' : text);
      this.savedAt.set(response?.modifiedAt ?? null);
      this.state.set('idle');
    } catch (e) {
      const apiError = toApiError(e);
      this.error.set(messageFor(apiError.fieldCode('answer') ?? apiError.code));
      this.state.set('error');
    }
  }
}
