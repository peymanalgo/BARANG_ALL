# BARANG Database Conventions

This document specializes the PostgreSQL and EF Core rules in `NAMING_CONVENTIONS.md` without replacing them.

## Ownership and schemas

- PostgreSQL is the system of record. Application-owned baseline objects use the `barang` schema; application tables are not placed in `public`.
- Each approved future business module owns its own lowercase schema. Phase 03 creates no business schema or table.
- EF migration history is `barang.__ef_migrations_history`. The initial migration creates only the `barang` schema; no custom metadata table is justified.

## Names and mappings

- PostgreSQL identifiers are unquoted lowercase `snake_case`; EF configurations explicitly preserve these names.
- Tables are plural; primary keys are `id`; foreign keys are `<entity>_id`; company-scoped tables include non-null `company_id` and company-aware unique/index definitions.
- Constraint names use `pk_`, `fk_`, `uq_`, and `ck_`; index names use `ix_` or `ux_` as defined in `NAMING_CONVENTIONS.md`.
- Defaults must express hard technical invariants, not mutable business policy. Name database defaults explicitly in migrations when supported and review provider-generated SQL.
- Canonical instants use `timestamp with time zone` and semantic `..._at` columns. Calendar dates use `date`. Never store Jalali/Gregorian formatted text or presentation time zones.
- Technical metadata uses explicit names such as `created_at`, `created_by_actor_id`, `updated_at`, `updated_by_actor_id`, and `correlation_id` only when the owning model requires them.
- Future lifecycle fields use `archived_at`/`archived_by_actor_id` or approved `deleted_at`/`deleted_by_actor_id`; physical deletion of operational/history data requires explicit approval.
- Optimistic concurrency uses C# `long Version` mapped to non-null PostgreSQL `bigint version`, configured with `IsConcurrencyToken()`, and incremented once per successful mutation.

## Migrations

- Migrations live only in `BARANG.Core.Infrastructure/Migrations` and use UTC-generated EF identifiers with concise PascalCase names.
- Migrations contain schema evolution and hard data invariants only—never UI localization, calendar conversion, workflow, or business orchestration.
- Exact CLI and deployment procedures are in `MIGRATION_STRATEGY.md`.
