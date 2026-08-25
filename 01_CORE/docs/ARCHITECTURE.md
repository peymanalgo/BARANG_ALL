# BARANG Core Architecture

These rules are fixed unless an architectural change is explicitly approved.

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
