using System.Net.Mail;
using System.Text.Json.Serialization;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Customers;

[JsonConverter(typeof(JsonStringEnumConverter<CustomerType>))]
public enum CustomerType { Individual = 0, LegalEntity = 1, Person = Individual, Organization = LegalEntity }

[JsonConverter(typeof(JsonStringEnumConverter<CustomerStatus>))]
public enum CustomerStatus { Active, Inactive, Blocked }

[JsonConverter(typeof(JsonStringEnumConverter<CustomerFinancialStatus>))]
public enum CustomerFinancialStatus { Normal, Debtor, Blocked }

public static class CustomerErrors
{
    public static Error LegalContactRequired => Error.BusinessRule("customer.contacts_legal_only");
    public static Error InvalidCode => Error.Validation("customer.invalid_code");
    public static Error InvalidData => Error.Validation("customer.invalid_data");
    public static Error InvalidEmail => Error.Validation("customer.invalid_email");
    public static Error InvalidPhone => Error.Validation("customer.invalid_phone");
    public static Error InvalidProfile => Error.Validation("customer.invalid_profile");
    public static Error InvalidStatus => Error.Validation("customer.invalid_status");
    public static Error InvalidFinancial => Error.Validation("customer.invalid_financial");
    public static Error InvalidReference => Error.Validation("customer.invalid_reference");
    public static Error DuplicateCode => Error.Conflict("customer.duplicate_code");
    public static Error DuplicateIdentifier => Error.Conflict("customer.duplicate_identifier");
    public static Error DuplicateBankAccount => Error.Conflict("customer.duplicate_bank_account");
    public static Error PrimaryConflict => Error.Conflict("customer.primary_conflict");
    public static Error TypeImmutable => Error.Conflict("customer.type_immutable");
    public static Error Referenced => Error.Conflict("customer.referenced");
    public static Error NotFound => Error.NotFound("customer.not_found");
    public static Error ContactNotFound => Error.NotFound("customer.contact_not_found");
    public static Error AddressNotFound => Error.NotFound("customer.address_not_found");
    public static Error BankAccountNotFound => Error.NotFound("customer.bank_account_not_found");
    public static Error Concurrency => Error.Concurrency("customer.concurrency_conflict");
}

public static class CustomerText
{
    public static string? Code(string? value) { string v=value?.Trim().ToUpperInvariant()??""; return v.Length is >=2 and <=50 && v.All(static c=>char.IsAsciiLetterOrDigit(c)||c is '-' or '_' or '.')?v:null; }
    public static string? Name(string? value,int max=200,bool optional=false) { string v=value?.Trim()??""; return v.Length==0&&optional?null:v.Length is >=1&&v.Length<=max?v:null; }
    public static string? Identifier(string? value) { string v=value?.Trim().ToUpperInvariant()??""; return v.Length==0?null:v.Length<=64?v:null; }
    public static string? Email(string? value)
    {
        string v=value?.Trim().ToLowerInvariant()??""; if(v.Length==0)return null; if(v.Length>320)return "";
        try { MailAddress parsed=new(v); return parsed.Address.Equals(v,StringComparison.OrdinalIgnoreCase)?v:""; } catch(FormatException){return "";}
    }
    public static string? Phone(string? value)
    {
        string raw=value?.Trim()??""; if(raw.Length==0)return null; bool plus=raw.StartsWith('+'); string digits=new(raw.Where(char.IsAsciiDigit).ToArray());
        if(digits.Length is <7 or >15 || raw.Skip(plus?1:0).Any(static c=>!char.IsAsciiDigit(c)&&c is not ' ' and not '-' and not '(' and not ')')) return "";
        return plus?"+"+digits:digits;
    }
    public static string? Category(string? value) { string v=value?.Trim().ToLowerInvariant()??""; return v.Length is >=2 and <=80&&v[0] is >= 'a' and <= 'z'&&v.All(static c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or '-')?v:null; }
    public static string? Currency(string? value) { string v=value?.Trim().ToUpperInvariant()??""; return v.Length==3&&v.All(char.IsAsciiLetter)?v:null; }
}

public sealed partial class Customer
{
    private Customer() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string CustomerCode { get; private set; }="";
    public CustomerType CustomerType { get; private set; }
    public string DisplayName { get; private set; }="";
    public CustomerStatus Status { get; private set; }
    public string? PrimaryMobile { get; private set; }
    public string? PrimaryPhone { get; private set; }
    public string? PrimaryEmail { get; private set; }
    public string? EconomicCode { get; private set; }
    public string? CustomerGroupCode { get; private set; }
    public string? PriceGroupCode { get; private set; }
    public Guid? SalesRepresentativePersonId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public bool IsActive => Status==CustomerStatus.Active;

