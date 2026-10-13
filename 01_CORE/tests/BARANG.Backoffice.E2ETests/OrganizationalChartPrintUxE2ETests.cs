using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed partial class OrganizationalChartActionSearchPrintE2ETests
{
    [Theory]
    [InlineData("en", "ltr")]
    [InlineData("fa", "rtl")]
    [InlineData("ar", "rtl")]
    public async Task PrintUXIconsLargeSelectorsFullWidthSearchAndScopedHorizontalTree(string culture, string direction)
    {
        await page.SetViewportSizeAsync(1440, 1000);
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=/master/organization-chart", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
        await Assertions.Expect(Search).ToBeVisibleAsync();
        Assert.True(await Search.EvaluateAsync<bool>("el => el.getBoundingClientRect().width >= el.closest('.barang-toolbar').getBoundingClientRect().width * .95"), "Search must occupy at least 95% of the toolbar.");
        await ExpandFixtureAsync();
        ILocator manager = Node(reportId).Locator("[data-chart-action='assignment-change-manager']");
        ILocator move = Node(reportId).Locator("[data-chart-action='assignment-move-unit']");
        await Assertions.Expect(manager.Locator("svg")).ToHaveAttributeAsync("data-icon", "user-manager");
        await Assertions.Expect(move.Locator("svg")).ToHaveAttributeAsync("data-icon", "move");
        Assert.NotEqual(await manager.Locator("svg").InnerHTMLAsync(), await move.Locator("svg").InnerHTMLAsync());
        foreach (ILocator action in new[] { manager, move })
        {
            string title = (await action.GetAttributeAsync("title"))!;
            Assert.False(string.IsNullOrWhiteSpace(title));
            await Assertions.Expect(action).ToHaveAttributeAsync("aria-label", title);
            await Assertions.Expect(action).ToHaveClassAsync(new Regex("barang-action-info"));
            if (culture != "en") Assert.DoesNotMatch("[A-Za-z]", title);
        }

        await manager.ClickAsync();
        await AssertFocusedDialogAsync("change-manager", "assignment-manager");
        await Dialog.Locator("[name='assignment-manager']").FillAsync("");
        await AssertComfortableSelectorAsync("assignment-manager", desktop: true);
        await Dialog.Locator("footer .btn-secondary").ClickAsync();
        await move.ClickAsync();
        await AssertFocusedDialogAsync("move-unit", "assignment-unit");
        await Dialog.Locator("[name='assignment-unit']").FillAsync("");
        await AssertComfortableSelectorAsync("assignment-unit", desktop: true);
        await Dialog.Locator("footer .btn-secondary").ClickAsync();
        await page.Locator("[data-chart-action='print'] button").ClickAsync();
        await Dialog.Locator("[name='chart-print-company']").FillAsync("");
        await AssertComfortableSelectorAsync("chart-print-company", desktop: true);
        await SelectAsync("chart-print-company", companyCode);
        await Dialog.Locator("footer .btn-primary").ClickAsync();
        await page.WaitForURLAsync($"**/master/organization-chart/print/{companyId}");
        await AssertScopedPrintPayloadAndDomAsync();
        await Assertions.Expect(Chart).ToHaveAttributeAsync("data-print-layout", "horizontal");
        await AssertHorizontalGeometryAsync(direction);
        string[] colors = await Chart.Locator("[data-print-node-kind]").EvaluateAllAsync<string[]>("nodes => [...new Set(nodes.map(node => getComputedStyle(node).backgroundColor))]");
        Assert.Equal(5, colors.Length);
        await Assertions.Expect(Node(managerAId)).ToHaveAttributeAsync("data-print-node-kind", "manager");
        await Assertions.Expect(Node(childId)).ToHaveAttributeAsync("data-print-node-kind", "employee");
        Assert.True(await page.EvaluateAsync<bool>("[...document.querySelectorAll('.print-children')].every(el => parseFloat(getComputedStyle(el).borderLeftWidth) >= 2 && parseFloat(getComputedStyle(el, '::before').borderTopWidth) >= 2 && parseFloat(getComputedStyle(el, '::before').width) > 0)"), "Horizontal connector rails and edges must be drawn.");
        await Assertions.Expect(page.Locator(".sidebar,.topbar,nav,.breadcrumbs")).ToHaveCountAsync(0);
        string artifacts = Environment.GetEnvironmentVariable("BARANG_E2E_ARTIFACTS") ?? Path.Combine(Environment.CurrentDirectory, "TestResults", "ChartPrintUx");
        Directory.CreateDirectory(artifacts);
        await Node(branchId).Locator("..").ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"horizontal-tree-{culture}.png") });
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await Assertions.Expect(page.Locator(".chart-print-actions")).ToBeHiddenAsync();
        await AssertHorizontalGeometryAsync(direction);
        foreach (string paper in new[] { "A4", "A3" })
        {
            byte[] bytes = await page.PdfAsync(new() { Format = paper, Landscape = true, PrintBackground = true, DisplayHeaderFooter = false, Path = Path.Combine(artifacts, $"chart-{culture}-{paper}.pdf") });
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            Assert.True(bytes.Length > 1000);
        }
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.Locator("[data-chart-print-action='close'] button").ClickAsync();
        await page.WaitForURLAsync("**/master/organization-chart");
        await page.SetViewportSizeAsync(390, 844);
        await Assertions.Expect(Search).ToBeVisibleAsync();
        Assert.True(await Search.EvaluateAsync<bool>("el => el.getBoundingClientRect().width >= el.closest('.barang-toolbar').getBoundingClientRect().width * .95"));
        await ExpandFixtureAsync();
        foreach (var target in new[] { ("assignment-change-manager", "assignment-manager"), ("assignment-move-unit", "assignment-unit") })
        {
            await Node(reportId).Locator($"[data-chart-action='{target.Item1}']").ClickAsync();
            await Dialog.Locator($"[name='{target.Item2}']").FillAsync("");
            await AssertComfortableSelectorAsync(target.Item2, desktop: false);
            await Dialog.Locator("footer .btn-secondary").ClickAsync();
        }
        await page.Locator("[data-chart-action='print'] button").ClickAsync();
        await Dialog.Locator("[name='chart-print-company']").FillAsync("");
        await AssertComfortableSelectorAsync("chart-print-company", desktop: false);
        await Dialog.Locator("footer .btn-secondary").ClickAsync();
        AssertBrowserHealthy();
    }

    [Fact]
    public async Task PrintUXLargeDeepStructuresContinueWithoutOverlapOrMissingNodes()
    {
        List<Guid> created = [];
        Guid parent = sourceUnitId;
        for (int index = 0; index < 9; index++)
        {
            parent = await CreateAsync("api/v1/organization/organizational-units", "api/v1/organization/organizational-units", new { code = $"UXD{index}{suffix}", displayName = $"Deep unit {index} {suffix}", branchId, parentId = parent });
            created.Add(parent);
        }
        for (int index = 0; index < 12; index++)
            created.Add(await CreateAsync("api/v1/organization/organizational-units", "api/v1/organization/organizational-units", new { code = $"UXW{index}{suffix}", displayName = $"Wide unit {index} {suffix}", branchId }));
        await page.Locator("[data-chart-action='print'] button").ClickAsync();
        await SelectAsync("chart-print-company", companyCode);
        await Dialog.Locator("footer .btn-primary").ClickAsync();
        await page.WaitForURLAsync($"**/master/organization-chart/print/{companyId}");
        await Assertions.Expect(Chart).ToBeVisibleAsync();
        Assert.True(await Chart.Locator("[data-print-sheet]").CountAsync() > 1);
        foreach (Guid id in created) await Assertions.Expect(Node(id)).ToHaveCountAsync(1);
        await AssertScopedPrintPayloadAndDomAsync();
        await AssertHorizontalGeometryAsync("ltr");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await AssertHorizontalGeometryAsync("ltr");
        Assert.True(await page.EvaluateAsync<bool>("[...document.querySelectorAll('.print-tree-sheet')].slice(1).every(sheet => getComputedStyle(sheet).breakBefore === 'page')"));
        byte[] pdf = await page.PdfAsync(new() { Format = "A4", Landscape = true, PrintBackground = true, DisplayHeaderFooter = false });
        Assert.True(Regex.Count(Encoding.Latin1.GetString(pdf), @"/Type\s*/Page\b") > 1);
        AssertBrowserHealthy();
    }

    private async Task AssertComfortableSelectorAsync(string field, bool desktop)
    {
        await Assertions.Expect(Dialog).ToHaveClassAsync(new Regex("modal-large"));
        ILocator list = Dialog.Locator(".searchable-select-list");
        await Assertions.Expect(list).ToBeVisibleAsync();
        await Assertions.Expect(list.GetByRole(AriaRole.Option).First).ToBeVisibleAsync();
        JsonElement geometry = await Dialog.EvaluateAsync<JsonElement>("""
            el => {
                const body = el.querySelector('.dialog-body');
                const list = el.querySelector('.searchable-select-list');
                const rect = el.getBoundingClientRect(), bodyRect = body.getBoundingClientRect(), listRect = list.getBoundingClientRect();
                const footer = el.querySelector('footer').getBoundingClientRect();
                return { width: rect.width, height: rect.height, fits: rect.left >= 0 && rect.right <= innerWidth + 1 && rect.top >= 0 && rect.bottom <= innerHeight + 1,
                    bodyScroll: body.scrollHeight - body.clientHeight, bodyTop: body.scrollTop,
                    listFits: listRect.top >= bodyRect.top && listRect.bottom <= bodyRect.bottom + 1,
                    footerVisible: footer.top >= 0 && footer.bottom <= innerHeight + 1 };
            }
            """);
        Assert.True(geometry.GetProperty("fits").GetBoolean(), $"{field}: dialog must fit the viewport: {geometry}");
        Assert.True(geometry.GetProperty("listFits").GetBoolean(), $"{field}: dropdown must not be clipped by the body: {geometry}");
        Assert.True(geometry.GetProperty("footerVisible").GetBoolean(), $"{field}: footer must remain visible: {geometry}");
        Assert.InRange(geometry.GetProperty("bodyScroll").GetDouble(), 0, 1);
        Assert.Equal(0, geometry.GetProperty("bodyTop").GetDouble());
        if (desktop)
        {
            Assert.True(geometry.GetProperty("width").GetDouble() >= 820);
            Assert.True(geometry.GetProperty("height").GetDouble() >= 560);
        }
    }

    private async Task AssertScopedPrintPayloadAndDomAsync()
    {
        JsonElement scoped = await GetAsync($"api/v1/organization/chart/companies/{companyId}");
        Assert.Equal(1, scoped.GetProperty("companies").GetArrayLength());
        Assert.Equal(companyId, scoped.GetProperty("companies")[0].GetProperty("id").GetGuid());
        List<string> expected = [companyId.ToString()];
        foreach (string collection in new[] { "branches", "units", "assignments" })
            foreach (JsonElement row in scoped.GetProperty(collection).EnumerateArray())
            {
                Assert.Equal(companyId, row.GetProperty("companyId").GetGuid());
                expected.Add(row.GetProperty("id").GetGuid().ToString());
            }
        await Assertions.Expect(Chart.Locator("article")).ToHaveCountAsync(expected.Count);
        Assert.Equal(expected.Order().ToArray(), await VisibleIdsAsync());
        await Assertions.Expect(Chart.Locator($"article:not([data-chart-company-id='{companyId}'])")).ToHaveCountAsync(0);
        await Assertions.Expect(Node(branchId)).ToBeVisibleAsync();
        await Assertions.Expect(Node(sourceUnitId)).ToBeVisibleAsync();
        await Assertions.Expect(Node(reportId)).ToBeVisibleAsync();
        JsonElement full = await GetAsync("api/v1/organization/chart");
        Guid foreignCompany = full.GetProperty("companies").EnumerateArray().First(row => row.GetProperty("id").GetGuid() != companyId).GetProperty("id").GetGuid();
        await Assertions.Expect(Node(foreignCompany)).ToHaveCountAsync(0);
        await Assertions.Expect(Chart.Locator($"[data-chart-company-id='{foreignCompany}']")).ToHaveCountAsync(0);
        using HttpRequestMessage mismatch = Request(HttpMethod.Get, $"api/v1/organization/chart/companies/{foreignCompany}");
        using HttpResponseMessage rejected = await api.SendAsync(mismatch);
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
    }

    private async Task AssertHorizontalGeometryAsync(string textDirection)
    {
        string[] issues = await Chart.EvaluateAsync<string[]>("""
            root => {
                const nodes = [...root.querySelectorAll('article[data-chart-node]')];
                const issues = [];
                for (let i = 0; i < nodes.length; i++) {
                    const a = nodes[i], ar = a.getBoundingClientRect();
                    for (let j = i + 1; j < nodes.length; j++) {
                        const b = nodes[j], br = b.getBoundingClientRect();
                        if (Math.min(ar.right, br.right) - Math.max(ar.left, br.left) > 1 && Math.min(ar.bottom, br.bottom) - Math.max(ar.top, br.top) > 1)
                            issues.push('Overlap: ' + a.dataset.chartNodeId + ' / ' + b.dataset.chartNodeId);
                    }
                    const parent = nodes.find(b => b.dataset.chartNodeId === a.dataset.printParentId);
                    if (parent && parent.closest('.print-tree-sheet') === a.closest('.print-tree-sheet') && ar.left < parent.getBoundingClientRect().right + 1)
                        issues.push('Child is not right of parent: ' + a.dataset.chartNodeId);
                }
                return issues;
            }
            """);
        Assert.Empty(issues);
        await Assertions.Expect(Chart.Locator($"article:not([dir='{textDirection}'])")).ToHaveCountAsync(0);
        Assert.True(await Chart.EvaluateAsync<bool>("(root, direction) => [...root.querySelectorAll('article')].every(node => getComputedStyle(node).direction === direction)", textDirection), "Printed card text must use the actual locale direction inside the LTR hierarchy.");
    }
}
