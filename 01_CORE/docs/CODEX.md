# Permanent Codex Instructions

- Audit/history changes must follow `AUDIT_AND_OBSERVABILITY.md`: use centralized event names, explicit sanitized metadata/change sets, the existing transaction boundary, `audit.read`, and PostgreSQL RLS. Never persist credentials, tokens, authorization headers, or secret-bearing request bodies.
- Lookup/master-data changes must follow `LOOKUPS_AND_MASTER_DATA.md`: immutable technical codes, normalized translations, deterministic scope, active/inactive lifecycle, transactionally required audit, and company RLS. Do not add business catalogs or hard-delete lookup rows casually.
- Customer changes must follow `CUSTOMER_MANAGEMENT.md`: company-owned aggregates and children, immutable customer codes, bounded search, active/inactive lifecycle, privacy-safe audit, composite ownership, optimistic concurrency, and PostgreSQL RLS. Do not add public or hard-delete customer APIs.

- Inspect the existing architecture and local instructions before editing.
- Preserve module boundaries and the documented dependency direction.
- Do not change architecture without explicit approval.
- Do not introduce premature microservices or speculative infrastructure.
- Keep business logic in Core and never duplicate it across clients or modules.
- Once modules exist, never mutate another module's tables directly.
- Add or adjust meaningful tests with each implementation.
- Run restore, Release build, and Release tests before completion.
- Never expose or commit secrets; retain safe configuration templates.
- Never commit build outputs, test results, logs, or generated artifacts.
- Keep changes scoped to the requested phase or task.
- Follow `CROSS_CUTTING_CONVENTIONS.md`: use the shared result/error, correlation, `TimeProvider`, current-actor, and company-context conventions rather than local substitutes.
- Follow `DATABASE_CONVENTIONS.md` and `MIGRATION_STRATEGY.md`; never auto-migrate from every production API instance or run database tests against a database not explicitly named as a test database.
- Follow `MULTI_COMPANY_FOUNDATION.md`; never accept a client-supplied company identifier as authorization proof, bypass company filters casually, or run the API with a PostgreSQL superuser/RLS-bypass role.
- Follow `IDENTITY_AND_ACCESS.md`; never expose credentials, invent ad-hoc permissions, authorize by role/unit display name, or bypass authenticated company access.
- Follow `GIT_WORKFLOW.md`; do not push, tag, rewrite history, or run destructive Git commands unless explicitly instructed.
