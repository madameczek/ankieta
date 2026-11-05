import { httpResource } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { PollDetail } from '../../core/api/api.models';
import { toApiError } from '../../core/api/api-error';

/** Provided by the poll layout: the poll id from the route and the loaded poll, shared by all tabs. */
@Injectable()
export class PollContext {
  readonly pollId = toSignal(inject(ActivatedRoute).paramMap.pipe(map((params) => Number(params.get('pollId')))), {
    requireSync: true,
  });

  readonly detail = httpResource<PollDetail>(() => `/api/polls/${this.pollId()}`);

  readonly isOwner = computed(() => this.detail.hasValue() && this.detail.value().role === 'owner');

  /** Error code of the last load, e.g. poll.not_found (D41). */
  readonly errorCode = computed(() => (this.detail.error() ? toApiError(this.detail.error()).code : null));

  url(path = ''): string {
    return `/api/polls/${this.pollId()}${path}`;
  }
}
