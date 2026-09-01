using BARANG.Core.Domain.Finance;
using Xunit;

namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707,CA1305
public sealed class FinanceFoundationTests
{
    private static readonly Guid Company=Guid.NewGuid(),Order=Guid.NewGuid(),Actor=Guid.NewGuid();private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-09-01T09:00:00Z");
    [Fact]public void Payment_requires_positive_decimal_amount(){Assert.True(Payment.Register(Company,Order,"cash",0,"IRR",null,Now,Actor,Now).IsFailure);Assert.True(Payment.Register(Company,Order,"cash",10.005m,"irr",null,Now,Actor,Now).IsSuccess);}
    [Fact]public void Payment_normalizes_method_currency_and_reference(){Payment p=Payment.Register(Company,Order," Bank_Transfer ",10,"irr"," REF-1 ",Now,Actor,Now).Value;Assert.Equal("bank_transfer",p.PaymentMethodCode);Assert.Equal("IRR",p.CurrencyCode);Assert.Equal("REF-1",p.ReferenceNumber);}
    [Fact]public void Reversal_is_new_record_and_original_is_unchanged(){Payment p=Payment.Register(Company,Order,"cash",100,"IRR",null,Now,Actor,Now).Value;Payment r=Payment.Reverse(p,40,"correction",null,Actor,Now).Value;Assert.Equal(PaymentRecordType.Payment,p.RecordType);Assert.Equal(PaymentRecordType.Reversal,r.RecordType);Assert.Equal(p.Id,r.OriginalPaymentId);Assert.Equal(100,p.Amount);}
    [Fact]public void Reversal_cannot_exceed_original(){Payment p=Payment.Register(Company,Order,"cash",100,"IRR",null,Now,Actor,Now).Value;Assert.True(Payment.Reverse(p,101,"bad",null,Actor,Now).IsFailure);}
    [Fact]public void Financial_transition_rules_are_centralized(){Assert.True(FinancialStatusTransitionService.CanTransition(FinancialStatus.Pending,FinancialStatus.Approved));Assert.False(FinancialStatusTransitionService.CanTransition(FinancialStatus.Approved,FinancialStatus.Rejected));}
    [Fact]public void Reject_and_suspend_require_reason(){OrderFinancialState s=OrderFinancialState.Create(Company,Order,Now);Assert.Equal(FinanceErrors.ReasonRequired.Code,s.Transition(FinancialStatus.Rejected,null,Actor,1,Now).Error.Code);Assert.True(s.Transition(FinancialStatus.Suspended,"review",Actor,1,Now).IsSuccess);}
    [Fact]public void Finance_state_uses_optimistic_concurrency(){OrderFinancialState s=OrderFinancialState.Create(Company,Order,Now);Assert.Equal(FinanceErrors.Concurrency.Code,s.Transition(FinancialStatus.Approved,null,Actor,2,Now).Error.Code);}
    [Fact]public void Invoice_number_and_type_are_immutable_creation_data(){Invoice i=Invoice.Create(Company,Order,Guid.NewGuid(),"INV-2026-0000000001",InvoiceType.Invoice,Actor,Now);Assert.Equal("INV-2026-0000000001",i.InvoiceNumber);Assert.Equal(InvoiceStatus.Issued,i.Status);}
}
#pragma warning restore CA1707,CA1305
