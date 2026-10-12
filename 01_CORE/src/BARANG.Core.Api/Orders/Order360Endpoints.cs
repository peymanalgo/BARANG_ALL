using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Common;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Api.Orders;

public static class Order360Endpoints
{
    public static IEndpointRouteBuilder MapOrder360Endpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/orders/{orderId:guid}/360", GetAsync)
            .RequireAuthorization(SecurityPermissions.OrderRead);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid orderId, CoreDbContext db, ITransactionExecutor transactions, CancellationToken ct)
    {
        Result<IResult> result = await transactions.ExecuteAsync<IResult>(async token =>
        {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null) return Result.Success<IResult>(Results.NotFound());

        Guid[] itemIds = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == orderId)
            .Select(x => x.Id).ToArrayAsync(ct);
        Guid[] jobIds = await db.ProductionJobs.AsNoTracking().Where(x => x.OrderId == orderId)
            .Select(x => x.Id).ToArrayAsync(ct);
        Guid[] inspectionIds = await db.QCInspections.AsNoTracking().Where(x => x.OrderId == orderId)
            .Select(x => x.Id).ToArrayAsync(ct);
        Guid[] packageIds = await db.Packages.AsNoTracking().Where(x => x.OrderId == orderId)
            .Select(x => x.Id).ToArrayAsync(ct);
        Guid[] shipmentIds = await db.Shipments.AsNoTracking().Where(x => x.OrderId == orderId)
            .Select(x => x.Id).ToArrayAsync(ct);

        var customer = await db.Customers.AsNoTracking().Where(x => x.Id == order.CustomerId)
            .Select(x => new { x.Id, x.CustomerCode, x.CustomerType, x.DisplayName, x.PrimaryMobile, x.PrimaryEmail, x.IsActive })
            .SingleAsync(ct);
        var branch = await db.Branches.AsNoTracking().Where(x => x.Id == order.BranchId)
            .Select(x => new { x.Id, x.Code, x.DisplayName, x.IsActive }).SingleAsync(ct);
        var pricing = await db.OrderPriceSnapshots.AsNoTracking().Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CalculatedAtUtc).Select(x => new { x.Id, x.CurrencyCode, x.RawProductTotal, x.PrintTotal, x.AdditionalServiceTotal, x.PackagingTotal, x.ShippingTotal, x.DiscountTotal, x.TaxTotal, x.FinalTotal, x.IsFinalized, x.CalculatedAtUtc }).FirstOrDefaultAsync(ct);
        var financialState = await db.OrderFinancialStates.AsNoTracking().Where(x => x.OrderId == orderId)
            .Select(x => new { x.Status, x.Reason, x.DecidedByUserId, x.DecidedAtUtc, x.Version }).FirstOrDefaultAsync(ct);
        var workflow = await db.WorkflowInstances.AsNoTracking().Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.StartedAtUtc).Select(x => new { x.Id, x.CurrentStage, x.Status, x.StartedAtUtc, x.SuspendedAtUtc, x.Version }).FirstOrDefaultAsync(ct);

        return Result.Success<IResult>(Results.Ok(new
        {
            summary = new { order.Id, order.OrderNumber, order.Status, order.Source, order.ExternalReference, order.OrderDate, order.RequestedDeliveryDate, order.SubmittedAtUtc, order.CancelledAtUtc, order.CancellationReasonCode, order.CancellationReasonText, order.Version },
            customer,
            customerBusinessProfile = await db.CustomerBusinessProfiles.AsNoTracking().Where(x => x.CustomerId == order.CustomerId)
                .Select(x => new { x.DateOfBirth, x.EconomicCode, x.CreditLimit, x.PaymentTermsDays, x.BusinessDetails, x.Version }).FirstOrDefaultAsync(ct),
            branch,
            businessProfile = await db.OrderBusinessProfiles.AsNoTracking().Where(x => x.OrderId == orderId)
                .Select(x => new { x.ShippingMethodCode, x.PackagingTypeCode, x.IncludeInvoiceInPackage, x.OrderTypeCode, x.Notes, x.Version }).FirstOrDefaultAsync(ct),
            items = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.LineNumber)
                .Select(x => new { x.Id, x.LineNumber, x.ProductId, x.ProductVariantId, x.Quantity, x.Notes, x.IsActive, x.SnapshotProductCode, x.SnapshotProductName, x.SnapshotVariantSku, x.SnapshotVariantDisplayName, x.SnapshotSizeCode, x.SnapshotColorCode, x.SnapshotUomCode, x.Version }).ToListAsync(ct),
            printOperations = await db.OrderPrintOperations.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.OrderItemId).ThenBy(x => x.Sequence)
                .Select(x => new { x.Id, x.OrderItemId, x.Sequence, x.PrintPositionCode, x.PrintMethodCode, x.Width, x.Height, x.ArtworkVersionId, x.FileVersionId, x.Notes, x.IsActive, x.Version }).ToListAsync(ct),
            printTechnicalProfiles = await db.PrintTechnicalProfiles.AsNoTracking().Where(x => db.OrderPrintOperations.Where(p => p.OrderId == orderId).Select(p => p.Id).Contains(x.OrderPrintOperationId))
                .Select(x => new { x.OrderPrintOperationId, x.DirectionCode, x.PrintSizeCode, x.PrintCount, x.PrinterEquipmentId, x.HeatPressEquipmentId, x.Specifications, x.Version }).ToListAsync(ct),
            productTechnicalProfiles = await db.ProductTechnicalProfiles.AsNoTracking().Where(x => db.OrderItems.Where(i => i.OrderId == orderId).Select(i => i.ProductId).Contains(x.ProductId))
                .Select(x => new { x.ProductId, x.ProductVariantId, x.BrandCode, x.MaterialCode, x.GenderCode, x.FitCode, x.StyleCode, x.TechnicalAttributes, x.Version }).ToListAsync(ct),
            itemTechnicalSnapshots = await db.OrderItemTechnicalSnapshots.AsNoTracking().Where(x => itemIds.Contains(x.OrderItemId))
                .Select(x => new { x.OrderItemId, x.BrandCode, x.MaterialCode, x.GenderCode, x.FitCode, x.StyleCode, x.TechnicalAttributes, x.CreatedAtUtc }).ToListAsync(ct),
            shippingAddress = await db.OrderAddressSnapshots.AsNoTracking().Where(x => x.OrderId == orderId)
                .Select(x => new { x.RecipientName, x.RecipientPhone, x.CountryCode, x.Province, x.City, x.District, x.AddressLine1, x.AddressLine2, x.PostalCode, x.Version }).FirstOrDefaultAsync(ct),
            pricing,
            finance = new
            {
                state = financialState,
                payments = await db.Payments.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.CreatedAtUtc).Select(x => new { x.Id, x.RecordType, x.PaymentMethodCode, x.Amount, x.CurrencyCode, x.ReferenceNumber, x.PaidAtUtc, x.Status, x.OriginalPaymentId, x.ReversalReason }).ToListAsync(ct),
                invoices = await db.Invoices.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.IssuedAtUtc).Select(x => new { x.Id, x.InvoiceNumber, x.InvoiceType, x.Status, x.IssuedAtUtc }).ToListAsync(ct)
            },
            inventory = new
            {
                reservations = await db.StockReservations.AsNoTracking().Where(x => itemIds.Contains(x.OrderItemId)).Select(x => new { x.Id, x.OrderItemId, x.WarehouseId, x.ProductVariantId, x.UomCode, x.RequestedQuantity, x.ReservedQuantity, x.RemainingQuantity, x.Status, x.ExpiresAtUtc, x.Version }).ToListAsync(ct),
                shortages = await db.StockShortages.AsNoTracking().Where(x => itemIds.Contains(x.OrderItemId)).Select(x => new { x.Id, x.OrderItemId, x.WarehouseId, x.ProductVariantId, x.RequiredQuantity, x.AvailableQuantity, x.ShortageQuantity, x.IsActive }).ToListAsync(ct)
            },
            production = await db.ProductionJobs.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.CreatedAtUtc).Select(x => new { x.Id, x.OrderItemId, x.PrintOperationId, x.JobType, x.Status, x.Priority, x.AssignedOperatorUserId, x.AssignedEquipmentId, x.PlannedStartUtc, x.PlannedEndUtc, x.StartedAtUtc, x.CompletedAtUtc, x.FailureReason, x.SuspensionReason, x.ReproductionRequestId, x.Version }).ToListAsync(ct),
            quality = new
            {
                inspections = await db.QCInspections.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.CreatedAtUtc).Select(x => new { x.Id, x.OrderItemId, x.ProductionJobId, x.TemplateId, x.InspectorUserId, x.Status, x.Outcome, x.ParentInspectionId, x.ReproductionRequestId, x.StartedAtUtc, x.CompletedAtUtc, x.DecisionReason, x.Notes, x.Version }).ToListAsync(ct),
                defects = await db.QCDefects.AsNoTracking().Where(x => inspectionIds.Contains(x.InspectionId)).Select(x => new { x.Id, x.InspectionId, x.DefectTypeId, x.Quantity, x.Severity, x.Notes, x.EvidenceFileVersionId, x.RecordedAtUtc }).ToListAsync(ct),
                reproductions = await db.ReproductionRequests.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.RequestedAtUtc).Select(x => new { x.Id, x.SourceInspectionId, x.SourceProductionJobId, x.RootProductionJobId, x.RequestedStage, x.Quantity, x.Reason, x.Status, x.Depth, x.RequestedAtUtc, x.Version }).ToListAsync(ct)
            },
            logistics = new
            {
                packages = await db.Packages.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.CreatedAtUtc).Select(x => new { x.Id, x.PackageNumber, x.PackageTypeCode, x.WeightKg, x.LengthCm, x.WidthCm, x.HeightCm, x.Status, x.CompletedAtUtc, x.Version }).ToListAsync(ct),
                packageItems = await db.PackageItems.AsNoTracking().Where(x => packageIds.Contains(x.PackageId)).Select(x => new { x.PackageId, x.OrderItemId, x.Quantity }).ToListAsync(ct),
                shipments = await db.Shipments.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.CreatedAtUtc).Select(x => new { x.Id, x.ShipmentNumber, x.CarrierCode, x.ShippingMethodCode, x.TrackingCode, x.ShipmentDateUtc, x.ExpectedDeliveryDate, x.DeliveredAtUtc, x.Status, x.LastReasonCode, x.LastReasonText, x.Version }).ToListAsync(ct),
                shipmentHistory = await db.ShipmentStatusHistory.AsNoTracking().Where(x => shipmentIds.Contains(x.ShipmentId)).OrderBy(x => x.OccurredAtUtc).Select(x => new { x.ShipmentId, x.Status, x.OccurredAtUtc, x.ActorUserId, x.SourceCode, x.ReasonCode, x.ReasonText }).ToListAsync(ct)
            },
            workflow = new
            {
                instance = workflow,
                tasks = workflow == null ? [] : await db.HumanTasks.AsNoTracking().Where(x => x.WorkflowInstanceId == workflow.Id).OrderBy(x => x.DueAtUtc).Select(x => new { x.Id, x.Stage, x.TaskTypeCode, x.RequiredPermissionCode, x.Status, x.Priority, x.DueAtUtc, x.ClaimedByUserId, x.Version }).ToListAsync(ct),
                history = workflow == null ? [] : await db.WorkflowHistory.AsNoTracking().Where(x => x.WorkflowInstanceId == workflow.Id).OrderBy(x => x.OccurredAtUtc).Select(x => new { x.Id, x.HumanTaskId, x.EventType, x.OccurredAtUtc, x.ActorUserId, x.Summary }).ToListAsync(ct)
            },
            timeline = await db.OrderTimelineEvents.AsNoTracking().Where(x => x.OrderId == orderId).OrderBy(x => x.OccurredAtUtc).Select(x => new { x.Id, x.EventType, x.OccurredAtUtc, x.ActorUserId, x.Summary, x.RelatedEntityId }).ToListAsync(ct),
            audit = await db.AuditEvents.AsNoTracking().Where(x => x.EntityId == orderId.ToString() || (x.EntityType == "order" && x.EntityId == orderId.ToString())).OrderByDescending(x => x.OccurredAtUtc).Take(100).Select(x => new { x.Id, x.Action, x.EntityType, x.OccurredAtUtc, x.ActorUserId, x.Outcome, x.CorrelationId }).ToListAsync(ct)
        }));
        }, ct);
        return result.IsSuccess ? result.Value : Results.Problem(statusCode: 500);
    }
}
