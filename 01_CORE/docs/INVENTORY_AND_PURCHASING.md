# Inventory and Purchasing

Phase 14 uses an immutable `StockTransaction` ledger and a transactionally maintained `StockBalance` projection keyed by company, warehouse, and product variant. `OnHand` is the sum of committed ledger deltas, `Reserved` is the remaining quantity of active reservations, and `Available = OnHand - Reserved`. Product master records never carry stock quantities.

## Inventory rules

- Receipts, issues, returns, scrap, transfer legs, and adjustments are append-only ledger records. The application role has no update or delete grant on the ledger.
- Server-generated/normalized references make retrying stock mutations idempotent. Quantities use `decimal(18,4)` and the variant product's controlled UOM.
- Reservation locks the PostgreSQL balance row with `FOR UPDATE`, checks available quantity, and updates balance plus reservation in one transaction. Insufficient requests create one active shortage per demand/warehouse rather than over-reserving.
- Full or partial release, expiry, and reserved-stock issue lock both reservation and balance. Expiry is an idempotent executable operation suitable for the later scheduler infrastructure.
- A transfer posts `TransferOut` when it enters transit and a correlated `TransferIn` exactly once at completion. No negative balance is allowed.
- Adjustments require `inventory.adjust` and a bounded reason; balances are never edited directly.

## Purchasing

Purchase requests may be manual or linked to a shortage and have immutable item demand. Purchase orders use the concurrency-safe PostgreSQL sequence format `PO-{UTC year}-{10 digit sequence}`; gaps are acceptable. Supplier, warehouse, and variants are protected by same-company foreign keys. Purchase price is transactional purchasing price and is unrelated to Phase 12 selling tariffs.

PO transitions are centralized: `Draft -> Submitted -> PartiallyReceived/Received -> Closed`, with cancellation from non-received states and a mandatory reason. Each partial receipt is immutable, idempotent by company/reference, cannot exceed ordered quantity, and creates its stock receipt in the same database transaction. Received quantity is derived from receipt rows.

## Security and consistency

Permissions are `inventory.read`, `inventory.manage`, `inventory.adjust`, `purchasing.read`, and `purchasing.manage`. System administrators receive them idempotently. Every Phase 14 table has EF company filters and PostgreSQL RLS using transaction-local `barang.company_id`; same-company keys prevent cross-company Warehouse, Variant, Supplier, OrderItem, and purchasing links. Mutable lifecycle rows use optimistic concurrency and return the standard 409 conflict.

Inventory and purchasing mutations emit Phase 06 audit entries. Concise purchasing history records lifecycle events without duplicating audit payloads. There are no DELETE APIs. Production, QC, shipping execution, supplier invoicing, accounting, and workflow remain future integrations.

## APIs

Inventory routes are under `/api/v1/inventory` for balances, ledger, receipt/issue, reservations, transfers, and adjustments. Purchasing routes are under `/api/v1/purchasing` for requests, orders, receipts, and close/cancel. All require authentication, selected company, permission, and RLS authorization.
