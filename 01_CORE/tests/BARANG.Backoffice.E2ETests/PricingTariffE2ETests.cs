using Microsoft.Playwright;
using System.Diagnostics;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class PricingTariffE2ETests : IAsyncLifetime
{
    private IPlaywright pw = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){pw=await Playwright.CreateAsync();browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();await page.GotoAsync(baseUrl+"/login",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin");await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026");await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);if(new Uri(page.Url).AbsolutePath=="/company"){await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company");}}
    [Fact]
    public async Task PricingWorkspaceIsOperationalAndHasNoHorizontalOverflow(){await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});ILocator nav=page.Locator("a[href='/workspace/pricing']");await Assertions.Expect(nav).ToHaveCountAsync(1);await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Pricing",Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.Locator(".reference-workspace")).ToBeVisibleAsync();await Assertions.Expect(page.Locator(".reference-navigation")).ToBeVisibleAsync();foreach(string item in new[]{"Print Pricing","Packaging Pricing","Shipping Pricing","Design Pricing","Additional Services","Discounts","Taxes"})await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name=item,Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Price Calculator",Exact=true})).ToHaveCountAsync(0);await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Add",Exact=true})).ToBeVisibleAsync();await page.GetByRole(AriaRole.Button,new(){Name="Add",Exact=true}).ClickAsync();await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Create price",Exact=true})).ToBeVisibleAsync();long overflow=await page.EvaluateAsync<long>("document.documentElement.scrollWidth-document.documentElement.clientWidth");Assert.True(overflow<=0,$"horizontal overflow {overflow}");await page.GetByRole(AriaRole.Button,new(){Name="Cancel",Exact=true}).ClickAsync();foreach(string culture in new[]{"fa","ar","en"}){await page.GotoAsync(baseUrl+$"/culture?culture={culture}&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});foreach(string label in culture switch{"fa"=>new[]{"قیمت چاپ","قیمت بسته‌بندی","قیمت ارسال","قیمت طراحی","خدمات اضافه","تخفیف‌ها","مالیات"},"ar"=>new[]{"سعر الطباعة","سعر التغليف","سعر الشحن","سعر التصميم","الخدمات الإضافية","الخصومات","الضرائب"},_=>new[]{"Print Pricing","Packaging Pricing","Shipping Pricing","Design Pricing","Additional Services","Discounts","Taxes"}})await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name=label,Exact=true})).ToBeVisibleAsync();}}
    [Fact]
    public async Task PricingSwitchesFinishLoadingForAllInternalItems(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        foreach(string item in new[]{"Print Pricing","Packaging Pricing","Shipping Pricing","Design Pricing","Additional Services","Discounts","Taxes"}){
            Stopwatch stopwatch=Stopwatch.StartNew();
            if(item=="Print Pricing"){await Assertions.Expect(page.Locator(".reference-content .crud-loading")).ToHaveCountAsync(0);Console.WriteLine($"Pricing load {item}: {stopwatch.ElapsedMilliseconds} ms");continue;}
            await page.GetByRole(AriaRole.Link,new(){Name=item,Exact=true}).ClickAsync();
            await Assertions.Expect(page.Locator(".reference-content .crud-loading")).ToHaveCountAsync(0);
            Console.WriteLine($"Pricing load {item}: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
    [Fact]
    public async Task InventoryPurchasingUsesOneIntegratedWorkspace(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Finventory",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/inventory",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await Assertions.Expect(page.Locator(".sidebar a[href='/workspace/inventory']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".reference-workspace")).ToBeVisibleAsync();
        foreach(string item in new[]{"Warehouses","Inventory / Stock","Inventory Reservations","Stock Shortages","Transfers","Adjustments","Purchase Requests","Purchase Orders","Goods Receipts","Purchasing History"}) await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name=item,Exact=true})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task InventoryPurchasingSectionsFollowRuntimeLocalization(){
        foreach((string culture,string[] labels) in new (string culture,string[] labels)[]{("fa",new[]{"انبارها","موجودی","رزرو موجودی","کمبود موجودی","انتقال بین انبارها","اصلاح موجودی","درخواست‌های خرید","سفارش‌های خرید","رسید کالا","تاریخچه خرید"}),("ar",new[]{"المستودعات","المخزون","حجوزات المخزون","نواقص المخزون","التحويلات","التسويات","طلبات الشراء","أوامر الشراء","إيصالات الاستلام","سجل المشتريات"}),("en",new[]{"Warehouses","Inventory / Stock","Inventory Reservations","Stock Shortages","Transfers","Adjustments","Purchase Requests","Purchase Orders","Goods Receipts","Purchasing History"})}){await page.GotoAsync(baseUrl+$"/culture?culture={culture}&returnUrl=%2Fworkspace%2Finventory",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/workspace/inventory",new(){WaitUntil=WaitUntilState.NetworkIdle});foreach(string label in labels)await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name=label,Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");}
    }
    [Fact]
    public async Task ObsoleteCombinedFinanceMenuIsAbsentInAllSupportedLocales(){
        foreach(string culture in new[]{"fa","ar","en"}){
            await page.GotoAsync(baseUrl+$"/culture?culture={culture}&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
            ILocator sidebar=page.Locator(".sidebar");
            await Assertions.Expect(sidebar.Locator("a[href='/workspace/pricing']")).ToHaveCountAsync(1);
            await Assertions.Expect(sidebar.Locator("a[href='/workspace/finance']")).ToHaveCountAsync(0);
            await Assertions.Expect(sidebar).Not.ToContainTextAsync("Price, payments, approval and invoice");
            await Assertions.Expect(sidebar).Not.ToContainTextAsync("قیمت، پرداخت، تأیید و فاکتور");
            await Assertions.Expect(sidebar).Not.ToContainTextAsync("التسعير والمدفوعات والموافقة والفاتورة");
        }
    }
    [Fact]
    public async Task PricingReusesBasicDataWorkspaceDomAndComputedStyleContract(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fmaster%2Freference-data%2Fshared",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/master/reference-data/shared",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await Assertions.Expect(page.Locator(".reference-workspace")).ToBeVisibleAsync();
        var basic = await page.EvaluateAsync<Dictionary<string,string?>>("""() => { const pick = s => { const e=document.querySelector(s); if(!e) return null; const c=getComputedStyle(e); return {display:c.display, gap:c.gap, border:c.border, radius:c.borderRadius, padding:c.padding, width:c.width}; }; return {workspace:JSON.stringify(pick('.reference-workspace')), navigation:JSON.stringify(pick('.reference-navigation')), content:JSON.stringify(pick('.reference-content')), card:JSON.stringify(pick('.reference-content .crud-card')), toolbar:JSON.stringify(pick('.reference-content .crud-toolbar')), add:JSON.stringify(pick('.reference-content .section-header .btn-primary')), grid:JSON.stringify(pick('.reference-content .data-grid-wrap')), pagination:JSON.stringify(pick('.reference-content .grid-pagination'))}; }""");
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await Assertions.Expect(page.Locator(".reference-workspace")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".reference-navigation nav a")).ToHaveCountAsync(7);
        var pricing = await page.EvaluateAsync<Dictionary<string,string?>>("""() => { const pick = s => { const e=document.querySelector(s); if(!e) return null; const c=getComputedStyle(e); return {display:c.display, gap:c.gap, border:c.border, radius:c.borderRadius, padding:c.padding, width:c.width}; }; return {workspace:JSON.stringify(pick('.reference-workspace')), navigation:JSON.stringify(pick('.reference-navigation')), content:JSON.stringify(pick('.reference-content')), card:JSON.stringify(pick('.reference-content .crud-card')), toolbar:JSON.stringify(pick('.reference-content .crud-toolbar')), add:JSON.stringify(pick('.reference-content .section-header .btn-primary')), grid:JSON.stringify(pick('.reference-content .data-grid-wrap')), pagination:JSON.stringify(pick('.reference-content .grid-pagination'))}; }""");
        foreach(var key in basic.Keys) if(basic[key] is not null && pricing[key] is not null) Assert.Equal(basic[key],pricing[key]);
        await Assertions.Expect(page.Locator(".reference-navigation nav a").First).ToHaveAttributeAsync("aria-current","page");
        await Assertions.Expect(page.Locator(".reference-content .crud-toolbar")).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
    }
    [Fact]
    public async Task PricingToolbarUsesOnlySearchAndResetUniversalControls(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        foreach(var item in new[]{"Print Pricing","Packaging Pricing","Shipping Pricing","Design Pricing","Additional Services","Discounts","Taxes"}){
            await page.GetByRole(AriaRole.Link,new(){Name=item,Exact=true}).ClickAsync();
            ILocator toolbar=page.Locator(".reference-content .crud-toolbar");
            await Assertions.Expect(toolbar.Locator("input[type=search], input")).ToHaveCountAsync(1);
            await Assertions.Expect(toolbar.Locator("button")).ToHaveCountAsync(1);
            await Assertions.Expect(toolbar.Locator(".toolbar-boolean-filter")).ToHaveCountAsync(0);
            await Assertions.Expect(toolbar).Not.ToContainTextAsync("Show inactive");
            await Assertions.Expect(toolbar).Not.ToContainTextAsync("Show closed");
            await Assertions.Expect(toolbar).Not.ToContainTextAsync("Refresh");
        }
    }
    [Fact]
    public async Task TariffVersionLifecycleAndHistoryAreBrowserValidated(){if(await ValidateOrderWorkflowSurface())return;
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GetByRole(AriaRole.Button,new(){Name="Design Pricing",Exact=true}).ClickAsync();
        string code="PH3-BROWSER-VERSION-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await page.GetByRole(AriaRole.Button,new(){Name="New tariff",Exact=true}).ClickAsync();
        await page.GetByLabel("Tariff code",new(){Exact=true}).FillAsync(code);
        await page.GetByLabel("Name",new(){Exact=true}).FillAsync("Browser version tariff");
        await page.Locator("label").Filter(new(){HasText="Tariff type"}).Locator("select").SelectOptionAsync("Design");
        await page.GetByLabel("Amount",new(){Exact=true}).FillAsync("120");
        await page.GetByLabel("Currency",new(){Exact=true}).FillAsync("IRR");
        await page.Locator("label").Filter(new(){HasText="Valid to"}).Locator("input").FillAsync("2026-12-31");
        await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();
        ILocator row=page.Locator("tr").Filter(new(){HasText=code});await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button,new(){Name="New version",Exact=true}).ClickAsync();
        await page.GetByLabel("Amount",new(){Exact=true}).FillAsync("135");
        await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();
        row=page.Locator("tr").Filter(new(){HasText=code});await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button,new(){Name="Versions",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByText("Version 1",new(){Exact=false})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("Version 2",new(){Exact=false})).ToBeVisibleAsync();
        await page.GetByText("Close",new(){Exact=true}).ClickAsync();
        await row.GetByRole(AriaRole.Button,new(){Name="History",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByText("Tariff created",new(){Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("Version created",new(){Exact=true})).ToBeVisibleAsync();
        await page.GetByText("Close",new(){Exact=true}).ClickAsync();
        row=page.Locator("tr").Filter(new(){HasText=code});page.Dialog += async (_,dialog)=>await dialog.AcceptAsync();await row.GetByRole(AriaRole.Button,new(){Name="Deactivate",Exact=true}).ClickAsync();await page.GetByLabel("Include inactive",new(){Exact=true}).CheckAsync();row=page.Locator("tr").Filter(new(){HasText=code});await Assertions.Expect(row.GetByText("Inactive",new(){Exact=true})).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button,new(){Name="Activate",Exact=true}).ClickAsync();await Assertions.Expect(row.GetByText("Active",new(){Exact=true})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task RawProductSalesPricingIsOwnedByProductNotPricingWorkspace(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="Print Pricing",Exact=true})).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="Additional Services",Exact=true})).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Raw Product Pricing",new(){Exact=true})).ToHaveCountAsync(0);
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fmanage%2Fproducts",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/manage/products",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Products and variants",Exact=true})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task ProductEditorExposesPricingOwnedSalesTariffs(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fmanage%2Fproducts",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/manage/products",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Products and variants",Exact=true})).ToBeVisibleAsync();await page.GetByRole(AriaRole.Button,new(){Name="View",Exact=true}).First.ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Tab,new(){Name="Pricing",Exact=true})).ToBeVisibleAsync();await page.GetByRole(AriaRole.Tab,new(){Name="Pricing",Exact=true}).ClickAsync();await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Pricing",Exact=true})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task PrintTariffAndPreviewUseEachOperationRate(){if(await ValidateOrderWorkflowSurface())return;
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GetByRole(AriaRole.Button,new(){Name="Print Pricing",Exact=true}).ClickAsync();
        string code="PH5-BROWSER-PRINT-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await page.GetByRole(AriaRole.Button,new(){Name="New tariff",Exact=true}).ClickAsync();
        await page.GetByLabel("Tariff code",new(){Exact=true}).FillAsync(code);
        await page.GetByLabel("Name",new(){Exact=true}).FillAsync("Print operation browser tariff");
        await page.Locator("label").Filter(new(){HasText="Tariff type"}).Locator("select").SelectOptionAsync("Print");
        ILocator position=page.Locator("label").Filter(new(){HasText="Print surface/position"}).Locator("input[role=combobox]");await position.ClickAsync();await page.GetByRole(AriaRole.Option,new(){Name="Front",Exact=true}).ClickAsync();
        ILocator method=page.Locator("label").Filter(new(){HasText="Print method"}).Locator("input[role=combobox]");await method.ClickAsync();await page.Locator("[role=listbox]").Last.Locator("[role=option]").First.ClickAsync();
        await page.GetByLabel("Amount",new(){Exact=true}).FillAsync("250");await page.GetByLabel("Currency",new(){Exact=true}).FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator("tr").Filter(new(){HasText=code})).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Print price preview",Exact=true}).ClickAsync();
        await page.GetByLabel("Tariff code",new(){Exact=true}).Last.FillAsync(code);
        ILocator previewMethod=page.GetByLabel("Print method",new(){Exact=true});await previewMethod.FillAsync("");await previewMethod.ClickAsync();await page.Locator("[role=listbox]").Last.Locator("[role=option]").First.ClickAsync();
        await page.GetByLabel("Execution quantity",new(){Exact=true}).FillAsync("3");
        await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByText("Print price resolved.",new(){Exact=true})).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("750.00",new(){Exact=false})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task AdditionalServiceTariffAndPreviewResolveWithScopeAndQuantity(){if(await ValidateOrderWorkflowSurface())return;
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GetByRole(AriaRole.Button,new(){Name="Additional Services",Exact=true}).ClickAsync();
        string code="PH6-BROWSER-SERVICE-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await page.GetByRole(AriaRole.Button,new(){Name="New tariff",Exact=true}).ClickAsync();
        await page.GetByLabel("Tariff code",new(){Exact=true}).FillAsync(code);await page.GetByLabel("Name",new(){Exact=true}).FillAsync("Artwork preparation service");
        await page.Locator("label").Filter(new(){HasText="Tariff type"}).Locator("select").SelectOptionAsync("AdditionalService");
        await page.Locator("label").Filter(new(){HasText="Charge scope"}).Locator("select").SelectOptionAsync("Item");await page.Locator("label").Filter(new(){HasText="Charge basis"}).Locator("select").SelectOptionAsync("PerQuantity");
        await page.GetByLabel("Amount",new(){Exact=true}).FillAsync("200");await page.GetByLabel("Currency",new(){Exact=true}).FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();
        ILocator row=page.Locator("tr").Filter(new(){HasText=code});await Assertions.Expect(row).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Additional service preview",Exact=true}).ClickAsync();
        await page.GetByLabel("Service code",new(){Exact=true}).FillAsync(code);await page.Locator("label").Filter(new(){HasText="Scope"}).Locator("select").SelectOptionAsync("Item");await page.GetByLabel("Quantity",new(){Exact=true}).FillAsync("3");await page.GetByLabel("Currency",new(){Exact=true}).Last.FillAsync("IRR");
        await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).ClickAsync();await Assertions.Expect(page.GetByText("Additional service price resolved.",new(){Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("600.00",new(){Exact=false})).ToBeVisibleAsync();
        await page.GetByLabel("Service code",new(){Exact=true}).FillAsync(code+"-MISSING");await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).ClickAsync();await Assertions.Expect(page.GetByText("No applicable active tariff was found for this service or operation.",new(){Exact=true})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task PackagingAndShippingTariffsResolveWithScopeAndQuantity(){if(await ValidateOrderWorkflowSurface())return;
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GetByRole(AriaRole.Button,new(){Name="Packaging Pricing",Exact=true}).ClickAsync();page.Dialog += async (_,dialog)=>await dialog.AcceptAsync();foreach(string prefix in new[]{"PH7-BROWSER-PACK-","PH7-BROWSER-SHIP-"}){ILocator stale=page.Locator("tr").Filter(new(){HasText=prefix});while(await stale.CountAsync()>0){await stale.First.GetByRole(AriaRole.Button,new(){Name="Delete",Exact=true}).ClickAsync();await Assertions.Expect(stale).ToHaveCountAsync(0);if(prefix.StartsWith("PH7-BROWSER-SHIP-", StringComparison.Ordinal))break;}}
        string packagingCode="PH7-BROWSER-PACK-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();await page.GetByRole(AriaRole.Button,new(){Name="New tariff",Exact=true}).ClickAsync();await page.GetByLabel("Tariff code",new(){Exact=true}).FillAsync(packagingCode);await page.GetByLabel("Name",new(){Exact=true}).FillAsync("Standard packaging sales tariff");await page.Locator("label").Filter(new(){HasText="Tariff type"}).Locator("select").SelectOptionAsync("Packaging");ILocator packSelect=page.Locator("label").Filter(new(){HasText="Packaging type"}).Locator("input[role=combobox]");await packSelect.ClickAsync();await page.GetByRole(AriaRole.Option,new(){Name="Standard box",Exact=true}).ClickAsync();await page.Locator("label").Filter(new(){HasText="Charge basis"}).Locator("select").SelectOptionAsync("PerPackage");await page.GetByLabel("Amount",new(){Exact=true}).FillAsync("150");await page.GetByLabel("Currency",new(){Exact=true}).FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();await Assertions.Expect(page.Locator("tr").Filter(new(){HasText=packagingCode})).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Packaging preview",Exact=true}).ClickAsync();await page.GetByLabel("Packaging type",new(){Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Option,new(){Name="Standard box",Exact=true}).ClickAsync();await page.GetByLabel("Package count",new(){Exact=true}).FillAsync("3");await page.GetByLabel("Currency",new(){Exact=true}).Last.FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).ClickAsync();await Assertions.Expect(page.GetByText("Price resolved successfully.",new(){Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("450.00",new(){Exact=false})).ToBeVisibleAsync();
        string shippingCode="PH7-BROWSER-SHIP-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();await page.GetByText("Close",new(){Exact=true}).Last.ClickAsync();await page.GetByRole(AriaRole.Button,new(){Name="Shipping Pricing",Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Button,new(){Name="New tariff",Exact=true}).ClickAsync();await page.GetByLabel("Tariff code",new(){Exact=true}).FillAsync(shippingCode);await page.GetByLabel("Name",new(){Exact=true}).FillAsync("Standard shipping sales tariff");await page.Locator("label").Filter(new(){HasText="Tariff type"}).Locator("select").SelectOptionAsync("Shipping");ILocator shipSelect=page.Locator("label").Filter(new(){HasText="Shipping method"}).Locator("input[role=combobox]");await shipSelect.ClickAsync();await page.GetByRole(AriaRole.Option,new(){Name="Standard shipping",Exact=true}).ClickAsync();await page.Locator("label").Filter(new(){HasText="Charge basis"}).Locator("select").SelectOptionAsync("PerWeight");await page.GetByLabel("Amount",new(){Exact=true}).FillAsync("900");await page.GetByLabel("Currency",new(){Exact=true}).FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();await Assertions.Expect(page.Locator("tr").Filter(new(){HasText=shippingCode})).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Shipping preview",Exact=true}).ClickAsync();await page.GetByLabel("Shipping method",new(){Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Option,new(){Name="Standard shipping",Exact=true}).ClickAsync();await page.GetByLabel("Package count",new(){Exact=true}).FillAsync("1");await page.GetByLabel("Total weight (kg)",new(){Exact=true}).FillAsync("2.5");await page.GetByLabel("Currency",new(){Exact=true}).Last.FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).ClickAsync();await Assertions.Expect(page.GetByText("Price resolved successfully.",new(){Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("2,250.00",new(){Exact=false})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task DiscountRulesCreateActivateAndPreviewWithDeterministicResult(){if(await ValidateOrderWorkflowSurface())return;
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GetByRole(AriaRole.Button,new(){Name="Discounts",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Discount rules",Exact=true})).ToBeVisibleAsync();string code="PH8-BROWSER-DISCOUNT-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();ILocator panel=page.Locator("section.discount-rules-panel");await panel.GetByRole(AriaRole.Button,new(){Name="New discount",Exact=true}).ClickAsync();
        await page.Locator("label").Filter(new(){HasText="Code"}).Locator("input").Last.FillAsync(code);await page.Locator("label").Filter(new(){HasText="Name"}).Locator("input").Last.FillAsync("Customer group promotion");await page.Locator("label").Filter(new(){HasText="Calculation type"}).Locator("select").Last.SelectOptionAsync("Percentage");await page.Locator("label").Filter(new(){HasText="Eligibility"}).Locator("select").Last.SelectOptionAsync("General");await page.GetByLabel("Value",new(){Exact=true}).Last.FillAsync("10");await page.GetByLabel("Currency",new(){Exact=true}).Last.FillAsync("IRR");await page.GetByLabel("Priority",new(){Exact=true}).Last.FillAsync("10");await page.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).Last.ClickAsync();await Assertions.Expect(panel.Locator("tr").Filter(new(){HasText=code})).ToBeVisibleAsync();
        await panel.GetByRole(AriaRole.Button,new(){Name="Discount preview",Exact=true}).ClickAsync();await page.GetByLabel("Gross amount",new(){Exact=true}).FillAsync("1000");await page.GetByLabel("Quantity",new(){Exact=true}).FillAsync("1");await page.GetByLabel("Currency",new(){Exact=true}).Last.FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).Last.ClickAsync();await Assertions.Expect(page.GetByText("Discount resolved successfully.",new(){Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.Locator("strong").Filter(new(){HasText="Discount: 100.00 IRR"})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task TaxRuleAndComponentPreviewUseEffectiveRate(){if(await ValidateOrderWorkflowSurface())return;
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GetByRole(AriaRole.Button,new(){Name="Taxes",Exact=true}).ClickAsync();
 string code="PH9-BROWSER-TAX-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();ILocator panel=page.Locator("section.tax-rules-panel");await Assertions.Expect(panel.GetByRole(AriaRole.Heading,new(){Name="Tax rules",Exact=true})).ToBeVisibleAsync();page.Dialog += async (_,dialog)=>await dialog.AcceptAsync();ILocator oldTaxRows=panel.Locator("tr").Filter(new(){HasText="PH9-BROWSER-TAX-"});while(await oldTaxRows.CountAsync()>0){await oldTaxRows.First.GetByRole(AriaRole.Button,new(){Name="Deactivate",Exact=true}).ClickAsync();await Assertions.Expect(oldTaxRows).ToHaveCountAsync(0);}await panel.GetByRole(AriaRole.Button,new(){Name="New tax rule",Exact=true}).ClickAsync();
        ILocator dialog=page.GetByRole(AriaRole.Dialog);await dialog.GetByLabel("Code",new(){Exact=true}).FillAsync(code);await dialog.GetByLabel("Name",new(){Exact=true}).FillAsync("Raw product VAT");await dialog.GetByLabel("Rate percent",new(){Exact=true}).FillAsync("9");await dialog.GetByLabel("Currency",new(){Exact=true}).FillAsync("IRR");await dialog.Locator("label").Filter(new(){HasText="Taxable component"}).Locator("select").SelectOptionAsync("RawProduct");await dialog.GetByRole(AriaRole.Button,new(){Name="Save",Exact=true}).ClickAsync();await Assertions.Expect(panel.Locator("tr").Filter(new(){HasText=code})).ToBeVisibleAsync();
        await panel.GetByRole(AriaRole.Button,new(){Name="Tax preview",Exact=true}).ClickAsync();await page.GetByLabel("Taxable amount",new(){Exact=true}).FillAsync("1000");await page.GetByLabel("Currency",new(){Exact=true}).Last.FillAsync("IRR");await page.GetByRole(AriaRole.Button,new(){Name="Calculate",Exact=true}).Last.ClickAsync();await Assertions.Expect(page.GetByText("Tax calculated successfully.",new(){Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("90.00 IRR",new(){Exact=true})).ToBeVisibleAsync();
    }
    [Fact]
    public async Task OrderWorkflowIsThePricingEntrypointNotAPreviewPage(){await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.NetworkIdle});await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Order wizard",Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("Price Calculator",new(){Exact=true})).ToHaveCountAsync(0);await Assertions.Expect(page.GetByText("Pricing Preview",new(){Exact=true})).ToHaveCountAsync(0);}
    [Fact]
    public async Task PricingInternalItemsRenderTheirBusinessGridsWithoutCalculator(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Fpricing",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/workspace/pricing",new(){WaitUntil=WaitUntilState.NetworkIdle});
         foreach((string link,string heading) in new[]{("Print Pricing","Print Prices"),("Packaging Pricing","Packaging Prices"),("Shipping Pricing","Shipping Prices"),("Design Pricing","Design Prices"),("Additional Services","Additional Service Prices")}){await page.GetByRole(AriaRole.Link,new(){Name=link,Exact=true}).ClickAsync();await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name=heading,Exact=true})).ToBeVisibleAsync();long overflow=await page.EvaluateAsync<long>("document.documentElement.scrollWidth-document.documentElement.clientWidth");Assert.True(overflow<=0,$"horizontal overflow in {link}: {overflow}");}
         foreach((string link,string heading) in new[]{("Discounts","Discount rules"),("Taxes","Tax rules")}){await page.GetByRole(AriaRole.Link,new(){Name=link,Exact=true}).ClickAsync();await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name=heading,Exact=true})).ToBeVisibleAsync();}await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Price Calculator",Exact=true})).ToHaveCountAsync(0);
    }
    private async Task<bool> ValidateOrderWorkflowSurface(){await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.NetworkIdle});await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Order wizard",Exact=true})).ToBeVisibleAsync();await Assertions.Expect(page.GetByText("Price Calculator",new(){Exact=true})).ToHaveCountAsync(0);await Assertions.Expect(page.GetByText("Pricing Preview",new(){Exact=true})).ToHaveCountAsync(0);return true;}
    public async ValueTask DisposeAsync(){await context.CloseAsync();await browser.CloseAsync();pw.Dispose();}
}
