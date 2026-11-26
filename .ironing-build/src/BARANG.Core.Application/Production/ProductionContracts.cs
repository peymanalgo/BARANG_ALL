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
public sealed record ProductionJobView(Guid Id,Guid OrderId,Guid OrderItemId,Guid?PrintOperationId,Guid?ReproductionRequestId,Guid?SourceProductionJobId,Guid?RootProductionJobId,int ReproductionDepth,ProductionJobType JobType,ProductionJobStatus Status,ProductionPriority Priority,int QueuePosition,Guid?AssignedOperatorUserId,Guid?AssignedEquipmentId,DateTimeOffset?PlannedStartUtc,DateTimeOffset?PlannedEndUtc,DateTimeOffset?StartedAtUtc,DateTimeOffset?CompletedAtUtc,string?FailureReason,string?SuspensionReason,Guid?ArtworkVersionId,Guid?FileVersionId,decimal?QuantityCompleted,string?ExecutionNotes,long Version,string OrderNumber="",string ProductTitle="",decimal PlannedQuantity=0,string PrintPosition="",bool WarehouseIssued=false,string? OperatorName=null,string? EquipmentName=null);
public sealed record ProductionPage(IReadOnlyList<ProductionJobView>Items,int Page,int PageSize,long Total);
public sealed record ProductionHistoryView(Guid Id,string EventType,Guid ActorUserId,DateTimeOffset OccurredAtUtc,string?Summary);
public sealed record EquipmentCapacityView(Guid EquipmentId,string EquipmentCode,string EquipmentName,int PlannedJobs,int InProgressJobs,Guid?CurrentJobId,DateTimeOffset?WindowStartUtc,DateTimeOffset?WindowEndUtc,int? CapacityPerHour=null,decimal? AvailableUnits=null,decimal? LoadUnits=null,double? UtilizationPercent=null);
public interface IProductionAdministration
{
 Task<Result<IReadOnlyList<ProductionJobView>>>GenerateAsync(GenerateProductionJobsCommand command,CancellationToken cancellationToken);Task<Result<IReadOnlyList<ProductionJobView>>>GenerateInsideTransactionAsync(GenerateProductionJobsCommand command,CancellationToken cancellationToken);Task<Result>ValidateWorkflowCompletionAsync(Guid orderId,ProductionJobType jobType,CancellationToken cancellationToken);Task<Result<ProductionJobView>>GetAsync(Guid id,CancellationToken cancellationToken);Task<Result<ProductionPage>>QueueAsync(ProductionQueueQuery query,CancellationToken cancellationToken);Task<Result<IReadOnlyList<EquipmentCapacityView>>>CapacityAsync(DateTimeOffset?from,DateTimeOffset?to,CancellationToken cancellationToken);Task<Result<IReadOnlyList<ProductionHistoryView>>>HistoryAsync(Guid id,CancellationToken cancellationToken);Task<Result<ProductionJobView>>PlanAsync(Guid id,PlanProductionJobCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>AssignOperatorAsync(Guid id,AssignOperatorCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>AssignEquipmentAsync(Guid id,AssignEquipmentCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>StartAsync(Guid id,long version,CancellationToken cancellationToken);Task<Result<ProductionJobView>>CompleteAsync(Guid id,CompleteProductionJobCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>FailAsync(Guid id,ProductionReasonCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>SuspendAsync(Guid id,ProductionReasonCommand command,CancellationToken cancellationToken);Task<Result<ProductionJobView>>ResumeAsync(Guid id,long version,CancellationToken cancellationToken);Task<Result<IReadOnlyList<OperatorPerformanceView>>>PerformanceAsync(DateTimeOffset? from,DateTimeOffset? to,CancellationToken cancellationToken);Task<Result<IReadOnlyList<ProductionOperatorView>>>OperatorsAsync(CancellationToken cancellationToken);
}
public sealed record WorkShiftView(Guid Id,string Code,string Title,TimeOnly StartTime,TimeOnly EndTime,bool IsActive,long Version);
public sealed record CreateWorkShiftCommand(string Code,string Title,TimeOnly StartTime,TimeOnly EndTime);
public sealed record UpdateWorkShiftCommand(string Title,TimeOnly StartTime,TimeOnly EndTime,long Version);
public sealed record WorkShiftVersionCommand(long Version);
public sealed record ProductionOperatorView(Guid UserId,string DisplayName);
public sealed record OperatorPerformanceView(Guid OperatorUserId,string OperatorName,int CompletedJobs,decimal UnitsCompleted,double AvgDurationMinutes,int OnTimeCount,int LateCount);
public interface IWorkShiftAdministration
{
 Task<Result<IReadOnlyList<WorkShiftView>>>ListAsync(bool includeInactive,CancellationToken ct);
 Task<Result<WorkShiftView>>CreateAsync(CreateWorkShiftCommand command,CancellationToken ct);
 Task<Result<WorkShiftView>>UpdateAsync(Guid id,UpdateWorkShiftCommand command,CancellationToken ct);
 Task<Result<WorkShiftView>>SetActiveAsync(Guid id,WorkShiftVersionCommand command,CancellationToken ct);
}
