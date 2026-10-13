using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class ReservationsShortagesE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

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
        if (new Uri(page.Url).AbsolutePath == "/company") await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();
    }

    [Fact]
    public async Task ReservationsAndShortagesAreIndependentReadWorkflowsWithCanonicalToolbars()
    {
        await GoTo("en");
        ILocator section = page.Locator("[data-reservation-shortage-grid]");
        await Assertions.Expect(section).ToBeVisibleAsync();
        await Assertions.Expect(section).ToHaveAttributeAsync("data-section", "reservations");
        await Assertions.Expect(section.Locator("h2")).ToHaveTextAsync("Inventory Reservations");
        await Assertions.Expect(section.Locator(".crud-toolbar input")).ToHaveCountAsync(1); // Search; status is a compact business select.
        await Assertions.Expect(section.Locator(".crud-toolbar button")).ToHaveCountAsync(1); // Reset only.
        await Assertions.Expect(section).Not.ToContainTextAsync("Show inactive");
        await Assertions.Expect(section).Not.ToContainTextAsync("Show closed");
        await Assertions.Expect(section).Not.ToContainTextAsync("Refresh");
        await page.GetByRole(AriaRole.Link, new() { Name = "Stock Shortages", Exact = true }).ClickAsync();
        await Assertions.Expect(section).ToHaveAttributeAsync("data-section", "shortages");
        await Assertions.Expect(section.Locator("h2")).ToHaveTextAsync("Stock Shortages");
        await Assertions.Expect(section.GetByRole(AriaRole.Columnheader, new() { Name = "Shortage", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(section.Locator(".crud-toolbar input")).ToHaveCountAsync(2); // Search plus warehouse selector.
        await Assertions.Expect(section.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await section.Locator(".crud-search input").FillAsync("not-a-real-stock-row");
        await Assertions.Expect(section).ToContainTextAsync("No shortages found");
        await section.Locator(".crud-toolbar button").ClickAsync();
        await Assertions.Expect(section.Locator(".crud-search input")).ToHaveValueAsync("");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
    }

    [Fact]
    public async Task ReservationsAndShortagesLocalizeInFaArEn()
    {
        foreach ((string culture, string title, string reservations, string shortages, string direction) in new[]
        {
            ("fa", "رزرو موجودی", "رزرو موجودی", "کمبود موجودی", "rtl"),
            ("ar", "حجوزات المخزون", "حجوزات المخزون", "نواقص المخزون", "rtl"),
            ("en", "Inventory Reservations", "Inventory Reservations", "Stock Shortages", "ltr")
        })
        {
            await GoTo(culture);
            ILocator section = page.Locator("[data-reservation-shortage-grid]");
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
            await Assertions.Expect(section.Locator("h2")).ToHaveTextAsync(title);
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = reservations, Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = shortages, Exact = true })).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = reservations, Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-reservation-shortage-grid]")).ToHaveAttributeAsync("data-section", "reservations");
            await page.GetByRole(AriaRole.Link, new() { Name = shortages, Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-reservation-shortage-grid]")).ToHaveAttributeAsync("data-section", "shortages");
        }
    }

    private async Task GoTo(string culture)
    {
        await page.GotoAsync(baseUrl + $"/culture?culture={culture}&returnUrl=%2Fworkspace%2Finventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/workspace/inventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Link, new() { Name = culture == "fa" ? "رزرو موجودی" : culture == "ar" ? "حجوزات المخزون" : "Inventory Reservations", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-reservation-shortage-grid]")).ToBeVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
