# BARANG Naming Conventions

This standard applies to future BARANG source code, APIs, data, infrastructure, tests, and operational definitions. Existing bootstrap names that follow it remain unchanged.

## General rules

- Technical identifiers and source-code names use clear English. Never transliterate or embed Persian or Arabic UI text in an identifier.
- Prefer full words and conventional terms. Avoid unnecessary prefixes, suffixes, acronyms, and generic names such as `Helper`, `Manager`, or `Common`.
- Names describe business meaning, not the current implementation provider. Domain and Application contracts must not contain names such as `Postgres`, `Npgsql`, or a future storage vendor.
- `fa`, `en`, and `ar` are the language codes. Localized text belongs in localization resources or data, not technical names. Presentation direction (`fa`/`ar` RTL and `en` LTR) is a UI concern.
- Persist canonical instants and dates independently of locale. Use `Date`, `DateTime`, `OccurredAt`, or `...At` by meaning; never use names such as `JalaliDate`, `ShamsiDate`, or `GregorianDate` for canonical persisted values. Persian UI may render Jalali dates, while other UIs render Gregorian dates.
- Use one term consistently for one concept. Do not create synonyms across modules, contracts, and data.

## .NET solution, projects, namespaces, and folders

- The solution is `BARANG.Core.slnx`.
- Production projects use `BARANG.Core.<Layer>` while the current Core remains layer projects: `BARANG.Core.Api`, `BARANG.Core.Application`, `BARANG.Core.Domain`, and `BARANG.Core.Infrastructure`.
- Test projects use `BARANG.Core.<TestKind>Tests`, for example `BARANG.Core.UnitTests` and `BARANG.Core.IntegrationTests`.
- Project folders and `.csproj` files exactly match the project name.
- Namespaces match the project and folder path: `BARANG.Core.<Layer>[.<Module>][.<Feature>]`.
- Source folders use PascalCase and plural feature groupings where natural, such as `Orders`, `Persistence`, and `Authorization`. Keep shallow folders; do not mirror abstractions that add no navigation value.

## Modules and ownership

- A module is a cohesive business capability named with a singular PascalCase noun, such as `Sales` or `Inventory`, after that module is approved.
- Module namespaces and folders use that exact name. PostgreSQL schemas and machine codes use its lowercase equivalent.
- Names owned by a module remain under its namespace/schema. Do not use another module's entity or table name to imply shared ownership.
- Cross-module contracts use business-neutral names and explicit contracts; they do not expose another module's persistence names.

## C# types and members

- Classes, records, enums, structs, methods, properties, events, and public constants use PascalCase.
- Interfaces use PascalCase with the conventional `I` prefix, such as `IClock`. Do not prefix other types.
- Parameters and local variables use camelCase. Private fields use `_camelCase` only when a field is necessary.
- Type names are singular. Collection properties use plural nouns.
- Async methods end in `Async` unless the framework convention fixes the name. Do not suffix types with `Impl`.
- Enums use a singular type name and singular PascalCase members; use `[Flags]` only for true combinable values.

## Application operations and services

- Commands are imperative and end in `Command`: `CreateOrderCommand`, `CancelOrderCommand`.
- Queries state the requested result and end in `Query`: `GetOrderByIdQuery`, `ListOrdersQuery`.
- Handlers repeat the operation name and end in `Handler`: `CreateOrderCommandHandler`, `GetOrderByIdQueryHandler`.
- A service name states its responsibility, such as `PricingCalculator` or `OrderNumberGenerator`. Use a `Service` suffix only when it improves meaning; avoid generic repositories, services, coordinators, helpers, and managers.
- Validators match the validated type and end in `Validator`: `CreateOrderCommandValidator`, `CreateOrderRequestValidator`.

## Domain model and events

