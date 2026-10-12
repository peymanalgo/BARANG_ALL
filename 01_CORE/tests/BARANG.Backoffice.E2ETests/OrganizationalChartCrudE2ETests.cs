using System.IO;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class OrganizationalChartCrudE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private BrowserDiagnostics diagnostics = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private ILocator Dialog => page.Locator(".chart-dialog-backdrop .crud-dialog");

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        BrowserTypeLaunchOptions launch = new() { Headless = true };
        string? executable = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? GetLocalBrowserExecutable();
        if (string.IsNullOrWhiteSpace(executable)) launch.Channel = "chrome";
        else launch.ExecutablePath = executable;

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(launch);
        context = await browser.NewContextAsync(new() { Locale = "en-US" });
        page = await context.NewPageAsync();
        diagnostics = new(page);
        diagnostics.Attach();

        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        ILocator chartLink = page.Locator("nav a[href='/master/organization-chart']");
        ILocator chartGroup = chartLink.Locator("xpath=ancestor::details[1]");
        if (!await chartGroup.EvaluateAsync<bool>("el => el.open"))
        {
            await chartGroup.Locator("summary").ClickAsync();
            await Assertions.Expect(chartLink).ToBeVisibleAsync();
        }
        await chartLink.ClickAsync();
        await page.WaitForURLAsync("**/master/organization-chart");
        await Assertions.Expect(page.Locator("[data-chart='organizational-chart']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("article[data-chart-node='company']").First).ToBeVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        diagnostics?.Dump("strict organizational chart CRUD browser diagnostics");
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Fact]
    public async Task Company_real_create_view_edit_status_and_reload()
    {
        string suffix = UniqueSuffix();
        string code = $"E2EC{suffix}";
        string displayName = $"E2E Company {suffix}";
        string updatedName = $"{displayName} Updated";

        await page.Locator("[data-chart-action='company-create']").ClickAsync();
        await EnsureDialogAsync("company", "Create");
        string[] fullCompanyFields =
        [
            "company-code", "company-legal-name", "company-display-name", "company-national-id",
            "company-registration-number", "company-economic-code", "company-tax-id", "company-phone",
            "company-mobile", "company-email", "company-website", "company-country-code", "company-province",
            "company-city", "company-postal-code", "company-address", "company-currency", "company-notes"
        ];
        foreach (string field in fullCompanyFields)
            await Assertions.Expect(Dialog.Locator($"[name='{field}']")).ToBeVisibleAsync();

        await FillAsync("company-code", code);
        await FillAsync("company-legal-name", $"E2E Legal {suffix}");
        await FillAsync("company-display-name", displayName);
        await FillAsync("company-phone", "02188776655");
        await FillAsync("company-mobile", "09121112233");
        await FillAsync("company-email", $"chart-{suffix}@example.test");
        await FillAsync("company-country-code", "IR");
        await FillAsync("company-city", "Tehran");
        await FillAsync("company-address", "Organizational Chart E2E address");
        await FillAsync("company-currency", "IRR");
        await SaveDialogAsync();

        ILocator company = CompanyByCode(code);
        await Assertions.Expect(company).ToBeVisibleAsync();
        await ReloadChartAsync();
        company = CompanyByCode(code);
        await Assertions.Expect(company).ToBeVisibleAsync();

        await company.Locator("[data-chart-action='company-view']").ClickAsync();
        await EnsureDialogAsync("company", "View");
        await Assertions.Expect(Field("company-code")).ToHaveValueAsync(code);
        await Assertions.Expect(Field("company-code")).ToBeDisabledAsync();
        await Assertions.Expect(Field("company-legal-name")).ToBeDisabledAsync();
        await Dialog.Locator("footer button.btn-primary").ClickAsync();
        await EnsureDialogAsync("company", "Edit");
        await Assertions.Expect(Field("company-code")).ToBeDisabledAsync();
        await Assertions.Expect(Field("company-display-name")).ToBeEditableAsync();
        string updatedLegalName = $"E2E Legal {suffix} Updated";
        await Field("company-legal-name").FillAsync(updatedLegalName);
        await Field("company-display-name").FillAsync(updatedName);
        await SaveDialogAsync();
        company = CompanyByCode(code);
        await Assertions.Expect(company.Locator(".org-title strong")).ToHaveTextAsync(updatedName);
        await ReloadChartAsync();
        await Assertions.Expect(CompanyByCode(code).Locator(".org-title strong")).ToHaveTextAsync(updatedName);
        await CompanyByCode(code).Locator("[data-chart-action='company-view']").ClickAsync();
        await EnsureDialogAsync("company", "View");
        await Assertions.Expect(Field("company-legal-name")).ToHaveValueAsync(updatedLegalName);
        await Assertions.Expect(Field("company-display-name")).ToHaveValueAsync(updatedName);
        await CloseDialogAsync();

        company = CompanyByCode(code);
        await company.Locator("[data-chart-action='company-toggle-status']").ClickAsync();
        await Assertions.Expect(CompanyByCode(code)).ToHaveAttributeAsync("data-chart-active", "false");
        await ReloadChartAsync();
        await Assertions.Expect(CompanyByCode(code)).ToHaveAttributeAsync("data-chart-active", "false");
        await CompanyByCode(code).Locator("[data-chart-action='company-toggle-status']").ClickAsync();
        await Assertions.Expect(CompanyByCode(code)).ToHaveAttributeAsync("data-chart-active", "true");
        await DeleteLeafNodeAsync(CompanyByCode(code), "company");
        await Assertions.Expect(CompanyByCode(code)).ToHaveCountAsync(0);
        await ReloadChartAsync();
        await Assertions.Expect(CompanyByCode(code)).ToHaveCountAsync(0);
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Current_company_edit_reconciles_authoritative_tree_immediately_and_after_reload()
    {
        ILocator company = await CurrentCompanyAsync();
        await ExpandAsync(company);
        await company.Locator("[data-chart-action='company-view']").ClickAsync();
        await EnsureDialogAsync("company", "View");
        string code = await Field("company-code").InputValueAsync();
        string originalLegalName = await Field("company-legal-name").InputValueAsync();
        string originalDisplayName = await Field("company-display-name").InputValueAsync();
        string marker = $" E2E {UniqueSuffix()}";
        string updatedLegalName = originalLegalName.Length + marker.Length <= 200 ? originalLegalName + marker : marker.Trim();
        string updatedDisplayName = originalDisplayName.Length + marker.Length <= 200 ? originalDisplayName + marker : marker.Trim();

        await Dialog.Locator("footer button.btn-primary").ClickAsync();
        await EnsureDialogAsync("company", "Edit");
        await Field("company-legal-name").FillAsync(updatedLegalName);
        await Field("company-display-name").FillAsync(updatedDisplayName);
        await SaveDialogAsync();
        await Assertions.Expect(CompanyByCode(code).Locator(".org-title strong")).ToHaveTextAsync(updatedDisplayName);
        await Assertions.Expect(CompanyByCode(code).Locator(":scope > button.org-expander")).ToHaveAttributeAsync("aria-expanded", "true");
        await ReloadChartAsync();
        await Assertions.Expect(CompanyByCode(code).Locator(".org-title strong")).ToHaveTextAsync(updatedDisplayName);

        company = CompanyByCode(code);
        await company.Locator("[data-chart-action='company-edit']").ClickAsync();
        await EnsureDialogAsync("company", "Edit");
        await Field("company-legal-name").FillAsync(originalLegalName);
        await Field("company-display-name").FillAsync(originalDisplayName);
        await SaveDialogAsync();
        await Assertions.Expect(CompanyByCode(code).Locator(".org-title strong")).ToHaveTextAsync(originalDisplayName);
        await ReloadChartAsync();
        await Assertions.Expect(CompanyByCode(code).Locator(".org-title strong")).ToHaveTextAsync(originalDisplayName);
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task CompanyEdit_UpdatesVisibleRowImmediatelyAndAfterReload()
    {
        ILocator initialCompany = await CurrentCompanyAsync();
        await ExpandAsync(initialCompany);
        string companyId = await initialCompany.GetAttributeAsync("data-chart-node-id") ?? "";
        Assert.True(Guid.TryParse(companyId, out _));
        ILocator visibleCompanyRow = page.Locator($"article[data-chart-node='company'][data-chart-node-id='{companyId}']");
        ILocator visibleCompanyName = visibleCompanyRow.Locator(":scope > .org-title > strong");
        string oldName = (await visibleCompanyName.InnerTextAsync()).Trim();
        string newName = $"E2E Visible Company {UniqueSuffix()}";

        await Assertions.Expect(visibleCompanyName).ToHaveTextAsync(oldName);
        await visibleCompanyRow.Locator("[data-chart-action='company-edit']").ClickAsync();
        await EnsureDialogAsync("company", "Edit");
        await Assertions.Expect(Field("company-display-name")).ToHaveValueAsync(oldName);
        string legalName = await Field("company-legal-name").InputValueAsync();
        await Field("company-display-name").FillAsync(newName);

        await Dialog.Locator("footer button.btn-primary").ClickAsync();
        await Assertions.Expect(visibleCompanyName).Not.ToHaveTextAsync(oldName);
        await Assertions.Expect(visibleCompanyName).ToHaveTextAsync(newName);
        await Assertions.Expect(visibleCompanyRow.Locator(":scope > button.org-expander")).ToHaveAttributeAsync("aria-expanded", "true");

        await ReloadChartAsync();
        visibleCompanyRow = page.Locator($"article[data-chart-node='company'][data-chart-node-id='{companyId}']");
        visibleCompanyName = visibleCompanyRow.Locator(":scope > .org-title > strong");
        await Assertions.Expect(visibleCompanyName).Not.ToHaveTextAsync(oldName);
        await Assertions.Expect(visibleCompanyName).ToHaveTextAsync(newName);

        await visibleCompanyRow.Locator("[data-chart-action='company-edit']").ClickAsync();
        await EnsureDialogAsync("company", "Edit");
        string restoreName = oldName.StartsWith("E2E Visible Company ", StringComparison.Ordinal) ? legalName : oldName;
        await Field("company-display-name").FillAsync(restoreName);
        await SaveDialogAsync();
        await Assertions.Expect(page.Locator($"article[data-chart-node='company'][data-chart-node-id='{companyId}'] .org-title strong")).ToHaveTextAsync(restoreName);
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Branch_real_create_view_edit_status_and_reload()
    {
        string suffix = UniqueSuffix();
        string code = $"E2EB{suffix}";
        string name = $"E2E Branch {suffix}";
        ILocator company = await CurrentCompanyAsync();

        await company.Locator("[data-chart-action='branch-create']").ClickAsync();
        await EnsureDialogAsync("branch", "Create");
        await Assertions.Expect(Dialog.Locator("[data-chart-scope='company'] strong")).ToHaveTextAsync(await CurrentCompanyNameAsync());
        await Assertions.Expect(Dialog.Locator("input[name*='company'], select[name*='company']")).ToHaveCountAsync(0);
        string[] fullBranchFields =
        [
            "branch-code", "branch-display-name", "branch-phone", "branch-mobile", "branch-email",
            "branch-province", "branch-city", "branch-postal-code", "branch-address", "branch-notes"
        ];
        foreach (string field in fullBranchFields)
            await Assertions.Expect(Dialog.Locator($"[name='{field}']")).ToBeVisibleAsync();
        await FillAsync("branch-code", code);
        await FillAsync("branch-display-name", name);
        await FillAsync("branch-phone", "02144556677");
        await FillAsync("branch-mobile", "09123334455");
        await FillAsync("branch-email", $"branch-{suffix}@example.test");
        await FillAsync("branch-city", "Tehran");
        await FillAsync("branch-address", "Branch E2E address");
        await SaveDialogAsync();

        await AssertDeleteBlockedAsync(await CurrentCompanyAsync(), "company");

        ILocator branch = BranchByCode(code);
        await Assertions.Expect(branch).ToBeVisibleAsync();
        await ReloadAndExpandCompanyAsync();
        branch = BranchByCode(code);
        await Assertions.Expect(branch).ToBeVisibleAsync();

        await branch.Locator("[data-chart-action='branch-view']").ClickAsync();
        await EnsureDialogAsync("branch", "View");
        await Assertions.Expect(Field("branch-code")).ToHaveValueAsync(code);
        await Assertions.Expect(Field("branch-display-name")).ToHaveValueAsync(name);
        await Assertions.Expect(Field("branch-display-name")).ToBeDisabledAsync();
        await CloseDialogAsync();

        branch = BranchByCode(code);
        await branch.Locator("[data-chart-action='branch-edit']").ClickAsync();
        await EnsureDialogAsync("branch", "Edit");
        string updatedName = $"{name} Updated";
        await Field("branch-display-name").FillAsync(updatedName);
        await SaveDialogAsync();
        await Assertions.Expect(BranchByCode(code).Locator(".org-title strong")).ToHaveTextAsync(updatedName);
        await ReloadAndExpandCompanyAsync();
        await Assertions.Expect(BranchByCode(code).Locator(".org-title strong")).ToHaveTextAsync(updatedName);

        await BranchByCode(code).Locator("[data-chart-action='branch-toggle-status']").ClickAsync();
        await Assertions.Expect(BranchByCode(code)).ToHaveAttributeAsync("data-chart-active", "false");
        await BranchByCode(code).Locator("[data-chart-action='branch-toggle-status']").ClickAsync();
        await Assertions.Expect(BranchByCode(code)).ToHaveAttributeAsync("data-chart-active", "true");
        await DeleteLeafNodeAsync(BranchByCode(code), "branch");
        await Assertions.Expect(BranchByCode(code)).ToHaveCountAsync(0);
        await ReloadAndExpandCompanyAsync();
        await Assertions.Expect(BranchByCode(code)).ToHaveCountAsync(0);
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Unit_real_create_view_edit_status_and_reload()
    {
        string suffix = UniqueSuffix();
        string branchCode = $"E2EUB{suffix}";
        string unitCode = $"E2EU{suffix}";
        await CreateBranchAsync(branchCode, $"E2E Unit Branch {suffix}");
        ILocator branch = BranchByCode(branchCode);

        await branch.Locator("[data-chart-action='unit-create']").ClickAsync();
        await EnsureDialogAsync("unit", "Create");
        await Assertions.Expect(Dialog.Locator("[data-chart-scope='unit'] strong")).ToHaveCountAsync(2);
        await Assertions.Expect(Field("unit-parent")).ToHaveAttributeAsync("role", "combobox");
        await Assertions.Expect(Field("unit-code")).ToBeEditableAsync();
        await FillAsync("unit-code", unitCode);
        await FillAsync("unit-display-name", $"E2E Unit {suffix}");
        await FillAsync("unit-description", "Unit created by strict chart E2E");
        await SaveDialogAsync();

        ILocator unit = UnitByCode(unitCode);
        await Assertions.Expect(unit).ToBeVisibleAsync();
        await ReloadAndExpandBranchAsync(branchCode);
        unit = UnitByCode(unitCode);
        await Assertions.Expect(unit).ToBeVisibleAsync();

        await unit.Locator("[data-chart-action='unit-view']").ClickAsync();
        await EnsureDialogAsync("unit", "View");
        await Assertions.Expect(Field("unit-code")).ToHaveValueAsync(unitCode);
        await Assertions.Expect(Field("unit-description")).ToHaveValueAsync("Unit created by strict chart E2E");
        await Assertions.Expect(Field("unit-display-name")).ToBeDisabledAsync();
        await CloseDialogAsync();

        unit = UnitByCode(unitCode);
        await unit.Locator("[data-chart-action='unit-edit']").ClickAsync();
        await EnsureDialogAsync("unit", "Edit");
        string updatedName = $"E2E Unit {suffix} Updated";
        await Field("unit-display-name").FillAsync(updatedName);
        await SaveDialogAsync();
        await Assertions.Expect(UnitByCode(unitCode).Locator(".org-title strong")).ToHaveTextAsync(updatedName);
        await ReloadAndExpandBranchAsync(branchCode);
        await Assertions.Expect(UnitByCode(unitCode).Locator(".org-title strong")).ToHaveTextAsync(updatedName);

        await UnitByCode(unitCode).Locator("[data-chart-action='unit-toggle-status']").ClickAsync();
        await Assertions.Expect(UnitByCode(unitCode)).ToHaveAttributeAsync("data-chart-active", "false");
        await UnitByCode(unitCode).Locator("[data-chart-action='unit-toggle-status']").ClickAsync();
        await Assertions.Expect(UnitByCode(unitCode)).ToHaveAttributeAsync("data-chart-active", "true");

        string childCode = $"E2ECU{suffix}";
        string childName = $"E2E Child Unit {suffix}";
        await UnitByCode(unitCode).Locator("[data-chart-action='unit-create']").ClickAsync();
        await EnsureDialogAsync("unit", "Create");
        await Assertions.Expect(Field("unit-parent")).ToHaveValueAsync(new System.Text.RegularExpressions.Regex(updatedName));
        await FillAsync("unit-code", childCode);
        await FillAsync("unit-display-name", childName);
        await SaveDialogAsync();
        await Assertions.Expect(UnitByCode(childCode)).ToBeVisibleAsync();
        await CollapseAndExpandAsync(UnitByCode(unitCode));
        await AssertDeleteBlockedAsync(UnitByCode(unitCode), "unit");
        await AssertDeleteBlockedAsync(BranchByCode(branchCode), "branch");

        await DeleteLeafNodeAsync(UnitByCode(childCode), "unit");
        await Assertions.Expect(UnitByCode(childCode)).ToHaveCountAsync(0);
        await DeleteLeafNodeAsync(UnitByCode(unitCode), "unit");
        await Assertions.Expect(UnitByCode(unitCode)).ToHaveCountAsync(0);
        await ReloadAndExpandCompanyAsync();
        await Assertions.Expect(BranchByCode(branchCode)).ToBeVisibleAsync();
        await Assertions.Expect(UnitByCode(unitCode)).ToHaveCountAsync(0);
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Person_assignment_real_create_view_edit_subordinate_remove_and_reload()
    {
        string suffix = UniqueSuffix();
        string branchCode = $"E2EAB{suffix}";
        string unitCode = $"E2EAU{suffix}";
        string secondUnitCode = $"E2EA2{suffix}";
        await CreateBranchAsync(branchCode, $"E2E Assignment Branch {suffix}");
        await CreateUnitAsync(branchCode, unitCode, $"E2E Assignment Unit {suffix}");
        await CreateUnitAsync(branchCode, secondUnitCode, $"E2E Target Unit {suffix}");
        ILocator unit = UnitByCode(unitCode);

        await unit.Locator("[data-chart-action='assignment-create']").ClickAsync();
        await EnsureAssignmentCreateFormAsync();
        await SelectPersonAsync("Docker Administrator");
        string selectedPersonId = await Dialog.Locator(".person-summary").GetAttributeAsync("data-selected-person-id") ?? "";
        Assert.True(Guid.TryParse(selectedPersonId, out Guid stablePersonId));
        Assert.NotEqual(Guid.Empty, stablePersonId);
        await Assertions.Expect(Field("assignment-unit")).ToBeDisabledAsync();
        await FillAsync("assignment-position-title", $"E2E Manager {suffix}");
        await FillAsync("assignment-position-code", $"M{suffix}");
        await FillAsync("assignment-start-date", "2026/09/08");
        await FillAsync("assignment-end-date", "2027/09/08");
        await FillAsync("assignment-notes", "Persistent chart assignment");
        await SaveDialogAsync();

        ILocator manager = AssignmentByPosition($"E2E Manager {suffix}");
        await Assertions.Expect(manager).ToBeVisibleAsync();
        string managerAssignmentId = await manager.GetAttributeAsync("data-chart-node-id") ?? "";
        Assert.True(Guid.TryParse(managerAssignmentId, out _));
        await ReloadAndExpandUnitAsync(branchCode, unitCode);
        manager = AssignmentByPosition($"E2E Manager {suffix}");
        await Assertions.Expect(manager).ToBeVisibleAsync();

        await manager.Locator("[data-chart-action='assignment-view']").ClickAsync();
        await EnsureDialogAsync("assignment", "View");
        await Assertions.Expect(Field("assignment-position-title")).ToHaveValueAsync($"E2E Manager {suffix}");
        await Assertions.Expect(Field("assignment-position-title")).ToBeDisabledAsync();
        await Assertions.Expect(Dialog.Locator("[name='assignment-person-search']")).ToHaveCountAsync(0);
        await Assertions.Expect(Dialog.Locator(".person-summary .status-badge")).ToBeVisibleAsync();
        await CloseDialogAsync();

        manager = AssignmentByPosition($"E2E Manager {suffix}");
        await manager.Locator("[data-chart-action='assignment-edit']").ClickAsync();
        await EnsureDialogAsync("assignment", "Edit");
        await Assertions.Expect(Field("assignment-unit")).ToBeEditableAsync();
        string updatedPosition = $"E2E Director {suffix}";
        await Field("assignment-position-title").FillAsync(updatedPosition);
        await SaveDialogAsync();
        await Assertions.Expect(AssignmentByPosition(updatedPosition)).ToBeVisibleAsync();
        await ReloadAndExpandUnitAsync(branchCode, unitCode);
        manager = AssignmentByPosition(updatedPosition);

        string alternatePosition = $"E2E Alternate {suffix}";
        await UnitByCode(secondUnitCode).Locator("[data-chart-action='assignment-create']").ClickAsync();
        await EnsureAssignmentCreateFormAsync();
        await SelectPersonAsync("Docker Administrator");
        await Assertions.Expect(Dialog.Locator(".person-summary")).ToHaveAttributeAsync("data-selected-person-id", stablePersonId.ToString());
        await FillAsync("assignment-position-title", alternatePosition);
        await FillAsync("assignment-position-code", $"A{suffix}");
        await SaveDialogAsync();
        ILocator alternateManager = AssignmentByPosition(alternatePosition);
        await Assertions.Expect(alternateManager).ToBeVisibleAsync();
        string alternateManagerId = await alternateManager.GetAttributeAsync("data-chart-node-id") ?? "";
        Assert.True(Guid.TryParse(alternateManagerId, out _));

        manager = AssignmentByPosition(updatedPosition);
        await manager.Locator("[data-chart-action='assignment-create-child']").ClickAsync();
        await EnsureAssignmentCreateFormAsync();
        await Assertions.Expect(Field("assignment-manager")).ToBeDisabledAsync();
        await Assertions.Expect(Field("assignment-manager")).Not.ToHaveValueAsync("");
        await SelectPersonAsync("132456879132456");
        string reportPersonId = await Dialog.Locator(".person-summary").GetAttributeAsync("data-selected-person-id") ?? "";
        Assert.True(Guid.TryParse(reportPersonId, out _));
        string reportPosition = $"E2E Specialist {suffix}";
        await FillAsync("assignment-position-title", reportPosition);
        await FillAsync("assignment-position-code", $"S{suffix}");
        await SaveDialogAsync();

        ILocator report = AssignmentByPosition(reportPosition);
        await Assertions.Expect(report).ToBeVisibleAsync();
        await Assertions.Expect(report).ToHaveAttributeAsync("data-manager-assignment-id", managerAssignmentId);
        await Assertions.Expect(report.Locator("xpath=ancestor::div[contains(@class,'org-assignment-children')]")).ToBeVisibleAsync();
        await ReloadAndExpandUnitAsync(branchCode, unitCode);
        report = AssignmentByPosition(reportPosition);
        await Assertions.Expect(report).ToHaveAttributeAsync("data-manager-assignment-id", managerAssignmentId);

        await report.Locator("[data-chart-action='assignment-change-manager']").ClickAsync();
        await EnsureDialogAsync("assignment", "Edit");
        await SelectSearchableOptionAsync("assignment-manager", alternatePosition);
        await SaveDialogAsync();
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveAttributeAsync("data-manager-assignment-id", alternateManagerId);

        report = AssignmentByPosition(reportPosition);
        await report.Locator("[data-chart-action='assignment-move-unit']").ClickAsync();
        await EnsureDialogAsync("assignment", "Edit");
        await SelectSearchableOptionAsync("assignment-unit", secondUnitCode);
        await SaveDialogAsync();
        string targetUnitId = await UnitByCode(secondUnitCode).GetAttributeAsync("data-chart-node-id") ?? "";
        Assert.True(Guid.TryParse(targetUnitId, out _));
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveAttributeAsync("data-chart-parent-node-id", targetUnitId);
        await ReloadAndExpandUnitAsync(branchCode, secondUnitCode);
        report = AssignmentByPosition(reportPosition);
        await Assertions.Expect(report).ToHaveAttributeAsync("data-manager-assignment-id", alternateManagerId);

        await report.Locator("[data-chart-action='assignment-toggle-status']").ClickAsync();
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveAttributeAsync("data-chart-active", "false");
        await ReloadAndExpandUnitAsync(branchCode, secondUnitCode);
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveAttributeAsync("data-chart-active", "false");
        await AssignmentByPosition(reportPosition).Locator("[data-chart-action='assignment-toggle-status']").ClickAsync();
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveAttributeAsync("data-chart-active", "true");

        await AssertDeleteBlockedAsync(AssignmentByPosition(alternatePosition), "assignment");
        await AssertDeleteBlockedAsync(UnitByCode(secondUnitCode), "unit");
        await DeleteLeafNodeAsync(AssignmentByPosition(reportPosition), "assignment");
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveCountAsync(0);
        await ReloadAndExpandUnitAsync(branchCode, secondUnitCode);
        await Assertions.Expect(AssignmentByPosition(reportPosition)).ToHaveCountAsync(0);

        await UnitByCode(secondUnitCode).Locator("[data-chart-action='assignment-create']").ClickAsync();
        await EnsureAssignmentCreateFormAsync();
        await Field("assignment-person-search").FillAsync("132456879132456");
        ILocator preservedPerson = Dialog.Locator("[role='option']").First;
        await Assertions.Expect(preservedPerson).ToBeVisibleAsync();
        await preservedPerson.ClickAsync();
        await Assertions.Expect(Dialog.Locator(".person-summary")).ToHaveAttributeAsync("data-selected-person-id", reportPersonId);
        await CloseDialogAsync();

        await DeleteLeafNodeAsync(AssignmentByPosition(alternatePosition), "assignment");
        await ReloadAndExpandUnitAsync(branchCode, unitCode);
        await DeleteLeafNodeAsync(AssignmentByPosition(updatedPosition), "assignment");
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Position_delete_action_confirms_and_archives_position_with_history()
    {
        string suffix = UniqueSuffix();
        string branchCode = $"E2EPB{suffix}";
        string unitCode = $"E2EPU{suffix}";
        await CreateBranchAsync(branchCode, $"E2E Position Delete Branch {suffix}");
        await CreateUnitAsync(branchCode, unitCode, $"E2E Position Delete Unit {suffix}");
        ILocator unit = UnitByCode(unitCode);
        await unit.Locator("[data-chart-action='position-create']").ClickAsync();
        await EnsureDialogAsync("unknown", "Create");
        await FillAsync("position-title", $"E2E Position Delete {suffix}");
        await FillAsync("position-code", $"PD{suffix}");
        await SaveDialogAsync();
        await ReloadAndExpandUnitAsync(branchCode, unitCode);

        ILocator position = page.Locator("article[data-chart-node='position'][data-chart-active='true']").Filter(new() { HasTextString = $"E2E Position Delete {suffix}" });
        await Assertions.Expect(position).ToBeVisibleAsync();
        ILocator delete = position.Locator("[data-chart-action='position-delete']");
        await Assertions.Expect(delete).ToBeVisibleAsync();
        await Assertions.Expect(delete).ToBeEnabledAsync();

        await delete.ClickAsync();
        ILocator confirmation = page.Locator("[data-chart-delete-confirm='position']");
        await Assertions.Expect(confirmation).ToBeVisibleAsync();
        await Assertions.Expect(confirmation.Locator("[data-chart-confirm-delete]")).ToBeEnabledAsync();
        await confirmation.Locator("[data-chart-confirm-delete]").ClickAsync();
        await Assertions.Expect(confirmation).ToBeHiddenAsync(new() { Timeout = 15_000 });

        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("deleted successfully");
        await Assertions.Expect(position).ToHaveCountAsync(0);
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Chart_icon_tooltips_and_accessible_names_switch_between_fa_ar_and_en()
    {
        string suffix = UniqueSuffix();
        string companyCode = $"E2EL{suffix}";
        string branchCode = $"E2ELB{suffix}";
        string unitCode = $"E2ELU{suffix}";
        string position = $"E2E Locale {suffix}";

        await page.Locator("[data-chart-action='company-create']").ClickAsync();
        await EnsureDialogAsync("company", "Create");
        await FillAsync("company-code", companyCode);
        await FillAsync("company-legal-name", $"E2E Locale Legal {suffix}");
        await FillAsync("company-display-name", $"E2E Locale Company {suffix}");
        await SaveDialogAsync();

        await CreateBranchAsync(branchCode, $"E2E Locale Branch {suffix}");
        await CreateUnitAsync(branchCode, unitCode, $"E2E Locale Unit {suffix}");
        await UnitByCode(unitCode).Locator("[data-chart-action='assignment-create']").ClickAsync();
        await EnsureAssignmentCreateFormAsync();
        await SelectPersonAsync("Docker Administrator");
        await FillAsync("assignment-position-title", position);
        await FillAsync("assignment-position-code", $"L{suffix}");
        await SaveDialogAsync();

        await AssertLocalizedChartActionsAsync("fa", "rtl", "ویرایش", "انتقال", "حذف", "عملیات بیشتر", branchCode, unitCode, position, companyCode);
        await AssertLocalizedChartActionsAsync("ar", "rtl", "تعديل", "نقل", "حذف", "إجراءات إضافية", branchCode, unitCode, position, companyCode);
        await AssertLocalizedChartActionsAsync("en", "ltr", "Edit", "Move", "Delete", "More actions", branchCode, unitCode, position, companyCode);

        await DeleteLeafNodeAsync(AssignmentByPosition(position), "assignment");
        await DeleteLeafNodeAsync(UnitByCode(unitCode), "unit");
        await DeleteLeafNodeAsync(BranchByCode(branchCode), "branch");
        await DeleteLeafNodeAsync(CompanyByCode(companyCode), "company");
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Organizational_unit_display_order_is_editable_and_reorders_children()
    {
        const string branchCode = "B21B1788281840";
        const string parentCode = "OPS";
        string suffix = UniqueSuffix();
        string firstCode = $"ORD-A{suffix}";
        string secondCode = $"ORD-B{suffix}";
        string firstName = $"Order A {suffix}";
        string secondName = $"Order B {suffix}";

        await ReloadAndExpandUnitAsync(branchCode, parentCode);
        ILocator parent = UnitByCode(parentCode);
        await parent.Locator("[data-chart-action='unit-create']").ClickAsync();
        await EnsureDialogAsync("unit", "Create");
        await FillAsync("unit-code", firstCode);
        await FillAsync("unit-display-name", firstName);
        await FillAsync("unit-display-order", "20");
        await Assertions.Expect(Field("unit-display-order")).ToHaveValueAsync("20");
        await SaveDialogAsync();

        parent = UnitByCode(parentCode);
        await parent.Locator("[data-chart-action='unit-create']").ClickAsync();
        await EnsureDialogAsync("unit", "Create");
        await FillAsync("unit-code", secondCode);
        await FillAsync("unit-display-name", secondName);
        await FillAsync("unit-display-order", "10");
        await SaveDialogAsync();
        await ReloadAndExpandUnitAsync(branchCode, parentCode);

        IReadOnlyList<string> initial = await page.Locator("article[data-chart-node='unit']").AllInnerTextsAsync();
        Assert.True(initial.ToList().FindIndex(x => x.Contains(secondName, StringComparison.Ordinal)) < initial.ToList().FindIndex(x => x.Contains(firstName, StringComparison.Ordinal)));

        ILocator first = UnitByCode(firstCode);
        await first.Locator("[data-chart-action='unit-edit']").ClickAsync();
        await EnsureDialogAsync("unit", "Edit");
        await FillAsync("unit-display-order", "5");
        await SaveDialogAsync();
        await ReloadAndExpandUnitAsync(branchCode, parentCode);
        IReadOnlyList<string> reordered = await page.Locator("article[data-chart-node='unit']").AllInnerTextsAsync();
        Assert.True(reordered.ToList().FindIndex(x => x.Contains(firstName, StringComparison.Ordinal)) < reordered.ToList().FindIndex(x => x.Contains(secondName, StringComparison.Ordinal)));

        await DeleteLeafNodeAsync(UnitByCode(firstCode), "unit");
        await DeleteLeafNodeAsync(UnitByCode(secondCode), "unit");
        AssertNoBrowserFailures();
    }

    [Fact]
    public async Task Targeted_cleanup_is_visible_in_real_browser_without_chart_branches_or_extra_people()
    {
        await Assertions.Expect(page.Locator("article[data-chart-node='branch']").Filter(new() { HasTextString = "Chart Branch" })).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("article[data-chart-node='branch']").Filter(new() { HasTextString = "مدیریت برنگ ایران" })).ToHaveCountAsync(0);
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=/master/persons", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("table.data-grid tbody tr")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator("table.data-grid")).ToContainTextAsync("پیمان خوشه چین");
        await Assertions.Expect(page.Locator("table.data-grid")).ToContainTextAsync("علیرضا اسکندری");
        AssertNoBrowserFailures();
    }

    private async Task CreateBranchAsync(string code, string name)
    {
        await (await CurrentCompanyAsync()).Locator("[data-chart-action='branch-create']").ClickAsync();
        await EnsureDialogAsync("branch", "Create");
        await FillAsync("branch-code", code);
        await FillAsync("branch-display-name", name);
        await SaveDialogAsync();
        await Assertions.Expect(BranchByCode(code)).ToBeVisibleAsync();
    }

    private async Task CreateUnitAsync(string branchCode, string code, string name)
    {
        await BranchByCode(branchCode).Locator("[data-chart-action='unit-create']").ClickAsync();
        await EnsureDialogAsync("unit", "Create");
        await FillAsync("unit-code", code);
        await FillAsync("unit-display-name", name);
        await SaveDialogAsync();
        await Assertions.Expect(UnitByCode(code)).ToBeVisibleAsync();
    }

    private async Task EnsureAssignmentCreateFormAsync()
    {
        await EnsureDialogAsync("assignment", "Create");
        string[] fields =
        [
            "assignment-person-search", "assignment-unit", "assignment-position-title", "assignment-position-code",
            "assignment-manager", "assignment-start-date", "assignment-end-date", "assignment-primary", "assignment-notes"
        ];
        foreach (string field in fields)
            await Assertions.Expect(Field(field)).ToBeVisibleAsync();
        await Assertions.Expect(Dialog.Locator(".person-summary")).ToBeVisibleAsync();
    }

    private async Task SelectPersonAsync(string query)
    {
        ILocator search = Field("assignment-person-search");
        await search.FillAsync(query);
        ILocator option = Dialog.Locator("[role='option']").First;
        await Assertions.Expect(option).ToBeVisibleAsync();
        await option.ClickAsync();
        await Assertions.Expect(Dialog.Locator(".person-summary strong").First).Not.ToHaveTextAsync("");
    }

    private async Task SelectSearchableOptionAsync(string fieldName, string optionText)
    {
        await Field(fieldName).FillAsync(optionText);
        ILocator option = Dialog.Locator("[role='option']").Filter(new() { HasTextString = optionText }).First;
        try
        {
            await Assertions.Expect(option).ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
        catch (Exception error)
        {
            IReadOnlyList<string> available = await Dialog.Locator("[role='option']").AllInnerTextsAsync();
            throw new InvalidOperationException($"Searchable field '{fieldName}' did not expose '{optionText}'. Available options: {string.Join(" | ", available)}", error);
        }
        await option.ClickAsync();
    }

    private async Task AssertLocalizedChartActionsAsync(
        string culture,
        string direction,
        string edit,
        string move,
        string delete,
        string more,
        string branchCode,
        string unitCode,
        string position,
        string companyCode)
    {
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=/master/organization-chart", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", culture);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
        await ReloadAndExpandUnitAsync(branchCode, unitCode);

        ILocator companyEdit = CompanyByCode(companyCode).Locator("[data-chart-action='company-edit']");
        await Assertions.Expect(companyEdit).ToHaveAttributeAsync("title", edit);
        await Assertions.Expect(companyEdit).ToHaveAttributeAsync("aria-label", edit);
        ILocator assignmentMove = AssignmentByPosition(position).Locator("[data-chart-action='assignment-move-unit']");
        await Assertions.Expect(assignmentMove).ToHaveAttributeAsync("title", move);
        await Assertions.Expect(assignmentMove).ToHaveAttributeAsync("aria-label", move);
        ILocator companyMore = CompanyByCode(companyCode).Locator("[data-chart-action='company-more']");
        await Assertions.Expect(companyMore).ToHaveAttributeAsync("title", more);
        await Assertions.Expect(companyMore).ToHaveAttributeAsync("aria-label", more);
        await companyMore.ClickAsync();
        ILocator companyDelete = CompanyByCode(companyCode).Locator("[data-chart-action='company-delete']");
        await Assertions.Expect(companyDelete).ToHaveAttributeAsync("title", delete);
        await Assertions.Expect(companyDelete).ToHaveAttributeAsync("aria-label", delete);
        await companyMore.ClickAsync();

        if (culture is "fa" or "ar")
        {
            await Assertions.Expect(page.Locator("[data-chart='organizational-chart'] [title='Move'], [data-chart='organizational-chart'] [title='Edit'], [data-chart='organizational-chart'] [title='Delete']")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("[data-chart='organizational-chart'] [aria-label='Move'], [data-chart='organizational-chart'] [aria-label='Edit'], [data-chart='organizational-chart'] [aria-label='Delete']")).ToHaveCountAsync(0);
        }
    }

    private async Task AssertDeleteBlockedAsync(ILocator node, string nodeType)
    {
        await node.Locator($"[data-chart-action='{nodeType}-more']").ClickAsync();
        ILocator delete = node.Locator($"[data-chart-action='{nodeType}-delete']");
        await Assertions.Expect(delete).ToBeVisibleAsync();
        await Assertions.Expect(delete).ToBeDisabledAsync();
        await Assertions.Expect(delete).Not.ToHaveAttributeAsync("title", "");
        await Assertions.Expect(node.Locator(".chart-action-reason")).ToBeVisibleAsync();
        await node.Locator($"[data-chart-action='{nodeType}-more']").ClickAsync();
    }

    private async Task DeleteLeafNodeAsync(ILocator node, string nodeType)
    {
        await node.Locator($"[data-chart-action='{nodeType}-more']").ClickAsync();
        ILocator delete = node.Locator($"[data-chart-action='{nodeType}-delete']");
        await Assertions.Expect(delete).ToBeVisibleAsync();
        await Assertions.Expect(delete).ToBeEnabledAsync();
        await delete.ClickAsync();
        ILocator confirmation = page.Locator($"[data-chart-delete-confirm='{nodeType}']");
        await Assertions.Expect(confirmation).ToBeVisibleAsync();
        await Assertions.Expect(confirmation.Locator("[data-chart-confirm-delete]")).ToBeEnabledAsync();
        await confirmation.Locator("[data-chart-confirm-delete]").ClickAsync();
        await Assertions.Expect(confirmation).ToBeHiddenAsync(new() { Timeout = 15_000 });
    }

    private async Task EnsureDialogAsync(string kind, string mode)
    {
        await Assertions.Expect(Dialog).ToBeVisibleAsync();
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("data-chart-dialog-kind", kind);
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("data-chart-dialog-mode", mode);
        await Assertions.Expect(Dialog).ToHaveAttributeAsync("role", "dialog");
    }

    private async Task SaveDialogAsync()
    {
        await Dialog.Locator("footer button.btn-primary").ClickAsync();
        await Assertions.Expect(Dialog).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator("[data-chart='organizational-chart']")).ToBeVisibleAsync();
    }

    private async Task CloseDialogAsync()
    {
        await Dialog.Locator("button.dialog-close").ClickAsync();
        await Assertions.Expect(Dialog).ToBeHiddenAsync();
    }

    private async Task FillAsync(string name, string value) => await Field(name).FillAsync(value);
    private ILocator Field(string name) => Dialog.Locator($"[name='{name}']");
    private ILocator CompanyByCode(string code) => page.Locator("article[data-chart-node='company']").Filter(new() { HasTextString = code });
    private ILocator BranchByCode(string code) => page.Locator("article[data-chart-node='branch']").Filter(new() { HasTextString = code });
    private ILocator UnitByCode(string code) => page.Locator("article[data-chart-node='unit']").Filter(new() { HasTextString = code });
    private ILocator AssignmentByPosition(string position) => page.Locator("article[data-chart-node='assignment']").Filter(new() { HasTextString = position });

    private async Task<string> CurrentCompanyNameAsync() => (await page.Locator(".company-chip strong").InnerTextAsync()).Trim();

    private async Task<ILocator> CurrentCompanyAsync()
    {
        string name = await CurrentCompanyNameAsync();
        ILocator company = page.Locator("article[data-chart-node='company']").Filter(new() { HasTextString = name });
        await Assertions.Expect(company).ToBeVisibleAsync();
        await Assertions.Expect(company.Locator("[data-chart-action='branch-create']")).ToBeVisibleAsync();
        return company;
    }

    private async Task ReloadChartAsync()
    {
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("[data-chart='organizational-chart']")).ToBeVisibleAsync();
    }

    private async Task ReloadAndExpandCompanyAsync()
    {
        await ReloadChartAsync();
        await ExpandAsync(await CurrentCompanyAsync());
    }

    private async Task ReloadAndExpandBranchAsync(string branchCode)
    {
        await ReloadAndExpandCompanyAsync();
        ILocator branch = BranchByCode(branchCode);
        await Assertions.Expect(branch).ToBeVisibleAsync();
        await ExpandAsync(branch);
    }

    private async Task ReloadAndExpandUnitAsync(string branchCode, string unitCode)
    {
        await ReloadAndExpandBranchAsync(branchCode);
        ILocator unit = UnitByCode(unitCode);
        await Assertions.Expect(unit).ToBeVisibleAsync();
        await ExpandAsync(unit);
    }

    private static async Task ExpandAsync(ILocator node)
    {
        ILocator expander = node.Locator(":scope > button.org-expander");
        await Assertions.Expect(expander).ToBeVisibleAsync();
        if (string.Equals(await expander.GetAttributeAsync("aria-expanded"), "false", StringComparison.OrdinalIgnoreCase))
            await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(expander.Locator("svg[data-icon='chevron-down']")).ToBeVisibleAsync();
    }

    private static async Task CollapseAndExpandAsync(ILocator node)
    {
        ILocator expander = node.Locator(":scope > button.org-expander");
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");
        await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "false");
        await expander.ClickAsync();
        await Assertions.Expect(expander).ToHaveAttributeAsync("aria-expanded", "true");
    }

    private void AssertNoBrowserFailures()
    {
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("pageerror:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("requestfailed:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics.Events, item => item.StartsWith("response:", StringComparison.OrdinalIgnoreCase));
    }

    private static string UniqueSuffix() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()[^9..];

    private static string? GetLocalBrowserExecutable()
    {
        string[] candidates = OperatingSystem.IsWindows()
            ?
            [
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            ]
            : OperatingSystem.IsLinux()
                ? ["/usr/bin/google-chrome", "/usr/bin/chromium", "/usr/bin/chromium-browser"]
                : OperatingSystem.IsMacOS()
                    ? ["/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge"]
                    : [];
        return candidates.FirstOrDefault(File.Exists);
    }
}
