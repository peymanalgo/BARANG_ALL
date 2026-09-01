using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Pricing;

namespace BARANG.Core.Domain.Finance;

public enum PaymentRecordType { Payment, Reversal }
public enum PaymentStatus { Completed }
public enum FinancialStatus { Pending, Approved, Rejected, Suspended }
public enum InvoiceType { Proforma, Invoice }
public enum InvoiceStatus { Issued }

public static class FinanceErrors
{
    public static Error InvalidData => Error.Validation("finance.invalid_data");
    public static Error NotFound => Error.NotFound("finance.resource.not_found");
    public static Error OrderNotEligible => Error.BusinessRule("finance.order.not_eligible");
    public static Error PricingRequired => Error.BusinessRule("finance.pricing.required");
    public static Error CurrencyMismatch => Error.Validation("finance.currency_mismatch");
    public static Error PaymentMethodInactive => Error.BusinessRule("finance.payment_method.inactive");
    public static Error DuplicateReference => Error.Conflict("finance.payment.duplicate_reference");
    public static Error AlreadyReversed => Error.Conflict("finance.payment.already_reversed");
    public static Error InvalidTransition => Error.BusinessRule("finance.invalid_status_transition");
    public static Error ReasonRequired => Error.Validation("finance.reason_required");
    public static Error Concurrency => Error.Concurrency("finance.concurrency_conflict");
}

