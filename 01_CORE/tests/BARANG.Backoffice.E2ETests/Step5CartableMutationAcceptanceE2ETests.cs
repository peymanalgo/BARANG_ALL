using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step5CartableMutationAcceptanceE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? throw new InvalidOperationException("BARANG_E2E_BASE_URL is required.");
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is required.");

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Required("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Required("BARANG_E2E_USERNAME"));
        await page.Locator("#password").FillAsync(Required("BARANG_E2E_PASSWORD"));
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await BrowserCompanyContext.SelectIfRequiredAsync(page);
    }

    [Fact]
    public async Task ReservationShortageKeepsTaskOpenWithPreciseUiError()
    {
        string task = Required("BARANG_STEP5_SHORTAGE_TASK_ID");
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable%2Ftask%2F{task}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync($"{baseUrl}/cartable/task/{task}?returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#reservation-warehouse").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("section.cartable-actions-panel button").First.ClickAsync();
        await page.Locator("section.cartable-operational-popup .btn-primary").ClickAsync();
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Shortage; task remains open.");
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/cartable/task/{task}\\?returnUrl="));
        await page.ScreenshotAsync(new() { Path = Path.Combine(Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACTS") ?? ".artifacts", "step5-shortage-open.png"), FullPage = true });
    }

    [Fact]
    public async Task PostedIssueCompletesInventoryTaskAndReloadsToCartable()
    {
        string task = Required("BARANG_STEP5_ISSUE_TASK_ID");
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable%2Ftask%2F{task}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync($"{baseUrl}/cartable/task/{task}?returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#reservation-warehouse").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("section.cartable-actions-panel button").First.ClickAsync();
        await page.Locator("section.cartable-operational-popup .btn-primary").ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("/cartable$"), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("Blazor error");
        await page.ScreenshotAsync(new() { Path = Path.Combine(Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACTS") ?? ".artifacts", "step5-issue-complete.png"), FullPage = true });
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
