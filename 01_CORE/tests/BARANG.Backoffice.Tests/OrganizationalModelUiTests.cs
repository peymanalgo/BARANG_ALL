using System.Xml.Linq;
using System.Reflection;
using System.Text.Json;
using BARANG.Backoffice.Components;

namespace BARANG.Backoffice.Tests;

public sealed class OrganizationalModelUiTests
{
    [Fact]
    public void ChartParserAcceptsNumericPositionAssignmentStatusFromApi()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {"companies":[],"branches":[],"units":[],"assignments":[],"positions":[
              {"id":"11111111-1111-1111-1111-111111111111","organizationalUnitId":"22222222-2222-2222-2222-222222222222","reportsToPositionId":null,"code":"POS","title":"Position","isActive":true,"version":1,
               "incumbents":[{"assignmentId":"33333333-3333-3333-3333-333333333333","personId":"44444444-4444-4444-4444-444444444444","personName":"Person","startDate":null,"endDate":null,"isPrimary":true,"status":0,"version":1}]}]}
            """);

        MethodInfo parser = typeof(OrganizationChartJson).GetMethod("ParseChart", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        OrganizationChartData result = (OrganizationChartData)parser.Invoke(null, [document.RootElement])!;

        PositionRow position = Assert.Single(result.Positions!);
        PositionIncumbentRow incumbent = Assert.Single(position.Incumbents);
        Assert.Equal("0", incumbent.Status);
    }

    [Fact]
    public void PersonPageHasExactlyFourPrimaryTabsAndNoPasswordExposure()
    {
        string text = File.ReadAllText(Path.Combine(Root(), "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Persons.razor"));
        Assert.Contains("(\"full\", \"FullInformation\")", text);
        Assert.Contains("(\"chart\", \"OrganizationalChart\")", text);
        Assert.Contains("(\"user\", \"UserInformation\")", text);
        Assert.Contains("(\"roles\", \"RolesPermissions\")", text);
        Assert.DoesNotContain("PasswordHash", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReadOnly", text);
    }

    [Fact]
    public void ChartAndPersonResourcesMatchInAllLanguages()
    {
        string directory = Path.Combine(Root(), "01_CORE", "src", "BARANG.Backoffice", "Localization");
        var keys = new[]
        {
            "OrganizationStrings.resx",
            "OrganizationStrings.en.resx",
            "OrganizationStrings.ar.resx"
        }.Select(x => XDocument.Load(Path.Combine(directory, x)).Root!.Elements("data").Select(e => (string)e.Attribute("name")!).ToArray()).ToArray();

        foreach (var language in keys)
        {
            Assert.Equal(language.Length, language.Distinct().Count());
        }

        Assert.Equal(keys[0].ToHashSet(), keys[1].ToHashSet());
        Assert.Equal(keys[0].ToHashSet(), keys[2].ToHashSet());
    }

    [Fact]
    public void Every_chart_icon_caption_is_translated_in_fa_en_and_ar()
    {
        string directory = Path.Combine(Root(), "01_CORE", "src", "BARANG.Backoffice", "Localization");
        string[] files = ["OrganizationStrings.resx", "OrganizationStrings.en.resx", "OrganizationStrings.ar.resx"];
        string[] actionKeys =
        [
            "View", "Edit", "AddBranch", "AddUnit", "AddPerson", "AddChildUnit", "AddSubordinate",
            "Move", "ChangeManager", "Activate", "Deactivate", "Delete", "RemoveAssignment",
            "Expand", "Collapse", "More", "ViewPerson"
        ];

        foreach (string file in files)
        {
            Dictionary<string, string> resources = XDocument.Load(Path.Combine(directory, file)).Root!
                .Elements("data")
                .ToDictionary(element => (string)element.Attribute("name")!, element => element.Element("value")?.Value.Trim() ?? "");

            foreach (string key in actionKeys)
            {
                Assert.True(resources.TryGetValue(key, out string? value), $"{file} is missing {key}.");
                Assert.False(string.IsNullOrWhiteSpace(value), $"{file}:{key} is blank.");
                if (!file.EndsWith(".en.resx", StringComparison.Ordinal))
                {
                    Assert.NotEqual(key, value);
                }
            }
        }

        Dictionary<string, string> persian = XDocument.Load(Path.Combine(directory, "OrganizationStrings.resx")).Root!
            .Elements("data")
            .ToDictionary(element => (string)element.Attribute("name")!, element => element.Element("value")?.Value.Trim() ?? "");
        Assert.DoesNotContain(new[] { "Move", "Edit", "Delete", "More" }, english => persian.Values.Contains(english, StringComparer.Ordinal));
    }

    [Fact]
    public void SharedChartIsUsedByMainPageAndPersonContext()
    {
        string root = Root();
        string main = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrganizationalChart.razor"));
        string people = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Persons.razor"));

        Assert.Contains("<OrganizationalChartTree", main);
        Assert.Contains("<ReadOnlyOrganizationalChart", people);
        Assert.Contains("HighlightPersonId", people);
    }

    [Fact]
    public void ChartOwnsDirectFormsAndNeverLinksToLegacyCompanyGrid()
    {
        string root = Root();
        string chart = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrganizationalChartTree.razor"));
        string page = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrganizationalChart.razor"));

        Assert.DoesNotContain("<CompanyForm", chart);
        Assert.DoesNotContain("<BranchForm", chart);
        Assert.DoesNotContain("<UnitForm", chart);
        Assert.DoesNotContain("CreateBranch", chart);
        Assert.DoesNotContain("CreateUnit", chart);
        Assert.DoesNotContain("CreateAssignment", chart);
        Assert.DoesNotContain("/manage/companies", chart + page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ActionRequested", chart);
    }

    [Fact]
    public void CompanyFormContainsFullBusinessSections()
    {
        string form = File.ReadAllText(Path.Combine(Root(), "01_CORE", "src", "BARANG.Backoffice", "Components", "CompanyForm.razor"));
        foreach (string field in new[]
                 {
                     "LegalName",
                     "DisplayName",
                     "NationalId",
                     "RegistrationNumber",
                     "EconomicCode",
                     "TaxId",
                     "Phone",
                     "Mobile",
                     "Email",
                     "Website",
                     "CountryCode",
                     "Province",
                     "City",
                     "Address",
                     "PostalCode",
                     "DefaultCurrency"
                 })
        {
            Assert.Contains(field, form);
        }

        Assert.Contains("FormMode.View", form);
    }

    [Fact]
    public void UserWorkflowUsesNewPasswordsOnlyAndEffectiveAccessIsReadOnly()
    {
        string page = File.ReadAllText(Path.Combine(Root(), "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Persons.razor"));
        Assert.Contains("CreateAccount", page);
        Assert.Contains("ResetPassword", page);
        Assert.Contains("effectivePermissions", page);
        Assert.Contains("ReadOnly", page);
        Assert.Contains("type=\"password\"", page);
        Assert.DoesNotContain("PasswordHash", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersonAndAssignmentDatesUseSharedLocalePicker()
    {
        string root = Root();
        string person = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Persons.razor"));
        string assignmentEditor = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrganizationalAssignmentEditor.razor"));

        Assert.Contains("@bind-Value=\"Birth\"", person);
        Assert.Equal(2, assignmentEditor.Split("<LocaleDateInput", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("placeholder=\"yyyy/mm/dd\"", person);
    }

    private static string Root()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException();
    }
}
