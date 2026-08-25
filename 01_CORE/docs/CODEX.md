# Permanent Codex Instructions

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
- Follow `GIT_WORKFLOW.md`; do not push, tag, rewrite history, or run destructive Git commands unless explicitly instructed.
