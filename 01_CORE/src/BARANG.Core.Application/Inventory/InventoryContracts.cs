using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;

namespace BARANG.Core.Application.Inventory;
public sealed record StockMovementCommand(Guid WarehouseId,Guid ProductVariantId,decimal Quantity,string Reference,string? ReasonCode,DateTimeOffset? OccurredAtUtc=null);
public sealed record ReserveStockCommand(Guid OrderItemId,Guid WarehouseId,decimal Quantity,DateTimeOffset? ExpiresAtUtc,string IdempotencyKey);
public sealed record ReleaseReservationCommand(decimal Quantity,long Version);
public sealed record IssueReservationCommand(decimal Quantity,long Version,string Reference);
public sealed record TransferCommand(Guid SourceWarehouseId,Guid DestinationWarehouseId,Guid ProductVariantId,decimal Quantity,string Reference);
public sealed record CompleteTransferCommand(long Version);
public sealed record PurchaseRequestItemCommand(Guid ProductVariantId,decimal Quantity);
public sealed record CreatePurchaseRequestCommand(Guid WarehouseId,string Reason,Guid? SourceShortageId,IReadOnlyList<PurchaseRequestItemCommand> Items);
public sealed record PurchaseOrderItemCommand(Guid ProductVariantId,decimal Quantity,decimal UnitPurchasePrice);
public sealed record CreatePurchaseOrderCommand(Guid SupplierId,Guid WarehouseId,string CurrencyCode,DateOnly? ExpectedDeliveryDate,IReadOnlyList<PurchaseOrderItemCommand> Items);
public sealed record PurchaseReceiptCommand(Guid PurchaseOrderItemId,decimal Quantity,string Reference);
public sealed record ClosePurchaseOrderCommand(bool Cancel,string? Reason,long Version);
public sealed record StockBalanceView(Guid WarehouseId,Guid ProductVariantId,decimal OnHand,decimal Reserved,decimal Available,long Version);
public sealed record StockTransactionView(Guid Id,Guid WarehouseId,Guid ProductVariantId,StockTransactionType Type,decimal QuantityDelta,string UomCode,string SourceType,Guid? SourceId,string Reference,DateTimeOffset OccurredAtUtc);
public sealed record ReservationView(Guid Id,Guid OrderItemId,Guid WarehouseId,Guid ProductVariantId,decimal RequestedQuantity,decimal ReservedQuantity,decimal RemainingQuantity,ReservationStatus Status,DateTimeOffset? ExpiresAtUtc,long Version);
public sealed record ReserveStockResult(ReservationView? Reservation,Guid? ShortageId,decimal Available,decimal ShortageQuantity);
public sealed record TransferView(Guid Id,Guid SourceWarehouseId,Guid DestinationWarehouseId,Guid ProductVariantId,decimal Quantity,StockTransferStatus Status,long Version);
public sealed record PurchaseRequestView(Guid Id,Guid WarehouseId,string Reason,PurchaseRequestStatus Status,long Version);
public sealed record PurchaseOrderItemView(Guid Id,Guid ProductVariantId,decimal OrderedQuantity,decimal ReceivedQuantity,decimal UnitPurchasePrice);
public sealed record PurchaseOrderView(Guid Id,string PurchaseOrderNumber,Guid SupplierId,Guid WarehouseId,string CurrencyCode,PurchaseOrderStatus Status,long Version,IReadOnlyList<PurchaseOrderItemView> Items);
public interface IInventoryAdministration
{
 Task<Result<IReadOnlyList<StockBalanceView>>> BalancesAsync(Guid?warehouseId,Guid?variantId,CancellationToken cancellationToken);Task<Result<IReadOnlyList<StockTransactionView>>> TransactionsAsync(Guid?warehouseId,Guid?variantId,CancellationToken cancellationToken);Task<Result<StockTransactionView>> ReceiveAsync(StockMovementCommand command,CancellationToken cancellationToken);Task<Result<StockTransactionView>> IssueAsync(StockMovementCommand command,StockTransactionType type,CancellationToken cancellationToken);Task<Result<StockTransactionView>> AdjustAsync(StockMovementCommand command,bool increase,CancellationToken cancellationToken);Task<Result<ReserveStockResult>> ReserveAsync(ReserveStockCommand command,CancellationToken cancellationToken);Task<Result<ReservationView>> ReleaseAsync(Guid id,ReleaseReservationCommand command,CancellationToken cancellationToken);Task<Result<ReservationView>> IssueReservedAsync(Guid id,IssueReservationCommand command,CancellationToken cancellationToken);Task<Result<int>> ExpireAsync(CancellationToken cancellationToken);Task<Result<TransferView>> TransferAsync(TransferCommand command,CancellationToken cancellationToken);Task<Result<TransferView>> CompleteTransferAsync(Guid id,CompleteTransferCommand command,CancellationToken cancellationToken);
}
public interface IPurchasingAdministration
{
 Task<Result<PurchaseRequestView>> CreateRequestAsync(CreatePurchaseRequestCommand command,CancellationToken cancellationToken);Task<Result<PurchaseRequestView>> SubmitRequestAsync(Guid id,long version,CancellationToken cancellationToken);Task<Result<PurchaseOrderView>> CreateOrderAsync(CreatePurchaseOrderCommand command,CancellationToken cancellationToken);Task<Result<PurchaseOrderView>> SubmitOrderAsync(Guid id,long version,CancellationToken cancellationToken);Task<Result<PurchaseOrderView>> ReceiveAsync(Guid id,PurchaseReceiptCommand command,CancellationToken cancellationToken);Task<Result<PurchaseOrderView>> CloseAsync(Guid id,ClosePurchaseOrderCommand command,CancellationToken cancellationToken);Task<Result<IReadOnlyList<PurchaseOrderView>>> ListOrdersAsync(CancellationToken cancellationToken);
}
