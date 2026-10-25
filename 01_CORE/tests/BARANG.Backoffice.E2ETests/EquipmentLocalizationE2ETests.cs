using Microsoft.Playwright;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BARANG.Backoffice.E2ETests;

[CollectionDefinition("EquipmentLocalization", DisableParallelization = true)]
public sealed class EquipmentLocalizationSet;

[Collection("EquipmentLocalization")]
public sealed class EquipmentLocalizationE2ETests : IAsyncLifetime
{
    private readonly Guid Company = Guid.Parse(Environment.GetEnvironmentVariable("BARANG_E2E_COMPANY_ID") ?? "42f0e459-6095-45bb-aa13-f38126d91d9c");
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly HttpClient api = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL") ?? "http://127.0.0.1:5869") };
    private string adminToken = "";
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){
        string username=Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin", password=Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026";
        playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.Locator("#username").FillAsync(username);await page.Locator("#password").FillAsync(password);await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);if(page.Url.Contains("/login",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Backoffice login failed: "+page.Url+" | "+await page.Locator("body").InnerTextAsync());
        if(new Uri(page.Url).AbsolutePath=="/company") { var companyButton=page.Locator($".company-list form:has(input[name=companyId][value='{Company}']) button[type=submit]"); await Assertions.Expect(companyButton).ToBeVisibleAsync(); await companyButton.ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); }
        using HttpResponseMessage login = await api.PostAsJsonAsync("api/v1/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        using JsonDocument loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        adminToken = loginJson.RootElement.GetProperty("accessToken").GetString()!;
    }
    public async ValueTask DisposeAsync(){if(context is not null)await context.DisposeAsync();if(browser is not null)await browser.DisposeAsync();playwright?.Dispose();api.Dispose();}
    [Theory][InlineData("fa","rtl",new[]{"شماره تجهیز","دسته‌بندی تجهیز","برند و مدل","شماره سریال","وضعیت تجهیز"},new[]{"اطلاعات عمومی","مشخصات فنی","انتصاب و مکان","خرید و ضمانت","تاریخچه خرابی","تاریخچه تعمیر","خدمات و نگهداری","بازرسی و کالیبراسیون","اسناد و پیوست‌ها","چرخه عمر"})][InlineData("ar","rtl",new[]{"رقم المعدة","فئة المعدة","العلامة والطراز","الرقم التسلسلي","حالة المعدة"},new[]{"المعلومات العامة","المواصفات الفنية","التعيين والموقع","الاقتناء والضمان","سجل الأعطال","سجل الإصلاح","الخدمة والصيانة","الفحص والمعايرة","المستندات والمرفقات","دورة الحياة"})][InlineData("en","ltr",new[]{"Equipment number","Equipment category","Brand and model","Serial number","Equipment status"},new[]{"General information","Technical specifications","Assignment and location","Acquisition and warranty","Failure history","Repair history","Service and maintenance","Inspection and calibration","Documents and attachments","Lifecycle"})]
    public async Task EquipmentGridAndVerticalEditorAreFullyLocalized(string culture,string direction,string[] headers,string[] tabs){await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2Fmaster%2Fequipment",new(){WaitUntil=WaitUntilState.NetworkIdle});Assert.Equal(culture,await page.Locator("html").GetAttributeAsync("lang"));Assert.Equal(direction,await page.Locator("html").GetAttributeAsync("dir"));await Assertions.Expect(page.Locator("[data-equipment-directory]")).ToBeVisibleAsync();foreach(string header in headers)await Assertions.Expect(page.Locator("[data-equipment-directory] thead")).ToContainTextAsync(header);await page.Locator("header.crud-header button").First.ClickAsync();ILocator modal=page.Locator("[data-equipment-editor]");await Assertions.Expect(modal).ToBeVisibleAsync();foreach(string tab in tabs)await Assertions.Expect(modal.Locator("[role=tab]").Filter(new(){HasText=tab})).ToBeVisibleAsync();Assert.Equal(0,await page.EvaluateAsync<int>("()=>document.body.scrollWidth-document.body.clientWidth>1?1:0"));}
    [Fact]
    public async Task EquipmentCrudAndStatusUseCanonicalContract()
    {
        await EnsureLookupValue("equipment.type", "PRESS");
        await EnsureLookupValue("equipment.status", "AVAILABLE");
        await EnsureLookupValue("equipment.status", "IN_SERVICE");
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fmaster%2Fequipment", new() { WaitUntil = WaitUntilState.NetworkIdle });
        string code = "E2E-EQ-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await page.Locator("header.crud-header button").First.ClickAsync();
        ILocator modal = page.Locator("[data-equipment-editor]");
        await modal.Locator("input[name=equipmentCode]").FillAsync(code);
        await modal.Locator("input[name=name]").FillAsync("E2E Press");
        await SelectReference(modal, "equipmentTypeCode", "PRESS");
        await modal.Locator("[data-equipment-tab='general']").ClickAsync();
        await modal.Locator("input[name=assetNumber]").FillAsync("ASSET-" + code);
        await modal.Locator("[data-equipment-tab='lifecycle']").ClickAsync();
        await SelectReference(modal, "operationalStatusCode", "AVAILABLE");
        await SelectReference(modal, "lifecycleStatusCode", "IN_SERVICE");
        await modal.Locator("[data-equipment-tab='technical']").ClickAsync();
        await modal.Locator("input[name=model]").FillAsync("Model-E2E");
        await modal.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        ILocator grid = page.Locator("[data-equipment-directory]");
        await grid.Locator(".search-box input").FillAsync(code);
        ILocator row = grid.Locator("tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row).ToContainTextAsync("E2E Press", new() { Timeout = 30000 });
        await row.Locator(".barang-action-menu-trigger").ClickAsync();
        await page.Locator(".barang-action-menu-popup button").Filter(new() { HasText = "Edit" }).ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Assertions.Expect(modal.Locator("input[name=assetNumber]")).ToHaveValueAsync("ASSET-" + code);
        await modal.Locator("input[name=name]").FillAsync("E2E Press Updated");
        await modal.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await Assertions.Expect(row).ToContainTextAsync("E2E Press Updated");
        await row.Locator(".barang-action-menu-trigger").ClickAsync();
        await page.Locator(".barang-action-menu-popup button").Filter(new() { HasText = "Deactivate" }).ClickAsync();
        await grid.Locator("select[aria-label]").SelectOptionAsync("inactive");
        await Assertions.Expect(row).ToContainTextAsync("E2E Press Updated");
    }

    private async Task EnsureLookupValue(string typeCode, string code)
    {
        using HttpRequestMessage get = new(HttpMethod.Get, $"api/v1/lookups/types/{typeCode}/values?activeOnly=false");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        get.Headers.Add("X-Company-ID", Company.ToString());
        using HttpResponseMessage response = await api.SendAsync(get);
        response.EnsureSuccessStatusCode();
        using JsonDocument values = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (values.RootElement.EnumerateArray().Any(x => x.TryGetProperty("code", out JsonElement c) && string.Equals(c.GetString(), code, StringComparison.OrdinalIgnoreCase))) return;
        using HttpRequestMessage post = new(HttpMethod.Post, $"api/v1/lookups/types/{typeCode}/values")
        {
            Content = JsonContent.Create(new { code, displayOrder = 0, translations = new Dictionary<string, string> { ["fa"] = code, ["ar"] = code, ["en"] = code } })
        };
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        post.Headers.Add("X-Company-ID", Company.ToString());
        using HttpResponseMessage created = await api.SendAsync(post);
        created.EnsureSuccessStatusCode();
    }

    private async Task SelectReference(ILocator modal, string name, string query)
    {
        ILocator input = modal.Locator($"input[name='{name}']");
        await input.FillAsync(query);
        ILocator option = page.Locator(".searchable-select-list button[role=option]").Filter(new() { HasText = query }).First;
        await Assertions.Expect(option).ToBeVisibleAsync();
        await option.ClickAsync();
    }

}
