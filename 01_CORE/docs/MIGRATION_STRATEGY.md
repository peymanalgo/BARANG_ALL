# BARANG Migration Strategy

EF Core migrations are owned by `BARANG.Core.Infrastructure`. The API checks connectivity but never migrates automatically on normal startup.

## CLI commands

Run from `01_CORE` after setting `ConnectionStrings__CoreDatabase` in the environment and restoring the local tool:

```powershell
dotnet tool restore
dotnet ef migrations add <PascalCaseName> --project src/BARANG.Core.Infrastructure --startup-project src/BARANG.Core.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations list --project src/BARANG.Core.Infrastructure --startup-project src/BARANG.Core.Infrastructure
dotnet ef database update --project src/BARANG.Core.Infrastructure --startup-project src/BARANG.Core.Infrastructure
dotnet ef migrations script --idempotent --project src/BARANG.Core.Infrastructure --startup-project src/BARANG.Core.Infrastructure --output artifacts/migrations.sql
dotnet ef database update <PreviousMigration> --project src/BARANG.Core.Infrastructure --startup-project src/BARANG.Core.Infrastructure
```

The last command is development rollback only. Production rollback normally restores a compatible application version and uses a reviewed forward-fix; a down migration is used only when proven safe.

## Environment strategy

- Local development: start PostgreSQL explicitly, review the generated migration, then run `database update` manually.
- Automated tests: the guarded test fixture applies migrations to a database whose name ends in `_test`.
- CI: provision an isolated PostgreSQL database, apply migrations, run database tests, and discard the database after the job.
- Staging: run a single controlled migration step before starting the new API version; block deployment on failure.
- Production/VPS: back up before risky changes, stop or gate incompatible writers, run one reviewed migration job/command, verify it, then roll out API instances. API replicas must never race to migrate.

Destructive or long-running migrations require explicit review, a tested backup/restore plan, compatibility analysis, and a rollback/forward-fix decision. Migration failure blocks deployment.

## Transactions, tests, and concurrency

- `ITransactionExecutor` owns Application orchestration transactions. It commits successful Results and rolls back failed Results, cancellation, or exceptions; Domain never commits or rolls back.
- Database integration tests require `BARANG_TEST_DB_CONNECTION`. The fixture rejects databases not ending in `_test`, applies migrations once, and never logs the connection string.
- Reset truncates all application tables with `RESTART IDENTITY CASCADE`, preserves `barang.__ef_migrations_history`, and recreates only the test-owned `barang_test` schema. This supports future foreign keys without drop/recreate per test.
- PostgreSQL concurrency tests use a test-only `barang_test.concurrency_probes` table and EF `IsConcurrencyToken()` mapping. No test entity enters the production model or migration.
