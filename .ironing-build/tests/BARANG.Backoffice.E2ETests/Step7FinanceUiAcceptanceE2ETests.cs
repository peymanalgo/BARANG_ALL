using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step7FinanceUiAcceptanceE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly string orderId = Environment.GetEnvironmentVariable("BARANG_E2E_FINANCE_ORDER_ID") ?? "";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Flogin", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        string? company = Environment.GetEnvironmentVariable("BARANG_E2E_COMPANY_ID");
        if (new Uri(page.Url).AbsolutePath == "/company" && company is not null)
            await page.Locator($".company-list form:has(input[name=companyId][value='{company}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(u => new Uri(u).AbsolutePath != "/company");
    }

    [Fact]
    public async Task FinanceEvidenceAndOperationsAreRenderedForPersistedOrder()
    {
        Assert.True(Guid.TryParse(orderId, out _), "BARANG_E2E_FINANCE_ORDER_ID must identify an isolated submitted order.");
        await page.GotoAsync($"{baseUrl}/finance/{orderId}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        ILocator evidence = page.Locator(".finance-evidence");
        await Assertions.Expect(evidence).ToBeVisibleAsync();
        await Assertions.Expect(evidence).ToContainTextAsync("Financial status");
        await Assertions.Expect(evidence).ToContainTextAsync("Approved pricing snapshot");
        await Assertions.Expect(evidence).ToContainTextAsync("Paid");
        await Assertions.Expect(evidence).ToContainTextAsync("Outstanding");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Payment", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Invoice", Exact = true })).ToBeVisibleAsync();
        ILocator paymentPanel = page.Locator(".management-grid section.panel").First;
        await paymentPanel.Locator("input").Nth(1).FillAsync("50");
        await paymentPanel.Locator("input").Nth(3).FillAsync("STEP7-UI-PAY-1");
        await paymentPanel.GetByRole(AriaRole.Button, new() { Name = "Payment", Exact = true }).ClickAsync();
        await Assertions.Expect(evidence).ToContainTextAsync("50");
        await page.GetByRole(AriaRole.Button, new() { Name = "Invoice", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".inline-error")).ToBeVisibleAsync();
        Assert.DoesNotContain("blazor-error-ui", await page.Locator("body").GetAttributeAsync("class") ?? "", StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await browser.DisposeAsync();
        playwright.Dispose();
    }
}
