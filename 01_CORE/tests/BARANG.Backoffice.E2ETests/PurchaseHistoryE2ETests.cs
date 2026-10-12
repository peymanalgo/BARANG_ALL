using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class PurchaseHistoryE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")??@"C:\Program Files\Google\Chrome\Application\chrome.exe"});context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();await page.GotoAsync(baseUrl+"/login",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin");await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026");await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);if(new Uri(page.Url).AbsolutePath=="/company"){await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();}}
    [Fact]
    public async Task Purchase_history_is_read_only_paged_and_localized(){
        await page.GotoAsync(baseUrl+"/culture?culture=fa&returnUrl=%2Fworkspace%2Finventory",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/inventory?section=history",new(){WaitUntil=WaitUntilState.NetworkIdle});
        var grid=page.Locator("[data-purchase-history-grid]"); await Assertions.Expect(grid).ToBeVisibleAsync(); await Assertions.Expect(grid.Locator(".crud-search input")).ToHaveCountAsync(1); await Assertions.Expect(grid.Locator(".crud-toolbar > button")).ToHaveCountAsync(1); await Assertions.Expect(grid.GetByRole(AriaRole.Button,new(){Name="افزودن",Exact=true})).ToHaveCountAsync(0); await Assertions.Expect(grid.Locator(".locale-date-input")).ToHaveCountAsync(2); await Assertions.Expect(grid.Locator(".locale-date-input").First).ToHaveAttributeAsync("data-calendar-culture","fa"); var view=grid.GetByRole(AriaRole.Button,new(){Name="مشاهده",Exact=true}).First; await Assertions.Expect(view).ToBeVisibleAsync(); await view.ClickAsync(); var detail=page.GetByRole(AriaRole.Dialog); await Assertions.Expect(detail).ToContainTextAsync("رسیدهای مرتبط"); await Assertions.Expect(detail.Locator("input,select,textarea")).ToHaveCountAsync(0); await detail.GetByRole(AriaRole.Button,new(){Name="بستن",Exact=true}).ClickAsync();
        await grid.Locator(".crud-search input").FillAsync("PO-"); await page.WaitForTimeoutAsync(350); await Assertions.Expect(grid).Not.ToContainTextAsync("خطایی غیرمنتظره رخ داد"); await grid.GetByRole(AriaRole.Button,new(){Name="بازنشانی",Exact=true}).ClickAsync();
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Finventory",new(){WaitUntil=WaitUntilState.NetworkIdle}); await page.GotoAsync(baseUrl+"/workspace/inventory?section=history",new(){WaitUntil=WaitUntilState.NetworkIdle}); await Assertions.Expect(page.Locator("[data-purchase-history-grid]")).ToContainTextAsync("Purchase History");
    }
    public async ValueTask DisposeAsync(){await context.DisposeAsync();await browser.DisposeAsync();playwright.Dispose();}
}
