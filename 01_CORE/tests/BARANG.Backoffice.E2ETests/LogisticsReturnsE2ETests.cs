using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class LogisticsReturnsE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? throw new InvalidOperationException("BARANG_E2E_BASE_URL is required.");

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        string browserPath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = browserPath });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await BrowserCompanyContext.SelectIfRequiredAsync(page);
    }

    [Fact]
    public async Task TraceableReturnFormIsVisibleWithoutMutatingBusinessRecords()
    {
        await page.GotoAsync($"{baseUrl}/workspace/logistics/returns", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".page-heading h1")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("button").Filter(new() { HasText = "Save" })).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("input")).ToHaveCountAsync(7);
        await Assertions.Expect(page.Locator(".inline-error")).ToHaveCountAsync(0);
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await browser.DisposeAsync();
        playwright.Dispose();
    }
}
