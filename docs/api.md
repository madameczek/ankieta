# API surface – polls

Agreed 2026-11-05 (D40–D44).

All routes under `/api`. JSON. Current user comes from the protected cookie (D10).
Access column: **V** visitor allowed, **U** any user, **P** participant of the poll (owner included), **O** owner of the poll.

## Session and users

| Method | Route | Access | Body / query | Success | Notes |
| --- | --- | --- | --- | --- | --- |
| GET | `/session` | V | – | 200 `{ user: {id, name} \| null }` | Angular calls it on startup. An unknown or invalid cookie is cleared |
| POST | `/session/{userId}` | V | – | 200 `{id, name}` + Set-Cookie | "become user" |
| DELETE | `/session` | V | – | 204, cookie cleared | "leave" |
| GET | `/users` | V | `?search=&page=1&pageSize=20` | 200 paged `{items:[{id,name,createdAt}], page, pageSize, total}` | name *contains*, sorted by name (D32) |
| POST | `/users` | V | `{name}` | 201 `{id, name}` + Set-Cookie | threshold check (D14); creator enters context |

## Polls

| Method | Route | Access | Body / query | Success | Notes |
| --- | --- | --- | --- | --- | --- |
| GET | `/polls` | U | – | 200 `[{id, title, role, answered, questionCount, modifiedAt}]` | owned + participating (D35) |
| POST | `/polls` | U | `{title, description?}` | 201 `{id}` | creates the owner row in the same transaction (D4); threshold check (D14) |
| GET | `/polls/{id}` | P | – | 200 `{id, title, description, role, questions:[{id, sortOrder, question, myAnswer?, myAnswerModifiedAt?}]}` | the participant view and the owner's editor share this endpoint |
| PUT | `/polls/{id}` | O | `{title, description?}` | 204 | |
| DELETE | `/polls/{id}` | O | – | 204 | soft delete (D8) |

## Questions (owner)

| Method | Route | Access | Body | Success | Notes |
| --- | --- | --- | --- | --- | --- |
| POST | `/polls/{id}/questions` | O | `{question}` | 201 `{id, sortOrder}` | appended at max + 1 |
| PUT | `/polls/{id}/questions/{qid}` | O | `{question}` | 204 | answers kept (D8) |
| DELETE | `/polls/{id}/questions/{qid}` | O | – | 204 | soft delete; gaps in sort_order are allowed |
| PUT | `/polls/{id}/questions/order` | O | `{questionIds:[…]}` | 204 | must contain exactly the poll's non-deleted questions; renumbered 1..n (D9) |

## Participants (owner)

| Method | Route | Access | Body | Success | Notes |
| --- | --- | --- | --- | --- | --- |
| GET | `/polls/{id}/participants` | O | – | 200 `[{userId, name, isOwner, answered}]` | |
| POST | `/polls/{id}/participants` | O | `{userId}` | 204 | idempotent if already a participant |
| DELETE | `/polls/{id}/participants/{userId}` | O | – | 204 | the owner can't be removed (D25); removing a non-participant is a no-op |

## Answers and results

| Method | Route | Access | Body | Success | Notes |
| --- | --- | --- | --- | --- | --- |
| PUT | `/polls/{id}/questions/{qid}/answer` | P | `{answer}` | 200 `{modifiedAt}` or 204 when cleared | upsert; empty or whitespace-only text deletes the row (D29, D31) |
| GET | `/polls/{id}/results` | O | – | 200 `{participantCount, questions:[{id, sortOrder, question, answers:[{userId, name, answer, modifiedAt}]}]}` | only current participants (D25, D33); answers sorted by name |

## Infrastructure

| Route | Notes |
| --- | --- |
| `/health/live`, `/health/ready` | ready checks the DB (D38); not rate-limited |
| `/docs`, `/openapi/v1.json` | Scalar + OpenAPI (D39) |

## Errors

`ProblemDetails` with a stable `code` extension on **every** error (English, mapped to Polish in Angular, D28). Validation errors add `errors: {field: [code]}`.
Order of checks on poll routes: session (401) → poll access (404 / 403) → body validation (400), so an outsider never learns more from a 400.
Create endpoints (users, polls, questions, participants) share the stricter `create` rate limit (D15).

| Status | When | Example codes |
| --- | --- | --- |
| 400 | validation failed | `validation.failed` + field codes: `name.required`, `name.too_long`, `search.too_long`, `page.invalid`, `pageSize.invalid`, `title.required`, `title.too_long`, `description.too_long`, `question.required`, `question.too_long`, `answer.too_long`, `questionIds.required`, `userId.required`; malformed or missing body: `request.invalid` (no field errors) |
| 401 | visitor calls a U/P/O endpoint | `session.required` |
| 404 | poll, question or user not found, **or the caller isn't a participant** | `poll.not_found`, `question.not_found`, `user.not_found` |
| 403 | participant calls an owner endpoint | `poll.owner_required` |
| 409 | conflict | `user.name_taken`, `participant.is_owner`, `questions.order_mismatch` |
| 429 | rate limit | `rate_limited` (+ `Retry-After`) |
| 503 | lockout threshold reached | `limit.users_reached`, `limit.polls_reached` |
| other | produced by the framework | unknown route `route.not_found`, wrong method `method.not_allowed`, unhandled error `server.error` |
