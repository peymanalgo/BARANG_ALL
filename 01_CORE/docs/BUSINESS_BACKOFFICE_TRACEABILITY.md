# Business Analysis to Backoffice Traceability

## Authority and method

This matrix was prepared from the complete **Business Analysis Document**, the complete **Software Architecture Document**, and `BARANG_Project_Phases_and_Tasks.xlsx` in `04_Document` on 2026-09-01. Business rules, data, actors, use cases, security, workflow, outputs, and operational UI behavior are authoritative. Obsolete implementation references in the source documents are intentionally mapped to the current .NET/PostgreSQL/Blazor architecture.

A row is `PASS` only where the complete applicable path exists: Backoffice UI → Core REST API → application/domain rule → PostgreSQL persistence/RLS → refreshed UI, with automated or runtime evidence. `PARTIAL` identifies a real implementation gap and is not a completion claim.

## Business-rule traceability

| Requirement / UC | Business rule and data | Domain / DB | API | Backoffice page | Permission / state | Test evidence | Status |
|---|---|---|---|---|---|---|---|
| BR-01 company isolation | Every business record is company-owned; branch/unit/warehouse scope; settings and lifecycle | Organization aggregates, company FKs, RLS policies | `/api/v1/organization/*` | Foundation administration | organization permissions + selected company | PostgreSQL RLS and Backoffice module tests | PASS |
| BR-02 identity and access | Active user, company access, roles, permissions, revocation | Security aggregates and access tables | `/api/v1/security/*`, `/api/v1/auth/*` | Users, access, roles, permissions | RBAC plus company context | authentication, permission and revocation tests | PASS |
| BR-03 customer | Person/organization, identity, contacts, addresses, history and search | Customer/contact/address aggregates | `/api/v1/customers/*` | Customer manager/detail | customer read/manage | customer API and Backoffice mutation tests | PASS |
| BR-03 financial customer profile | Credit ceiling/payment terms, economic identity and source-defined business profile; person birth date only | `CustomerBusinessProfile`, typed constraints, company FK and RLS | customer business-profile GET/PUT | Customer editor plus localized business-profile form | customer read/manage; optimistic concurrency | `BusinessProfileTests`, `BusinessProfileApiTests` persisted read/update/409/RLS | PASS |
| BR-04 master data | Product/variant, size, color, UOM, product type, supplier relations and equipment | Product, variant, supplier, supplier-product, equipment, lookup tables | `/api/v1/products`, `/suppliers`, `/equipment`, `/lookups` | Master data workspaces | product/supplier/equipment permissions | Core and Backoffice mutation tests | PASS |
| BR-04 technical garment attributes | Product type/UOM plus variant size/color and product-or-SKU brand, material/fabric, gender, fit, style and bounded attributes | `ProductTechnicalProfile`; immutable `OrderItemTechnicalSnapshot` frozen on submit | product technical-profile GET/PUT | Product editor link and localized product/SKU technical form with master-data code selectors | product read/manage; draft/final snapshot state | normalization/concurrency tests and API submit snapshot/RLS test | PASS |
| BR-05 artwork/files | Immutable versions, secure metadata/download, SHA-256, links, tags, derivatives, archive | Artwork/File aggregates, MinIO adapter, RLS | `/api/v1/artworks/*`, `/api/v1/files/*` | Artwork/file manager and history | artwork/file permissions | storage/security/integration tests | PASS |
| BR-06 order aggregate | Multi-item draft, immutable number/snapshots, source, branch, customer, dates, address | Order aggregate and snapshots | `/api/v1/orders/*` | Order wizard, search, Order 360 | order create/manage/submit/cancel | order contract/concurrency tests | PASS |
| BR-06 source header fields | Shipping method, package method, invoice-in-package, order notes/type; responsible stage remains workflow-owned | `OrderBusinessProfile`; complete address snapshot; workflow instance/task is the authoritative responsibility context | order business-profile GET/PUT and Order 360 | Wizard/header profile, complete shipping form, Order 360 profile/workflow/finance tabs | order read/manage; draft-only profile mutation; workflow RBAC | profile API 409/post-submit test; Order submit atomically starts workflow | PASS |
| BR-07 print specifications | Multiple operations; position/method/dimensions; exact artwork/file version | OrderPrintOperation | order item print-operation APIs | Order wizard and Order 360 | draft-only mutation | order API tests | PASS |
| BR-07 execution specification fields | Per-operation dimensions, direction, print size/count, designated active printer/heat-press equipment and bounded specifications/errors | `OrderPrintOperation` plus `PrintTechnicalProfile`, same-company equipment FKs/RLS | print-operation API plus technical-profile GET/PUT | Wizard persists/reloads dimensions; localized print technical form uses equipment/options | draft-only order manage; production execution remains Production-owned | API persistence/409/post-submit/RLS test and order contract tests | PASS |
| BR-08 pricing | Raw product + every print + services + packaging + shipping − discount + tax; decimal/currency/rounding; immutable snapshot | Tariffs and price snapshot aggregates | `/api/v1/pricing/*`, order pricing | Wizard preview, finance/order views | pricing permissions; draft/finalized states | deterministic and PostgreSQL pricing tests | PASS |
| BR-09 finance | Multiple payments, reversal, approval/reject/suspend, balance, invoices and history | Finance/payment/invoice aggregates | `/api/v1/orders/{id}/finance/*` | Finance workspace and Order 360 | finance/payment/invoice permissions | finance state and API tests | PASS |
| BR-10 inventory/purchasing | Immutable ledger, balance, reservation, shortage, PR/PO/partial receipt/transfer | Inventory/purchasing aggregates | `/api/v1/inventory/*`, `/purchasing/*` | Inventory/purchasing workspace | inventory/purchasing permissions | contention, consistency and API tests | PASS |
| BR-11 production | Plan, priority, assignment, design/print/heat press, lifecycle, capacity/history | ProductionJob and history | `/api/v1/production/*` | Production workspace and Order 360 | production permissions | lifecycle and integration tests | PASS |
| BR-12 quality/reproduction | Versioned checklist/results, configurable defects, review, rejection, stage-specific reproduction and repeated QC | QC and reproduction aggregates | `/api/v1/quality/*` | QC/reproduction workspace and Order 360 | QC/reproduction permissions | state/chain/history tests | PASS |
| BR-13 packaging/logistics | Packages/items, handoff, shipment/tracking, delivery/failure/return and history | Logistics aggregates | `/api/v1/logistics/*` | Logistics workspace and Order 360 | packaging/shipping/delivery permissions | logistics integration tests | PASS |
| BR-14 workflow | Current stage/owner/role, entered/due times, SLA/escalation, decisions/reasons, suspend/cancel/history | Workflow instance/task/SLA/history | `/api/v1/workflows/*` | Workflow center and Order 360 | workflow permissions and task ownership | claim race/state/integration tests | PASS |
| BR-15 cross-cutting | UTC/canonical dates; fa Jalali, en/ar Gregorian; audit, idempotency, concurrency, archive, notifications | Shared infrastructure and localized Backoffice | Problem Details, audit, notifications, idempotency | centralized date service; notification center; audit trail | 401/403/409/422 and scoped permissions | Core/Backoffice/integration tests | PASS |

