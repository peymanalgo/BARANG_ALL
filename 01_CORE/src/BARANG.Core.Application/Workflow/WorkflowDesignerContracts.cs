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