    public static Result<Customer> Create(Guid id,Guid companyId,string code,CustomerType type,string displayName,string? mobile,string? phone,string? email,string? economicCode,string? customerGroupCode,string? priceGroupCode,Guid? salesRepresentativePersonId,string? notes,DateTimeOffset now)
        => CreateCore(id,companyId,code,type,displayName,mobile,phone,email,economicCode,customerGroupCode,priceGroupCode,salesRepresentativePersonId,notes,now,true);

    // Compatibility for non-customer test fixtures. API creation always uses the normalized overload and a matching profile.
    public static Result<Customer> Create(Guid id,Guid companyId,string code,CustomerType type,string? displayName,string? firstName,string? lastName,string? legalName,string? nationalId,string? registrationNo,string? taxId,string? mobile,string? phone,string? email,string? notes,DateTimeOffset now)
    {
        if ((nationalId?.Trim().Length ?? 0) > 64 || (registrationNo?.Trim().Length ?? 0) > 64)
            return Result.Failure<Customer>(CustomerErrors.InvalidData);
        string derived=type==CustomerType.Individual?string.Join(' ',new[]{firstName?.Trim(),lastName?.Trim()}.Where(x=>!string.IsNullOrWhiteSpace(x))):legalName?.Trim()??"";
        return CreateCore(id,companyId,code,type,string.IsNullOrWhiteSpace(displayName)?derived:displayName,mobile,phone,email,taxId,null,null,null,notes,now,false);
    }

    private static Result<Customer> CreateCore(Guid id,Guid companyId,string code,CustomerType type,string? displayName,string? mobile,string? phone,string? email,string? economicCode,string? customerGroupCode,string? priceGroupCode,Guid? salesRepresentativePersonId,string? notes,DateTimeOffset now,bool requireIndividualMobile)
    {
        string? normalizedCode=CustomerText.Code(code);
        if(id==Guid.Empty||companyId==Guid.Empty||normalizedCode is null||type is not (CustomerType.Individual or CustomerType.LegalEntity))return Result.Failure<Customer>(CustomerErrors.InvalidCode);
        Result<CustomerData> data=Validate(type,displayName,mobile,phone,email,economicCode,customerGroupCode,priceGroupCode,salesRepresentativePersonId,notes,requireIndividualMobile);
        if(data.IsFailure)return Result.Failure<Customer>(data.Error); CustomerData d=data.Value;
        return Result.Success(new Customer{Id=id,CompanyId=companyId,CustomerCode=normalizedCode,CustomerType=type,DisplayName=d.DisplayName,Status=CustomerStatus.Active,PrimaryMobile=d.Mobile,PrimaryPhone=d.Phone,PrimaryEmail=d.Email,EconomicCode=d.EconomicCode,CustomerGroupCode=d.CustomerGroupCode,PriceGroupCode=d.PriceGroupCode,SalesRepresentativePersonId=d.SalesRepresentativePersonId,Notes=d.Notes,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1});
    }

    public Result Update(string displayName,string? mobile,string? phone,string? email,string? economicCode,string? customerGroupCode,string? priceGroupCode,Guid? salesRepresentativePersonId,string? notes,long version,DateTimeOffset now)
    {
        if(version!=Version)return Result.Failure(CustomerErrors.Concurrency);
        Result<CustomerData> data=Validate(CustomerType,displayName,mobile,phone,email,economicCode,customerGroupCode,priceGroupCode,salesRepresentativePersonId,notes,true);
        if(data.IsFailure)return Result.Failure(data.Error); CustomerData d=data.Value;
        DisplayName=d.DisplayName;PrimaryMobile=d.Mobile;PrimaryPhone=d.Phone;PrimaryEmail=d.Email;EconomicCode=d.EconomicCode;CustomerGroupCode=d.CustomerGroupCode;PriceGroupCode=d.PriceGroupCode;SalesRepresentativePersonId=d.SalesRepresentativePersonId;Notes=d.Notes;Touch(now);return Result.Success();
    }

