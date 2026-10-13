using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class DomainReferenceE2ETests : IAsyncLifetime
{
    private readonly string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
    private IPlaywright playwright=null!;
    private IBrowser browser=null!;
    private IBrowserContext context=null!;
    private IPage page=null!;
    public async ValueTask InitializeAsync(){
        playwright=await Playwright.CreateAsync();
        browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});
        context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});
        page=await context.NewPageAsync();
        await Goto("en","/login");
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??throw new InvalidOperationException("BARANG_E2E_USERNAME is required."));
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??throw new InvalidOperationException("BARANG_E2E_PASSWORD is required."));
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.DoesNotContain("/login",page.Url,StringComparison.OrdinalIgnoreCase);
        if(new Uri(page.Url).AbsolutePath!="/company")await page.GotoAsync(baseUrl+"/company");
        await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company");
        await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
    }
    public async ValueTask DisposeAsync(){if(context is not null)await context.DisposeAsync();if(browser is not null)await browser.DisposeAsync();playwright?.Dispose();}
    public static TheoryData<string,string> Catalogs=>new(){
        {"customers","customer.group"},
        {"shared","price.group"},
        {"shared","legal.entity.type"},
        {"shared","payment.term"},
        {"shared","payment.condition"},
        {"shared","payment.method"},
        {"shared","tax.status"},
        {"shared","address.type"},
        {"shared","contact.role"},
        {"shared","bank"},
        {"shared","bank.account.type"},
        {"products","product.type"},
        {"products","product.size"},
        {"products","product.color"},
        {"shared","unit.of.measure"},
        {"products","product.brand"},
        {"products","product.material"},
        {"products","product.gender"},
        {"products","product.fit"},
        {"products","product.style"},
        {"artwork","artwork.category"},
        {"artwork","artwork.tag"},
        {"orders","order.type"},
        {"inventory","inventory.adjustment.reason"},
        {"inventory","equipment.type"},
        {"inventory","equipment.status"},
        {"production","print.method"},
        {"production","print.direction"},
        {"production","print.size"},
        {"quality","defect.category"},
        {"quality","defect.type"},
        {"shared","packaging.type"},
        {"shared","shipping.method"},
        {"logistics","shipping.carrier"}
    };
    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task EveryCatalogSupportsStrictViewEditActivationAndSafeDelete(string domain,string type){
        string stamp=Guid.NewGuid().ToString("N"),code="e2e."+stamp,name="Reference "+stamp;
        if(type is "artwork.tag" or "defect.type") code=code.ToUpperInvariant();
        string? categoryName=null;
        if(type=="defect.type"){
            categoryName="Category "+stamp;
            await Goto("en","/master/reference-data/quality/defect.category");
            await CreateValue(page.Locator("[data-reference-type='defect.category']"),"category."+stamp,categoryName);
        }
        await Goto("en",$"/master/reference-data/{domain}/{type}");
        if (type == "artwork.category")
        {
            await Assertions.Expect(page.Locator(".design-category-tree")).ToBeVisibleAsync();
            Assert.Equal(0, await page.Locator(".reference-editor").CountAsync());
            return;
        }
        ILocator grid=page.Locator($"[data-reference-type='{type}']");
        await Assertions.Expect(grid).ToBeVisibleAsync();
        await CreateValue(grid,code,name,categoryName);
        await grid.Locator(".search-box input").FillAsync(code);
        ILocator row=grid.Locator("tbody tr").Filter(new(){HasText=code});
        await Assertions.Expect(row).ToContainTextAsync(name);
        string id=(await row.GetAttributeAsync("data-id"))!;
        Assert.True(Guid.TryParse(id,out _));
        await Action(row,"eye").ClickAsync();await Assertions.Expect(Editor.Locator("[name=reference-name-en]")).ToBeDisabledAsync();await Assertions.Expect(Editor.Locator("footer .btn-primary")).ToHaveCountAsync(0);await Cancel();
        await Action(row,"edit").ClickAsync();await Assertions.Expect(Editor.Locator("[name=reference-code]")).ToBeDisabledAsync();
        string edited=name+" Updated";await Editor.Locator("[name=reference-name-en]").FillAsync(edited);await Editor.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();await Assertions.Expect(row).ToContainTextAsync(edited);await Assertions.Expect(row).ToHaveAttributeAsync("data-id",id);
        await Action(row,"edit").ClickAsync();await Editor.Locator("[name=reference-name-en]").FillAsync("Discarded");await Cancel();await Assertions.Expect(row).ToContainTextAsync(edited);
        await Action(row,"power").ClickAsync();await Assertions.Expect(row).ToContainTextAsync("Inactive");
        await Action(row,"power").ClickAsync();await Assertions.Expect(row.Locator(".status-badge")).ToHaveTextAsync("Active");
        await grid.Locator(".barang-sort-header").Filter(new(){HasText="Code"}).ClickAsync();
        await page.ReloadAsync(new(){WaitUntil=WaitUntilState.DOMContentLoaded});await grid.Locator(".search-box input").FillAsync(code);await Assertions.Expect(row).ToContainTextAsync(edited);
        var style=await row.Locator("td").Nth(1).EvaluateAsync<string>("e=>getComputedStyle(e).boxShadow");Assert.True(style is "none" or "",$"ordinary cell must not have a box shadow, actual: {style}");
        await Action(row,"trash").ClickAsync();
        ILocator confirm=page.Locator(".barang-modal:has(> header h2:text-is(\"Delete\"))");await confirm.Locator("footer .btn-danger").ClickAsync();await Assertions.Expect(confirm).ToBeHiddenAsync();await Assertions.Expect(row).ToBeHiddenAsync();
        await page.ReloadAsync(new(){WaitUntil=WaitUntilState.DOMContentLoaded});await grid.Locator(".search-box input").FillAsync(code);await Assertions.Expect(row).ToBeHiddenAsync();
    }
    [Theory]
    [InlineData("fa","rtl")]
    [InlineData("ar","rtl")]
    [InlineData("en","ltr")]
    public async Task NavigationIsIntegratedUniqueLocalizedAndResponsive(string culture,string direction){
        await Goto(culture,"/manage/customers");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang",culture);await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",direction);
        await Assertions.Expect(page.Locator(".sidebar-navigation > a[href='/manage/customers']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".nav-group a[href='/manage/customers']")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".sidebar a[href='/master/lookups']")).ToHaveCountAsync(0);
        ILocator master=page.Locator(".nav-group").Filter(new(){Has=page.Locator("a[href='/master/reference-data/customers']")});
        await master.Locator(":scope > summary").ClickAsync();
        int masterCategoryCount = await master.Locator(":scope > .nav-children > a").CountAsync();
        Assert.True(masterCategoryCount > 0, "The rendered Master Data group must expose its current configured categories.");
        await Assertions.Expect(master.Locator(".nav-subgroup")).ToHaveCountAsync(0);
        ILocator system=page.Locator("[data-navigation-group=Nav_SystemManagement]");
        await Assertions.Expect(system).ToHaveCountAsync(1);
        string[] requiredSystem=["/master/organization-chart","/master/persons","/master/suppliers","/master/equipment","/manage/products"];
        Assert.Equal(requiredSystem,await system.Locator(":scope > .nav-children > a").EvaluateAllAsync<string[]>("links=>links.map(a=>a.getAttribute('href'))"));
        string[] expectedSystemLabels=culture switch {"en"=>["Organizational Chart","Persons","Suppliers","Company Equipment","Products"],"ar"=>["الهيكل التنظيمي","الأشخاص","الموردون","معدات الشركة","المنتجات"],_=>["چارت سازمانی","اشخاص","تأمین‌کنندگان","تجهیزات شرکت","محصولات"]};
        await Assertions.Expect(system.Locator("a[href='/master/suppliers']")).ToHaveTextAsync(expectedSystemLabels[2]);
        await Assertions.Expect(system.Locator("a[href='/master/equipment']")).ToHaveTextAsync(expectedSystemLabels[3]);
        string[] domains=["shared","customers","suppliers","products","artwork","orders","pricing","finance","inventory","production","quality","logistics","workflow"];
        foreach(string domain in domains){
            await Goto(culture,$"/master/reference-data/{domain}");
            ILocator workspace=page.Locator($"[data-domain='{domain}']");await Assertions.Expect(workspace).ToBeVisibleAsync();
            await Assertions.Expect(workspace.Locator(".reference-navigation")).ToBeVisibleAsync();
            ILocator links=workspace.Locator(".reference-navigation a");int count=await links.CountAsync();
            if(domain is "workflow" or "finance" or "pricing"){Assert.Equal(0,count);await Assertions.Expect(workspace.Locator(".reference-content p")).ToBeVisibleAsync();}
            else{
                Assert.True(count>0,$"reference workspace '{domain}' should expose at least one catalog");
                string href=(await links.Last.GetAttributeAsync("href"))!;
                await links.Last.ClickAsync();
                await Assertions.Expect(workspace.Locator(".reference-navigation a[aria-current=page]")).ToHaveCountAsync(1);
                await Assertions.Expect(workspace.Locator(".reference-navigation a[aria-current=page]")).ToHaveAttributeAsync("href",href);
                await Assertions.Expect(workspace.Locator("[data-reference-type]")).ToHaveAttributeAsync("data-reference-type",href.Split('/').Last());
            }
        }
        await page.SetViewportSizeAsync(390,844);
        await Goto(culture,"/master/reference-data/customers");
        ILocator panel=page.Locator(".reference-navigation");await panel.Locator("summary").ClickAsync();await Assertions.Expect(panel.Locator("nav")).ToBeHiddenAsync();await panel.Locator("summary").ClickAsync();await Assertions.Expect(panel.Locator("nav")).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=document.documentElement.clientWidth+1"));
        Assert.DoesNotContain("Nav_Reference_",await page.Locator(".sidebar").InnerTextAsync());
    }

    [Theory]
    [InlineData("fa", "داده‌های پایه قیمت‌گذاری")]
    [InlineData("ar", "البيانات المرجعية للتسعير")]
    [InlineData("en", "Pricing Reference Data")]
    public async Task PricingReferenceDataUsesOneIntegratedWorkspaceWithoutUnapprovedCatalogs(string culture, string title)
    {
        await Goto(culture, "/master/reference-data/pricing");
        await Assertions.Expect(page.Locator("header.crud-header h1")).ToHaveTextAsync(title);
        ILocator workspace = page.Locator("[data-domain='pricing']");
        await Assertions.Expect(workspace).ToBeVisibleAsync();
        await Assertions.Expect(workspace.Locator(".reference-navigation")).ToBeVisibleAsync();
        await Assertions.Expect(workspace.Locator(".reference-navigation a")).ToHaveCountAsync(0);
        await Assertions.Expect(workspace.Locator(".reference-content p")).ToBeVisibleAsync();
        ILocator master = page.Locator(".nav-group").Filter(new(){Has=page.Locator("a[href='/master/reference-data/pricing']")});
        await Assertions.Expect(master.Locator(":scope > .nav-children > a[href='/master/reference-data/pricing']")).ToHaveCountAsync(1);
        await Assertions.Expect(master.Locator(".nav-subgroup")).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("fa", "داده‌های پایه تجهیزات", "دسته‌بندی تجهیزات")]
    [InlineData("ar", "البيانات المرجعية للمعدات", "فئات المعدات")]
    [InlineData("en", "Equipment Reference Data", "Equipment Categories")]
    public async Task EquipmentReferenceWorkspaceUsesTheSelectedLanguage(string culture, string title, string category)
    {
        await Goto(culture, "/master/reference-data/equipment/equipment.category");
        await Assertions.Expect(page.Locator("header.crud-header h1")).ToHaveTextAsync(title);
        await Assertions.Expect(page.Locator(".reference-navigation a").First).ToHaveTextAsync(category);
        await Assertions.Expect(page.Locator(".reference-content [data-reference-type='equipment.category'] h2")).ToHaveTextAsync(category);
        string body = await page.Locator("main").InnerTextAsync();
        Assert.DoesNotContain(culture == "fa" ? "Equipment" : culture == "ar" ? "Equipment" : "داده‌های پایه", body, StringComparison.Ordinal);
    }
    [Fact]
    public async Task CustomerSelectorShowsInactiveHistoryAndSwitchesCaptionsWithoutChangingIdentity(){
        string stamp=Guid.NewGuid().ToString("N")[..12],code="history."+stamp,name="History "+stamp,customerCode="HIST"+stamp;
        await Goto("en","/master/reference-data/customers/customer.group");
        ILocator grid=page.Locator("[data-reference-type='customer.group']");
        await CreateValue(grid,code,name);
        await Goto("en","/manage/customers");
        await page.Locator("header.customer-page-header button.btn-primary").ClickAsync();
        ILocator root=page.Locator(".barang-modal:has(> .dialog-body > .customer-tabs)");
        await Field(root,"Customer Number").FillAsync(customerCode);await Field(root,"Mobile").FillAsync("09123334444");await Field(root,"First Name").FillAsync("Reference");await Field(root,"Last Name").FillAsync(stamp);
        ILocator input=root.Locator("input[name='customer.group']");await input.FillAsync(name);await input.Locator("..").GetByRole(AriaRole.Option,new(){Name=name,Exact=true}).ClickAsync();
        await root.Locator(".customer-editor-actions .btn-primary").ClickAsync();await Assertions.Expect(root).ToBeHiddenAsync();
        await Goto("en","/master/reference-data/customers/customer.group");await grid.Locator(".search-box input").FillAsync(code);ILocator value=grid.Locator("tbody tr").Filter(new(){HasText=code});await Action(value,"power").ClickAsync();await Assertions.Expect(value).ToContainTextAsync("Inactive");
        await Action(value,"trash").ClickAsync();ILocator confirm=page.Locator(".barang-modal:has(> header h2:text-is(\"Delete\"))");await confirm.Locator("footer .btn-danger").ClickAsync();await Assertions.Expect(confirm.Locator(".inline-error")).ToContainTextAsync("Deactivate");await confirm.Locator("footer .btn-secondary").ClickAsync();
        foreach(string culture in new[]{"fa","ar","en"}){
            await Goto(culture,"/manage/customers");await page.Locator(".customer-toolbar .customer-search input").FillAsync(customerCode);
            ILocator customer=page.Locator(".customer-grid tbody tr").Filter(new(){HasText=customerCode});await Action(customer,"edit").ClickAsync();
            await Assertions.Expect(root.Locator("input[name='customer.group']")).ToHaveValueAsync(new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(culture=="fa"?"فارسی "+name:culture=="ar"?"عربي "+name:name)));
            await root.Locator(".customer-editor-actions .btn-primary").ClickAsync();await Assertions.Expect(root).ToBeHiddenAsync();await Assertions.Expect(customer).ToBeVisibleAsync();
        }
        await Goto("en","/manage/customers");await page.Locator("header.customer-page-header button.btn-primary").ClickAsync();
        await input.FillAsync(name);await Assertions.Expect(input.Locator("..").GetByRole(AriaRole.Option,new(){Name=name,Exact=true})).ToHaveCountAsync(0);
        await root.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
    }

    [Theory]
    [InlineData("products","product.type","/manage/products",true,0)]
    [InlineData("inventory","equipment.type","/master/equipment",true,0)]
    [InlineData("logistics","shipping.carrier","/workspace/logistics",false,1)]
    [InlineData("artwork","artwork.tag","/manage/artwork-files",false,0)]
    [InlineData("quality","defect.type","/workspace/quality",false,0)]
    public async Task CrossDomainSelectorsDisplayCatalogCaptionsInEveryLanguage(string domain,string type,string route,bool modal,int index)
    {
        string stamp=Guid.NewGuid().ToString("N")[..12],code="cross."+stamp,name="Cross "+stamp;
        if(type is "artwork.tag" or "defect.type")code=code.ToUpperInvariant();
        string? category=null;
        if(type=="defect.type"){
            category="Category "+stamp;
            await Goto("en","/master/reference-data/quality/defect.category");
            await CreateValue(page.Locator("[data-reference-type='defect.category']"),"category."+stamp,category);
        }
        await Goto("en",$"/master/reference-data/{domain}/{type}");
        await CreateValue(page.Locator($"[data-reference-type='{type}']"),code,name,category);
        foreach(string culture in new[]{"fa","ar","en"}){
            await Goto(culture,route);
            if(modal)await page.Locator(".crud-header > button.btn-primary").ClickAsync();
            ILocator host=modal?page.Locator(".crud-dialog"):page.Locator("main");
            ILocator input = type == "equipment.type"
                ? host.Locator("input[name='equipmentTypeCode']")
                : host.Locator("input[role=combobox]").Nth(index);
            string caption=culture=="fa"?"فارسی "+name:culture=="ar"?"عربي "+name:name;
            await input.FillAsync(caption);
            await input.Locator("..").GetByRole(AriaRole.Option,new(){Name=caption,Exact=true}).ClickAsync();
            await Assertions.Expect(input).ToHaveValueAsync(caption);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");
            if(modal)await host.Locator("footer .btn-secondary").ClickAsync();
        }
        await RemoveReference(domain,type,code);
        if(category is not null)await RemoveReference("quality","defect.category","category."+stamp);
    }
    private async Task RemoveReference(string domain,string type,string code)
    {
        await Goto("en",$"/master/reference-data/{domain}/{type}");
        var grid=page.Locator($"[data-reference-type='{type}']");await grid.Locator(".search-box input").FillAsync(code);
        var row=grid.Locator("tbody tr").Filter(new(){HasText=code});await Assertions.Expect(row).ToHaveCountAsync(1);
        await Action(row,"trash").ClickAsync();var confirm=page.Locator(".barang-modal:has(> header h2:text-is(\"Delete\"))");
        await confirm.Locator("footer .btn-danger").ClickAsync();await Assertions.Expect(confirm).ToBeHiddenAsync();await Assertions.Expect(row).ToHaveCountAsync(0);
    }

    private ILocator Editor=>page.Locator(".barang-modal:has(> .dialog-body > .reference-editor)");
    private Task<Microsoft.Playwright.IResponse?> Goto(string culture,string path)=>page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl={Uri.EscapeDataString(path)}",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
    private static ILocator Action(ILocator row,string icon)=>row.Locator($".barang-icon-button:has(svg[data-icon='{icon}'])");
    private static ILocator Field(ILocator root,string label)=>root.Locator("label").Filter(new(){HasText=label}).Locator("input").First;
    private async Task Cancel(){await Editor.Locator("footer .btn-secondary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();}
    private async Task CreateValue(ILocator grid,string code,string name,string? categoryName=null){await grid.GetByRole(AriaRole.Button,new(){Name="Add",Exact=true}).ClickAsync();await Editor.Locator("[name=reference-code]").FillAsync(code);foreach(string culture in new[]{"fa","ar","en"})await Editor.Locator($"[name='reference-name-{culture}']").FillAsync(culture=="fa"?"فارسی "+name:culture=="ar"?"عربي "+name:name);if(categoryName is not null){var category=Editor.Locator("[name=reference-category]");await category.FillAsync(categoryName);await category.Locator("..").GetByRole(AriaRole.Option,new(){Name=categoryName,Exact=true}).ClickAsync();await Editor.Locator("[name=reference-severity]").SelectOptionAsync("Major");await Editor.Locator("[name=reference-blocking]").CheckAsync();}await Editor.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();}
}
