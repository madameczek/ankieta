import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { TextFieldModule } from '@angular/cdk/text-field';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal, untracked } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { Limits, PollQuestion } from '../../../core/api/api.models';
import { isScreenError, toApiError } from '../../../core/api/api-error';
import { Notifier } from '../../../core/notifier';
import { confirm } from '../../../shared/confirm-dialog';
import { SaveBar, SaveState } from '../../../shared/save-bar';
import { HasUnsavedChanges } from '../../../shared/unsaved-changes.guard';
import { firstMessage } from '../create-poll-dialog';
import { OwnerOnly } from '../owner-only';
import { PollContext } from '../poll-context';

/** Owner only (D26): poll fields, questions (add, edit, drag & drop order, delete) and deleting the poll. */
@Component({
  selector: 'app-editor',
  imports: [
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    TextFieldModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    OwnerOnly,
    SaveBar,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './editor.html',
  styleUrl: './editor.scss',
})
export class Editor implements HasUnsavedChanges {
  protected readonly poll = inject(PollContext);
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  protected readonly limits = Limits;

  // Poll fields: reset only when another poll loads, not on every reload after a question change (unsaved input stays)
  protected readonly title = linkedSignal({
    source: () => this.poll.current()?.id,
    computation: () => untracked(() => this.poll.current()?.title ?? ''),
  });
  protected readonly description = linkedSignal({
    source: () => this.poll.current()?.id,
    computation: () => untracked(() => this.poll.current()?.description ?? ''),
  });
  /** Last saved poll fields: follow every reload, set at once after a save. */
  private readonly savedTitle = linkedSignal(() => this.poll.current()?.title ?? '');
  private readonly savedDescription = linkedSignal(() => this.poll.current()?.description ?? '');
  protected readonly pollDirty = computed(
    () => this.title() !== this.savedTitle() || this.description() !== this.savedDescription(),
  );
  protected readonly pollState = signal<SaveState>('idle');
  protected readonly pollError = signal<string | null>(null);

  /** Local copy, so a drop shows at once (optimistic, D9). */
  protected readonly questions = linkedSignal<PollQuestion[]>(() => [...(this.poll.current()?.questions ?? [])]);
  protected readonly editingId = signal<number | null>(null);
  protected readonly editText = signal('');
  protected readonly newQuestion = signal('');
  protected readonly questionError = signal<string | null>(null);
  protected readonly busy = signal(false);

  /** D46: unsaved poll fields, a typed new question or an open question edit with changes. */
  hasUnsavedChanges(): boolean {
    return !this.deleted && (this.pollDirty() || !!this.newQuestion().trim() || this.editDirty());
  }

  /** Set once the poll is deleted: nothing left to save, leave without asking. */
  private deleted = false;

  protected readonly editDirty = computed(() => {
    const id = this.editingId();
    return id !== null && this.editText() !== this.questions().find((q) => q.id === id)?.question;
  });

  protected async savePoll(): Promise<void> {
    if (!this.pollDirty() || !this.title().trim() || this.pollState() === 'saving') {
      return;
    }
    this.pollError.set(null);
    this.pollState.set('saving');
    const title = this.title();
    const description = this.description();
    const body = { title, description: description || null };
    const ok = await this.run(
      () => this.http.put(this.poll.url(), body),
      (m) => {
        this.pollError.set(m);
        this.pollState.set('error');
      },
    );
    if (ok) {
      this.savedTitle.set(title);
      this.savedDescription.set(description);
    }
    if (this.pollState() === 'saving') {
      // saved, or an error the interceptor already showed: the status falls back to saved / unsaved
      this.pollState.set('idle');
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
    const previous = this.questions();
    const reordered = [...previous];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.questions.set(reordered);

    const body = { questionIds: reordered.map((q) => q.id) };
    const ok = await this.run(() => this.http.put(this.poll.url('/questions/order'), body), (m) => this.notifier.info(m));
    if (!ok) {
      // put the order back at once, then fetch the server's current list (it may have changed: order_mismatch)
      this.questions.set(previous);
      this.poll.detail.reload();
    }
  }

  protected deletePoll(): void {
    const title = this.poll.current()?.title ?? '';
    confirm(this.dialog, {
      title: 'Usunąć ankietę?',
      message: `Ankieta „${title}” zniknie dla wszystkich uczestników.`,
      confirm: 'Usuń ankietę',
      danger: true,
    }).subscribe(async (ok) => {
      if (ok && (await this.run(() => this.http.delete(this.poll.url()), (m) => this.notifier.info(m), false))) {
        this.notifier.info('Usunięto ankietę.');
        this.deleted = true;
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
      if (isScreenError(error)) {
        onError(firstMessage(error));
      }
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
