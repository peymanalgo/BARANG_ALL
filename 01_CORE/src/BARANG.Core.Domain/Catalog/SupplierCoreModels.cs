using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;

namespace BARANG.Core.Domain.Catalog;

public static class SupplierCoreErrors
{
    public static Error InvalidData => Error.Validation("supplier.invalid_data");
    public static Error InvalidReference => Error.BusinessRule("supplier.invalid_reference");
    public static Error InvalidDefault => Error.BusinessRule("supplier.inactive_default");
    public static Error Referenced => Error.BusinessRule("supplier.referenced");
    public static Error Blocked => Error.BusinessRule("supplier.blocked");
    public static Error NotFound => Error.NotFound("supplier.not_found");
    public static Error Concurrency => Error.Concurrency("supplier.concurrency_conflict");
}

public sealed record SupplierIdentityData(
    string SupplierCode = "",
    string DisplayName = "",
    string EntityType = "Individual",
    string Status = "Active",
    string? FirstName = null,
    string? LastName = null,
    string? NationalId = null,
    string? Occupation = null,
    string? LegalName = null,
    string? TradeName = null,
    string? RegistrationNo = null,
    string? TaxId = null,
    Guid? LegalEntityTypeId = null,
    DateOnly? EstablishmentDate = null,
    string? BusinessActivity = null,
    string? PrimaryMobile = null,
    string? PrimaryPhone = null,
    string? PrimaryEmail = null,
    string? Website = null,
    string? Notes = null,
    Guid? GradeId = null,
    bool IsPreferred = false,
    DateOnly? CooperationStartDate = null,
    Guid? HoldReasonId = null);

public sealed record SupplierFinancialData(
    Guid? PaymentTermId = null,
    Guid? PaymentConditionId = null,
    Guid? PaymentMethodId = null,
    string DefaultCurrencyCode = "IRT",
    decimal? MinimumOrderAmount = null,
    int? DefaultLeadTimeDays = null,
    decimal BaseDiscountPercent = 0,
    bool CreditPurchaseAllowed = false,
    decimal PurchaseCreditLimit = 0,
    Guid? ShippingMethodId = null,
    Guid? FreightDeliveryTermId = null);

public sealed record SupplierAddressData(
    string AddressName = "",
    Guid? AddressTypeId = null,
    string CountryCode = "IR",
    string? ProvinceCode = null,
    string? CityCode = null,
    string? PostalCode = null,
    string FullAddress = "",
    string? Landline = null,
    string? Mobile = null,
    string? Email = null,
    string? RecipientName = null,
    string? Notes = null,
    bool IsDefault = false,
    bool IsActive = true);

public sealed record SupplierContactData(
    string FirstName = "",
    string? LastName = null,
    Guid? ContactRoleId = null,
    string? PositionTitle = null,
    string? Landline = null,
    string? Mobile = null,
    string? Extension = null,
    string? Email = null,
    string? Notes = null,
    bool IsPrimary = false,
    bool IsActive = true);

public sealed record SupplierBankData(
    Guid? BankId = null,
    string? AccountName = null,
    Guid? AccountTypeId = null,
    string OwnershipType = "Individual",
    string AccountHolderNames = "",
    string? AccountNumber = null,
    string? Iban = null,
    string? CardNumber = null,
    string? BranchCode = null,
    string? BranchName = null,
    string CurrencyCode = "IRT",
    bool IsDefault = false,
    bool IsActive = true,
    string? Notes = null);

