using BARANG.Core.Application.Finance;
using BARANG.Core.Application.Production;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Application.Quality;
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

public sealed class WorkflowBusinessCompletionAdapter(CoreDbContext db, IFinanceAdministration finance, IProductionAdministration production, InventoryAdministration inventory, IQualityAdministration quality, ITransactionExecutor tx) : IWorkflowBusinessCompletionAdapter
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
        if(outcome==WorkflowTaskOutcome.Rejected&&(stage==WorkflowStage.Packaging||stage==WorkflowStage.Shipping||stage==WorkflowStage.Delivery))return Result.Failure(WorkflowErrors.BusinessRejected);
        if(outcome==WorkflowTaskOutcome.Rejected&&stage!=WorkflowStage.QualityControl)return Result.Success();
        // Later-stage services own their transitions. Their persisted evidence is validated before orchestration advances.
        bool ready=stage switch
        {
            WorkflowStage.AdministrativeReview=>true,
            WorkflowStage.DesignPreparation=>(await production.ValidateWorkflowCompletionAsync(orderId,Domain.Production.ProductionJobType.DesignPreparation,token)).IsSuccess,
            WorkflowStage.InventoryReservation=>await inventory.HasCompleteReservationEvidenceAsync(orderId,token),
            WorkflowStage.Printing=>(await production.ValidateWorkflowCompletionAsync(orderId,Domain.Production.ProductionJobType.Print,token)).IsSuccess,
            WorkflowStage.HeatPress=>(await production.ValidateWorkflowCompletionAsync(orderId,Domain.Production.ProductionJobType.HeatPress,token)).IsSuccess,
            WorkflowStage.QualityControl=>(await quality.ValidateWorkflowCompletionAsync(orderId,outcome==WorkflowTaskOutcome.Approved?"Approve":"Reject",token)).IsSuccess,
            WorkflowStage.Packaging=>await PackagingEvidenceAsync(orderId,token),
            WorkflowStage.Shipping=>await db.Shipments.AnyAsync(x=>x.OrderId==orderId&&(x.Status==Domain.Logistics.ShipmentStatus.Shipped||x.Status==Domain.Logistics.ShipmentStatus.InTransit||x.Status==Domain.Logistics.ShipmentStatus.Delivered),token),
            WorkflowStage.Delivery=>await db.Shipments.AnyAsync(x=>x.OrderId==orderId&&x.Status==Domain.Logistics.ShipmentStatus.Delivered,token),
            _=>false
        };
        return ready?Result.Success():Result.Failure(WorkflowErrors.BusinessRejected);
        },ct);
    }

    private async Task<bool> PackagingEvidenceAsync(Guid orderId,CancellationToken token)
    {
        List<Domain.Logistics.Package> packages=await db.Packages.Where(x=>x.OrderId==orderId&&x.Status!=Domain.Logistics.PackageStatus.Draft).ToListAsync(token);
        if(packages.Count==0)return false;
        List<Domain.Logistics.PackageItem> allocations=await db.PackageItems.Where(x=>x.OrderId==orderId&&packages.Select(p=>p.Id).Contains(x.PackageId)).ToListAsync(token);
        if(allocations.Count==0)return false;
        foreach(var allocationGroup in allocations.GroupBy(x=>x.OrderItemId))
        {
            decimal accepted=await (from i in db.QCInspections join d in db.QCInspectionDispositions on new{i.CompanyId,InspectionId=i.Id} equals new{d.CompanyId,InspectionId=d.InspectionId} where i.OrderItemId==allocationGroup.Key&&i.Status==Domain.Quality.QCInspectionStatus.Approved&&d.RejectedQuantity==0&&d.ReproductionRequiredQuantity==0 select d.AcceptedQuantity).SumAsync(token);
            if(allocationGroup.Sum(x=>x.Quantity)>accepted)return false;
        }
        return true;
    }
}
#pragma warning restore CA1725
