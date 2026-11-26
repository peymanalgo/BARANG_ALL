# Pricing, tariffs, discounts, and tax

Phase 12 adds company-owned pricing configuration and immutable Order pricing snapshots. It does not implement Finance, invoicing, payments, purchasing, inventory, production costing, or a promotion/coupon engine.

## Model and resolution

`Tariff` is effective-dated and supports `RawProduct`, `Print`, `Packaging`, `Shipping`, and `AdditionalService`. Raw-product keys identify an exact ProductVariant. Print keys use controlled position and optional method codes, so any number of print operations is priced independently. Packaging and shipping use the technical `DEFAULT` key; additional-service tariffs use their controlled code. Amounts are non-negative `decimal(18,2)` values with an explicit three-letter currency.

An active tariff applies when `ValidFrom <= pricing date <= ValidTo` (or `ValidTo` is null). Application checks and PostgreSQL exclusion constraints reject overlapping active windows for the same normalized key. Inactive and expired rules are ignored for new calculations but remain available to historical snapshots. Tariff rows are lifecycle-managed; there is no DELETE API. For upgrade compatibility, a company with no tariff configuration at all can still finalize a legacy Order with an explicit zero-valued `UNCONFIGURED` snapshot; as soon as that company creates any tariff, missing required raw/print keys are rejected. This preserves Phase 11 data while making partial pricing configuration fail closed.

Discount rules support fixed amounts and percentages. Automatic precedence is customer, customer-group, source/partner, then general; exactly one highest-specificity active rule is selected and rules are never stacked. Manual discounts are supplied only to the separately authorized recalculation endpoint and require `pricing.discount.apply`; they are bounded, audited, and cannot reduce payable value below zero. Tax resolves an effective-dated rule matching the customer's financial `TaxStatusCode` before the effective-dated `DEFAULT` rule; the selected rate and exemption are snapshotted. There is no external tax service.

Customer `PriceGroupId` is an optional company-scoped `price.group` reference. Tariffs may carry the same optional reference; the resolver prefers a matching price-group tariff over an ungrouped tariff and never combines both. This is tariff selection, not customer-group discounting. Client captions/codes are not accepted as authorization or identity proof.

## Formula and rounding

The canonical formula is:

`Raw product + Print operations + Additional services + Packaging + Shipping - Discount + Tax = Final total`

Raw and print tariff amounts are multiplied by OrderItem quantity. Packaging and shipping are order-level and counted once. Additional services are order-level controlled tariff components. Calculations use `decimal` only. Monetary boundaries use two decimal places with `MidpointRounding.AwayFromZero`; the final total is recomposed from the rounded breakdown so it always reconciles exactly. A calculation rejects mixed currencies and missing raw/print tariffs.

## Drafts and immutable snapshots

Draft recalculation creates a new current `OrderPriceSnapshot`, `OrderItemPriceSnapshot`, and typed `PriceComponentSnapshot`. The former current draft snapshot is retained and marked non-current; child rows are never overwritten. Submit recalculates in the existing Order transaction, marks the new snapshot finalized, freezes product snapshots, then assigns the immutable Order number. A repeated Submit returns the already submitted Order and creates neither another number nor another snapshot. Finalized snapshots cannot be recalculated by normal APIs, so later tariff changes do not affect submitted history.

The PostgreSQL/MinIO boundary is unaffected: pricing is entirely transactional PostgreSQL metadata.

## API and security

- `GET/POST/PUT /api/v1/pricing/tariffs`
- `POST /api/v1/pricing/discount-rules`
- `POST /api/v1/pricing/tax-rules`
- `GET /api/v1/orders/{orderId}/pricing`
- `POST /api/v1/orders/{orderId}/pricing/recalculate`
- `POST /api/v1/orders/{orderId}/pricing/recalculate-with-manual-discount`

Permissions are `pricing.read`, `pricing.manage`, and `pricing.discount.apply`, combined with Order permissions on Order pricing routes. System administrators receive them idempotently. Every pricing table has EF company filtering and PostgreSQL RLS based on transaction-local `barang.company_id`; the application roles have no DELETE grant. Tariff/rule mutations, draft recalculation, and finalization emit Phase 06 audit events in the same database transaction. Mutable tariff and draft snapshot state use optimistic concurrency and return conflict errors for stale versions.

Finance can consume finalized snapshots later without recalculating them. Any later approval, invoice, payment, or accounting model must reference the finalized snapshot and must not mutate it.