public static class SupplierCoreRules
{
    public static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static string? Compact(string? value) => Trim(value)?.Replace(" ", "", StringComparison.Ordinal);
    public static Result<SupplierIdentityData> Identity(SupplierIdentityData d)
    {
        if (d.SupplierCode?.Trim().Length > 50 || d.DisplayName?.Trim().Length > 200 || d.EntityType?.Trim().Length > 20 || d.Status?.Trim().Length > 20 || d.FirstName?.Trim().Length > 100 || d.LastName?.Trim().Length > 100 || d.NationalId?.Trim().Length > 64 || d.Occupation?.Trim().Length > 200 || d.LegalName?.Trim().Length > 200 || d.TradeName?.Trim().Length > 200 || d.RegistrationNo?.Trim().Length > 64 || d.TaxId?.Trim().Length > 64 || d.BusinessActivity?.Trim().Length > 500 || d.PrimaryMobile?.Trim().Length > 16 || d.PrimaryPhone?.Trim().Length > 16 || d.PrimaryEmail?.Trim().Length > 320 || d.Website?.Trim().Length > 500 || d.Notes?.Trim().Length > 2000 || d.EntityType is not ("Individual" or "LegalEntity") || d.Status is not ("Active" or "Inactive" or "Blocked"))
            return Result.Failure<SupplierIdentityData>(SupplierCoreErrors.InvalidData);
        string? code = CatalogText.Code(d.SupplierCode,50);
        if (code is null || d.Status=="Blocked" && d.HoldReasonId is null || d.EntityType=="LegalEntity" && string.IsNullOrWhiteSpace(d.LegalName))
            return Result.Failure<SupplierIdentityData>(SupplierCoreErrors.InvalidData);
        d = d with { SupplierCode = code, DisplayName = d.DisplayName?.Trim() ?? "", EntityType = d.EntityType.Trim(), Status = d.Status.Trim(), FirstName = SupplierCoreRules.Trim(d.FirstName), LastName = SupplierCoreRules.Trim(d.LastName), NationalId = SupplierCoreRules.Trim(d.NationalId), Occupation = SupplierCoreRules.Trim(d.Occupation), LegalName = SupplierCoreRules.Trim(d.LegalName), TradeName = SupplierCoreRules.Trim(d.TradeName), RegistrationNo = SupplierCoreRules.Trim(d.RegistrationNo), TaxId = SupplierCoreRules.Trim(d.TaxId), BusinessActivity = SupplierCoreRules.Trim(d.BusinessActivity), PrimaryMobile = CustomerText.Phone(d.PrimaryMobile), PrimaryPhone = CustomerText.Phone(d.PrimaryPhone), PrimaryEmail = CustomerText.Email(d.PrimaryEmail), Website = SupplierCoreRules.Trim(d.Website), Notes = SupplierCoreRules.Trim(d.Notes) };
        if (d.PrimaryMobile=="" || d.PrimaryPhone=="" || d.PrimaryEmail=="")
            return Result.Failure<SupplierIdentityData>(SupplierCoreErrors.InvalidData);
        string display = d.DisplayName.Length>0 ? d.DisplayName : d.EntityType=="LegalEntity" ? d.LegalName! : string.Join(" ",new[]{d.FirstName,d.LastName}.Where(x=>x is not null));
        if (string.IsNullOrWhiteSpace(display)) return Result.Failure<SupplierIdentityData>(SupplierCoreErrors.InvalidData);
        return Result.Success(d with { SupplierCode=code,DisplayName=display,HoldReasonId=d.Status=="Blocked"?d.HoldReasonId:null });
    }
    public static Result<SupplierFinancialData> Financial(SupplierFinancialData d)
    {
        if (CustomerText.Currency(d.DefaultCurrencyCode) is null || d.MinimumOrderAmount<0 || d.DefaultLeadTimeDays<0 || d.BaseDiscountPercent is <0 or >100 || d.PurchaseCreditLimit<0)
            return Result.Failure<SupplierFinancialData>(SupplierCoreErrors.InvalidData);
        return Result.Success(d with { DefaultCurrencyCode=CustomerText.Currency(d.DefaultCurrencyCode)! });
    }
    public static Result<SupplierAddressData> Address(SupplierAddressData d)
    {
        if (d.AddressName?.Trim().Length > 100 || d.CountryCode?.Trim().Length > 2 || d.ProvinceCode?.Trim().Length > 80 || d.CityCode?.Trim().Length > 80 || d.PostalCode?.Trim().Length > 32 || d.FullAddress?.Trim().Length > 1000 || d.Landline?.Trim().Length > 16 || d.Mobile?.Trim().Length > 16 || d.Email?.Trim().Length > 320 || d.RecipientName?.Trim().Length > 200 || d.Notes?.Trim().Length > 2000 || string.IsNullOrWhiteSpace(d.AddressName) || string.IsNullOrWhiteSpace(d.FullAddress) || d.AddressTypeId is null || d.CountryCode?.Trim().Length!=2 || !d.CountryCode.Trim().All(char.IsAsciiLetter))
            return Result.Failure<SupplierAddressData>(SupplierCoreErrors.InvalidData);
        if (d.IsDefault&&!d.IsActive) return Result.Failure<SupplierAddressData>(SupplierCoreErrors.InvalidDefault);
        d=d with { AddressName = d.AddressName.Trim(), CountryCode = d.CountryCode.Trim().ToUpperInvariant(), ProvinceCode = SupplierCoreRules.Trim(d.ProvinceCode), CityCode = SupplierCoreRules.Trim(d.CityCode), PostalCode = SupplierCoreRules.Trim(d.PostalCode), FullAddress = d.FullAddress.Trim(), Landline = CustomerText.Phone(d.Landline), Mobile = CustomerText.Phone(d.Mobile), Email = CustomerText.Email(d.Email), RecipientName = SupplierCoreRules.Trim(d.RecipientName), Notes = SupplierCoreRules.Trim(d.Notes) };
        return d.Landline==""||d.Mobile==""||d.Email=="" ? Result.Failure<SupplierAddressData>(SupplierCoreErrors.InvalidData) : Result.Success(d);
    }
    public static Result<SupplierContactData> Contact(SupplierContactData d)
    {
        if (d.FirstName?.Trim().Length > 100 || d.LastName?.Trim().Length > 100 || d.PositionTitle?.Trim().Length > 150 || d.Landline?.Trim().Length > 16 || d.Mobile?.Trim().Length > 16 || d.Extension?.Trim().Length > 12 || d.Email?.Trim().Length > 320 || d.Notes?.Trim().Length > 2000 || string.IsNullOrWhiteSpace(d.FirstName) || d.ContactRoleId is null)
            return Result.Failure<SupplierContactData>(SupplierCoreErrors.InvalidData);
        if (d.IsPrimary&&!d.IsActive) return Result.Failure<SupplierContactData>(SupplierCoreErrors.InvalidDefault);
        d=d with { FirstName = d.FirstName.Trim(), LastName = SupplierCoreRules.Trim(d.LastName), PositionTitle = SupplierCoreRules.Trim(d.PositionTitle), Landline = CustomerText.Phone(d.Landline), Mobile = CustomerText.Phone(d.Mobile), Extension = SupplierCoreRules.Trim(d.Extension), Email = CustomerText.Email(d.Email), Notes = SupplierCoreRules.Trim(d.Notes) };
        return d.Landline==""||d.Mobile==""||d.Email=="" ? Result.Failure<SupplierContactData>(SupplierCoreErrors.InvalidData) : Result.Success(d);
    }
    public static Result<SupplierBankData> Bank(SupplierBankData d)
    {
        if (d.AccountName?.Trim().Length > 150 || d.OwnershipType?.Trim().Length > 20 || d.AccountHolderNames?.Trim().Length > 400 || d.AccountNumber?.Trim().Length > 64 || d.Iban?.Trim().Length > 34 || d.CardNumber?.Trim().Length > 19 || d.BranchCode?.Trim().Length > 40 || d.BranchName?.Trim().Length > 150 || d.CurrencyCode?.Trim().Length > 3 || d.Notes?.Trim().Length > 2000 || d.BankId is null || string.IsNullOrWhiteSpace(d.AccountHolderNames) || d.OwnershipType is not ("Individual" or "Joint") || CustomerText.Currency(d.CurrencyCode) is null)
            return Result.Failure<SupplierBankData>(SupplierCoreErrors.InvalidData);
        if (d.IsDefault&&!d.IsActive) return Result.Failure<SupplierBankData>(SupplierCoreErrors.InvalidDefault);
        d=d with { AccountName = SupplierCoreRules.Trim(d.AccountName), OwnershipType = d.OwnershipType.Trim(), AccountHolderNames = d.AccountHolderNames.Trim(), AccountNumber = SupplierCoreRules.Trim(d.AccountNumber), Iban = SupplierCoreRules.Compact(d.Iban)?.ToUpperInvariant(), CardNumber = SupplierCoreRules.Compact(d.CardNumber), BranchCode = SupplierCoreRules.Trim(d.BranchCode), BranchName = SupplierCoreRules.Trim(d.BranchName), CurrencyCode = CustomerText.Currency(d.CurrencyCode) ?? "", Notes = SupplierCoreRules.Trim(d.Notes) };
        if (d.Iban is string iban && (iban.Length is <15 or >34 || !iban.Take(2).All(char.IsAsciiLetterUpper) || !iban.Skip(2).Take(2).All(char.IsAsciiDigit) || !iban.All(char.IsAsciiLetterOrDigit)))
            return Result.Failure<SupplierBankData>(SupplierCoreErrors.InvalidData);
        if (d.CardNumber is string card && (card.Length is <13 or >19 || !card.All(char.IsAsciiDigit)))
            return Result.Failure<SupplierBankData>(SupplierCoreErrors.InvalidData);
        return Result.Success(d);
    }
}

