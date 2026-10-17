using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5510";
string apiUrl = Environment.GetEnvironmentVariable("BARANG_E2E_API_URL") ?? "http://localhost:5808";
string browserPath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe";
string company = Environment.GetEnvironmentVariable("BARANG_E2E_COMPANY_ID") ?? throw new InvalidOperationException("BARANG_E2E_COMPANY_ID is required");
string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin";
string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026";
string warehouseId = Environment.GetEnvironmentVariable("BARANG_ADJUSTMENT_WAREHOUSE_ID") ?? throw new InvalidOperationException("BARANG_ADJUSTMENT_WAREHOUSE_ID is required");
string productCode = Environment.GetEnvironmentVariable("BARANG_ADJUSTMENT_PRODUCT_CODE") ?? "ADJ-P";
string sku = Environment.GetEnvironmentVariable("BARANG_ADJUSTMENT_SKU") ?? "ADJ-SKU";
string reference = "ADJ-BROWSER-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

using HttpClient http = new() { BaseAddress = new Uri(apiUrl) };
using HttpResponseMessage loginResponse = await http.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
loginResponse.EnsureSuccessStatusCode();
using JsonDocument login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
string token = login.RootElement.GetProperty("accessToken").GetString()!;
using HttpRequestMessage select = new(HttpMethod.Post, "/api/v1/auth/me/company") { Content = JsonContent.Create(new { companyId = company }) };
select.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
(await http.SendAsync(select)).EnsureSuccessStatusCode();

using HttpRequestMessage warehouses = new(HttpMethod.Get, "/api/v1/organization/warehouses?pageNumber=1&pageSize=100");
warehouses.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); warehouses.Headers.Add("X-Company-ID", company);
using JsonDocument warehouseJson = JsonDocument.Parse(await (await http.SendAsync(warehouses)).Content.ReadAsStringAsync());
JsonElement warehouseItems = warehouseJson.RootElement.ValueKind == JsonValueKind.Object && warehouseJson.RootElement.TryGetProperty("items", out JsonElement wi) ? wi : warehouseJson.RootElement;
if (!warehouseItems.EnumerateArray().Any(x => x.GetProperty("id").GetString() == warehouseId)) throw new InvalidOperationException("The isolated warehouse prerequisite is absent.");

using IPlaywright playwright = await Playwright.CreateAsync();
await using IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = browserPath });
await using IBrowserContext context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
context.SetDefaultTimeout(12000);
IPage page = await context.NewPageAsync();
List<string> consoleErrors = [];
page.Console += (_, message) => { if (message.Type == "error") consoleErrors.Add(message.Text); };
await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.Locator("#username").FillAsync(username); await page.Locator("#password").FillAsync(password); await page.Locator("button[type=submit]").ClickAsync();
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
if (new Uri(page.Url).AbsolutePath == "/company") { await page.Locator(".company-list form button[type=submit]").First.ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); }

await page.GotoAsync($"{baseUrl}/workspace/inventory?section=adjustments", new() { WaitUntil = WaitUntilState.NetworkIdle });
ILocator section = page.Locator("section.stock-adjustment-section"); await section.WaitForAsync();
ILocator search = section.Locator("input").First; await search.FillAsync("NO-SUCH-ADJUSTMENT-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); await page.WaitForTimeoutAsync(500);
if (await section.GetByText("اصلاحی یافت نشد", new() { Exact = true }).CountAsync() != 1) throw new InvalidOperationException($"Successful-empty state was not rendered. body={await section.InnerTextAsync()}");
if (await section.GetByText("دریافت اصلاح‌ها انجام نشد؛ دوباره تلاش کنید.", new() { Exact = true }).CountAsync() != 0) throw new InvalidOperationException("Empty state incorrectly contains a load error.");
await search.FillAsync(""); await page.WaitForTimeoutAsync(500);

await section.Locator(".section-header button").ClickAsync();
ILocator dialog = page.GetByRole(AriaRole.Dialog); await dialog.WaitForAsync();
await Choose(page, dialog, 0, "ADJ-W - Adjustment Browser Warehouse");
await Choose(page, dialog, 1, $"{productCode} - Adjustment Product");
await Choose(page, dialog, 2, sku);
await dialog.Locator("select").SelectOptionAsync("positive");
await dialog.Locator("input[type=number]").FillAsync("7.5");
await Choose(page, dialog, 3, "چرخه شمارش");
await dialog.Locator("input").Last.FillAsync(reference);
await dialog.GetByRole(AriaRole.Button, new() { Name = "ثبت اصلاح", Exact = true }).ClickAsync();
await page.GetByRole(AriaRole.Dialog).Last.GetByRole(AriaRole.Button, new() { Name = "تأیید", Exact = true }).ClickAsync();
await section.GetByText(reference, new() { Exact = true }).WaitForAsync();
ILocator postedRow = section.Locator("tbody tr").Filter(new() { HasText = reference });
if (await postedRow.CountAsync() != 1) throw new InvalidOperationException("Posted adjustment was not persisted as one grid row.");
await postedRow.GetByRole(AriaRole.Button).ClickAsync();
await page.GetByRole(AriaRole.Button, new() { Name = "مشاهده", Exact = true }).ClickAsync(); await page.WaitForTimeoutAsync(500);
if (await page.GetByRole(AriaRole.Dialog).Last.GetByText(reference, new() { Exact = true }).CountAsync() != 1) throw new InvalidOperationException($"View action did not reload persisted adjustment details. body={await page.Locator("body").InnerTextAsync()}");
await page.Keyboard.PressAsync("Escape");

await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle }); await section.WaitForAsync(); await section.GetByText(reference, new() { Exact = true }).WaitForAsync();
await page.ScreenshotAsync(new() { Path = ".artifacts/stock-adjustments-browser.png", FullPage = true });
if (consoleErrors.Count > 0) throw new InvalidOperationException("Browser console errors: " + string.Join(" | ", consoleErrors));
Console.WriteLine($"PASS stock-adjustments url={page.Url} reference={reference} rows={await section.Locator("tbody tr").CountAsync()} screenshot=.artifacts/stock-adjustments-browser.png");

static async Task Choose(IPage page, ILocator dialog, int index, string optionText)
{
    ILocator input = dialog.Locator(".searchable-select input").Nth(index);
    await input.FocusAsync(); await input.PressAsync("ArrowDown"); await input.FillAsync(""); await page.WaitForTimeoutAsync(350);
    ILocator option = page.GetByRole(AriaRole.Option, new() { Name = optionText, Exact = true });
    if (await option.CountAsync() == 0) throw new InvalidOperationException($"Option missing index={index} expected={optionText} dialog={await dialog.InnerTextAsync()} section={await page.Locator("section.stock-adjustment-section").GetAttributeAsync("data-warehouse-options")}/{await page.Locator("section.stock-adjustment-section").GetAttributeAsync("data-product-options")}/{await page.Locator("section.stock-adjustment-section").GetAttributeAsync("data-reason-options")} body={await page.Locator("body").InnerTextAsync()}");
    await option.ClickAsync();
}