- Entities and aggregate roots use singular business nouns without `Entity`, `Model`, or `AggregateRoot` suffixes: `Order`, `OrderLine`.
- Value objects use the represented concept without a `ValueObject` suffix: `Money`, `Address`.
- Identifier types, when justified, use `<Entity>Id`: `OrderId`. Do not introduce wrapper identifiers mechanically.
- Domain events describe completed facts in past tense and end in `DomainEvent`: `OrderPlacedDomainEvent`.
- Application/integration events describe completed facts and end in `ApplicationEvent` or `IntegrationEvent` only when that distinction exists: `OrderPlacedIntegrationEvent`.
- Event handlers use `<EventName>Handler`.

## DTOs and API contracts

- Public input contracts use an action plus `Request`: `CreateOrderRequest`, `ChangeAddressRequest`.
- Public output contracts use the represented resource plus `Response`: `OrderResponse`, `OrderSummaryResponse`.
- Use `Dto` only for a real internal data-transfer shape that is neither an API request nor response. Do not suffix domain types with `Dto`.
- Contract property names use PascalCase in C# and serialize to camelCase JSON.
- Collection responses use a meaningful plural property such as `items`; pagination metadata uses `page`, `pageSize`, and `totalCount`.

## Authorization

- Permission codes are stable lowercase dot-separated identifiers: `<module>.<resource>.<action>`, for example `sales.orders.read` and `sales.orders.cancel`.
- Actions use a small verb vocabulary such as `read`, `create`, `update`, `cancel`, `approve`, and `archive`; do not encode UI screens or roles.
- C# permission constants use PascalCase groups and members, for example `SalesPermissions.OrdersRead`.
- Authorization policy names use the permission code when the policy represents one permission. Composite policies use a clear PascalCase business rule, such as `CanApproveOrder`.
- Company scope is contextual authorization, not part of a permission code. Do not create per-company permission names.

## PostgreSQL

- All identifiers use unquoted lowercase `snake_case` and must fit PostgreSQL's 63-byte identifier limit. Shorten only with unambiguous full words; avoid cryptic abbreviations.
- Each future module owns a lowercase schema matching its module name, for example `sales`. Shared platform tables require an explicitly approved owning schema; do not default new tables to `public` without a reason.
- Tables use plural nouns: `orders`, `order_lines`. Columns use singular names: `status`, `order_id`.
- Primary key columns are `id`. Company-scoped operational tables include `company_id`; company isolation must also be represented in applicable unique constraints and indexes.
- Foreign-key columns use `<referenced_singular>_id`, such as `customer_id`. Do not encode navigation paths in column names.
- Canonical instants use `timestamp with time zone` and semantic `..._at` names such as `created_at` and `occurred_at`. Calendar dates use `date` and semantic names such as `effective_date`. Never persist UI-formatted date strings.
- Future retention fields use explicit names such as `archived_at` or `deleted_at` only when the approved lifecycle requires them. Names such as `hard_delete` must not normalize physical deletion of operational/history data.
- Explicit database object names use:
  - primary key: `pk_<table>`
  - foreign key: `fk_<table>_<referenced_table>_<column>`
  - unique constraint: `uq_<table>_<columns>`
  - check constraint: `ck_<table>_<rule>`
  - non-unique index: `ix_<table>_<columns>`
  - unique index, when an index rather than a constraint is required: `ux_<table>_<columns>`
- Prefer identity columns or application-generated identifiers. If a sequence is explicitly required, use `<table>_<column>_seq`.
- Constraint/index names describe the effective columns, including `company_id` when it participates. Do not include provider or environment names.

## EF Core

- `DbSet` properties use plural PascalCase entity names: `Orders`, `OrderLines`.
- Entity configuration classes use `<Entity>Configuration`, live in the owning module's Infrastructure persistence area, and implement the standard EF configuration interface directly.
- Migrations use UTC timestamp prefixes generated by EF and a concise PascalCase action: `20260825143000_CreateSalesSchema`. Describe schema changes, not ticket numbers.
- Migration names and configuration code preserve module-owned schema/table names. Do not expose EF types in Domain or Application contracts.
- Core baseline objects and EF history use the `barang` schema. See `DATABASE_CONVENTIONS.md` for defaults, technical metadata, lifecycle, and migration-specific rules.

