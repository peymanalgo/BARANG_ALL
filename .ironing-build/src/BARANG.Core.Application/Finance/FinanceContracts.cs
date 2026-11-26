using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Finance;

namespace BARANG.Core.Application.Finance;

public sealed record RegisterPaymentCommand(string PaymentMethodCode,decimal Amount,string CurrencyCode,string? ReferenceNumber,DateTimeOffset PaidAtUtc);
public sealed record ReversePaymentCommand(decimal Amount,string Reason,string? ReferenceNumber);
public sealed record FinanceDecisionCommand(FinancialStatus TargetStatus,string? Reason,long Version);
public sealed record PaymentView(Guid Id,Guid OrderId,PaymentRecordType RecordType,string PaymentMethodCode,decimal Amount,string CurrencyCode,string? ReferenceNumber,DateTimeOffset PaidAtUtc,Guid? OriginalPaymentId,string? ReversalReason,long Version);
public sealed record FinanceBalanceView(Guid OrderId,decimal OrderTotal,decimal Paid,decimal Reversed,decimal NetPaid,decimal Remaining,decimal Overpaid,string CurrencyCode);
public sealed record FinancialStateView(Guid Id,Guid OrderId,FinancialStatus Status,string? Reason,Guid? DecidedByUserId,DateTimeOffset? DecidedAtUtc,long Version,Guid? ApprovedPriceSnapshotId);
public sealed record InvoiceItemView(int LineNumber,string ProductCode,string ProductName,string VariantSku,decimal Quantity,decimal Subtotal,decimal Discount,decimal Tax,decimal FinalTotal);
public sealed record InvoiceView(Guid Id,Guid OrderId,string InvoiceNumber,InvoiceType InvoiceType,InvoiceStatus Status,DateTimeOffset IssuedAtUtc,string OrderNumber,string CustomerCode,string CustomerName,string CurrencyCode,decimal Subtotal,decimal DiscountTotal,decimal TaxTotal,decimal FinalTotal,IReadOnlyList<InvoiceItemView> Items);
public sealed record FinanceHistoryView(Guid Id,string EventType,DateTimeOffset OccurredAtUtc,Guid ActorUserId,Guid? RelatedEntityId,string? Summary);
public interface IFinanceAdministration
{
    Task<Result<IReadOnlyList<PaymentView>>> ListPaymentsAsync(Guid orderId,CancellationToken cancellationToken);
    Task<Result<PaymentView>> RegisterPaymentAsync(Guid orderId,RegisterPaymentCommand command,CancellationToken cancellationToken);
    Task<Result<PaymentView>> ReversePaymentAsync(Guid orderId,Guid paymentId,ReversePaymentCommand command,CancellationToken cancellationToken);
    Task<Result<FinanceBalanceView>> GetBalanceAsync(Guid orderId,CancellationToken cancellationToken);
    Task<Result<FinancialStateView>> GetFinancialStateAsync(Guid orderId,CancellationToken cancellationToken);
    Task<Result<FinancialStateView>> DecideAsync(Guid orderId,FinanceDecisionCommand command,CancellationToken cancellationToken);
    Task<Result<InvoiceView>> GenerateInvoiceAsync(Guid orderId,InvoiceType type,CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<InvoiceView>>> ListInvoicesAsync(Guid orderId,CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<FinanceHistoryView>>> HistoryAsync(Guid orderId,CancellationToken cancellationToken);
}
