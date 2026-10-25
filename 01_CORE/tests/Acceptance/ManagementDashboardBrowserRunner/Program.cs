using Microsoft.Playwright;

string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://127.0.0.1:5822";
string browserPath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe";
string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required");
string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required");
string company = Environment.GetEnvironmentVariable("BARANG_E2E_COMPANY_ID") ?? throw new InvalidOperationException("BARANG_E2E_COMPANY_ID is required");
string artifactDirectory = Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACT_DIR") ?? ".artifacts";
bool requireAttention = !string.Equals(Environment.GetEnvironmentVariable("BARANG_E2E_REQUIRE_ATTENTION"), "false", StringComparison.OrdinalIgnoreCase);

Directory.CreateDirectory(artifactDirectory);
using IPlaywright playwright = await Playwright.CreateAsync();
await using IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = browserPath });
await using IBrowserContext context = await browser.NewContextAsync(new() { Locale = "fa-IR", ViewportSize = new() { Width = 1440, Height = 1000 } });
context.SetDefaultTimeout(12000);
IPage page = await context.NewPageAsync();
List<string> consoleErrors = [];
page.Console += (_, message) => { if (message.Type == "error") consoleErrors.Add($"{message.Text} @ {message.Location}"); };

await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.Locator("#username").FillAsync(username);
await page.Locator("#password").FillAsync(password);
await page.Locator("button[type=submit]").ClickAsync();
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
if (new Uri(page.Url).AbsolutePath == "/company")
{
    ILocator choices = page.Locator(".company-list form button[type=submit]");
    if (await choices.CountAsync() == 0) throw new InvalidOperationException("No company selection was rendered after login.");
    ILocator selected = page.Locator($"form input[name='companyId'][value='{company}']").Locator("xpath=..").Locator("button[type=submit]");
    if (await selected.CountAsync() == 0) selected = choices.First;
    await selected.ClickAsync();
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
}

await page.GotoAsync($"{baseUrl}/workspace/management", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.Locator("#dashboard-heading").WaitForAsync(new() { State = WaitForSelectorState.Visible });
if (await page.Locator(".dashboard-error").CountAsync() != 0) throw new InvalidOperationException("Dashboard rendered an error state.");
if (await page.Locator(".metric-card").CountAsync() < 3) throw new InvalidOperationException("Dashboard did not render the expected primary indicators.");
if ((await page.Locator(".metric-value").AllTextContentsAsync()).All(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("Dashboard primary indicators are blank.");
if (await page.Locator(".dashboard-hero").CountAsync() != 1 || await page.Locator(".dashboard-toolbar").CountAsync() != 1) throw new InvalidOperationException("Dashboard header/toolbar is incomplete.");
await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "management-dashboard-fa-desktop.png"), FullPage = false });
ILocator attention = page.Locator(".attention-row");
if (requireAttention)
{
    if (await attention.CountAsync() == 0) throw new InvalidOperationException("A validated populated fixture did not render a Needs Attention record.");
    await attention.First.ClickAsync();
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    if (!new Uri(page.Url).AbsolutePath.Equals("/workspace/inventory", StringComparison.OrdinalIgnoreCase) && !new Uri(page.Url).AbsolutePath.Contains("/workspace/orders", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Needs Attention drill-down navigated to an unexpected route: {page.Url}");
    await page.Locator(".inventory-workspace").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    if (!await page.Locator(".reference-navigation").IsVisibleAsync()) throw new InvalidOperationException($"Needs Attention drill-down did not render the inventory navigation: {page.Url}");
    await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "management-dashboard-attention-drilldown.png"), FullPage = false });
}
await page.CloseAsync();
page = await context.NewPageAsync();
page.Console += (_, message) => { if (message.Type == "error") consoleErrors.Add($"{message.Text} @ {message.Location}"); };
await page.GotoAsync($"{baseUrl}/workspace/management", new() { WaitUntil = WaitUntilState.Commit });
await page.Locator("#dashboard-heading").WaitForAsync(new() { State = WaitForSelectorState.Visible });

await page.SetViewportSizeAsync(390, 844);
await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.Locator("#dashboard-heading").WaitForAsync(new() { State = WaitForSelectorState.Visible });
await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "management-dashboard-fa-mobile.png"), FullPage = false });

await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Fmanagement", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.Locator("#dashboard-heading").WaitForAsync(new() { State = WaitForSelectorState.Visible });
await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "management-dashboard-en.png"), FullPage = false });

await page.GotoAsync($"{baseUrl}/culture?culture=ar&returnUrl=%2Fworkspace%2Fmanagement", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.Locator("#dashboard-heading").WaitForAsync(new() { State = WaitForSelectorState.Visible });
await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "management-dashboard-ar.png"), FullPage = false });

if (consoleErrors.Count > 0) throw new InvalidOperationException("Browser console errors: " + string.Join(" | ", consoleErrors));
Console.WriteLine($"PASS management-dashboard url={page.Url} metricCards={await page.Locator(".metric-card").CountAsync()} stages={await page.Locator(".stage-row").CountAsync()} attention={await page.Locator(".attention-row").CountAsync()} screenshots={artifactDirectory}");
