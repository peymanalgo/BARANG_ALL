using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step3ReservationAcceptanceE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly string taskId = Environment.GetEnvironmentVariable("BARANG_STEP3_TASK_ID") ?? "dddddddd-dddd-dddd-dddd-ddddddddddd1";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company")
        {
            await page.Locator(".company-list form button[type=submit]").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    [Fact]
    public async Task InventoryReservationTaskCanBeCompletedThroughAuthenticatedUi()
    {
        await page.GotoAsync($"{baseUrl}/cartable/task/{taskId}?returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("#reservation-warehouse")).ToBeVisibleAsync();
        await page.Locator("#reservation-warehouse").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        bool inventoryIssue = await page.Locator("h2").Filter(new() { HasText = "صدور کالا از انبار" }).CountAsync() > 0
            || await page.Locator("h2").Filter(new() { HasText = "Warehouse issue" }).CountAsync() > 0;
        await page.Locator("section.cartable-actions-panel button").First.ClickAsync();
        await page.Locator("section.cartable-operational-popup .cartable-v5-outcomes button.btn-primary").First.ClickAsync();
        if (inventoryIssue)
        {
            await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/cartable$"));
            return;
        }
        await page.Locator(".dialog-card .form-actions button.btn-primary").Last.ClickAsync(new() { Force = true });
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/cartable$"));
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
