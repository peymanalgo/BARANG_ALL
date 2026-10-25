using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BARANG.TestSupport;

// Shared API fixture only. The production Supplier module is deliberately unchanged.
public sealed class SupplierReferenceScenario(HttpClient client, string token, Guid company, CancellationToken cancellationToken)
{
    public static readonly string[] Types =
    [
        "legal.entity.type","contact.role","address.type","payment.term","payment.condition",
        "payment.method","bank","bank.account.type","price.group","tax.status","shipping.method","packaging.type","unit.of.measure"
    ];
    // These catalogs are consumed as stable business codes in the current model, not FK-backed
    // relationships.  Only the FK-backed values can be used by the database-level protected
    // deletion assertion below.
    public static readonly string[] ReferencedTypes =
    [
        "legal.entity.type","contact.role","address.type","payment.term","payment.condition",
        "payment.method","bank","bank.account.type","tax.status"
    ];
    public static readonly string[] WorkspaceTypes = ["supplier.category","supplier.grade","freight.delivery.term","supplier.hold.reason"];
    public static bool Global(string type) => type is "bank" or "payment.method";
    public Dictionary<string, JsonElement> References { get; } = [];
    public Guid? CustomerId { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? OrderId { get; private set; }
    public SupplierReferenceScenario InCompany(Guid target) => new(client, token, target, cancellationToken);
    public static Guid Id(JsonElement value) => value.GetProperty("id").GetGuid();
    public static long Version(JsonElement value) => value.GetProperty("version").GetInt64();
    public static Dictionary<string,string> Captions(string suffix) => new()
    {
        ["fa"]="\u0645\u0631\u062c\u0639 "+suffix,
        ["ar"]="\u0645\u0631\u062c\u0639\u064a "+suffix,
        ["en"]="Reference "+suffix
    };
    public Task<JsonElement> Send(HttpMethod method, string path, object? body=null, int expected=200)
        => Send(client, method, path, token, company, body, expected, cancellationToken);
    public static async Task<JsonElement> Send(HttpClient client, HttpMethod method, string path, string? token, Guid? company,
        object? body, int expected, CancellationToken ct)
    {
        using HttpRequestMessage request=new(method,path);
        if(token is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        if(company is not null)request.Headers.Add("X-Company-ID",company.ToString());
        if(body is not null)request.Content=JsonContent.Create(body);
        using HttpResponseMessage response=await client.SendAsync(request,ct);
        int status=(int)response.StatusCode;
        if(status!=expected)
            throw new InvalidOperationException($"{method} {path}: expected {expected}, received {status}.");
        string bodyText=await response.Content.ReadAsStringAsync(ct);
        if(string.IsNullOrWhiteSpace(bodyText)&&status is 401 or 403 or 204)
            return JsonSerializer.SerializeToElement(new{});
        using JsonDocument document=JsonDocument.Parse(bodyText);
        return document.RootElement.Clone();
    }
    public Task<JsonElement> CreateValue(string type,string code,int order=0)
        => Send(HttpMethod.Post,$"/api/v1/lookups/types/{type}/values",new{code,displayOrder=order,translations=Captions(code)},201);
    public Task<JsonElement> Value(Guid id)=>Send(HttpMethod.Get,$"/api/v1/lookups/values/{id}");
    public Task<JsonElement> Options(string type,string culture,bool active)
        =>Send(HttpMethod.Get,$"/api/v1/lookups/{type}/options?culture={culture}&activeOnly={active.ToString().ToLowerInvariant()}");
    public Task<JsonElement> Activate(JsonElement value,bool active)
        =>Send(HttpMethod.Post,$"/api/v1/lookups/values/{Id(value)}/activation",new{isActive=active,version=Version(value)});
    public Task<JsonElement> Delete(JsonElement value,int expected=200)
        =>Send(HttpMethod.Delete,$"/api/v1/lookups/values/{Id(value)}?version={Version(value)}",expected:expected);

    public async Task SeedDependencies(Guid branch,string stamp)
    {
        foreach(string type in Types)
            References.Add(type,await CreateValue(type,$"srdep.{stamp}.{References.Count:D2}"));
        Guid Ref(string type)=>Id(References[type]);
        string Code(string type)=>References[type].GetProperty("code").GetString()!;
        JsonElement customer=await Send(HttpMethod.Post,"/api/v1/customers",new{
            customerCode="SRDEP-"+stamp,customerType="LegalEntity",legalName="Supplier reference acceptance "+stamp,
            primaryMobile="09120000077",legalEntityTypeId=Ref("legal.entity.type"),legalEntityTypeCode=Code("legal.entity.type")
        },201);
        CustomerId=Id(customer);
        await Send(HttpMethod.Post,$"/api/v1/customers/{CustomerId}/contacts",new{
            contactRoleId=Ref("contact.role"),contactTypeCode=Code("contact.role"),firstName="Reference",lastName="Fixture",mobile="09120000078"
        },201);
        await Send(HttpMethod.Post,$"/api/v1/customers/{CustomerId}/addresses",new{
            addressTypeId=Ref("address.type"),addressTypeCode=Code("address.type"),title="Reference fixture",
            countryCode="IR",addressLine1="Synthetic supplier reference acceptance address",isDefault=false
        },201);
        await Send(HttpMethod.Put,$"/api/v1/customers/{CustomerId}/financial",new{
            paymentTermId=Ref("payment.term"),paymentTermCode=Code("payment.term"),
            paymentConditionId=Ref("payment.condition"),paymentConditionCode=Code("payment.condition"),
            preferredPaymentMethodId=Ref("payment.method"),preferredPaymentMethodCode=Code("payment.method"),
            taxStatusId=Ref("tax.status"),taxStatusCode=Code("tax.status"),financialStatus="Normal",defaultCurrencyCode="IRR"
        });
        await Send(HttpMethod.Post,$"/api/v1/customers/{CustomerId}/bank-accounts",new{
            bankId=Ref("bank"),bankCode=Code("bank"),accountTypeId=Ref("bank.account.type"),accountTypeCode=Code("bank.account.type"),
            accountHolderName="Synthetic Reference Fixture",accountNumber="123456789",currencyCode="IRR",ownershipType="Individual"
        },201);
        ProductId=Id(await Send(HttpMethod.Post,"/api/v1/products",new{
            productCode="SRDEP-"+stamp,name="Supplier reference unit fixture "+stamp,productTypeCode="APPAREL",unitOfMeasureCode=Code("unit.of.measure")
        },201));
        OrderId=Id(await Send(HttpMethod.Post,"/api/v1/orders",new{
            branchId=branch,customerId=CustomerId,source="Internal",orderDate=DateTime.UtcNow.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture),
            externalReference="SUPPLIER-REFERENCE-ACCEPTANCE-"+stamp
        },201));
        await Send(HttpMethod.Put,$"/api/v1/orders/{OrderId}/business-profile",new{
            shippingMethodCode=Code("shipping.method"),packagingTypeCode=Code("packaging.type"),
            notes="Synthetic dependency fixture; no production or purchasing operation."
        });
    }

    // Business history must not be deleted to clean a test. Retire only this fixture's records.
    public async Task RetireDependencies()
    {
        if(OrderId is Guid order)
        {
            JsonElement row=await Send(HttpMethod.Get,$"/api/v1/orders/{order}");
            if(row.GetProperty("status").GetString()=="Draft")
                await Send(HttpMethod.Post,$"/api/v1/orders/{order}/cancel",new{version=Version(row),reasonCode="SUPPLIER_REFERENCE_ACCEPTANCE",reasonText="Supplier reference acceptance fixture retired"});
        }
        if(CustomerId is Guid customer)
        {
            JsonElement row=await Send(HttpMethod.Get,$"/api/v1/customers/{customer}");
            await Send(HttpMethod.Post,$"/api/v1/customers/{customer}/status",new{status="Inactive",version=Version(row)});
        }
        if(ProductId is Guid product)
        {
            JsonElement row=await Send(HttpMethod.Get,$"/api/v1/products/{product}");
            await Send(HttpMethod.Post,$"/api/v1/products/{product}/activation",new{isActive=false,version=Version(row)});
        }
        foreach(JsonElement value in References.Values)await Activate(await Value(Id(value)),false);
    }
}
