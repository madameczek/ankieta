import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, linkedSignal, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Limits, Paged, User, UserListItem } from '../../core/api/api.models';
import { toApiError } from '../../core/api/api-error';
import { Notifier } from '../../core/notifier';
import { SessionStore } from '../../core/session/session.store';
import { LoadError } from '../../shared/load-error';
import { CreateUserDialog } from './create-user-dialog';

/** D32: user list with server-side search and paging; open to visitors. "Become user" switches at once (D51). */
@Component({
  selector: 'app-user-list',
  imports: [
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTableModule,
    LoadError,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-header">
      <h1>Użytkownicy</h1>
      <button mat-flat-button (click)="createUser()">
        <mat-icon>person_add</mat-icon>
        Nowy użytkownik
      </button>
    </div>

    <mat-form-field class="full-width">
      <mat-label>Szukaj</mat-label>
      <mat-icon matPrefix>search</mat-icon>
      <input
        matInput
        [value]="search()"
        (input)="search.set($any($event.target).value)"
        [attr.maxlength]="searchMaxLength"
        autocomplete="off"
      />
    </mat-form-field>

    @if (users.isLoading()) {
      <mat-progress-bar mode="indeterminate" />
    }

    @if (users.error()) {
      <app-load-error [error]="users.error()" (retry)="users.reload()" />
    } @else if (users.hasValue() && users.value().items.length === 0) {
      <p class="empty-state">
        {{ debouncedSearch() ? 'Brak użytkowników pasujących do wyszukiwania.' : 'Nie ma jeszcze użytkowników.' }}
      </p>
    } @else if (users.hasValue()) {
      <!-- narrow screens: the table scrolls inside this box instead of widening the page -->
      <div class="table-scroll">
        <table mat-table [dataSource]="users.value().items" class="full-width">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Nazwa</th>
            <td mat-cell *matCellDef="let user">{{ user.name }}</td>
          </ng-container>
          <ng-container matColumnDef="createdAt">
            <th mat-header-cell *matHeaderCellDef>Utworzony</th>
            <td mat-cell *matCellDef="let user">{{ user.createdAt | date: 'dd.MM.yyyy HH:mm' }}</td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let user" class="actions">
              @if (user.id === session.user()?.id) {
                <span class="muted">To Ty</span>
              } @else {
                <button mat-stroked-button (click)="become(user)">Wejdź w rolę</button>
              }
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let user; columns: columns" [class.current]="user.id === session.user()?.id"></tr>
        </table>
      </div>
      <mat-paginator
        [length]="users.value().total"
        [pageIndex]="page() - 1"
        [pageSize]="pageSize()"
        [pageSizeOptions]="[10, 20, 50]"
        (page)="onPage($event)"
      />
    }
  `,
  styles: `
    .actions {
      text-align: right;
    }
    .table-scroll {
      overflow-x: auto;
    }
    @media (max-width: 599px) {
      .mat-column-createdAt {
        display: none;
      }
    }
    tr.current {
      background: var(--mat-sys-secondary-container);
    }
  `,
})
export class UserList {
  protected readonly session = inject(SessionStore);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  protected readonly columns = ['name', 'createdAt', 'actions'];
  protected readonly searchMaxLength = Limits.search;
  protected readonly search = signal('');
  protected readonly pageSize = signal(20);
  protected readonly debouncedSearch = toSignal(
    toObservable(this.search).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );
  /** Back to page 1 when the (debounced) search changes, in the same request. */
  protected readonly page = linkedSignal({ source: this.debouncedSearch, computation: () => 1 });

  protected readonly users = httpResource<Paged<UserListItem>>(() => ({
    url: '/api/users',
    params: { search: this.debouncedSearch().trim(), page: this.page(), pageSize: this.pageSize() },
  }));

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
  }

  protected async become(user: User): Promise<void> {
    try {
      await this.session.become(user.id);
    } catch (e) {
      const error = toApiError(e);
      if (error.status === 404) {
        this.notifier.error(error.code);
        this.users.reload();
      }
      return;
    }
    this.notifier.info(`Jesteś teraz: ${user.name}`);
    await this.router.navigate(['/polls']);
  }

  protected createUser(): void {
    this.dialog
      .open<CreateUserDialog, void, User>(CreateUserDialog, { width: '420px' })
      .afterClosed()
      .subscribe((user) => {
        if (user) {
          this.notifier.info(`Jesteś teraz: ${user.name}`);
          void this.router.navigate(['/polls']);
        }
      });
  }
}
