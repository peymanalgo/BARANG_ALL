# Quality Control and Reproduction

Phase 16 adds company-owned quality-control records around the immutable Order and Production foundations. It does not orchestrate workflow, shipping, claims, refunds, or accounting.

## Templates and results

`QCChecklistTemplate` has a normalized company-unique technical code, optional Production job/product applicability, lifecycle, and optimistic version. Criteria are append-only definitions ordered by `SortOrder`; used templates are not edited in place, so an inspection's `TemplateId` and `TemplateVersion`, plus criterion code/name/type snapshots on every result, preserve historical meaning. Criteria support `PassFail`, `Numeric`, `Text`, and controlled `Choice` values. Required criteria must be present before approval and typed/ranged validation occurs in the domain.

`QCInspection` belongs to the same company as its Order, OrderItem, ProductionJob, and template. One production output has one inspection; reproduced output is a new ProductionJob and therefore receives a new inspection. Results and defects are append-only after recording, and finalized inspections cannot be edited.

## Defects and lifecycle

`DefectType` is a company-owned, normalized, active/inactive catalog. Inactive types remain valid historically. A defect records quantity, severity, optional criterion, notes, actor, and an optional exact Phase 10 `FileVersion` evidence reference. Same-company foreign keys and RLS prevent foreign evidence or catalog links.

Transitions are centralized: an inspection starts `InProgress`, can be suspended or escalated to `ManagementReview`, and finishes `Approved` or `Rejected`. Reasons are mandatory for suspension, escalation, and rejection. Approval requires every required criterion and rejects any blocking defect. Clients cannot assign status directly. Every mutable state uses the existing version convention and stale versions return conflict errors.

## Reproduction chains

A rejected inspection may produce one idempotent `ReproductionRequest`. Its requested stage is `DesignPreparation`, `Print`, `HeatPress`, or `FullItem`. Generation creates new Phase 15 ProductionJobs with request, immediate source job, root job, and depth references; it never rewrites or deletes earlier jobs, inspections, results, or defects. A full-item request deterministically generates design preparation, all current frozen Order print operations, and heat press jobs. Retried generation returns the existing jobs. Completed reproduction jobs can be inspected again with a parent-inspection reference, preserving an unbounded, auditable chain without imposing an invented retry limit.

## Analytics, security, and consistency

Bounded analytics return inspection outcomes, reproduction counts, and top defect frequency/quantity for an explicit UTC interval. They query operational PostgreSQL data directly; no BI warehouse is introduced.

Permissions are `qc.read`, `qc.inspect`, `qc.review`, `qc.manage`, and `reproduction.manage`; system administrators receive them idempotently. EF company filters and PostgreSQL RLS protect templates, criteria, inspections, results, defect catalogs/records, reproduction requests, and QC history. Same-company composite foreign keys protect Order, Production, and File references. Result/defect/history rows are granted no update/delete to application roles; committed QC/reproduction data has no DELETE API.

Mutations, concise `QCHistory`, and Phase 06 audit records share the PostgreSQL transaction. Audit actions cover template/catalog changes, inspection/result/defect decisions, reproduction requests, and job generation. Timeline records are business-facing and do not duplicate large technical audit payloads.

The API is rooted at `/api/v1/quality`. Future Workflow may call these commands and react to their outcomes; it must not bypass their state, permission, company, or idempotency rules.

## Approved mixed-outcome and reinspection policy (STEP 10)

Full acceptance is allowed only when every inspectable obligation for the exact OrderItem and Job is resolved. A disposition containing rejected or reproduction-required quantity keeps Quality incomplete and cannot progress automatically to shipping. A reproduction request creates a new, linked Job; its output is inspected independently and an accepted replacement resolves only the linked reproduction obligation. Original inspections, results, defects, and production history remain immutable, and original rejected quantity is never double-counted as replacement output.

Reinspection of the same Job is not an ordinary action. The canonical service rejects a second direct inspection with `qc.inspection.reinspection_not_allowed`; corrections must use a separately approved traceable process, which is outside this step. Quality inspection has no stock, payment, refund, or purchasing-receipt-QC side effects. Workflow completion uses the same Quality service for runtime, generic, legacy, and administrative paths.
