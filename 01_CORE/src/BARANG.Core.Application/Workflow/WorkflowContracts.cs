using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Workflow;

namespace BARANG.Core.Application.Workflow;

public sealed record StartWorkflowCommand(Guid OrderId);
public sealed record WorkflowVersionCommand(long Version);
public sealed record WorkflowReasonCommand(string Reason, long Version);
public sealed record CompleteWorkflowTaskCommand(WorkflowTaskOutcome Outcome, string? Reason, long Version);
public sealed record UpsertSlaCommand(WorkflowStage Stage, int DurationMinutes, string EscalationPermissionCode, bool IsActive, long Version = 0);
public sealed record WorkflowInstanceView(Guid Id, Guid OrderId, string DefinitionCode, string Provider, string ProviderInstanceId, WorkflowStage CurrentStage, WorkflowInstanceStatus Status, DateTimeOffset StartedAtUtc, DateTimeOffset? SuspendedAtUtc, long Version);
public sealed record HumanTaskView(Guid Id, Guid WorkflowInstanceId, Guid OrderId, WorkflowStage Stage, string TaskTypeCode, string RequiredPermissionCode, HumanTaskStatus Status, int Priority, DateTimeOffset DueAtUtc, Guid? ClaimedByUserId, long Version);
public sealed record WorkflowHistoryView(Guid Id, Guid? HumanTaskId, string EventType, DateTimeOffset OccurredAtUtc, Guid? ActorUserId, string? Summary);
public sealed record WorkflowSlaView(Guid Id, WorkflowStage Stage, int DurationMinutes, string EscalationPermissionCode, bool IsActive, long Version);
public sealed record ReconciliationView(Guid Id, Guid WorkflowInstanceId, string IssueCode, ReconciliationStatus Status, DateTimeOffset DetectedAtUtc);
public sealed record WorkflowPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

public interface IWorkflowEngineAdapter
{
    string ProviderName { get; }
    Task<Result<string>> StartAsync(string definitionCode, Guid correlationId, IReadOnlyDictionary<string, object?> input, CancellationToken cancellationToken);
    Task<Result> SignalAsync(string providerInstanceId, string signal, CancellationToken cancellationToken);
    Task<Result> SuspendAsync(string providerInstanceId, CancellationToken cancellationToken);
    Task<Result> ResumeAsync(string providerInstanceId, CancellationToken cancellationToken);
    Task<Result> CancelAsync(string providerInstanceId, CancellationToken cancellationToken);
}

public interface IWorkflowBusinessCompletionAdapter
{
    Task<Result> ExecuteAsync(WorkflowStage stage, Guid orderId, WorkflowTaskOutcome outcome, string? reason, CancellationToken cancellationToken);
}

public interface IWorkflowAdministration
{
    Task<Result<WorkflowInstanceView>> StartAsync(StartWorkflowCommand command, CancellationToken ct);
    Task<Result<WorkflowInstanceView>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<HumanTaskView>> ClaimAsync(Guid taskId, long version, CancellationToken ct);
    Task<Result<HumanTaskView>> ReleaseAsync(Guid taskId, long version, CancellationToken ct);
    Task<Result<HumanTaskView>> CompleteAsync(Guid taskId, CompleteWorkflowTaskCommand command, CancellationToken ct);
    Task<Result<WorkflowInstanceView>> SuspendAsync(Guid id, WorkflowReasonCommand command, CancellationToken ct);
    Task<Result<WorkflowInstanceView>> ResumeAsync(Guid id, WorkflowVersionCommand command, CancellationToken ct);
    Task<Result<WorkflowInstanceView>> CancelAsync(Guid id, WorkflowReasonCommand command, CancellationToken ct);
    Task<Result<WorkflowPage<HumanTaskView>>> MyTasksAsync(HumanTaskStatus? status, int page, int pageSize, CancellationToken ct);
    Task<Result<WorkflowPage<HumanTaskView>>> RoleTasksAsync(WorkflowStage? stage, HumanTaskStatus? status, int page, int pageSize, CancellationToken ct);
    Task<Result<IReadOnlyList<WorkflowHistoryView>>> HistoryAsync(Guid id, CancellationToken ct);
    Task<Result<WorkflowSlaView>> UpsertSlaAsync(UpsertSlaCommand command, CancellationToken ct);
    Task<Result<int>> EscalateOverdueAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<ReconciliationView>>> ReconcileAsync(CancellationToken ct);
}
