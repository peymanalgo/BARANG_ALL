using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Workflow;

public enum WorkflowInstanceStatus { Running, Suspended, Completed, Cancelled, Failed }
public enum WorkflowStage { AdministrativeReview, FinancialApproval, ProductionPlanning, DesignPreparation, InventoryReservation, Printing, HeatPress, QualityControl, Packaging, Shipping, Delivery, Completed }
public enum HumanTaskStatus { Open, Claimed, Completed, Cancelled }
public enum WorkflowTaskOutcome { Approved, Rejected, Completed, ReproductionRequested }
public enum ReconciliationStatus { Open, Resolved }

public static class WorkflowErrors
{
    public static Error Invalid => Error.Validation("workflow.invalid_data");
    public static Error NotFound => Error.NotFound("workflow.not_found");
    public static Error Concurrency => Error.Concurrency("workflow.concurrency_conflict");
    public static Error InvalidTransition => Error.BusinessRule("workflow.invalid_transition");
    public static Error NotEligible => Error.Authorization("workflow.task_not_eligible");
    public static Error NotOwner => Error.Authorization("workflow.task_not_owner");
    public static Error ReasonRequired => Error.Validation("workflow.reason_required");
    public static Error BusinessRejected => Error.BusinessRule("workflow.business_operation_failed");
}

public sealed class WorkflowInstance
{
    private WorkflowInstance() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; }
    public string DefinitionCode { get; private set; } = "ORDER_FULFILLMENT_V1"; public string ProviderInstanceId { get; private set; } = "";
    public WorkflowStage CurrentStage { get; private set; } public WorkflowInstanceStatus Status { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SuspendedAtUtc { get; private set; } public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? LastReason { get; private set; } public long Version { get; private set; }
    public static WorkflowInstance Create(Guid company, Guid order, string providerId, DateTimeOffset now) => new() { Id = Guid.NewGuid(), CompanyId = company, OrderId = order, ProviderInstanceId = providerId, CurrentStage = WorkflowStage.AdministrativeReview, Status = WorkflowInstanceStatus.Running, StartedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
    public Result Advance(WorkflowStage next, long expected, DateTimeOffset now) { if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status != WorkflowInstanceStatus.Running) return Result.Failure(WorkflowErrors.InvalidTransition); CurrentStage = next; if (next == WorkflowStage.Completed) { Status = WorkflowInstanceStatus.Completed; CompletedAtUtc = now; } Touch(now); return Result.Success(); }
    public Result Suspend(string reason, long expected, DateTimeOffset now) { if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(WorkflowErrors.ReasonRequired); if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status != WorkflowInstanceStatus.Running) return Result.Failure(WorkflowErrors.InvalidTransition); Status = WorkflowInstanceStatus.Suspended; SuspendedAtUtc = now; LastReason = reason.Trim(); Touch(now); return Result.Success(); }
    public Result Resume(long expected, DateTimeOffset now) { if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status != WorkflowInstanceStatus.Suspended) return Result.Failure(WorkflowErrors.InvalidTransition); Status = WorkflowInstanceStatus.Running; SuspendedAtUtc = null; Touch(now); return Result.Success(); }
    public Result Cancel(string reason, long expected, DateTimeOffset now) { if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(WorkflowErrors.ReasonRequired); if (Status == WorkflowInstanceStatus.Cancelled) return Result.Success(); if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Failed) return Result.Failure(WorkflowErrors.InvalidTransition); Status = WorkflowInstanceStatus.Cancelled; LastReason = reason.Trim(); Touch(now); return Result.Success(); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class HumanTask
{
    private HumanTask() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid WorkflowInstanceId { get; private set; } public Guid OrderId { get; private set; }
    public WorkflowStage Stage { get; private set; } public string TaskTypeCode { get; private set; } = ""; public string RequiredPermissionCode { get; private set; } = "";
    public HumanTaskStatus Status { get; private set; } public int Priority { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset DueAtUtc { get; private set; } public Guid? ClaimedByUserId { get; private set; } public DateTimeOffset? ClaimedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; } public WorkflowTaskOutcome? Outcome { get; private set; } public string? OutcomeReason { get; private set; } public long Version { get; private set; }
    public static HumanTask Create(Guid company, Guid instance, Guid order, WorkflowStage stage, string type, string permission, int priority, DateTimeOffset now, DateTimeOffset due) => new() { Id = Guid.NewGuid(), CompanyId = company, WorkflowInstanceId = instance, OrderId = order, Stage = stage, TaskTypeCode = type, RequiredPermissionCode = permission, Status = HumanTaskStatus.Open, Priority = priority, CreatedAtUtc = now, DueAtUtc = due, Version = 1 };
    public Result Claim(Guid actor, long expected, DateTimeOffset now) { if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status != HumanTaskStatus.Open) return Result.Failure(WorkflowErrors.InvalidTransition); Status = HumanTaskStatus.Claimed; ClaimedByUserId = actor; ClaimedAtUtc = now; Version++; return Result.Success(); }
    public Result Release(Guid actor, long expected) { if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status != HumanTaskStatus.Claimed || ClaimedByUserId != actor) return Result.Failure(WorkflowErrors.NotOwner); Status = HumanTaskStatus.Open; ClaimedByUserId = null; ClaimedAtUtc = null; Version++; return Result.Success(); }
    public Result Complete(Guid actor, WorkflowTaskOutcome outcome, string? reason, long expected, DateTimeOffset now) { if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (Status != HumanTaskStatus.Claimed || ClaimedByUserId != actor) return Result.Failure(WorkflowErrors.NotOwner); Status = HumanTaskStatus.Completed; Outcome = outcome; OutcomeReason = reason?.Trim(); CompletedAtUtc = now; Version++; return Result.Success(); }
    public void Cancel() { if (Status is HumanTaskStatus.Open or HumanTaskStatus.Claimed) { Status = HumanTaskStatus.Cancelled; Version++; } }
}

