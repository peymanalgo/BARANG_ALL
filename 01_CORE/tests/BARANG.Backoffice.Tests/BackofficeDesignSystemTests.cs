using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BARANG.Backoffice.Tests;

public sealed class BackofficeDesignSystemTests
{
    [Fact]
    public void ApplicationLoadsTheSingleBarangDesignSystemAfterLegacyBaseStyles()
    {
        string root = FindRepositoryRoot();
        string app = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "App.razor");

        Assert.Contains("app.css", app);
        Assert.Contains("barang-design-system.css", app);
        Assert.DoesNotContain("master-crud.css", app);
        Assert.True(app.IndexOf("app.css", StringComparison.Ordinal) < app.IndexOf("barang-design-system.css", StringComparison.Ordinal));
    }

    [Fact]
    public void SharedDesignSystemDefinesTheGoverningTokensAndReusableFoundations()
    {
        string root = FindRepositoryRoot();
        string components = Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components");
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "barang-design-system.css");

        string[] tokens =
        [
            "--barang-sidebar-bg: #342464", "--barang-accent: #6c4df6", "--barang-page-bg: #f7f5ff",
            "--barang-surface", "--barang-border", "--barang-control-height", "--barang-modal-md", "--barang-z-modal"
        ];
        Assert.All(tokens, token => Assert.Contains(token, css));

        string[] foundations =
        [
            "BarangPageHeader.razor", "BarangBreadcrumb.razor", "BarangButton.razor", "BarangIconButton.razor",
            "BarangModal.razor", "BarangFormSection.razor", "BarangTabs.razor", "BarangDataGrid.razor",
            "BarangCardGrid.razor", "BarangJsonGrid.razor", "BarangToolbar.razor", "BarangKpiCard.razor", "SearchableSelect.razor",
            "LocaleDateInput.razor", "CrudEmptyState.razor", "BarangLoadingState.razor", "BarangErrorState.razor",
            "BarangToast.razor", "BarangPagination.razor", "BarangConfirmDialog.razor"
        ];
        Assert.All(foundations, name => Assert.True(File.Exists(Path.Combine(components, name)), $"Missing shared component {name}."));
    }

    [Fact]
    public void ShellUsesRequiredLanguagesResponsiveNavigationAndForbiddenFeaturesAreAbsent()
    {
        string root = FindRepositoryRoot();
        string shell = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Layout", "MainLayout.razor");
        string languages = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "LanguageSwitcher.razor");

        Assert.Contains("<details", shell);
        Assert.Contains("sidebar-account", shell);
        Assert.Contains("mobile-navigation-trigger", shell);
        Assert.DoesNotContain("sidebar-collapse-toggle", shell);
        Assert.DoesNotContain("mobile-sidebar-toggle", shell);
        Assert.DoesNotContain("sidebar-expand-toggle", shell);
        Assert.DoesNotContain("mobile-nav-toggle", shell);
        Assert.DoesNotContain("shell-location", shell);
        Assert.DoesNotContain("topbar-leading", shell);
        Assert.Contains("company-chip", shell);
        Assert.DoesNotContain("smart assistant", shell, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("service-status", shell, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Workspace", shell, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("[(\"fa\", \"فارسی\"), (\"ar\", \"العربية\"), (\"en\", \"English\")]", languages);
    }

    [Fact]
    public void EveryRoutedPageUsesExplicitButtonTypesAndSemanticCrudDialogs()
    {
        string root = FindRepositoryRoot();
        string components = Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components");
        string[] routedPages = Directory.GetFiles(components, "*.razor", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("@page ", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(45, routedPages.Length);
        foreach (string file in Directory.GetFiles(components, "*.razor", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            foreach (Match button in Regex.Matches(source, "<button\\b(?:(?!>).)*>", RegexOptions.Singleline))
                Assert.Contains("type=", button.Value, StringComparison.OrdinalIgnoreCase);

            if (source.Contains("class=\"crud-dialog", StringComparison.Ordinal))
            {
                Assert.Contains("role=\"dialog\"", source);
                Assert.Contains("aria-modal=\"true\"", source);
            }
        }
        Assert.All(routedPages, file => Assert.DoesNotContain("<pre>", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IconOnlyCrudActionsHaveLocalizedAccessibleNames()
    {
        string root = FindRepositoryRoot();
        string components = Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components");

        foreach (string file in Directory.GetFiles(components, "*.razor", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            foreach (Match action in Regex.Matches(source, "<(?:button|a)\\b(?:(?!>)(?:icon-action|.)).*?>", RegexOptions.Singleline))
            {
                if (!action.Value.Contains("icon-action", StringComparison.Ordinal)) continue;
                Assert.Contains("aria-label=", action.Value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryLocalizationFamilyHasCompleteFaEnArKeys()
    {
        string root = FindRepositoryRoot();
        string localization = Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization");
        string[] families = ["AppStrings", "CrudStrings", "NavigationStrings", "OrganizationStrings", "ClassificationStrings", "BusinessStrings", "UserAccessStrings", "CustomerStrings", "ReferenceStrings"];

        foreach (string family in families)
        {
            string[] files = Directory.GetFiles(localization, $"{family}*.resx");
            Assert.True(files.Length >= 3, $"{family} must contain fa/en/ar resources.");
            string[] expected = Keys(files[0]);
            Assert.All(files, file => Assert.Equal(expected, Keys(file)));
        }
    }

    [Fact]
    public void ComplexSelectorsAndDatesUseCentralizedControls()
    {
        string root = FindRepositoryRoot();
        string[] selectorPages = ["OrderWizard.razor", "BusinessProfileEditor.razor", "SupplierProductLifecycle.razor", "MasterEntityManager.razor", "UserAccessManagement.razor"];
        Assert.All(selectorPages, page => Assert.Contains("<SearchableSelect", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", page)));
        Assert.Contains("<LocaleDateInput", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        Assert.Contains("<LocaleDateInput", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "BusinessProfileEditor.razor"));
        Assert.Contains("<LocaleDateInput", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Persons.razor"));
        Assert.Contains("<LocaleDateInput", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "LogisticsWorkspace.razor"));
        string management = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "ManagementDashboard.razor");
        Assert.DoesNotContain("BarangJsonGrid", management);
        Assert.DoesNotContain("management-grid", management);
    }

    [Fact]
    public void DevelopmentGuidanceReferencesTheSingleRevisedStandard()
    {
        string root = FindRepositoryRoot();
        string readme = Read(root, "01_CORE", "README.md");
        Assert.Contains("04_Document/01_UI_UX/BARANG_BACKOFFICE_UI_UX_STANDARD_REVISED.md", readme);
        Assert.Contains("shared BARANG", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SharedLayoutRulesEnforceHorizontalToolbarsSafeModalsMirroredTreesAndCleanHeader()
    {
        string root = FindRepositoryRoot();
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "barang-design-system.css");
        string shell = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Layout", "MainLayout.razor");
        string tree = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrganizationalChartTree.razor");
        string persons = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Persons.razor");
        string standard = Read(root, "04_Document", "01_UI_UX", "BARANG_BACKOFFICE_UI_UX_STANDARD_REVISED.md");

        Assert.Contains("flex-wrap: nowrap", css);
        Assert.Contains("--barang-toolbar-gap: 10px", css);
        Assert.Contains("--barang-modal-body-padding-inline: 22px", css);
        Assert.Contains("--barang-form-section-padding: 16px", css);
        Assert.Contains("[dir=\"rtl\"] .org-expander[aria-expanded=\"false\"]", css);
        Assert.Contains("justify-content: flex-end", css);
        Assert.Contains(".search-box > .barang-icon { display: none; }", css);
        Assert.Contains(".search-box input { padding-inline: 11px; }", css);
        Assert.DoesNotContain("sidebar-collapse-toggle", shell);
        Assert.DoesNotContain("sidebar-expand-toggle", shell);
        Assert.DoesNotContain("shell-location", shell);
        Assert.Contains("org-disclosure-icon", tree);
        Assert.Contains("class=\"crud-form", persons);

        Assert.Contains("MUST remain on one horizontal row", standard);
        Assert.Contains("Modal and form safe spacing is mandatory", standard);
        Assert.Contains("MUST mirror between RTL and LTR", standard);
        Assert.Contains("reversible collapse/expand control", standard);
        Assert.Contains("MUST be solved in these shared foundations", standard);
    }

    [Fact]
    public void SortHeadersAndActionIconsUseCentralAccessibleSemanticComponents()
    {
        string root = FindRepositoryRoot();
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "barang-design-system.css");
        string iconButton = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "BarangIconButton.razor");
        string sortHeader = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "BarangSortHeader.razor");
        string customers = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "CustomerManager.razor");
        string users = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "UserAccessManagement.razor");
        string chart = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrganizationalChartTree.razor");

        Assert.Contains("--barang-action-size: 36px", css);
        Assert.Contains("--barang-action-icon-size: 18px", css);
        Assert.Contains(".barang-sort-header", css);
        Assert.Contains("background: transparent; border: 0", css);
        Assert.Contains("aria-label=\"@Label\"", iconButton);
        Assert.Contains("title=\"@Label\"", iconButton);
        Assert.Contains("\"trash\" or \"unlink\" => \"danger\"", iconButton);
        Assert.Contains("arrow-up", sortHeader);
        Assert.Contains("arrow-down", sortHeader);
        Assert.Contains("<BarangSortHeader", customers);
        Assert.Contains("<BarangSortHeader", users);
        Assert.Contains("<BarangIconButton", chart);
        Assert.DoesNotContain("class=\"icon-action\"", customers);
        Assert.DoesNotContain("class=\"node-action\"", chart);
    }

    [Fact]
    public void DesignBankEditorIsOneScrollableLocalizedPageWithoutSideTabs()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "DesignBank.razor");
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "barang-design-system.css");

        Assert.DoesNotContain("<BarangTabs", page);
        Assert.DoesNotContain("design-editor-layout", page);
        Assert.DoesNotContain("Tab==", page);
        Assert.Contains("design-editor-single-page", page);
        Assert.Contains("@D[\"General\"]", page);
        Assert.Contains("@D[\"Classification\"]", page);
        Assert.Contains("@D[\"Files\"]", page);
        Assert.Contains("@D[\"History\"]", page);
        Assert.Contains("@D[\"Event\"]", page);
        Assert.Contains("@D[\"OccurredAt\"]", page);
        Assert.Contains("@D[\"Actor\"]", page);
        Assert.Contains("@D[\"Details\"]", page);
        Assert.Contains("@onclick=\"Save\"", page);
        Assert.Contains("disabled=\"@Saving\"", page);
        Assert.Contains("CloseConfirmOpen", page);
        Assert.Contains("accept=\"image/*\"", page);
        Assert.Contains("design-editor-single-page", css);
        Assert.Contains("overflow-y: auto", css);
        Assert.Contains("design-editor-modal", css);
    }

    private static string[] Keys(string file) => XDocument.Load(file).Root!
        .Elements("data")
        .Select(element => (string)element.Attribute("name")!)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static string Read(string root, params string[] segments) => File.ReadAllText(Path.Combine([root, .. segments]));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
