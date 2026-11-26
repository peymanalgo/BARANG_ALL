using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BARANG.Backoffice.Tests;

public sealed class UserAccessManagementUiTests
{
    private static readonly string Root = FindRepositoryRoot();
    private static readonly string Page = File.ReadAllText(Path.Combine(Root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "UserAccessManagement.razor"));

    [Fact]
    public void IntegratedPageHasUserAccessTabsAndInteractiveServer()
    {
        Assert.Contains("@page \"/admin/user-access\"", Page);
        Assert.Contains("@rendermode InteractiveServer", Page);
        Assert.Contains("Authorize(Policy = AuthPolicies.UserAccessManagement)", Page);
        Assert.Contains("RequireClaim(AuthClaimTypes.Permission, \"security.user.read\")", File.ReadAllText(Path.Combine(Root, "01_CORE", "src", "BARANG.Backoffice", "Program.cs")));
        Assert.Equal(4, Regex.Matches(Page, "role=\\\"tab\\\"").Count);
        Assert.Contains("ActiveTab == \"users\"", Page);
        Assert.Contains("ActiveTab == \"roles\"", Page);
        Assert.Contains("ActiveTab == \"access\"", Page);
        Assert.Contains("ActiveTab == \"substitutions\"", Page);
    }

    [Fact]
    public void UsersWorkflowUsesPersonMasterDataAndNeverExposesPasswordHashes()
    {
        Assert.Contains("person-candidates", Page);
        Assert.Contains("<SearchableSelect Options=\"PersonOptions\"", Page);
        Assert.Contains("PersonCard", Page);
        Assert.Contains("UserAccountInformation", Page);
        Assert.Contains("type=\"password\"", Page);
        Assert.Contains("autocomplete=\"new-password\"", Page);
        Assert.DoesNotContain("passwordHash", Page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersonSelectionIsStableUntilExplicitlyChanged()
    {
        Assert.Contains("if (SelectedPerson is null)", Page);
        Assert.Contains("ChangePerson", Page);
        Assert.Contains("if (SelectedPerson is not null) return;", Page);
        Assert.Contains("autocomplete=\"username\"", Page);
    }

    [Fact]
    public void UserAccountEditIsDistinctAuthorizedAndPersistsOnlySupportedAccountFields()
    {
        Assert.Contains("OpenUser(row.Id, true)", Page);
        Assert.Contains("CanEditUser", Page);
        Assert.Contains("security.user.edit", Page);
        Assert.Contains("security.user.status.manage", Page);
        Assert.Contains("SaveUserAccount", Page);
        Assert.Contains("/account", Page);
        Assert.Contains("/activation", Page);
        Assert.Contains("EditUsername", Page);
        Assert.Contains("EditEnabled", Page);
        Assert.Contains("@PersonCard(SelectedUser.Person)", Page);
        Assert.DoesNotContain("passwordHash", Page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UsersGridHasDesktopFitSizingAndCompactActionColumn()
    {
        string css = File.ReadAllText(Path.Combine(Root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "barang-design-system.css"));
        Assert.Contains("uam-users-grid-wrap{overflow-x:hidden}", css);
        Assert.Contains("table-layout:fixed", css);
        Assert.Contains(".uam-col-actions{width:78px}", css);
        Assert.Contains("@media(max-width:900px)", css);
        Assert.Contains(".uam-users-grid-wrap{overflow-x:auto}", css);
    }

    [Fact]
    public void AccessWorkspaceUsesCatalogFourCrudColumnsAndExplicitBulkSave()
    {
        Assert.Contains("api/v1/security/permissions", Page);
        Assert.Contains("CrudCapabilities = [\"View\", \"Create\", \"Edit\", \"Delete\"]", Page);
        Assert.Contains("SpecialPermissions", Page);
        Assert.Contains("Direct", Page);
        Assert.Contains("Inherited", Page);
        Assert.Contains("Effective", Page);
        Assert.Contains("PermissionDirty", Page);
        Assert.Contains("SavePermissions", Page);
        Assert.Contains("SetModule", Page);
        Assert.Contains("ExpandAll", Page);
        Assert.Contains("CollapseAll", Page);
    }

    [Fact]
    public void AllIntroducedEntitySelectorsUseSharedSearchableSelector()
    {
        Assert.True(Regex.Matches(Page, "<SearchableSelect").Count >= 6);
        Assert.Contains("SearchPersons", Page);
        Assert.Contains("SearchAccessRoles", Page);
        Assert.Contains("SearchAccessUsers", Page);
        Assert.Contains("SearchAssignmentRoles", Page);
    }

    [Fact]
    public void OldFragmentedSecurityRoutesAndMenuResourcesAreGone()
    {
        string pages = Path.Combine(Root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages");
        Assert.False(File.Exists(Path.Combine(pages, "SecurityManager.razor")));
        Assert.False(File.Exists(Path.Combine(pages, "UserAccessManager.razor")));
        Assert.False(File.Exists(Path.Combine(pages, "RolePermissionManager.razor")));
        foreach (string resource in Directory.GetFiles(Path.Combine(Root, "01_CORE", "src", "BARANG.Backoffice", "Localization"), "NavigationStrings*.resx"))
        {
            string[] keys = XDocument.Load(resource).Root!.Elements("data").Select(x => (string)x.Attribute("name")!).ToArray();
            Assert.Contains("Nav_UserAccessManagement", keys);
            Assert.DoesNotContain("Nav_Security", keys);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException();
    }
}
