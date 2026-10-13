using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class WarehouseSectionE2ETests : IAsyncLifetime
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
            await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath != "/company");
        }
    }

    [Fact]
    public async Task WarehouseGridAndCrudAreRealServerBackedWorkflows()
    {
        await GoToWarehouses("en");
        ILocator section = page.Locator("[data-warehouse-grid]");
        await Assertions.Expect(section).ToBeVisibleAsync();
        await Assertions.Expect(section.Locator(".crud-toolbar input")).ToHaveCountAsync(1);
        await Assertions.Expect(section.Locator(".crud-toolbar button")).ToHaveCountAsync(1);
        await Assertions.Expect(section.Locator(".barang-sort-header")).ToHaveCountAsync(4);
        await section.Locator(".barang-sort-header").Nth(1).ClickAsync();
        await Assertions.Expect(section.Locator("tbody")).ToBeVisibleAsync();
        await Assertions.Expect(section.Locator(".crud-toolbar")).Not.ToContainTextAsync("Refresh");
        await Assertions.Expect(section.Locator(".crud-toolbar")).Not.ToContainTextAsync("Show inactive");
        await Assertions.Expect(section.Locator(".crud-toolbar")).Not.ToContainTextAsync("Show closed");

        string suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        string code = "E2E-WH-" + suffix;
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        ILocator dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByText("Warehouse code", new() { Exact = true }).Locator("..") .Locator("input").FillAsync(code);
        await dialog.GetByText("Warehouse name", new() { Exact = true }).Locator("..") .Locator("input").FillAsync("Browser warehouse " + suffix);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        ILocator row = section.Locator("tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row).ToBeVisibleAsync();

        await row.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog).Locator("input").First).ToBeDisabledAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();

        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByText("Warehouse name", new() { Exact = true }).Locator("..") .Locator("input").FillAsync("Edited warehouse " + suffix);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(section.Locator("tbody tr").Filter(new() { HasText = "Edited warehouse " + suffix })).ToBeVisibleAsync();

        page.Dialog += async (_, d) => await d.AcceptAsync();
        row = section.Locator("tbody tr").Filter(new() { HasText = code });
        await row.GetByRole(AriaRole.Button, new() { Name = "Deactivate", Exact = true }).ClickAsync();
        row = section.Locator("tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row.GetByText("Inactive", new() { Exact = true })).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Activate", Exact = true }).ClickAsync();
        row = section.Locator("tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row.GetByText("Active", new() { Exact = true })).ToBeVisibleAsync();

        await section.Locator(".crud-search input").FillAsync(code);
        await Assertions.Expect(section.Locator("tbody tr").Filter(new() { HasText = code })).ToHaveCountAsync(1);
        await section.Locator(".crud-toolbar button").ClickAsync();
        await Assertions.Expect(section.Locator(".crud-search input")).ToHaveValueAsync("");

        await row.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Assertions.Expect(section).ToContainTextAsync("protected records");
        string disposableCode = "E2E-WH-DEL-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByText("Warehouse code", new() { Exact = true }).Locator("..").Locator("input").FillAsync(disposableCode);
        await dialog.GetByText("Warehouse name", new() { Exact = true }).Locator("..").Locator("input").FillAsync("Disposable warehouse " + disposableCode);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        ILocator disposableRow = section.Locator("tbody tr").Filter(new() { HasText = disposableCode });
        await Assertions.Expect(disposableRow).ToBeVisibleAsync();
        await disposableRow.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Assertions.Expect(section.Locator("tbody tr").Filter(new() { HasText = disposableCode })).ToHaveCountAsync(0);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-warehouse-grid] tbody tr").Filter(new() { HasText = code })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task WarehouseSectionLocalizesAndPreservesDirection()
    {
        foreach ((string culture, string title, string direction) in new[]
        {
            ("fa", "انبارها", "rtl"),
            ("ar", "المستودعات", "rtl"),
            ("en", "Warehouses", "ltr")
        })
        {
            await GoToWarehouses(culture);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
            await Assertions.Expect(page.Locator("[data-warehouse-grid] h2")).ToHaveTextAsync(title);
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
        }
    }

    private async Task GoToWarehouses(string culture)
    {
        await page.GotoAsync(baseUrl + $"/culture?culture={culture}&returnUrl=%2Fworkspace%2Finventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/workspace/inventory", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Link, new() { Name = culture == "fa" ? "انبارها" : culture == "ar" ? "المستودعات" : "Warehouses", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-warehouse-grid]")).ToBeVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        pw.Dispose();
    }
}
