using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step10QualityAcceptanceE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;

    private static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is required.");

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Required("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{Required("BARANG_E2E_BASE_URL")}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Required("BARANG_E2E_USERNAME"));
        await page.Locator("#password").FillAsync(Required("BARANG_E2E_PASSWORD"));
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await BrowserCompanyContext.SelectIfRequiredAsync(page);
    }

    [Fact]
    public async Task QualityInspectionIsRecordedAndReloadedThroughWorkspace()
    {
        string baseUrl = Required("BARANG_E2E_BASE_URL");
        await page.GotoAsync($"{baseUrl}/workspace/quality", new() { WaitUntil = WaitUntilState.NetworkIdle });
        ILocator first = page.Locator(".quality-workspace").Nth(0);
        await first.Locator("input").Nth(0).FillAsync(Required("BARANG_STEP10_JOB_ID"));
        await first.Locator("input").Nth(1).FillAsync(Required("BARANG_STEP10_TEMPLATE_ID"));
        await first.GetByRole(AriaRole.Button).Nth(0).ClickAsync();
        await Assertions.Expect(page.Locator(".inline-error")).ToHaveCountAsync(0);

        ILocator disposition = page.Locator(".quality-workspace").Nth(1);
        await disposition.Locator("input[type=number]").Nth(0).FillAsync(Required("BARANG_STEP10_INSPECTABLE_QUANTITY"));
        await disposition.Locator("input").Nth(3).FillAsync("accepted through browser");
        await disposition.GetByRole(AriaRole.Button).Nth(0).ClickAsync();
        await Assertions.Expect(page.Locator(".inline-error")).ToHaveCountAsync(0);
        ILocator result = page.Locator(".quality-workspace").Nth(2);
        await result.Locator("input").Nth(0).FillAsync(Required("BARANG_STEP10_CRITERION_ID"));
        await result.GetByRole(AriaRole.Button).Nth(0).ClickAsync();
        if (await page.Locator(".inline-error").CountAsync() > 0) Console.WriteLine($"quality result error: {await page.Locator(".inline-error").InnerTextAsync()}");
        await Assertions.Expect(page.Locator(".inline-error")).ToHaveCountAsync(0);
        await disposition.GetByRole(AriaRole.Button).Nth(1).ClickAsync();
        await Assertions.Expect(page.Locator(".inline-error")).ToHaveCountAsync(0);

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await first.Locator("input").Nth(2).FillAsync(Required("BARANG_STEP10_INSPECTION_ID"));
        await first.GetByRole(AriaRole.Button).Nth(1).ClickAsync();
        await Assertions.Expect(page.Locator(".quality-json").Nth(0)).ToContainTextAsync(Required("BARANG_STEP10_INSPECTION_ID"));
        await Assertions.Expect(page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACTS") ?? ".artifacts", "step10-quality-browser.png"), FullPage = true });
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
