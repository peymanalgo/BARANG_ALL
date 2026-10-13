using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class LayoutRegressionE2ETests : IAsyncLifetime
{
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        string? executable = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH");
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, ExecutablePath = executable });
        context = await browser.NewContextAsync(new BrowserNewContextOptions { Locale = "en-US", ViewportSize = new ViewportSize { Width = 1440, Height = 900 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        if (new Uri(page.Url).AbsolutePath.Equals("/company", StringComparison.OrdinalIgnoreCase))
        {
            await page.Locator(".company-list form").First.Locator("button[type=submit]").ClickAsync();
            await page.WaitForURLAsync(url => !new Uri(url).AbsolutePath.Equals("/company", StringComparison.OrdinalIgnoreCase));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Fact]
    public async Task DesktopGridToolbarsKeepSearchStatusAndRefreshOnOneRow()
    {
        await SetViewport(1440, 900);
        await Navigate("en", "/master/equipment");
        ILocator equipmentToolbar = page.Locator(".crud-toolbar");
        ILocator equipmentSearch = equipmentToolbar.Locator(".search-box");
        ILocator equipmentStatus = equipmentToolbar.Locator(":scope > select");
        ILocator equipmentRefresh = equipmentToolbar.Locator(":scope > button");
        await AssertSameRow(equipmentSearch, equipmentStatus, equipmentRefresh);

        Box searchBox = await Bounds(equipmentSearch);
        Box statusBox = await Bounds(equipmentStatus);
        Assert.True(searchBox.Width > statusBox.Width, "Equipment search must grow while the status selector remains compact.");
        Assert.InRange(statusBox.Width, 120, 220);

        await Navigate("en", "/master/persons");
        ILocator personsToolbar = page.Locator(".crud-toolbar");
        await AssertSameRow(personsToolbar.Locator(".search-box"), personsToolbar.Locator(":scope > button"));
    }

    [Fact]
    public async Task PersonAndGenericCrudModalsKeepFieldsInsideSafeInnerSpacing()
    {
        await SetViewport(1440, 900);
        await Navigate("en", "/master/persons");
        await page.Locator("header.crud-header button.btn-primary").ClickAsync();
        ILocator personDialog = page.Locator(".dialog-backdrop .crud-dialog");
        await Assertions.Expect(personDialog).ToBeVisibleAsync();
        await AssertModalFieldSpacing(personDialog);
        ILocator personFooter = personDialog.Locator(":scope > .dialog-body .crud-form > footer");
        await Assertions.Expect(personFooter).ToBeVisibleAsync();
        await AssertFooterDoesNotOverlapFields(personDialog, personFooter);
        await personFooter.Locator("button.btn-secondary").ClickAsync();

        await Navigate("en", "/master/equipment");
        await page.Locator("header.crud-header button.btn-primary").ClickAsync();
        ILocator equipmentDialog = page.Locator(".dialog-backdrop .crud-dialog");
        await Assertions.Expect(equipmentDialog).ToBeVisibleAsync();
        await AssertModalFieldSpacing(equipmentDialog);
        ILocator equipmentFooter = equipmentDialog.Locator(":scope > footer");
        await Assertions.Expect(equipmentFooter).ToBeVisibleAsync();
        await AssertFooterDoesNotOverlapFields(equipmentDialog, equipmentFooter);
    }

    [Fact]
    public async Task OrganizationalTreeDisclosureAndNestedNodesMirrorInFaArAndEn()
    {
        await Navigate("fa", "/master/organization-chart");
        var path = await ReadDisclosurePath();
        foreach ((string culture, string direction) in new[] { ("fa", "rtl"), ("ar", "rtl"), ("en", "ltr") })
        {
            await SetViewport(1440, 900);
            await Navigate(culture, "/master/organization-chart");
            Assert.Equal(direction, await page.Locator("html").GetAttributeAsync("dir"));

            ILocator chart = page.Locator("[data-chart='organizational-chart']");
            ILocator company = chart.Locator($"article.company-node[data-chart-node-id='{path.Company}']");
            ILocator branch = chart.Locator($"article.branch-node[data-chart-node-id='{path.Branch}']");
            ILocator unit = chart.Locator($"article.unit-node[data-chart-node-id='{path.Unit}']");
            ILocator person = chart.Locator($"article.person-node[data-chart-node-id='{path.Assignment}']");
            await company.WaitForAsync();
            ILocator companyExpander = company.Locator(":scope > button.org-expander");
            await EnsureCollapsed(companyExpander);
            await Assertions.Expect(branch).ToBeHiddenAsync();
            ILocator collapsedIcon = companyExpander.Locator("svg[data-icon='chevron-right']");
            await Assertions.Expect(collapsedIcon).ToBeVisibleAsync();
            string transform = await collapsedIcon.EvaluateAsync<string>("el => getComputedStyle(el).transform");
            if (direction == "rtl") Assert.Contains("-1", transform);
            else Assert.Equal("none", transform);
            await companyExpander.ClickAsync();
            await Assertions.Expect(companyExpander).ToHaveAttributeAsync("aria-expanded", "true");

            await branch.WaitForAsync();
            await EnsureExpanded(branch.Locator(":scope > button.org-expander"));
            await unit.WaitForAsync();
            ILocator unitExpander = unit.Locator(":scope > button.org-expander");
            await Assertions.Expect(unitExpander).ToBeVisibleAsync();
            await EnsureCollapsed(unitExpander);
            await Assertions.Expect(person).ToBeHiddenAsync();
            await EnsureExpanded(unitExpander);
            await Assertions.Expect(person).ToBeVisibleAsync();
            await Assertions.Expect(person).ToHaveAttributeAsync("data-chart-parent-node-id", path.Unit.ToString());

            await AssertDisclosureSide(company, direction);
            await AssertDisclosureSide(branch, direction);
            await AssertDisclosureSide(unit, direction);

            double companyX = await CenterX(company.Locator(":scope > .org-expander"));
            double branchX = await CenterX(branch.Locator(":scope > .org-expander"));
            double unitX = await CenterX(unit.Locator(":scope > .org-expander"));
            if (direction == "rtl")
            {
                Assert.True(companyX > branchX && branchX > unitX, $"RTL nesting must progress toward inline end: {companyX}, {branchX}, {unitX}.");
            }
            else
            {
                Assert.True(companyX < branchX && branchX < unitX, $"LTR nesting must progress toward inline end: {companyX}, {branchX}, {unitX}.");
            }

            await Assertions.Expect(person).ToBeVisibleAsync();
            await AssertDisclosureSide(person, direction);
        }
    }

    private async Task<(Guid Company, Guid Branch, Guid Unit, Guid Assignment)> ReadDisclosurePath()
    {
        // A leaf correctly has a span, not an expand button. Choose a populated
        // hierarchy from authoritative data, never by DOM order or by whether
        // the UI happens to render a button (which would mask a missing-button bug).
        ILocator companies = page.Locator("[data-chart='organizational-chart'] article.company-node");
        await companies.First.WaitForAsync();
        string[] visibleCompanyIds = await companies.EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.chartNodeId)");
        using HttpClient api = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL") ?? "http://localhost:8080") };
        using HttpResponseMessage login = await api.PostAsJsonAsync("api/v1/auth/login", new
        {
            username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME"),
            password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")
        }, TestContext.Current.CancellationToken);
        Assert.True(login.IsSuccessStatusCode, $"Disclosure fixture login failed: {login.StatusCode}");
        JsonElement session = await login.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        using HttpRequestMessage request = new(HttpMethod.Get, "api/v1/organization/chart");
        request.Headers.Add("X-Company-ID", visibleCompanyIds[0]);
        using HttpResponseMessage response = await api.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"Disclosure fixture chart read failed: {response.StatusCode}");
        JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        JsonElement[] assignments = data.GetProperty("assignments").EnumerateArray().ToArray();
        JsonElement[] branches = data.GetProperty("branches").EnumerateArray().ToArray();
        JsonElement[] candidates = data.GetProperty("units").EnumerateArray()
            .Where(unit => unit.GetProperty("parentId").ValueKind == JsonValueKind.Null
                && unit.GetProperty("branchId").ValueKind != JsonValueKind.Null
                && visibleCompanyIds.Contains(unit.GetProperty("companyId").GetString())
                && branches.Any(branch => branch.GetProperty("id").GetGuid() == unit.GetProperty("branchId").GetGuid()
                    && branch.GetProperty("companyId").GetGuid() == unit.GetProperty("companyId").GetGuid())
                && assignments.Any(assignment => assignment.GetProperty("organizationalUnitId").GetGuid() == unit.GetProperty("id").GetGuid()))
            .OrderBy(unit => unit.GetProperty("createdAtUtc").GetDateTimeOffset())
            .ThenBy(unit => unit.GetProperty("id").GetGuid())
            .ToArray();
        Assert.True(candidates.Length > 0, "The disclosure regression requires a Company > Branch > root Unit > Person hierarchy; none exists in authoritative chart data.");
        JsonElement selected = candidates[0];
        Guid unitId = selected.GetProperty("id").GetGuid();
        Guid assignmentId = assignments.First(assignment => assignment.GetProperty("organizationalUnitId").GetGuid() == unitId).GetProperty("id").GetGuid();
        return (selected.GetProperty("companyId").GetGuid(), selected.GetProperty("branchId").GetGuid(), unitId, assignmentId);
    }

    [Fact]
    public async Task HeaderControlsAndGridSearchFollowTheCleanupInAllDirections()
    {
        foreach ((string culture, string direction) in new[] { ("fa", "rtl"), ("ar", "rtl"), ("en", "ltr") })
        {
            await SetViewport(1440, 900);
            await Navigate(culture, "/master/persons");
            Assert.Equal(direction, await page.Locator("html").GetAttributeAsync("dir"));
            Assert.Equal(0, await page.Locator(".sidebar-collapse-toggle, .mobile-sidebar-toggle, .sidebar-expand-toggle, .mobile-nav-toggle").CountAsync());
            Assert.Equal(0, await page.Locator(".topbar-leading, .shell-location").CountAsync());
            await Assertions.Expect(page.Locator("header.crud-header h1")).ToBeVisibleAsync();

            Box topbar = await Bounds(page.Locator("header.topbar"));
            Box actions = await Bounds(page.Locator("header.topbar > .top-actions"));
            double inlineEndGap = direction == "rtl"
                ? actions.X - topbar.X
                : (topbar.X + topbar.Width) - (actions.X + actions.Width);
            Assert.InRange(inlineEndGap, 15, 25);

            ILocator search = page.Locator(".crud-toolbar .search-box").First;
            await Assertions.Expect(search).ToBeVisibleAsync();
            await Assertions.Expect(search.Locator(":scope > .barang-icon")).ToBeHiddenAsync();
            string paddingInlineStart = await search.Locator("input").EvaluateAsync<string>("el => getComputedStyle(el).paddingInlineStart");
            Assert.Equal("11px", paddingInlineStart);
        }

        await SetViewport(1440, 900);
        await Navigate("en", "/master/persons");
        ILocator personRows = page.Locator(".data-grid tbody tr");
        await Assertions.Expect(personRows.First).ToBeVisibleAsync();
        int initialRows = await personRows.CountAsync();
        ILocator personSearch = page.Locator(".crud-toolbar .search-box input");
        await personSearch.FillAsync("__barang_no_matching_person__");
        await personSearch.PressAsync("Enter");
        await Assertions.Expect(personRows).ToHaveCountAsync(0);
        await personSearch.FillAsync("");
        await personSearch.PressAsync("Enter");
        await Assertions.Expect(personRows).ToHaveCountAsync(initialRows);

        await SetViewport(390, 844);
        await Navigate("en", "/");
        ILocator mobileShell = page.Locator(".shell");
        ILocator mobileOpen = page.Locator("button.mobile-navigation-trigger");
        await Assertions.Expect(mobileOpen).ToBeVisibleAsync();
        Assert.Equal(0, await mobileOpen.Locator("svg").CountAsync());
        await mobileOpen.ClickAsync();
        await Assertions.Expect(mobileShell).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("navigation-open"));
        await Assertions.Expect(page.Locator("aside.sidebar")).ToBeInViewportAsync();
        await page.Locator("button.sidebar-scrim").ClickAsync(new LocatorClickOptions { Position = new Position { X = 380, Y = 420 } });
        await Assertions.Expect(mobileShell).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("navigation-open"));
        bool overflow = await page.Locator("body").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1");
        Assert.False(overflow, "The narrow shell introduced horizontal overflow.");
    }

    private async Task Navigate(string culture, string path)
    {
        string returnUrl = Uri.EscapeDataString(path);
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl={returnUrl}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SetViewport(int width, int height) => await page.SetViewportSizeAsync(width, height);

    private static async Task AssertSameRow(params ILocator[] controls)
    {
        Box[] boxes = new Box[controls.Length];
        for (int index = 0; index < controls.Length; index++)
        {
            await Assertions.Expect(controls[index]).ToBeVisibleAsync();
            boxes[index] = await Bounds(controls[index]);
        }

        double firstCenter = boxes[0].Y + boxes[0].Height / 2;
        Assert.All(boxes, box => Assert.InRange(Math.Abs((box.Y + box.Height / 2) - firstCenter), 0, 3));
    }

    private static async Task AssertModalFieldSpacing(ILocator dialog)
    {
        Box dialogBox = await Bounds(dialog);
        IReadOnlyList<ILocator> controls = await dialog.Locator("input, select, textarea").AllAsync();
        Assert.NotEmpty(controls);
        foreach (ILocator control in controls)
        {
            if (!await control.IsVisibleAsync()) continue;
            Box box = await Bounds(control);
            Assert.True(box.X - dialogBox.X >= 18, $"A modal control is only {box.X - dialogBox.X:0.##}px from the left edge.");
            Assert.True((dialogBox.X + dialogBox.Width) - (box.X + box.Width) >= 18, "A modal control is too close to the right edge.");
        }
    }

    private static async Task AssertFooterDoesNotOverlapFields(ILocator dialog, ILocator footer)
    {
        Box footerBox = await Bounds(footer);
        IReadOnlyList<ILocator> controls = await dialog.Locator("input, select, textarea").AllAsync();
        double lastFieldBottom = 0;
        foreach (ILocator control in controls)
        {
            if (!await control.IsVisibleAsync()) continue;
            Box box = await Bounds(control);
            lastFieldBottom = Math.Max(lastFieldBottom, box.Y + box.Height);
        }
        Assert.True(footerBox.Y >= lastFieldBottom, $"Footer starts at {footerBox.Y} while a field continues to {lastFieldBottom}.");
    }

    private static async Task EnsureCollapsed(ILocator expander)
    {
        if (string.Equals(await expander.GetAttributeAsync("aria-expanded"), "true", StringComparison.OrdinalIgnoreCase)) await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");
    }

    private static async Task EnsureExpanded(ILocator expander)
    {
        if (string.Equals(await expander.GetAttributeAsync("aria-expanded"), "false", StringComparison.OrdinalIgnoreCase)) await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");
    }

    private static async Task AssertDisclosureSide(ILocator node, string direction)
    {
        double expanderX = await CenterX(node.Locator(":scope > .org-expander"));
        double titleX = await CenterX(node.Locator(":scope > .org-title"));
        if (direction == "rtl") Assert.True(expanderX > titleX, $"RTL disclosure must be on the inline-start/right side: {expanderX} <= {titleX}.");
        else Assert.True(expanderX < titleX, $"LTR disclosure must be on the inline-start/left side: {expanderX} >= {titleX}.");
    }

    private static async Task<double> CenterX(ILocator locator)
    {
        Box box = await Bounds(locator);
        return box.X + box.Width / 2;
    }

    private static async Task<Box> Bounds(ILocator locator) => await locator.EvaluateAsync<Box>("el => { const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; }")
        ?? throw new Xunit.Sdk.XunitException("Element has no rendered bounds.");

    private sealed class Box
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
