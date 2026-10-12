using BARANG.Core.Domain.Common;
using System.Text.Json.Serialization;

namespace BARANG.Core.Domain.Customers;

[JsonConverter(typeof(JsonStringEnumConverter<BankAccountOwnership>))]
public enum BankAccountOwnership { Individual, Joint }

public sealed partial class Customer
{
    public Guid? CustomerGroupId { get; private set; }
    public Guid? PriceGroupId { get; private set; }
    public void AssignReferences(Guid? customerGroupId, Guid? priceGroupId) { CustomerGroupId = customerGroupId; PriceGroupId = priceGroupId; }
}
public sealed partial class LegalCustomerProfile
{
    public Guid? LegalEntityTypeId { get; private set; }
    public void AssignReference(Guid? legalEntityTypeId) => LegalEntityTypeId = legalEntityTypeId;
}
public sealed partial class CustomerFinancialProfile
{
    public Guid? PaymentTermId { get; private set; }
    public Guid? PaymentConditionId { get; private set; }
    public Guid? PreferredPaymentMethodId { get; private set; }
    public Guid? TaxStatusId { get; private set; }
    public void AssignReferences(Guid? term, Guid? condition, Guid? method, Guid? tax)
    { PaymentTermId = term; PaymentConditionId = condition; PreferredPaymentMethodId = method; TaxStatusId = tax; }
}
public sealed partial class CustomerAddress
{
    public Guid? AddressTypeId { get; private set; }
    public string? Email { get; private set; }
    public string? Notes { get; private set; }
    public Result AssignDetails(Guid? addressTypeId, string? email, string? notes)
    {
        string? normalizedEmail = CustomerText.Email(email);
        if (normalizedEmail == "" || notes?.Length > 1000) return Result.Failure(CustomerErrors.InvalidData);
        AddressTypeId = addressTypeId; Email = normalizedEmail; Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        return Result.Success();
    }
}
public sealed partial class CustomerContact
{
    public Guid? ContactRoleId { get; private set; }
    public void AssignReference(Guid? contactRoleId) => ContactRoleId = contactRoleId;
}
public sealed partial class CustomerBankAccount
{
    public Guid? BankId { get; private set; }
    public Guid? AccountTypeId { get; private set; }
    public string? AccountTypeCode { get; private set; }
    public string? AccountName { get; private set; }
    public BankAccountOwnership OwnershipType { get; private set; }
    public string? CardNumber { get; private set; }
    public string? BranchCode { get; private set; }
    public Result AssignDetails(Guid? bankId, Guid? accountTypeId, string? accountTypeCode, string? accountName, BankAccountOwnership ownershipType, string? cardNumber, string? branchCode)
    {
        string card = (cardNumber ?? "").Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (!Enum.IsDefined(ownershipType) || accountName?.Length > 200 || branchCode?.Length > 50 || card.Length > 19 || card.Any(c => !char.IsAsciiDigit(c))) return Result.Failure(CustomerErrors.InvalidData);
        BankId = bankId; AccountTypeId = accountTypeId; AccountTypeCode = accountTypeCode;
        AccountName = string.IsNullOrWhiteSpace(accountName) ? null : accountName.Trim(); OwnershipType = ownershipType;
        CardNumber = card.Length == 0 ? null : card; BranchCode = string.IsNullOrWhiteSpace(branchCode) ? null : branchCode.Trim();
        return Result.Success();
    }
}