## Use-case coverage

| Use cases | Data / business service | API | Backoffice | Permission / workflow | Test | Status |
|---|---|---|---|---|---|---|
| UC-001–UC-009 Foundation | Company, settings, branch, unit, warehouse | Organization APIs | Foundation managers | organization RBAC | API/UI mutation tests | PASS |
| UC-010–UC-018 Security | User, role, permission, company access | Security/auth APIs | Security managers | system/company administration | auth/RBAC tests | PASS |
| UC-019–UC-027 Customers | Customer, contact, address | Customer APIs | Customer list/detail/forms | customer RBAC | person/org/contact/address tests | PASS |
| UC-028–UC-037 Master data | Lookup, product, variant, supplier, equipment | Master APIs | Master managers | master-data RBAC | mutation/lifecycle tests | PASS |
| UC-038–UC-045 Artwork/files | Artwork/tag/version/file/link/derivative | Artwork/file APIs | Artwork/file manager/history | artwork/file RBAC | upload/hash/download/isolation tests | PASS |
| UC-046–UC-060 Orders | Draft, items, print specs and technical profiles, complete address snapshot, pricing, submit/cancel/change request/search/timeline and automatic workflow start | Order/profile APIs plus `/orders/{id}/360` | Wizard, localized profile forms, order search, complete Order 360 | centralized order/workflow states | order, pricing, workflow, profile API/concurrency tests | PASS |
| UC-061–UC-068 Pricing | tariff/discount/tax/calculation/snapshot | Pricing APIs | pricing management/wizard/detail | pricing RBAC | deterministic pricing tests | PASS |
| UC-069–UC-076 Finance | payment/reversal/approval/balance/invoice/history | Finance APIs | Finance workspace/Order 360 | finance RBAC/states | finance tests | PASS |
| UC-077–UC-090 Inventory/purchasing | ledger/reservation/shortage/PR/PO/receipt/transfer | Inventory/purchasing APIs | operational workspace | inventory/purchasing RBAC | concurrency/consistency tests | PASS |
| UC-091–UC-099 Production | generation/planning/assignment/execution/capacity/history | Production APIs | Production workspace | production job states | lifecycle tests | PASS |
| UC-100–UC-106 QC/reproduction | inspection/results/defects/review/reproduction/repeated QC | Quality APIs | QC workspace | QC state machine | state and chain tests | PASS |
| UC-107–UC-111 Packaging/delivery | package/handoff/shipment/tracking/delivery/return | Logistics APIs | Logistics workspace | logistics states | logistics tests | PASS |
| UC-112–UC-114 Workflow/reporting/audit | task inbox/history, reports and audit evidence | Workflow/report/audit/notification APIs | Workflow, reports, notification center, audit trail, Order 360 | scoped read/action permissions | workflow/audit/UI tests | PASS |

