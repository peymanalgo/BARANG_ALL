using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class UserCreationFocusE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly string personName = Environment.GetEnvironmentVariable("BARANG_E2E_FOCUS_PERSON") ?? "Chrome Acceptance Person";
    private readonly string username = Environment.GetEnvironmentVariable("BARANG_E2E_FOCUS_USERNAME") ?? "chrome.focus.acceptance";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company") { await page.Locator(".company-list form button[type=submit]").First.ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); }
    }

    [Fact]
    public async Task Selected_person_survives_username_focus_and_save_reloads_grid()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fadmin%2Fuser-access", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".uam-primary-action").ClickAsync();
        ILocator dialog = page.Locator(".uam-dialog[aria-label='Add User']");
        ILocator personSearch = dialog.Locator(".searchable-select input").First;
        await personSearch.FillAsync(personName);
        await page.WaitForTimeoutAsync(500);
        await personSearch.PressAsync("ArrowDown");
        await personSearch.PressAsync("Enter");
        await Assertions.Expect(dialog.Locator(".uam-person-card")).ToBeVisibleAsync();
        ILocator usernameInput = dialog.Locator("input[autocomplete='username']");
        await usernameInput.FocusAsync();
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog.Locator(".uam-person-card")).ToContainTextAsync(personName);
        await usernameInput.FillAsync(username);
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_FOCUS_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_FOCUS_PASSWORD is required.");
        await dialog.Locator("input[autocomplete='new-password']").Nth(0).FillAsync(password);
        await dialog.Locator("input[autocomplete='new-password']").Nth(1).FillAsync(password);
        await dialog.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await page.Locator(".uam-search input").First.FillAsync(username);
        await page.Locator(".uam-search input").First.PressAsync("Tab");
        await page.WaitForTimeoutAsync(500);
        await Assertions.Expect(page.Locator(".uam-grid tbody tr")).ToContainTextAsync(username.ToUpperInvariant());
    }

    public async ValueTask DisposeAsync() { await context.CloseAsync(); await browser.CloseAsync(); playwright.Dispose(); }
}
