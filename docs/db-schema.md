# Database schema

Diagram: [db-schema.svg](db-schema.svg). Source of truth: `src/polls/db/migrations.sql` (decisions D3–D9, D31).

```text
┌───────────────────────────────────────┐         ┌─────────────────────────────────┐         ┌──────────────────────────────────────┐
│ users                                 │         │ user_poll                       │         │ polls                                │
├───────────────────────────────────────┤         ├─────────────────────────────────┤         ├──────────────────────────────────────┤
│ PK  id          int identity          │1──┐     │ PK,FK  id_poll   int            │*───────1│ PK  id           int identity        │1─┐
│ UQ  name        nvarchar(100)         │   ├────*│ PK,FK  id_user   int            │         │     title        nvarchar(200)       │  │
│     created_at  datetime2             │   │     │        is_owner  bit            │         │     description  nvarchar(2000) NULL │  │
└───────────────────────────────────────┘   │     └─────────────────────────────────┘         │     created_at   datetime2           │  │
                                            │                                                 │     modified_at  datetime2           │  │
                                            │                                                 │     deleted_at   datetime2 NULL      │  │
                                            │                                                 └──────────────────────────────────────┘  │
                                            │                                                                                           │
                                            │                                                                                           │
                                            │                                                                                           │
┌───────────────────────────────────────┐   │     ┌─────────────────────────────────┐         ┌──────────────────────────────────────┐  │
│ threshold_warnings                    │   │     │ answers                         │         │ questions                            │  │
├───────────────────────────────────────┤   │     ├─────────────────────────────────┤         ├──────────────────────────────────────┤  │
│ PK  resource  varchar(20)             │   │     │ PK  id           int identity   │   ┌────1│ PK  id           int identity        │  │
│     sent_at   datetime2               │   │     │ FK  id_question  int            │*──┘     │ FK  id_poll      int                 │*─┘
└───────────────────────────────────────┘   └────*│ FK  id_user      int            │         │     sort_order   int                 │
┌───────────────────────────────────────┐         │     answer       nvarchar(4000) │         │     question     nvarchar(1000)      │
│ data_protection_keys                  │         │     modified_at  datetime2      │         │     modified_at  datetime2           │
├───────────────────────────────────────┤         └─────────────────────────────────┘         │     deleted_at   datetime2 NULL      │
│ PK  id             int identity       │                                                     └──────────────────────────────────────┘
│     friendly_name  nvarchar(max) NULL │
│     xml            nvarchar(max) NULL │
└───────────────────────────────────────┘
```

`1 ─ *` one-to-many · PK primary key · FK foreign key · UQ unique · `NULL` = optional, all other columns NOT NULL.
`threshold_warnings` and `data_protection_keys` have no relations (thresholds D14, Data Protection keys D22).

Constraints:
- user_poll: one owner per poll – unique (id_poll) WHERE is_owner = 1
- questions: unique (id_poll, sort_order) WHERE deleted_at IS NULL
- answers: unique (id_question, id_user) – one answer per user and question
- Soft delete: polls.deleted_at, questions.deleted_at (hidden by query filters)
- CHECK not empty: users.name, polls.title, questions.question, answers.answer
- threshold_warnings.resource IN ('users', 'polls'); dates are UTC (SYSUTCDATETIME())
