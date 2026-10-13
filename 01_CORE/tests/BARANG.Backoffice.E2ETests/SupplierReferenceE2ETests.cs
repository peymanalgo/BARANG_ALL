using System.Text.Json;
using System.Xml.Linq;
using BARANG.TestSupport;
using Microsoft.Playwright;
using static BARANG.TestSupport.SupplierReferenceScenario;

namespace BARANG.Backoffice.E2ETests;

public sealed class SupplierReferenceE2ETests:IAsyncLifetime
{
    private static readonly Guid Company=Guid.Parse("42f0e459-6095-45bb-aa13-f38126d91d9c");
    private static readonly Guid Branch=Guid.Parse("2509b4d4-aef0-4d80-8569-b001c6c41190");
    private readonly string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
    private readonly HttpClient client=new(){BaseAddress=new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL")??"http://localhost:8080")};
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private IPlaywright playwright=null!;
    private IBrowser browser=null!;
    private IBrowserContext context=null!;
    private IPage page=null!;
    private SupplierReferenceScenario api=null!;
    private readonly List<Guid> disposableValues=[];
    private ILocator Editor=>page.Locator(".barang-modal:has(> .dialog-body > .reference-editor)");
    private ILocator DeleteDialog=>page.Locator(".barang-modal:has(footer .btn-danger)");
    public static TheoryData<string> Catalogs=>new(WorkspaceTypes);
    private static string Username=>Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??throw new InvalidOperationException("E2E username is required.");
    private static string Password=>Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??throw new InvalidOperationException("E2E password is required.");

    public async ValueTask InitializeAsync()
    {
        JsonElement login=await SupplierReferenceScenario.Send(client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username=Username,password=Password},200,Ct);
        api=new(client,login.GetProperty("accessToken").GetString()!,Company,Ct);
        await api.Send(HttpMethod.Post,"/api/v1/auth/me/company",new{companyId=Company});
        playwright=await Playwright.CreateAsync();
        browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});
        context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});
        page=await context.NewPageAsync();
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Flogin");
        await page.Locator("#username").FillAsync(Username);await page.Locator("#password").FillAsync(Password);
        Task loginNavigation=page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/login",new(){WaitUntil=WaitUntilState.Commit});
        await page.Locator("button[type=submit]").ClickAsync();await loginNavigation;
        if(new Uri(page.Url).AbsolutePath!="/company")await page.GotoAsync(baseUrl+"/company");
        Task companyNavigation=page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company",new(){WaitUntil=WaitUntilState.Commit});
        await page.Locator($".company-list form:has(input[name=companyId][value='{Company}']) button[type=submit]").ClickAsync();await companyNavigation;
        await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
    }
    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach(Guid id in disposableValues)Assert.True((await api.Delete(await api.Value(id))).GetBoolean());
            if(api is not null)await api.Send(HttpMethod.Post,"/api/v1/auth/logout",expected:204);
        }
        finally
        {
            if(context is not null)await context.DisposeAsync();
            if(browser is not null)await browser.DisposeAsync();
            playwright?.Dispose();client.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task EachApprovedCatalogSupportsCompleteCrudPagingAndLiveLocalizedIdentity(string type)
    {
        string prefix="sre2e."+Guid.NewGuid().ToString("N")[..12]+".",code=prefix+"20";
        for(int i=0;i<20;i++)disposableValues.Add(Id(await api.CreateValue(type,prefix+i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture),i)));
        ILocator grid=await Open(type);
        await Search(grid,prefix,20);
        await Assertions.Expect(grid.Locator("tbody tr")).ToHaveCountAsync(20);
        await grid.Locator(".section-header .btn-primary").ClickAsync();
        await Assertions.Expect(Editor).ToBeVisibleAsync();
        await Editor.Locator("[name=reference-code]").FillAsync(code);
        await FillCaptions(Captions(code),"Initial description");
        await Editor.Locator("[name=reference-order]").FillAsync("21");
        await Editor.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(Editor).ToBeHiddenAsync();
        await Assertions.Expect(grid.Locator(".barang-pagination button").Last).ToBeEnabledAsync();
        await Assertions.Expect(grid.Locator("tbody tr td:first-child")).ToHaveTextAsync(Enumerable.Range(0,20).Select(i=>prefix+i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)));
        await grid.Locator(".barang-pagination button").Last.ClickAsync();
        await Assertions.Expect(grid.Locator("tbody tr td:first-child")).ToHaveTextAsync(new[]{code});
        await grid.Locator(".barang-pagination button").First.ClickAsync();
        await grid.Locator(".barang-sort-header").Nth(0).ClickAsync();
        await Assertions.Expect(grid.Locator("tbody tr td:first-child")).ToHaveTextAsync(Enumerable.Range(1,20).Reverse().Select(i=>prefix+i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)));
        await grid.Locator(".barang-pagination button").Last.ClickAsync();
        await Assertions.Expect(grid.Locator("tbody tr td:first-child")).ToHaveTextAsync(new[]{prefix+"00"});
        await grid.Locator(".barang-pagination button").First.ClickAsync();
        await grid.Locator(".barang-sort-header").Nth(1).ClickAsync();
        await Assertions.Expect(grid.Locator("tbody tr td:first-child")).ToHaveTextAsync(Enumerable.Range(0,20).Select(i=>prefix+i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)));
        await Search(grid,code);
        ILocator row=grid.Locator("tbody tr");
        await Assertions.Expect(row).ToHaveCountAsync(1);
        Guid id=Guid.Parse((await row.GetAttributeAsync("data-id"))!);disposableValues.Add(id);
        await Action(row,"eye").ClickAsync();
        await Assertions.Expect(Editor.Locator("[name=reference-name-en]")).ToBeDisabledAsync();
        await Assertions.Expect(Editor.Locator("footer .btn-primary")).ToHaveCountAsync(0);
        await Cancel();
        await Action(row,"edit").ClickAsync();
        await Assertions.Expect(Editor.Locator("[name=reference-code]")).ToBeDisabledAsync();
        Dictionary<string,string> edited=Captions(code+" updated");
        await FillCaptions(edited,"Updated description");
        await Editor.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(Editor).ToBeHiddenAsync();
        await Assertions.Expect(row.Locator("td").Nth(1)).ToHaveTextAsync(edited["en"]);
        long version=Version(await api.Value(id));
        await Action(row,"edit").ClickAsync();
        await Editor.Locator("[name=reference-name-en]").FillAsync("Discarded supplier reference edit");
        await Cancel();
        Assert.Equal(version,Version(await api.Value(id)));
        await Assertions.Expect(row.Locator("td").Nth(1)).ToHaveTextAsync(edited["en"]);
        await Action(row,"power").ClickAsync();
        await Assertions.Expect(row.Locator(".status-badge")).ToHaveTextAsync(Caption("CrudStrings","Inactive","en"));

        foreach(string culture in new[]{"fa","ar","en"})
        {
            await Switch(culture);
            await Search(grid,code);
            await Assertions.Expect(row).ToHaveAttributeAsync("data-id",id.ToString());
            await Assertions.Expect(row.Locator("td").Nth(1)).ToHaveTextAsync(edited[culture]);
            await Assertions.Expect(row.Locator(".status-badge")).ToHaveTextAsync(Caption("CrudStrings","Inactive",culture));
            await Assertions.Expect(Action(row,"eye")).ToHaveAttributeAsync("title",Caption("CrudStrings","View",culture));
            await Assertions.Expect(Action(row,"edit")).ToHaveAttributeAsync("aria-label",Caption("CrudStrings","Edit",culture));
            Assert.DoesNotContain((await api.Options(type,culture,true)).EnumerateArray(),x=>Id(x)==id);
            JsonElement historical=Assert.Single((await api.Options(type,culture,false)).EnumerateArray(),x=>Id(x)==id);
            Assert.Equal(edited[culture],historical.GetProperty("displayName").GetString());
            Assert.Equal(code,historical.GetProperty("code").GetString());
            Assert.False(historical.GetProperty("isActive").GetBoolean());
            await AssertFont(culture);
            await Action(row,"eye").ClickAsync();
            await Assertions.Expect(Editor.Locator($"[name=reference-name-{culture}]")).ToHaveValueAsync(edited[culture]);
            await Assertions.Expect(Editor.Locator($"[name=reference-description-{culture}]")).ToHaveValueAsync("Updated description "+culture);
            await AssertFont(culture);await AssertModalViewport();await Cancel();
        }
        await page.SetViewportSizeAsync(390,844);
        await Action(row,"edit").ClickAsync();await AssertModalViewport();await AssertFont("en");await Cancel();
        await page.SetViewportSizeAsync(1440,1000);
        await Action(row,"power").ClickAsync();
        await Assertions.Expect(row.Locator(".status-badge")).ToHaveTextAsync(Caption("CrudStrings","Active","en"));
        Assert.Contains((await api.Options(type,"en",true)).EnumerateArray(),x=>Id(x)==id);
        await page.ReloadAsync();await Search(grid,code);
        await Assertions.Expect(row).ToHaveAttributeAsync("data-id",id.ToString());
        await Assertions.Expect(row.Locator("td").Nth(1)).ToHaveTextAsync(edited["en"]);
        Assert.Equal(0,await row.Locator("td").Nth(1).Locator(":scope > *").CountAsync());
        Assert.True(await row.Locator("td").Nth(1).EvaluateAsync<bool>("el=>{const s=getComputedStyle(el);return s.boxShadow==='none'&&s.borderLeftWidth==='0px'&&s.borderRightWidth==='0px';}"));
        await Action(row,"trash").ClickAsync();
        await DeleteDialog.Locator("footer .btn-danger").ClickAsync();
        await Assertions.Expect(DeleteDialog).ToBeHiddenAsync();
        await Assertions.Expect(row).ToHaveCountAsync(0);
        await api.Send(HttpMethod.Get,$"/api/v1/lookups/values/{id}",expected:404);disposableValues.Remove(id);
        await page.ReloadAsync();await Search(grid,code,0);
        await Assertions.Expect(row).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task EverySharedCatalogBlocksReferencedDeleteWithLocalizedReasonAndRetainsInactiveHistory()
    {
        string stamp=Guid.NewGuid().ToString("N")[..12];
        try
        {
            await api.SeedDependencies(Branch,stamp);
            foreach(var (type,initial) in api.References)
            {
                Guid id=Id(initial);string code=initial.GetProperty("code").GetString()!;
                ILocator grid=await Open(type);
                foreach(string culture in new[]{"fa","ar","en"})
                {
                    await Switch(culture,"shared");await Search(grid,code);
                    ILocator row=grid.Locator($"tbody tr[data-id='{id}']");
                    await Assertions.Expect(row).ToHaveCountAsync(1);
                    await Action(row,"trash").ClickAsync();
                    await DeleteDialog.Locator("footer .btn-danger").ClickAsync();
                    await Assertions.Expect(DeleteDialog.Locator(".inline-error")).ToHaveTextAsync(Caption("ReferenceStrings","Referenced",culture));
                    await Assertions.Expect(DeleteDialog).ToBeVisibleAsync();
                    Assert.Equal(id,Id(await api.Value(id)));
                    await DeleteDialog.Locator("footer .btn-secondary").ClickAsync();
                    await Assertions.Expect(DeleteDialog).ToBeHiddenAsync();
                }
                ILocator retained=grid.Locator($"tbody tr[data-id='{id}']");
                await Action(retained,"power").ClickAsync();
                await Assertions.Expect(retained.Locator(".status-badge")).ToHaveTextAsync(Caption("CrudStrings","Inactive","en"));
                foreach(string culture in new[]{"fa","ar","en"})
                {
                    Assert.DoesNotContain((await api.Options(type,culture,true)).EnumerateArray(),x=>Id(x)==id);
                    Assert.Contains((await api.Options(type,culture,false)).EnumerateArray(),x=>Id(x)==id);
                }
                await page.ReloadAsync();await Search(grid,code);
                await Assertions.Expect(retained.Locator(".status-badge")).ToHaveTextAsync(Caption("CrudStrings","Inactive","en"));
            }
        }
        finally { await api.RetireDependencies(); }
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task SupplierWorkspaceNavigationIsUniquePermissionBasedLocalizedAndResponsive(string culture)
    {
        await Open(WorkspaceTypes[0]);await Switch(culture);
        ILocator supplier=page.Locator(".sidebar a[href='/master/reference-data/suppliers']");
        await Assertions.Expect(supplier).ToHaveCountAsync(1);
        await Assertions.Expect(supplier).ToHaveTextAsync(Caption("NavigationStrings","Nav_Reference_suppliers",culture));
        await Assertions.Expect(page.Locator(".nav-group > .nav-children > a[href='/master/reference-data/suppliers']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".nav-subgroup a[href='/master/reference-data/suppliers']")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("[data-navigation-group=Nav_SystemManagement] a[href='/master/suppliers']")).ToHaveCountAsync(1);
        ILocator panel=page.Locator("[data-domain=suppliers] .reference-navigation");
        string[] routes=await panel.Locator("a").EvaluateAllAsync<string[]>("links=>links.map(a=>a.getAttribute('href').split('/').pop())");
        Assert.Equal(WorkspaceTypes.Order(),routes.Order());
        JsonElement domains=await api.Send(HttpMethod.Get,$"/api/v1/lookups/domains?culture={culture}");
        JsonElement catalogs=domains.EnumerateArray().Single(x=>x.GetProperty("code").GetString()=="suppliers").GetProperty("types");
        foreach(string type in WorkspaceTypes)
        {
            ILocator link=panel.Locator($"a[href='/master/reference-data/suppliers/{type}']");
            await Assertions.Expect(link).ToHaveTextAsync(catalogs.EnumerateArray().Single(x=>x.GetProperty("code").GetString()==type).GetProperty("caption").GetString()!);
            await link.ClickAsync();
            await Assertions.Expect(link).ToHaveAttributeAsync("aria-current","page");
            await Assertions.Expect(page.Locator("[data-reference-type]")).ToHaveAttributeAsync("data-reference-type",type);
            await Assertions.Expect(page.Locator(".reference-scope-note")).ToHaveTextAsync(Caption("ReferenceStrings",Global(type)?"GlobalScope":"CompanyScope",culture));
        }
        await AssertFont(culture);
        await page.SetViewportSizeAsync(390,844);
        await panel.Locator("summary").ClickAsync();await Assertions.Expect(panel.Locator("nav")).ToBeHiddenAsync();
        await panel.Locator("summary").ClickAsync();await Assertions.Expect(panel.Locator("nav")).ToBeVisibleAsync();
        Assert.Equal(390,await page.EvaluateAsync<int>("()=>innerWidth"));
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");
        await AssertFont(culture);
    }

    private async Task<ILocator> Open(string type)
    {
        string domain=Types.Contains(type,StringComparer.Ordinal)?"shared":"suppliers";
        await page.GotoAsync(baseUrl+$"/master/reference-data/{domain}");
        ILocator link=page.Locator($".reference-navigation a[href='/master/reference-data/{domain}/{type}']");
        await Assertions.Expect(link).ToBeVisibleAsync();await link.ClickAsync();
        ILocator grid=page.Locator($"[data-reference-type='{type}']");
        await Assertions.Expect(grid).ToBeVisibleAsync();await Ready($"[data-reference-type='{type}'] .search-box input","input");
        return grid;
    }
    private async Task Search(ILocator grid,string search,int expectedRows=1)
    {
        string type=(await grid.GetAttributeAsync("data-reference-type"))!;
        await Ready($"[data-reference-type='{type}'] .search-box input","input");
        await grid.Locator(".search-box input").FillAsync(search);
        await Assertions.Expect(grid.Locator("tbody tr")).ToHaveCountAsync(expectedRows);
    }
    private async Task Switch(string culture,string domain="suppliers")
    {
        string marker=Guid.NewGuid().ToString("N"),url=page.Url;
        await page.EvaluateAsync("value=>window.__supplierReferenceDocument=value",marker);
        await Ready($".language-switch a[hreflang='{culture}']","click");
        await page.Locator($".language-switch a[hreflang='{culture}']").ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang",culture);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(Caption("NavigationStrings",$"Nav_Reference_{domain}",culture));
        Assert.Equal(marker,await page.EvaluateAsync<string>("()=>window.__supplierReferenceDocument"));
        Assert.Equal(url,page.Url);
    }
    private Task<Microsoft.Playwright.IJSHandle> Ready(string selector,string eventName)=>page.WaitForFunctionAsync("({selector,eventName})=>{const el=document.querySelector(selector);return !!el&&Object.keys(el).some(k=>k.startsWith('_blazorEvents_')&&!!el[k].getHandler?.(eventName));}",new{selector,eventName});
    private async Task FillCaptions(Dictionary<string,string> names,string description)
    {
        foreach(string culture in new[]{"fa","ar","en"})
        {
            await Editor.Locator($"[name=reference-name-{culture}]").FillAsync(names[culture]);
            await Editor.Locator($"[name=reference-description-{culture}]").FillAsync(description+" "+culture);
        }
    }
    private async Task Cancel(){await Editor.Locator("footer .btn-secondary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();}
    private static ILocator Action(ILocator row,string icon)=>row.Locator($".barang-icon-button:has(svg[data-icon='{icon}'])");
    private async Task AssertModalViewport()
    {
        await Assertions.Expect(Editor).ToBeVisibleAsync();var box=await Editor.BoundingBoxAsync();Assert.NotNull(box);
        Assert.True(box.X>=0&&box.Y>=0&&box.X+box.Width<=page.ViewportSize!.Width+1&&box.Y+box.Height<=page.ViewportSize.Height+1);
        Assert.True(await Editor.Locator("footer").EvaluateAsync<bool>("el=>{const r=el.getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight+1;}"));
    }
    private async Task AssertFont(string culture)
    {
        string family=culture switch{"fa"=>"Vazirmatn","ar"=>"Noto Sans Arabic",_=>"Inter"};
        Assert.True(await page.EvaluateAsync<bool>("async family=>{const faces=await document.fonts.load('400 14px \"'+family+'\"');return faces.length>0&&faces.every(x=>x.status==='loaded');}",family));
        string[] failures=await page.EvaluateAsync<string[]>(@"family=>{
            const result=[];const primary=s=>s.split(',')[0].trim().replaceAll('""','').replaceAll(""'"",'');
            for(const el of document.querySelectorAll('.reference-navigation a,h1,[data-reference-type] td:nth-child(2),[data-reference-type] button,.reference-editor input,.reference-editor textarea,.barang-modal footer button')){
                if(el.getClientRects().length&&primary(getComputedStyle(el).fontFamily)!==family)result.push(el.tagName+':'+getComputedStyle(el).fontFamily);
            }return result;
        }",family);
        Assert.Empty(failures);
    }
    private static string Caption(string family,string key,string culture)
    {
        DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        string suffix=culture=="fa"?"":"."+culture;
        XDocument resource=XDocument.Load(Path.Combine(root?.FullName??throw new DirectoryNotFoundException(),"01_CORE","src","BARANG.Backoffice","Localization",family+suffix+".resx"));
        return resource.Root!.Elements("data").Single(x=>x.Attribute("name")!.Value==key).Element("value")!.Value;
    }
}
