using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class PurchaseRequestsE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await BrowserCompanyContext.SelectIfRequiredAsync(page);
    }

    [Fact]
    public async Task PurchaseRequestGridAndSameSessionMultilineDraftWork()
    {
        await page.GotoAsync(baseUrl + "/culture?culture=en&returnUrl=%2Fworkspace%2Finventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/workspace/inventory?section=requests", new() { WaitUntil = WaitUntilState.NetworkIdle });
        var grid = page.Locator("[data-purchase-request-grid]");
        await Assertions.Expect(grid).ToBeVisibleAsync();
        await Assertions.Expect(grid.Locator(".crud-search input")).ToHaveCountAsync(1);
        await Assertions.Expect(grid.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await Assertions.Expect(grid).Not.ToContainTextAsync("Show inactive");
        await Assertions.Expect(grid).Not.ToContainTextAsync("Refresh");
        await grid.GetByRole(AriaRole.Button, new() { Name = "Add request", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add purchase request" });
        await Choose(dialog.Locator("label").Filter(new() { HasText = "Destination warehouse" }).Locator("input[role=combobox]"), "EW0_1788283843295");
        await dialog.Locator("label").Filter(new() { HasText = "Reason/Purpose" }).Locator("input").FillAsync("Replenishment request");
        var product = dialog.Locator("label").Filter(new() { HasText = "Product" }).Locator("input[role=combobox]");
        await Choose(product, "I14P1788268154");
        await Choose(dialog.Locator("label").Filter(new() { HasText = "SKU" }).Locator("input[role=combobox]"), "I14SKU1788268154");
        await dialog.Locator("input[type=number]").FillAsync("2");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Add line", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog.Locator("tbody tr")).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save draft", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(grid).ToContainTextAsync("PR-");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-purchase-request-grid]")).ToContainTextAsync("PR-");
    }

    private async Task Choose(ILocator input, string search)
    {
        await input.FillAsync(search);
        var options = page.Locator(".searchable-select-list[role=listbox] [role=option]");
        await Assertions.Expect(options).ToHaveCountAsync(1);
        await options.First.ClickAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await browser.DisposeAsync();
        playwright.Dispose();
    }
}