    public Result SetStatus(CustomerStatus status,long version,DateTimeOffset now)
    {
        if(version!=Version)return Result.Failure(CustomerErrors.Concurrency);
        if(!Enum.IsDefined(status))return Result.Failure(CustomerErrors.InvalidStatus);
        if(Status!=status){Status=status;Touch(now);} return Result.Success();
    }
    public void SetActive(bool active,DateTimeOffset now){if(IsActive!=active){Status=active?CustomerStatus.Active:CustomerStatus.Inactive;Touch(now);}}

    private static Result<CustomerData> Validate(CustomerType type,string? displayName,string? mobile,string? phone,string? email,string? economicCode,string? customerGroupCode,string? priceGroupCode,Guid? salesRepresentativePersonId,string? notes,bool requireIndividualMobile)
    {
        string? name=CustomerText.Name(displayName),m=CustomerText.Phone(mobile),p=CustomerText.Phone(phone),e=CustomerText.Email(email),economic=CustomerText.Identifier(economicCode),group=OptionalCategory(customerGroupCode),price=OptionalCategory(priceGroupCode);
        if(e=="")return Result.Failure<CustomerData>(CustomerErrors.InvalidEmail);
        if(m==""||p=="")return Result.Failure<CustomerData>(CustomerErrors.InvalidPhone);
        if(name is null||(requireIndividualMobile&&type==CustomerType.Individual&&m is null)||(economicCode?.Trim().Length>0&&economic is null)||(customerGroupCode?.Trim().Length>0&&group is null)||(priceGroupCode?.Trim().Length>0&&price is null)||salesRepresentativePersonId==Guid.Empty||notes?.Trim().Length>2000)return Result.Failure<CustomerData>(CustomerErrors.InvalidData);
        return Result.Success(new CustomerData(name,m,p,e,economic,group,price,salesRepresentativePersonId,CustomerText.Name(notes,2000,true)));
    }
    private static string? OptionalCategory(string? value)=>string.IsNullOrWhiteSpace(value)?null:CustomerText.Category(value);
    private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
    private sealed record CustomerData(string DisplayName,string? Mobile,string? Phone,string? Email,string? EconomicCode,string? CustomerGroupCode,string? PriceGroupCode,Guid? SalesRepresentativePersonId,string? Notes);
}


public sealed class CustomerFavoriteDesign
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ArtworkId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CustomerFavoriteDesign Create(Guid id, Guid companyId, Guid customerId, Guid artworkId, DateTimeOffset now)
        => new() { Id = id, CompanyId = companyId, CustomerId = customerId, ArtworkId = artworkId, CreatedAtUtc = now };
}


public enum CustomerWalletDirection { Credit = 1, Debit = 2 }

public sealed class CustomerWalletTransaction
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public CustomerWalletDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public string ReasonCode { get; private set; } = "";
    public string? ReferenceNote { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<CustomerWalletTransaction> Create(Guid id, Guid companyId, Guid customerId, CustomerWalletDirection direction, decimal amount, string currencyCode, string reasonCode, string? referenceNote, Guid? orderId, DateTimeOffset now)
    {
        if (customerId == Guid.Empty) return Result.Failure<CustomerWalletTransaction>(CustomerErrors.InvalidData);
        if (amount <= 0) return Result.Failure<CustomerWalletTransaction>(CustomerErrors.InvalidData);
        if (string.IsNullOrWhiteSpace(currencyCode) || string.IsNullOrWhiteSpace(reasonCode)) return Result.Failure<CustomerWalletTransaction>(CustomerErrors.InvalidData);
        return Result.Success(new CustomerWalletTransaction { Id = id, CompanyId = companyId, CustomerId = customerId, Direction = direction, Amount = decimal.Round(amount, 0), CurrencyCode = currencyCode.Trim().ToUpperInvariant(), ReasonCode = reasonCode.Trim(), ReferenceNote = CustomerText.Name(referenceNote, 500, true), OrderId = orderId is Guid o && o != Guid.Empty ? o : null, CreatedAtUtc = now });
    }
}


