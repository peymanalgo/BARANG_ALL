using System.Text.Json.Serialization;
using System.Text.Json;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))] public enum OrderStatus { Draft, Submitted, Cancelled }
[JsonConverter(typeof(JsonStringEnumConverter<OrderSource>))] public enum OrderSource { Internal, Web, Mobile, Partner }
[JsonConverter(typeof(JsonStringEnumConverter<OrderChangeRequestStatus>))] public enum OrderChangeRequestStatus { Pending, Approved, Rejected, Cancelled }

public static class OrderStatusTransitionService
{
    public static bool CanTransition(OrderStatus source, OrderStatus target) => (source, target) is (OrderStatus.Draft, OrderStatus.Submitted) or (OrderStatus.Draft, OrderStatus.Cancelled) or (OrderStatus.Submitted, OrderStatus.Cancelled);
}
public static class OrderPrintClassification
{
    public static IReadOnlySet<string> Positions { get; } = new HashSet<string>(["FRONT", "BACK", "SLEEVE", "OTHER"], StringComparer.Ordinal);
}

public static class OrderErrors
{
    public static Error InvalidData => Error.Validation("order.invalid_data"); public static Error InvalidPrintMethod => Error.Validation("order.invalid_print_method"); public static Error NotFound => Error.NotFound("order.not_found"); public static Error NotDraft => Error.BusinessRule("order.not_draft"); public static Error InvalidTransition => Error.BusinessRule("order.invalid_status_transition"); public static Error Incomplete => Error.Validation("order.incomplete"); public static Error ItemRequired => Error.Validation("order.item_required"); public static Error InvalidCustomer => Error.BusinessRule("order.invalid_customer"); public static Error InvalidProduct => Error.BusinessRule("order.invalid_product"); public static Error InvalidVariant => Error.BusinessRule("order.invalid_variant"); public static Error InvalidArtworkVersion => Error.BusinessRule("order.invalid_artwork_version"); public static Error InvalidFileVersion => Error.BusinessRule("order.invalid_file_version"); public static Error InvalidShippingAddress => Error.Validation("order.invalid_shipping_address"); public static Error DuplicateExternalReference => Error.Conflict("order.duplicate_external_reference"); public static Error AlreadySubmitted => Error.Conflict("order.already_submitted"); public static Error CancellationReasonRequired => Error.Validation("order.cancellation_reason_required"); public static Error Concurrency => Error.Concurrency("order.concurrency_conflict");
}

public static class OrderText
{
    public static string? Code(string? value, int max = 50, bool optional = false) { string v = value?.Trim().ToUpperInvariant() ?? ""; return v.Length == 0 && optional ? null : v.Length is >= 1 && v.Length <= max && v.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '/') ? v : null; }
    public static string? Text(string? value, int max, bool optional = true) { string v = value?.Trim() ?? ""; return v.Length == 0 && optional ? null : v.Length is >= 1 && v.Length <= max ? v : null; }
}

