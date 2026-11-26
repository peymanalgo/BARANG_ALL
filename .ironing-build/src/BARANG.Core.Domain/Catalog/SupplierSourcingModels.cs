using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Catalog;

public static class SupplierSourcingErrors
{
    public static Error InvalidData => Error.Validation("supplier.sourcing.invalid_data");
    public static Error InvalidReference => Error.BusinessRule("supplier.sourcing.invalid_reference");
    public static Error NotFound => Error.NotFound("supplier.sourcing.not_found");
    public static Error Concurrency => Error.Concurrency("supplier.sourcing.concurrency");
    public static Error Ineligible => Error.BusinessRule("supplier.sourcing.ineligible");
    public static Error Overlap => Error.Conflict("supplier.sourcing.price_overlap");
    public static Error Published => Error.BusinessRule("supplier.sourcing.published_immutable");
    public static Error Referenced => Error.BusinessRule("supplier.sourcing.referenced");
    public static Error EmptyList => Error.BusinessRule("supplier.sourcing.empty_list");
}
public sealed record SupplierSourcingData(Guid ProductId,Guid? ProductVariantId=null,Guid? PurchaseUnitId=null,
    Guid? PackagingTypeId=null,string? SupplierItemCode=null,string? SupplierItemName=null,
    string? Description=null,string? Brand=null,decimal? MinimumOrderQuantity=null,decimal? MinimumOrderAmount=null,
    int? LeadTimeDays=null,string Availability="Available",bool IsPreferred=false,bool IsActive=true,string? Notes=null);
public sealed record SupplierPriceTerms(Guid PurchaseUnitId,decimal UnitPrice,decimal MinimumQuantity=0,
    decimal DiscountPercent=0,Guid? TaxStatusId=null,decimal TaxRatePercent=0,decimal FreightPerUnit=0,string? Notes=null);
public sealed record SupplierPriceData(Guid SupplierProductId,string CurrencyCode,DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,SupplierPriceTerms Terms);
public sealed record SupplierPriceListData(string Code,string Title,string CurrencyCode,DateOnly EffectiveFrom,
    DateOnly? EffectiveTo=null,string? Notes=null);
public sealed record SupplierPriceListLineData(Guid SupplierProductId,SupplierPriceTerms Terms);

