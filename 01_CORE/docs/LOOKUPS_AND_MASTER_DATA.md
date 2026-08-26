# Lookup and Master Data Foundation

## Model and scope

`LookupType` defines a language-neutral category and is deterministically either `Global` (`company_id` is null) or `Company` (`company_id` is required). `LookupValue` inherits its type's ownership. Type codes are globally unique, normalized lowercase technical identifiers; value codes are unique within a type. Codes are immutable. There is no mixed global/company value set and therefore no shadowing or precedence rule.

Global/system rows are deployment-owned and read-only through the normal API. Company lookup types and values are managed through the API. System rows reject metadata, activation, and value changes. Hierarchy and arbitrary JSON metadata are deliberately deferred: neither has a proven cross-module requirement.

Values use stable UUIDs, active/inactive lifecycle, restrictive foreign keys, and no public delete operation. Deactivation prevents default selection while preserving historical references. `version` is an EF concurrency token and stale writes return `lookup.concurrency_conflict`.

## Localization

`lookup_value_translations` stores one normalized `fa`, `en`, or `ar` row per value. Technical codes never depend on display text. The compact option resolver applies the same fallback for every client:

1. requested normalized culture;
2. default `fa` translation;
3. technical value code.

Type display names are administrative technical descriptions, not localized UI identifiers. A type translation table can be added if a future UI demonstrates that requirement.

## Authorization and isolation

- `lookup.read` protects all read and option endpoints.
- `lookup.manage` protects company-owned mutations.
- anonymous access is denied.
- global/system writes are deployment operations, not API operations.

EF filters and PostgreSQL RLS expose global rows plus rows for the transaction-local `barang.company_id`. Writes require the current company. Normal API/database roles do not bypass RLS, and transaction-local settings prevent pooled connection leakage.

## API

Routes are under `/api/v1/lookups`: type list/by-id/by-code, values by type/by-id/by-code, company type/value creation and updates, activation, translation upsert, and `GET /{typeCode}/options?culture=fa&activeOnly=true`. Options sort by `displayOrder`, then code. There are no DELETE routes.

## Audit and transactions

Mutations emit centralized `lookup.type.*`, `lookup.value.*`, and `lookup.translation.updated` events through Phase 06 `IAuditWriter`. Entity mutation and required audit are saved by the same transaction executor, so rollback cannot leave a false successful audit. Codes and typed fields contain no credentials; arbitrary metadata is not accepted.

## Caching and seeding

`ILookupOptionReader` is the cache-friendly boundary. A future decorator may cache by company, type, culture, and active-only flag; no cache or Redis is introduced now. Invalidations must follow committed mutations.

System catalogs are seeded idempotently by code through reviewed migrations/deployment tooling. This phase seeds no business catalog. Permission rows are idempotent and existing `system.administrator` roles receive the two new permissions during migration.

Future modules reference lookup UUIDs/codes but must not mutate lookup tables directly. UTC audit timestamps remain canonical; no alternate calendar values are persisted.