public sealed class WorkflowSlaPolicy
{
    private WorkflowSlaPolicy() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public WorkflowStage Stage { get; private set; } public int DurationMinutes { get; private set; }
    public string EscalationPermissionCode { get; private set; } = "workflow.manage"; public bool IsActive { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<WorkflowSlaPolicy> Create(Guid company, WorkflowStage stage, int minutes, string escalationPermission, DateTimeOffset now) => company == Guid.Empty || minutes is < 1 or > 525600 || string.IsNullOrWhiteSpace(escalationPermission) ? Result.Failure<WorkflowSlaPolicy>(WorkflowErrors.Invalid) : Result.Success(new WorkflowSlaPolicy { Id = Guid.NewGuid(), CompanyId = company, Stage = stage, DurationMinutes = minutes, EscalationPermissionCode = escalationPermission.Trim(), IsActive = true, CreatedAtUtc = now, Version = 1 });
    public Result Update(int minutes, bool active, long expected) { if (expected != Version) return Result.Failure(WorkflowErrors.Concurrency); if (minutes is < 1 or > 525600) return Result.Failure(WorkflowErrors.Invalid); DurationMinutes = minutes; IsActive = active; Version++; return Result.Success(); }
}

public sealed class WorkflowEscalation { private WorkflowEscalation() { } public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid HumanTaskId { get; private set; } public int Level { get; private set; } public DateTimeOffset EscalatedAtUtc { get; private set; } public string TargetPermissionCode { get; private set; } = ""; public static WorkflowEscalation Create(Guid c, Guid t, string p, DateTimeOffset n) => new() { Id = Guid.NewGuid(), CompanyId = c, HumanTaskId = t, Level = 1, EscalatedAtUtc = n, TargetPermissionCode = p }; }
public sealed class WorkflowHistory { private WorkflowHistory() { } public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid WorkflowInstanceId { get; private set; } public Guid? HumanTaskId { get; private set; } public string EventType { get; private set; } = ""; public DateTimeOffset OccurredAtUtc { get; private set; } public Guid? ActorUserId { get; private set; } public string? Summary { get; private set; } public static WorkflowHistory Create(Guid c, Guid i, Guid? t, string e, DateTimeOffset n, Guid? a = null, string? s = null) => new() { Id = Guid.NewGuid(), CompanyId = c, WorkflowInstanceId = i, HumanTaskId = t, EventType = e, OccurredAtUtc = n, ActorUserId = a, Summary = s }; }
public sealed class WorkflowReconciliationIssue { private WorkflowReconciliationIssue() { } public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid WorkflowInstanceId { get; private set; } public string IssueCode { get; private set; } = ""; public ReconciliationStatus Status { get; private set; } public DateTimeOffset DetectedAtUtc { get; private set; } public DateTimeOffset? ResolvedAtUtc { get; private set; } public static WorkflowReconciliationIssue Create(Guid c, Guid i, string code, DateTimeOffset n) => new() { Id = Guid.NewGuid(), CompanyId = c, WorkflowInstanceId = i, IssueCode = code, Status = ReconciliationStatus.Open, DetectedAtUtc = n }; public void Resolve(DateTimeOffset n) { Status = ReconciliationStatus.Resolved; ResolvedAtUtc = n; } }
