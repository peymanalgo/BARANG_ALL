using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Application.Auditing;

namespace BARANG.Core.Application.Customers;

public sealed partial class CustomerAdministration
{
    private sealed record OptionalReference(CustomerReferenceValue? Item)
    {
        public Guid? Id => Item?.Id;
        public string? Code => Item?.Code;
    }
    private async Task<Result<OptionalReference>> ResolveReference(string type, Guid? id, string? code, Guid? previousId, string? previousCode, CancellationToken ct)
    {
        if (id is null && string.IsNullOrWhiteSpace(code)) return Result.Success(new OptionalReference(null));
        CustomerReferenceValue? value = await store.FindReferenceAsync(type, id, code?.Trim().ToLowerInvariant(), ct);
        if (value is null || (id is not null && !string.IsNullOrWhiteSpace(code) && !string.Equals(value.Code, code.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<OptionalReference>(CustomerErrors.InvalidReference);
        bool unchanged = previousId == value.Id || (previousId is null && string.Equals(previousCode, value.Code, StringComparison.OrdinalIgnoreCase));
        return !value.IsActive && !unchanged ? Result.Failure<OptionalReference>(CustomerErrors.InvalidReference) : Result.Success(new OptionalReference(value));
    }
    private async Task<Result<CustomerDataCommand>> ResolveRoot(CustomerDataCommand command, Customer? previous, LegalCustomerProfile? legal, CancellationToken ct)
    {
        var CustomerGroup=await ResolveReference("customer.group",command.CustomerGroupId,command.CustomerGroupCode,previous?.CustomerGroupId,previous?.CustomerGroupCode,ct);
        if(CustomerGroup.IsFailure)return Result.Failure<CustomerDataCommand>(CustomerGroup.Error);
        var PriceGroup=await ResolveReference("price.group",command.PriceGroupId,command.PriceGroupCode,previous?.PriceGroupId,previous?.PriceGroupCode,ct);
        if(PriceGroup.IsFailure)return Result.Failure<CustomerDataCommand>(PriceGroup.Error);
        var LegalEntityType=await ResolveReference("legal.entity.type",command.LegalEntityTypeId,command.LegalEntityTypeCode,legal?.LegalEntityTypeId,legal?.LegalEntityTypeCode,ct);
        if(LegalEntityType.IsFailure)return Result.Failure<CustomerDataCommand>(LegalEntityType.Error);
        return Result.Success(command with { CustomerGroupId=CustomerGroup.Value?.Id, CustomerGroupCode=CustomerGroup.Value?.Code, PriceGroupId=PriceGroup.Value?.Id, PriceGroupCode=PriceGroup.Value?.Code, LegalEntityTypeId=LegalEntityType.Value?.Id, LegalEntityTypeCode=LegalEntityType.Value?.Code });
    }
    private async Task<Result<CustomerUpdateCommand>> ResolveRoot(CustomerUpdateCommand command, Customer previous, LegalCustomerProfile? legal, CancellationToken ct)
    {
        var CustomerGroup=await ResolveReference("customer.group",command.CustomerGroupId,command.CustomerGroupCode,previous.CustomerGroupId,previous.CustomerGroupCode,ct);
        if(CustomerGroup.IsFailure)return Result.Failure<CustomerUpdateCommand>(CustomerGroup.Error);
        var PriceGroup=await ResolveReference("price.group",command.PriceGroupId,command.PriceGroupCode,previous.PriceGroupId,previous.PriceGroupCode,ct);
        if(PriceGroup.IsFailure)return Result.Failure<CustomerUpdateCommand>(PriceGroup.Error);
        var LegalEntityType=await ResolveReference("legal.entity.type",command.LegalEntityTypeId,command.LegalEntityTypeCode,legal?.LegalEntityTypeId,legal?.LegalEntityTypeCode,ct);
        if(LegalEntityType.IsFailure)return Result.Failure<CustomerUpdateCommand>(LegalEntityType.Error);
        return Result.Success(command with { CustomerGroupId=CustomerGroup.Value?.Id, CustomerGroupCode=CustomerGroup.Value?.Code, PriceGroupId=PriceGroup.Value?.Id, PriceGroupCode=PriceGroup.Value?.Code, LegalEntityTypeId=LegalEntityType.Value?.Id, LegalEntityTypeCode=LegalEntityType.Value?.Code });
    }
    private async Task<Result<AddressCommand>> ResolveAddress(AddressCommand command, CustomerAddress? previous, CancellationToken ct)
    {
        var AddressType=await ResolveReference("address.type",command.AddressTypeId,command.AddressTypeCode,previous?.AddressTypeId,previous?.AddressTypeCode,ct);
        if(AddressType.IsFailure)return Result.Failure<AddressCommand>(AddressType.Error);
        return Result.Success(command with { AddressTypeId=AddressType.Value?.Id, AddressTypeCode=AddressType.Value?.Code ?? "" });
    }
    private async Task<Result<FinancialProfileCommand>> ResolveFinancial(FinancialProfileCommand command, CustomerFinancialProfile? previous, CancellationToken ct)
    {
        var PaymentTerm=await ResolveReference("payment.term",command.PaymentTermId,command.PaymentTermCode,previous?.PaymentTermId,previous?.PaymentTermCode,ct);
        if(PaymentTerm.IsFailure)return Result.Failure<FinancialProfileCommand>(PaymentTerm.Error);
        var PaymentCondition=await ResolveReference("payment.condition",command.PaymentConditionId,command.PaymentConditionCode,previous?.PaymentConditionId,previous?.PaymentConditionCode,ct);
        if(PaymentCondition.IsFailure)return Result.Failure<FinancialProfileCommand>(PaymentCondition.Error);
        var PreferredPaymentMethod=await ResolveReference("payment.method",command.PreferredPaymentMethodId,command.PreferredPaymentMethodCode,previous?.PreferredPaymentMethodId,previous?.PreferredPaymentMethodCode,ct);
        if(PreferredPaymentMethod.IsFailure)return Result.Failure<FinancialProfileCommand>(PreferredPaymentMethod.Error);
        var TaxStatus=await ResolveReference("tax.status",command.TaxStatusId,command.TaxStatusCode,previous?.TaxStatusId,previous?.TaxStatusCode,ct);
        if(TaxStatus.IsFailure)return Result.Failure<FinancialProfileCommand>(TaxStatus.Error);
        return Result.Success(command with { PaymentTermId=PaymentTerm.Value?.Id, PaymentTermCode=PaymentTerm.Value?.Code, PaymentConditionId=PaymentCondition.Value?.Id, PaymentConditionCode=PaymentCondition.Value?.Code, PreferredPaymentMethodId=PreferredPaymentMethod.Value?.Id, PreferredPaymentMethodCode=PreferredPaymentMethod.Value?.Code, TaxStatusId=TaxStatus.Value?.Id, TaxStatusCode=TaxStatus.Value?.Code });
    }
    private async Task<Result<BankAccountCommand>> ResolveBank(BankAccountCommand command, CustomerBankAccount? previous, CancellationToken ct)
    {
        var Bank=await ResolveReference("bank",command.BankId,command.BankCode,previous?.BankId,previous?.BankCode,ct);
        if(Bank.IsFailure)return Result.Failure<BankAccountCommand>(Bank.Error);
        var AccountType=await ResolveReference("bank.account.type",command.AccountTypeId,command.AccountTypeCode,previous?.AccountTypeId,previous?.AccountTypeCode,ct);
        if(AccountType.IsFailure)return Result.Failure<BankAccountCommand>(AccountType.Error);
        return Result.Success(command with { BankId=Bank.Value?.Id, BankCode=Bank.Value?.Code, AccountTypeId=AccountType.Value?.Id, AccountTypeCode=AccountType.Value?.Code });
    }

    public Task<Result<CustomerDefaultAddressResolution>> ResolveDefaultAddressAsync(Guid customerId,CancellationToken ct) => Read(async token =>
    {
        if (await store.FindAsync(customerId,token) is null) return Result.Failure<CustomerDefaultAddressResolution>(CustomerErrors.NotFound);
        var address=(await store.ListAddressesAsync(customerId,true,token)).SingleOrDefault(x=>x.IsDefault);
        return Result.Success(new CustomerDefaultAddressResolution(address is null ? null : AddressView(address)));
    },ct);

    public Task<Result> DeleteAddressAsync(Guid customerId,Guid id,long version,CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Customer? owner=await Owner(customerId,token);
        if(owner is null)return Result.Failure(CustomerErrors.NotFound);
        CustomerAddress? value=await store.FindAddressAsync(customerId,id,token);
        if(value is null)return Result.Failure(CustomerErrors.NotFound);
        if(value.Version!=version)return Result.Failure(CustomerErrors.Concurrency);
        store.Remove(value);
        Write("customer.address.deleted","customer_address",id,AuditChangeKind.Archived,new Dictionary<string,object?>{{"customerId",customerId}});
        return Result.Success();
    },ct);

    public Task<Result> DeleteContactAsync(Guid customerId,Guid id,long version,CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Customer? owner=await Owner(customerId,token);
        if(owner is null)return Result.Failure(CustomerErrors.NotFound);
        if(owner.CustomerType!=CustomerType.LegalEntity)return Result.Failure(CustomerErrors.LegalContactRequired);
        CustomerContact? value=await store.FindContactAsync(customerId,id,token);
        if(value is null)return Result.Failure(CustomerErrors.NotFound);
        if(value.Version!=version)return Result.Failure(CustomerErrors.Concurrency);
        store.Remove(value);
        Write("customer.contact.deleted","customer_contact",id,AuditChangeKind.Archived,new Dictionary<string,object?>{{"customerId",customerId}});
        return Result.Success();
    },ct);

    public Task<Result> DeleteBankAccountAsync(Guid customerId,Guid id,long version,CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Customer? owner=await Owner(customerId,token);
        if(owner is null)return Result.Failure(CustomerErrors.NotFound);
        CustomerBankAccount? value=await store.FindBankAccountAsync(customerId,id,token);
        if(value is null)return Result.Failure(CustomerErrors.NotFound);
        if(value.Version!=version)return Result.Failure(CustomerErrors.Concurrency);
        store.Remove(value);
        Write("customer.bank_account.deleted","customer_bank_account",id,AuditChangeKind.Archived,new Dictionary<string,object?>{{"customerId",customerId}});
        return Result.Success();
    },ct);
}
