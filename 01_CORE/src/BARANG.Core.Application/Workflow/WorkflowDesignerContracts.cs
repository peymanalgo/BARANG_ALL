using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Workflow;

public sealed record WorkflowLocalizedText(string Fa, string En, string Ar);
public sealed record DecisionAttachmentView(Guid Id, Guid RuntimeTaskId, Guid FileId, Guid FileVersionId, string FileName, string MediaType, long SizeBytes, DateTimeOffset CreatedAtUtc);
public sealed record CartablePossibleNextView(string? Outcome, WorkflowLocalizedText? DestinationStage, bool IsTerminal);
public sealed record CartableWorkflowContextView(WorkflowLocalizedText? PreviousStage, string? PreviousOutcome, WorkflowLocalizedText CurrentStage, string? CurrentPositionTitle, bool NextDependsOnDecision, IReadOnlyList<CartablePossibleNextView> PossibleNext);
public sealed record CartableWorkloadSummary(int TotalReceived, int Urgent, int NeedsAttention, int FollowUpReached);
public sealed record CartableWorkbenchSummary(string? OrderNumber, string? CustomerName, DateOnly? OrderDate, string EffectiveStatusCode, IReadOnlyList<WorkflowLocalizedText> CurrentStages, string Priority, int ItemCount, decimal TotalQuantity, decimal? GrandTotal, string? CurrencyCode, WorkflowLocalizedText? CurrentStage);
public sealed record CartableWorkbenchDetailView(Guid OrderId, CartableWorkbenchSummary Summary, CartableWorkflowContextView? WorkflowContext);
public sealed record CartableSavedFilterDefinition(string? Search, string? StageKey, Guid? CustomerId, DateOnly? FromDate, DateOnly? ToDate, string? Outcome, string? Status, string? Sort, bool Descending, bool StarredOnly, string? Aging, string? Priority, string? FollowUp);
public sealed record CartableSavedFilterView(Guid Id, string Name, CartableSavedFilterDefinition Definition, long Version);

public sealed record WorkflowGraphCommand(
    Guid? VersionId,
    WorkflowLocalizedText? Name,
    WorkflowLocalizedText? ChangeNote,
    IReadOnlyList<WorkflowGraphNode> Nodes,
    IReadOnlyList<WorkflowGraphTransition> Transitions);
public sealed record WorkflowGraphNode(string Key, int Type, WorkflowLocalizedText? Title, WorkflowLocalizedText? Description, Guid? PositionId, decimal X, decimal Y);
public sealed record WorkflowGraphTransition(string FromKey, string ToKey, string? FieldCode, int? Operator, string? Value, bool IsDefault, int SortOrder, string? OutcomeCode, string? SourceAnchor, string? TargetAnchor, WorkflowLocalizedText? Title);
public sealed record WorkflowVersionView(Guid Id, int Number, string Status, DateTimeOffset CreatedAtUtc, bool IsReadOnly, IReadOnlyList<WorkflowGraphNodeView> Nodes, IReadOnlyList<WorkflowGraphTransitionView> Transitions, long Version);
public sealed record WorkflowGraphNodeView(Guid Id, string Key, int Type, WorkflowLocalizedText Title, WorkflowLocalizedText Description, Guid? PositionId, decimal X, decimal Y, long Version);
public sealed record WorkflowGraphTransitionView(Guid Id, string FromKey, string ToKey, string? FieldCode, int? Operator, string? Value, bool IsDefault, int SortOrder, string? OutcomeCode, string SourceAnchor, string TargetAnchor, WorkflowLocalizedText Title, long Version);
public sealed record WorkflowDefinitionView(Guid Id, string Code, WorkflowLocalizedText Name, WorkflowVersionView Current, IReadOnlyList<WorkflowVersionSummary> History);
public sealed record WorkflowVersionSummary(Guid Id, int Number, string Status, DateTimeOffset CreatedAtUtc, bool IsReadOnly);

public sealed record StartDefinitionWorkflowCommand(Guid OrderId);
public sealed record CompleteDefinitionWorkflowTaskCommand(string? ResultJson, IReadOnlyList<DecisionAttachmentCommand>? Attachments, long Version);
public sealed record DecisionAttachmentCommand(Guid FileId, Guid FileVersionId);
public sealed record ClaimDefinitionWorkflowTaskCommand(long Version);
public sealed record ControlledForwardCommand(Guid TargetUserId, string Reason, long Version);
public sealed record CartableTaskQuery(string? Search, int Page, int PageSize, Guid? TaskId, string? StageKey, Guid? CustomerId, DateOnly? FromDate, DateOnly? ToDate, string? Outcome, string? Status, string? Sort, bool Descending, bool StarredOnly, string? Aging, string? Priority, string? FollowUp);
public sealed record CartableWorkbenchQuery(CartableTaskQuery Query, bool Historical);
public sealed record CartablePersonalStateCommand(bool? IsStarred, bool? Seen, DateTimeOffset? FollowUpAtUtc = null);
public enum CartableSavedFilterTab { Received, MyReferrals }
public sealed record SaveCartableFilterCommand(CartableSavedFilterTab Tab, string Name, CartableSavedFilterDefinition Definition);
public sealed record UpdateCartableFilterCommand(string Name, CartableSavedFilterDefinition Definition, long Version);
public sealed record WorkflowRuntimeTaskView(Guid Id, Guid WorkflowInstanceId, Guid OrderId, string NodeKey, string? PositionTitle, string Status, DateTimeOffset CreatedAtUtc, Guid? ClaimedByUserId, long Version, IReadOnlyList<string> Outcomes, bool IsStarred, bool Seen, DateTimeOffset? FollowUpAtUtc);
public sealed record WorkflowFileDownload(string FileName, string MediaType, long Length, Stream Content);

