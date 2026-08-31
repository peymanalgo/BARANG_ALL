using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Orders;

namespace BARANG.Core.Application.Orders;

public sealed record AddressCommand(Guid? SourceCustomerAddressId,string RecipientName,string? RecipientPhone,string CountryCode,string? Province,string? City,string? District,string AddressLine1,string? AddressLine2,string? PostalCode,long Version=0);
public sealed record CreateDraftCommand(Guid BranchId,Guid CustomerId,OrderSource Source,string? ExternalReference,DateOnly OrderDate,DateOnly? RequestedDeliveryDate,AddressCommand? ShippingAddress=null);
public sealed record EditDraftCommand(Guid BranchId,Guid CustomerId,string? ExternalReference,DateOnly OrderDate,DateOnly? RequestedDeliveryDate,long Version);
public sealed record ItemCommand(Guid ProductId,Guid ProductVariantId,decimal Quantity,string? Notes,long Version=0);
public sealed record PrintOperationCommand(int Sequence,string PrintPositionCode,string? PrintMethodCode,decimal? Width,decimal? Height,Guid? ArtworkVersionId,Guid? FileVersionId,string? Notes,long Version=0);
public sealed record CancelOrderCommand(string ReasonCode,string? ReasonText,long Version);
public sealed record ChangeRequestCommand(string ChangeType,string Reason,string RequestedChangesJson);
public sealed record OrderSearchQuery(string? OrderNumber,Guid? CustomerId,string? CustomerSearch,OrderStatus? Status,Guid? BranchId,OrderSource? Source,DateOnly? OrderDateFrom,DateOnly? OrderDateTo,DateTimeOffset? SubmittedFrom,DateTimeOffset? SubmittedTo,string? ExternalReference,int Page=1,int PageSize=20);
public sealed record ProductSnapshotView(string? ProductCode,string? ProductName,string? VariantSku,string? VariantDisplayName,string? SizeCode,string? ColorCode,string? UomCode);
public sealed record OrderItemView(Guid Id,int LineNumber,Guid ProductId,Guid ProductVariantId,decimal Quantity,string? Notes,bool IsActive,ProductSnapshotView Snapshot,long Version,IReadOnlyList<OrderPrintOperationView> PrintOperations);
public sealed record OrderPrintOperationView(Guid Id,Guid OrderItemId,int Sequence,string PrintPositionCode,string? PrintMethodCode,decimal? Width,decimal? Height,Guid? ArtworkVersionId,Guid? FileVersionId,string? Notes,bool IsActive,long Version);
public sealed record OrderAddressView(Guid Id,Guid? SourceCustomerAddressId,string RecipientName,string? RecipientPhone,string CountryCode,string? Province,string? City,string? District,string AddressLine1,string? AddressLine2,string? PostalCode,long Version);
public sealed record OrderView(Guid Id,Guid BranchId,Guid CustomerId,string? OrderNumber,OrderSource Source,string? ExternalReference,OrderStatus Status,DateOnly OrderDate,DateOnly? RequestedDeliveryDate,DateTimeOffset? SubmittedAtUtc,DateTimeOffset? CancelledAtUtc,string? CancellationReasonCode,string? CancellationReasonText,long Version,OrderAddressView? ShippingAddress,IReadOnlyList<OrderItemView> Items);
public sealed record OrderSummaryView(Guid Id,string? OrderNumber,Guid CustomerId,string CustomerCode,string CustomerName,Guid BranchId,OrderStatus Status,OrderSource Source,DateOnly OrderDate,DateTimeOffset CreatedAtUtc,long Version);
public sealed record OrderTimelineView(Guid Id,string EventType,DateTimeOffset OccurredAtUtc,Guid ActorUserId,string? Summary,Guid? RelatedEntityId);
public sealed record OrderValidationIssue(string Code,string? Field=null);
public sealed record OrderChangeRequestView(Guid Id,Guid OrderId,string ChangeType,string Reason,string RequestedChangesJson,OrderChangeRequestStatus Status,long Version);
public sealed record Page<T>(IReadOnlyList<T> Items,int PageNumber,int PageSize,long TotalCount);

public interface IOrderAdministration
{
 Task<Result<OrderView>>CreateDraftAsync(CreateDraftCommand command,CancellationToken ct);Task<Result<OrderView>>GetAsync(Guid id,CancellationToken ct);Task<Result<OrderView>>EditDraftAsync(Guid id,EditDraftCommand command,CancellationToken ct);Task<Result<OrderView>>SetShippingAsync(Guid id,AddressCommand command,CancellationToken ct);
 Task<Result<OrderItemView>>AddItemAsync(Guid orderId,ItemCommand command,CancellationToken ct);Task<Result<OrderItemView>>UpdateItemAsync(Guid orderId,Guid itemId,ItemCommand command,CancellationToken ct);Task<Result>RemoveItemAsync(Guid orderId,Guid itemId,long version,CancellationToken ct);
 Task<Result<OrderPrintOperationView>>AddPrintAsync(Guid orderId,Guid itemId,PrintOperationCommand command,CancellationToken ct);Task<Result<OrderPrintOperationView>>UpdatePrintAsync(Guid orderId,Guid itemId,Guid operationId,PrintOperationCommand command,CancellationToken ct);Task<Result>RemovePrintAsync(Guid orderId,Guid itemId,Guid operationId,long version,CancellationToken ct);
 Task<Result<IReadOnlyList<OrderValidationIssue>>>ValidateAsync(Guid id,CancellationToken ct);Task<Result<OrderView>>SubmitAsync(Guid id,long version,CancellationToken ct);Task<Result<OrderView>>CancelAsync(Guid id,CancelOrderCommand command,CancellationToken ct);Task<Result<OrderChangeRequestView>>CreateChangeRequestAsync(Guid id,ChangeRequestCommand command,CancellationToken ct);Task<Result<IReadOnlyList<OrderTimelineView>>>TimelineAsync(Guid id,CancellationToken ct);Task<Result<Page<OrderSummaryView>>>SearchAsync(OrderSearchQuery query,CancellationToken ct);
}
