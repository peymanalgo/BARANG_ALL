using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class NavigationCleanupE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company") { await page.Locator(".company-list form button[type=submit]").First.ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); }
    }

    [Theory]
    [InlineData("fa", "rtl")]
    [InlineData("ar", "rtl")]
    [InlineData("en", "ltr")]
    public async Task Direct_menu_pages_are_blank_and_directional(string culture, string direction)
    {
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2F", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
        foreach (string route in new[] { "/", "/workspace/notifications", "/workspace/management" })
        {
            await page.GotoAsync($"{baseUrl}{route}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions.Expect(page.Locator("main.content")).ToHaveTextAsync("");
            Assert.Equal(0, await page.Locator("main.content .panel, main.content table, main.content .kpi-card, main.content .management-grid, main.content .toolbar").CountAsync());
        }
    }

    [Fact]
    public async Task Notifications_and_management_dashboard_are_sibling_direct_items()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Forders", new() { WaitUntil = WaitUntilState.NetworkIdle });
        foreach (string route in new[] { "/workspace/notifications", "/workspace/management" })
        {
            ILocator link = page.Locator($"nav.sidebar-navigation > a.nav-item[href='{route}']");
            await Assertions.Expect(link).ToHaveCountAsync(1);
            await Assertions.Expect(link.Locator(".nav-chevron")).ToHaveCountAsync(0);
        }
        Assert.Equal(0, await page.Locator("nav.sidebar-navigation a[href='/workspace/reports'], nav.sidebar-navigation [data-navigation-group='Nav_Reporting']").CountAsync());
        await Assertions.Expect(page.Locator("[data-navigation-group='Nav_SystemManagement']")).ToHaveCountAsync(1);
        string[] hrefs = await page.Locator("nav.sidebar-navigation > a.nav-item").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('href'))");
        Assert.Equal(Array.IndexOf(hrefs, "/workspace/orders") + 1, Array.IndexOf(hrefs, "/cartable"));
    }

    public async ValueTask DisposeAsync() { await context.CloseAsync(); await browser.CloseAsync(); playwright.Dispose(); }
}