public interface IWorkflowDesignerAdministration
{
    Task<Result<object>> GetAsync(Guid? versionId, CancellationToken ct);
    Task<Result<object>> SaveDraftAsync(WorkflowGraphCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> ValidateAsync(Guid versionId, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> ValidateAsync(WorkflowGraphCommand command, CancellationToken ct);
    Task<Result<object>> PublishAsync(Guid versionId, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> PositionsAsync(string? search, int limit, CancellationToken ct);
    IReadOnlyList<object> ConditionFields { get; }
}

public interface IWorkflowDefinitionRuntime
{
    Task<Result<object>> StartAsync(StartDefinitionWorkflowCommand command, CancellationToken ct);
    Task<Result<object>> StartInsideTransactionAsync(Guid orderId, CancellationToken ct);
    Task<Result<object>> CompleteTaskAsync(Guid id, string? resultJson, IReadOnlyList<DecisionAttachmentCommand>? attachments, long version, CancellationToken ct);
    Task<Result<object>> ClaimTaskAsync(Guid id, long version, CancellationToken ct);
    Task<Result<object>> ReleaseTaskAsync(Guid id, long version, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> DecisionAttachmentsAsync(Guid orderId, CancellationToken ct);
    Task<Result<WorkflowFileDownload>> DownloadDecisionAttachmentAsync(Guid id, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> ForwardTargetsAsync(Guid id, string? search, CancellationToken ct);
    Task<Result<object>> ForwardTaskAsync(Guid id, ControlledForwardCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> ListSubstitutionsAsync(string? search, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> SubstitutionCandidatesAsync(string? search, CancellationToken ct);
    Task<Result<object>> CreateSubstitutionAsync(CreateWorkflowSubstitutionCommand command, CancellationToken ct);
    Task<Result<object>> RevokeSubstitutionAsync(Guid id, long version, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> TasksAsync(Guid instanceId, CancellationToken ct);
    Task<Result<object>> EligibleTasksAsync(CartableTaskQuery query, CancellationToken ct);
    Task<Result<object>> MyReferralsAsync(CartableTaskQuery query, CancellationToken ct);
    Task<Result<object>> QuickPreviewAsync(Guid id, CancellationToken ct);
    Task<Result<object>> SetPersonalStateAsync(Guid taskId, CartablePersonalStateCommand command, CancellationToken ct);
    Task<Result<CartableWorkflowContextView>> WorkflowContextAsync(Guid id, CancellationToken ct);
    Task<Result<CartableWorkloadSummary>> WorkloadSummaryAsync(CancellationToken ct);
    Task<Result<object>> WorkbenchAsync(CartableWorkbenchQuery query, CancellationToken ct);
    Task<Result<CartableWorkbenchDetailView>> WorkbenchDetailAsync(Guid id, bool historical, CancellationToken ct);
    Task<Result<IReadOnlyList<object>>> WorkbenchHistoryAsync(Guid id, CancellationToken ct);
}

public sealed record CreateWorkflowSubstitutionCommand(Guid PrincipalUserId, Guid SubstituteUserId, DateTimeOffset EffectiveFromUtc, DateTimeOffset EffectiveToUtc, string? Reason);
public interface ICartableSavedFilterAdministration
{
    Task<Result<IReadOnlyList<CartableSavedFilterView>>> ListAsync(CartableSavedFilterTab tab, CancellationToken ct);
    Task<Result<CartableSavedFilterView>> CreateAsync(SaveCartableFilterCommand command, CancellationToken ct);
    Task<Result<CartableSavedFilterView>> UpdateAsync(Guid id, UpdateCartableFilterCommand command, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, long version, CancellationToken ct);
}

public static class DecisionAttachmentPolicy
{
    public const int MaximumCount = 5;
}

public static class CartableAgingPolicy
{
    public const int AttentionHours = 8;
    public const int LongWaitingHours = 24;
    public static string Category(DateTimeOffset occurredAtUtc, DateTimeOffset now)
    {
        TimeSpan age = now - occurredAtUtc;
        if (age.TotalHours >= LongWaitingHours) return "long-waiting";
        if (age.TotalHours >= AttentionHours) return "attention";
        return "normal";
    }
}
