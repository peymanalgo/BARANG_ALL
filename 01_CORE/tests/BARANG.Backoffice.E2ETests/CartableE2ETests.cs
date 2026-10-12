using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class CartableE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")??@"C:\Program Files\Google\Chrome\Application\chrome.exe"});context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();await page.GotoAsync($"{baseUrl}/login",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin");await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026");await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);if(new Uri(page.Url).AbsolutePath=="/company"){await page.Locator(".company-list form button[type=submit]").First.ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);}}
    [Theory]
    [InlineData("fa","کارتابل من","rtl")]
    [InlineData("ar","صندوق مهامي","rtl")]
    [InlineData("en","My Cartable","ltr")]
    public async Task CartableLoadsInAllLocales(string culture,string title,string direction){await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2Fcartable",new(){WaitUntil=WaitUntilState.NetworkIdle});await page.WaitForSelectorAsync("h1");await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(title);await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",direction);await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("DateTimeOffset");}
    [Fact]
    public async Task DeployedNavigationHasSiblingDirectCartable(){await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Forders",new(){WaitUntil=WaitUntilState.NetworkIdle});var orders=page.Locator("a.nav-item[href='/workspace/orders']");var cartable=page.Locator("a.nav-item[href='/cartable']");await Assertions.Expect(orders).ToHaveCountAsync(1);await Assertions.Expect(cartable).ToHaveCountAsync(1);var hrefs=await page.Locator("nav.sidebar-navigation > a.nav-item").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('href'))");Assert.True(Array.IndexOf(hrefs,"/cartable") == Array.IndexOf(hrefs,"/workspace/orders") + 1);await Assertions.Expect(cartable.Locator(".nav-chevron")).ToHaveCountAsync(0);await cartable.ClickAsync();await page.WaitForURLAsync("**/cartable",new(){WaitUntil=WaitUntilState.NetworkIdle});await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("My Cartable");}
    public async ValueTask DisposeAsync(){await context.CloseAsync();await browser.CloseAsync();playwright.Dispose();}
}
