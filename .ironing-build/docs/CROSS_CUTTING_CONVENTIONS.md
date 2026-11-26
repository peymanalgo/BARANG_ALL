# BARANG Cross-Cutting Conventions

These conventions are mandatory for future Core modules and preserve provider-neutral Domain/Application contracts.

## Results, errors, and exceptions

- Use `Result` or `Result<T>` for expected validation, business-rule, not-found, conflict, authorization, authentication, and concurrency outcomes.
- An `Error` contains a stable English technical code, category, and optional technical description. Codes and categories drive mapping; localized user-facing messages are resolved later at an API/UI boundary.
- Read `Result<T>.Value` only after success and `Result.Error` only after failure. Invalid access fails immediately.
- Use `DomainException` only when continuing would violate an invariant and returning a Result is not a reasonable control path.
- Use `InfrastructureException` when Infrastructure adds stable context to a technical failure; always retain the original exception as `InnerException`.
- Unexpected exceptions propagate to the API exception handler. Do not catch and replace them merely to hide them, and never expose exception, database, or stack details to clients.
- Domain and Application contain no HTTP status, Problem Details, or localized message logic.

## Correlation and structured logging

- API clients may send one `X-Correlation-ID` containing at most 64 ASCII letters, digits, `.`, `_`, or `-`. Invalid or missing values are replaced with a generated identifier.
- The API returns the identifier in `X-Correlation-ID`. Request-safe context makes it available to Application and Infrastructure; future background/integration scopes must establish their own correlation identifier.
- Request logs use structured `CorrelationId`, `RequestPath`, `RequestMethod`, `ActorId`, `UserId`, `CompanyId`, and `SourceChannel` properties when available.
- Never log passwords, tokens, secrets, sensitive personal data, request bodies by default, or complete file contents. Container stdout/stderr remains the provider-neutral production sink.

## API failure mapping

- The API maps errors to RFC Problem Details: validation 400, authentication 401, authorization 403, not found 404, conflict/concurrency 409, business rule 422, and infrastructure/unexpected 500.
- Problem Details includes stable `code`, `category`, and `correlationId` extensions. Its safe title can later be localized; technical descriptions and stack traces are not returned.
- Result-to-HTTP mapping and global exception handling remain in API, never Domain or Application.

## Canonical time

- Inject the built-in `TimeProvider` and call `GetUtcNow()` for business timestamps. Tests provide a deterministic `TimeProvider`.
- Do not scatter `DateTime.Now` or `DateTime.UtcNow`. Persist instants canonically and calendar dates without UI formatting.
- Persian UI converts input/display to Jalali; English and Arabic UI use Gregorian. Conversion is outside domain persistence and does not alter canonical values.

## Actor and company context

- `ICurrentActor` exposes a provider-neutral actor descriptor for internal, B2C, mobile, partner/client, background-job, and system sources. It does not expose `ClaimsPrincipal` or define permissions.
- `ICompanyContext` exposes only a validated current company. A company-scoped operation calls `RequireCompanyId()` and fails clearly when context is absent.
- Never populate company context by trusting a raw route, query, or header value. Later authentication/authorization infrastructure resolves permitted company scope. Background jobs establish it explicitly.

## Pagination

- Use one-based `PagedRequest` pagination with a default page size of 20 and maximum of 100.
- `PagedResult<T>` returns immutable items plus page number, page size, total count, total pages, and previous/next indicators.
- Add sorting/filter contracts only for concrete use cases; do not create unbounded dynamic query languages.

## Idempotency

- Sensitive create operations will accept an API `Idempotency-Key`, translate it to `IdempotencyKey`, and scope uniqueness by operation, actor/client, and company where applicable.
- Keys are 8-128 safe ASCII characters. Records include canonical expiration and a stable result/reference so retries can return the established outcome.
- Storage must be durable, bounded by expiration, and transactional with the protected operation where required. A process-global dictionary is never an implementation.

## Optimistic concurrency

- Mutable aggregates that can receive competing updates opt into a non-negative `long Version` mapped to PostgreSQL `bigint version` and configured with EF Core `IsConcurrencyToken()`.
- The version starts at an explicitly configured value and increments exactly once for each successful state change. It is not a PostgreSQL-provider type and does not leak provider names into contracts.
- API clients will receive an opaque concurrency token and must provide it for protected updates. A stale token becomes `Error.Concurrency(...)` and HTTP 409.
- Never silently overwrite a concurrent update. Do not automatically retry a business command when doing so could overwrite another actor's valid change; the client or workflow must reconcile and resubmit.
- No base aggregate or EF convention is introduced until a real aggregate needs the pattern.