## Output and UI evidence

| Output | Operational view | Status |
|---|---|---|
| Order number/status and complete cross-department context | Order search plus Order 360 structured tabs | PASS |
| Invoice and financial history | Finance workspace and Order 360 | PASS |
| Print-ready file and version | Artwork/file history and secure download | PASS |
| Production job/history/capacity | Production workspace | PASS |
| QC checklist/defects/reproduction chain | QC workspace | PASS |
| Packaging/shipping information and history | Logistics workspace | PASS |
| Human task inbox, SLA and escalation | Workflow center | PASS |
| Finance/inventory/production/QC/logistics reports | Reporting workspace; Core-authoritative bounded reads | PASS |
| Audit trail | Permission-protected audit page and Order 360 audit tab | PASS |
| Dashboard | Permission-filtered live module summary | PASS |

## Intentionally out of scope

| Source requirement | Reason | Future scope |
|---|---|---|
| B2C storefront/customer self-service | Standalone internal Backoffice is the requested system boundary | Separate B2C phase |
| Mobile application | Explicitly excluded | Separate mobile phase |
| External carrier, bank, fiscal, SMS/email/push providers | Provider-neutral abstractions exist; no paid/cloud dependency is authorized | Provider integration phases |
| Advanced BI/data warehouse, route optimization, OEE and accounting ledger | Beyond operational OMS baseline | Future analytics/integration work |

## Completion gate

All in-scope rows are implemented. The automated gate comprises 187 Core unit tests, 4 architecture tests, 85 isolated PostgreSQL integration/API tests and 24 Backoffice tests. The profile API E2E covers persisted UI contracts, concurrency, immutable submit snapshots and RLS; the existing module E2E fixtures collectively cover the standalone walk-in lifecycle and exception paths. Runtime health and the representative cross-module Order 360 flow are revalidated before release.
