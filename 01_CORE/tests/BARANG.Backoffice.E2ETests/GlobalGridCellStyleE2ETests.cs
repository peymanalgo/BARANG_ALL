using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class GlobalGridCellStyleE2ETests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new() { WriteIndented = true };
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly string artifacts = Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACTS") ?? Path.Combine(AppContext.BaseDirectory, "artifacts", "global-grid-cell-style");
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private BrowserDiagnostics diagnostics = null!;

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        diagnostics = new(page);
        diagnostics.Attach();
        Directory.CreateDirectory(artifacts);
        await Navigate("en", "/login");
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company")
        {
            await page.Locator(".company-list form button[type=submit]").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Theory]
    [InlineData("persons", "en")]
    [InlineData("persons", "fa")]
    [InlineData("persons", "ar")]
    [InlineData("customers", "en")]
    [InlineData("customers", "fa")]
    [InlineData("customers", "ar")]
    [InlineData("users", "en")]
    [InlineData("users", "fa")]
    [InlineData("users", "ar")]
    [InlineData("products", "en")]
    [InlineData("products", "fa")]
    [InlineData("products", "ar")]
    public async Task OrdinaryCellsAreUnframedAndInteractionsAndSemanticsSurvive(string grid, string culture)
    {
        await Navigate(culture, Route(grid));
        await AssertLanguage(culture);
        ILocator table = page.Locator("table.data-grid").First;
        await Assertions.Expect(table.Locator("tbody tr").First).ToBeVisibleAsync();
        await AssertOrdinaryCells(table);
        await AssertSemanticElements(table);

        ILocator primary = table.Locator(grid == "users" ? "tbody tr:first-child td:first-child strong" : "tbody tr:first-child td:nth-child(2)").First;
        Assert.False(string.IsNullOrWhiteSpace(await primary.InnerTextAsync()), "A populated primary cell must be validated.");
        if (grid is "persons" or "customers")
        {
            ILocator link = primary.Locator("button");
            Assert.True(await link.EvaluateAsync<bool>("el => getComputedStyle(el).color === getComputedStyle(el.closest('td')).color && getComputedStyle(el).fontSize === getComputedStyle(el.closest('td')).fontSize"), "Entity links must inherit ordinary cell color and font size.");
            await link.HoverAsync();
            await AssertOrdinaryCells(table);
            Assert.Contains("underline", await link.EvaluateAsync<string>("el => getComputedStyle(el).textDecorationLine"));
            await page.Mouse.MoveAsync(0, 0);
            await page.Keyboard.PressAsync("Tab");
            await link.FocusAsync();
            Assert.True(await link.EvaluateAsync<bool>("el => el.matches(':focus-visible') && getComputedStyle(el).outlineStyle !== 'none' && parseFloat(getComputedStyle(el).outlineWidth) >= 2"), "Keyboard focus must remain visible on plain entity links.");
            await page.Keyboard.PressAsync("Enter");
        }
        else
        {
            await table.Locator("tbody .barang-icon-button:has(svg[data-icon='eye'])").First.ClickAsync();
        }
        ILocator dialog = page.Locator(".dialog-backdrop .crud-dialog");
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog.Locator("input").First).ToBeVisibleAsync();
        await dialog.Locator("footer button.btn-secondary").Last.ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();

        ILocator sort = table.Locator(".barang-sort-header").First;
        string previousSort = await sort.Locator("svg").GetAttributeAsync("data-icon") ?? "";
        await sort.ClickAsync();
        await Assertions.Expect(sort).ToHaveClassAsync(new Regex("active"));
        await Assertions.Expect(sort.Locator("svg")).Not.ToHaveAttributeAsync("data-icon", previousSort);
        await Assertions.Expect(sort).ToHaveCSSAsync("border-top-width", "0px");

        ILocator rows = table.Locator("tbody tr");
        int rowCount = await rows.CountAsync();
        ILocator search = page.Locator(".search-box input").First;
        await search.FillAsync("__barang_grid_style_no_match_7e2c6f__");
        await search.PressAsync("Enter");
        await Assertions.Expect(page.Locator("table.data-grid tbody tr")).ToHaveCountAsync(0);
        await search.FillAsync("");
        await search.PressAsync("Enter");
        await Assertions.Expect(rows).ToHaveCountAsync(rowCount);
        await AssertOrdinaryCells(table);
        await page.Mouse.MoveAsync(0, 0);
        await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{grid}-{culture}-desktop.png"), FullPage = true });

        if (grid == "users")
        {
            await page.Locator(".uam-tabs button").Nth(1).ClickAsync();
            await Assertions.Expect(page.Locator(".uam-scope").First).ToBeVisibleAsync();
            await AssertOrdinaryCells(page.Locator("table.data-grid"));
            await AssertSemanticElements(page.Locator("table.data-grid"));
            await Assertions.Expect(page.Locator(".uam-scope").First).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
            Assert.True(await page.Locator(".uam-scope").First.EvaluateAsync<bool>("el => parseFloat(getComputedStyle(el).borderTopLeftRadius) > 0"), "Business scope chips must retain their semantic styling.");
        }
        AssertNoBrowserErrors();
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("ar")]
    public async Task NarrowGridsKeepPlainCellsAndReachableSemanticActions(string culture)
    {
        await page.SetViewportSizeAsync(390, 844);
        foreach (string grid in new[] { "persons", "customers", "users", "products" })
        {
            await Navigate(culture, Route(grid));
            await AssertLanguage(culture);
            ILocator table = page.Locator("table.data-grid").First;
            await Assertions.Expect(table.Locator("tbody tr").First).ToBeVisibleAsync();
            await AssertOrdinaryCells(table);
            Assert.False(await page.Locator("body").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1"), $"{grid}/{culture} overflows the mobile page.");
            ILocator action = table.Locator("tbody .barang-icon-button:has(svg[data-icon='eye'])").First;
            await action.ScrollIntoViewIfNeededAsync();
            await Assertions.Expect(action).ToBeInViewportAsync();
            await AssertSemanticElements(table);
            await action.ClickAsync();
            ILocator dialog = page.Locator(".dialog-backdrop .crud-dialog");
            await Assertions.Expect(dialog).ToBeVisibleAsync();
            await dialog.Locator("footer button.btn-secondary").Last.ClickAsync();
            await Assertions.Expect(dialog).ToBeHiddenAsync();
            await table.Locator("tbody td").First.ScrollIntoViewIfNeededAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{grid}-{culture}-mobile.png"), FullPage = true });
        }
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task AllPermissionVisibleGridRoutesFollowTheSharedOrdinaryCellContract()
    {
        await Navigate("en", "/");
        string[] routes = await page.Locator(".sidebar-navigation a[href]").EvaluateAllAsync<string[]>("links => [...new Set(links.map(link => link.getAttribute('href')).filter(href => href && href !== '/'))]");
        Assert.True(routes.Length >= 25, "The global audit must cover the full permission-visible navigation.");
        List<object> audited = [];
        foreach (string route in routes)
        {
            await Navigate("en", route);
            await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
            foreach (ILocator table in await page.Locator("table:has(tbody td)").AllAsync())
            {
                int count = await AssertOrdinaryCells(table);
                audited.Add(new { route, ordinaryElements = count });
            }
        }
        Assert.True(audited.Count >= 6, $"Only {audited.Count} populated grids were audited.");
        await File.WriteAllTextAsync(Path.Combine(artifacts, "audited-grid-routes.json"), JsonSerializer.Serialize(audited, ArtifactJsonOptions), TestContext.Current.CancellationToken);
        AssertNoBrowserErrors();
    }

    private static async Task<int> AssertOrdinaryCells(ILocator table)
    {
        string[] violations = await table.EvaluateAsync<string[]>("""
            table => {
                const semantic = '.status-badge, .customer-status, .status-dot, .uam-scope, .uam-chip, .row-actions, .grid-actions, .barang-icon-button, .icon-action, .searchable-select, .locale-date-input';
                return [...table.querySelectorAll('tbody td :is(span, strong, small, code, a, button, div)')]
                    .filter(el => !el.closest(semantic) && el.textContent.trim())
                    .flatMap(el => {
                        const s = getComputedStyle(el);
                        const framed = ['borderTopWidth','borderRightWidth','borderBottomWidth','borderLeftWidth'].some(p => parseFloat(s[p]) > 0)
                            || ['borderTopLeftRadius','borderTopRightRadius','borderBottomLeftRadius','borderBottomRightRadius'].some(p => parseFloat(s[p]) > 0)
                            || s.boxShadow !== 'none' || s.backgroundImage !== 'none'
                            || !['rgba(0, 0, 0, 0)', 'transparent'].includes(s.backgroundColor);
                        return framed ? [el.outerHTML.slice(0, 180) + ' | ' + JSON.stringify({border:s.border, radius:s.borderRadius, background:s.backgroundColor, shadow:s.boxShadow})] : [];
                    });
            }
            """);
        Assert.True(violations.Length == 0, "Ordinary cells must not have decorative frames: " + string.Join("; ", violations));
        return await table.Locator("tbody td").CountAsync();
    }

    private static async Task AssertSemanticElements(ILocator table)
    {
        ILocator badge = table.Locator(".status-badge, .customer-status").First;
        await Assertions.Expect(badge).ToBeVisibleAsync();
        Assert.True(await badge.EvaluateAsync<bool>("el => getComputedStyle(el).backgroundColor !== 'rgba(0, 0, 0, 0)' && parseFloat(getComputedStyle(el).borderTopLeftRadius) > 0"), "Status badges must remain visibly semantic.");
        foreach (ILocator action in await table.Locator("tbody tr:first-child .barang-icon-button").AllAsync())
        {
            string? title = await action.GetAttributeAsync("title");
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.Equal(title, await action.GetAttributeAsync("aria-label"));
            Assert.True(await action.EvaluateAsync<bool>("el => { const s=getComputedStyle(el),r=el.getBoundingClientRect(); return parseFloat(s.borderTopWidth) === 1 && parseFloat(s.borderTopLeftRadius)>0 && r.width>=34 && r.width<=38 && r.height>=34 && r.height<=38; }"), "Semantic actions must retain their shared border, radius and 34-38px target.");
        }
    }

    private Task<Microsoft.Playwright.IResponse?> Navigate(string culture, string route) => page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl={Uri.EscapeDataString(route)}", new() { WaitUntil = WaitUntilState.NetworkIdle });
    private async Task AssertLanguage(string culture)
    {
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", culture);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", culture == "en" ? "ltr" : "rtl");
    }
    private void AssertNoBrowserErrors()
    {
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("pageerror:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("requestfailed:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("response:", StringComparison.OrdinalIgnoreCase));
    }
    private static string Route(string grid) => grid switch
    {
        "persons" => "/master/persons",
        "customers" => "/manage/customers",
        "users" => "/admin/user-access",
        "products" => "/manage/products",
        _ => throw new ArgumentOutOfRangeException(nameof(grid))
    };
}
