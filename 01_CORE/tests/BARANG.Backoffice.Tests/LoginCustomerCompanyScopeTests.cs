using System.Xml.Linq;

namespace BARANG.Backoffice.Tests;

public sealed class LoginCustomerCompanyScopeTests
{
    [Fact]
    public void CustomerGeneralUsesOneActionAreaAndAnAtomicStatusUpdate()
    {
        string editor = Read("01_CORE/src/BARANG.Backoffice/Components/CustomerEditor.razor");
        string owner = Read("01_CORE/src/BARANG.Backoffice/Components/Pages/CustomerManager.razor");
        string endpoint = Read("01_CORE/src/BARANG.Core.Api/Customers/CustomerEndpoints.cs");
        Assert.Contains("name=\"customer-status\"", editor);
        Assert.Contains("<textarea name=\"customer-notes\" rows=\"4\"", editor);
        Assert.Contains("data-customer-editor-actions=\"@Tab\"", editor);
        Assert.Contains("Closed.InvokeAsync()", editor);
        Assert.DoesNotContain("SaveStatus", editor);
        Assert.DoesNotContain("<Footer>", owner);
        Assert.Contains("await Load(); EditorOpen = false;", owner);
        Assert.Contains("r.Status is not null", endpoint);
        Assert.Contains("permissions.AuthorizeAsync(SecurityPermissions.CustomerStatusManage", endpoint);
    }

    [Fact]
    public void CompanySelectionRefreshesIdentityAndStartsANewScopedPage()
    {
        string endpoint = Read("01_CORE/src/BARANG.Backoffice/Auth/BackofficeAuthEndpoints.cs");
        string middleware = Read("01_CORE/src/BARANG.Backoffice/Auth/SessionValidationMiddleware.cs");
        Assert.Contains("core.GetMeAsync(token, selected.Value.CompanyId", endpoint);
        Assert.Contains("SignInAsync(http, identity, current.Value, selected.Value)", endpoint);
        Assert.Contains("SafeReturnUrl(form.ReturnUrl)", endpoint);
        Assert.Contains("selectingCompany ? null : companyId", middleware);
        Assert.Contains("CacheControl = \"no-store\"", middleware);
    }

    [Theory]
    [InlineData("", "سامانه جامع مرکزی شرکت برنگ")]
    [InlineData(".ar", "النظام المركزي المتكامل لشركة برنگ")]
    [InlineData(".en", "BARANG Company Integrated Central System")]
    public void ApprovedLoginCopyIsLocalized(string suffix, string title)
    {
        XDocument resources = XDocument.Parse(Read($"01_CORE/src/BARANG.Backoffice/Localization/AppStrings{suffix}.resx"));
        Dictionary<string,string> values = resources.Root!.Elements("data").ToDictionary(x => x.Attribute("name")!.Value, x => x.Element("value")!.Value);
        Assert.Equal(title, values["LoginBrandTagline"]);
        Assert.False(string.IsNullOrWhiteSpace(values["LoginCompanyName"]));
        Assert.False(string.IsNullOrWhiteSpace(values["LoginPlatformLabel"]));
        string page = Read("01_CORE/src/BARANG.Backoffice/Components/Pages/Login.razor");
        Assert.DoesNotContain("L[\"Backoffice\"]", page);
        Assert.DoesNotContain("<strong>BARANG</strong>", page);
    }

    private static string Read(string path)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while(root is not null && !Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        return File.ReadAllText(Path.Combine(root?.FullName ?? throw new DirectoryNotFoundException(),path));
    }
}
