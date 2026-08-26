# BARANG Core Architecture

These rules are fixed unless an architectural change is explicitly approved.

The reusable durable audit, history, RLS, query, logging, metrics, and retention conventions are defined in [AUDIT_AND_OBSERVABILITY.md](AUDIT_AND_OBSERVABILITY.md). Future modules emit explicit technical events through `IAuditWriter`; they must not add broad entity serialization or bypass company isolation.

## System shape

- BARANG Core is a modular monolith: one deployable backend with strongly owned internal modules.
- One shared Core serves the ERP/backoffice, B2C website, and mobile application through APIs.
- PostgreSQL is the single system of record.
- The platform is API-first. External clients never access the database directly.
- UI clients present workflows but never own or duplicate business rules.
- Core business operations use transactional consistency.

## Boundaries and dependencies

- Every future module strictly owns its behavior and data.
- Modules communicate through explicit application contracts; they do not mutate another module's tables directly.
- Project dependencies flow inward: `Api -> Application`, `Api -> Infrastructure`, `Infrastructure -> Application`, `Infrastructure -> Domain`, and `Application -> Domain`. Domain depends on nothing.
- Business processes remain part of Core. A future workflow engine may coordinate approved processes but must not become the owner of business rules.

## Data and operations

- Future operational data uses an explicit soft-delete or archive strategy where retention and traceability are required.
- State-changing operations must be auditable with actor, time, company context, and meaningful change information.
- Future multi-company data is isolated by an explicit company context and enforced consistently at application and persistence boundaries.
- Prefer configuration over hard coding where behavior legitimately varies by environment or company; stable invariants remain code.
- Heavy files will use object storage when introduced. PostgreSQL retains authoritative file metadata, ownership, and relationships.

## Cross-cutting foundation

- Expected validation and business failures use the shared provider-neutral `Result`/`Error` model; exceptions are reserved for exceptional domain conditions and technical failures.
- API requests carry a validated correlation identifier through request-safe context, structured logs, responses, and Problem Details.
- Canonical business time comes from `TimeProvider` in UTC. Calendar conversion remains a UI/API-boundary concern and never changes persisted meaning.
- Current actor and company context are provider-neutral Application abstractions. Company-scoped operations must require a validated context rather than trust raw client input.
- Future sensitive create operations use scoped, expiring idempotency records. Mutable aggregates use explicit optimistic concurrency tokens and never silently overwrite concurrent updates.
- `CROSS_CUTTING_CONVENTIONS.md` defines the required usage and API mapping conventions.
- Database objects follow `DATABASE_CONVENTIONS.md`; migrations are Infrastructure-owned and applied as an explicit deployment step under `MIGRATION_STRATEGY.md`, never blindly by every API instance.
- `/health/live` reports process liveness, `/health/ready` checks PostgreSQL readiness, and `/health` remains the aggregate compatibility endpoint.
- Application orchestration owns transactions through the focused transaction executor; Domain code never commits or rolls back.
- The `Organization` module and its layered EF filters, same-company constraints, and transaction-local PostgreSQL RLS establish the multi-company foundation described in `MULTI_COMPANY_FOUNDATION.md`.
- Internal identity, opaque bearer sessions, permission-based RBAC, security audit events, and authenticated company resolution follow `IDENTITY_AND_ACCESS.md`.
- Reusable global/company lookup types, localized values, lifecycle, RLS, and option-query conventions follow `LOOKUPS_AND_MASTER_DATA.md`.
- Company-owned customer aggregates, contacts, addresses, search, lifecycle, privacy, and future order-reference rules follow `CUSTOMER_MANAGEMENT.md`.
