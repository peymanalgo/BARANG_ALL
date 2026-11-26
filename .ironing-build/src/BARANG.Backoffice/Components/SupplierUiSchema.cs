using System.Globalization;
using System.Text.Json;

namespace BARANG.Backoffice.Components;

public sealed record SupplierUiField(string Key,string Label,string Kind="text",string Default="",int MaxLength=0,string? Reference=null,bool Required=false,string? Only=null,int Scale=2);
public static class SupplierUiSchema
{
    public static readonly SupplierUiField[] Identity = [
        new("supplierCode","SupplierCode","technical","",50,null,true,null),
        new("displayName","DisplayName","text","",200,null,false,null),
        new("entityType","EntityType","entity","Individual",20,null,false,null),
        new("status","Status","status","Active",20,null,false,null),
        new("firstName","FirstName","text","",100,null,false,"Individual"),
        new("lastName","LastName","text","",100,null,false,"Individual"),
        new("nationalId","NationalId","technical","",64,null,false,null),
        new("occupation","Occupation","text","",200,null,false,"Individual"),
        new("legalName","LegalName","text","",200,null,true,"LegalEntity"),
        new("tradeName","TradeName","text","",200,null,false,"LegalEntity"),
        new("registrationNo","RegistrationNo","technical","",64,null,false,"LegalEntity"),
        new("taxId","TaxId","technical","",64,null,false,null),
        new("legalEntityTypeId","LegalEntityTypeId","reference","",0,"legal.entity.type",false,"LegalEntity"),
        new("establishmentDate","EstablishmentDate","date","",0,null,false,"LegalEntity"),
        new("businessActivity","BusinessActivity","text","",500,null,false,null),
        new("primaryMobile","PrimaryMobile","technical","",16,null,false,null),
        new("primaryPhone","PrimaryPhone","technical","",16,null,false,null),
        new("primaryEmail","PrimaryEmail","email","",320,null,false,null),
        new("website","Website","text","",500,null,false,null),
        new("notes","Notes","textarea","",2000,null,false,null),
        new("gradeId","GradeId","reference","",0,"supplier.grade",false,null),
        new("isPreferred","IsPreferred","bool","false",0,null,false,null),
        new("cooperationStartDate","CooperationStartDate","date","",0,null,false,null),
        new("holdReasonId","HoldReasonId","reference","",0,"supplier.hold.reason",false,"Blocked")
    ];
    public static readonly SupplierUiField[] Financial = [
        new("paymentTermId","PaymentTermId","reference","",0,"payment.term",false,null),
        new("paymentConditionId","PaymentConditionId","reference","",0,"payment.condition",false,null),
        new("paymentMethodId","PaymentMethodId","reference","",0,"payment.method",false,null),
        new("defaultCurrencyCode","DefaultCurrencyCode","technical","IRR",3,null,true,null),
        new("minimumOrderAmount","MinimumOrderAmount","decimal","",0,null,false,null),
        new("defaultLeadTimeDays","DefaultLeadTimeDays","integer","",0,null,false,null),
        new("baseDiscountPercent","BaseDiscountPercent","decimal","0",0,null,false,null),
        new("creditPurchaseAllowed","CreditPurchaseAllowed","bool","false",0,null,false,null),
        new("purchaseCreditLimit","PurchaseCreditLimit","decimal","0",0,null,false,null),
        new("shippingMethodId","ShippingMethodId","reference","",0,"shipping.method",false,null),
        new("freightDeliveryTermId","FreightDeliveryTermId","reference","",0,"freight.delivery.term",false,null)
    ];
    public static readonly SupplierUiField[] Address = [
        new("addressName","AddressName","text","",100,null,true,null),
        new("addressTypeId","AddressTypeId","reference","",0,"address.type",true,null),
        new("countryCode","CountryCode","technical","IR",2,null,true,null),
        new("provinceCode","ProvinceCode","province","",80,null,false,null),
        new("cityCode","CityCode","city","",80,null,false,null),
        new("postalCode","PostalCode","technical","",32,null,false,null),
        new("fullAddress","FullAddress","textarea","",1000,null,true,null),
        new("landline","Landline","technical","",16,null,false,null),
        new("mobile","Mobile","technical","",16,null,false,null),
        new("email","Email","email","",320,null,false,null),
        new("recipientName","RecipientName","text","",200,null,false,null),
        new("notes","Notes","textarea","",2000,null,false,null),
        new("isDefault","IsDefault","bool","false",0,null,false,null),
        new("isActive","IsActive","bool","true",0,null,false,null)
    ];
    public static readonly SupplierUiField[] Contact = [
        new("firstName","FirstName","text","",100,null,true,null),
        new("lastName","LastName","text","",100,null,false,null),
        new("contactRoleId","ContactRoleId","reference","",0,"contact.role",true,null),
        new("positionTitle","PositionTitle","text","",150,null,false,null),
        new("landline","Landline","technical","",16,null,false,null),
        new("mobile","Mobile","technical","",16,null,false,null),
        new("extension","Extension","technical","",12,null,false,null),
        new("email","Email","email","",320,null,false,null),
        new("notes","Notes","textarea","",2000,null,false,null),
        new("isPrimary","IsPrimary","bool","false",0,null,false,null),
        new("isActive","IsActive","bool","true",0,null,false,null)
    ];
    public static readonly SupplierUiField[] Bank = [
        new("bankId","BankId","reference","",0,"bank",true,null),
        new("accountName","AccountName","text","",150,null,false,null),
        new("accountTypeId","AccountTypeId","reference","",0,"bank.account.type",false,null),
        new("ownershipType","OwnershipType","ownership","Individual",20,null,false,null),
        new("accountHolderNames","AccountHolderNames","text","",400,null,true,null),
        new("accountNumber","AccountNumber","technical","",64,null,false,null),
        new("iban","Iban","technical","",34,null,false,null),
        new("cardNumber","CardNumber","technical","",19,null,false,null),
        new("branchCode","BranchCode","technical","",40,null,false,null),
        new("branchName","BranchName","text","",150,null,false,null),
        new("currencyCode","CurrencyCode","technical","IRR",3,null,true,null),
        new("isDefault","IsDefault","bool","false",0,null,false,null),
        new("isActive","IsActive","bool","true",0,null,false,null),
        new("notes","Notes","textarea","",2000,null,false,null)
    ];
    public static SupplierUiField[] Children(string kind)=>kind switch{"addresses"=>Address,"contacts"=>Contact,"bank-accounts"=>Bank,_=>[]};
    public static Dictionary<string,string> Empty(IEnumerable<SupplierUiField> fields)=>fields.ToDictionary(x=>x.Key,x=>x.Default);
    public static string Text(JsonElement value,string name)=>value.TryGetProperty(name,out var p)&&p.ValueKind!=JsonValueKind.Null?p.ToString():"";
    public static Dictionary<string,string> Read(JsonElement value,IEnumerable<SupplierUiField> fields)=>fields.ToDictionary(x=>x.Key,x=>value.TryGetProperty(x.Key,out var p)&&p.ValueKind!=JsonValueKind.Null?p.ValueKind is JsonValueKind.True or JsonValueKind.False?p.GetBoolean().ToString().ToLowerInvariant():p.ToString():x.Kind is "bool" or "entity" or "status" or "ownership"?x.Default:"");
    public static Dictionary<string,object?> Body(IReadOnlyDictionary<string,string> data,IEnumerable<SupplierUiField> fields)=>fields.ToDictionary(x=>x.Key,x=>Native(data.GetValueOrDefault(x.Key,x.Default),x));
    private static object? Native(string value,SupplierUiField item)=>item.Kind switch{
        "reference"=>Guid.TryParse(value,out var id)?id:null,
        "bool"=>string.Equals(value,"true",StringComparison.OrdinalIgnoreCase),
        "integer"=>string.IsNullOrWhiteSpace(value)?null:int.Parse(value,CultureInfo.InvariantCulture),
        "decimal"=>string.IsNullOrWhiteSpace(value)?null:decimal.Parse(value,CultureInfo.InvariantCulture),
        "date"=>string.IsNullOrWhiteSpace(value)?null:value,
        _=>value};
    public static string Error(Api.ApiResult<JsonElement> result)=>result.ProblemCode switch{
        "supplier.invalid_reference"=>"InvalidReference","supplier.inactive_default"=>"InactiveDefault","supplier.referenced" or "organization.delete.protected_dependency"=>"DeleteProtected",
        "supplier.blocked"=>"BlockedReason","catalog.duplicate_code" or "core.database.unique_conflict"=>"Duplicate",
        _=>result.Failure switch{Api.ApiFailure.Forbidden=>"Forbidden",Api.ApiFailure.Conflict=>"Conflict",Api.ApiFailure.Unavailable=>"LoadError",_=>"ValidationError"}};
}