public static class SupplierSourcingRules
{
    public static bool Number(decimal value) => value>=0&&value<=99999999999999.9999m&&decimal.Round(value,4)==value;
    public static bool OptionalNumber(decimal? value) => value is null||Number(value.Value);
    public static string? Currency(string? value)
    {
        string code=value?.Trim().ToUpperInvariant()??"";
        return code.Length==3&&code.All(char.IsAsciiLetterUpper)?code:null;
    }
    private static bool Text(string? value,int max)=>value is null||value.Trim().Length<=max;
    public static bool Sourcing(SupplierSourcingData data)=>data is not null&&data.ProductId!=Guid.Empty&&
        (data.ProductVariantId is null||data.ProductVariantId!=Guid.Empty)&&
        (data.PurchaseUnitId is null||data.PurchaseUnitId!=Guid.Empty)&&
        (data.PackagingTypeId is null||data.PackagingTypeId!=Guid.Empty)&&
        (string.IsNullOrWhiteSpace(data.SupplierItemCode)||CatalogText.Code(data.SupplierItemCode,80) is not null)&&
        Text(data.SupplierItemName,200)&&Text(data.Description,2000)&&Text(data.Brand,150)&&Text(data.Notes,2000)&&
        OptionalNumber(data.MinimumOrderQuantity)&&OptionalNumber(data.MinimumOrderAmount)&&data.LeadTimeDays is not <0&&
        data.Availability is "Available" or "OnRequest" or "Unavailable";
    public static bool Terms(SupplierPriceTerms data)=>data is not null&&data.PurchaseUnitId!=Guid.Empty&&
        Number(data.UnitPrice)&&Number(data.MinimumQuantity)&&Number(data.DiscountPercent)&&data.DiscountPercent<=100&&
        Number(data.TaxRatePercent)&&data.TaxRatePercent<=100&&Number(data.FreightPerUnit)&&Text(data.Notes,2000);
    public static bool Period(DateOnly from,DateOnly? to)=>from!=default&&(to is null||to>=from);
    public static bool Header(SupplierPriceListData data)=>data is not null&&CatalogText.Code(data.Code,50) is not null&&
        CatalogText.Name(data.Title,200) is not null&&Currency(data.CurrencyCode) is not null&&Period(data.EffectiveFrom,data.EffectiveTo)&&Text(data.Notes,2000);
    public static decimal NetUnitCost(SupplierPriceTerms terms)
    {
        decimal discounted=terms.UnitPrice*(1-terms.DiscountPercent/100m);
        return decimal.Round(discounted*(1+terms.TaxRatePercent/100m)+terms.FreightPerUnit,4,MidpointRounding.AwayFromZero);
    }
    public static bool Overlaps(DateOnly from,DateOnly? to,DateOnly otherFrom,DateOnly? otherTo)=>
        (to is null||otherFrom<=to)&&(otherTo is null||from<=otherTo);
    public static bool Applies(DateOnly from,DateOnly? to,DateOnly day)=>from<=day&&(to is null||to>=day);
}
public sealed partial class SupplierProduct
{
    public Guid? PurchaseUnitId { get; private set; }
    public Guid? PackagingTypeId { get; private set; }
    public string? SupplierItemName { get; private set; }
    public string? Description { get; private set; }
    public string? Brand { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public string Availability { get; private set; }="Available";
    public string? Notes { get; private set; }
    public static Result<SupplierProduct> CreateSourcing(Guid id,Guid company,Guid supplier,SupplierSourcingData data,DateTimeOffset now)
    {
        if(!SupplierSourcingRules.Sourcing(data))return Result.Failure<SupplierProduct>(SupplierSourcingErrors.InvalidData);
        var result=Create(id,company,supplier,data.ProductId,data.ProductVariantId,data.SupplierItemCode,data.IsPreferred,data.MinimumOrderQuantity,data.LeadTimeDays,now);
        if(result.IsFailure)return result;
        result.Value.UpdateSourcing(data,now,false);return result;
    }
    public Result UpdateSourcing(SupplierSourcingData data,DateTimeOffset now,bool touch=true)
    {
        if(!SupplierSourcingRules.Sourcing(data)||data.ProductId!=ProductId||data.ProductVariantId!=ProductVariantId)
            return Result.Failure(SupplierSourcingErrors.InvalidData);
        var changed=Update(data.SupplierItemCode,data.IsPreferred,data.MinimumOrderQuantity,data.LeadTimeDays,now,false);
        if(changed.IsFailure)return changed;
        PurchaseUnitId=data.PurchaseUnitId;PackagingTypeId=data.PackagingTypeId;SupplierItemName=data.SupplierItemName?.Trim();
        Description=data.Description?.Trim();Brand=data.Brand?.Trim();MinimumOrderAmount=data.MinimumOrderAmount;
        Availability=data.Availability;Notes=data.Notes?.Trim();IsActive=data.IsActive;if(touch)Touch(now);return Result.Success();
    }
    public void SelectPreferred(bool preferred,DateTimeOffset now){if(IsPreferred!=preferred){IsPreferred=preferred;Touch(now);}}
}
public sealed class SupplierProductPrice : CatalogEntity
{
    private SupplierProductPrice() { }
    public Guid SupplierId { get; private set; }
    public Guid SupplierProductId { get; private set; }
    public Guid? PriceListId { get; private set; }
    public string CurrencyCode { get; private set; }="";
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public string Status=>PublishedAtUtc is null?"Draft":IsActive?"Active":"Inactive";
    public Guid PurchaseUnitId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public Guid? TaxStatusId { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal FreightPerUnit { get; private set; }
    public string? Notes { get; private set; }
    public SupplierPriceTerms Terms => new(PurchaseUnitId,UnitPrice,MinimumQuantity,DiscountPercent,TaxStatusId,TaxRatePercent,FreightPerUnit,Notes);
    private void ApplyTerms(SupplierPriceTerms terms)
    {
        PurchaseUnitId=terms.PurchaseUnitId;UnitPrice=terms.UnitPrice;MinimumQuantity=terms.MinimumQuantity;
        DiscountPercent=terms.DiscountPercent;TaxStatusId=terms.TaxStatusId;TaxRatePercent=terms.TaxRatePercent;
        FreightPerUnit=terms.FreightPerUnit;Notes=terms.Notes?.Trim();
    }
    public static Result<SupplierProductPrice> Create(Guid company,Guid supplier,SupplierPriceData data,DateTimeOffset now,Guid? priceListId=null)
    {
        if(company==Guid.Empty||supplier==Guid.Empty||data is null||data.SupplierProductId==Guid.Empty||
           SupplierSourcingRules.Currency(data.CurrencyCode) is null||!SupplierSourcingRules.Period(data.EffectiveFrom,data.EffectiveTo)||!SupplierSourcingRules.Terms(data.Terms))
            return Result.Failure<SupplierProductPrice>(SupplierSourcingErrors.InvalidData);
        var row=new SupplierProductPrice{SupplierId=supplier,SupplierProductId=data.SupplierProductId,PriceListId=priceListId,
            CurrencyCode=SupplierSourcingRules.Currency(data.CurrencyCode)!,EffectiveFrom=data.EffectiveFrom,EffectiveTo=data.EffectiveTo};
        row.Initialize(Guid.NewGuid(),company,now);row.IsActive=false;row.ApplyTerms(data.Terms);return Result.Success(row);
    }
    public Result Update(SupplierPriceData data,DateTimeOffset now)
    {
        if(PublishedAtUtc is not null)return Result.Failure(SupplierSourcingErrors.Published);
        if(data is null||data.SupplierProductId!=SupplierProductId||SupplierSourcingRules.Currency(data.CurrencyCode) is null||
           !SupplierSourcingRules.Period(data.EffectiveFrom,data.EffectiveTo)||!SupplierSourcingRules.Terms(data.Terms))
            return Result.Failure(SupplierSourcingErrors.InvalidData);
        CurrencyCode=SupplierSourcingRules.Currency(data.CurrencyCode)!;EffectiveFrom=data.EffectiveFrom;EffectiveTo=data.EffectiveTo;
        ApplyTerms(data.Terms);Touch(now);return Result.Success();
    }
    public void Publish(bool active,DateTimeOffset now){if(active)PublishedAtUtc??=now;IsActive=active;Touch(now);}
    public Result End(DateOnly date,DateTimeOffset now)
    {
        if(PublishedAtUtc is null||date<EffectiveFrom||date<DateOnly.FromDateTime(now.UtcDateTime)||(EffectiveTo is not null&&date>EffectiveTo))
            return Result.Failure(SupplierSourcingErrors.InvalidData);
        EffectiveTo=date;Touch(now);return Result.Success();
    }
}
public sealed class SupplierPriceList : CatalogEntity
{
    private SupplierPriceList() { }
    public Guid SupplierId { get; private set; }
    public string Code { get; private set; }="";
    public string Title { get; private set; }="";
    public string CurrencyCode { get; private set; }="";
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public string Status=>PublishedAtUtc is null?"Draft":IsActive?"Active":"Inactive";
    public static Result<SupplierPriceList> Create(Guid company,Guid supplier,SupplierPriceListData data,DateTimeOffset now)
    {
        if(company==Guid.Empty||supplier==Guid.Empty||!SupplierSourcingRules.Header(data))return Result.Failure<SupplierPriceList>(SupplierSourcingErrors.InvalidData);
        var row=new SupplierPriceList{SupplierId=supplier,Code=CatalogText.Code(data.Code,50)!};
        row.Initialize(Guid.NewGuid(),company,now);row.IsActive=false;row.Apply(data);return Result.Success(row);
    }
    private void Apply(SupplierPriceListData data){Title=data.Title.Trim();CurrencyCode=SupplierSourcingRules.Currency(data.CurrencyCode)!;EffectiveFrom=data.EffectiveFrom;EffectiveTo=data.EffectiveTo;Notes=data.Notes?.Trim();}
    public Result Update(SupplierPriceListData data,DateTimeOffset now)
    {
        if(PublishedAtUtc is not null)return Result.Failure(SupplierSourcingErrors.Published);
        if(!SupplierSourcingRules.Header(data)||CatalogText.Code(data.Code,50)!=Code)return Result.Failure(SupplierSourcingErrors.InvalidData);
        Apply(data);Touch(now);return Result.Success();
    }
    public void Publish(bool active,DateTimeOffset now){if(active)PublishedAtUtc??=now;IsActive=active;Touch(now);}
    public void LinesChanged(DateTimeOffset now)=>Touch(now);
}
public sealed class SupplierPriceListLine : CatalogEntity
{
    private SupplierPriceListLine() { }
    public Guid SupplierId { get; private set; }
    public Guid PriceListId { get; private set; }
    public Guid SupplierProductId { get; private set; }
    public Guid? PublishedPriceId { get; private set; }
    public Guid PurchaseUnitId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public Guid? TaxStatusId { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal FreightPerUnit { get; private set; }
    public string? Notes { get; private set; }
    public SupplierPriceTerms Terms => new(PurchaseUnitId,UnitPrice,MinimumQuantity,DiscountPercent,TaxStatusId,TaxRatePercent,FreightPerUnit,Notes);
    private void ApplyTerms(SupplierPriceTerms terms)
    {
        PurchaseUnitId=terms.PurchaseUnitId;UnitPrice=terms.UnitPrice;MinimumQuantity=terms.MinimumQuantity;
        DiscountPercent=terms.DiscountPercent;TaxStatusId=terms.TaxStatusId;TaxRatePercent=terms.TaxRatePercent;
        FreightPerUnit=terms.FreightPerUnit;Notes=terms.Notes?.Trim();
    }
    public static Result<SupplierPriceListLine> Create(SupplierPriceList header,SupplierPriceListLineData data,DateTimeOffset now)
    {
        if(data is null||data.SupplierProductId==Guid.Empty||!SupplierSourcingRules.Terms(data.Terms))return Result.Failure<SupplierPriceListLine>(SupplierSourcingErrors.InvalidData);
        if(header.PublishedAtUtc is not null)return Result.Failure<SupplierPriceListLine>(SupplierSourcingErrors.Published);
        var row=new SupplierPriceListLine{SupplierId=header.SupplierId,PriceListId=header.Id,SupplierProductId=data.SupplierProductId};
        row.Initialize(Guid.NewGuid(),header.CompanyId,now);row.ApplyTerms(data.Terms);return Result.Success(row);
    }
    public Result Update(SupplierPriceListLineData data,DateTimeOffset now)
    {
        if(PublishedPriceId is not null)return Result.Failure(SupplierSourcingErrors.Published);
        if(data is null||data.SupplierProductId!=SupplierProductId||!SupplierSourcingRules.Terms(data.Terms))return Result.Failure(SupplierSourcingErrors.InvalidData);
        ApplyTerms(data.Terms);Touch(now);return Result.Success();
    }
    public void BindPublishedPrice(Guid id,DateTimeOffset now){PublishedPriceId=id;Touch(now);}
}
