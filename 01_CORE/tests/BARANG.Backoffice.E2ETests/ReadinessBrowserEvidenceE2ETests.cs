using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class ReadinessBrowserEvidenceE2ETests : IAsyncLifetime
{
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private string artifactDirectory = null!;

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        artifactDirectory = Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACT_DIR") ?? Path.Combine(Environment.CurrentDirectory, "step3-readiness-browser");
        Directory.CreateDirectory(artifactDirectory);
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US" });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Fact]
    public async Task ShellEvidenceCapturesScreenshotAndTrace()
    {
        string trace = Path.Combine(artifactDirectory, "shell.trace.zip");
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2F", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("aside.sidebar")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("a.home-shortcut").First).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "shell.png"), FullPage = true });
        await context.Tracing.StopAsync(new() { Path = trace });
    }

    [Theory]
    [InlineData("fa", "rtl", "^سلام، .+$")]
    [InlineData("ar", "rtl", "^مرحباً، .+$")]
    [InlineData("en", "ltr", "^Hello, .+$")]
    public async Task CultureEvidenceCapturesScreenshotAndTrace(string culture, string direction, string greetingPattern)
    {
        string safeCulture = culture.ToLowerInvariant();
        string trace = Path.Combine(artifactDirectory, $"culture-{safeCulture}.trace.zip");
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2F", new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal(culture, await page.Locator("html").GetAttributeAsync("lang"));
        Assert.Equal(direction, await page.Locator("html").GetAttributeAsync("dir"));
        await Assertions.Expect(page.Locator("main h1")).ToHaveTextAsync(new Regex(greetingPattern));
        await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, $"culture-{safeCulture}.png"), FullPage = true });
        await context.Tracing.StopAsync(new() { Path = trace });
    }
}
