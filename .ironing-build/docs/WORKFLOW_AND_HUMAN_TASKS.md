# Workflow Engine and Human Tasks

Phase 18 adds orchestration without moving business invariants out of their owning modules. Application contracts (`IWorkflowEngineAdapter`, `IWorkflowBusinessCompletionAdapter`, and `IWorkflowAdministration`) contain no Elsa types. Infrastructure pins the self-hosted MIT-licensed `Elsa.Workflows.Core` 3.7.1 package. The Elsa adapter supplies the provider execution/correlation boundary; durable tenant state remains in BARANG PostgreSQL, so Elsa persistence cannot bypass company isolation.

## Order fulfillment definition

`ORDER_FULFILLMENT_V1` proceeds through Administrative Review, Financial Approval, Production Planning, Design Preparation, Inventory Reservation/Purchasing, Printing, Heat Press, Quality Control, Packaging, Shipping, Delivery, then Completed. QC's `ReproductionRequested` outcome routes back to Design Preparation. Completion first invokes or validates the authoritative Finance, Production, Inventory, QC, or Logistics service state; the workflow advances only on success. It never writes those aggregate statuses directly.

Human task types and required permissions are technical codes. Candidate owners are active company users whose effective roles grant both workflow API access and the stage permission; user IDs are never embedded in definitions. Claim uses a PostgreSQL row lock plus optimistic version. Only the claimant can release or complete it. Completed/cancelled tasks cannot be reclaimed. `MyTasks` includes owned and eligible unclaimed tasks; `RoleTasks` is an authorization-safe company task pool. Both are bounded to 100 rows per page.

## SLA, lifecycle, and consistency

Company SLA policies define duration in minutes and escalation permission by stage. `DueAtUtc` is calculated at task creation in UTC. Suspension keeps the deadline unchanged—the SLA clock continues—and preserves open tasks. Escalation is durable and idempotent per task/level and exposes an audit/history event suitable for Phase 19 notification publication. Resume and cancel are state-checked and versioned. Cancellation is accepted only after the existing Order service has successfully cancelled the Order; workflow cannot bypass Order cancellation rules.

Workflow history is append-only business history, separate from Phase 06 audit. Reconciliation compares running instances with Order and open-task state. It reports unsafe inconsistencies for operator review rather than forcing business state. Retries are protected by unique Order/workflow, stage/task, and escalation constraints.

## Security and persistence

Permissions are `workflow.read`, `workflow.task.claim`, `workflow.task.complete`, and `workflow.manage`; system administrator receives them idempotently. Workflow instances, human tasks, SLA policies, escalations, history, and reconciliation issues carry `CompanyId`, EF company filters, same-company foreign keys, PostgreSQL RLS, and restricted app-role grants. There is no normal DELETE API. Security/audit mutations are committed with BARANG metadata transactions.

API routes are under `/api/v1/workflows`, including start/read/history, claim/release/complete, suspend/resume/cancel, SLA administration, escalation processing, reconciliation, `tasks/mine`, and `tasks/roles`.

Elsa introduces no cloud, commercial, or region-dependent service. It is replaceable behind the adapter. Phase 19 can consume escalation events for notification delivery; later workflow definitions can reuse the same ownership, history, and reconciliation contracts.
