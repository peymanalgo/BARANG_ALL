# Public and client REST API

Phase 20 exposes a reusable, versioned machine-client surface at `/api/v1/client`. It is an adapter over the existing Customer, Catalog, Order, File, Pricing, and Logistics application services; it does not duplicate their business rules or bypass PostgreSQL RLS.

## Authentication and scopes

Internal users continue to use `Authorization: Bearer <session-token>`. Partner, Web, and Mobile server applications use `Authorization: ApiKey <client-id>.<secret>` plus `X-Company-ID`. API secrets are random 256-bit values, returned only when a client is created or rotated, and stored only as a SHA-256 fingerprint. Because the generated credential has full cryptographic entropy, a fast verifier is appropriate; plaintext is never persisted or logged. Rotation invalidates the previous credential immediately. Clients and credentials can be deactivated with optimistic concurrency. Each client also receives a same-ID, noninteractive application principal so existing actor foreign keys remain valid; its independently generated password is discarded and it receives no interactive company access or role assignment.

An API client must have explicit company access and an explicit scope. Supported scopes are `catalog.read`, `customer.profile.read`, `customer.profile.write`, `client.order.read`, `client.order.write`, `client.file.upload`, `pricing.preview`, and `tracking.read`. Scope checks never establish company context: the client/company link is validated first, then normal company-scoped services and RLS execute. Administrative client creation, rotation, and activation use `/api/v1/security/api-clients` and require the existing `security.user.manage` permission.

BARANG does not yet have end-customer authentication. Customer registration/profile routes therefore represent trusted channel-client operations inside the authorized company, not an invented consumer login. A future B2C identity adapter must bind a consumer subject to exactly one customer before calling these contracts.

## Error and request contracts

All failures use the existing RFC 7807 Problem Details contract with HTTP status, stable technical `code`, `category`, `correlationId`, and request instance. Exceptions, credentials, SQL, and provider details are not returned. Correlation headers continue through audit and request logs.

Pagination uses `page`/`pageSize`, one-based pages, a maximum page size of 100, and stable service ordering. Filters are explicit; arbitrary SQL or sort expressions are not accepted. Existing internal APIs retain their established `pageNumber` spelling for compatibility.

`Idempotency-Key` is required for draft creation, upload, and submit. Keys are 8–128 safe ASCII characters. Draft keys become a persisted external reference and use the existing company/source uniqueness constraint. Upload keys deterministically identify the logical file while Phase 10 preserves immutable versions and server-generated storage keys. Submit delegates to the already idempotent Order finalization, which cannot allocate a second order number or pricing snapshot. A partner-supplied external reference remains unique by company and source; retries resolve the existing draft. The key is not an authoritative business identifier and never replaces the client/company boundary.

## Endpoints

- Catalog: `/api/v1/client/catalog/products` and product variants expose only public identifiers, codes, names, option attributes, and order-usability. Supplier, cost, inventory, and security data are excluded.
- Customers: registration, scoped profile update/read, and active addresses reuse Phase 08 validation.
- Orders: draft header, items, print operations, pricing preview, submit, tracking, and cancellation reuse Phases 11–12 and 17.
- Files: multipart `/api/v1/client/files/upload` reuses Phase 10 filename, size, signature/MIME, SHA-256, MinIO, and authorization controls. No object URL or storage key is returned.
- Tracking returns only order status, a deliberately small public timeline subset, shipment/carrier/tracking state, and delivery time. Audit, finance, production, and internal QC details are excluded.

Clients never submit authoritative price totals, status assignments, company ownership, storage keys, or tracking state. Draft-only and cancellation constraints are identical to internal commands.

## Rate limits and OpenAPI

ASP.NET Core fixed-window policies cover login, client reads, mutations, uploads, and submit. Defaults are intentionally generous for local development and configurable under `RateLimits` (`LoginPerMinute`, `ClientReadPerMinute`, `ClientMutationPerMinute`, `UploadPerMinute`, and `SubmitPerMinute`). A rejection returns HTTP 429. No cloud gateway is required.

The development OpenAPI document at `/openapi/v1.json` includes the complete `/api/v1/client` request/response and multipart schemas. Authentication uses the two documented Authorization schemes above; mutation operations document `Idempotency-Key`, and pagination/error conventions are defined here. Examples must use synthetic credentials only.

## Isolation, audit, and future clients

API client authentication is global identity resolution only. Business queries begin after trusted company validation and use transaction-local `barang.company_id`, EF filters, same-company foreign keys, permissions/scopes, and PostgreSQL RLS. Client creation, rotation, activation, authorization denial, submit, cancellation, and dedupe conflicts flow through existing audit/request observability without logging secrets. Web, Mobile, and additional partner adapters should reuse this surface or its Application contracts rather than creating parallel business services.
