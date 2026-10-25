using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class DesignCategoryAcceptanceTempE2ETests : IAsyncLifetime
{
    private IPlaywright pw = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync()
    {
        pw = await Playwright.CreateAsync();
        browser = await pw.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Flogin", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")!);
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")!);
        await page.Locator("button[type=submit]").ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company") await page.Locator(".company-list form button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(url => new Uri(url).AbsolutePath != "/company");
    }
    public async ValueTask DisposeAsync(){ await context.DisposeAsync(); await browser.DisposeAsync(); pw.Dispose(); }
    [Fact] public async Task TreeCrud(){
        var master = page.Locator(".nav-group[data-navigation-group='Nav_MasterData']"); await master.Locator(":scope > summary").ClickAsync();
        var artwork = master.GetByRole(AriaRole.Link,new(){Name="Design Bank Reference Data",Exact=true}); await artwork.ClickAsync(); await page.WaitForURLAsync("**/master/reference-data/artwork"); await page.WaitForTimeoutAsync(2000);
        await Assertions.Expect(page.Locator("[role=tree]")).ToBeVisibleAsync(); Assert.Equal(0,await page.Locator("table").CountAsync());
        string s=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture); string root=$"E2E ROOT {s}",child=$"E2E CHILD {s}",grand=$"E2E GRANDCHILD {s}";
        await page.Locator(".design-category-admin header button[title='Add root category']").ClickAsync(); await Assertions.Expect(page.Locator("input[name=category-code]")).ToBeVisibleAsync(); await Fill("root",root); await Save();
        var rootNode=NodeByCaption(root); await Assertions.Expect(rootNode).ToBeVisibleAsync();
        await rootNode.Locator("button[title='Add subcategory']").ClickAsync(); await Fill("child",child); await Save();
        rootNode=NodeByCaption(root); await Assertions.Expect(rootNode).ToContainTextAsync(child);
        var childNode=NodeByCaption(child); await childNode.Locator("button[title='Add subcategory']").ClickAsync(); await Fill("grand",grand); await Save();
        await Assertions.Expect(NodeByCaption(grand)).ToBeVisibleAsync();
        await rootNode.Locator(".tree-toggle[title='Collapse']").First.ClickAsync(); await Assertions.Expect(NodeByCaption(child)).ToBeHiddenAsync();
        await rootNode.Locator(".tree-toggle[title='Expand']").First.ClickAsync(); await Assertions.Expect(NodeByCaption(grand)).ToBeVisibleAsync();
        await childNode.Locator("button[title='Edit']").First.ClickAsync(); await page.Locator("input[name=category-en]").FillAsync(child+" EDITED"); await Save(); await Assertions.Expect(NodeByCaption(child+" EDITED")).ToBeVisibleAsync();
        await page.ReloadAsync(new(){WaitUntil=WaitUntilState.NetworkIdle}); await Assertions.Expect(NodeByCaption(grand)).ToBeVisibleAsync();
        await NodeByCaption(grand).GetByRole(AriaRole.Button,new(){Name="Delete",Exact=true}).ClickAsync(); await page.GetByRole(AriaRole.Button,new(){Name="Confirm",Exact=true}).ClickAsync(); await Assertions.Expect(NodeByCaption(grand)).ToBeHiddenAsync();
    }
    private async Task Fill(string suffix,string value){ await page.Locator("input[name=category-code]").FillAsync("e2e."+suffix+DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); await page.Locator("input[name=category-fa]").FillAsync(value); await page.Locator("input[name=category-ar]").FillAsync(value); await page.Locator("input[name=category-en]").FillAsync(value); }
    private async Task Save(){ await page.Locator(".barang-modal.modal-large footer .btn-primary").ClickAsync(); await Assertions.Expect(page.Locator(".barang-modal.modal-large")).ToBeHiddenAsync(new(){Timeout=15000}); }
    private ILocator NodeByCaption(string caption) => page.Locator("[data-category-id]").Filter(new(){Has=page.Locator(":scope > .design-tree-row > .tree-node-caption").Filter(new(){HasText=caption})}).First;
}