## REST API and JSON

- Routes are lowercase, plural, and kebab-case where more than one word is needed: `/api/v1/orders` and `/api/v1/order-lines`.
- Version APIs by major URL segment (`v1`, `v2`). Do not add a version until a public contract exists, and do not encode minor/patch versions in routes.
- Route parameters and query parameters use camelCase: `/orders/{orderId}?companyId=...`. Prefer company context from the authenticated request over client-selected company identifiers when security is implemented.
- Resource routes use nouns; operations use HTTP semantics. Add an action subresource only when no standard resource operation expresses the business action, for example `/orders/{orderId}/cancellation`.
- JSON properties use camelCase. Enum wire values use documented lowercase strings when exposed; never expose database constraint, table, or provider names.
- API contracts use locale-neutral canonical values. A language may be requested with standard language codes, but localized labels and calendar rendering remain presentation concerns.

## Configuration and environment

- .NET configuration sections and keys use PascalCase hierarchical names: `DatabaseAvailability:MaximumAttempts`.
- Options classes match the section and end in `Options`: `DatabaseAvailabilityOptions`.
- Environment variables use uppercase `SNAKE_CASE` for container/platform variables and double underscores for .NET hierarchy: `POSTGRES_DB`, `ConnectionStrings__CoreDatabase`, `DatabaseAvailability__MaximumAttempts`.
- Secret variable names describe purpose, not values. Never embed an environment (`Production`) or an actual tenant/company name unless configuration is genuinely environment-specific.

## Docker

- Compose service and resource keys use short lowercase `snake_case` names, such as `api`, `postgres`, and `barang_postgres_data`.
- Let Compose generate container names as `<project>-<service>-<replica>`; do not set `container_name`, which prevents scaling and project isolation.
- Compose project names use lowercase kebab-case, such as `barang-core`. Let Compose derive network and volume deployment names from the project and resource key unless an external resource requires an explicit name. Persistent volume keys describe owned data; avoid host paths and personal names.
- Image repositories use lowercase kebab-case and explicit stable tags. Runtime Domain/Application contracts never reference container service names.

## Jobs, workflows, audit, and errors

- Background job classes use an imperative business action plus `Job`: `ExpireReservationsJob`. Stable job codes use `<module>.<kebab-action>`, such as `inventory.expire-reservations`.
- Workflow definition codes use `<module>.<kebab-name>`, for example `sales.order-approval`. Definition display names are localized separately.
- Workflow stage, task, and outcome codes are lowercase kebab-case business terms: `manager-review`, `verify-credit`, `approved`, `rejected`. C# types remain PascalCase. Do not encode localized labels, ordering numbers, or engine/vendor names in stable codes.
- Audit/business event codes describe completed facts as `<module>.<resource>.<past-tense-event>`, for example `sales.order.created` and `sales.order.archived`. They are stable, English, and independent of UI wording.
- Error codes use lowercase dot-separated `<module>.<resource>.<condition>`, for example `sales.order.not_found` and `sales.order.invalid_state`. Infrastructure errors use their BARANG capability, not a vendor name. Messages are localized separately and must not be used as identifiers.
- Optimistic concurrency properties use `Version` in C# and `version` in PostgreSQL. The token is a non-negative `long`/`bigint`, exposed at API boundaries as an opaque concurrency value rather than a localized or provider-specific name.

## Tests

- Test projects follow `BARANG.Core.<TestKind>Tests`.
- Test classes use `<Subject>Tests`; integration fixtures use `<Subject>Factory` or `<Subject>Fixture` when needed.
- Test methods use PascalCase behavior names without underscores, matching the configured analyzers: `HealthReturnsSuccess`, `CancelOrderRejectsCompletedOrder`.
- Name the observed behavior and outcome, not implementation steps. Use `Theory` data labels and fixture names that remain deterministic and contain no secrets or machine-specific identifiers.
