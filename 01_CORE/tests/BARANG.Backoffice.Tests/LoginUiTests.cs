using System.Security.Cryptography;
using System.Xml.Linq;

namespace BARANG.Backoffice.Tests;

public sealed class LoginUiTests
{
    [Fact]
    public void LoginRetainsTheAuthenticationFormContractAndSecurityAttributes()
    {
        string page = Read("01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Login.razor");

        Assert.Contains("method=\"post\" action=\"/auth/login\"", page);
        Assert.Contains("<AntiforgeryToken />", page);
        Assert.Contains("name=\"returnUrl\"", page);
        Assert.Contains("name=\"username\" autocomplete=\"username\"", page);
        Assert.Contains("name=\"password\" type=\"password\" autocomplete=\"current-password\"", page);
        Assert.Contains("class=\"primary-button login-submit\" type=\"submit\"", page);
        Assert.Contains("required autofocus", page);
        Assert.Contains("required />", page);
    }

    [Fact]
    public void LoginUsesTheReferenceVisualStructureWithoutForbiddenFeatures()
    {
        string page = Read("01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Login.razor");
        string css = Read("01_CORE", "src", "BARANG.Backoffice", "wwwroot", "login.css");

        Assert.Contains("<link rel=\"stylesheet\" href=\"/login.css\"", page);
        Assert.Contains("login-shell", page);
        Assert.Contains("login-brand-side", page);
        Assert.Contains("login-form-side", page);
        Assert.Contains("<LanguageSwitcher />", page);
        Assert.Contains("data-enhance-nav=\"false\"", page);
        Assert.Contains("grid-template-columns: .95fr 1.05fr", css);
        Assert.Contains("border-radius: 34px", css);
        Assert.Contains("height: 56px", css);
        Assert.Contains("height: 58px", css);
        Assert.Contains("@media (max-width: 900px)", css);
        Assert.DoesNotContain("smart assistant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("service-status", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Workspace", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoginUsesTheRequiredBackgroundAssetWithoutChangingIt()
    {
        string root = FindRepositoryRoot();
        byte[] deployed = File.ReadAllBytes(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "images", "login-background.png"));
        string css = Read("01_CORE", "src", "BARANG.Backoffice", "wwwroot", "login.css");

        Assert.Equal("8882C504A542E4DAF98D39382C0CAD67F9D9EF3CF615E75050659F9436064D2B", Convert.ToHexString(SHA256.HashData(deployed)));
        Assert.Contains("url('/images/login-background.png')", css);
    }

    [Fact]
    public void EveryVisibleLoginElementInheritsOneFontFamilyToken()
    {
        string css = Read("01_CORE", "src", "BARANG.Backoffice", "wwwroot", "login.css");
        string shared = Read("01_CORE", "src", "BARANG.Backoffice", "wwwroot", "barang-design-system.css");
        Assert.DoesNotContain("font-family", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--barang-font-fa: \"Vazirmatn\"", shared);
        Assert.Contains("--barang-font-ar: \"Noto Sans Arabic\"", shared);
        Assert.Contains("--barang-font-en: \"Inter\"", shared);
        Assert.Contains(":where(body *) { font-family: inherit; }", shared);
    }

    [Fact]
    public void LoginPresentationIsLocalizedForExactlyFaArAndEn()
    {
        string switcher = Read("01_CORE", "src", "BARANG.Backoffice", "Components", "LanguageSwitcher.razor");
        Assert.Contains("[(\"fa\", \"فارسی\"), (\"ar\", \"العربية\"), (\"en\", \"English\")]", switcher);

        string[] resourceFiles = ["AppStrings.resx", "AppStrings.ar.resx", "AppStrings.en.resx"];
        string[] requiredKeys = ["LoginWelcome", "LoginBrandTagline", "LoginBrandDescription", "UsernamePlaceholder", "PasswordPlaceholder", "LoginFooter"];
        foreach (string file in resourceFiles)
        {
            HashSet<string> keys = XDocument.Load(Path.Combine(FindRepositoryRoot(), "01_CORE", "src", "BARANG.Backoffice", "Localization", file))
                .Root!.Elements("data").Select(node => (string)node.Attribute("name")!).ToHashSet(StringComparer.Ordinal);
            Assert.All(requiredKeys, key => Assert.Contains(key, keys));
        }
    }

    private static string Read(params string[] segments) => File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. segments]));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
