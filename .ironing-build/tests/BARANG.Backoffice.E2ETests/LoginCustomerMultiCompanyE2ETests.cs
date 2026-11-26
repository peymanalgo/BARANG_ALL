using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class LoginCustomerMultiCompanyE2ETests : IAsyncLifetime
{
    private static readonly string[] SystemManagementRoutes = ["/manage/products", "/master/equipment", "/master/organization-chart", "/master/persons", "/master/suppliers"];
    private static readonly string[] CustomerStatuses = ["Active", "Inactive", "Blocked"];
    private static readonly string[] IranianBranches = ["تبریز", "تهران"];
    private static readonly Guid Iran=Guid.Parse("42f0e459-6095-45bb-aa13-f38126d91d9c");
    private static readonly Guid Iraq=Guid.Parse("d82f44d5-8d22-4154-8eed-e7075ee6ce9f");
    private static readonly Guid UnauthorizedCompany=Guid.Parse("aa4b06c1-9568-49c4-8b49-b9422309b0ca");
    private readonly string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
    private readonly HttpClient api=new(){BaseAddress=new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL")??"http://localhost:8080")};
    private IPlaywright playwright=null!;
    private IBrowser browser=null!;
    private string adminToken="";
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private static string Username=>Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
    private static string Password=>Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");

    public async ValueTask InitializeAsync()
    {
        playwright=await Playwright.CreateAsync();
        browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});
        adminToken=(await Api(HttpMethod.Post,"/api/v1/auth/login",null,null,new{username=Username,password=Password})).GetProperty("accessToken").GetString()!;
    }
    public async ValueTask DisposeAsync(){api.Dispose();await browser.DisposeAsync();playwright.Dispose();}

    [Theory]
    [InlineData("fa")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task CustomerGeneralSavesStatusAndNotesTogetherAndNavigationIsDirect(string culture)
    {
        string marker="UX"+Guid.NewGuid().ToString("N")[..12];
        JsonElement created=await CreateCustomer(Iran,marker+"A");
        Guid id=created.GetProperty("id").GetGuid();
        await using IBrowserContext context=await NewContext();
        IPage page=await context.NewPageAsync();
        try
        {
            await Login(page,culture,Username,Password,Iran);
            await page.GotoAsync(baseUrl+"/master/reference-data/customers");
            await Assertions.Expect(page.Locator(".reference-workspace")).ToBeVisibleAsync();
            ILocator master=page.Locator("[data-navigation-group=Nav_MasterData]");
            ILocator system=page.Locator("[data-navigation-group=Nav_SystemManagement]");
            await Assertions.Expect(system).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("[data-navigation-group=Nav_SystemManagement]")).ToHaveCountAsync(1);
            await Assertions.Expect(system.Locator("summary")).ToHaveTextAsync(Caption("NavigationStrings","Nav_SystemManagement",culture));
            Assert.Equal(SystemManagementRoutes,
                (await system.Locator("a").EvaluateAllAsync<string[]>("els=>els.map(e=>e.getAttribute('href')).sort()")));
            await Assertions.Expect(page.Locator(".reference-workspace [data-navigation-item=Nav_SystemManagement]")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");

            await page.GotoAsync(baseUrl+"/manage/customers");
            await SearchCustomers(page,marker);
            ILocator row=page.Locator($"tr[data-customer-id='{id}']");
            await Assertions.Expect(row).ToBeVisibleAsync();
            string notes="First line "+marker+"\nSecond line "+culture;
            foreach(string status in new[]{"Blocked","Inactive","Active"})
            {
                await row.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Edit",culture),Exact=true}).ClickAsync();
                ILocator dialog=page.GetByRole(AriaRole.Dialog);
                ILocator selector=dialog.Locator(".customer-editor-panel fieldset select[name=customer-status]");
                await Assertions.Expect(selector).ToBeVisibleAsync();
                await Assertions.Expect(selector).ToBeEnabledAsync();
                Assert.Equal(CustomerStatuses,await selector.Locator("option").EvaluateAllAsync<string[]>("els=>els.map(e=>e.value)"));
                ILocator actions=dialog.Locator("[data-customer-editor-actions=general]");
                await Assertions.Expect(actions.GetByRole(AriaRole.Button)).ToHaveCountAsync(2);
                ILocator save=actions.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Save",culture),Exact=true});
                ILocator cancel=actions.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Cancel",culture),Exact=true});
                var saveBox=await save.BoundingBoxAsync();var cancelBox=await cancel.BoundingBoxAsync();
                Assert.NotNull(saveBox);Assert.NotNull(cancelBox);Assert.InRange(Math.Abs(saveBox.Y-cancelBox.Y),0,2);
                ILocator textarea=dialog.Locator("textarea[name=customer-notes]");
                Assert.True(await textarea.EvaluateAsync<bool>("el=>el.rows>=4&&getComputedStyle(el).borderTopStyle==='solid'&&el.getBoundingClientRect().height>=80"));
                await selector.SelectOptionAsync(status);await textarea.FillAsync(notes);
                await save.ClickAsync();await Assertions.Expect(dialog).ToBeHiddenAsync();
                await Assertions.Expect(row.Locator(".customer-status")).ToHaveTextAsync(Caption("CustomerStrings",status,culture));
                JsonElement saved=await Api(HttpMethod.Get,$"/api/v1/customers/{id}",adminToken,Iran);
                Assert.Equal(status,saved.GetProperty("status").GetString());Assert.Equal(notes,saved.GetProperty("notes").GetString());
            }
            await row.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Edit",culture),Exact=true}).ClickAsync();
            ILocator editDialog=page.GetByRole(AriaRole.Dialog);
            await editDialog.Locator("textarea[name=customer-notes]").FillAsync("Discard this change");
            await editDialog.Locator("select[name=customer-status]").SelectOptionAsync("Blocked");
            await editDialog.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Cancel",culture),Exact=true}).ClickAsync();
            await Assertions.Expect(editDialog).ToBeHiddenAsync();
            await page.ReloadAsync();
            await SearchCustomers(page,marker);
            JsonElement cancelled=await Api(HttpMethod.Get,$"/api/v1/customers/{id}",adminToken,Iran);
            Assert.Equal("Active",cancelled.GetProperty("status").GetString());Assert.Equal(notes,cancelled.GetProperty("notes").GetString());
            await row.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","View",culture),Exact=true}).ClickAsync();
            ILocator view=page.GetByRole(AriaRole.Dialog);
            await Assertions.Expect(view.Locator("select[name=customer-status]")).ToBeDisabledAsync();
            await Assertions.Expect(view.Locator("textarea[name=customer-notes]")).ToBeDisabledAsync();
            await Assertions.Expect(view.Locator("textarea[name=customer-notes]")).ToHaveValueAsync(notes);
            await Assertions.Expect(view.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Save",culture),Exact=true})).ToHaveCountAsync(0);
        }
        finally
        {
            await RetireHistoricalCustomer(Iran,id);
        }
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task AminAdminSwitchesBothDirectionsWithoutReloginOrStaleCompanyData(string culture)
    {
        string marker="ADMIN"+Guid.NewGuid().ToString("N")[..10];
        JsonElement a=await CreateCustomer(Iran,marker+"A"),b=await CreateCustomer(Iraq,marker+"B");
        Guid idA=a.GetProperty("id").GetGuid(),idB=b.GetProperty("id").GetGuid();
        await using IBrowserContext context=await NewContext();IPage page=await context.NewPageAsync();
        int logins=0;page.Request+=(_,request)=>{if(request.Url.EndsWith("/auth/login",StringComparison.Ordinal))Interlocked.Increment(ref logins);};
        try
        {
            JsonElement choices=await Api(HttpMethod.Get,"/api/v1/auth/me/companies",adminToken,null);
            Assert.Contains(choices.EnumerateArray(),x=>x.GetProperty("companyId").GetGuid()==Iran);
            Assert.Contains(choices.EnumerateArray(),x=>x.GetProperty("companyId").GetGuid()==Iraq);
            await Login(page,culture,Username,Password,Iran);
            await page.GotoAsync(baseUrl+"/manage/customers");
            await AssertCustomerScope(page,idA,idB,marker);
            foreach(Guid company in new[]{Iraq,Iran})
            {
                await Switch(page,company);
                await AssertCustomerScope(page,company==Iran?idA:idB,company==Iran?idB:idA,marker);
                await page.GotoAsync(baseUrl+"/master/organization-chart");
                await Assertions.Expect(page.Locator("[data-chart-node=company]")).ToHaveCountAsync(1);
                await Assertions.Expect(page.Locator("[data-chart-node=company]")).ToHaveAttributeAsync("data-chart-node-id",company.ToString());
                await Assertions.Expect(page.Locator(".organizational-chart-page")).ToHaveAttributeAsync("data-company-directory","false");
                JsonElement chart=await Api(HttpMethod.Get,"/api/v1/organization/chart",adminToken,company);
                Assert.Equal(company,Assert.Single(chart.GetProperty("companies").EnumerateArray()).GetProperty("id").GetGuid());
                Assert.All(chart.GetProperty("branches").EnumerateArray(),x=>Assert.Equal(company,x.GetProperty("companyId").GetGuid()));
                Assert.All(chart.GetProperty("units").EnumerateArray(),x=>Assert.Equal(company,x.GetProperty("companyId").GetGuid()));
                await page.GotoAsync(baseUrl+"/manage/customers");
            }
            Assert.Equal(1,logins);
            JsonElement permissionsA=await Api(HttpMethod.Get,"/api/v1/security/permissions",adminToken,Iran),permissionsB=await Api(HttpMethod.Get,"/api/v1/security/permissions",adminToken,Iraq);
            Assert.NotEmpty(permissionsA.EnumerateArray());
            Assert.Equal(permissionsA.EnumerateArray().Select(x=>x.GetProperty("id").GetGuid()).Order(),permissionsB.EnumerateArray().Select(x=>x.GetProperty("id").GetGuid()).Order());
        }
        finally{await DeleteCustomer(Iran,idA);await DeleteCustomer(Iraq,idB);}
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task MultiCompanyHumanCanSwitchButForgedCompanySelectionIsRejected(string culture)
    {
        string marker="HUMAN"+Guid.NewGuid().ToString("N")[..10];
        JsonElement a=await CreateCustomer(Iran,marker+"A"),b=await CreateCustomer(Iraq,marker+"B");
        Guid idA=a.GetProperty("id").GetGuid(),idB=b.GetProperty("id").GetGuid();
        Human human=await MakeHuman();
        await using IBrowserContext context=await NewContext();IPage page=await context.NewPageAsync();
        try
        {
            JsonElement choices=await Api(HttpMethod.Get,"/api/v1/auth/me/companies",human.Token,null);
            Assert.Equal(new[]{Iran,Iraq}.Order(),choices.EnumerateArray().Select(x=>x.GetProperty("companyId").GetGuid()).Order());
            await Login(page,culture,human.Username,human.Password,Iran);
            await page.GotoAsync(baseUrl+"/manage/customers");await AssertCustomerScope(page,idA,idB,marker);
            await Switch(page,Iraq);await AssertCustomerScope(page,idB,idA,marker);
            await Switch(page,Iran);await AssertCustomerScope(page,idA,idB,marker);
            await page.Locator(".company-chip").ClickAsync();
            await Assertions.Expect(page.Locator(".company-list form")).ToHaveCountAsync(2);
            await Assertions.Expect(page.Locator($"input[name=companyId][value='{UnauthorizedCompany}']")).ToHaveCountAsync(0);
            ILocator forged=page.Locator(".company-list form").First;
            await forged.Locator("input[name=companyId]").EvaluateAsync("el=>el.value='"+UnauthorizedCompany+"'");
            await forged.Locator("button[type=submit]").ClickAsync();
            await Assertions.Expect(page.Locator(".company-selection-error")).ToHaveTextAsync(Caption("AppStrings","CompanyNoAccessReason",culture));
            using HttpResponseMessage denied=await api.SendAsync(Request(HttpMethod.Get,"/api/v1/customers",human.Token,UnauthorizedCompany),Ct);
            Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
            Assert.Contains("security.company_access.denied",await denied.Content.ReadAsStringAsync(Ct),StringComparison.Ordinal);
            await SelectCompany(page,Iraq);
            await Assertions.Expect(page.Locator(".company-chip strong")).ToHaveTextAsync(await CompanyName(Iraq));
        }
        finally{await DeleteCustomer(Iran,idA);await DeleteCustomer(Iraq,idB);await DisableHuman(human);}
    }

    [Fact]
    public async Task AminCanCreateAnIraqBranchFromAnExplicitParentWithoutUsingTheDefaultCompany()
    {
        string code="SCOPE"+Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),name="Iraq scoped branch "+code;
        await using IBrowserContext context=await NewContext();IPage page=await context.NewPageAsync();Guid? created=null;
        try
        {
            await Login(page,"en",Username,Password,Iran);
            await page.GotoAsync(baseUrl+"/master/organization-chart");
            await WaitForHandler(page,"[data-chart-action=company-directory]","click");
            await page.Locator("[data-chart-action=company-directory]").ClickAsync();
            await Assertions.Expect(page.Locator(".organizational-chart-page")).ToHaveAttributeAsync("data-company-directory","true");
            await page.Locator($"[data-chart-node=company][data-chart-node-id='{Iraq}'] [data-chart-action=branch-create]").ClickAsync();
            ILocator dialog=page.Locator("[data-chart-dialog-kind=branch]");
            await Assertions.Expect(dialog).ToBeVisibleAsync();
            ILocator inputs=dialog.Locator("input:not([readonly]):not([disabled])");
            await inputs.Nth(0).FillAsync(code);await inputs.Nth(1).FillAsync(name);
            await dialog.GetByRole(AriaRole.Button,new(){Name=Caption("CrudStrings","Save","en"),Exact=true}).ClickAsync();
            await Assertions.Expect(dialog).ToBeHiddenAsync();
            JsonElement branches=await Api(HttpMethod.Get,"/api/v1/organization/branches",adminToken,Iraq);
            JsonElement branch=Assert.Single(branches.EnumerateArray(),x=>x.GetProperty("code").GetString()==code);
            created=branch.GetProperty("id").GetGuid();
            Assert.Equal(Iraq,branch.GetProperty("companyId").GetGuid());
            await Assertions.Expect(page.Locator($"[data-chart-node=branch][data-chart-node-id='{created}']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".company-chip strong")).ToHaveTextAsync(await CompanyName(Iran));
            Assert.DoesNotContain((await Api(HttpMethod.Get,"/api/v1/organization/branches",adminToken,Iran)).EnumerateArray(),x=>x.GetProperty("id").GetGuid()==created);
            await Switch(page,Iraq);
            await Assertions.Expect(page.Locator("[data-chart-node=company]")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("[data-chart-node=company]")).ToHaveAttributeAsync("data-chart-node-id",Iraq.ToString());
            await Switch(page,Iran);
            await Assertions.Expect(page.Locator("[data-chart-node=company]")).ToHaveAttributeAsync("data-chart-node-id",Iran.ToString());
            await Assertions.Expect(page.Locator($"[data-chart-node=branch][data-chart-node-id='{created}']")).ToHaveCountAsync(0);
        }
        finally
        {
            JsonElement remaining=await Api(HttpMethod.Get,"/api/v1/organization/branches",adminToken,Iraq);
            foreach(JsonElement branch in remaining.EnumerateArray().Where(x=>x.GetProperty("code").GetString()==code))
            {
                JsonElement deleted=await Api(HttpMethod.Delete,$"/api/v1/organization/branches/{branch.GetProperty("id").GetGuid()}?version={branch.GetProperty("version").GetInt64()}",adminToken,Iraq);
                Assert.True(deleted.GetBoolean());
            }
            JsonElement verified=await Api(HttpMethod.Get,"/api/v1/organization/branches",adminToken,Iraq);
            Assert.DoesNotContain(verified.EnumerateArray(),x=>x.GetProperty("code").GetString()==code);
        }
    }

    [Fact]
    public async Task CleanedIranianChartRetainsTehranAndTabrizAndMarksProtectedBranchesInactive()
    {
        JsonElement chart=await Api(HttpMethod.Get,"/api/v1/organization/chart",adminToken,Iran);
        Assert.Equal(Iran,Assert.Single(chart.GetProperty("companies").EnumerateArray()).GetProperty("id").GetGuid());
        JsonElement[] branches=chart.GetProperty("branches").EnumerateArray().ToArray();
        Assert.Equal(IranianBranches.Order(),branches.Where(x=>x.GetProperty("isActive").GetBoolean()).Select(x=>x.GetProperty("displayName").GetString()).Order());
        Assert.All(branches.Where(x=>!x.GetProperty("isActive").GetBoolean()),x=>Assert.Contains(x.GetProperty("id").GetGuid(),new[]{Guid.Parse("238a028c-76cf-4ed4-892f-80dff9261e16"),Guid.Parse("35298368-f3c2-4680-b4bb-a39cfd310fe1")}));
        Assert.Equal(2,chart.GetProperty("units").GetArrayLength());
        Assert.Equal(2,chart.GetProperty("assignments").GetArrayLength());
        await using IBrowserContext context=await NewContext();IPage page=await context.NewPageAsync();
        await Login(page,"fa",Username,Password,Iran);await page.GotoAsync(baseUrl+"/master/organization-chart");
        string selector=$"[data-chart-node=company][data-chart-node-id='{Iran}'] > button.org-expander";
        await WaitForHandler(page,selector,"click");
        ILocator expander=page.Locator(selector);
        if(await expander.GetAttributeAsync("aria-expanded")=="false")await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded","true");
        await Assertions.Expect(page.Locator("[data-chart-node=branch][data-chart-active=true]")).ToHaveCountAsync(2);
        foreach(JsonElement branch in branches)
            await Assertions.Expect(page.Locator($"[data-chart-node=branch][data-chart-node-id='{branch.GetProperty("id").GetGuid()}']")).ToBeVisibleAsync();
    }

    private async Task<IBrowserContext> NewContext()=>await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});
    private async Task Login(IPage page,string culture,string username,string password,Guid company)
    {
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2Flogin");
        await page.Locator("#username").FillAsync(username);await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>!new Uri(url).AbsolutePath.StartsWith("/login",StringComparison.Ordinal));
        if(new Uri(page.Url).AbsolutePath=="/company")await SelectCompany(page,company);
        else{await page.GotoAsync(baseUrl+"/company");await SelectCompany(page,company);}
        await Assertions.Expect(page.Locator(".company-chip strong")).ToHaveTextAsync(await CompanyName(company));
    }
    private async Task Switch(IPage page,Guid company)
    {
        string previousPath=new Uri(page.Url).AbsolutePath;
        await page.Locator(".company-chip").ClickAsync();
        await Assertions.Expect(page.Locator(".company-list")).ToBeVisibleAsync();
        await SelectCompany(page,company);
        await Assertions.Expect(page.Locator(".company-chip strong")).ToHaveTextAsync(await CompanyName(company));
        Assert.Equal(previousPath,new Uri(page.Url).AbsolutePath);
    }
    private static async Task SelectCompany(IPage page,Guid company)
    {
        await page.Locator($".company-list form:has(input[name=companyId][value='{company}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company");
        await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
    }
    private static async Task AssertCustomerScope(IPage page,Guid present,Guid absent,string marker)
    {
        await SearchCustomers(page,marker);
        await Assertions.Expect(page.Locator($"tr[data-customer-id='{present}']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator($"tr[data-customer-id='{absent}']")).ToHaveCountAsync(0);
    }
    private static async Task SearchCustomers(IPage page,string marker)
    {
        await WaitForHandler(page,".customer-search input","input");
        await page.Locator(".customer-search input").FillAsync(marker);
        await Assertions.Expect(page.Locator(".customer-search input")).ToHaveValueAsync(marker);
    }
    private static async Task WaitForHandler(IPage page,string selector,string eventName)
    {
        // Wait for the deployed Blazor event registration, not merely prerendered HTML.
        await page.WaitForFunctionAsync("({selector,eventName})=>{const el=document.querySelector(selector);return !!el&&Object.keys(el).some(key=>key.startsWith('_blazorEvents_')&&!!el[key].getHandler?.(eventName));}",new{selector,eventName});
    }
    private async Task<string> CompanyName(Guid company)=>(await Api(HttpMethod.Get,$"/api/v1/organization/companies/{company}",adminToken,Iran)).GetProperty("displayName").GetString()!;
    private async Task<JsonElement> CreateCustomer(Guid company,string code)=>await Api(HttpMethod.Post,"/api/v1/customers",adminToken,company,new{customerCode=code,customerType="Individual",firstName=code,lastName="Scope",primaryMobile="+989121234567"},HttpStatusCode.Created);
    private async Task DeleteCustomer(Guid company,Guid id)
    {
        JsonElement row=await Api(HttpMethod.Get,$"/api/v1/customers/{id}",adminToken,company);
        await Api(HttpMethod.Delete,$"/api/v1/customers/{id}?version={row.GetProperty("version").GetInt64()}",adminToken,company);
        using HttpResponseMessage deleted=await api.SendAsync(Request(HttpMethod.Get,$"/api/v1/customers/{id}",adminToken,company),Ct);
        Assert.Equal(HttpStatusCode.NotFound,deleted.StatusCode);
    }
    private async Task RetireHistoricalCustomer(Guid company,Guid id)
    {
        JsonElement row=await Api(HttpMethod.Get,$"/api/v1/customers/{id}",adminToken,company);
        JsonElement rejected=await Api(HttpMethod.Delete,$"/api/v1/customers/{id}?version={row.GetProperty("version").GetInt64()}",adminToken,company,null,HttpStatusCode.Conflict);
        Assert.Equal("customer.referenced",rejected.GetProperty("code").GetString());
        JsonElement inactive=await Api(HttpMethod.Post,$"/api/v1/customers/{id}/status",adminToken,company,new{status="Inactive",version=row.GetProperty("version").GetInt64()});
        Assert.Equal("Inactive",inactive.GetProperty("status").GetString());
        JsonElement persisted=await Api(HttpMethod.Get,$"/api/v1/customers/{id}",adminToken,company);
        Assert.Equal("Inactive",persisted.GetProperty("status").GetString());
    }
    private async Task<Human> MakeHuman()
    {
        string username="scope."+Guid.NewGuid().ToString("N"),password="ScopeBrowser!"+Guid.NewGuid().ToString("N");
        JsonElement person=await Api(HttpMethod.Post,"/api/v1/organization/persons",adminToken,Iran,new{firstName="Scope",lastName="Verification",phone="+989120001234"},HttpStatusCode.Created);
        JsonElement user=await Api(HttpMethod.Post,"/api/v1/security/users/linked",adminToken,Iran,new{personId=person.GetProperty("id").GetGuid(),username,password,enabled=true});
        Guid userId=user.GetProperty("id").GetGuid();
        await Api(HttpMethod.Post,$"/api/v1/security/users/{userId}/companies",adminToken,Iran,new{companyId=Iraq});
        JsonElement role=await Api(HttpMethod.Post,"/api/v1/security/roles",adminToken,Iran,new{code=username,displayTitle=username});
        Guid roleId=role.GetProperty("id").GetGuid();
        JsonElement permissions=await Api(HttpMethod.Get,"/api/v1/security/permissions",adminToken,Iran);
        foreach(string permission in new[]{"customer.read","organization.unit.manage","organization.branch.manage"})
        {
            Guid permissionId=permissions.EnumerateArray().Single(x=>x.GetProperty("code").GetString()==permission).GetProperty("id").GetGuid();
            await Api(HttpMethod.Post,$"/api/v1/security/roles/{roleId}/permissions/{permissionId}",adminToken,Iran,null,HttpStatusCode.NoContent);
        }
        foreach(Guid company in new[]{Iran,Iraq})await Api(HttpMethod.Post,$"/api/v1/security/users/{userId}/roles",adminToken,company,new{roleId,companyId=company});
        string token=(await Api(HttpMethod.Post,"/api/v1/auth/login",null,null,new{username,password})).GetProperty("accessToken").GetString()!;
        return new(userId,username,password,token);
    }
    private async Task DisableHuman(Human human)
    {
        JsonElement details=await Api(HttpMethod.Get,$"/api/v1/security/users/{human.Id}",adminToken,Iran);
        await Api(HttpMethod.Put,$"/api/v1/security/users/{human.Id}/activation",adminToken,Iran,new{isActive=false,version=details.GetProperty("account").GetProperty("version").GetInt64()});
    }
    private static HttpRequestMessage Request(HttpMethod method,string path,string? token,Guid? company,object? body=null)
    {
        HttpRequestMessage request=new(method,path);if(token is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        if(company is not null)request.Headers.Add("X-Company-ID",company.ToString());if(body is not null)request.Content=JsonContent.Create(body);return request;
    }
    private async Task<JsonElement> Api(HttpMethod method,string path,string? token,Guid? company,object? body=null,HttpStatusCode expected=HttpStatusCode.OK)
    {
        using HttpRequestMessage request=Request(method,path,token,company,body);using HttpResponseMessage response=await api.SendAsync(request,Ct);
        Assert.True(response.StatusCode==expected,$"{method} {path}: expected {expected}, received {response.StatusCode}. {await response.Content.ReadAsStringAsync(Ct)}");
        return expected==HttpStatusCode.NoContent?JsonSerializer.SerializeToElement(new{}):(await response.Content.ReadFromJsonAsync<JsonElement>(Ct));
    }
    private static string Caption(string family,string key,string culture)
    {
        DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        string suffix=culture=="fa"?"":"."+culture;
        XDocument resource=XDocument.Load(Path.Combine(root?.FullName??throw new DirectoryNotFoundException(),"01_CORE","src","BARANG.Backoffice","Localization",family+suffix+".resx"));
        return resource.Root!.Elements("data").Single(x=>x.Attribute("name")!.Value==key).Element("value")!.Value;
    }
    private sealed record Human(Guid Id,string Username,string Password,string Token);
}
