using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step9ProductionAcceptanceE2ETests : IAsyncLifetime
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
    public async Task ProductionWorkspaceGeneratesIdempotentlyAndEnforcesIssueGate()
    {
        string orderId = Required("BARANG_STEP9_ORDER_ID");
        string equipmentId = Required("BARANG_STEP9_EQUIPMENT_ID");
        string userId = Required("BARANG_STEP9_USER_ID");
        string item1Id = Required("BARANG_STEP9_ITEM1_ID");
        string item2Id = Required("BARANG_STEP9_ITEM2_ID");
        bool expectSuccess = string.Equals(Environment.GetEnvironmentVariable("BARANG_STEP9_EXPECT_SUCCESS"), "true", StringComparison.OrdinalIgnoreCase);
        int expectedJobsPerItem = int.TryParse(Environment.GetEnvironmentVariable("BARANG_STEP9_EXPECTED_JOBS_PER_ITEM"), out int configuredJobs) ? configuredJobs : 3;
        await page.GotoAsync($"{baseUrl}/workspace/production", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The application may be rendered in fa/en depending on the persisted user preference.
        await Assertions.Expect(page.Locator(".page-heading h1")).ToHaveCountAsync(1);
        ILocator fields = page.Locator("input:not([type=hidden])");
        await fields.Nth(0).FillAsync(orderId);
        ILocator actions = page.Locator(".form-actions button");
        await actions.Nth(0).ClickAsync();
        await Assertions.Expect(page.Locator(".record-row").Filter(new() { HasText = item1Id })).ToHaveCountAsync(expectedJobsPerItem);
        await Assertions.Expect(page.Locator(".record-row").Filter(new() { HasText = item2Id })).ToHaveCountAsync(expectedJobsPerItem);

        // The retry uses the same canonical generation key and must not add another job.
        await actions.Nth(0).ClickAsync();
        await Assertions.Expect(page.Locator(".record-row").Filter(new() { HasText = item1Id })).ToHaveCountAsync(expectedJobsPerItem);
        await Assertions.Expect(page.Locator(".record-row").Filter(new() { HasText = item2Id })).ToHaveCountAsync(expectedJobsPerItem);

        // Select the actual HeatPress row.  The fixture has two independent same-SKU
        // items and a print operation bound to the same artwork/file version.
        ILocator firstJob = page.Locator(".record-row").Filter(new() { HasText = $"2 · Status0" }).Filter(new() { HasText = item1Id }).First;
        await Assertions.Expect(firstJob).ToContainTextAsync("OrderItem:");
        await Assertions.Expect(firstJob).ToContainTextAsync("Artwork:");
        await Assertions.Expect(firstJob).ToContainTextAsync("File:");
        await firstJob.ClickAsync();
        fields = page.Locator("input:not([type=hidden])");
        await fields.Nth(2).FillAsync(userId);
        await actions.Nth(2).ClickAsync();
        Assert.NotEqual("0", await page.Locator("input[type=number]").InputValueAsync());
        fields = page.Locator("input:not([type=hidden])");
        await fields.Nth(3).FillAsync(equipmentId);
        await actions.Nth(3).ClickAsync();
        Assert.NotEqual("0", await page.Locator("input[type=number]").InputValueAsync());
        await page.Locator("input[placeholder='2026-01-01T08:00:00Z']").FillAsync("2026-10-16T08:00:00Z");
        await page.Locator("input[placeholder='2026-01-01T09:00:00Z']").FillAsync("2026-10-16T09:00:00Z");
        await actions.Nth(1).ClickAsync();

        await actions.Nth(4).ClickAsync();
        if (expectSuccess)
        {
            await Assertions.Expect(page.Locator(".inline-success")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".record-row").Filter(new() { HasText = "2 · Status2" }).First).ToBeVisibleAsync();
            string completionQuantity = Environment.GetEnvironmentVariable("BARANG_STEP9_COMPLETE_QUANTITY") ?? "2";
            await page.Locator("input:not([type=hidden])").Nth(5).FillAsync(completionQuantity);
            await actions.Nth(5).ClickAsync();
            await Assertions.Expect(page.Locator(".inline-success")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".record-row").Filter(new() { HasText = "2 · Status4" }).First).ToBeVisibleAsync();
        }
        else
        {
            await Assertions.Expect(page.Locator(".inline-error")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".record-row").First).Not.ToContainTextAsync("InProgress");
        }
        // The shared layout always renders the banner; a healthy circuit keeps it hidden.
        await Assertions.Expect(page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACTS") ?? ".artifacts", "step9-production-issue-gate.png"), FullPage = true });
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
