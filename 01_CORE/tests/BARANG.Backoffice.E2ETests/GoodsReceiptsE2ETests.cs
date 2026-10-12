using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class GoodsReceiptsE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")??@"C:\Program Files\Google\Chrome\Application\chrome.exe"});context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();await page.GotoAsync(baseUrl+"/login",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin");await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026");await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);if(new Uri(page.Url).AbsolutePath=="/company"){await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();}}
    [Fact]
    public async Task Goods_receipt_draft_post_and_overreceipt_are_real_workflow(){
        await page.GotoAsync(baseUrl+"/culture?culture=fa&returnUrl=%2Fworkspace%2Finventory",new(){WaitUntil=WaitUntilState.NetworkIdle});
        await page.GotoAsync(baseUrl+"/workspace/inventory?section=receipts",new(){WaitUntil=WaitUntilState.NetworkIdle});
        var grid=page.Locator("[data-goods-receipt-grid]"); await Assertions.Expect(grid).ToBeVisibleAsync(); await Assertions.Expect(grid.Locator(".crud-search input")).ToHaveCountAsync(1); await Assertions.Expect(grid.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await grid.GetByRole(AriaRole.Button,new(){Name="افزودن رسید کالا",Exact=true}).ClickAsync(); var dialog=page.GetByRole(AriaRole.Dialog); await Assertions.Expect(dialog.Locator(".locale-date-input")).ToHaveCountAsync(1); await Assertions.Expect(dialog.Locator(".locale-date-input")).ToHaveAttributeAsync("data-calendar-culture","fa");
        var po=dialog.Locator("input[name=goods-receipt-po]"); await po.FillAsync("PO-"); var option=po.Locator("xpath=..").Locator("[role=option]").First; await Assertions.Expect(option).ToBeVisibleAsync(); await option.ClickAsync(); await Assertions.Expect(dialog.Locator("tbody tr")).Not.ToHaveCountAsync(0);
        var qty=dialog.Locator("tbody tr input[type=number]").First; await qty.FillAsync("1"); await dialog.GetByRole(AriaRole.Button,new(){Name="ذخیره پیش‌نویس",Exact=true}).ClickAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("پیش‌نویس رسید ذخیره شد"); await Assertions.Expect(dialog).ToBeVisibleAsync(); await dialog.GetByRole(AriaRole.Button,new(){Name="ثبت رسید",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("رسید ثبت شد"); await Assertions.Expect(dialog).ToHaveCountAsync(0); await Assertions.Expect(grid).ToContainTextAsync("GR-");
        await grid.GetByRole(AriaRole.Button,new(){Name="افزودن رسید کالا",Exact=true}).ClickAsync(); dialog=page.GetByRole(AriaRole.Dialog); po=dialog.Locator("input[name=goods-receipt-po]"); await po.FillAsync("PO-"); await po.Locator("xpath=..").Locator("[role=option]").First.ClickAsync(); await Assertions.Expect(dialog.Locator("tbody tr")).Not.ToHaveCountAsync(0); await dialog.Locator("tbody tr input[type=number]").First.FillAsync("2"); await dialog.GetByRole(AriaRole.Button,new(){Name="ذخیره پیش‌نویس",Exact=true}).ClickAsync(); await Assertions.Expect(dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("مقدار رسید از مقدار باقیمانده بیشتر است");
    }
    public async ValueTask DisposeAsync(){await context.DisposeAsync();await browser.DisposeAsync();playwright.Dispose();}
}