public sealed class Payment
{
    private Payment() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public PaymentRecordType RecordType { get; private set; } public string PaymentMethodCode { get; private set; } = string.Empty; public decimal Amount { get; private set; } public string CurrencyCode { get; private set; } = string.Empty; public string? ReferenceNumber { get; private set; } public DateTimeOffset PaidAtUtc { get; private set; } public Guid RegisteredByUserId { get; private set; } public PaymentStatus Status { get; private set; } public Guid? OriginalPaymentId { get; private set; } public string? ReversalReason { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<Payment> Register(Guid company,Guid order,string method,decimal amount,string currency,string? reference,DateTimeOffset paidAt,Guid actor,DateTimeOffset now)
    {
        string m=Normalize(method);string c=(currency??string.Empty).Trim().ToUpperInvariant();string? r=Text(reference,150);if(company==Guid.Empty||order==Guid.Empty||actor==Guid.Empty||m.Length is < 2 or > 100||amount<=0||c.Length!=3||!c.All(char.IsAsciiLetter)||(!string.IsNullOrWhiteSpace(reference)&&r is null))return Result.Failure<Payment>(FinanceErrors.InvalidData);return Result.Success(new Payment{Id=Guid.NewGuid(),CompanyId=company,OrderId=order,RecordType=PaymentRecordType.Payment,PaymentMethodCode=m,Amount=Money.Round(amount),CurrencyCode=c,ReferenceNumber=r,PaidAtUtc=paidAt,RegisteredByUserId=actor,Status=PaymentStatus.Completed,CreatedAtUtc=now,Version=1});
    }
    public static Result<Payment> Reverse(Payment original,decimal amount,string reason,string? reference,Guid actor,DateTimeOffset now)
    {
        string? why=Text(reason,500);string? r=Text(reference,150);if(original.RecordType!=PaymentRecordType.Payment||amount<=0||amount>original.Amount||why is null||actor==Guid.Empty)return Result.Failure<Payment>(FinanceErrors.InvalidData);return Result.Success(new Payment{Id=Guid.NewGuid(),CompanyId=original.CompanyId,OrderId=original.OrderId,RecordType=PaymentRecordType.Reversal,PaymentMethodCode=original.PaymentMethodCode,Amount=Money.Round(amount),CurrencyCode=original.CurrencyCode,ReferenceNumber=r,PaidAtUtc=now,RegisteredByUserId=actor,Status=PaymentStatus.Completed,OriginalPaymentId=original.Id,ReversalReason=why,CreatedAtUtc=now,Version=1});
    }
    public static string Normalize(string? value)=>(value??string.Empty).Trim().ToLowerInvariant();internal static string? Text(string? value,int max){if(string.IsNullOrWhiteSpace(value))return null;string x=value.Trim();return x.Length<=max?x:null;}
}

public static class FinancialStatusTransitionService
{
    public static bool CanTransition(FinancialStatus source,FinancialStatus target)=>(source,target) is (FinancialStatus.Pending,FinancialStatus.Approved) or (FinancialStatus.Pending,FinancialStatus.Rejected) or (FinancialStatus.Pending,FinancialStatus.Suspended) or (FinancialStatus.Approved,FinancialStatus.Suspended) or (FinancialStatus.Suspended,FinancialStatus.Approved) or (FinancialStatus.Suspended,FinancialStatus.Rejected) or (FinancialStatus.Rejected,FinancialStatus.Pending);
}

public sealed class OrderFinancialState
{
    private OrderFinancialState() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public FinancialStatus Status { get; private set; } public string? Reason { get; private set; } public Guid? DecidedByUserId { get; private set; } public DateTimeOffset? DecidedAtUtc { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static OrderFinancialState Create(Guid company,Guid order,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,OrderId=order,Status=FinancialStatus.Pending,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1};
    public Result Transition(FinancialStatus target,string? reason,Guid actor,long expectedVersion,DateTimeOffset now){if(expectedVersion!=Version)return Result.Failure(FinanceErrors.Concurrency);if(!FinancialStatusTransitionService.CanTransition(Status,target))return Result.Failure(FinanceErrors.InvalidTransition);string? why=Payment.Text(reason,500);if(target is FinancialStatus.Rejected or FinancialStatus.Suspended&&why is null)return Result.Failure(FinanceErrors.ReasonRequired);Status=target;Reason=why;DecidedByUserId=actor;DecidedAtUtc=now;UpdatedAtUtc=now;Version++;return Result.Success();}
}

public sealed class Invoice
{
    private Invoice() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public Guid PriceSnapshotId { get; private set; } public string InvoiceNumber { get; private set; }=string.Empty; public InvoiceType InvoiceType { get; private set; } public InvoiceStatus Status { get; private set; } public DateTimeOffset IssuedAtUtc { get; private set; } public Guid CreatedByUserId { get; private set; } public long Version { get; private set; }
    public static Invoice Create(Guid company,Guid order,Guid priceSnapshot,string number,InvoiceType type,Guid actor,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,OrderId=order,PriceSnapshotId=priceSnapshot,InvoiceNumber=number,InvoiceType=type,Status=InvoiceStatus.Issued,IssuedAtUtc=now,CreatedByUserId=actor,Version=1};
}

public sealed class InvoiceSnapshot
{
    private InvoiceSnapshot() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid InvoiceId { get; private set; } public string OrderNumber { get; private set; }=string.Empty; public string CustomerCode { get; private set; }=string.Empty; public string CustomerName { get; private set; }=string.Empty; public string CurrencyCode { get; private set; }=string.Empty; public decimal RawProductTotal { get; private set; } public decimal PrintTotal { get; private set; } public decimal AdditionalServiceTotal { get; private set; } public decimal PackagingTotal { get; private set; } public decimal ShippingTotal { get; private set; } public decimal Subtotal { get; private set; } public decimal DiscountTotal { get; private set; } public decimal TaxTotal { get; private set; } public decimal FinalTotal { get; private set; }
    public static InvoiceSnapshot Create(Guid company,Guid invoice,string orderNumber,string customerCode,string customerName,OrderPriceSnapshot p)=>new(){Id=Guid.NewGuid(),CompanyId=company,InvoiceId=invoice,OrderNumber=orderNumber,CustomerCode=customerCode,CustomerName=customerName,CurrencyCode=p.CurrencyCode,RawProductTotal=p.RawProductTotal,PrintTotal=p.PrintTotal,AdditionalServiceTotal=p.AdditionalServiceTotal,PackagingTotal=p.PackagingTotal,ShippingTotal=p.ShippingTotal,Subtotal=p.Subtotal,DiscountTotal=p.DiscountTotal,TaxTotal=p.TaxTotal,FinalTotal=p.FinalTotal};
}

public sealed class InvoiceItemSnapshot
{
    private InvoiceItemSnapshot() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid InvoiceId { get; private set; } public Guid OrderItemId { get; private set; } public int LineNumber { get; private set; } public string ProductCode { get; private set; }=string.Empty; public string ProductName { get; private set; }=string.Empty; public string VariantSku { get; private set; }=string.Empty; public decimal Quantity { get; private set; } public decimal Subtotal { get; private set; } public decimal Discount { get; private set; } public decimal Tax { get; private set; } public decimal FinalTotal { get; private set; }
    public static InvoiceItemSnapshot Create(Guid company,Guid invoice,Guid item,int line,string productCode,string productName,string sku,decimal quantity,OrderItemPriceSnapshot p)=>new(){Id=Guid.NewGuid(),CompanyId=company,InvoiceId=invoice,OrderItemId=item,LineNumber=line,ProductCode=productCode,ProductName=productName,VariantSku=sku,Quantity=quantity,Subtotal=p.Subtotal,Discount=p.DiscountAllocated,Tax=p.TaxAmount,FinalTotal=p.FinalTotal};
}

public sealed class FinanceHistory
{
    private FinanceHistory() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public string EventType { get; private set; }=string.Empty; public DateTimeOffset OccurredAtUtc { get; private set; } public Guid ActorUserId { get; private set; } public Guid? RelatedEntityId { get; private set; } public string? Summary { get; private set; }
    public static FinanceHistory Create(Guid company,Guid order,string type,Guid actor,DateTimeOffset now,Guid? related=null,string? summary=null)=>new(){Id=Guid.NewGuid(),CompanyId=company,OrderId=order,EventType=type,ActorUserId=actor,OccurredAtUtc=now,RelatedEntityId=related,Summary=Payment.Text(summary,500)};
}
