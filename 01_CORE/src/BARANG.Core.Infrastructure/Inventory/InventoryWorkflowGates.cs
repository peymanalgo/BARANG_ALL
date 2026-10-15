using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Inventory;

public sealed partial class InventoryAdministration
{
    /// <summary>
    /// Validates the evidence required by the InventoryReservation workflow stage.
    /// This is deliberately read-only: reservation creation remains owned by
    /// ReserveOrderInsideTransactionAsync and this gate runs in that same transaction.
    /// </summary>
    public async Task<Result> ValidateReservationWorkflowCompletionAsync(
        Guid orderId,
        Guid warehouseId,
        ReserveOrderInventoryResult result,
        CancellationToken cancellationToken)
    {
        List<Domain.Orders.OrderItem> items = await db.OrderItems
            .Where(x => x.OrderId == orderId && x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        if (items.Count == 0 || result.OrderId != orderId || result.WarehouseId != warehouseId || result.Shortages.Count != 0)
            return Result.Failure(Error.BusinessRule("inventory.reservation.incomplete"));

        if (result.Reservations.Count != items.Count || result.Reservations.Select(x => x.OrderItemId).Distinct().Count() != items.Count)
            return Result.Failure(Error.BusinessRule("inventory.reservation.incomplete"));

        foreach (Domain.Orders.OrderItem item in items)
        {
            ReservationView? reservation = result.Reservations.SingleOrDefault(x => x.OrderItemId == item.Id);
            if (reservation is null || reservation.WarehouseId != warehouseId || reservation.ProductVariantId != item.ProductVariantId ||
                reservation.RequestedQuantity != item.Quantity || reservation.ReservedQuantity != item.Quantity ||
                reservation.RemainingQuantity != item.Quantity || reservation.Status != ReservationStatus.Active)
                return Result.Failure(Error.BusinessRule("inventory.reservation.incomplete"));

            string expectedUom = await Uom(item.ProductVariantId, cancellationToken);
            if (!string.Equals(InventoryText.Code(reservation.UomCode), expectedUom, StringComparison.OrdinalIgnoreCase))
                return Result.Failure(Error.BusinessRule("inventory.reservation.incomplete"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates issue evidence for every current active order item. New movements
    /// are supplied by the owning issue service because they are still pending in
    /// the ambient transaction and are not visible to a database query yet.
    /// </summary>
    public async Task<Result> ValidateIssueWorkflowCompletionAsync(
        Guid orderId,
        Guid warehouseId,
        IssueOrderInventoryResult result,
        CancellationToken cancellationToken)
    {
        List<Domain.Orders.OrderItem> items = await db.OrderItems
            .Where(x => x.OrderId == orderId && x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        if (items.Count == 0 || result.OrderId != orderId || result.WarehouseId != warehouseId)
            return Result.Failure(Error.BusinessRule("inventory.issue.incomplete"));

        List<ReservationEvidence> reservations = (await db.StockReservations
            .Where(x => x.OrderItemId != Guid.Empty && items.Select(i => i.Id).Contains(x.OrderItemId) && x.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken)).Select(ReservationEvidence.FromEntity).ToList();
        reservations.AddRange(result.Reservations
            .Where(x => reservations.All(existing => existing.Id != x.Id))
            .Select(ReservationEvidence.FromView));

        List<TransactionEvidence> transactions = (await db.StockTransactions
            .Where(x => x.TransactionType == StockTransactionType.Issue && x.SourceType == "WAREHOUSE_ISSUE" &&
                        x.SourceId != null && reservations.Select(r => r.Id).Contains(x.SourceId.Value))
            .ToListAsync(cancellationToken)).Select(TransactionEvidence.FromEntity).ToList();
        transactions.AddRange(result.Transactions
            .Where(x => transactions.All(existing => existing.Id != x.Id))
            .Select(TransactionEvidence.FromView));

        foreach (Domain.Orders.OrderItem item in items)
        {
            List<ReservationEvidence> itemReservations = reservations
                .Where(x => x.OrderItemId == item.Id && x.WarehouseId == warehouseId && x.ProductVariantId == item.ProductVariantId &&
                            x.Status is ReservationStatus.Active or ReservationStatus.PartiallyReleased or ReservationStatus.Consumed)
                .ToList();
            if (itemReservations.Count == 0)
                return Result.Failure(Error.BusinessRule("inventory.issue.incomplete"));

            string expectedUom = await Uom(item.ProductVariantId, cancellationToken);
            if (itemReservations.Any(x => !string.Equals(InventoryText.Code(x.UomCode), expectedUom, StringComparison.OrdinalIgnoreCase)))
                return Result.Failure(Error.BusinessRule("inventory.issue.incomplete"));

            decimal issued = transactions
                .Where(x => x.SourceId is Guid source && itemReservations.Any(r => r.Id == source) &&
                            x.ProductVariantId == item.ProductVariantId && x.WarehouseId == warehouseId &&
                            string.Equals(InventoryText.Code(x.UomCode), expectedUom, StringComparison.OrdinalIgnoreCase))
                .Sum(x => Math.Abs(x.QuantityDelta));
            if (issued < item.Quantity)
                return Result.Failure(Error.BusinessRule("inventory.issue.incomplete"));
        }

        return Result.Success();
    }

    /// <summary>Used by the legacy workflow adapter when no new result is in flight.</summary>
    public async Task<bool> HasCompleteReservationEvidenceAsync(Guid orderId, CancellationToken cancellationToken)
    {
        List<Domain.Orders.OrderItem> items = await db.OrderItems.Where(x => x.OrderId == orderId && x.IsActive).ToListAsync(cancellationToken);
        if (items.Count == 0) return false;
        List<StockReservation> reservations = await db.StockReservations
            .Where(x => items.Select(i => i.Id).Contains(x.OrderItemId) && x.Status == ReservationStatus.Active && x.RemainingQuantity == x.RequestedQuantity)
            .ToListAsync(cancellationToken);
        foreach (Domain.Orders.OrderItem item in items)
        {
            string uom = await Uom(item.ProductVariantId, cancellationToken);
            if (!reservations.Any(r => r.OrderItemId == item.Id && r.ProductVariantId == item.ProductVariantId && r.RequestedQuantity == item.Quantity && string.Equals(r.UomCode, uom, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }

    /// <summary>Used by the legacy workflow adapter when no new result is in flight.</summary>
    public async Task<bool> HasCompleteIssueEvidenceAsync(Guid orderId, CancellationToken cancellationToken)
    {
        List<Domain.Orders.OrderItem> items = await db.OrderItems.Where(x => x.OrderId == orderId && x.IsActive).ToListAsync(cancellationToken);
        if (items.Count == 0) return false;
        List<StockReservation> reservations = await db.StockReservations.Where(x => items.Select(i => i.Id).Contains(x.OrderItemId)).ToListAsync(cancellationToken);
        List<Guid> reservationIds = reservations.Select(x => x.Id).ToList();
        List<StockTransaction> issues = await db.StockTransactions.Where(x => x.TransactionType == StockTransactionType.Issue && x.SourceType == "WAREHOUSE_ISSUE" && x.SourceId != null && reservationIds.Contains(x.SourceId.Value)).ToListAsync(cancellationToken);
        foreach (Domain.Orders.OrderItem item in items)
        {
            string uom = await Uom(item.ProductVariantId, cancellationToken);
            List<Guid> ids = reservations.Where(r => r.OrderItemId == item.Id && r.ProductVariantId == item.ProductVariantId).Select(r => r.Id).ToList();
            if (reservations.Where(r => ids.Contains(r.Id)).Any(r => r.WarehouseId == Guid.Empty || !string.Equals(InventoryText.Code(r.UomCode), uom, StringComparison.OrdinalIgnoreCase)) ||
                issues.Where(x => x.SourceId != null && ids.Contains(x.SourceId.Value) && x.WarehouseId == reservations.First(r => r.Id == x.SourceId.Value).WarehouseId && string.Equals(InventoryText.Code(x.UomCode), uom, StringComparison.OrdinalIgnoreCase)).Sum(x => Math.Abs(x.QuantityDelta)) < item.Quantity)
                return false;
        }
        return true;
    }

    private sealed record ReservationEvidence(Guid Id, Guid OrderItemId, Guid WarehouseId, Guid ProductVariantId, string UomCode, decimal RequestedQuantity, decimal RemainingQuantity, ReservationStatus Status)
    {
        public static ReservationEvidence FromEntity(StockReservation x) => new(x.Id, x.OrderItemId, x.WarehouseId, x.ProductVariantId, x.UomCode, x.RequestedQuantity, x.RemainingQuantity, x.Status);
        public static ReservationEvidence FromView(ReservationView x) => new(x.Id, x.OrderItemId, x.WarehouseId, x.ProductVariantId, x.UomCode, x.RequestedQuantity, x.RemainingQuantity, x.Status);
    }

    private sealed record TransactionEvidence(Guid Id, Guid WarehouseId, Guid ProductVariantId, decimal QuantityDelta, string UomCode, Guid? SourceId)
    {
        public static TransactionEvidence FromEntity(StockTransaction x) => new(x.Id, x.WarehouseId, x.ProductVariantId, x.QuantityDelta, x.UomCode, x.SourceId);
        public static TransactionEvidence FromView(StockTransactionView x) => new(x.Id, x.WarehouseId, x.ProductVariantId, x.QuantityDelta, x.UomCode, x.SourceId);
    }
}
