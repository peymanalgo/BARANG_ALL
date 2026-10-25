# Packaging, Shipping, and Delivery

Phase 17 provides company-owned logistics records without carrier-specific integrations, warehouse picking, billing, refunds, or workflow orchestration.

## Packages and allocation

A `Package` has an immutable, database-sequence-backed number (`PKG-{UTC year}-{sequence}`), Order, controlled package-type code, positive weight/dimensions, lifecycle, completion actor/time, and optimistic version. `PackageItem` is an append-only allocation of a positive OrderItem quantity. Allocation is same-company and same-order, duplicate item rows are rejected, and the application checks the aggregate quantity across all packages against the submitted OrderItem quantity inside the transaction.

Packages remain editable only in `Draft`. `CompletePackaging` is centralized and requires at least one allocation plus an approved Phase 16 QC inspection for each allocated item. Completion is idempotent, audited, and freezes allocation. There is no delete or ordinary reopen path.

## Shipping warehouse handoff

`ShippingHandoff` records receipt and release of a completed package at an active Phase 04 warehouse whose technical type is `Shipping`. It records actors, UTC timestamps, status, and version. Handoff does not edit stock quantities: any future physical stock movement must invoke Phase 14's immutable inventory ledger commands. A returned shipment similarly records an active `Returns` warehouse but does not create refund or accounting entries.

## Shipment lifecycle

A `Shipment` groups one or more completed packages from one Order. A package can belong to only one shipment. Creation is retry-safe when the exact package set is already assigned and uses an immutable sequence-backed number (`SHP-{UTC year}-{sequence}`). Carrier and shipping method are normalized technical codes. Tracking is normalized through the Application-level `ICarrierService`; the local implementation performs validation only and has no external dependency.

Status changes are command-specific and centralized: `Ready -> Shipped -> InTransit -> Delivered`; `Shipped/InTransit -> FailedDelivery -> Returned`. Failure and return require bounded technical reason code/text, return requires a same-company Returns warehouse, and all transitions use optimistic concurrency. Delivery, failure, return, and duplicate same-state retries preserve timestamps and never rewrite prior status records.

`ShipmentStatusHistory` is append-only business history containing status, UTC occurrence, actor/source, and safe reason metadata. It is separate from Phase 06 audit. Tracking and every lifecycle mutation are audited in the same PostgreSQL transaction.

## Security and API

Permissions are `logistics.read`, `packaging.manage`, `shipping.manage`, and `delivery.manage`. System administrators receive them idempotently. All six tables use EF company filters, PostgreSQL RLS, same-company composite foreign keys, restrictive delete behavior, and no DELETE API. Package allocations, shipment assignments, and status history have no update/delete grants.

Routes are rooted at `/api/v1/logistics` and cover packages, allocations, completion, warehouse handoff/release, shipment creation/read, tracking, explicit lifecycle actions, and ordered history. Future carrier adapters may implement `ICarrierService`; Domain and Application remain independent of carrier SDKs.

## STEP 11 acceptance additions

Packaging completion now compares each package allocation with the independently persisted accepted QC quantity; rejected or reproduction-required quantities are not eligible. Shipping requires tracking evidence. Delivery is cumulative per exact `OrderItemId` across delivered shipments and changes the order to `Delivered` only when every active item is fully delivered. Generic workflow completion cannot satisfy Packaging, Shipping, or Delivery without these persisted records.

Customer returns are separate from warehouse shipment return. `POST /api/v1/logistics/shipments/{shipmentId}/returns` records an exact delivered shipment/order-item quantity, canonical UOM, reason, actor, company, and idempotency key. It never creates stock, refund, payment, or balance effects. The quantity is limited to the shipment's package allocation minus already-posted customer returns; follow-up warehouse disposition and Finance handling remain explicit later actions.
