import { HttpClient, httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { debounceTime, distinctUntilChanged, firstValueFrom } from 'rxjs';
import { Paged, Participant, UserListItem } from '../../../core/api/api.models';
import { toApiError } from '../../../core/api/api-error';
import { messageFor } from '../../../core/api/error-messages';
import { Notifier } from '../../../core/notifier';
import { confirm } from '../../../shared/confirm-dialog';
import { PollContext } from '../poll-context';

/** Owner only (D25): who is assigned; add via user search, remove (not the owner). */
@Component({
  selector: 'app-participants',
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    MatProgressBarModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-form-field class="full-width">
      <mat-label>Dodaj uczestnika</mat-label>
      <mat-icon matPrefix>person_search</mat-icon>
      <input
        matInput
        [value]="search()"
        (input)="search.set($any($event.target).value)"
        [matAutocomplete]="auto"
        autocomplete="off"
      />
      <mat-autocomplete #auto="matAutocomplete" (optionSelected)="add($event)">
        @for (user of candidates(); track user.id) {
          <mat-option [value]="user">{{ user.name }}</mat-option>
        } @empty {
          @if (debouncedSearch().trim()) {
            <mat-option disabled>Brak pasujących użytkowników</mat-option>
          }
        }
      </mat-autocomplete>
    </mat-form-field>

    @if (participants.isLoading() && !participants.hasValue()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (participants.error()) {
      <p class="error-text" role="alert">{{ errorMessage() }}</p>
    } @else if (participants.hasValue()) {
      <mat-list>
        @for (participant of participants.value(); track participant.userId) {
          <mat-list-item>
            <span matListItemTitle>
              {{ participant.name }}
              @if (participant.isOwner) {
                <span class="role-badge owner">Właściciel</span>
              }
            </span>
            @if (!participant.isOwner) {
              <button mat-icon-button matListItemMeta (click)="remove(participant)" aria-label="Usuń uczestnika">
                <mat-icon>person_remove</mat-icon>
              </button>
            }
          </mat-list-item>
        }
      </mat-list>
    }
  `,
})
export class Participants {
  private readonly poll = inject(PollContext);
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  protected readonly participants = httpResource<Participant[]>(() => this.poll.url('/participants'));

  protected readonly search = signal('');
  protected readonly debouncedSearch = toSignal(toObservable(this.search).pipe(debounceTime(300), distinctUntilChanged()), {
    initialValue: '',
  });
  private readonly matches = httpResource<Paged<UserListItem>>(() => {
    const search = this.debouncedSearch().trim();
    return search ? { url: '/api/users', params: { search, pageSize: 10 } } : undefined;
  });

  /** Search results without people already assigned. */
  protected readonly candidates = computed(() => {
    const assigned = new Set((this.participants.hasValue() ? this.participants.value() : []).map((p) => p.userId));
    return (this.matches.hasValue() ? this.matches.value().items : []).filter((u) => !assigned.has(u.id));
  });

  protected errorMessage(): string {
    return messageFor(toApiError(this.participants.error()).code);
  }

  protected async add(event: MatAutocompleteSelectedEvent): Promise<void> {
    const user = event.option.value as UserListItem;
    this.search.set('');
    try {
      await firstValueFrom(this.http.post(this.poll.url('/participants'), { userId: user.id }));
      this.notifier.info(`Dodano: ${user.name}`);
      this.participants.reload();
    } catch (e) {
      this.showClientError(e);
    }
  }

  protected remove(participant: Participant): void {
    confirm(this.dialog, {
      title: 'Usunąć uczestnika?',
      message: `${participant.name} straci dostęp do ankiety. Odpowiedzi zostaną ukryte, nie usunięte.`,
      confirm: 'Usuń',
      danger: true,
    }).subscribe(async (ok) => {
      if (!ok) {
        return;
      }
      try {
        await firstValueFrom(this.http.delete(this.poll.url(`/participants/${participant.userId}`)));
        this.participants.reload();
      } catch (e) {
        this.showClientError(e);
      }
    });
  }

  /** 4xx the interceptor doesn't show (e.g. user.not_found, participant.is_owner). */
  private showClientError(e: unknown): void {
    const error = toApiError(e);
    if (error.status === 400 || error.status === 404 || error.status === 409) {
      this.notifier.error(error.code);
    }
  }
}
