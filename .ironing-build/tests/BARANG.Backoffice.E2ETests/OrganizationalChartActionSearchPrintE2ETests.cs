using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed partial class OrganizationalChartActionSearchPrintE2ETests : IAsyncLifetime
{
    private static readonly string[] BrowserCandidates = [@"C:\Program Files\Google\Chrome\Application\chrome.exe", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", "/usr/bin/google-chrome", "/usr/bin/chromium"];
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private BrowserDiagnostics diagnostics = null!;
    private readonly HttpClient api = new();
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly string suffix = Guid.NewGuid().ToString("N")[..10];
    private readonly List<(string Route, Guid Id, long Version, Guid Company)> cleanup = [];
    private Guid companyId, branchId, sourceUnitId, targetUnitId, managerAId, managerBId, reportId, childId;
    private string companyName = "", companyCode = "";
    private string SourceCode => $"ASPS{suffix}";
    private string TargetCode => $"ASPT{suffix}";
    private string BranchCode => $"ASPB{suffix}";
    private string Position(string name) => $"Chart {name} {suffix}";
    private ILocator Chart => page.Locator("[data-chart='organizational-chart']");
    private ILocator Dialog => page.GetByRole(AriaRole.Dialog);
    private ILocator Search => page.Locator("[name='chart-search']");
    private ILocator Node(Guid id) => Chart.Locator($"article[data-chart-node-id='{id}']");

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        playwright = await Playwright.CreateAsync();
        string? executable = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? BrowserCandidates.FirstOrDefault(File.Exists);
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = executable, Channel = executable is null ? "chrome" : null });
        context = await browser.NewContextAsync(new() { Locale = "en-US" });
        page = await context.NewPageAsync();
        diagnostics = new(page);
        diagnostics.Attach();
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.GotoAsync($"{baseUrl}/master/organization-chart", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(Chart).ToBeVisibleAsync();
        companyName = (await page.Locator(".company-chip strong").InnerTextAsync()).Trim();
        ILocator current = Chart.Locator("article[data-chart-node='company']").Filter(new() { HasTextString = companyName });
        companyId = Guid.Parse((await current.GetAttributeAsync("data-chart-node-id"))!);
        companyCode = (await current.Locator(".org-title small").InnerTextAsync()).Trim();

        api.BaseAddress = new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL") ?? "http://localhost:8080");
        using HttpResponseMessage login = await api.PostAsJsonAsync("api/v1/auth/login", new { username, password });
        Assert.True(login.IsSuccessStatusCode, $"Fixture login failed: {login.StatusCode}");
        JsonElement loginData = await login.Content.ReadFromJsonAsync<JsonElement>();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData.GetProperty("accessToken").GetString());
        branchId = await CreateAsync($"api/v1/organization/branches/company/{companyId}", "api/v1/organization/branches", new { code = BranchCode, displayName = Position("Branch") });
        sourceUnitId = await CreateAsync("api/v1/organization/organizational-units", "api/v1/organization/organizational-units", new { code = SourceCode, displayName = Position("Source"), branchId });
        targetUnitId = await CreateAsync("api/v1/organization/organizational-units", "api/v1/organization/organizational-units", new { code = TargetCode, displayName = Position("Target"), branchId });
        Guid personA = await CreatePersonAsync("ManagerA");
        Guid personB = await CreatePersonAsync("ManagerB");
        Guid personC = await CreatePersonAsync("Report");
        Guid personD = await CreatePersonAsync("Child");
        managerAId = await CreateAssignmentAsync(personA, sourceUnitId, null, "ManagerA");
        managerBId = await CreateAssignmentAsync(personB, targetUnitId, null, "ManagerB");
        reportId = await CreateAssignmentAsync(personC, sourceUnitId, managerAId, "Report");
        childId = await CreateAssignmentAsync(personD, sourceUnitId, reportId, "Child");
        await ReloadAsync();
    }

    public async ValueTask DisposeAsync()
    {
        diagnostics?.Dump("Organizational Chart action/search/print diagnostics");
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
        foreach (var item in cleanup.AsEnumerable().Reverse())
        {
            long version = item.Version;
            if (item.Route.EndsWith("/assignments", StringComparison.Ordinal))
            {
                JsonElement chart = await GetAsync("api/v1/organization/chart", item.Company);
                version = chart.GetProperty("assignments").EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == item.Id).GetProperty("version").GetInt64();
            }
            using HttpRequestMessage request = Request(HttpMethod.Delete, $"{item.Route}/{item.Id}?version={version}", item.Company);
            using HttpResponseMessage response = await api.SendAsync(request);
            Assert.True(response.IsSuccessStatusCode, $"Fixture cleanup failed: {item.Route} {response.StatusCode}");
        }
        api.Dispose();
    }

    [Fact]
    public async Task EditManagerAndMoveHaveDistinctFieldsAndPersistOnlyTheIntendedChange()
    {
        await ExpandFixtureAsync();
        JsonElement original = await AssignmentAsync(reportId);
        await Node(reportId).Locator("[data-chart-action='assignment-edit']").ClickAsync();
        await Assertions.Expect(Dialog.Locator("[data-chart-form='assignment']")).ToBeVisibleAsync();
        foreach (string name in new[] { "assignment-unit", "assignment-manager", "assignment-position-title", "assignment-position-code", "assignment-start-date", "assignment-end-date", "assignment-notes" })
            await Assertions.Expect(Dialog.Locator($"[name='{name}']")).ToBeEditableAsync();
        await Dialog.Locator(".dialog-close").ClickAsync();

        await Node(reportId).Locator("[data-chart-action='assignment-change-manager']").ClickAsync();
        await AssertFocusedDialogAsync("change-manager", "assignment-manager");
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("aria-label", "Change Manager");
        await Dialog.Locator("[name='assignment-manager']").FillAsync(Position("Report"));
        await Assertions.Expect(Dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        await Dialog.Locator("[name='assignment-manager']").FillAsync(Position("Child"));
        await Assertions.Expect(Dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        await SelectAsync("assignment-manager", Position("ManagerB"));
        await SaveAsync();
        await Assertions.Expect(Node(reportId)).ToHaveAttributeAsync("data-manager-assignment-id", managerBId.ToString());
        await Assertions.Expect(Node(companyId).Locator(".org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Node(branchId).Locator(".org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        JsonElement afterManager = await AssignmentAsync(reportId);
        AssertOnlyChanged(original, afterManager, "managerAssignmentId");
        Assert.Equal(managerBId, afterManager.GetProperty("managerAssignmentId").GetGuid());

        await Node(reportId).Locator("[data-chart-action='assignment-move-unit']").ClickAsync();
        await AssertFocusedDialogAsync("move-unit", "assignment-unit");
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("aria-label", "Move to Organizational Unit");
        await SelectAsync("assignment-unit", TargetCode);
        await SaveAsync();
        await Assertions.Expect(Node(reportId)).ToHaveAttributeAsync("data-chart-parent-node-id", targetUnitId.ToString());
        await Assertions.Expect(Node(reportId)).ToHaveAttributeAsync("data-manager-assignment-id", managerBId.ToString());
        await Assertions.Expect(Node(targetUnitId).Locator(".org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        JsonElement afterMove = await AssignmentAsync(reportId);
        AssertOnlyChanged(afterManager, afterMove, "organizationalUnitId");
        Assert.Equal(targetUnitId, afterMove.GetProperty("organizationalUnitId").GetGuid());
        await ReloadAsync();
        await ExpandFixtureAsync();
        await Assertions.Expect(Node(reportId)).ToHaveAttributeAsync("data-chart-parent-node-id", targetUnitId.ToString());
        await Assertions.Expect(Node(reportId)).ToHaveAttributeAsync("data-manager-assignment-id", managerBId.ToString());
        AssertOnlyChanged(afterMove, await AssignmentAsync(reportId));
        AssertBrowserHealthy();
    }

    [Theory]
    [InlineData("en", "ltr", "Search company, branch, unit or person", "Print Organizational Chart", "Select Company", "Print", "Save as PDF", "Move to Organizational Unit")]
    [InlineData("fa", "rtl", "جستجوی شرکت، شعبه، واحد یا شخص", "چاپ چارت سازمانی", "انتخاب شرکت", "چاپ", "ذخیره به صورت PDF", "انتقال به واحد سازمانی")]
    [InlineData("ar", "rtl", "البحث عن شركة أو فرع أو وحدة أو شخص", "طباعة الهيكل التنظيمي", "اختيار الشركة", "طباعة", "حفظ بصيغة PDF", "نقل إلى وحدة تنظيمية")]
    public async Task SearchFocusedDialogsAndGraphicalPrintWorkInEachLanguage(string culture, string direction, string searchLabel, string printTitle, string companyLabel, string printLabel, string pdfLabel, string moveTitle)
    {
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=/master/organization-chart", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", culture);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
        await Assertions.Expect(Search).ToHaveAttributeAsync("placeholder", searchLabel);
        await ExpandAsync(companyId);
        await ExpandAsync(branchId);
        await ExpandAsync(sourceUnitId);
        string[] before = await VisibleIdsAsync();
        await page.EvaluateAsync("window.chartSearchDocument = 'unchanged'");
        int documentNavigations = 0;
        page.FrameNavigated += (_, frame) => { if (frame == page.MainFrame) documentNavigations++; };
        foreach (var match in new[] { (companyCode, companyId), (BranchCode, branchId), (SourceCode, sourceUnitId), (Position("Report"), reportId) })
        {
            await Search.FillAsync(match.Item1);
            await Assertions.Expect(Node(match.Item2)).ToHaveAttributeAsync("data-chart-search-match", "true");
            await Assertions.Expect(Node(match.Item2)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("search-match"));
            await Assertions.Expect(Node(targetUnitId)).ToHaveCountAsync(0);
        }
        await Assertions.Expect(Node(companyId).Locator(".org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Node(branchId).Locator(".org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(Node(sourceUnitId).Locator(".org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        await Search.FillAsync($"missing-{suffix}");
        await Assertions.Expect(page.Locator("[data-chart-search-empty]")).ToBeVisibleAsync();
        await Assertions.Expect(Chart.Locator("article")).ToHaveCountAsync(0);
        await Search.FillAsync("");
        await Assertions.Expect(Node(reportId)).ToBeVisibleAsync();
        Assert.Equal(before, await VisibleIdsAsync());
        Assert.Equal(0, documentNavigations);
        Assert.Equal("unchanged", await page.EvaluateAsync<string>("window.chartSearchDocument"));

        ILocator managerAction = Node(reportId).Locator("[data-chart-action='assignment-change-manager']");
        string managerTitle = (await managerAction.GetAttributeAsync("title"))!;
        await managerAction.ClickAsync();
        await AssertFocusedDialogAsync("change-manager", "assignment-manager");
        await Assertions.Expect(Dialog).ToHaveAccessibleNameAsync(managerTitle);
        if (culture != "en") Assert.DoesNotContain("Change", managerTitle, StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Dialog).ToBeHiddenAsync();
        await Node(reportId).Locator("[data-chart-action='assignment-move-unit']").ClickAsync();
        await AssertFocusedDialogAsync("move-unit", "assignment-unit");
        await Assertions.Expect(Dialog).ToHaveAccessibleNameAsync(moveTitle);
        await Dialog.Locator("footer .btn-secondary").ClickAsync();
        await Assertions.Expect(Dialog).ToBeHiddenAsync();

        await page.Locator("[data-chart-action='print'] button").ClickAsync();
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("data-chart-workflow", "print-company");
        await Assertions.Expect(Dialog).ToHaveAccessibleNameAsync(printTitle);
        await Assertions.Expect(Dialog.GetByLabel(companyLabel, new() { Exact = false })).ToBeEditableAsync();
        await Assertions.Expect(Dialog.Locator("footer .btn-primary")).ToBeDisabledAsync();
        await SelectAsync("chart-print-company", companyCode);
        await Dialog.Locator("footer .btn-primary").ClickAsync();
        await page.WaitForURLAsync($"**/master/organization-chart/print/{companyId}");
        await AssertPreviewAsync(companyId, companyName);
        await Assertions.Expect(Node(reportId)).ToBeVisibleAsync();
        await Assertions.Expect(Node(childId).Locator("xpath=ancestor::div[contains(@class,'org-assignment-children')]").First).ToBeVisibleAsync();
        JsonElement authoritative = await GetAsync("api/v1/organization/chart");
        foreach (string collection in new[] { "branches", "units", "assignments" })
        {
            foreach (JsonElement row in authoritative.GetProperty(collection).EnumerateArray())
                if (row.GetProperty("companyId").GetGuid() == companyId)
                    await Assertions.Expect(Node(row.GetProperty("id").GetGuid())).ToBeVisibleAsync();
        }
        await Assertions.Expect(page.Locator("[data-chart-print-action='print'] button")).ToHaveTextAsync(printLabel);
        await Assertions.Expect(page.Locator("[data-chart-print-action='pdf'] button")).ToHaveTextAsync(pdfLabel);
        await Assertions.Expect(page.Locator(".chart-print-heading time")).Not.ToHaveTextAsync("");
        await page.EvaluateAsync("window.printCalls = 0; window.print = () => { window.printCalls++; }");
        await page.Locator("[data-chart-print-action='print'] button").ClickAsync();
        await Assertions.Expect(page.Locator("[data-chart-print-action='pdf'] button")).ToBeEnabledAsync();
        await page.Locator("[data-chart-print-action='pdf'] button").ClickAsync();
        await page.WaitForFunctionAsync("window.printCalls === 2");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await Assertions.Expect(page.Locator(".chart-print-actions")).ToBeHiddenAsync();
        await Assertions.Expect(Chart.Locator("button")).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("getComputedStyle(document.querySelector('.org-assignment-branch')).breakInside === 'avoid'"));
        foreach (string size in new[] { "A4", "A3" })
        {
            byte[] pdf = await page.PdfAsync(new() { Format = size, PrintBackground = true, DisplayHeaderFooter = false });
            Assert.True(pdf.Length > 1000);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        }
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.SetViewportSizeAsync(390, 844);
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"), "Print preview must fit a mobile viewport.");
        await page.Locator("[data-chart-print-action='close'] button").ClickAsync();
        await page.WaitForURLAsync("**/master/organization-chart");
        AssertBrowserHealthy();
    }

    [Fact]
    public async Task PrintReloadsTheSelectedCompanyScopeWithoutChangingTheActiveCompany()
    {
        JsonElement authorized = await GetAsync("api/v1/auth/me/companies");
        HashSet<Guid> allowedIds = authorized.EnumerateArray().Select(row => row.GetProperty("companyId").GetGuid()).ToHashSet();
        JsonElement projection = await GetAsync("api/v1/organization/chart");
        JsonElement other = projection.GetProperty("companies").EnumerateArray().First(row => !allowedIds.Contains(row.GetProperty("id").GetGuid()));
        Guid otherId = other.GetProperty("id").GetGuid();
        string otherCode = other.GetProperty("code").GetString()!;
        Guid freshUnit = await CreateAsync("api/v1/organization/organizational-units", "api/v1/organization/organizational-units", new { code = $"ASPF{suffix}", displayName = Position("FreshUnit"), branchId });
        await Assertions.Expect(Node(freshUnit)).ToHaveCountAsync(0);
        await page.Locator("[data-chart-action='print'] button").ClickAsync();
        await Dialog.Locator("[name='chart-print-company']").FillAsync(otherCode);
        await Assertions.Expect(Dialog.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        await SelectAsync("chart-print-company", companyCode);
        await Dialog.Locator("footer .btn-primary").ClickAsync();
        await page.WaitForURLAsync($"**/master/organization-chart/print/{companyId}");
        await AssertPreviewAsync(companyId, companyName);
        await Assertions.Expect(Node(freshUnit)).ToBeVisibleAsync();
        await Assertions.Expect(Node(branchId)).ToBeVisibleAsync();
        await Assertions.Expect(Node(reportId)).ToBeVisibleAsync();
        await Assertions.Expect(Node(otherId)).ToHaveCountAsync(0);
        await page.Locator("[data-chart-print-action='close'] button").ClickAsync();
        await page.WaitForURLAsync("**/master/organization-chart");
        await Assertions.Expect(page.Locator(".company-chip strong")).ToHaveTextAsync(companyName);
        await page.GotoAsync($"{baseUrl}/master/organization-chart/print/{otherId}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
        await Assertions.Expect(Chart).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("[data-chart-print-action='print'],[data-chart-print-action='pdf']")).ToHaveCountAsync(0);
        AssertBrowserHealthy();
    }

    [Fact]
    public async Task UnavailableCompanyPrintRouteShowsErrorWithoutChartOrPrintControls()
    {
        await page.GotoAsync($"{baseUrl}/master/organization-chart/print/{Guid.NewGuid()}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("The company chart could not be loaded. Check your company access and try again.");
        await Assertions.Expect(Chart).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("[data-chart-print-action='print'],[data-chart-print-action='pdf']")).ToHaveCountAsync(0);
        AssertBrowserHealthy();
    }

    private async Task AssertFocusedDialogAsync(string workflow, string field)
    {
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("data-chart-workflow", workflow);
        await Assertions.Expect(Dialog.Locator("input:not([disabled]),select:not([disabled]),textarea:not([disabled])")).ToHaveCountAsync(1);
        await Assertions.Expect(Dialog.Locator($"[name='{field}']")).ToHaveAttributeAsync("role", "combobox");
        await Assertions.Expect(Dialog.Locator($"[name='{field}']")).ToBeEditableAsync();
        await Assertions.Expect(Dialog.Locator("[data-chart-form='assignment'],[name='assignment-position-title'],[name='assignment-start-date'],[name='assignment-end-date']")).ToHaveCountAsync(0);
        await Assertions.Expect(Dialog.Locator(".person-summary")).ToContainTextAsync(Position("Report"));
    }

    private async Task AssertPreviewAsync(Guid id, string name)
    {
        await Assertions.Expect(page.Locator("[data-chart-print-preview]")).ToHaveAttributeAsync("data-print-company-id", id.ToString());
        await Assertions.Expect(page.Locator(".chart-print-heading h1")).ToHaveTextAsync(name);
        await Assertions.Expect(Chart.Locator("[data-chart-node='company']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".sidebar,.topbar,nav,.breadcrumbs")).ToHaveCountAsync(0);
        await Assertions.Expect(Chart.Locator("[data-chart-action],button")).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("parseFloat(getComputedStyle(document.querySelector('.org-children')).borderInlineStartWidth) > 0"));
    }

    private async Task SelectAsync(string name, string query)
    {
        await Dialog.Locator($"[name='{name}']").FillAsync(query);
        ILocator option = Dialog.GetByRole(AriaRole.Option).Filter(new() { HasTextString = query });
        await Assertions.Expect(option).ToHaveCountAsync(1);
        await option.ClickAsync();
    }

    private async Task SaveAsync()
    {
        await Dialog.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(Dialog).ToBeHiddenAsync(new() { Timeout = 15000 });
        await Assertions.Expect(Chart).ToBeVisibleAsync();
    }

    private async Task ReloadAsync()
    {
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(Chart).ToBeVisibleAsync();
    }

    private async Task ExpandFixtureAsync()
    {
        foreach (Guid id in new[] { companyId, branchId, sourceUnitId, targetUnitId }) await ExpandAsync(id);
    }

    private async Task ExpandAsync(Guid id)
    {
        ILocator expander = Node(id).Locator(":scope > button.org-expander");
        await Assertions.Expect(expander).ToBeVisibleAsync();
        if (await expander.GetAttributeAsync("aria-expanded") == "false") await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");
    }

    private Task<string[]> VisibleIdsAsync() => Chart.Locator("article").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.chartNodeId).sort()");
    private async Task<JsonElement> AssignmentAsync(Guid id) => (await GetAsync("api/v1/organization/chart")).GetProperty("assignments").EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == id).Clone();
    private static void AssertOnlyChanged(JsonElement before, JsonElement after, params string[] allowed)
    {
        foreach (JsonProperty property in before.EnumerateObject())
            if (property.Name != "version" && !allowed.Contains(property.Name))
                Assert.Equal(property.Value.GetRawText(), after.GetProperty(property.Name).GetRawText());
    }

    private HttpRequestMessage Request(HttpMethod method, string route, Guid? company = null)
    {
        HttpRequestMessage request = new(method, route);
        request.Headers.Add("X-Company-ID", (company ?? companyId).ToString());
        return request;
    }

    private async Task<JsonElement> GetAsync(string route, Guid? company = null)
    {
        using HttpRequestMessage request = Request(HttpMethod.Get, route, company);
        using HttpResponseMessage response = await api.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"Fixture GET {route} failed: {response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<Guid> CreateAsync(string route, string deleteRoute, object body, Guid? company = null)
    {
        using HttpRequestMessage request = Request(HttpMethod.Post, route, company);
        request.Content = JsonContent.Create(body);
        using HttpResponseMessage response = await api.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Fixture POST {route} failed: {response.StatusCode} {text}");
        using JsonDocument document = JsonDocument.Parse(text);
        Guid id = document.RootElement.GetProperty("id").GetGuid();
        cleanup.Add((deleteRoute, id, document.RootElement.GetProperty("version").GetInt64(), company ?? companyId));
        return id;
    }

    private Task<Guid> CreatePersonAsync(string name) => CreateAsync("api/v1/organization/persons", "api/v1/organization/persons", new { firstName = "Chart", lastName = $"{name} {suffix}" });
    private Task<Guid> CreateAssignmentAsync(Guid person, Guid unit, Guid? manager, string name) => CreateAsync("api/v1/organization/chart/assignments", "api/v1/organization/chart/assignments", new
    {
        personId = person, organizationalUnitId = unit, managerAssignmentId = manager,
        positionCode = $"{name}{suffix}", positionTitle = Position(name), isPrimary = false,
        startDate = "2026-09-08", endDate = "2027-09-08", notes = "Preserve these assignment notes"
    });

    private void AssertBrowserHealthy()
    {
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("pageerror:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("requestfailed:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("response:", StringComparison.OrdinalIgnoreCase));
    }
}