public sealed partial class Supplier
{
    public long SupplierNumber { get; private set; }
    public string EntityType { get; private set; } = "Individual";
    public bool IsBlocked { get; private set; }
    public string Status => IsBlocked ? "Blocked" : IsActive ? "Active" : "Inactive";
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? NationalId { get; private set; }
    public string? Occupation { get; private set; }
    public string? TradeName { get; private set; }
    public Guid? LegalEntityTypeId { get; private set; }
    public DateOnly? EstablishmentDate { get; private set; }
    public string? BusinessActivity { get; private set; }
    public Guid? GradeId { get; private set; }
    public bool IsPreferred { get; private set; }
    public DateOnly? CooperationStartDate { get; private set; }
    public Guid? HoldReasonId { get; private set; }
    public static Result<Supplier> CreateCore(Guid id,Guid company,SupplierIdentityData data,DateTimeOffset now)
    {
        var valid=SupplierCoreRules.Identity(data);
        if(valid.IsFailure)return Result.Failure<Supplier>(valid.Error);
        if(id==Guid.Empty||company==Guid.Empty)return Result.Failure<Supplier>(SupplierCoreErrors.InvalidData);
        Supplier value=new(){SupplierCode=valid.Value.SupplierCode};value.Initialize(id,company,now);
        Result applied=value.ApplyCore(valid.Value,now,false);
        return applied.IsFailure?Result.Failure<Supplier>(applied.Error):Result.Success(value);
    }
    public Result UpdateCore(SupplierIdentityData data,DateTimeOffset now)
    {
        var valid=SupplierCoreRules.Identity(data);
        if(valid.IsFailure)return Result.Failure(valid.Error);
        if(valid.Value.SupplierCode!=SupplierCode)return Result.Failure(SupplierCoreErrors.InvalidData);
        return ApplyCore(valid.Value,now,true);
    }
    private Result ApplyCore(SupplierIdentityData d,DateTimeOffset now,bool touch)
    {
        Result original=Update(d.DisplayName,d.EntityType=="LegalEntity"?d.LegalName:null,d.EntityType=="LegalEntity"?d.RegistrationNo:null,d.TaxId,d.PrimaryEmail,d.PrimaryPhone,d.PrimaryMobile,d.Website,d.Notes,now,false);
        if(original.IsFailure)return original;
        EntityType=d.EntityType;IsBlocked=d.Status=="Blocked";IsActive=d.Status=="Active";
        FirstName=d.EntityType=="Individual"?d.FirstName:null;
        LastName=d.EntityType=="Individual"?d.LastName:null;
        NationalId=d.NationalId;
        Occupation=d.EntityType=="Individual"?d.Occupation:null;
        TradeName=d.EntityType=="LegalEntity"?d.TradeName:null;
        LegalEntityTypeId=d.EntityType=="LegalEntity"?d.LegalEntityTypeId:null;
        EstablishmentDate=d.EntityType=="LegalEntity"?d.EstablishmentDate:null;
        BusinessActivity=d.BusinessActivity;
        GradeId=d.GradeId;
        IsPreferred=d.IsPreferred;
        CooperationStartDate=d.CooperationStartDate;
        HoldReasonId=d.HoldReasonId;
        if(touch)Touch(now);return Result.Success();
    }
    public Result SetCoreStatus(string status,Guid? reason,DateTimeOffset now)
    {
        if(status is not ("Active" or "Inactive" or "Blocked") || status=="Blocked"&&reason is null)return Result.Failure(SupplierCoreErrors.InvalidData);
        IsBlocked=status=="Blocked";IsActive=status=="Active";HoldReasonId=IsBlocked?reason:null;Touch(now);return Result.Success();
    }
}

