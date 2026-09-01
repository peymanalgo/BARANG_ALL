# Background jobs, notifications, and cache

Phase 19 adds replaceable asynchronous infrastructure without moving business rules out of their owning application services.

## Hangfire and scheduling

Hangfire runs inside Core and persists its state in the PostgreSQL `hangfire` schema. Infrastructure owns all Hangfire types; Application exposes only scheduler contracts. Three idempotent recurring jobs run each minute: the outbox dispatcher, Phase 18 SLA escalation, and the Phase 10 file-processing bridge. Concurrent executions are suppressed, while the underlying business operations retain their own database idempotency constraints. The Hangfire dashboard is not exposed.

## Transactional outbox

`outbox_messages` records a bounded JSON payload, stable message and correlation IDs, optional company ownership, attempts, next-attempt time, and terminal state. Business code inserts an outbox row in the same PostgreSQL transaction as its audit and mutation. Dispatch uses company-scoped RLS transactions and row locking. Handlers are keyed by message identity; retries use bounded exponential backoff (five attempts, capped at 30 minutes). Permanent or exhausted failures create a durable `dead_letter_items` review record. Manual retry requires `backgroundjob.review` and never deletes failure history.

PostgreSQL and an external provider are not a distributed transaction. A committed outbox row is the durable promise; provider calls are retried and handlers must be idempotent.

## Notifications

Notifications use technical channels `InApp`, `Sms`, `Email`, and `Push`. In-app notifications are fully implemented, company/user isolated, paginated, and support read/all-read commands. SMS, email, and push are provider-neutral Application contracts backed by safe local implementations for development. No provider credentials or sensitive full payloads are stored. External providers can be introduced later without exposing their SDKs to Domain or Application.

Permissions are `notification.read`, `notification.manage`, and `backgroundjob.review`. System administrators receive them idempotently. Company-owned notification and review tables use EF filters, PostgreSQL RLS, same transaction-local `barang.company_id`, and no DELETE grant.

## Redis cache

`IApplicationCache` is an Application abstraction implemented by Redis. Cache values are JSON read optimizations only; PostgreSQL remains authoritative. Keys have the form `barang:{company-N}:culture:{normalized-culture}:{identity}`, so tenant and culture are mandatory boundaries. Explicit removal is available after committed mutations; callers must invalidate through a post-commit action/outbox handler when introducing cached master-data reads. Authentication, authorization, company access, and revocation decisions are deliberately not cached. TTL is a performance fallback, not the correctness mechanism.

Local Redis is pinned in Compose, bound to localhost port 6379, health checked, and uses a named persistent volume. Production configuration is supplied through `ConnectionStrings:Redis`; no production secret is tracked.

## Operations and safety

Operational attempts belong in structured logs and Hangfire state; security-sensitive manual retries/read-state actions use durable audit. Useful operational measures are pending outbox count, exhausted/dead-letter count, retry attempts, notification failures, and scheduler duration. Background processors enumerate active companies and establish trusted company and system-actor scopes before beginning an RLS transaction, preventing pooled-context leakage.

Local services remain available at Core `http://localhost:8080`, Backoffice `http://localhost:5100`, and MinIO Console `http://localhost:9001`. Redis is a protocol endpoint at `localhost:6379`, not a web UI.

## Dependency decision

Hangfire Core/AspNetCore 1.8.25 and Hangfire.PostgreSql 1.21.1 are pinned, self-hosted, Linux-compatible, and provider-contained. Hangfire is LGPL-3.0 with an optional commercial offering; Phase 19 uses only the open-source feature set. StackExchange.Redis 3.1.31 is MIT licensed, mature, self-hostable, Linux-compatible, and replaceable behind `IApplicationCache`. None requires a paid or cloud service, payment account, or region-specific SaaS access.
