import { httpResource } from '@angular/common/http';
import { Injectable, computed, inject, linkedSignal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { PollDetail, PollQuestion } from '../../core/api/api.models';
import { toApiError } from '../../core/api/api-error';

/** Provided by the poll layout: the poll id from the route and the loaded poll, shared by all tabs. */
@Injectable()
export class PollContext {
  readonly pollId = toSignal(inject(ActivatedRoute).paramMap.pipe(map((params) => Number(params.get('pollId')))), {
    requireSync: true,
  });

  readonly detail = httpResource<PollDetail>(() => `/api/polls/${this.pollId()}`);

  /**
   * The last successfully loaded poll. A failed reload keeps it, so the tabs (and their unsaved input) stay;
   * it is dropped when the route switches to another poll.
   */
  readonly current = linkedSignal<PollDetail | undefined, PollDetail | undefined>({
    source: () => (this.detail.hasValue() ? this.detail.value() : undefined),
    computation: (loaded, previous) =>
      loaded ?? (previous?.value?.id === this.pollId() ? previous.value : undefined),
  });

  readonly isOwner = computed(() => this.current()?.role === 'owner');

  /** Error code of the last load, e.g. poll.not_found (D41). */
  readonly errorCode = computed(() => (this.detail.error() ? toApiError(this.detail.error()).code : null));

  url(path = ''): string {
    return `/api/polls/${this.pollId()}${path}`;
  }

  /** Updates one question locally (after saving an answer), without reloading – other tabs' input stays. */
  patchQuestion(questionId: number, change: Partial<PollQuestion>): void {
    const poll = this.current();
    if (!poll) {
      return;
    }
    const updated = { ...poll, questions: poll.questions.map((q) => (q.id === questionId ? { ...q, ...change } : q)) };
    this.detail.set(updated);
  }
}
