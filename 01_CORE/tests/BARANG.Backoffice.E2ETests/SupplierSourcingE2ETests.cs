using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using BARANG.TestSupport;
using Microsoft.Playwright;
using static BARANG.TestSupport.SupplierReferenceScenario;

namespace BARANG.Backoffice.E2ETests;

public sealed class SupplierSourcingE2ETests:IAsyncLifetime
{
    private static readonly Guid Company=Guid.Parse("42f0e459-6095-45bb-aa13-f38126d91d9c"),Other=Guid.Parse("d82f44d5-8d22-4154-8eed-e7075ee6ce9f");
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow);
    private readonly string stamp=Guid.NewGuid().ToString("N")[..9];
    private readonly string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
    private readonly HttpClient client=new(){BaseAddress=new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL")??"http://localhost:8080")};
    private IPlaywright playwright=null!;private IBrowser browser=null!;private IBrowserContext context=null!;private IPage page=null!;private SupplierReferenceScenario api=null!;
    private readonly Dictionary<string,JsonElement> references=[];
    private readonly List<(Guid Company,Guid Id)> suppliers=[],products=[];
    private Guid supplier,product,variant;
    private string supplierCode="",productCode="",productName="",sku="",culture="en";
    private ILocator RootEditor=>page.Locator("[data-supplier-editor]");
    private ILocator Grid(string kind)=>page.Locator($"[data-sourcing-grid='{kind}']");
    private ILocator Editor(string kind)=>page.Locator($"[data-sourcing-editor='{kind}']");
    private static JsonElement Record(JsonElement value,string kind)=>value.GetProperty(kind=="products"?"relationship":kind=="prices"?"price":kind=="price-lists"?"priceList":"line");
    private static string ApiRoot(Guid id)=>$"/api/v1/suppliers/{id}/sourcing";
    public async ValueTask InitializeAsync(){
        string username=Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??throw new InvalidOperationException("E2E username required");
        string password=Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??throw new InvalidOperationException("E2E password required");
        var login=await SupplierReferenceScenario.Send(client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username,password},200,Ct);
        api=new(client,login.GetProperty("accessToken").GetString()!,Company,Ct);
        foreach(string type in new[]{"unit.of.measure","packaging.type","tax.status"})references[type]=await api.CreateValue(type,"source."+stamp+"."+references.Count.ToString(CultureInfo.InvariantCulture));
        supplierCode="SOURCE-E2E-"+stamp;supplier=await SeedSupplier(api,Company,supplierCode);
        productCode="SOURCE-P-"+stamp;productName="Sourcing product "+stamp;sku="SOURCE-SKU-"+stamp;
        var productRow=await api.Send(HttpMethod.Post,"/api/v1/products",new{productCode,name=productName,productTypeCode="RAW",unitOfMeasureCode=references["unit.of.measure"].GetProperty("code").GetString()},201);
        product=Id(productRow);productCode=productRow.GetProperty("productCode").GetString()!;products.Add((Company,product));
        var variantRow=await api.Send(HttpMethod.Post,$"/api/v1/products/{product}/variants",new{sku,displayName="Sourcing SKU "+stamp},201);
        variant=Id(variantRow);sku=variantRow.GetProperty("sku").GetString()!;
        playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});
        context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Flogin");
        await Ready(".language-switch a[hreflang='fa']","click");
        await page.Locator("#username").FillAsync(username);await page.Locator("#password").FillAsync(password);Task loginNavigation=page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/login",new(){WaitUntil=WaitUntilState.Commit});await page.Locator("button[type=submit]").ClickAsync();await loginNavigation;await SelectCompany(Company);
    }
    public async ValueTask DisposeAsync(){
        try{
            foreach(var(scope,id)in suppliers){
                var owner=api.InCompany(scope);
                foreach(string kind in new[]{"prices","price-lists"}){
                    var data=await owner.Send(HttpMethod.Get,ApiRoot(id)+"/"+kind+"?pageSize=100");
                    foreach(var entry in data.GetProperty("items").EnumerateArray()){
                        var row=Record(entry,kind);
                        if(row.GetProperty("isActive").GetBoolean()&&(kind!="prices"||row.GetProperty("priceListId").ValueKind==JsonValueKind.Null))
                            await owner.Send(HttpMethod.Post,ApiRoot(id)+"/"+kind+"/"+Id(row)+"/activation",new{isActive=false,version=Version(row)});
                    }
                }
                var links=await owner.Send(HttpMethod.Get,ApiRoot(id)+"/products?pageSize=100");
                foreach(var item in links.GetProperty("items").EnumerateArray()){
                    var row=Record(item,"products");if(row.GetProperty("isActive").GetBoolean())
                        await owner.Send(HttpMethod.Post,ApiRoot(id)+"/products/"+Id(row)+"/activation",new{isActive=false,version=Version(row)});
                }
                var root=(await owner.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}")).GetProperty("supplier");
                await owner.Send(HttpMethod.Post,$"/api/v1/suppliers/{id}/status",new{status="Inactive",version=Version(root)});
            }
            foreach(var(scope,id)in products){var owner=api.InCompany(scope);var row=await owner.Send(HttpMethod.Get,$"/api/v1/products/{id}");await owner.Send(HttpMethod.Post,$"/api/v1/products/{id}/activation",new{isActive=false,version=Version(row)});}
            foreach(var value in references.Values)await api.Activate(await api.Value(Id(value)),false);
            await api.Send(HttpMethod.Post,"/api/v1/auth/logout",expected:204);
        }finally{if(context is not null)await context.DisposeAsync();if(browser is not null)await browser.DisposeAsync();playwright?.Dispose();client.Dispose();}
    }
    [Theory][InlineData("fa")][InlineData("ar")][InlineData("en")]
    public async Task ProductSkuCrudPriceHistoryDatesFontsAndMobileWork(string language){
        culture=language;await Switch(language);await OpenSupplier(supplierCode);await Tab("products");
        await Grid("products").Locator("[data-sourcing-add]").ClickAsync();await Assertions.Expect(Editor("products")).ToBeVisibleAsync();
        await Choose(Editor("products"),"productId",productName,productName+" - "+productCode);
        await Choose(Editor("products"),"productVariantId",sku,sku+" - Sourcing SKU "+stamp);
        await ChooseReference(Editor("products"),"purchaseUnitId","unit.of.measure");await ChooseReference(Editor("products"),"packagingTypeId","packaging.type");
        await Input(Editor("products"),"supplierItemCode","ITEM-"+stamp);await Input(Editor("products"),"supplierItemName","Supplier item "+stamp);
        await Input(Editor("products"),"minimumOrderQuantity","2");await Input(Editor("products"),"leadTimeDays","7");await Input(Editor("products"),"notes","Source notes");
        await Save(Editor("products"));await Assertions.Expect(Grid("products").Locator("tbody tr")).ToHaveCountAsync(1);
        Guid link=Id(Record(Assert.Single((await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/products")).GetProperty("items").EnumerateArray()),"products"));
        var row=Grid("products").Locator($"tr[data-id='{link}']");await Action(row,"eye").ClickAsync();
        await Assertions.Expect(Editor("products").Locator("[name=notes]")).ToBeDisabledAsync();await Assertions.Expect(Editor("products").Locator("[name=notes]")).ToHaveValueAsync("Source notes");await Cancel(Editor("products"));
        await Action(row,"edit").ClickAsync();await Input(Editor("products"),"notes","Discard these notes");await Cancel(Editor("products"),true);
        Assert.Equal("Source notes",Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/products/"+link),"products").GetProperty("notes").GetString());
        await Action(row,"edit").ClickAsync();await Input(Editor("products"),"notes","Persisted sourcing notes");await Input(Editor("products"),"leadTimeDays","9");await Save(Editor("products"));
        foreach(bool active in new[]{false,true}){
            await Action(row,"power").ClickAsync();await Assertions.Expect(Action(row,"power")).ToHaveAttributeAsync("aria-label",Caption("SupplierStrings",active?"Deactivate":"Activate",culture));
            Assert.Equal(active,Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/products/"+link),"products").GetProperty("isActive").GetBoolean());
        }
        Assert.Equal("Persisted sourcing notes",Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/products/"+link),"products").GetProperty("notes").GetString());
        await Tab("prices");Guid price=await AddPrice(link,Today.AddDays(-1),Today.AddDays(30),"12.3456");
        var priceRow=Grid("prices").Locator($"tr[data-id='{price}']");await Action(priceRow,"edit").ClickAsync();await Input(Editor("prices"),"unitPrice","15.4321");await Save(Editor("prices"));
        await Action(priceRow,"power").ClickAsync();await Assertions.Expect(priceRow).ToContainTextAsync(Caption("SupplierStrings","Active",culture));
        var current=Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/prices/"+price),"prices");Assert.Equal(15.4321m,current.GetProperty("unitPrice").GetDecimal());
        var readiness=Assert.Single((await api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={product}&productVariantId={variant}&quantity=2")).EnumerateArray());
        Assert.Equal(9,readiness.GetProperty("leadTimeDays").GetInt32());Assert.Equal(2,readiness.GetProperty("minimumOrderQuantity").GetDecimal());
        Assert.Equal(15.4321m,Record(Assert.Single(readiness.GetProperty("prices").EnumerateArray()),"prices").GetProperty("unitPrice").GetDecimal());
        Guid overlap=await AddPrice(link,Today,Today.AddDays(10),"99");
        await Action(Grid("prices").Locator($"tr[data-id='{overlap}']"),"power").ClickAsync();
        await Assertions.Expect(Grid("prices").GetByRole(AriaRole.Alert)).ToHaveTextAsync(Caption("SupplierSourcingStrings","PriceOverlap",culture));
        Guid history=await AddPrice(link,new(1985,6,8),new(1985,6,30),"8");
        var historyRow=Grid("prices").Locator($"tr[data-id='{history}']");await Action(historyRow,"power").ClickAsync();
        await Assertions.Expect(historyRow).ToContainTextAsync(DateText(new(1985,6,8),culture));
        Assert.Equal("1985-06-08",Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/prices/"+history),"prices").GetProperty("effectiveFrom").GetString());
        await Action(priceRow,"trash").ClickAsync();await page.Locator("[data-sourcing-delete=prices] > footer .btn-danger").ClickAsync();
        await Assertions.Expect(page.Locator("[data-sourcing-delete=prices]").GetByRole(AriaRole.Alert)).ToHaveTextAsync(Caption("SupplierSourcingStrings","DeleteProtected",culture));
        await page.Locator("[data-sourcing-delete=prices] > footer .btn-secondary").ClickAsync();
        await Action(priceRow,"eye").ClickAsync();await Assertions.Expect(Editor("prices").Locator("[name=unitPrice]")).ToBeDisabledAsync();
        await AssertFonts(language);await page.SetViewportSizeAsync(390,844);await Modal(Editor("prices"));await AssertFonts(language);
        await page.SetViewportSizeAsync(1440,1000);await Cancel(Editor("prices"));
        await page.ReloadAsync();await OpenSupplier(supplierCode);await Tab("prices");
        await Assertions.Expect(Grid("prices").Locator($"tr[data-id='{history}']")).ToBeVisibleAsync();
        Assert.False((await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/prices/"+history)).GetProperty("isCurrent").GetBoolean());
        await Tab("products");await Action(Grid("products").Locator($"tr[data-id='{link}']"),"trash").ClickAsync();await page.Locator("[data-sourcing-delete=products] > footer .btn-danger").ClickAsync();
        await Assertions.Expect(page.Locator("[data-sourcing-delete=products]").GetByRole(AriaRole.Alert)).ToHaveTextAsync(Caption("SupplierSourcingStrings","DeleteProtected",culture));
    }
    [Theory][InlineData("fa")][InlineData("ar")][InlineData("en")]
    public async Task PriceListHeaderLinesPublicationReloadAndLanguageAreReal(string language){
        culture=language;Guid link=await SeedLink(supplier,variant,"LIST-ITEM");
        await Switch(language);await OpenSupplier(supplierCode);await Tab("prices");await Grid("prices").Locator("[data-sourcing-switch=price-lists]").ClickAsync();
        await Grid("price-lists").Locator("[data-sourcing-add]").ClickAsync();
        await Input(Editor("price-lists"),"code","LIST-"+stamp);await Input(Editor("price-lists"),"title","List "+stamp);
        await Currency(Editor("price-lists"));await DateInput(Editor("price-lists"),"effectiveFrom",Today);await DateInput(Editor("price-lists"),"effectiveTo",Today.AddDays(30));
        await Save(Editor("price-lists"));await Assertions.Expect(Grid("price-lists").Locator("tbody tr")).ToHaveCountAsync(1);
        Guid list=Id(Record(Assert.Single((await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/price-lists")).GetProperty("items").EnumerateArray()),"price-lists"));
        var row=Grid("price-lists").Locator($"tr[data-id='{list}']");await Action(row,"edit").ClickAsync();await Input(Editor("price-lists"),"title","Edited list "+stamp);await Save(Editor("price-lists"));
        await Action(row,"orders").ClickAsync();await Grid("lines").Locator("[data-sourcing-add]").ClickAsync();
        await Choose(Editor("lines"),"supplierProductId",productName,productName+" - "+sku);await ChooseReference(Editor("lines"),"purchaseUnitId","unit.of.measure");
        await Input(Editor("lines"),"unitPrice","22.1256");await Input(Editor("lines"),"minimumQuantity","2");await Input(Editor("lines"),"discountPercent","10");
        await Save(Editor("lines"));await Assertions.Expect(Grid("lines").Locator("tbody tr")).ToHaveCountAsync(1);
        var lineRow=Grid("lines").Locator("tbody tr");await Action(lineRow,"edit").ClickAsync();await Input(Editor("lines"),"unitPrice","24.5");await Save(Editor("lines"));
        await Action(lineRow,"eye").ClickAsync();await Assertions.Expect(Editor("lines").Locator("[name=unitPrice]")).ToBeDisabledAsync();await Cancel(Editor("lines"));
        await page.Locator("[data-sourcing-lines] > footer .btn-secondary").ClickAsync();
        await Action(row,"power").ClickAsync();await Assertions.Expect(row).ToContainTextAsync(Caption("SupplierStrings","Active",culture));
        var data=await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/price-lists/"+list+"/lines");var line=Record(Assert.Single(data.GetProperty("items").EnumerateArray()),"lines");
        Assert.Equal(24.5m,line.GetProperty("unitPrice").GetDecimal());Assert.Equal(JsonValueKind.String,line.GetProperty("publishedPriceId").ValueKind);
        foreach(bool active in new[]{false,true}){
            await Action(row,"power").ClickAsync();await Assertions.Expect(Action(row,"power")).ToHaveAttributeAsync("aria-label",Caption("SupplierStrings",active?"Deactivate":"Activate",culture));
            Assert.Equal(active,Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/price-lists/"+list),"price-lists").GetProperty("isActive").GetBoolean());
        }
        await page.ReloadAsync();await OpenSupplier(supplierCode);await Tab("prices");await Grid("prices").Locator("[data-sourcing-switch=price-lists]").ClickAsync();
        await Assertions.Expect(Grid("price-lists").Locator($"tr[data-id='{list}']")).ToContainTextAsync("Edited list "+stamp);
        await Action(Grid("price-lists").Locator($"tr[data-id='{list}']"),"orders").ClickAsync();
        await Assertions.Expect(Grid("lines").Locator("[data-sourcing-add]")).ToHaveCountAsync(0);await Assertions.Expect(Grid("lines").Locator("tbody tr")).ToHaveCountAsync(1);
        await AssertFonts(language);
        var source=Assert.Single((await api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={product}&productVariantId={variant}&quantity=2")).EnumerateArray());
        Assert.Equal(link,source.GetProperty("supplierProductId").GetGuid());Assert.Equal(24.5m,Record(Assert.Single(source.GetProperty("prices").EnumerateArray()),"prices").GetProperty("unitPrice").GetDecimal());
    }
    [Fact]public async Task PreferredReplacementAndCompanyIsolationWorkInBothDirections(){
        Guid first=await SeedLink(supplier,variant,"PREFERRED-A"),secondSupplier=await SeedSupplier(api,Company,"SOURCE-B-"+stamp);
        Guid second=await SeedLink(secondSupplier,variant,"PREFERRED-B");
        await OpenSupplier(supplierCode);await Tab("products");await Action(Grid("products").Locator($"tr[data-id='{first}']"),"check").ClickAsync();
        await Assertions.Expect(Action(Grid("products").Locator($"tr[data-id='{first}']"),"check")).ToHaveAttributeAsync("aria-label",Caption("SupplierSourcingStrings","UnsetPreferred","en"));
        await OpenSupplier("SOURCE-B-"+stamp);await Tab("products");await Action(Grid("products").Locator($"tr[data-id='{second}']"),"check").ClickAsync();
        await Assertions.Expect(Action(Grid("products").Locator($"tr[data-id='{second}']"),"check")).ToHaveAttributeAsync("aria-label",Caption("SupplierSourcingStrings","UnsetPreferred","en"));
        var sources=(await api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={product}&productVariantId={variant}&quantity=2")).EnumerateArray().ToArray();
        Assert.Equal(secondSupplier,Assert.Single(sources,x=>x.GetProperty("isPreferred").GetBoolean()).GetProperty("supplierId").GetGuid());
        Assert.False(Record(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/products/"+first),"products").GetProperty("isPreferred").GetBoolean());
        Guid foreign=await SeedSupplier(api.InCompany(Other),Other,"SOURCE-OTHER-"+stamp);
        await SelectCompany(Other);await OpenSupplier("SOURCE-OTHER-"+stamp);await Tab("products");await Assertions.Expect(Grid("products").Locator("tbody tr")).ToHaveCountAsync(0);
        await api.InCompany(Other).Send(HttpMethod.Get,ApiRoot(supplier)+"/products/"+first,expected:404);
        await api.Send(HttpMethod.Get,ApiRoot(foreign)+"/products",expected:404);
        await api.InCompany(Guid.NewGuid()).Send(HttpMethod.Get,ApiRoot(supplier)+"/products",expected:404);
        await DirectorySearch(supplierCode);await Assertions.Expect(page.Locator("[data-supplier-directory] tbody tr")).ToHaveCountAsync(0);
        await SelectCompany(Company);await DirectorySearch("SOURCE-OTHER-"+stamp);await Assertions.Expect(page.Locator("[data-supplier-directory] tbody tr")).ToHaveCountAsync(0);
        await OpenSupplier(supplierCode);await Tab("products");await Assertions.Expect(Grid("products").Locator($"tr[data-id='{first}']")).ToBeVisibleAsync();
    }
    [Fact]public async Task SearchSortPagingAndSafeRelationshipDeletionUseCommittedRows(){
        for(int i=0;i<12;i++){
            Guid skuId=Id(await api.Send(HttpMethod.Post,$"/api/v1/products/{product}/variants",new{sku="PAGE-"+stamp+"-"+i.ToString("D2",CultureInfo.InvariantCulture),displayName="Paging "+i.ToString(CultureInfo.InvariantCulture)},201));
            await SeedLink(supplier,skuId,"PAGE-"+i.ToString("D2",CultureInfo.InvariantCulture));
        }
        await OpenSupplier(supplierCode);await Tab("products");var grid=Grid("products");var rows=grid.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(10);await grid.Locator(".barang-sort-header").Nth(1).ClickAsync();
        await Assertions.Expect(rows.Locator("td:nth-child(2)")).ToHaveTextAsync(Enumerable.Range(0,10).Select(i=>"PAGE-"+i.ToString("D2",CultureInfo.InvariantCulture)).ToArray());
        await grid.Locator(".barang-pagination button").Last.ClickAsync();await Assertions.Expect(rows.Locator("td:nth-child(2)")).ToHaveTextAsync(new[]{"PAGE-10","PAGE-11"});
        await grid.Locator(".barang-pagination button").First.ClickAsync();await Assertions.Expect(rows).ToHaveCountAsync(10);
        await grid.Locator(".barang-sort-header").Nth(1).ClickAsync();await Assertions.Expect(rows.Locator("td:nth-child(2)").First).ToHaveTextAsync("PAGE-11");
        await grid.Locator("[name=sourcingSearch]").FillAsync("PAGE-03");await Assertions.Expect(rows.Locator("td:nth-child(2)").First).ToHaveTextAsync("PAGE-03");
        string id=(await rows.GetAttributeAsync("data-id"))!;await Action(rows,"trash").ClickAsync();await page.Locator("[data-sourcing-delete=products] > footer .btn-danger").ClickAsync();
        await Assertions.Expect(page.Locator("[data-sourcing-delete=products]")).ToBeHiddenAsync();await Assertions.Expect(rows).ToHaveCountAsync(0);
        await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/products/"+id,expected:404);
        await page.ReloadAsync();await OpenSupplier(supplierCode);await Tab("products");await grid.Locator("[name=sourcingSearch]").FillAsync("PAGE-03");await Assertions.Expect(rows).ToHaveCountAsync(0);
    }
    private async Task<Guid> SeedSupplier(SupplierReferenceScenario target,Guid scope,string code){
        Guid category=Id((await target.Options("supplier.category","en",true)).EnumerateArray().First());
        Guid id=Id((await target.Send(HttpMethod.Post,"/api/v1/suppliers/core",new{identity=new{supplierCode=code,displayName="Supplier sourcing "+code,entityType="Individual",status="Active"},categoryIds=new[]{category}},201)).GetProperty("supplier"));
        suppliers.Add((scope,id));return id;
    }
    private async Task<Guid> SeedLink(Guid owner,Guid skuId,string code)=>Id(Record(await api.Send(HttpMethod.Post,ApiRoot(owner)+"/products",new{data=new{
        productId=product,productVariantId=skuId,purchaseUnitId=Id(references["unit.of.measure"]),packagingTypeId=Id(references["packaging.type"]),
        supplierItemCode=code,minimumOrderQuantity=2,leadTimeDays=7,isActive=true,availability="Available"}},201),"products"));
    private async Task<Guid> AddPrice(Guid link,DateOnly from,DateOnly to,string amount){
        await Grid("prices").Locator("[data-sourcing-add]").ClickAsync();await Choose(Editor("prices"),"supplierProductId",productName,productName+" - "+sku);
        await ChooseReference(Editor("prices"),"purchaseUnitId","unit.of.measure");await Currency(Editor("prices"));
        await DateInput(Editor("prices"),"effectiveFrom",from);await DateInput(Editor("prices"),"effectiveTo",to);await Input(Editor("prices"),"unitPrice",amount);await Input(Editor("prices"),"minimumQuantity","2");
        await Save(Editor("prices"));
        var values=(await api.Send(HttpMethod.Get,ApiRoot(supplier)+"/prices?pageSize=100")).GetProperty("items").EnumerateArray();
        return Id(Record(Assert.Single(values,x=>Record(x,"prices").GetProperty("supplierProductId").GetGuid()==link&&
            Record(x,"prices").GetProperty("effectiveFrom").GetString()==from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)&&
            Record(x,"prices").GetProperty("unitPrice").GetDecimal()==decimal.Parse(amount,CultureInfo.InvariantCulture)),"prices"));
    }
    private Task Ready(string selector,string eventName)=>page.WaitForFunctionAsync("({selector,eventName})=>{const el=document.querySelector(selector);return !!el&&Object.keys(el).some(key=>key.startsWith('_blazorEvents_')&&!!el[key].getHandler?.(eventName));}",new{selector,eventName});
    private async Task SelectCompany(Guid company){
        await page.GotoAsync(baseUrl+"/company");await page.Locator($".company-list form:has(input[name=companyId][value='{company}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company");await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
    }
    private async Task DirectorySearch(string code){
        await page.GotoAsync(baseUrl+"/master/suppliers");await Ready("[data-supplier-directory] [name=supplierSearch]","input");
        await page.Locator("[data-supplier-directory] [name=supplierSearch]").FillAsync(code);
    }
    private async Task OpenSupplier(string code){
        await DirectorySearch(code);var row=page.Locator("[data-supplier-directory] tbody tr").Filter(new(){HasText=code});
        await Assertions.Expect(row).ToHaveCountAsync(1);await Action(row,"edit").ClickAsync();await Assertions.Expect(RootEditor).ToBeVisibleAsync();
    }
    private async Task Tab(string kind){await RootEditor.Locator($"[data-supplier-tab='{kind}']").ClickAsync();await Assertions.Expect(RootEditor.Locator($"[data-supplier-tab='{kind}']")).ToHaveAttributeAsync("aria-selected","true");await Assertions.Expect(Grid(kind)).ToBeVisibleAsync();}
    private async Task Switch(string value){
        await Ready($".language-switch a[hreflang='{value}']","click");string marker=Guid.NewGuid().ToString("N");await page.EvaluateAsync("value=>window.__sourcingDocument=value",marker);
        await page.Locator($".language-switch a[hreflang='{value}']").ClickAsync();await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang",value);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",value=="en"?"ltr":"rtl");Assert.Equal(marker,await page.EvaluateAsync<string>("()=>window.__sourcingDocument"));culture=value;
    }
    private static ILocator Action(ILocator row,string icon)=>row.Locator($".barang-icon-button:has(svg[data-icon='{icon}'])");
    private static Task Input(ILocator parent,string name,string value)=>parent.Locator($"[name='{name}']").FillAsync(value);
    private static async Task Save(ILocator dialog){await dialog.Locator(":scope > footer .btn-primary").ClickAsync();await Assertions.Expect(dialog).ToBeHiddenAsync();}
    private async Task Cancel(ILocator dialog,bool dirty=false){
        await dialog.Locator(":scope > footer .btn-secondary").ClickAsync();
        if(dirty){await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name=Caption("SupplierStrings","DiscardTitle",culture),Exact=true})).ToBeVisibleAsync();await page.GetByRole(AriaRole.Button,new(){Name=Caption("SupplierStrings","Discard",culture),Exact=true}).ClickAsync();}
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }
    private static async Task Choose(ILocator parent,string name,string search,string label){
        var input=parent.Locator($"[name='{name}']");await Assertions.Expect(input).ToBeEnabledAsync();await input.FillAsync(search);
        await input.Locator("..").GetByRole(AriaRole.Option,new(){Name=label,Exact=true}).ClickAsync();await Assertions.Expect(input).ToHaveValueAsync(label);
    }
    private Task ChooseReference(ILocator parent,string name,string type){
        string caption=Captions(references[type].GetProperty("code").GetString()!)[culture];return Choose(parent,name,caption,caption);
    }
    private Task Currency(ILocator parent)=>Choose(parent,"currencyCode","USD","USD - "+Caption("SupplierSourcingStrings","Currency_USD",culture));
    private async Task DateInput(ILocator parent,string name,DateOnly date){await Input(parent,name,DateText(date,culture));await parent.Locator($"[name='{name}']").BlurAsync();await Assertions.Expect(parent.Locator($"[name='{name}']")).ToHaveValueAsync(DateText(date,culture));}
    private static string DateText(DateOnly date,string language){
        if(language!="fa")return date.ToString("yyyy/MM/dd",CultureInfo.InvariantCulture);
        var calendar=new PersianCalendar();var value=date.ToDateTime(TimeOnly.MinValue);
        return calendar.GetYear(value).ToString("D4",CultureInfo.InvariantCulture)+"/"+calendar.GetMonth(value).ToString("D2",CultureInfo.InvariantCulture)+"/"+calendar.GetDayOfMonth(value).ToString("D2",CultureInfo.InvariantCulture);
    }
    private async Task AssertFonts(string language){
        string font=language=="fa"?"Vazirmatn":language=="ar"?"Noto Sans Arabic":"Inter";
        await page.EvaluateAsync("font=>document.fonts.load('14px \"'+font+'\"')",font);
        Assert.True(await page.EvaluateAsync<bool>("font=>document.fonts.check('14px \"'+font+'\"')",font));
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",language=="en"?"ltr":"rtl");
        Assert.Empty(await page.Locator("[data-supplier-editor]").EvaluateAsync<string[]>("(root,font)=>[root,...root.querySelectorAll('h2,h3,label,input,button,td,th,textarea')].filter(el=>el.getClientRects().length&&!getComputedStyle(el).fontFamily.toLowerCase().includes(font.toLowerCase())).map(el=>el.tagName+':'+getComputedStyle(el).fontFamily)",font));
    }
    private async Task Modal(ILocator dialog){
        var box=await dialog.BoundingBoxAsync();Assert.NotNull(box);Assert.True(box.X>=0&&box.Y>=0&&box.X+box.Width<=391&&box.Y+box.Height<=845);
        Assert.True(await dialog.Locator(":scope > footer").EvaluateAsync<bool>("el=>{const r=el.getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight+1;}"));
        Assert.True(await dialog.EvaluateAsync<bool>("el=>el.scrollWidth<=el.clientWidth+1"));
    }
    private static string Caption(string resource,string key,string language){
        DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        string suffix=language=="fa"?"":"."+language;
        return XDocument.Load(Path.Combine(root!.FullName,"01_CORE","src","BARANG.Backoffice","Localization",resource+suffix+".resx"))
            .Root!.Elements("data").Single(x=>x.Attribute("name")!.Value==key).Element("value")!.Value;
    }
}
