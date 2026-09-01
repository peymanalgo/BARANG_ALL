using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Production;

namespace BARANG.Core.Application.Production;
public sealed record GenerateProductionJobsCommand(Guid OrderId);
public sealed record PlanProductionJobCommand(ProductionPriority Priority,DateTimeOffset? PlannedStartUtc,DateTimeOffset? PlannedEndUtc,int QueuePosition,long Version);
public sealed record AssignOperatorCommand(Guid OperatorUserId,long Version);
public sealed record AssignEquipmentCommand(Guid? EquipmentId,long Version);
public sealed record ProductionVersionCommand(long Version);
public sealed record ProductionReasonCommand(string Reason,long Version);
public sealed record CompleteProductionJobCommand(decimal? QuantityCompleted,string? Notes,long Version);
public sealed record ProductionQueueQuery(ProductionJobStatus?Status,ProductionJobType?JobType,Guid?OperatorUserId,Guid?EquipmentId,Guid?BranchId,ProductionPriority?Priority,DateOnly?PlannedDate,int Page=1,int PageSize=20);
public sealed record ProductionJobView(Guid Id,Guid OrderId,Guid OrderItemId,Guid?PrintOperationId,Guid?ReproductionRequestId,Guid?SourceProductionJobId,Guid?RootProductionJobId,int ReproductionDepth,ProductionJobType JobType,ProductionJobStatus Status,ProductionPriority Priority,int QueuePosition,Guid?AssignedOperatorUserId,Guid?AssignedEquipmentId,DateTimeOffset?PlannedStartUtc,DateTimeOffset?PlannedEndUtc,DateTimeOffset?StartedAtUtc,DateTimeOffset?CompletedAtUtc,string?FailureReason,string?SuspensionReason,Guid?ArtworkVersionId,Guid?FileVersionId,decimal?QuantityCompleted,string?ExecutionNotes,long Version);
public sealed record ProductionPage(IReadOnlyList<ProductionJobView>Items,int Page,int PageSize,long Total);
public sealed record ProductionHistoryView(Guid Id,string EventType,Guid ActorUserId,DateTimeOffset OccurredAtUtc,string?Summary);
public sealed record EquipmentCapacityView(Guid EquipmentId,string EquipmentCode,string EquipmentName,int PlannedJobs,int InProgressJobs,Guid?CurrentJobId,DateTimeOffset?WindowStartUtc,DateTimeOffset?WindowEndUtc);
public interface IProductionAdministration
{
 Task<Result<IReadOnlyList<ProductionJobView>>>GenerateAsync(GenerateProductionJobsCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>GetAsync(Guid id,CancellationToken cancellationToken);Task<Result<ProductionPage>>QueueAsync(ProductionQueueQuery query,CancellationToken cancellationToken);Task<Result<IReadOnlyList<EquipmentCapacityView>>>CapacityAsync(DateTimeOffset?from,DateTimeOffset?to,CancellationToken cancellationToken);Task<Result<IReadOnlyList<ProductionHistoryView>>>HistoryAsync(Guid id,CancellationToken cancellationToken);Task<Result<ProductionJobView>>PlanAsync(Guid id,PlanProductionJobCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>AssignOperatorAsync(Guid id,AssignOperatorCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>AssignEquipmentAsync(Guid id,AssignEquipmentCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>StartAsync(Guid id,long version,CancellationToken cancellationToken);Task<Result<ProductionJobView>>CompleteAsync(Guid id,CompleteProductionJobCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>FailAsync(Guid id,ProductionReasonCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>SuspendAsync(Guid id,ProductionReasonCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>ResumeAsync(Guid id,long version,CancellationToken cancellationToken);
}
