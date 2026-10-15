using BARANG.Core.Application.Finance;
using BARANG.Core.Application.Production;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Infrastructure.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Finance;
using BARANG.Core.Domain.Orders;
using BARANG.Core.Domain.Workflow;
using BARANG.Core.Infrastructure.Persistence;
using BARANG.Core.Application.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Workflow;
#pragma warning disable CA1725

public sealed class WorkflowBusinessCompletionAdapter(CoreDbContext db, IFinanceAdministration finance, IProductionAdministration production, InventoryAdministration inventory, ITransactionExecutor tx) : IWorkflowBusinessCompletionAdapter
{
    public async Task<Result> ExecuteAsync(WorkflowStage stage,Guid orderId,WorkflowTaskOutcome outcome,string? reason,CancellationToken ct)
    {
        if(stage==WorkflowStage.FinancialApproval)
        {
            var state=await finance.GetFinancialStateAsync(orderId,ct);if(state.IsFailure)return Result.Failure(state.Error);
            Guid? currentSnapshot=await db.OrderPriceSnapshots.AsNoTracking().Where(x=>x.OrderId==orderId&&x.IsCurrent&&x.IsFinalized).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
            if(outcome==WorkflowTaskOutcome.Approved&&state.Value.Status==FinancialStatus.Approved&&state.Value.ApprovedPriceSnapshotId is Guid approvedSnapshot&&approvedSnapshot==currentSnapshot)return Result.Success();
            if(outcome==WorkflowTaskOutcome.Rejected&&state.Value.Status==FinancialStatus.Rejected)return Result.Success();
            return Result.Failure(FinanceErrors.ApprovalRequired);
        }
        if(stage==WorkflowStage.ProductionPlanning){var generated=await production.GenerateAsync(new(orderId),ct);return generated.IsSuccess?Result.Success():Result.Failure(generated.Error);}
        return await tx.ExecuteAsync(async token=>
        {
        Order? order=await db.Orders.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==orderId,token);
        if(order is null||order.Status!=OrderStatus.Submitted)return Result.Failure(WorkflowErrors.BusinessRejected);
        if(outcome==WorkflowTaskOutcome.Rejected)return Result.Success();
        // Later-stage services own their transitions. Their persisted evidence is validated before orchestration advances.
        bool ready=stage switch
        {
            WorkflowStage.AdministrativeReview=>true,
            WorkflowStage.DesignPreparation=>await db.ProductionJobs.AnyAsync(x=>x.OrderId==orderId&&x.JobType==Domain.Production.ProductionJobType.DesignPreparation&&x.Status==Domain.Production.ProductionJobStatus.Completed,token),
            WorkflowStage.InventoryReservation=>await inventory.HasCompleteReservationEvidenceAsync(orderId,token),
            WorkflowStage.Printing=>await db.ProductionJobs.AnyAsync(x=>x.OrderId==orderId&&x.JobType==Domain.Production.ProductionJobType.Print&&x.Status==Domain.Production.ProductionJobStatus.Completed,token),
            WorkflowStage.HeatPress=>await db.ProductionJobs.AnyAsync(x=>x.OrderId==orderId&&x.JobType==Domain.Production.ProductionJobType.HeatPress&&x.Status==Domain.Production.ProductionJobStatus.Completed,token),
            WorkflowStage.QualityControl=>await db.QCInspections.AnyAsync(x=>x.OrderId==orderId&&x.Outcome==Domain.Quality.QCOutcome.Approved,token),
            WorkflowStage.Packaging=>await db.Packages.AnyAsync(x=>x.OrderId==orderId&&x.Status!=Domain.Logistics.PackageStatus.Draft,token),
            WorkflowStage.Shipping=>await db.Shipments.AnyAsync(x=>x.OrderId==orderId&&x.Status!=Domain.Logistics.ShipmentStatus.Ready,token),
            WorkflowStage.Delivery=>await db.Shipments.AnyAsync(x=>x.OrderId==orderId&&x.Status==Domain.Logistics.ShipmentStatus.Delivered,token),
            _=>false
        };
        return ready?Result.Success():Result.Failure(WorkflowErrors.BusinessRejected);
        },ct);
    }
}
#pragma warning restore CA1725
