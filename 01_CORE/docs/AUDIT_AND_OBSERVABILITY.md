# Audit and observability

## Responsibilities

Durable audit, operational logs, and metrics are separate signals. `barang.audit_events` is the append-only business/security record and is transactionally required for audited mutations. JSON console logs diagnose runtime behavior and may be rotated independently. Built-in `System.Diagnostics.Metrics` instruments request count, failures, and duration through the `BARANG.Core` meter; an OpenTelemetry-compatible listener can be added later without changing application code.

## Event schema and taxonomy

Each event stores UTC occurrence time, nullable company, safe actor snapshot, actor type, technical action, optional generic entity type/text ID, correlation/request IDs, source channel, outcome/failure code, bounded IP/user-agent values, and sanitized JSONB `metadata` and `changes`. Messages are technical and non-localized. Event names are centralized in `AuditActions` and use dotted namespaces such as `authentication.login.succeeded`, `company.context.selected`, `security.user_role.assigned`, and `system.operation.failed`.

`changes` contains a lifecycle `kind` (`Added`, `Modified`, or `Archived`) and only meaningful changed fields. Each field uses `{ "old": value, "new": value }`; JSON `null` represents absence. Navigation graphs and whole EF entities are never serialized. Future modules opt in explicitly by calling `IAuditWriter`; there is no broad SaveChanges interceptor, avoiding accidental framework/entity capture and recursive saves.

## Trusted context and transactions

`AuditWriter` populates time from `TimeProvider`, actor from `ICurrentActor`, company from `ICompanyContext`, correlation from `ICorrelationContext`, and safe request attributes from `IAuditRequestContext`. Callers provide only event/entity/change facts and may supply a company for legitimate transitions such as company selection.

The writer adds the event to the existing scoped `CoreDbContext`. `EfTransactionExecutor` saves the mutation and audit event together. A failed or rolled-back mutation cannot leave a successful audit row. Required audit persistence failure fails the transaction; it is never silently downgraded to a log. Failed authentication uses its own successful audit transaction. Operational logging/metrics failure policy remains non-authoritative and must not replace durable audit.

Example for a future module:

```csharp
audit.Write(new AuditWriteRequest(
    "inventory.item.updated", "inventory.item", item.Id.ToString(),
    ChangeKind: AuditChangeKind.Modified,
    Changes: new Dictionary<string, AuditValueChange>
    {
        ["displayName"] = new(oldName, item.DisplayName)
    }));
```

## Redaction and privacy

`AuditRedactor` recursively matches keys case-insensitively after punctuation removal. Passwords, password hashes, tokens, access/refresh tokens, authorization values, secrets, and API keys are replaced by `[REDACTED]`. Credentials, request/response bodies, raw headers, session tokens, and hashes must never be supplied as audit values. Actor usernames may be stored as stable safe snapshots; failed-login behavior remains indistinguishable to callers.

## Isolation, permissions, and API

PostgreSQL RLS permits company-scoped reads only when `barang.company_id` matches. Inserts allow global events, the selected company, or a company currently assigned to the authenticated user. No UPDATE or DELETE RLS policy exists, and the API exposes no mutation route. Global/null-company events are not exposed to ordinary company readers; future cross-company access requires a separately designed privileged permission and database path.

`audit.read` protects:

- `GET /api/v1/audit/events`
- `GET /api/v1/audit/events/{id}`

Reads require authenticated company context. The list is newest-first with ID tie-breaking, requires page number/size, caps page size at 100, and supports bounded time, actor, action, entity, correlation, and outcome filters. DTOs expose only sanitized technical fields.

## Operations and retention

JSON request logs retain correlation, actor/user, company, channel, method, path, status, and duration without global body logging. Problem Details retains the same correlation ID. Metrics use low-cardinality method/status tags. Authentication-failure and durable-audit-write counters can be added to the existing meter when an operational exporter/alert policy is selected.

Audit is append-only and has no business soft-delete semantics. Retention is an organizational/legal policy and is intentionally not hard-coded. Monitor row/index growth; future privileged infrastructure may archive and then purge according to policy. The time-leading query model supports later PostgreSQL range partitioning by `occurred_at_utc`; introducing partition management must use reviewed migrations and must not expose audit CRUD.

No new NuGet dependency was added. The implementation uses .NET, EF Core, and PostgreSQL capabilities already licensed, self-hosted, maintained, replaceable, and available without SaaS/payment dependencies.
