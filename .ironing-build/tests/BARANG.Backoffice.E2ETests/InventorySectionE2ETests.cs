using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class InventorySectionE2ETests : IAsyncLifetime
{
    private IPlaywright pw = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

    public async ValueTask InitializeAsync()
    {
        pw = await Playwright.CreateAsync();
        browser = await pw.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company")
        {
            await BrowserCompanyContext.SelectIfRequiredAsync(page);
        }
    }

    [Fact]
    public async Task InventoryBalanceGridIsReadOnlyPagedSearchableAndKardexConnected()
    {
        await GoToInventory("en");
        ILocator section = page.Locator("[data-inventory-grid]");
        await Assertions.Expect(section).ToBeVisibleAsync();
        await Assertions.Expect(section.Locator(".crud-toolbar input")).ToHaveCountAsync(2); // canonical Search plus the business Warehouse selector
        await Assertions.Expect(section.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await Assertions.Expect(section.Locator(".section-header button")).ToHaveCountAsync(0);
        await Assertions.Expect(section).Not.ToContainTextAsync("Show inactive");
        await Assertions.Expect(section).Not.ToContainTextAsync("Show closed");
        await Assertions.Expect(section).Not.ToContainTextAsync("Refresh");

        ILocator rows = section.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(4);
        await Assertions.Expect(rows.First).ToContainTextAsync("I14SKU1788268154");
        await Assertions.Expect(section.Locator(".barang-sort-header")).ToHaveCountAsync(4);
        await section.Locator(".barang-sort-header").Nth(2).ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(4);

        await section.Locator(".crud-search input").FillAsync("I14SKU1788268154");
        await Assertions.Expect(rows).ToHaveCountAsync(4);
        await section.Locator(".crud-toolbar button").ClickAsync();
        await Assertions.Expect(section.Locator(".crud-search input")).ToHaveValueAsync("");

        await rows.First.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToContainTextAsync("On hand");
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog).Locator("input,select,textarea")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Button, new() { Name = "View Kardex", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToContainTextAsync("Recorded movements");
        var kardexRows = await page.GetByRole(AriaRole.Dialog).Locator("tbody tr").CountAsync();
        Assert.True(kardexRows > 0, "The Inventory row must expose its authoritative filtered Kardex movements.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).Last.ClickAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
    }

    [Fact]
    public async Task InventoryBalanceGridLocalizesAndSwitchesDirection()
    {
        foreach ((string culture, string title, string direction) in new[]
        {
            ("fa", "موجودی", "rtl"),
            ("ar", "المخزون", "rtl"),
            ("en", "Inventory", "ltr")
        })
        {
            await GoToInventory(culture);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
            await Assertions.Expect(page.Locator("[data-inventory-grid] h2")).ToHaveTextAsync(title);
            await Assertions.Expect(page.Locator("[data-inventory-grid] .crud-toolbar")).Not.ToContainTextAsync(culture == "fa" ? "Refresh" : culture == "ar" ? "Refresh" : "تازهسازی");
        }
    }

    private async Task GoToInventory(string culture)
    {
        await page.GotoAsync(baseUrl + $"/culture?culture={culture}&returnUrl=%2Fworkspace%2Finventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/workspace/inventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Link, new() { Name = culture == "fa" ? "موجودی" : culture == "ar" ? "المخزون" : "Inventory / Stock", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-inventory-grid]")).ToBeVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        pw.Dispose();
    }
}
