# BARANG Multi-Company Foundation

The `Organization` module owns companies, settings, branches, organizational units, and warehouse master data. Organizational units represent structure only and never grant permissions.

## Model and configuration

- Company codes are immutable, normalized uppercase English technical codes and globally unique. Branch, unit, warehouse, and setting codes are unique per company.
- Company-owned rows have a non-null `company_id`, canonical UTC timestamps, active/inactive lifecycle, and an optimistic `bigint version`. Operational records are never physically deleted through the application.
- Company settings are controlled typed values (`Text`, `Boolean`, `WholeNumber`, or `DecimalNumber`). They are for non-secret company behavior/configuration; credentials, tokens, connection strings, personal data, arbitrary documents, and translated UI labels do not belong there.
- Warehouses are master data only. Their controlled type has no stock or workflow behavior in this phase.
- Organizational-unit parent and optional branch references are protected by same-company composite foreign keys. Application traversal also prevents indirect hierarchy cycles.

## Context and isolation

- Normal company-owned EF queries require `ICompanyContext` and have global company filters. Missing context returns no scoped rows; application operations explicitly require context.
- `X-Company-ID` is an interim development/test mechanism only. It is accepted only in Development/Testing when `CompanyResolution:AllowDevelopmentHeader` is true, and only after the API verifies that the company exists and is active. Production rejects the header.
- Administration routes are likewise disabled outside Development/Testing and require `CompanyResolution:AllowDevelopmentAdministration`. Phase 05 will replace this gate with authenticated RBAC without changing application contracts.
- There is no unrestricted scoped-data bypass in Phase 04. Company root administration remains unscoped; access policy will be attached in Phase 05.

## PostgreSQL defense

`company_settings`, `branches`, `organizational_units`, and `warehouses` use enabled and forced RLS. Each policy compares `company_id` with `barang.company_id`. Infrastructure sets that value with transaction-local `set_config(..., true)` only after validated `ICompanyContext` resolution. Transaction completion clears it, so pooled connections cannot retain a company.

The API runtime role must be a non-superuser without `BYPASSRLS`; PostgreSQL owners/superusers are reserved for controlled migrations. Fresh local Compose volumes provision `barang_app` with configurable development credentials and default table privileges. Existing databases and production deployments must provision an equivalent least-privilege role before API startup. No application-level system/RLS bypass exists in this phase.

## API

Development/test administration routes are under `/api/v1/organization` for companies, settings, branches, organizational units, and warehouses. Endpoints use request contracts and Application views rather than EF entities. Scoped routes derive company ownership from validated context, never request bodies or query parameters.