public abstract class SupplierOwnedEntity : CatalogEntity
{
    public Guid SupplierId { get; protected set; }
    public bool IsDefault { get; protected set; }
    protected void InitializeOwner(Guid supplierId,Guid company,DateTimeOffset now) {Initialize(Guid.NewGuid(),company,now);SupplierId=supplierId;}
    public Result SelectDefault(bool selected,DateTimeOffset now)
    {
        if(selected&&!IsActive)return Result.Failure(SupplierCoreErrors.InvalidDefault);
        if(IsDefault!=selected){IsDefault=selected;Touch(now);}return Result.Success();
    }
    public void ChangeActivation(bool active,DateTimeOffset now)
    {
        if(!active)IsDefault=false;IsActive=active;Touch(now);
    }
}

public sealed class SupplierAddress : SupplierOwnedEntity
{
    private SupplierAddress() { }
    public string AddressName { get; private set; } = "";
    public Guid? AddressTypeId { get; private set; }
    public string CountryCode { get; private set; } = "IR";
    public string? ProvinceCode { get; private set; }
    public string? CityCode { get; private set; }
    public string? PostalCode { get; private set; }
    public string FullAddress { get; private set; } = "";
    public string? Landline { get; private set; }
    public string? Mobile { get; private set; }
    public string? Email { get; private set; }
    public string? RecipientName { get; private set; }
    public string? Notes { get; private set; }
    public static Result<SupplierAddress> Create(Guid owner,Guid company,SupplierAddressData data,DateTimeOffset now)
    {
        if(owner==Guid.Empty||company==Guid.Empty)return Result.Failure<SupplierAddress>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Address(data);if(valid.IsFailure)return Result.Failure<SupplierAddress>(valid.Error);
        SupplierAddress value=new();value.InitializeOwner(owner,company,now);value.Apply(valid.Value);return Result.Success(value);
    }
    public Result Update(SupplierAddressData data,DateTimeOffset now)
    {
        var valid=SupplierCoreRules.Address(data);if(valid.IsFailure)return Result.Failure(valid.Error);
        Apply(valid.Value);Touch(now);return Result.Success();
    }
    private void Apply(SupplierAddressData d)
    {
        AddressName=d.AddressName;
        AddressTypeId=d.AddressTypeId;
        CountryCode=d.CountryCode;
        ProvinceCode=d.ProvinceCode;
        CityCode=d.CityCode;
        PostalCode=d.PostalCode;
        FullAddress=d.FullAddress;
        Landline=d.Landline;
        Mobile=d.Mobile;
        Email=d.Email;
        RecipientName=d.RecipientName;
        Notes=d.Notes;
        IsActive=d.IsActive;IsDefault=d.IsDefault;
    }
}

