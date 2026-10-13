using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using BARANG.TestSupport;
using Microsoft.Playwright;
using static BARANG.TestSupport.SupplierReferenceScenario;

namespace BARANG.Backoffice.E2ETests;

public sealed class SupplierCoreE2ETests:IAsyncLifetime
{
    private static readonly Guid Company=Guid.Parse("42f0e459-6095-45bb-aa13-f38126d91d9c"),Other=Guid.Parse("d82f44d5-8d22-4154-8eed-e7075ee6ce9f");
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private readonly string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
    private readonly HttpClient client=new(){BaseAddress=new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL")??"http://localhost:8080")};
    private IPlaywright playwright=null!;private IBrowser browser=null!;private IBrowserContext context=null!;private IPage page=null!;private SupplierReferenceScenario api=null!;
    private readonly Dictionary<string,JsonElement> refs=[];
    private readonly List<(Guid Company,Guid Id)> suppliers=[];
    private ILocator Editor=>page.Locator("[data-supplier-editor]");
    private ILocator Grid=>page.Locator("[data-supplier-directory]");
    private static string Username=>Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??throw new InvalidOperationException("E2E username is required.");
    private static string Password=>Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??throw new InvalidOperationException("E2E password is required.");
    public async ValueTask InitializeAsync(){
        var login=await SupplierReferenceScenario.Send(client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username=Username,password=Password},200,Ct);
        api=new(client,login.GetProperty("accessToken").GetString()!,Company,Ct);await api.Send(HttpMethod.Post,"/api/v1/auth/me/company",new{companyId=Company});
        playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});
        context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Flogin",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.Locator("#username").FillAsync(Username);await page.Locator("#password").FillAsync(Password);Task loginNavigation=page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/login",new(){WaitUntil=WaitUntilState.Commit});await page.Locator("button[type=submit]").ClickAsync();await loginNavigation;
        await Assertions.Expect(page.Locator(".company-list")).ToBeVisibleAsync();await SelectCompany(Company);
    }
    public async ValueTask DisposeAsync(){
        try{
            foreach(var(company,id)in suppliers){
                var owner=api.InCompany(company);
                foreach(string kind in new[]{"addresses","contacts","bank-accounts"}){
                    foreach(var child in(await owner.Send(HttpMethod.Get,$"/api/v1/suppliers/{id}/{kind}")).EnumerateArray())
                        await owner.Send(HttpMethod.Delete,$"/api/v1/suppliers/{id}/{kind}/{Id(child)}?version={Version(child)}");
                }
                var current=await owner.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}");
                await owner.Send(HttpMethod.Delete,$"/api/v1/suppliers/{id}?version={Version(current.GetProperty("supplier"))}");
            }
            foreach(var value in refs.Values)await api.Delete(await api.Value(Id(value)));
            if(api is not null)await api.Send(HttpMethod.Post,"/api/v1/auth/logout",expected:204);
        }finally{if(context is not null)await context.DisposeAsync();if(browser is not null)await browser.DisposeAsync();playwright?.Dispose();client.Dispose();}
    }
    [Theory]
    [InlineData("fa","Individual")][InlineData("ar","Individual")][InlineData("en","Individual")]
    [InlineData("fa","LegalEntity")][InlineData("ar","LegalEntity")][InlineData("en","LegalEntity")]
    public async Task FullSupplierAndChildCrudCommercialDatesAndMobileWorkInEachLanguage(string culture,string entityType){
        string stamp=Guid.NewGuid().ToString("N")[..10],code="SUP-E2E-"+stamp;
        foreach(string type in new[]{"supplier.category","supplier.grade","supplier.hold.reason","legal.entity.type","contact.role","address.type","bank","bank.account.type","payment.term","payment.condition","payment.method","shipping.method","freight.delivery.term"})
            refs[type]=await api.CreateValue(type,"coree2e."+stamp+"."+refs.Count.ToString(CultureInfo.InvariantCulture));
        await OpenDirectory();await Switch(culture);await Search(code,0);
        await page.Locator("[data-supplier-add]").ClickAsync();await Assertions.Expect(Editor).ToBeVisibleAsync();
        await Input(Editor,"supplierCode",code);await Input(Editor,"displayName","Supplier "+stamp);
        await Editor.Locator("[name=entityType]").SelectOptionAsync(entityType);
        await Input(Editor,"nationalId","NATIONAL-"+stamp);await Input(Editor,"primaryMobile","+989120000123");await Input(Editor,"primaryEmail",stamp+"@example.com");
        await Input(Editor,"notes","Initial notes");
        await Input(Editor,"cooperationStartDate",DateText(culture));await Editor.Locator("[name=cooperationStartDate]").BlurAsync();
        if(entityType=="LegalEntity"){
            await Input(Editor,"legalName","Legal "+stamp);await Input(Editor,"tradeName","Trade "+stamp);await Input(Editor,"registrationNo","REG-"+stamp);
            await Input(Editor,"establishmentDate",DateText(culture));await Editor.Locator("[name=establishmentDate]").BlurAsync();
            await Reference(Editor,"legalEntityTypeId","legal.entity.type",culture);
        }else{await Input(Editor,"firstName","Supplier");await Input(Editor,"lastName","Individual");await Input(Editor,"occupation","Supplier activity");}
        await Reference(Editor,"supplierCategories","supplier.category",culture);await Reference(Editor,"gradeId","supplier.grade",culture);
        await Tab("financial");
        foreach(var(key,type)in new[]{("paymentTermId","payment.term"),("paymentConditionId","payment.condition"),("paymentMethodId","payment.method"),("shippingMethodId","shipping.method"),("freightDeliveryTermId","freight.delivery.term")})await Reference(Editor,key,type,culture);
        await Input(Editor,"defaultCurrencyCode","IRR");await Input(Editor,"minimumOrderAmount","125");await Input(Editor,"defaultLeadTimeDays","4");await Input(Editor,"baseDiscountPercent","7.5");await Input(Editor,"purchaseCreditLimit","2000");
        await Editor.Locator("[name=creditPurchaseAllowed]").CheckAsync();await Editor.Locator("[name=isPreferred]").CheckAsync();
        await Editor.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();await Search(code,1);
        var saved=Assert.Single((await api.Send(HttpMethod.Get,"/api/v1/suppliers/core?search="+code)).GetProperty("items").EnumerateArray());Guid id=Id(saved.GetProperty("supplier"));suppliers.Add((Company,id));
        ILocator row=Grid.Locator($"tbody tr[data-id='{id}']");
        Assert.Matches("^[1-9][0-9]*$",await row.Locator("td").First.InnerTextAsync());
        Assert.Equal("1985-06-08",saved.GetProperty("supplier").GetProperty("cooperationStartDate").GetString());
        var detail=await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}");
        Assert.Equal(Id(refs["supplier.grade"]),detail.GetProperty("supplier").GetProperty("gradeId").GetGuid());
        Assert.Equal(Id(refs["payment.term"]),detail.GetProperty("financial").GetProperty("paymentTermId").GetGuid());
        Assert.Equal(7.5m,detail.GetProperty("financial").GetProperty("baseDiscountPercent").GetDecimal());
        Assert.Equal(Id(refs["supplier.category"]),Assert.Single(detail.GetProperty("categoryIds").EnumerateArray()).GetGuid());
        await Action(row,"eye").ClickAsync();await Assertions.Expect(Editor.Locator("footer .btn-primary")).ToHaveCountAsync(0);
        foreach(var control in await Editor.Locator("input,select,textarea").AllAsync())await Assertions.Expect(control).ToBeDisabledAsync();
        await Assertions.Expect(Editor.Locator("[name=cooperationStartDate]")).ToHaveValueAsync(DateText(culture));await CloseEditor();
        await Action(row,"edit").ClickAsync();await Input(Editor,"notes","Saved notes");await Editor.Locator("[name=status]").SelectOptionAsync("Inactive");
        await Editor.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();
        await Assertions.Expect(row.Locator(".customer-status")).ToHaveTextAsync(Caption("Inactive",culture));
        detail=await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}");Assert.Equal("Saved notes",detail.GetProperty("supplier").GetProperty("notes").GetString());
        await Action(row,"edit").ClickAsync();await Input(Editor,"notes","Discard me");await Editor.Locator("footer .btn-secondary").ClickAsync();
        await Assertions.Expect(Editor).ToBeVisibleAsync();await page.GetByRole(AriaRole.Button,new(){Name=Caption("Discard",culture),Exact=true}).ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();
        detail=await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}");Assert.Equal("Saved notes",detail.GetProperty("supplier").GetProperty("notes").GetString());
        await Action(row,"power").ClickAsync();await Assertions.Expect(row.Locator(".customer-status")).ToHaveTextAsync(Caption("Active",culture));
        await Action(row,"unlink").ClickAsync();await Reference(Editor,"holdReasonId","supplier.hold.reason",culture);
        await Editor.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();await Assertions.Expect(row.Locator(".customer-status")).ToHaveTextAsync(Caption("Blocked",culture));
        await Action(row,"restore").ClickAsync();await Assertions.Expect(row.Locator(".customer-status")).ToHaveTextAsync(Caption("Active",culture));
        foreach(string kind in new[]{"addresses","contacts","bank-accounts"}){
            await Action(row,"edit").ClickAsync();await Tab(kind);
            await ChildCrud(id,kind,culture);await CloseEditor();
        }
        await api.Activate(await api.Value(Id(refs["supplier.grade"])),false);
        foreach(string language in new[]{"fa","ar","en"}){
            await Switch(language);await Search(code,1);await Action(row,"eye").ClickAsync();
            await Assertions.Expect(Editor.Locator("[name=cooperationStartDate]")).ToHaveValueAsync(DateText(language));
            if(entityType=="LegalEntity")await Assertions.Expect(Editor.Locator("[name=establishmentDate]")).ToHaveValueAsync(DateText(language));
            await Assertions.Expect(Editor.Locator("[name=gradeId]")).ToHaveValueAsync(RefCaption("supplier.grade",language)+" ("+ReferenceCaption("InactiveHistorical",language)+")");
            await Font(language);await Modal(Editor);await CloseEditor();
        }
        await Action(row,"edit").ClickAsync();await Input(Editor,"notes","Inactive grade retained");await Editor.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();
        detail=await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}");Assert.Equal(Id(refs["supplier.grade"]),detail.GetProperty("supplier").GetProperty("gradeId").GetGuid());
        foreach(string language in new[]{"fa","ar","en"}){
            await Switch(language);await Search(code,1);await Action(row,"edit").ClickAsync();await page.SetViewportSizeAsync(390,844);await Modal(Editor);await Font(language);await CloseEditor();await page.SetViewportSizeAsync(1440,1000);
        }
        await page.ReloadAsync();await Search(code,1);
        Assert.True(await row.Locator(".entity-link").EvaluateAsync<bool>("el=>{const s=getComputedStyle(el);return ['borderTopWidth','borderRightWidth','borderBottomWidth','borderLeftWidth'].every(k=>s[k]==='0px')&&s.boxShadow==='none';}"));
        await Action(row,"trash").ClickAsync();await page.Locator("[data-supplier-delete] footer .btn-danger").ClickAsync();await Assertions.Expect(page.Locator("[data-supplier-delete]")).ToBeHiddenAsync();await Search(code,0);
        suppliers.Remove((Company,id));await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}",expected:404);
        await page.ReloadAsync();await Search(code,0);
    }
    private async Task ChildCrud(Guid owner,string kind,string culture){
        ILocator grid=page.Locator($"[data-supplier-child='{kind}']"),dialog=page.Locator($"[data-supplier-child-editor='{kind}']");
        string nameKey=kind=="addresses"?"addressName":kind=="contacts"?"firstName":"accountName",selected=kind=="contacts"?"isPrimary":"isDefault";
        string path=$"/api/v1/suppliers/{owner}/{kind}";
        for(int index=0;index<2;index++){
            await grid.Locator(".child-header .btn-primary").ClickAsync();await Assertions.Expect(dialog).ToBeVisibleAsync();await Input(dialog,nameKey,"Child "+index);
            if(kind=="addresses"){await Reference(dialog,"addressTypeId","address.type",culture);await Input(dialog,"fullAddress","Full supplier address "+index);await Input(dialog,"recipientName","Recipient");await Input(dialog,"email","address@example.com");}
            else if(kind=="contacts"){await Input(dialog,"lastName","Contact");await Reference(dialog,"contactRoleId","contact.role",culture);await Input(dialog,"extension","12");await Input(dialog,"mobile","+989120000124");}
            else{await Reference(dialog,"bankId","bank",culture);await Reference(dialog,"accountTypeId","bank.account.type",culture);await Input(dialog,"accountHolderNames","First; Second");await dialog.Locator("[name=ownershipType]").SelectOptionAsync("Joint");await Input(dialog,"accountNumber","ACCOUNT-"+index);await Input(dialog,"cardNumber","6037991234567890");await Input(dialog,"branchCode","123");await Input(dialog,"branchName","Main branch");}
            if(index==0)await dialog.Locator("[name="+selected+"]").CheckAsync();
            await dialog.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(dialog).ToBeHiddenAsync();await Assertions.Expect(grid.Locator("tbody tr")).ToHaveCountAsync(index+1);
        }
        var children=(await api.Send(HttpMethod.Get,path)).EnumerateArray().ToArray();Guid first=Id(children[0]),second=Id(children[1]);
        ILocator firstRow=grid.Locator($"tbody tr[data-id='{first}']"),secondRow=grid.Locator($"tbody tr[data-id='{second}']");
        await Action(secondRow,"check").ClickAsync();await Assertions.Expect(secondRow.Locator(".status-badge").Last).ToHaveTextAsync(Caption(kind=="contacts"?"Primary":"Default",culture));
        children=(await api.Send(HttpMethod.Get,path)).EnumerateArray().ToArray();Assert.Equal(second,Id(Assert.Single(children,x=>x.GetProperty(selected).GetBoolean())));
        if(kind=="addresses")Assert.Equal(second,Id((await api.Send(HttpMethod.Get,$"/api/v1/suppliers/{owner}/default-address")).GetProperty("address")));
        await Action(firstRow,"eye").ClickAsync();await Assertions.Expect(dialog.Locator("footer .btn-primary")).ToHaveCountAsync(0);
        foreach(var control in await dialog.Locator("input,select,textarea").AllAsync())await Assertions.Expect(control).ToBeDisabledAsync();
        await dialog.Locator("footer .btn-secondary").ClickAsync();await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Action(firstRow,"edit").ClickAsync();await Input(dialog,"notes","Child saved notes");await dialog.Locator("footer .btn-primary").ClickAsync();await Assertions.Expect(dialog).ToBeHiddenAsync();
        Assert.Equal("Child saved notes",(await api.Send(HttpMethod.Get,path)).EnumerateArray().Single(x=>Id(x)==first).GetProperty("notes").GetString());
        await Action(firstRow,"edit").ClickAsync();await Input(dialog,"notes","Discard child");await dialog.Locator("footer .btn-secondary").ClickAsync();
        await page.GetByRole(AriaRole.Button,new(){Name=Caption("Discard",culture),Exact=true}).ClickAsync();await Assertions.Expect(dialog).ToBeHiddenAsync();
        Assert.Equal("Child saved notes",(await api.Send(HttpMethod.Get,path)).EnumerateArray().Single(x=>Id(x)==first).GetProperty("notes").GetString());
        await Action(secondRow,"power").ClickAsync();await Assertions.Expect(secondRow.Locator(".status-badge").First).ToHaveTextAsync(CrudCaption("Inactive",culture));
        Assert.DoesNotContain((await api.Send(HttpMethod.Get,path)).EnumerateArray(),x=>x.GetProperty(selected).GetBoolean());
        await Action(secondRow,"power").ClickAsync();await Assertions.Expect(secondRow.Locator(".status-badge").First).ToHaveTextAsync(CrudCaption("Active",culture));
        await Action(secondRow,"check").ClickAsync();await Assertions.Expect(secondRow.Locator(".status-badge").Last).ToHaveTextAsync(Caption(kind=="contacts"?"Primary":"Default",culture));
        await CloseEditor();await page.ReloadAsync();await Ready("[data-supplier-directory] input[name=supplierSearch]","input");
        await Grid.Locator("[name=supplierSearch]").FillAsync("");
        var ownerRow=Grid.Locator($"tbody tr[data-id='{owner}']");
        var ownerDetail=await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{owner}");await Search(ownerDetail.GetProperty("supplier").GetProperty("supplierCode").GetString()!,1);
        await Action(ownerRow,"trash").ClickAsync();await page.Locator("[data-supplier-delete] footer .btn-danger").ClickAsync();
        await Assertions.Expect(page.Locator("[data-supplier-delete] .inline-error")).ToHaveTextAsync(Caption("DeleteProtected",culture));
        await page.Locator("[data-supplier-delete] footer .btn-secondary").ClickAsync();
        await Action(ownerRow,"edit").ClickAsync();await Tab(kind);
        await Assertions.Expect(grid.Locator("tbody tr")).ToHaveCountAsync(2);
        await Assertions.Expect(secondRow.Locator(".status-badge").Last).ToHaveTextAsync(Caption(kind=="contacts"?"Primary":"Default",culture));
        await Action(firstRow,"edit").ClickAsync();await page.SetViewportSizeAsync(390,844);await Modal(dialog);await Font(culture);await dialog.Locator("footer .btn-secondary").ClickAsync();await page.SetViewportSizeAsync(1440,1000);
        foreach(Guid id in new[]{first,second}){
            await Action(grid.Locator($"tbody tr[data-id='{id}']"),"trash").ClickAsync();
            var deletion=page.Locator($"[data-supplier-child-delete='{kind}']");await deletion.Locator("footer .btn-danger").ClickAsync();await Assertions.Expect(deletion).ToBeHiddenAsync();
        }
        await Assertions.Expect(grid.Locator("tbody tr")).ToHaveCountAsync(0);Assert.Empty((await api.Send(HttpMethod.Get,path)).EnumerateArray());
    }
    [Fact]public async Task CompanySwitchShowsOnlyCurrentSupplierDataInBothDirections(){
        string stamp=Guid.NewGuid().ToString("N")[..10];
        var a=await SeedSupplier(Company,"ISOLATE-A-"+stamp);var b=await SeedSupplier(Other,"ISOLATE-B-"+stamp);
        await OpenDirectory();await Search(stamp,1);await Assertions.Expect(Grid.Locator("tbody")).ToContainTextAsync("ISOLATE-A-"+stamp);await Assertions.Expect(Grid.Locator("tbody")).Not.ToContainTextAsync("ISOLATE-B-"+stamp);
        await SelectCompany(Other);await OpenDirectory();await Search(stamp,1);await Assertions.Expect(Grid.Locator("tbody")).ToContainTextAsync("ISOLATE-B-"+stamp);await Assertions.Expect(Grid.Locator("tbody")).Not.ToContainTextAsync("ISOLATE-A-"+stamp);
        await api.InCompany(Other).Send(HttpMethod.Get,$"/api/v1/suppliers/core/{a}",expected:404);await api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{b}",expected:404);
        await SelectCompany(Company);await OpenDirectory();await Search(stamp,1);await Assertions.Expect(Grid.Locator("tbody")).ToContainTextAsync("ISOLATE-A-"+stamp);await Assertions.Expect(Grid.Locator("tbody")).Not.ToContainTextAsync("ISOLATE-B-"+stamp);
    }
    [Fact]public async Task SupplierEditorVerticalTabsPreserveStateAndFitEveryTab(){
        await OpenDirectory();var first=Grid.Locator("tbody tr").First;await first.WaitForAsync();string code=await first.Locator(".entity-link").InnerTextAsync();
        foreach(string culture in new[]{"fa","ar","en"}){
            await Switch(culture);await Search(code,1);var row=Grid.Locator("tbody tr").First;
            await Action(row,"eye").ClickAsync();await Assertions.Expect(Editor).ToBeVisibleAsync();
            var tabs=Editor.Locator(".supplier-tabs button");Assert.Equal(12,await tabs.CountAsync());
            await Assertions.Expect(Editor.Locator(".supplier-tabs")).ToHaveAttributeAsync("aria-orientation","vertical");
            Assert.True(await Editor.EvaluateAsync<bool>("el=>{const m=el.matches('.barang-modal')?el:el.querySelector('.barang-modal'),b=m?.querySelector('.dialog-body');return !!m&&!!b&&m.scrollWidth<=m.clientWidth+1&&b.scrollWidth<=b.clientWidth+1&&[...m.querySelectorAll('.data-grid-wrap')].every(x=>x.scrollWidth<=x.clientWidth+1);}"));
            Assert.True(await Editor.EvaluateAsync<bool>("el=>{const n=el.querySelector('.supplier-tabs').getBoundingClientRect(),c=el.querySelector('.supplier-tab-content').getBoundingClientRect();return document.documentElement.dir==='rtl'?n.right>=c.right:n.left<=c.left; }"));
            foreach(var tab in await tabs.AllAsync()){await tab.ClickAsync();await Assertions.Expect(tab).ToHaveAttributeAsync("aria-selected","true");}
            await CloseEditor();
            await Action(row,"edit").ClickAsync();await Assertions.Expect(Editor).ToBeVisibleAsync();
            await Input(Editor,"notes","vertical state marker");await Tab("addresses");await Tab("general");await Assertions.Expect(Editor.Locator("[name=notes]")).ToHaveValueAsync("vertical state marker");
            if(culture=="en"){await page.SetViewportSizeAsync(390,844);await Modal(Editor);Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));await page.SetViewportSizeAsync(1440,1000);}
            await Editor.Locator(":scope > footer .btn-secondary").ClickAsync();await page.GetByRole(AriaRole.Button,new(){Name=Caption("Discard",culture),Exact=true}).ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();
        }
    }
    [Fact]public async Task GridSearchFiltersSortAndPagingRemainAuthoritative(){
        string stamp="GRID-"+Guid.NewGuid().ToString("N")[..8];
        for(int i=0;i<21;i++)await SeedSupplier(Company,stamp+"-"+i.ToString("D2",CultureInfo.InvariantCulture));
        await OpenDirectory();await Search(stamp+"-20",1);
        var rows=Grid.Locator("tbody tr");
        await Assertions.Expect(rows.Locator(".entity-link")).ToHaveTextAsync(stamp+"-20");
        await Search(stamp,20);
        await Assertions.Expect(rows.Locator(".entity-link")).ToHaveTextAsync(Enumerable.Range(0,20).Select(i=>stamp+"-"+i.ToString("D2",CultureInfo.InvariantCulture)).ToArray());
        long[] ascending=(await rows.Locator("td:first-child").AllTextContentsAsync()).Select(long.Parse).ToArray();Assert.Equal(ascending.Order(),ascending);
        await Grid.Locator(".barang-pagination button").Last.ClickAsync();await Assertions.Expect(rows).ToHaveCountAsync(1);
        await Grid.Locator(".barang-pagination button").First.ClickAsync();await Assertions.Expect(rows).ToHaveCountAsync(20);
        await Grid.Locator(".barang-sort-header").First.ClickAsync();
        await Assertions.Expect(rows.Locator("td:first-child").First).ToHaveTextAsync((ascending.Max()+1).ToString(CultureInfo.InvariantCulture));
        long[] descending=(await rows.Locator("td:first-child").AllTextContentsAsync()).Select(long.Parse).ToArray();Assert.Equal(descending.OrderDescending(),descending);
        await Grid.Locator("[name=supplierStatusFilter]").SelectOptionAsync("Inactive");await Assertions.Expect(rows).ToHaveCountAsync(0);
        await Grid.Locator("[name=supplierStatusFilter]").SelectOptionAsync("Active");await Assertions.Expect(rows).ToHaveCountAsync(20);
        await Grid.Locator("[name=supplierTypeFilter]").SelectOptionAsync("LegalEntity");await Assertions.Expect(rows).ToHaveCountAsync(0);
        await Grid.Locator("[name=supplierTypeFilter]").SelectOptionAsync("Individual");await Assertions.Expect(rows).ToHaveCountAsync(20);
    }
    private async Task<Guid> SeedSupplier(Guid company,string code){
        var target=api.InCompany(company);Guid category=Id((await target.Options("supplier.category","en",true)).EnumerateArray().First());
        var value=await target.Send(HttpMethod.Post,"/api/v1/suppliers/core",new{identity=new{supplierCode=code,displayName=code,entityType="Individual",status="Active"},categoryIds=new[]{category}},201);
        Guid id=Id(value.GetProperty("supplier"));suppliers.Add((company,id));return id;
    }
    private async Task OpenDirectory(){await page.GotoAsync(baseUrl+"/master/suppliers",new(){WaitUntil=WaitUntilState.DOMContentLoaded});await Assertions.Expect(Grid).ToBeVisibleAsync();await Ready("[data-supplier-directory] input[name=supplierSearch]","input");}
    private async Task SelectCompany(Guid company){
        if(new Uri(page.Url).AbsolutePath!="/company")await page.GotoAsync(baseUrl+"/company",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.Locator($".company-list form:has(input[name=companyId][value='{company}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company",new(){WaitUntil=WaitUntilState.DOMContentLoaded});await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
    }
    private async Task Search(string query,int count){await Ready("[data-supplier-directory] input[name=supplierSearch]","input");await Grid.Locator("[name=supplierSearch]").FillAsync(query);await Assertions.Expect(Grid.Locator("tbody tr")).ToHaveCountAsync(count);}
    private async Task Switch(string culture){
        await Ready($".language-switch a[hreflang='{culture}']","click");
        string marker=Guid.NewGuid().ToString("N");await page.EvaluateAsync("m=>window.__supplierCoreDocument=m",marker);
        await page.Locator($".language-switch a[hreflang='{culture}']").ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang",culture);await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(Caption("Title",culture));Assert.Equal(marker,await page.EvaluateAsync<string>("()=>window.__supplierCoreDocument"));
    }
    private Task<Microsoft.Playwright.IJSHandle> Ready(string selector,string eventName)=>page.WaitForFunctionAsync("({selector,eventName})=>{const el=document.querySelector(selector);return !!el&&Object.keys(el).some(k=>k.startsWith('_blazorEvents_')&&!!el[k].getHandler?.(eventName));}",new{selector,eventName},new(){Timeout=15000});
    private async Task Tab(string name){await Editor.Locator($"[data-supplier-tab='{name}']").ClickAsync();await Assertions.Expect(Editor.Locator($"[data-supplier-tab='{name}']")).ToHaveAttributeAsync("aria-selected","true");}
    private async Task CloseEditor(){await Editor.Locator(":scope > footer .btn-secondary").ClickAsync();await Assertions.Expect(Editor).ToBeHiddenAsync();}
    private static ILocator Action(ILocator row,string icon)=>row.Locator($".barang-icon-button:has(svg[data-icon='{icon}'])");
    private static Task Input(ILocator parent,string name,string value)=>parent.Locator($"[name='{name}']").FillAsync(value);
    private async Task Reference(ILocator parent,string name,string type,string culture){
        var input=parent.Locator($"[name='{name}']");await Assertions.Expect(input).ToBeEnabledAsync();await input.FillAsync(RefCaption(type,culture));
        await input.Locator("..").GetByRole(AriaRole.Option,new(){Name=RefCaption(type,culture),Exact=true}).ClickAsync();
        if(name!="supplierCategories")await Assertions.Expect(input).ToHaveValueAsync(RefCaption(type,culture));
        else await Assertions.Expect(parent.Locator($"[data-reference-id='{Id(refs[type])}']")).ToBeVisibleAsync();
    }
    private string RefCaption(string type,string culture)=>Captions(refs[type].GetProperty("code").GetString()!)[culture];
    private static string DateText(string culture)=>culture=="fa"?"1364/03/18":"1985/06/08";
    private async Task Modal(ILocator dialog){
        var box=await dialog.BoundingBoxAsync();Assert.NotNull(box);
        Assert.True(box.X>=0&&box.Y>=0&&box.X+box.Width<=page.ViewportSize!.Width+1&&box.Y+box.Height<=page.ViewportSize.Height+1);
        Assert.True(await dialog.Locator(":scope > footer").EvaluateAsync<bool>("el=>{const r=el.getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight+1;}"));
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
    }
    private async Task Font(string culture){
        string family=culture=="fa"?"Vazirmatn":culture=="ar"?"Noto Sans Arabic":"Inter";
        Assert.True(await page.EvaluateAsync<bool>("async f=>{const faces=await document.fonts.load('400 14px \"'+f+'\"');return faces.length>0&&faces.every(x=>x.status==='loaded');}",family));
        string[] failures=await page.EvaluateAsync<string[]>(@"family=>{
          const primary=s=>s.split(',')[0].trim().replaceAll('""','').replaceAll(""'"",'');
          return [...document.querySelectorAll('h1,[data-supplier-directory] td,[data-supplier-editor] label,[data-supplier-editor] input,[data-supplier-editor] button,[data-supplier-child-editor] input,[data-supplier-child-editor] button')]
            .filter(el=>el.getClientRects().length&&primary(getComputedStyle(el).fontFamily)!==family).map(el=>el.tagName+':'+getComputedStyle(el).fontFamily);
        }",family);Assert.Empty(failures);
    }
    private static string Caption(string key,string culture)=>Resource("SupplierStrings",key,culture);
    private static string CrudCaption(string key,string culture)=>Resource("CrudStrings",key,culture);
    private static string ReferenceCaption(string key,string culture)=>Resource("ReferenceStrings",key,culture);
    private static string Resource(string family,string key,string culture){
        DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        var doc=XDocument.Load(Path.Combine(root!.FullName,"01_CORE","src","BARANG.Backoffice","Localization",family+(culture=="fa"?"":"."+culture)+".resx"));
        return doc.Root!.Elements("data").Single(x=>x.Attribute("name")!.Value==key).Element("value")!.Value;
    }
}
