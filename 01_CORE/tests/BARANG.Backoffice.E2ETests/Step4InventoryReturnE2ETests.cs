using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step4InventoryReturnE2ETests : IAsyncLifetime
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
        if (new Uri(page.Url).AbsolutePath == "/company") await page.Locator(".company-list form button[type=submit]").First.ClickAsync();
    }

    [Fact]
    public async Task UsableReturnIsPostedFromTheRealWarehouseReturnFormAndReloads()
    {
        await page.GotoAsync(baseUrl + "/workspace/inventory?section=returns", new() { WaitUntil = WaitUntilState.NetworkIdle });
        ILocator section = page.Locator(".inventory-return");
        await Assertions.Expect(section).ToBeVisibleAsync();
        ILocator issue = section.Locator("select").First;
        Assert.True(await issue.Locator("option").CountAsync() > 1, "No returnable issue was exposed by the authenticated form.");
        await issue.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await section.Locator("input[type=number]").FillAsync("1");
        await section.Locator("input[type=number]").PressAsync("Tab");
        ILocator post = section.Locator("button.btn-primary");
        await Assertions.Expect(post).ToBeEnabledAsync();
        await post.ClickAsync();
        await Assertions.Expect(issue).ToHaveValueAsync("");
        await Assertions.Expect(section.Locator("button.btn-primary")).ToBeDisabledAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