public sealed class SupplierContact : SupplierOwnedEntity
{
    private SupplierContact() { }
    public string FirstName { get; private set; } = "";
    public string? LastName { get; private set; }
    public Guid? ContactRoleId { get; private set; }
    public string? PositionTitle { get; private set; }
    public string? Landline { get; private set; }
    public string? Mobile { get; private set; }
    public string? Extension { get; private set; }
    public string? Email { get; private set; }
    public string? Notes { get; private set; }
    public bool IsPrimary => IsDefault;
    public static Result<SupplierContact> Create(Guid owner,Guid company,SupplierContactData data,DateTimeOffset now)
    {
        if(owner==Guid.Empty||company==Guid.Empty)return Result.Failure<SupplierContact>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Contact(data);if(valid.IsFailure)return Result.Failure<SupplierContact>(valid.Error);
        SupplierContact value=new();value.InitializeOwner(owner,company,now);value.Apply(valid.Value);return Result.Success(value);
    }
    public Result Update(SupplierContactData data,DateTimeOffset now)
    {
        var valid=SupplierCoreRules.Contact(data);if(valid.IsFailure)return Result.Failure(valid.Error);
        Apply(valid.Value);Touch(now);return Result.Success();
    }
    private void Apply(SupplierContactData d)
    {
        FirstName=d.FirstName;
        LastName=d.LastName;
        ContactRoleId=d.ContactRoleId;
        PositionTitle=d.PositionTitle;
        Landline=d.Landline;
        Mobile=d.Mobile;
        Extension=d.Extension;
        Email=d.Email;
        Notes=d.Notes;
        IsActive=d.IsActive;IsDefault=d.IsPrimary;
    }
}

