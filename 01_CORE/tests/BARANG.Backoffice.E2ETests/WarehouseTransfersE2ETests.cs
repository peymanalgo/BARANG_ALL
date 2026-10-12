using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class WarehouseTransfersE2ETests : IAsyncLifetime
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
        if (new Uri(page.Url).AbsolutePath == "/company") await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();
    }

    [Fact]
    public async Task Transfer_workspace_is_dedicated_and_uses_canonical_grid()
    {
        await GoTo("en");
        var section = page.Locator("[data-transfer-grid]");
        await Assertions.Expect(section).ToBeVisibleAsync();
        await Assertions.Expect(section.Locator("h2")).ToHaveTextAsync("Warehouse Transfers");
        await Assertions.Expect(section.Locator(".crud-search input")).ToHaveCountAsync(1);
        await Assertions.Expect(section.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await Assertions.Expect(section).Not.ToContainTextAsync("Show inactive");
        await Assertions.Expect(section).Not.ToContainTextAsync("Show closed");
        await Assertions.Expect(section).Not.ToContainTextAsync("Refresh");
        await Assertions.Expect(section.Locator(".section-header").GetByRole(AriaRole.Button, new() { Name = "Add transfer", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("text=خطایی غیرمنتظره رخ داد")).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
    }

    [Fact]
    public async Task Transfer_workspace_localizes_and_keeps_internal_navigation()
    {
        foreach ((string culture, string title, string direction) in new[] { ("fa", "انتقال بین انبارها", "rtl"), ("ar", "التحويل بين المستودعات", "rtl"), ("en", "Warehouse Transfers", "ltr") })
        {
            await GoTo(culture);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
            await Assertions.Expect(page.Locator("[data-transfer-grid] h2")).ToHaveTextAsync(title);
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = culture == "fa" ? "انتقال بین انبارها" : culture == "ar" ? "التحويلات" : "Transfers", Exact = true })).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Transfer_can_start_and_complete_through_the_real_editor()
    {
        await GoTo("en");
        var section = page.Locator("[data-transfer-grid]");
        await section.GetByRole(AriaRole.Button, new() { Name = "Add transfer", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog);
        var selectors = dialog.Locator("input[role=combobox]");
        await selectors.Nth(0).ClickAsync();
        await dialog.GetByRole(AriaRole.Option, new() { Name = "EW0_1788283843295 - Updated Warehouse 0", Exact = true }).ClickAsync();
        await selectors.Nth(1).ClickAsync();
        await dialog.GetByRole(AriaRole.Option, new() { Name = "EW1_1788283843295 - Updated Warehouse 1", Exact = true }).ClickAsync();
        await selectors.Nth(2).ClickAsync();
        await dialog.GetByRole(AriaRole.Option, new() { Name = "I14P1788268154 - Phase14 Product", Exact = true }).ClickAsync();
        await selectors.Nth(3).ClickAsync();
        await dialog.GetByRole(AriaRole.Option, new() { Name = "I14SKU1788268154", Exact = true }).ClickAsync();
        await dialog.Locator("input[type=number]").FillAsync("1");
        page.Dialog += async (_, browserDialog) => await browserDialog.AcceptAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Start transfer", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        var row = section.Locator("tbody tr").Filter(new() { HasText = "EW0_1788283843295" }).Last;
        await Assertions.Expect(row).ToContainTextAsync("EW0_1788283843295");
        await Assertions.Expect(row).ToContainTextAsync("In transit");
        await row.Locator("button[aria-label='Complete']").ClickAsync();
        var confirm = page.GetByRole(AriaRole.Dialog);
        await confirm.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
        await Assertions.Expect(section).ToContainTextAsync("Completed");
    }

    private async Task GoTo(string culture)
    {
        await page.GotoAsync(baseUrl + $"/culture?culture={culture}&returnUrl=%2Fworkspace%2Finventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/workspace/inventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Link, new() { Name = culture == "fa" ? "انتقال بین انبارها" : culture == "ar" ? "التحويلات" : "Transfers", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-transfer-grid]")).ToBeVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        pw.Dispose();
    }
}
