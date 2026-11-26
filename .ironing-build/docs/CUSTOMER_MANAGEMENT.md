# Customer Management

## Aggregate and identity

`Customer` is a company-owned aggregate root with stable UUID and manually supplied, normalized uppercase `CustomerCode`. Codes are immutable and unique inside a company. `CustomerType` is the stable domain enum `Person` or `Organization`: a Person requires a first or last name; an Organization requires `LegalName`. `DisplayName` may be supplied or is deterministically derived from those names.

Optional national and registration identifiers are normalized uppercase and hard-unique per company when present. Code and these identifiers are the only blocking duplicate rules. Email, mobile, phone, tax ID, and names may legitimately be shared; they are searchable advisory duplicate signals rather than hard constraints.

## Contacts and addresses

Contacts and addresses are aggregate children with explicit `CompanyId` and a composite foreign key to the owning customer. This prevents cross-company child attachment at the database boundary.

Contact and address categories are normalized language-neutral technical codes. They are not constrained to a seeded catalog in this phase; future administrators can associate these codes with Phase 07 lookup catalogs without changing identifiers or the customer schema. `CustomerType` remains an enum because Person/Organization is a stable invariant.

There may be at most one active primary contact and one active primary address per customer. Application validation and filtered PostgreSQL unique indexes enforce the rule. A primary may be explicitly unset; deactivation automatically unsets it and does not silently select a replacement.

## Normalization and lifecycle

Emails are trimmed, basic syntax-validated, and normalized lowercase. Phones accept international `+` form and common human separators, then persist only an optional leading `+` and 7–15 digits. No country-specific numbering assumption or external validation service is used.

Customers, contacts, and addresses use active/inactive lifecycle and optimistic `version` tokens. Public DELETE routes do not exist. Inactive customers remain retrievable for future order history but are excluded by default from selector options.

## API and authorization

Routes under `/api/v1/customers` provide paginated directory search, lookup by ID/code, compact selector options, customer mutation/activation, and contact/address list and mutation/activation. Search is case-insensitive contains matching over code, names, communication fields, and identifiers; `%` and `_` are escaped. Results use stable code/ID ordering. Page size and selector limit are capped at 100.

- `customer.read`: directory, detail, children, and options.
- `customer.manage`: customer/contact/address mutations.

Anonymous access is denied. The authenticated current company determines ownership; request bodies never accept `CompanyId`.

## Isolation, audit, and privacy

EF filters and PostgreSQL RLS protect `customers`, `customer_contacts`, and `customer_addresses` using transaction-local `barang.company_id`. The application role receives SELECT/INSERT/UPDATE only, not DELETE. Composite ownership keys prevent cross-company child references.

Mutations emit centralized `customer.*`, `customer.contact.*`, and `customer.address.*` events in the same transaction. Change sets include relevant directory/lifecycle/category state without full address or communication payloads. Operational logs do not receive request bodies.

UTC timestamps and locale-independent values are canonical. Future Persian UI may render dates as Jalali while English/Arabic render Gregorian; persisted data does not change. Future Orders should reference the stable customer UUID and accept inactive customers for historical reads while excluding them from new-order selectors.

No packages or external CRM, geocoding, phone, email, or validation services were added.
