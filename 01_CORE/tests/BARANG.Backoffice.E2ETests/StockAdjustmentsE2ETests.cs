using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class StockAdjustmentsE2ETests : IAsyncLifetime
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
    public async Task Adjustment_grid_is_canonical_and_localized()
    {
        await GoTo("en");
        var section = page.Locator("[data-adjustment-grid]");
        Assert.True(int.Parse(await section.GetAttributeAsync("data-warehouse-options") ?? "0") > 1, "warehouse option count=" + await section.GetAttributeAsync("data-warehouse-options"));
        await Assertions.Expect(section).ToBeVisibleAsync();
        await Assertions.Expect(section.Locator(".crud-search input")).ToHaveCountAsync(1);
        await Assertions.Expect(section.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await Assertions.Expect(section).Not.ToContainTextAsync("Show inactive");
        await Assertions.Expect(section).Not.ToContainTextAsync("Show closed");
        await Assertions.Expect(section).Not.ToContainTextAsync("Refresh");
        await Assertions.Expect(section.GetByRole(AriaRole.Button, new() { Name = "Add adjustment", Exact = true })).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth"));

        foreach ((string culture, string title, string direction) in new[] { ("fa", "اصلاح موجودی", "rtl"), ("ar", "تسوية المخزون", "rtl"), ("en", "Stock Adjustments", "ltr") })
        {
            await GoTo(culture);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
            await Assertions.Expect(page.Locator("[data-adjustment-grid] h2")).ToHaveTextAsync(title);
        }
    }

    [Fact]
    public async Task Positive_adjustment_posts_from_real_editor()
    {
        await GoTo("en");
        var section = page.Locator("[data-adjustment-grid]");
        Assert.True(int.Parse(await section.GetAttributeAsync("data-warehouse-options") ?? "0") > 1, "warehouse option count=" + await section.GetAttributeAsync("data-warehouse-options"));
        await section.GetByRole(AriaRole.Button, new() { Name = "Add adjustment", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add stock adjustment" });
        var selectors = dialog.Locator("input[role=combobox]");
        await Choose(selectors.Nth(0), "EW0_1788283843295");
        await Choose(selectors.Nth(1), "I14P1788268154");
        await Choose(selectors.Nth(2), "I14SKU1788268154");
        var reasonInput = dialog.Locator("label").Filter(new() { HasText = "Reason" }).Locator("input[role=combobox]");
        await reasonInput.FillAsync("count");
        await reasonInput.PressAsync("Enter");
        await dialog.Locator("input[type=number]").FillAsync("1");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Post adjustment", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(section).ToContainTextAsync("ADJ-");
    }

    private async Task GoTo(string culture)
    {
        await page.GotoAsync(baseUrl + $"/culture?culture={culture}&returnUrl=%2Fworkspace%2Finventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/workspace/inventory?section=adjustments", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-adjustment-grid]")).ToBeVisibleAsync();
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
        await context.CloseAsync();
        await browser.CloseAsync();
        pw.Dispose();
    }
}
