namespace BARANG.Backoffice.Tests;

public sealed class HomeWorkspaceTests
{
    [Fact]
    public void HomeUsesBoundedAuthoritativeWorkAndReferralQueries()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Home.razor");

        Assert.Contains("runtime/tasks/eligible", page);
        Assert.Contains("runtime/tasks/referrals", page);
        Assert.Contains("pageSize=5", page);
        Assert.Contains("OpenTask", page);
        Assert.Contains("OpenReferral", page);
        Assert.DoesNotContain("CompleteWorkflowTask", page);
        Assert.DoesNotContain("DecisionNote", page);
        Assert.DoesNotContain("ClaimTask", page);
    }

    [Fact]
    public void HomeUsesPermissionAwareNavigationAndOptionalExistingSources()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Home.razor");

        Assert.Contains("BuildQuickLinks", page);
        Assert.Contains("ModuleCatalog.IsVisible", page);
        Assert.Contains("audit.read", page);
        Assert.Contains("notification.read", page);
        Assert.Contains("Task.WhenAll", page);
        Assert.Contains("AuthClaimTypes.CompanyId", page);
    }

    [Fact]
    public void HomeHasLocalizedDateAndPersonalWorkspaceResources()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Home.razor");
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "home.css");

        Assert.Contains("ILocaleDateService", page);
        Assert.Contains("CultureInfo.CurrentUICulture", page);
        Assert.Contains("HomeStrings", page);
        Assert.Contains("home-columns", css);
        Assert.Contains("@media", css);
        Assert.True(File.Exists(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "HomeStrings.en.resx")));
        Assert.True(File.Exists(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "HomeStrings.ar.resx")));
    }

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine([root, .. parts]));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
