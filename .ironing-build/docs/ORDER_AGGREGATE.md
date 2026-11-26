# Order aggregate

Phase 11 makes `Order` the company-owned aggregate root. Its header records branch, customer, technical source (`Internal`, `Web`, `Mobile`, `Partner`), optional external reference, `DateOnly` business dates, status, actor/timestamps, and optimistic-concurrency version. UTC is used for instants; PostgreSQL stores business dates as `date`; Jalali values are never persisted.

## Boundary and lifecycle

The implemented lifecycle is deliberately limited to `Draft`, `Submitted`, and `Cancelled`. Ordinary commands cannot assign status. Submit and cancel are centralized transitions. Pricing, inventory, production, QC, shipping execution, and workflow state are later-phase concerns.

Draft headers, shipping snapshots, items, and print operations are editable with a version token. Item/print removal is represented by `IsActive=false`; the application role receives no DELETE permission. A submitted order is immutable through ordinary header/child commands. Cancellation is permitted from Draft or Submitted, requires a normalized technical reason code, records actor/time, and does not delete data.

## Items and snapshots

Each active `OrderItem` has a deterministic line number, positive quantity, and same-company Product/ProductVariant references. The variant must belong to the selected product and both masters must be active for draft selection and submission. Submit copies a controlled typed snapshot: product code/name, variant SKU/display name, size, color, and UOM. Those snapshot columns, not later master values, are submitted historical truth.

`OrderPrintOperation` supports multiple ordered operations per item with controlled technical position/method codes, optional dimensions, and exact same-company `ArtworkVersion` and `FileVersion` references. The initial closed vocabulary is FRONT/BACK/SLEEVE/OTHER and SCREEN/DTG/HEAT_TRANSFER/EMBROIDERY; it can later migrate to Phase 07 Lookups without changing persisted technical codes. Logical/current-file pointers are not consumed. The central Phase 10 entity-type vocabulary and dual-authorized Order routes support Order, OrderItem, and OrderPrintOperation links; arbitrary entity types and mutable logical-file-only consumption are rejected.

## Address, sources, and validation

`OrderAddressSnapshot` is copied from validated input with an optional traceable CustomerAddress id that must belong to the order's customer and company. It is independently mutable only in Draft and is stable after submit. A partial unique index on company/source/external reference prevents duplicate channel ingestion without treating content hashes as idempotency keys.

The centralized completeness validator checks Draft status, active same-company branch/customer, at least one active item, positive quantities, and usable same-company product/variant relations. It returns stable technical issue codes. Shipping is optional because no delivery-mode rule exists yet; requirements can be added centrally later.

## Numbering and submit consistency

Drafts use their UUID. Submit obtains `nextval('barang.order_number_seq')` inside the PostgreSQL transaction and formats `ORD-{GregorianYear}-{10-digit sequence}`. The sequence is concurrency-safe, gaps are acceptable, and a database unique constraint protects company/order number. A submitted retry returns the existing result and cannot allocate another number or timeline event.

Submit runs validation, freezes product snapshots, assigns the number, changes status, creates timeline and durable audit records, and commits PostgreSQL atomically. No external system participates in this transaction.

## Change requests, timeline, audit, and search

`OrderChangeRequest` is foundation only: whitelisted technical change type, bounded reason, bounded JSONB proposal, review metadata, status, and version. It never executes JSON patch or mutates an order automatically.

Timeline events are concise business history (`DraftCreated`, header/item/print changes, `Submitted`, `Cancelled`, `ChangeRequested`) ordered by instant/id. Durable audit separately records compliance events and contains no full aggregate payload. Search is company-filtered, parameterized, stable newest-first, supports header/customer/status/source/date filters, and caps page size at 100.

## Security and extension points

Permissions are `order.read`, `order.create`, `order.manage`, `order.submit`, `order.cancel`, and `order.change.manage`; the system-administrator role is bootstrapped idempotently. All six order tables have EF company filters, PostgreSQL RLS using transaction-local `barang.company_id`, same-company composite foreign keys, restrictive deletes, and no DELETE grant. Permissions never bypass RLS.

Future Pricing, Inventory, Production, and Workflow modules should consume the immutable submitted snapshot and exact artwork/file versions, add transitions through the centralized order policy, and use change requests for controlled post-submit changes rather than editing children directly.

API routes are under `/api/v1/orders`; Core remains at `http://localhost:8080`, Backoffice at `http://localhost:5100`, and the Phase 10 private MinIO console at `http://localhost:9001` in local development.