public sealed class SupplierBankAccount : SupplierOwnedEntity
{
    private SupplierBankAccount() { }
    public Guid? BankId { get; private set; }
    public string? AccountName { get; private set; }
    public Guid? AccountTypeId { get; private set; }
    public string OwnershipType { get; private set; } = "Individual";
    public string AccountHolderNames { get; private set; } = "";
    public string? AccountNumber { get; private set; }
    public string? Iban { get; private set; }
    public string? CardNumber { get; private set; }
    public string? BranchCode { get; private set; }
    public string? BranchName { get; private set; }
    public string CurrencyCode { get; private set; } = "IRT";
    public string? Notes { get; private set; }
    public static Result<SupplierBankAccount> Create(Guid owner,Guid company,SupplierBankData data,DateTimeOffset now)
    {
        if(owner==Guid.Empty||company==Guid.Empty)return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Bank(data);if(valid.IsFailure)return Result.Failure<SupplierBankAccount>(valid.Error);
        SupplierBankAccount value=new();value.InitializeOwner(owner,company,now);value.Apply(valid.Value);return Result.Success(value);
    }
    public Result Update(SupplierBankData data,DateTimeOffset now)
    {
        var valid=SupplierCoreRules.Bank(data);if(valid.IsFailure)return Result.Failure(valid.Error);
        Apply(valid.Value);Touch(now);return Result.Success();
    }
    private void Apply(SupplierBankData d)
    {
        BankId=d.BankId;
        AccountName=d.AccountName;
        AccountTypeId=d.AccountTypeId;
        OwnershipType=d.OwnershipType;
        AccountHolderNames=d.AccountHolderNames;
        AccountNumber=d.AccountNumber;
        Iban=d.Iban;
        CardNumber=d.CardNumber;
        BranchCode=d.BranchCode;
        BranchName=d.BranchName;
        CurrencyCode=d.CurrencyCode;
        Notes=d.Notes;
        IsActive=d.IsActive;IsDefault=d.IsDefault;
    }
}

public sealed class SupplierFinancialProfile : CatalogEntity
{
    private SupplierFinancialProfile() { }
    public Guid SupplierId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public Guid? PaymentConditionId { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public string DefaultCurrencyCode { get; private set; } = "IRT";
    public decimal? MinimumOrderAmount { get; private set; }
    public int? DefaultLeadTimeDays { get; private set; }
    public decimal BaseDiscountPercent { get; private set; } = 0;
    public bool CreditPurchaseAllowed { get; private set; }
    public decimal PurchaseCreditLimit { get; private set; } = 0;
    public Guid? ShippingMethodId { get; private set; }
    public Guid? FreightDeliveryTermId { get; private set; }
    public static Result<SupplierFinancialProfile> Create(Guid owner,Guid company,SupplierFinancialData data,DateTimeOffset now)
    {
        if(owner==Guid.Empty||company==Guid.Empty)return Result.Failure<SupplierFinancialProfile>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Financial(data);if(valid.IsFailure)return Result.Failure<SupplierFinancialProfile>(valid.Error);
        SupplierFinancialProfile value=new(){SupplierId=owner};value.Initialize(Guid.NewGuid(),company,now);value.Apply(valid.Value);return Result.Success(value);
    }
    public Result Update(SupplierFinancialData data,DateTimeOffset now)
    {
        var valid=SupplierCoreRules.Financial(data);if(valid.IsFailure)return Result.Failure(valid.Error);
        Apply(valid.Value);Touch(now);return Result.Success();
    }
    private void Apply(SupplierFinancialData d)
    {
        PaymentTermId=d.PaymentTermId;
        PaymentConditionId=d.PaymentConditionId;
        PaymentMethodId=d.PaymentMethodId;
        DefaultCurrencyCode=d.DefaultCurrencyCode;
        MinimumOrderAmount=d.MinimumOrderAmount;
        DefaultLeadTimeDays=d.DefaultLeadTimeDays;
        BaseDiscountPercent=d.BaseDiscountPercent;
        CreditPurchaseAllowed=d.CreditPurchaseAllowed;
        PurchaseCreditLimit=d.PurchaseCreditLimit;
        ShippingMethodId=d.ShippingMethodId;
        FreightDeliveryTermId=d.FreightDeliveryTermId;
    }
}
public sealed class SupplierCategoryAssignment : CatalogEntity
{
    private SupplierCategoryAssignment() { }
    public Guid SupplierId { get; private set; }
    public Guid CategoryId { get; private set; }
    public static SupplierCategoryAssignment Create(Guid owner,Guid company,Guid category,DateTimeOffset now)
    {
        if(owner==Guid.Empty||company==Guid.Empty||category==Guid.Empty)throw new ArgumentException("Supplier category identifiers are required.");
        SupplierCategoryAssignment value=new(){SupplierId=owner,CategoryId=category};value.Initialize(Guid.NewGuid(),company,now);return value;
    }
}
