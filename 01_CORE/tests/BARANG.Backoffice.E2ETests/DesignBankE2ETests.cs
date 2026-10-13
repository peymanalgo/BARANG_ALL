using Microsoft.Playwright;
using System.IO.Compression;

namespace BARANG.Backoffice.E2ETests;

public sealed class DesignBankE2ETests : IAsyncLifetime
{
    private const string Company = "42f0e459-6095-45bb-aa13-f38126d91d9c";
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await Login();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await browser.DisposeAsync();
        playwright.Dispose();
    }

    [Fact]
    public async Task DesignBankCreateReloadTabsAndLocalizedDirection()
    {
        string code = "E2E-D" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Go("en", "/manage/design-bank");
        await Assertions.Expect(page.Locator(".sidebar-navigation > a[href='/manage/design-bank']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".sidebar a[href='/manage/artwork-files/tags'],.sidebar a[href='/manage/artwork-files/history']")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("h1")).ToBeVisibleAsync();
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));
        await page.Locator(".crud-header button.btn-primary").ClickAsync();
        ILocator modal = page.Locator(".barang-modal").Last;
        Assert.Contains("modal-extra-large", await modal.GetAttributeAsync("class"));
        Assert.False(await modal.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1"));
        ILocator inputs = modal.Locator("input");
        await inputs.Nth(0).FillAsync(code);
        await inputs.Nth(1).FillAsync("Browser Design");
        await Assertions.Expect(modal.Locator("[role=tab]")).ToHaveCountAsync(4);
        foreach (ILocator tab in await modal.Locator("[role=tab]").AllAsync())
        {
            await tab.ClickAsync();
            Assert.False(await modal.EvaluateAsync<bool>("el => { const b = el.querySelector('.dialog-body'); return !!b && b.scrollWidth > b.clientWidth + 1; }"));
        }
        await modal.Locator("button.btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await page.Locator(".crud-toolbar input").FillAsync(code);
        await Assertions.Expect(page.Locator(".barang-loading-state")).ToHaveCountAsync(0, new() { Timeout = 15000 });
        ILocator row = page.Locator(".data-grid tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".data-grid tbody tr").Filter(new() { HasText = code })).ToBeVisibleAsync();

        await Go("fa", "/manage/design-bank");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(page.Locator(".crud-header button.btn-primary")).ToBeVisibleAsync();
        await Go("ar", "/manage/design-bank");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Go("en", "/manage/design-bank");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [Fact]
    public async Task DesignFileUploadCreatesThumbnailAndHistoryEvents()
    {
        string code = "E2E-F" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Go("en", "/manage/design-bank");
        await page.Locator(".crud-header button.btn-primary").ClickAsync();
        ILocator modal = page.Locator(".barang-modal").Last;
        await modal.Locator("input").Nth(0).FillAsync(code);
        await modal.Locator("input").Nth(1).FillAsync("File Design");
        await modal.Locator("button.btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await page.Locator(".crud-toolbar input").FillAsync(code);
        await Assertions.Expect(page.Locator(".barang-loading-state")).ToHaveCountAsync(0, new() { Timeout = 15000 });
        ILocator row = page.Locator(".data-grid tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        modal = page.Locator(".barang-modal").Last;
        await modal.GetByRole(AriaRole.Tab, new() { Name = "Files", Exact = true }).ClickAsync();
        string path = Path.Combine(Path.GetTempPath(), $"{code}.png");
        int seed = Math.Abs(code.GetHashCode());
        await File.WriteAllBytesAsync(path, CreatePng((byte)(seed % 251), (byte)((seed / 251) % 251), (byte)((seed / 63001) % 251)), TestContext.Current.CancellationToken);
        try
        {
            await modal.Locator("input[type=file]").SetInputFilesAsync(path);
            await Assertions.Expect(modal).ToContainTextAsync("Upload completed.", new() { Timeout = 15000 });
        }
        finally { File.Delete(path); }
        await modal.GetByRole(AriaRole.Tab, new() { Name = "History", Exact = true }).ClickAsync();
        await Assertions.Expect(modal).ToContainTextAsync("Created");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await Assertions.Expect(row.Locator(".design-thumb")).ToBeVisibleAsync(new() { Timeout = 15000 });
        await row.Locator(".design-thumb-button").ClickAsync();
        await Assertions.Expect(page.Locator(".design-viewer")).ToBeVisibleAsync();
        await page.Locator(".barang-modal").Last.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
    }

    [Fact]
    public async Task DesignCategoryTreeCreatesRootAndChildAndReloads()
    {
        string rootCode = "E2E-C" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Go("en", "/master/design-categories");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).First.ClickAsync();
        ILocator modal = page.Locator(".barang-modal").Last;
        await modal.Locator("input").Nth(0).FillAsync(rootCode);
        await modal.Locator("input").Nth(1).FillAsync("Root");
        await modal.Locator("input").Nth(2).FillAsync("جذر");
        await modal.Locator("input").Nth(3).FillAsync("Root");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".design-tree-row").Filter(new() { HasText = rootCode })).ToBeVisibleAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".design-tree-row").Filter(new() { HasText = rootCode })).ToBeVisibleAsync();
        string parentCode = rootCode;
        for (int level = 2; level <= 10; level++)
        {
            string childCode = $"{rootCode}-{level}";
            ILocator parentRow = page.Locator(".design-tree-row").Filter(new() { HasText = parentCode }).First;
            await parentRow.GetByRole(AriaRole.Button, new() { Name = "Add subcategory", Exact = true }).ClickAsync(new() { Force = true });
            modal = page.Locator(".barang-modal").Last;
            await modal.Locator("input").Nth(0).FillAsync(childCode);
            await modal.Locator("input").Nth(1).FillAsync($"Level {level}");
            await modal.Locator("input").Nth(2).FillAsync($"المستوى {level}");
            await modal.Locator("input").Nth(3).FillAsync($"Level {level}");
            await modal.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
            await Assertions.Expect(modal).ToBeHiddenAsync();
            if (level >= 3)
            {
                ILocator expandRow = page.Locator(".design-tree-row").Filter(new() { HasText = parentCode }).First;
                ILocator expand = expandRow.GetByRole(AriaRole.Button, new() { Name = "Expand", Exact = true });
                if (await expand.CountAsync() > 0) await expand.ClickAsync(new() { Force = true });
            }
            await Assertions.Expect(page.Locator(".design-tree-row").Filter(new() { HasText = childCode })).ToBeVisibleAsync();
            parentCode = childCode;
        }
    }

    [Fact]
    public async Task DesignCategoriesNavigationOpensVisibleCategoryTree()
    {
        await Go("en", "/");
        ILocator masterData = page.Locator("details[data-navigation-group='Nav_MasterData']");
        if (await masterData.GetAttributeAsync("open") is null)
            await masterData.Locator(":scope > summary").ClickAsync();
        ILocator designReferences = masterData.Locator("a[href='/master/reference-data/artwork']");
        await designReferences.ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/master/reference-data/artwork$"));
        await Assertions.Expect(page.Locator(".reference-navigation a").First).ToContainTextAsync("Artwork Categories");
        await Assertions.Expect(page.Locator(".design-category-tree")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".data-grid")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".embedded-design-category-admin h1")).ToContainTextAsync("Design categories");

        await Go("fa", "/");
        masterData = page.Locator("details[data-navigation-group='Nav_MasterData']");
        if (await masterData.GetAttributeAsync("open") is null)
            await masterData.Locator(":scope > summary").ClickAsync();
        designReferences = masterData.Locator("a[href='/master/reference-data/artwork']");
        await designReferences.ClickAsync();
        await page.Locator(".reference-navigation a").First.ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(page.Locator(".design-category-tree")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".embedded-design-category-admin h1")).ToContainTextAsync("دسته");
    }

    [Fact]
    public async Task DesignEditorCategoryTabReportsLayoutGeometry()
    {
        string code = "E2E-L" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Go("en", "/manage/design-bank");
        await page.Locator(".crud-header button.btn-primary").ClickAsync();
        ILocator modal = page.Locator(".barang-modal").Last;
        await modal.Locator("input").Nth(0).FillAsync(code);
        await modal.Locator("input").Nth(1).FillAsync("Layout Design");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await page.Locator(".crud-toolbar input").FillAsync(code);
        ILocator row = page.Locator(".data-grid tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        modal = page.Locator(".barang-modal").Last;
        await modal.GetByRole(AriaRole.Tab, new() { Name = "Classification", Exact = true }).ClickAsync();
        ILocator picker = modal.Locator(".design-category-picker");
        await Assertions.Expect(picker.Locator(".design-category-field")).ToHaveCountAsync(1);
        Assert.Equal("DIV", await picker.Locator(".design-category-field").EvaluateAsync<string>("el => el.tagName"));
        await picker.Locator(".design-category-field").ClickAsync();
        await Assertions.Expect(picker.Locator(".design-category-popover")).ToBeVisibleAsync();
        await Assertions.Expect(picker.Locator("ul.design-category-tree-group").First).ToBeVisibleAsync();
        Assert.True(await picker.Locator("li.design-category-node").CountAsync() > 0);
        Assert.True(await picker.Locator("li.design-category-node .tree-toggle").CountAsync() > 0);
        ILocator firstExpander = picker.Locator("li.design-category-node .tree-toggle").First;
        await firstExpander.ClickAsync();
        await firstExpander.ClickAsync();
        ILocator nested = picker.Locator("ul.design-category-tree-group ul.design-category-tree-group .tree-node-select:not(.is-inactive)").First;
        string nestedCaption = (await nested.InnerTextAsync()).Trim();
        await nested.ClickAsync();
        await Assertions.Expect(picker.Locator(".design-category-popover")).ToBeHiddenAsync();
        Assert.Contains(" > ", await picker.Locator(".design-category-field").InnerTextAsync());
        await picker.Locator(".design-category-field").ClickAsync();
        await picker.Locator(".design-category-popover input").FillAsync(nestedCaption);
        await Assertions.Expect(picker.Locator(".design-category-popover .tree-node-select:not(.is-inactive)").Filter(new() { HasText = nestedCaption }).First).ToBeVisibleAsync();
        string geometry = await modal.EvaluateAsync<string>("el => JSON.stringify({modal:{cw:el.clientWidth,sw:el.scrollWidth,ch:el.clientHeight,sh:el.scrollHeight},body:(b=>({cw:b.clientWidth,sw:b.scrollWidth,ch:b.clientHeight,sh:b.scrollHeight,overflow:getComputedStyle(b).overflow,overflowY:getComputedStyle(b).overflowY,rect:b.getBoundingClientRect().toJSON()}))(el.querySelector('.dialog-body')),layout:(b=>({cw:b.clientWidth,sw:b.scrollWidth,ch:b.clientHeight,sh:b.scrollHeight,overflow:getComputedStyle(b).overflow,overflowY:getComputedStyle(b).overflowY,rect:b.getBoundingClientRect().toJSON()}))(el.querySelector('.design-editor-layout')),picker:(b=>({cw:b.clientWidth,sw:b.scrollWidth,ch:b.clientHeight,sh:b.scrollHeight,overflow:getComputedStyle(b).overflow,overflowY:getComputedStyle(b).overflowY,rect:b.getBoundingClientRect().toJSON()}))(el.querySelector('.design-category-popover'))})");
        Console.WriteLine($"DESIGN_LAYOUT_GEOMETRY {geometry}");
        Assert.Equal("visible", await modal.EvaluateAsync<string>("el => getComputedStyle(el.querySelector('.dialog-body')).overflowY"));
        Assert.False(await modal.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1 || el.querySelector('.dialog-body').scrollWidth > el.querySelector('.dialog-body').clientWidth + 1"));
        await Assertions.Expect(modal.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true })).ToBeVisibleAsync();
        Assert.False(await modal.EvaluateAsync<bool>("el => { const p=el.querySelector('.design-category-popover'); return p.scrollWidth > p.clientWidth + 1; }"));
        await picker.Locator(".design-category-popover .tree-node-select:not(.is-inactive)").Filter(new() { HasText = nestedCaption }).First.ClickAsync();
        modal = page.Locator(".barang-modal").Last;
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".crud-toolbar input").FillAsync(code);
        row = page.Locator(".data-grid tbody tr").Filter(new() { HasText = code });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        modal = page.Locator(".barang-modal").Last;
        await modal.GetByRole(AriaRole.Tab, new() { Name = "Classification", Exact = true }).ClickAsync();
        Assert.Contains(" > ", await modal.Locator(".design-category-field").InnerTextAsync());
        await modal.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await Go("fa", "/manage/design-bank");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
    }

    [Fact]
    public async Task DesignClassificationCatalogsAreSelectableAndPersisted()
    {
        string stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        string[] types = ["artwork.subcategory", "artwork.type", "artwork.style", "artwork.theme", "artwork.audience", "artwork.license", "artwork.usage"];
        Dictionary<string, string> captions = [];
        foreach (string type in types)
        {
            string code = $"E2E-DESIGN-{type[(type.LastIndexOf('.') + 1)..].ToUpperInvariant()}-{stamp}";
            string caption = $"Browser {type[(type.LastIndexOf('.') + 1)..]} {stamp}";
            await Go("en", $"/master/reference-data/artwork/{type}");
            ILocator grid = page.Locator($"[data-reference-type='{type}']");
            await Assertions.Expect(grid).ToBeVisibleAsync();
            await grid.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
            ILocator editor = page.Locator(".barang-modal").Last;
            await editor.Locator("[name=reference-code]").FillAsync(code);
            await editor.Locator("[name=reference-name-fa]").FillAsync($"فارسی {caption}");
            await editor.Locator("[name=reference-name-ar]").FillAsync($"عربي {caption}");
            await editor.Locator("[name=reference-name-en]").FillAsync(caption);
            await editor.Locator("footer .btn-primary").ClickAsync();
            await Assertions.Expect(editor).ToBeHiddenAsync();
            captions[type] = caption;
        }

        string designCode = $"E2E-DESIGN-REFERENCE-{stamp}";
        await Go("en", "/manage/design-bank");
        await page.Locator(".crud-header button.btn-primary").ClickAsync();
        ILocator modal = page.Locator(".barang-modal").Last;
        await modal.Locator("input").Nth(0).FillAsync(designCode);
        await modal.Locator("input").Nth(1).FillAsync("Browser reference integration");
        await modal.GetByRole(AriaRole.Tab, new() { Name = "Classification", Exact = true }).ClickAsync();
        foreach ((string type, string caption) in captions)
        {
            ILocator input = modal.Locator($"input[name='artwork-{type[(type.LastIndexOf('.') + 1)..]}']");
            await input.FillAsync(caption);
            await Assertions.Expect(modal.Locator("[role=option]").Filter(new() { HasText = caption }).First).ToBeVisibleAsync();
            await modal.Locator("[role=option]").Filter(new() { HasText = caption }).First.ClickAsync();
        }
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await page.Locator(".crud-toolbar input").FillAsync(designCode);
        ILocator row = page.Locator(".data-grid tbody tr").Filter(new() { HasText = designCode });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        modal = page.Locator(".barang-modal").Last;
        await modal.GetByRole(AriaRole.Tab, new() { Name = "Classification", Exact = true }).ClickAsync();
        foreach ((string type, string caption) in captions)
            await Assertions.Expect(modal.Locator($"input[name='artwork-{type[(type.LastIndexOf('.') + 1)..]}']")).ToHaveValueAsync(caption);
        await modal.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
    }

    private async Task Login()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Flogin", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company")
            await page.Locator($".company-list form:has(input[name=companyId][value='{Company}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(u => new Uri(u).AbsolutePath != "/company");
    }

    private Task<Microsoft.Playwright.IResponse?> Go(string culture, string path) => page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl={Uri.EscapeDataString(path)}", new() { WaitUntil = WaitUntilState.NetworkIdle });

    private static byte[] CreatePng(byte red, byte green, byte blue)
    {
        using MemoryStream output = new();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Chunk(output, "IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 2, 0, 0, 0]);
        using MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, CompressionMode.Compress, true)) zlib.Write([0, red, green, blue]);
        Chunk(output, "IDAT", compressed.ToArray());
        Chunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void Chunk(Stream output, string name, byte[] data)
    {
        WriteInt(output, data.Length);
        byte[] type = System.Text.Encoding.ASCII.GetBytes(name);
        output.Write(type); output.Write(data);
        byte[] crcInput = new byte[type.Length + data.Length]; type.CopyTo(crcInput, 0); data.CopyTo(crcInput, type.Length);
        WriteInt(output, Crc32(crcInput));
    }

    private static void WriteInt(Stream output, int value) => output.Write([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
    private static int Crc32(byte[] bytes)
    {
        uint crc = 0xffffffff;
        foreach (byte value in bytes) { crc ^= value; for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1)); }
        return unchecked((int)~crc);
    }
}
