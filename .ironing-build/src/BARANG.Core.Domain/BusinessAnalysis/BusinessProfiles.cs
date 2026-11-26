using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.BusinessAnalysis;

public static class BusinessProfileErrors
{
    public static Error Invalid => Error.Validation("business_profile.invalid_data");
    public static Error NotFound => Error.NotFound("business_profile.not_found");
    public static Error Concurrency => Error.Concurrency("business_profile.concurrency_conflict");
    public static Error ParentInvalid => Error.BusinessRule("business_profile.invalid_parent");
}

internal static class BusinessProfileText
{
    public static string? Code(string? value, int max = 50, bool optional = true)
    {
        string text = value?.Trim().ToUpperInvariant() ?? "";
        if (text.Length == 0) return optional ? null : null;
        return text.Length <= max && text.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.') ? text : null;
    }
    public static string? Text(string? value, int max)
    {
        string text = value?.Trim() ?? "";
        return text.Length == 0 ? null : text.Length <= max ? text : null;
    }
}

public sealed class CustomerBusinessProfile
{
    private CustomerBusinessProfile() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? EconomicCode { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public string? BusinessDetails { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<CustomerBusinessProfile> Create(Guid company, Guid customer, DateOnly? birth, string? economic, decimal credit, int terms, string? details, DateTimeOffset now)
    { var x=new CustomerBusinessProfile{Id=Guid.NewGuid(),CompanyId=company,CustomerId=customer,Version=1};Result r=x.Apply(birth,economic,credit,terms,details,now,false);return r.IsFailure?Result.Failure<CustomerBusinessProfile>(r.Error):Result.Success(x); }
    public Result Update(DateOnly? birth,string? economic,decimal credit,int terms,string? details,long expected,DateTimeOffset now)=>expected!=Version?Result.Failure(BusinessProfileErrors.Concurrency):Apply(birth,economic,credit,terms,details,now,true);
    private Result Apply(DateOnly? birth,string? economic,decimal credit,int terms,string? details,DateTimeOffset now,bool touch){string?e=BusinessProfileText.Code(economic,64),d=BusinessProfileText.Text(details,1000);if(CompanyId==Guid.Empty||CustomerId==Guid.Empty||credit<0||terms is <0 or >3650||(!string.IsNullOrWhiteSpace(economic)&&e is null)||(!string.IsNullOrWhiteSpace(details)&&d is null)||birth>DateOnly.FromDateTime(now.UtcDateTime))return Result.Failure(BusinessProfileErrors.Invalid);DateOfBirth=birth;EconomicCode=e;CreditLimit=credit;PaymentTermsDays=terms;BusinessDetails=d;UpdatedAtUtc=now;if(touch)Version++;return Result.Success();}
}

public sealed class ProductTechnicalProfile
{
    private ProductTechnicalProfile() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid ProductId { get; private set; } public Guid? ProductVariantId { get; private set; }
    public string? BrandCode { get; private set; } public string? MaterialCode { get; private set; } public string? GenderCode { get; private set; } public string? FitCode { get; private set; } public string? StyleCode { get; private set; } public string? TechnicalAttributes { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<ProductTechnicalProfile>Create(Guid company,Guid product,Guid?variant,string?brand,string?material,string?gender,string?fit,string?style,string?attributes,DateTimeOffset now){var x=new ProductTechnicalProfile{Id=Guid.NewGuid(),CompanyId=company,ProductId=product,ProductVariantId=variant,Version=1};Result r=x.Apply(brand,material,gender,fit,style,attributes,now,false);return r.IsFailure?Result.Failure<ProductTechnicalProfile>(r.Error):Result.Success(x);}
    public Result Update(string?brand,string?material,string?gender,string?fit,string?style,string?attributes,long expected,DateTimeOffset now)=>expected!=Version?Result.Failure(BusinessProfileErrors.Concurrency):Apply(brand,material,gender,fit,style,attributes,now,true);
    private Result Apply(string?brand,string?material,string?gender,string?fit,string?style,string?attributes,DateTimeOffset now,bool touch){string?b=BusinessProfileText.Code(brand),m=BusinessProfileText.Code(material),g=BusinessProfileText.Code(gender),f=BusinessProfileText.Code(fit),s=BusinessProfileText.Code(style),a=BusinessProfileText.Text(attributes,2000);if(CompanyId==Guid.Empty||ProductId==Guid.Empty||new[]{(brand,b),(material,m),(gender,g),(fit,f),(style,s)}.Any(x=>!string.IsNullOrWhiteSpace(x.Item1)&&x.Item2 is null)||(!string.IsNullOrWhiteSpace(attributes)&&a is null))return Result.Failure(BusinessProfileErrors.Invalid);BrandCode=b;MaterialCode=m;GenderCode=g;FitCode=f;StyleCode=s;TechnicalAttributes=a;UpdatedAtUtc=now;if(touch)Version++;return Result.Success();}
}

public sealed class OrderBusinessProfile
{
    private OrderBusinessProfile() { }
    public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid OrderId{get;private set;}public string?ShippingMethodCode{get;private set;}public string?PackagingTypeCode{get;private set;}public bool IncludeInvoiceInPackage{get;private set;}public string?OrderTypeCode{get;private set;}public string?Notes{get;private set;}public DateTimeOffset UpdatedAtUtc{get;private set;}public long Version{get;private set;}
    public static Result<OrderBusinessProfile>Create(Guid company,Guid order,string?shipping,string?packaging,bool invoice,string?type,string?notes,DateTimeOffset now){var x=new OrderBusinessProfile{Id=Guid.NewGuid(),CompanyId=company,OrderId=order,Version=1};Result r=x.Apply(shipping,packaging,invoice,type,notes,now,false);return r.IsFailure?Result.Failure<OrderBusinessProfile>(r.Error):Result.Success(x);}
    public Result Update(string?shipping,string?packaging,bool invoice,string?type,string?notes,long expected,DateTimeOffset now)=>expected!=Version?Result.Failure(BusinessProfileErrors.Concurrency):Apply(shipping,packaging,invoice,type,notes,now,true);
    private Result Apply(string?shipping,string?packaging,bool invoice,string?type,string?notes,DateTimeOffset now,bool touch){string?s=BusinessProfileText.Code(shipping),p=BusinessProfileText.Code(packaging),t=BusinessProfileText.Code(type),n=BusinessProfileText.Text(notes,2000);if(CompanyId==Guid.Empty||OrderId==Guid.Empty||new[]{(shipping,s),(packaging,p),(type,t)}.Any(x=>!string.IsNullOrWhiteSpace(x.Item1)&&x.Item2 is null)||(!string.IsNullOrWhiteSpace(notes)&&n is null))return Result.Failure(BusinessProfileErrors.Invalid);ShippingMethodCode=s;PackagingTypeCode=p;IncludeInvoiceInPackage=invoice;OrderTypeCode=t;Notes=n;UpdatedAtUtc=now;if(touch)Version++;return Result.Success();}
}

public sealed class PrintTechnicalProfile
{
    private PrintTechnicalProfile() { }
    public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid OrderPrintOperationId{get;private set;}public string?DirectionCode{get;private set;}public string?PrintSizeCode{get;private set;}public int PrintCount{get;private set;}public Guid?PrinterEquipmentId{get;private set;}public Guid?HeatPressEquipmentId{get;private set;}public string?Specifications{get;private set;}public DateTimeOffset UpdatedAtUtc{get;private set;}public long Version{get;private set;}
    public static Result<PrintTechnicalProfile>Create(Guid company,Guid operation,string?direction,string?size,int count,Guid?printer,Guid?press,string?specifications,DateTimeOffset now){var x=new PrintTechnicalProfile{Id=Guid.NewGuid(),CompanyId=company,OrderPrintOperationId=operation,Version=1};Result r=x.Apply(direction,size,count,printer,press,specifications,now,false);return r.IsFailure?Result.Failure<PrintTechnicalProfile>(r.Error):Result.Success(x);}
    public Result Update(string?direction,string?size,int count,Guid?printer,Guid?press,string?specifications,long expected,DateTimeOffset now)=>expected!=Version?Result.Failure(BusinessProfileErrors.Concurrency):Apply(direction,size,count,printer,press,specifications,now,true);
    private Result Apply(string?direction,string?size,int count,Guid?printer,Guid?press,string?specifications,DateTimeOffset now,bool touch){string?d=BusinessProfileText.Code(direction),s=BusinessProfileText.Code(size),x=BusinessProfileText.Text(specifications,2000);if(CompanyId==Guid.Empty||OrderPrintOperationId==Guid.Empty||count<1||(!string.IsNullOrWhiteSpace(direction)&&d is null)||(!string.IsNullOrWhiteSpace(size)&&s is null)||(!string.IsNullOrWhiteSpace(specifications)&&x is null))return Result.Failure(BusinessProfileErrors.Invalid);DirectionCode=d;PrintSizeCode=s;PrintCount=count;PrinterEquipmentId=printer;HeatPressEquipmentId=press;Specifications=x;UpdatedAtUtc=now;if(touch)Version++;return Result.Success();}
}

public sealed class OrderItemTechnicalSnapshot
{
    private OrderItemTechnicalSnapshot() { }
    public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid OrderItemId{get;private set;}public string?BrandCode{get;private set;}public string?MaterialCode{get;private set;}public string?GenderCode{get;private set;}public string?FitCode{get;private set;}public string?StyleCode{get;private set;}public string?TechnicalAttributes{get;private set;}public DateTimeOffset CreatedAtUtc{get;private set;}
    public static OrderItemTechnicalSnapshot Create(Guid company,Guid item,ProductTechnicalProfile? product,ProductTechnicalProfile? variant,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,OrderItemId=item,BrandCode=variant?.BrandCode??product?.BrandCode,MaterialCode=variant?.MaterialCode??product?.MaterialCode,GenderCode=variant?.GenderCode??product?.GenderCode,FitCode=variant?.FitCode??product?.FitCode,StyleCode=variant?.StyleCode??product?.StyleCode,TechnicalAttributes=variant?.TechnicalAttributes??product?.TechnicalAttributes,CreatedAtUtc=now};
}
