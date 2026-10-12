using System.Globalization;
using System.Text.Json;
using BARANG.Backoffice.Api;

namespace BARANG.Backoffice.Components;

public static class SupplierSourcingUiSchema
{
    public static readonly SupplierUiField[] Product=[
        new("supplierItemCode","SupplierItemCode","technical",MaxLength:80),
        new("supplierItemName","SupplierItemName",MaxLength:200),
        new("description","Description","textarea",MaxLength:2000),
        new("brand","SupplierBrand",MaxLength:150),
        new("purchaseUnitId","PurchaseUnit","reference",Reference:"unit.of.measure",Required:true),
        new("packagingTypeId","PackagingType","reference",Reference:"packaging.type"),
        new("minimumOrderQuantity","MOQ","decimal",Scale:4),
        new("minimumOrderAmount","MinimumOrderAmount","decimal",Scale:4),
        new("leadTimeDays","LeadTimeDays","integer"),
        new("isPreferred","PreferredForItem","bool","false"),
        new("isActive","IsActive","bool","true"),
        new("notes","Notes","textarea",MaxLength:2000)];
    public static readonly SupplierUiField[] Terms=[
        new("purchaseUnitId","PurchaseUnit","reference",Reference:"unit.of.measure",Required:true),
        new("unitPrice","UnitPrice","decimal","0",Required:true,Scale:4),
        new("minimumQuantity","MinimumQuantity","decimal","0",Scale:4),
        new("discountPercent","DiscountPercent","decimal","0",Scale:4),
        new("taxStatusId","TaxTreatment","reference",Reference:"tax.status"),
        new("taxRatePercent","TaxRatePercent","decimal","0",Scale:4),
        new("freightPerUnit","FreightPerUnit","decimal","0",Scale:4),
        new("notes","Notes","textarea",MaxLength:2000)];
    public static readonly SupplierUiField[] Period=[
        new("effectiveFrom","EffectiveFrom","date",Required:true),
        new("effectiveTo","EffectiveTo","date")];
    public static readonly SupplierUiField[] Header=[
        new("code","PriceListCode","technical",MaxLength:50,Required:true),
        new("title","PriceListTitle",MaxLength:200,Required:true),
        ..Period,new("notes","Notes","textarea",MaxLength:2000)];
    public static SupplierUiField[] Fields(string kind)=>kind switch{"products"=>Product,"prices"=>[..Period,..Terms],"price-lists"=>Header,_=>Terms};
    public static Dictionary<string,string> Empty(string kind,string currency)
    {
        var result=SupplierUiSchema.Empty(Fields(kind));
        result["productId"]=result["productVariantId"]=result["supplierProductId"]="";
        result["currencyCode"]=currency;result["availability"]="Available";
        if(result.ContainsKey("effectiveFrom"))result["effectiveFrom"]=DateTime.UtcNow.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        return result;
    }
    public static Dictionary<string,string> Read(string kind,JsonElement row)
    {
        var result=SupplierUiSchema.Read(row,Fields(kind));
        foreach(string key in new[]{"productId","productVariantId","supplierProductId","currencyCode","availability"})
            result[key]=SupplierUiSchema.Text(row,key);
        return result;
    }
    public static object Body(string kind,Dictionary<string,string> values)
    {
        if(kind=="products"){
            var result=SupplierUiSchema.Body(values,Product);result["productId"]=Guid.Parse(values["productId"]);
            result["productVariantId"]=Guid.TryParse(values["productVariantId"],out var variant)?variant:null;
            result["availability"]=values["availability"];return result;
        }
        if(kind=="price-lists"){var result=SupplierUiSchema.Body(values,Header);result["currencyCode"]=values["currencyCode"];return result;}
        var body=new Dictionary<string,object?>{{"supplierProductId",Guid.Parse(values["supplierProductId"])},{"terms",SupplierUiSchema.Body(values,Terms)}};
        if(kind=="prices"){body["currencyCode"]=values["currencyCode"];body["effectiveFrom"]=values["effectiveFrom"];body["effectiveTo"]=string.IsNullOrWhiteSpace(values["effectiveTo"])?null:values["effectiveTo"];}
        return body;
    }
    public static string Error(ApiResult<JsonElement> result)=>result.ProblemCode switch{
        "supplier.sourcing.price_overlap"=>"PriceOverlap","supplier.sourcing.published_immutable"=>"PublishedImmutable",
        "supplier.sourcing.referenced" or "supplier.referenced" or "organization.delete.protected_dependency"=>"DeleteProtected",
        "supplier.sourcing.invalid_reference"=>"InvalidReference","supplier.sourcing.ineligible"=>"Ineligible",
        "supplier.sourcing.empty_list"=>"EmptyList","catalog.duplicate_relationship" or "core.database.unique_conflict"=>"Duplicate",
        _=>result.Failure switch{ApiFailure.Forbidden=>"Forbidden",ApiFailure.Conflict=>"Conflict",ApiFailure.Unavailable=>"LoadError",_=>"ValidationError"}};
}