public static class WalletErrors
{
    public static Error NotFound => Error.NotFound("wallet.topup_not_found");
    public static Error CustomerNotFound => Error.NotFound("wallet.customer_not_found");
    public static Error InvalidAmount => Error.Validation("wallet.invalid_amount");
    public static Error BelowMinimum => Error.Validation("wallet.below_minimum");
    public static Error NotPending => Error.BusinessRule("wallet.topup_not_pending");
    public static Error AlreadyProcessed => Error.BusinessRule("wallet.topup_already_processed");
    public static Error InsufficientBalance => Error.BusinessRule("wallet.insufficient_balance");
    public static Error AlreadyPaid => Error.BusinessRule("wallet.order_already_paid");
    public static Error OrderNotEligible => Error.BusinessRule("wallet.order_not_eligible");
    public static Error PricingRequired => Error.BusinessRule("wallet.pricing_required");
    public static Error CurrencyMismatch => Error.BusinessRule("wallet.currency_mismatch");
    public static Error Concurrency => Error.Conflict("wallet.concurrency");
}

public enum CustomerWalletTopUpStatus { Pending = 1, Paid = 2, Failed = 3, Cancelled = 4 }

public sealed class CustomerWalletTopUp
{
    public const string DefaultGatewayCode = "mock";

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Number { get; private set; } = "";
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public CustomerWalletTopUpStatus Status { get; private set; }
    public string GatewayCode { get; private set; } = DefaultGatewayCode;
    public Guid PaymentToken { get; private set; }
    public string? GatewayReference { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<CustomerWalletTopUp> Create(Guid id, Guid companyId, Guid customerId, string number, decimal amount, string currencyCode, Guid paymentToken, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || customerId == Guid.Empty) return Result.Failure<CustomerWalletTopUp>(WalletErrors.CustomerNotFound);
        if (amount <= 0) return Result.Failure<CustomerWalletTopUp>(WalletErrors.InvalidAmount);
        string currency = (currencyCode ?? "").Trim().ToUpperInvariant();
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) return Result.Failure<CustomerWalletTopUp>(WalletErrors.InvalidAmount);
        if (paymentToken == Guid.Empty) return Result.Failure<CustomerWalletTopUp>(WalletErrors.InvalidAmount);
        return Result.Success(new CustomerWalletTopUp { Id = id, CompanyId = companyId, CustomerId = customerId, Number = number.Trim(), Amount = decimal.Round(amount, 0), CurrencyCode = currency, Status = CustomerWalletTopUpStatus.Pending, GatewayCode = DefaultGatewayCode, PaymentToken = paymentToken, CreatedAtUtc = now, Version = 1 });
    }

    public Result MarkPaid(string? gatewayReference, DateTimeOffset now)
    {
        if (Status != CustomerWalletTopUpStatus.Pending) return Result.Failure(WalletErrors.AlreadyProcessed);
        Status = CustomerWalletTopUpStatus.Paid;
        GatewayReference = CustomerWalletTopUp.Text(gatewayReference, 100) ?? $"GATEWAY-{now.Ticks}";
        PaidAtUtc = now;
        Version++;
        return Result.Success();
    }

    public Result MarkFailed(string? gatewayReference, DateTimeOffset now)
    {
        if (Status != CustomerWalletTopUpStatus.Pending) return Result.Failure(WalletErrors.AlreadyProcessed);
        Status = CustomerWalletTopUpStatus.Failed;
        GatewayReference = CustomerWalletTopUp.Text(gatewayReference, 100);
        Version++;
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset now)
    {
        if (Status != CustomerWalletTopUpStatus.Pending) return Result.Failure(WalletErrors.NotPending);
        Status = CustomerWalletTopUpStatus.Cancelled;
        Version++;
        return Result.Success();
    }

    internal static string? Text(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; string x = value.Trim(); return x.Length <= max ? x : null; }
}


public sealed class CustomerFeedback
{
    public const int SubjectMinLength = 3;
    public const int SubjectMaxLength = 120;
    public const int BodyMinLength = 10;
    public const int BodyMaxLength = 2000;

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string FeedbackTypeCode { get; private set; } = "";
    public string Subject { get; private set; } = "";
    public string Body { get; private set; } = "";
    public string StatusCode { get; private set; } = "new";
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<CustomerFeedback> Create(Guid id, Guid companyId, Guid customerId, string feedbackTypeCode, string subject, string body, DateTimeOffset now)
    {
        string? type = CustomerText.Category(feedbackTypeCode);
        string? s = CustomerText.Name(subject);
        string? b = CustomerText.Name(body);
        if (type is null) return Result.Failure<CustomerFeedback>(CustomerErrors.InvalidData);
        if (s is null || s.Length is < SubjectMinLength or > SubjectMaxLength) return Result.Failure<CustomerFeedback>(CustomerErrors.InvalidData);
        if (b is null || b.Length is < BodyMinLength or > BodyMaxLength) return Result.Failure<CustomerFeedback>(CustomerErrors.InvalidData);
        return Result.Success(new CustomerFeedback { Id = id, CompanyId = companyId, CustomerId = customerId, FeedbackTypeCode = type, Subject = s, Body = b, StatusCode = "new", CreatedAtUtc = now });
    }

