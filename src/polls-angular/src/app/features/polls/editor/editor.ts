import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, linkedSignal, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { Limits, PollQuestion } from '../../../core/api/api.models';
import { toApiError } from '../../../core/api/api-error';
import { Notifier } from '../../../core/notifier';
import { confirm } from '../../../shared/confirm-dialog';
import { firstMessage } from '../create-poll-dialog';
import { PollContext } from '../poll-context';

/** Owner only (D26): poll fields, questions (add, edit, drag & drop order, delete) and deleting the poll. */
@Component({
  selector: 'app-editor',
  imports: [CdkDropList, CdkDrag, CdkDragHandle, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './editor.html',
  styleUrl: './editor.scss',
})
export class Editor {
  protected readonly poll = inject(PollContext);
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  protected readonly limits = Limits;

  // Poll fields, reset whenever the poll reloads
  protected readonly title = linkedSignal(() => (this.poll.detail.hasValue() ? this.poll.detail.value().title : ''));
  protected readonly description = linkedSignal(() =>
    this.poll.detail.hasValue() ? (this.poll.detail.value().description ?? '') : '',
  );
  protected readonly pollError = signal<string | null>(null);

  /** Local copy, so a drop shows at once (optimistic, D9). */
  protected readonly questions = linkedSignal<PollQuestion[]>(() =>
    this.poll.detail.hasValue() ? [...this.poll.detail.value().questions] : [],
  );
  protected readonly editingId = signal<number | null>(null);
  protected readonly editText = signal('');
  protected readonly newQuestion = signal('');
  protected readonly questionError = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected async savePoll(): Promise<void> {
    this.pollError.set(null);
    const body = { title: this.title(), description: this.description() || null };
    if (await this.run(() => this.http.put(this.poll.url(), body), (m) => this.pollError.set(m))) {
      this.notifier.info('Zapisano ankietę.');
    }
  }

  protected async addQuestion(): Promise<void> {
    this.questionError.set(null);
    const body = { question: this.newQuestion() };
    if (await this.run(() => this.http.post(this.poll.url('/questions'), body), (m) => this.questionError.set(m))) {
      this.newQuestion.set('');
    }
  }

  protected startEdit(question: PollQuestion): void {
    this.editingId.set(question.id);
    this.editText.set(question.question);
    this.questionError.set(null);
  }

  protected async saveEdit(question: PollQuestion): Promise<void> {
    const body = { question: this.editText() };
    if (await this.run(() => this.http.put(this.poll.url(`/questions/${question.id}`), body), (m) => this.questionError.set(m))) {
      this.editingId.set(null);
    }
  }

  protected deleteQuestion(question: PollQuestion): void {
    confirm(this.dialog, {
      title: 'Usunąć pytanie?',
      message: `„${question.question}” zniknie z ankiety razem z odpowiedziami.`,
      confirm: 'Usuń',
      danger: true,
    }).subscribe((ok) => {
      if (ok) {
        void this.run(() => this.http.delete(this.poll.url(`/questions/${question.id}`)), (m) => this.questionError.set(m));
      }
    });
  }

  protected async drop(event: CdkDragDrop<PollQuestion[]>): Promise<void> {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    const reordered = [...this.questions()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.questions.set(reordered);

    const body = { questionIds: reordered.map((q) => q.id) };
    const ok = await this.run(() => this.http.put(this.poll.url('/questions/order'), body), (m) => this.notifier.info(m));
    if (!ok) {
      // restore the server's order
      this.poll.detail.reload();
    }
  }

  protected deletePoll(): void {
    const title = this.poll.detail.hasValue() ? this.poll.detail.value().title : '';
    confirm(this.dialog, {
      title: 'Usunąć ankietę?',
      message: `Ankieta „${title}” zniknie dla wszystkich uczestników.`,
      confirm: 'Usuń ankietę',
      danger: true,
    }).subscribe(async (ok) => {
      if (ok && (await this.run(() => this.http.delete(this.poll.url()), (m) => this.notifier.info(m), false))) {
        this.notifier.info('Usunięto ankietę.');
        await this.router.navigate(['/polls']);
      }
    });
  }

  /** Runs a write; on success reloads the poll, on a 4xx shows the message. Returns success. */
  private async run(
    request: () => Observable<unknown>,
    onError: (message: string) => void,
    reload = true,
  ): Promise<boolean> {
    this.busy.set(true);
    try {
      await firstValueFrom(request());
      if (reload) {
        this.poll.detail.reload();
      }
      return true;
    } catch (e) {
      const error = toApiError(e);
      // 403/429/5xx are already shown by the interceptor
      if (error.status === 400 || error.status === 404 || error.status === 409) {
        onError(firstMessage(error));
      }
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
