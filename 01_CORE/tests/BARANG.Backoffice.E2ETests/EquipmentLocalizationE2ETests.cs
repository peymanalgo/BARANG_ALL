using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

[CollectionDefinition("EquipmentLocalization", DisableParallelization = true)]
public sealed class EquipmentLocalizationCollection;

[Collection("EquipmentLocalization")]
public sealed class EquipmentLocalizationE2ETests : IAsyncLifetime
{
    private static readonly Guid Company = Guid.Parse("42f0e459-6095-45bb-aa13-f38126d91d9c");
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){
        string username=Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin", password=Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026";
        playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.Locator("#username").FillAsync(username);await page.Locator("#password").FillAsync(password);await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);if(page.Url.Contains("/login",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Backoffice login failed: "+page.Url+" | "+await page.Locator("body").InnerTextAsync());
        if(new Uri(page.Url).AbsolutePath=="/company") { var companyButton=page.Locator($".company-list form:has(input[name=companyId][value='{Company}']) button[type=submit]"); await Assertions.Expect(companyButton).ToBeVisibleAsync(); await companyButton.ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); }
    }
    public async ValueTask DisposeAsync(){if(context is not null)await context.DisposeAsync();if(browser is not null)await browser.DisposeAsync();playwright?.Dispose();}
    [Theory][InlineData("fa","rtl",new[]{"شماره تجهیز","دسته‌بندی تجهیز","برند و مدل","شماره سریال","وضعیت تجهیز"},new[]{"اطلاعات عمومی","مشخصات فنی","انتصاب و مکان","خرید و ضمانت","تاریخچه خرابی","تاریخچه تعمیر","خدمات و نگهداری","بازرسی و کالیبراسیون","اسناد و پیوست‌ها","چرخه عمر"})][InlineData("ar","rtl",new[]{"رقم المعدة","فئة المعدة","العلامة والطراز","الرقم التسلسلي","حالة المعدة"},new[]{"المعلومات العامة","المواصفات الفنية","التعيين والموقع","الاقتناء والضمان","سجل الأعطال","سجل الإصلاح","الخدمة والصيانة","الفحص والمعايرة","المستندات والمرفقات","دورة الحياة"})][InlineData("en","ltr",new[]{"Equipment number","Equipment category","Brand and model","Serial number","Equipment status"},new[]{"General information","Technical specifications","Assignment and location","Acquisition and warranty","Failure history","Repair history","Service and maintenance","Inspection and calibration","Documents and attachments","Lifecycle"})]
    public async Task Equipment_grid_and_vertical_editor_are_fully_localized(string culture,string direction,string[] headers,string[] tabs){await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2Fmaster%2Fequipment",new(){WaitUntil=WaitUntilState.NetworkIdle});Assert.Equal(culture,await page.Locator("html").GetAttributeAsync("lang"));Assert.Equal(direction,await page.Locator("html").GetAttributeAsync("dir"));await Assertions.Expect(page.Locator("[data-equipment-directory]")).ToBeVisibleAsync();foreach(string header in headers)await Assertions.Expect(page.Locator("[data-equipment-directory] thead")).ToContainTextAsync(header);await page.Locator("header.crud-header button").First.ClickAsync();ILocator modal=page.Locator("[data-equipment-editor]");await Assertions.Expect(modal).ToBeVisibleAsync();foreach(string tab in tabs)await Assertions.Expect(modal.Locator("[role=tab]").Filter(new(){HasText=tab})).ToBeVisibleAsync();Assert.Equal(0,await page.EvaluateAsync<int>("()=>document.body.scrollWidth-document.body.clientWidth>1?1:0"));}
}
