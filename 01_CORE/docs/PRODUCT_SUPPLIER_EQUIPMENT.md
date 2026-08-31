# Product, Supplier, and Equipment foundation

Phase 09 adds company-owned master data only. It deliberately contains no stock, price, purchasing transaction, order, production job, schedule, or maintenance transaction state.

## Model and technical codes

- `Product` has an immutable normalized company-unique product code, business-entered name, optional description, product-type code, unit-of-measure code, lifecycle state, UTC timestamps, and optimistic version.
- `ProductVariant` is the future inventory/order SKU. Its immutable normalized SKU is company-unique; barcode is optional and company-unique. Size and color are controlled language-neutral codes. No quantity or price is stored.
- Product type, size, color, unit of measure, equipment type, and equipment status remain controlled technical codes resolved through the existing Lookup module. Deployments define the catalogs `product.type`, `product.size`, `product.color`, `unit.of.measure`, `equipment.type`, and `equipment.status`; Phase 09 does not seed business-specific values or add an EAV subsystem. Stored codes are language-neutral and immutable on their owning records, while active Lookup values and their fa/en/ar translations provide selectable options and labels. Historical records retain their stored code if a Lookup value is later deactivated.
- Product inactivity makes variants unavailable from new-use option APIs without changing variant lifecycle flags. Both remain retrievable for history.

## Suppliers and equipment

`Supplier` remains separate from Customer and contains only primary purchasing contact fields. Normalization reuses Customer email/phone rules. `SupplierProduct` always identifies a Product and may additionally identify one of that Product's Variants. Partial unique indexes make product-level and variant-specific sourcing deterministic. Supplier item code, preferred flag, non-negative MOQ, and non-negative lead time are planning hints, not purchasing transactions or prices.

`Equipment` has a stable company-unique code, extensible type/status codes, and optional same-company Branch/OrganizationalUnit references. Lifecycle is separate from operational status. Schedules, capacity planning, production jobs, maintenance, and telemetry are deferred.

## Security, isolation, audit, and concurrency

Permissions are `product.read/manage`, `supplier.read/manage`, and `equipment.read/manage`; system-administrator roles receive them idempotently. Every endpoint requires authentication and its permission. EF filters and PostgreSQL RLS use transaction-local `barang.company_id`. Composite foreign keys reject cross-company references. The app role receives SELECT/INSERT/UPDATE only, APIs expose no DELETE routes, and inactive records remain available historically.

Mutations emit Phase 06 technical audit events inside the existing transaction executor. Entity versions are concurrency tokens and stale writes return the existing conflict response. Operational logs do not include request payloads, supplier contact data, serial numbers, or secrets.

## APIs and future use

- `/api/v1/products`, product options, variants, and variant options
- `/api/v1/suppliers`, supplier options, and supplier-product relationships
- `/api/v1/equipment` and options filtered by type, status, or branch

Search is bounded, escaped, paginated, company-filtered, and stably ordered. Options are compact, active-only, and capped at 100. Stable IDs and technical codes are extension points for later Inventory, Orders, Pricing, Purchasing, and Production; those phases own transactional state.

No dependency was added. The implementation uses existing .NET, EF Core, Npgsql/PostgreSQL, Lookup, Audit, and transaction capabilities, avoiding licensing, hosting, payment, and regional-access risk.