public sealed class Order
{
    private Order() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? OrderNumber { get; private set; }
    public OrderSource Source { get; private set; }
    public string? ExternalReference { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? RequestedDeliveryDate { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public string? CancellationReasonText { get; private set; }
    public Guid? ShippingAddressSnapshotId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<Order> CreateDraft(Guid id, Guid company, Guid branch, Guid customer, OrderSource source, string? externalReference, DateOnly orderDate, DateOnly? delivery, Guid actor, DateTimeOffset now) { string? ext = OrderText.Text(externalReference, 150); if (id == Guid.Empty || company == Guid.Empty || branch == Guid.Empty || customer == Guid.Empty || actor == Guid.Empty || !Enum.IsDefined(source) || (!string.IsNullOrWhiteSpace(externalReference) && ext is null) || delivery < orderDate) return Result.Failure<Order>(OrderErrors.InvalidData); return Result.Success(new Order { Id = id, CompanyId = company, BranchId = branch, CustomerId = customer, Source = source, ExternalReference = ext, Status = OrderStatus.Draft, OrderDate = orderDate, RequestedDeliveryDate = delivery, CreatedByUserId = actor, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); }
    public Result EditDraft(Guid branch, Guid customer, DateOnly orderDate, DateOnly? delivery, string? externalReference, DateTimeOffset now) { if (Status != OrderStatus.Draft) return Result.Failure(OrderErrors.NotDraft); string? ext = OrderText.Text(externalReference, 150); if (branch == Guid.Empty || customer == Guid.Empty || (!string.IsNullOrWhiteSpace(externalReference) && ext is null) || delivery < orderDate) return Result.Failure(OrderErrors.InvalidData); BranchId = branch; CustomerId = customer; OrderDate = orderDate; RequestedDeliveryDate = delivery; ExternalReference = ext; Touch(now); return Result.Success(); }
    public Result AttachShipping(Guid id, DateTimeOffset now) { if (Status != OrderStatus.Draft) return Result.Failure(OrderErrors.NotDraft); ShippingAddressSnapshotId = id; Touch(now); return Result.Success(); }
    public Result Submit(string number, DateTimeOffset now) { if (!OrderStatusTransitionService.CanTransition(Status, OrderStatus.Submitted)) return Result.Failure(OrderErrors.InvalidTransition); if (OrderText.Code(number, 40) is null) return Result.Failure(OrderErrors.InvalidData); OrderNumber = number; Status = OrderStatus.Submitted; SubmittedAtUtc = now; Touch(now); return Result.Success(); }
    public Result Cancel(string reasonCode, string? reasonText, Guid actor, DateTimeOffset now) { if (!OrderStatusTransitionService.CanTransition(Status, OrderStatus.Cancelled)) return Result.Failure(OrderErrors.InvalidTransition); string? code = OrderText.Code(reasonCode, 50), text = OrderText.Text(reasonText, 500); if (code is null) return Result.Failure(OrderErrors.CancellationReasonRequired); Status = OrderStatus.Cancelled; CancellationReasonCode = code; CancellationReasonText = text; CancelledByUserId = actor; CancelledAtUtc = now; Touch(now); return Result.Success(); }
    public Result MarkPricingRecalculated(DateTimeOffset now) { if (Status != OrderStatus.Draft) return Result.Failure(OrderErrors.NotDraft); Touch(now); return Result.Success(); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class OrderItem
{
    private OrderItem() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid OrderId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public string? SnapshotProductCode { get; private set; }
    public string? SnapshotProductName { get; private set; }
    public string? SnapshotVariantSku { get; private set; }
    public string? SnapshotVariantDisplayName { get; private set; }
    public string? SnapshotSizeCode { get; private set; }
    public string? SnapshotColorCode { get; private set; }
    public string? SnapshotUomCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<OrderItem> Create(Guid id, Guid company, Guid order, int line, Guid product, Guid variant, decimal quantity, string? notes, DateTimeOffset now) { string? n = OrderText.Text(notes, 1000); if (id == Guid.Empty || company == Guid.Empty || order == Guid.Empty || product == Guid.Empty || variant == Guid.Empty || line < 1 || quantity <= 0 || (!string.IsNullOrWhiteSpace(notes) && n is null)) return Result.Failure<OrderItem>(OrderErrors.InvalidData); return Result.Success(new OrderItem { Id = id, CompanyId = company, OrderId = order, LineNumber = line, ProductId = product, ProductVariantId = variant, Quantity = quantity, Notes = n, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); }
    public Result Update(decimal quantity, string? notes, DateTimeOffset now) { string? n = OrderText.Text(notes, 1000); if (!IsActive || quantity <= 0 || (!string.IsNullOrWhiteSpace(notes) && n is null)) return Result.Failure(OrderErrors.InvalidData); Quantity = quantity; Notes = n; Touch(now); return Result.Success(); }
    public void Remove(DateTimeOffset now) { if (IsActive) { IsActive = false; Touch(now); } }
    public void Freeze(string productCode, string productName, string sku, string? variantName, string? size, string? color, string uom, DateTimeOffset now) { SnapshotProductCode = productCode; SnapshotProductName = productName; SnapshotVariantSku = sku; SnapshotVariantDisplayName = variantName; SnapshotSizeCode = size; SnapshotColorCode = color; SnapshotUomCode = uom; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class OrderPrintOperation
{
    private OrderPrintOperation() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public int Sequence { get; private set; }
    public string PrintPositionCode { get; private set; } = ""; public string? PrintMethodCode { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public Guid? ArtworkVersionId { get; private set; }
    public Guid? FileVersionId { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<OrderPrintOperation> Create(Guid id, Guid company, Guid order, Guid item, int sequence, string position, string? method, decimal? width, decimal? height, Guid? artworkVersion, Guid? fileVersion, string? notes, DateTimeOffset now) { OrderPrintOperation x = new() { Id = id, CompanyId = company, OrderId = order, OrderItemId = item, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1, IsActive = true }; Result r = x.Apply(sequence, position, method, width, height, artworkVersion, fileVersion, notes, now, false); return r.IsFailure ? Result.Failure<OrderPrintOperation>(r.Error) : Result.Success(x); }
    public Result Update(int sequence, string position, string? method, decimal? width, decimal? height, Guid? artworkVersion, Guid? fileVersion, string? notes, DateTimeOffset now) => Apply(sequence, position, method, width, height, artworkVersion, fileVersion, notes, now, true); public void Remove(DateTimeOffset now) { if (IsActive) { IsActive = false; Touch(now); } }
    private Result Apply(int sequence, string position, string? method, decimal? width, decimal? height, Guid? artworkVersion, Guid? fileVersion, string? notes, DateTimeOffset now, bool touch) { string? p = OrderText.Code(position, 50), m = string.IsNullOrWhiteSpace(method) ? null : OrderText.Code(method, 50), n = OrderText.Text(notes, 1000); if (sequence < 1 || p is null || !OrderPrintClassification.Positions.Contains(p) || width <= 0 || height <= 0 || (!string.IsNullOrWhiteSpace(method) && m is null) || (!string.IsNullOrWhiteSpace(notes) && n is null)) return Result.Failure(OrderErrors.InvalidData); Sequence = sequence; PrintPositionCode = p; PrintMethodCode = m; Width = width; Height = height; ArtworkVersionId = artworkVersion; FileVersionId = fileVersion; Notes = n; if (touch) Touch(now); return Result.Success(); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class OrderAddressSnapshot
{
    private OrderAddressSnapshot() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? SourceCustomerAddressId { get; private set; }
    public string RecipientName { get; private set; } = ""; public string? RecipientPhone { get; private set; }
    public string CountryCode { get; private set; } = ""; public string? Province { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string AddressLine1 { get; private set; } = ""; public string? AddressLine2 { get; private set; }
    public string? PostalCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<OrderAddressSnapshot> Create(Guid id, Guid company, Guid order, Guid? source, string recipient, string? phone, string country, string? province, string? city, string? district, string line1, string? line2, string? postal, DateTimeOffset now) { OrderAddressSnapshot x = new() { Id = id, CompanyId = company, OrderId = order, SourceCustomerAddressId = source, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }; Result r = x.Apply(recipient, phone, country, province, city, district, line1, line2, postal, now, false); return r.IsFailure ? Result.Failure<OrderAddressSnapshot>(r.Error) : Result.Success(x); }
    public Result Update(string recipient, string? phone, string country, string? province, string? city, string? district, string line1, string? line2, string? postal, DateTimeOffset now) => Apply(recipient, phone, country, province, city, district, line1, line2, postal, now, true);
    private Result Apply(string recipient, string? phone, string country, string? province, string? city, string? district, string line1, string? line2, string? postal, DateTimeOffset now, bool touch) { string? r = OrderText.Text(recipient, 200, false), p = OrderText.Text(phone, 32), pr = OrderText.Text(province, 150), c = OrderText.Text(city, 150), d = OrderText.Text(district, 150), l1 = OrderText.Text(line1, 500, false), l2 = OrderText.Text(line2, 500), pc = OrderText.Text(postal, 32); string cc = country.Trim().ToUpperInvariant(); if (r is null || l1 is null || cc.Length != 2 || !cc.All(char.IsAsciiLetter)) return Result.Failure(OrderErrors.InvalidShippingAddress); RecipientName = r; RecipientPhone = p; CountryCode = cc; Province = pr; City = c; District = d; AddressLine1 = l1; AddressLine2 = l2; PostalCode = pc; if (touch) { UpdatedAtUtc = now; Version++; } return Result.Success(); }
}

public sealed class OrderTimelineEvent { private OrderTimelineEvent() { } public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public string EventType { get; private set; } = ""; public DateTimeOffset OccurredAtUtc { get; private set; } public Guid ActorUserId { get; private set; } public string? Summary { get; private set; } public Guid? RelatedEntityId { get; private set; } public static OrderTimelineEvent Create(Guid company, Guid order, string type, Guid actor, DateTimeOffset now, string? summary = null, Guid? related = null) => new() { Id = Guid.NewGuid(), CompanyId = company, OrderId = order, EventType = type, ActorUserId = actor, OccurredAtUtc = now, Summary = OrderText.Text(summary, 500), RelatedEntityId = related }; }
public sealed class OrderChangeRequest
{
    private static readonly HashSet<string> AllowedTypes = new(["SHIPPING_ADDRESS", "CUSTOMER_REFERENCE", "ITEM_SPECIFICATION", "PRINT_SPECIFICATION", "REQUESTED_DELIVERY_DATE"], StringComparer.Ordinal);
    private OrderChangeRequest() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public Guid RequestedByUserId { get; private set; } public DateTimeOffset RequestedAtUtc { get; private set; } public string ChangeType { get; private set; } = ""; public string Reason { get; private set; } = ""; public OrderChangeRequestStatus Status { get; private set; } public string RequestedChangesJson { get; private set; } = "{}"; public Guid? ReviewedByUserId { get; private set; } public DateTimeOffset? ReviewedAtUtc { get; private set; } public string? ResolutionReason { get; private set; } public long Version { get; private set; }
    public static Result<OrderChangeRequest> Create(Guid company, Guid order, Guid actor, string type, string reason, string json, DateTimeOffset now)
    {
        string? t = OrderText.Code(type, 50), r = OrderText.Text(reason, 1000, false);
        if (t is null || !AllowedTypes.Contains(t) || r is null || json.Length > 8000 || !ValidObject(json)) return Result.Failure<OrderChangeRequest>(OrderErrors.InvalidData);
        return Result.Success(new OrderChangeRequest { Id = Guid.NewGuid(), CompanyId = company, OrderId = order, RequestedByUserId = actor, RequestedAtUtc = now, ChangeType = t, Reason = r, RequestedChangesJson = json, Status = OrderChangeRequestStatus.Pending, Version = 1 });
    }
    private static bool ValidObject(string json) { try { using JsonDocument d = JsonDocument.Parse(json); return d.RootElement.ValueKind == JsonValueKind.Object; } catch (JsonException) { return false; } }
}