    public static readonly IReadOnlySet<string> AllowedStatuses = new HashSet<string>(["new", "in_review", "resolved"], StringComparer.Ordinal);

    public Result SetStatus(string statusCode)
    {
        string? code = CustomerText.Category(statusCode);
        if (code is null || !AllowedStatuses.Contains(code)) return Result.Failure(CustomerErrors.InvalidData);
        StatusCode = code;
        return Result.Success();
    }
}


public sealed class CustomerCustomDesign
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Title { get; private set; } = "";
    public Guid FileVersionId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<CustomerCustomDesign> Create(Guid id, Guid companyId, Guid customerId, string title, Guid fileVersionId, DateTimeOffset now)
    {
        string? t = CustomerText.Name(title);
        if (t is null || t.Length > 120) return Result.Failure<CustomerCustomDesign>(CustomerErrors.InvalidData);
        if (fileVersionId == Guid.Empty) return Result.Failure<CustomerCustomDesign>(CustomerErrors.InvalidData);
        return Result.Success(new CustomerCustomDesign { Id = id, CompanyId = companyId, CustomerId = customerId, Title = t, FileVersionId = fileVersionId, CreatedAtUtc = now });
    }
}

public sealed class ReferralErrors
{
    public static Error CodeNotFound => Error.NotFound("referral.code_not_found");
    public static Error AlreadyReferred => Error.Conflict("referral.already_referred");
    public static Error SelfReferral => Error.BusinessRule("referral.self_referral");
    public static Error NotEligible => Error.BusinessRule("referral.not_eligible");
    public static Error InvalidCode => Error.Validation("referral.invalid_code");
}

public enum CustomerReferralStatus { Pending = 1, Rewarded = 2 }

// A referred customer is linked to exactly one referrer; the reward is a gift
// credit to the referrer wallet when the referred customer pays their first order.
public sealed class CustomerReferral
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ReferrerCustomerId { get; private set; }
    public Guid ReferredCustomerId { get; private set; }
    public string CodeUsed { get; private set; } = "";
    public CustomerReferralStatus Status { get; private set; }
    public decimal RewardAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public Guid? RewardTransactionId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RewardedAtUtc { get; private set; }

    public static Result<CustomerReferral> Create(Guid id, Guid companyId, Guid referrerCustomerId, Guid referredCustomerId, string codeUsed, decimal rewardAmount, string currencyCode, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || referrerCustomerId == Guid.Empty || referredCustomerId == Guid.Empty) return Result.Failure<CustomerReferral>(ReferralErrors.NotEligible);
        if (referrerCustomerId == referredCustomerId) return Result.Failure<CustomerReferral>(ReferralErrors.SelfReferral);
        string code = (codeUsed ?? "").Trim().ToUpperInvariant();
        if (code.Length is < 4 or > 20) return Result.Failure<CustomerReferral>(ReferralErrors.InvalidCode);
        return Result.Success(new CustomerReferral { Id = id, CompanyId = companyId, ReferrerCustomerId = referrerCustomerId, ReferredCustomerId = referredCustomerId, CodeUsed = code, Status = CustomerReferralStatus.Pending, RewardAmount = decimal.Round(rewardAmount, 0), CurrencyCode = currencyCode.Trim().ToUpperInvariant(), CreatedAtUtc = now });
    }

    public Result Reward(Guid transactionId, DateTimeOffset now)
    {
        if (Status != CustomerReferralStatus.Pending) return Result.Failure(ReferralErrors.AlreadyReferred);
        Status = CustomerReferralStatus.Rewarded;
        RewardTransactionId = transactionId;
        RewardedAtUtc = now;
        return Result.Success();
    }
}

// One stable invite code per customer, generated lazily on first use.
public sealed class CustomerReferralCode
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Code { get; private set; } = "";
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CustomerReferralCode Create(Guid id, Guid companyId, Guid customerId, string code, DateTimeOffset now)
        => new() { Id = id, CompanyId = companyId, CustomerId = customerId, Code = code.Trim().ToUpperInvariant(), CreatedAtUtc = now };
}
