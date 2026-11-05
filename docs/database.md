# Database – migrations and schema script

The schema is defined by EF Core code-first (Fluent API, D12, D52). Migrations live in
`src/polls/polls.Api/Infrastructure/Migrations/`. The idempotent SQL script `src/polls/db/migrations.sql`
is generated from them and committed. It is what gets run against the database (D13) and what the tests apply.

Diagram of the tables and relations: [db-schema.svg](db-schema.svg), text version in [db-schema.md](db-schema.md).
After a model change, update the table list in `docs/tools/db-schema.py` and run `python docs/tools/db-schema.py`.

All commands run in `src/polls`.

## One-time setup

```
dotnet tool restore
```

Restores `dotnet-ef`, pinned in `dotnet-tools.json`.

## After a model change

1. Add a migration:
   ```
   dotnet ef migrations add <Name> --project polls.Api --output-dir Infrastructure/Migrations --namespace Polls.Api.Infrastructure.Migrations
   ```
2. Regenerate the script (all migrations, each guarded by `__EFMigrationsHistory`, safe to re-run):
   ```
   dotnet ef migrations script --idempotent --project polls.Api --output db/migrations.sql
   ```
3. Run the tests. They fail if the model has changes without a migration, or if `db/migrations.sql` doesn't match the migrations.

Other commands:

| Task | Command |
| --- | --- |
| List migrations | `dotnet ef migrations list --project polls.Api` |
| Remove the last migration (not yet applied anywhere) | `dotnet ef migrations remove --project polls.Api` |

## Applying the script

With the DDL login (the app's own login has DML rights only):

```
sqlcmd -I -S <server> -d <database> -U <ddl-login> -i db/migrations.sql
```

`-I` is required: it turns on `QUOTED_IDENTIFIER`, which the filtered unique indexes need. Without it the script fails with
"CREATE INDEX failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'" and rolls back.
`sqlcmd` handles the `GO` batch separators. SSMS and Azure Data Studio work too (`QUOTED_IDENTIFIER` is on there by default).

## Later option

A CD job that runs a container with the EF tools (or an `efbundle`) and applies the migrations before the API rollout.
The committed script stays as the reviewable schema artifact either way.
