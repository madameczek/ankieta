# UI – polls

Agreed 2026-11-05 (D45–D51).

Angular (latest), standalone, signals, zoneless, Angular Material + CDK (D18). UI text in Polish (D28).
API calls follow `docs/api.md`. Access: **V** visitor, **U** user, **P** participant (owner included), **O** owner.

## App shell

- Toolbar: app name (link to start page) · current user on the right.
  - Visitor: label "Gość" + button "Wybierz użytkownika" (→ `/users`).
  - User: name chip with a menu: "Zmień użytkownika" (→ `/users`), "Wyjdź" (`DELETE /session` → `/users`).
- On startup the app calls `GET /api/session` and keeps the result in a `currentUser` signal (`SessionStore`). Routes render only after the call.
- Start page `/`: user → `/polls`, visitor → `/users`.

## Routes

| Route | Access | Screen | Guard |
| --- | --- | --- | --- |
| `/users` | V | User list | – |
| `/polls` | U | My polls | `userGuard` → `/users` |
| `/polls/:id` | P | Poll layout (tabs, below) | `userGuard` |
| `/polls/:id/answers` | P | My answers (default tab) | |
| `/polls/:id/edit` | O | Poll editor | owner tab only shown to the owner; the API enforces it (403) |
| `/polls/:id/participants` | O | Participants | |
| `/polls/:id/results` | O | Results | |
| `**` | V | Not found | – |

## Screens

### `/users` – User list (D32)
- Search field (debounced 300 ms) + `mat-table` (name, created) + `mat-paginator` (server paging, 20 per page).
- Each row: button "Wejdź w rolę" (`POST /session/{id}` → `/polls`), with no confirmation; snackbar "Jesteś teraz: <nazwa>" (D51). The current user's row is highlighted and has no button.
- Button "Nowy użytkownik" opens a dialog with a name field (`POST /users`). On success the new user is current, then `/polls`.
  - `user.name_taken` → field error; `limit.users_reached` → message in the dialog.

### `/polls` – My polls (D35)
- `mat-nav-list` rows (D50): title, role badge ("Właściciel" / "Uczestnik"), progress "3 / 5 odpowiedzi", modified date. Click → `/polls/:id`.
- Empty state: "Nie uczestniczysz jeszcze w żadnej ankiecie" + "Utwórz ankietę".
- Button "Nowa ankieta" opens a dialog (title, optional description); `POST /polls` → `/polls/:id/edit`.
  - `limit.polls_reached` → message in the dialog.

### `/polls/:id` – Poll layout
- Header: title, description, role badge.
- Tabs (`mat-tab-nav-bar` + router outlet):
  - participant: no tabs, only "Moje odpowiedzi"
  - owner: "Moje odpowiedzi" · "Edycja" · "Uczestnicy" · "Wyniki"
- 404 from the API → in-page message "Ankieta nie istnieje lub nie masz do niej dostępu" + link to `/polls` (D41).

### `/polls/:id/answers` – My answers (D29, D31)
- Questions in `sort_order`. Each one: question text, `textarea` (autosize, max 4000 characters with a counter), button "Zapisz", status line:
  - "Zapisano 12:34" · "Niezapisane zmiany" · "Zapisywanie…" · error text.
- Saving empty text deletes the answer; the status becomes "Brak odpowiedzi".
- Unsaved changes: leaving the route asks "Masz niezapisane odpowiedzi. Opuścić stronę?" (`canDeactivate` guard), and closing or reloading the tab triggers the browser's `beforeunload` prompt (D46).
- No questions yet → "Właściciel nie dodał jeszcze pytań".

### `/polls/:id/edit` – Poll editor (D9, D26)
- Form: title, description, button "Zapisz" (`PUT /polls/{id}`).
- Question list (`cdkDropList`) with a drag handle, text, "Edytuj" (inline textarea with Save / Cancel), "Usuń" (confirm dialog).
  - Drop → optimistic reorder, then `PUT /questions/order`; on error the old order is restored and a snackbar is shown.
- At the bottom: textarea + "Dodaj pytanie" (`POST /questions`, appended at the end).
- Danger zone: "Usuń ankietę" (confirm dialog with the title) → `DELETE /polls/{id}` → `/polls`.

### `/polls/:id/participants` – Participants (D25)
- Table: name, "Właściciel" badge, answered count, "Usuń" (hidden for the owner; confirm dialog).
- "Dodaj uczestnika": `mat-autocomplete` over `GET /users?search=` (debounced), excluding current participants. Pick → `POST /participants`.

### `/polls/:id/results` – Results (D33)
- For each question in `sort_order`: question text, then a list of answers (name, answer text with line breaks kept, modified date).
- Question without answers → "Brak odpowiedzi". Header shows "Odpowiedziało X z Y uczestników".

## Cross-cutting

- **HTTP:** one `ApiErrorInterceptor` turns `ProblemDetails` into an `ApiError {status, code, errors}`. One map `error-messages.ts` gives `code` → Polish text (D28).
  - 401 `session.required` → clear `currentUser`, go to `/users`, snackbar "Wybierz użytkownika".
  - 403 → snackbar "Tylko właściciel może to zrobić".
  - 404 → handled by the screen (poll layout message).
  - 429 → snackbar "Zbyt wiele żądań, spróbuj za chwilę".
  - 503 `limit.*` → shown in the create dialog.
  - 400 → field errors on the form; unknown fields → snackbar.
  - network / 5xx → snackbar "Błąd serwera".
- **Data loading:** `httpResource` / `resource()` for reads; services with plain `HttpClient` for writes; signal stores only for the session.
- **States:** every list has loading (progress bar), empty and error states.
- **Dates:** shown in local time, `dd.MM.yyyy HH:mm`, Polish locale (`pl`).
- **Theme:** Material 3, follows the system light/dark setting (`color-scheme: light dark`), no toggle (D47).
- **Responsive:** single column below 600 px; tables become lists on small screens.
- **Accessibility:** labels on all fields; drag & drop also has keyboard "move up / down" buttons; focus moves to a dialog's first field.

## Structure (`src/polls-angular`)

```
src/app/
  core/       session store, api error interceptor, error-messages, guards, layout (shell)
  features/
    users/    user-list, create-user-dialog
    polls/    poll-list, create-poll-dialog, poll-layout,
              answers/, editor/, participants/, results/
  shared/     confirm-dialog, status components, date pipe config
```

Tests (D48): Vitest unit tests for services, the interceptor, guards and the error map; Playwright e2e for the main flows (create user → create poll → add questions and participant → answer → results), run against a throwaway SQL Server container (D49).

Dev: `ng serve` with `proxy.conf.json` mapping `/api` to the local API (D36).
