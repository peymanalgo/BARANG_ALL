using BARANG.Backoffice.Auth;
using BARANG.Backoffice.Localization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BARANG.Backoffice.Tests;

public sealed class LocaleAndCompanyFlowTests
{
    [Theory]
    [InlineData("fa", "fa", "rtl", "Jalali")]
    [InlineData("fa-IR", "fa", "rtl", "Jalali")]
    [InlineData("en", "en", "ltr", "Gregorian")]
    [InlineData("ar", "ar", "rtl", "Gregorian")]
    [InlineData("unknown", "fa", "rtl", "Jalali")]
    public void LocaleMappingIsStable(string input, string code, string direction, string calendar)
    {
        LocaleDefinition actual = LocalePolicy.Resolve(input);
        Assert.Equal(code, actual.Code); Assert.Equal(direction, actual.Direction); Assert.Equal(calendar, actual.DateCalendar);
    }

    [Theory]
    [InlineData(0, CompanyFlow.NoAccess)]
    [InlineData(1, CompanyFlow.AutoSelect)]
    [InlineData(2, CompanyFlow.RequireSelection)]
    public void CompanyFlowMatchesMembershipCount(int count, CompanyFlow expected) => Assert.Equal(expected, CompanyFlowResolver.Resolve(count));

    [Fact]public void SystemAdministratorWithoutActiveCompanyUsesRecoveryFlow()=>Assert.Equal(CompanyFlow.Recovery,CompanyFlowResolver.Resolve(0,true));

    [Fact]
    public void CentralDateServiceRoundTripsJalaliAndGregorian()
    {
        LocaleDateService dates=new();DateOnly canonical=new(2026,3,21);
        Assert.Equal("1405/01/01",dates.Format(canonical,"fa"));Assert.Equal("2026/03/21",dates.Format(canonical,"en"));
        Assert.True(dates.TryParse("1405/01/01","fa",out DateOnly parsed));Assert.Equal(canonical,parsed);
    }

    [Fact]
    public void ExplicitBusinessExamplePreservesCanonicalDateAcrossLanguages()
    {
        LocaleDateService dates=new();
        Assert.True(dates.TryParse("1364/03/18","fa",out DateOnly canonical));
        Assert.Equal(new DateOnly(1985,6,8),canonical);
        Assert.Equal("1985/06/08",dates.Format(canonical,"en"));
        Assert.Equal("1985/06/08",dates.Format(canonical,"ar"));
        Assert.Equal("1364/03/18",dates.Format(canonical,"fa"));
    }

    [Fact]
    public void InvalidJalaliDateIsRejectedWithoutNormalization()
    {
        LocaleDateService dates=new();
        Assert.False(dates.TryParse("1405/13/01","fa",out DateOnly parsed));
        Assert.Equal(default,parsed);
    }

    [Fact]
    public void CentralCalendarOperationsPreserveCanonicalDateAcrossLanguageSwitches()
    {
        LocaleDateService dates=new();
        DateOnly canonical=dates.FromCalendar(1405,7,18,"fa");
        Assert.Equal("1405/07/18",dates.Format(canonical,"fa"));
        Assert.Equal("2026/10/10",dates.Format(canonical,"en"));
        Assert.Equal("2026/10/10",dates.Format(canonical,"ar"));
        Assert.Equal(canonical,dates.FromCalendar(2026,10,10,"en"));
    }

    [Fact]
    public void CurrentOrderBusinessDateRoundTripsAcrossAllSupportedLanguages()
    {
        LocaleDateService dates=new();
        Assert.True(dates.TryParse("1405/07/18","fa",out DateOnly canonical));
        Assert.Equal(new DateOnly(2026,10,10),canonical);
        Assert.Equal("2026/10/10",dates.Format(canonical,"en"));
        Assert.Equal("2026/10/10",dates.Format(canonical,"ar"));
        Assert.Equal("1405/07/18",dates.Format(canonical,"fa"));
        Assert.Equal("2026-10-10", dates.Canonical(canonical));
    }

    [Fact]
    public void OrderWizardUsesInvariantCanonicalDateSerialization()
    {
        string root = FindRepositoryRoot();
        string wizard = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        Assert.Contains("orderDate=Dates.Canonical(od)", wizard, StringComparison.Ordinal);
        Assert.DoesNotContain("orderDate=od.ToString(\"yyyy-MM-dd\")", wizard, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("۱۴۰۵/۰۷/۱۸","fa")]
    [InlineData("١٤٠٥/٠٧/١٨","fa")]
    public void LocaleDateServiceNormalizesSupportedLocalizedDigits(string value,string culture)
    {
        LocaleDateService dates=new();
        Assert.True(dates.TryParse(value,culture,out DateOnly canonical));
        Assert.Equal(new DateOnly(2026,10,10),canonical);
    }

    [Fact]
    public void DatePickerUsesCentralDateEngineInsteadOfPageLocalCalendarLogic()
    {
        string root=FindRepositoryRoot();
        string picker=File.ReadAllText(Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Components","LocaleDateInput.razor"));
        string pricing=File.ReadAllText(Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Components","Pages","PricingWorkspace.razor"));
        Assert.DoesNotContain("PersianCalendar",picker,StringComparison.Ordinal);
        Assert.Contains("<LocaleDateInput",pricing,StringComparison.Ordinal);
    }

    [Fact]
    public void BackofficeHasNoCompetingUserFacingCalendarImplementation()
    {
        string root=FindRepositoryRoot();
        string components=Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Components");
        string[] sources=Directory.EnumerateFiles(components,"*.razor",SearchOption.AllDirectories).ToArray();
        Assert.DoesNotContain(sources, path => File.ReadAllText(path).Contains("PersianCalendar",StringComparison.Ordinal));
        Assert.DoesNotContain(sources, path => Regex.IsMatch(File.ReadAllText(path), "<input\\s+[^>]*type=\\\"(?:date|datetime-local)\\\"", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void BackofficeHasNoCultureSensitiveCanonicalDateParsing()
    {
        string root = FindRepositoryRoot();
        string components = Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components");
        string[] sources = Directory.EnumerateFiles(components, "*.razor", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(components, "*.cs", SearchOption.AllDirectories))
            .ToArray();

        Assert.DoesNotContain(sources, path => File.ReadLines(path).Any(line =>
            Regex.IsMatch(line, @"DateOnly\.(?:TryParse|Parse)\(", RegexOptions.CultureInvariant) &&
            !line.Contains("CultureInfo.InvariantCulture", StringComparison.Ordinal)));
    }

    [Fact]
    public void ProductPricingGridParsesApiIsoDatesAsCanonicalBeforeLocalization()
    {
        LocaleDateService dates=new();
        Assert.True(dates.TryParseCanonical("2026-09-26",out DateOnly canonical));
        Assert.Equal("1405/07/04",dates.Format(canonical,"fa"));
        string root=FindRepositoryRoot();
        string product=File.ReadAllText(Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Components","Pages","ProductManager.razor"));
        Assert.Contains("Dates.TryParseCanonical",product,StringComparison.Ordinal);
        Assert.DoesNotContain("DateOnly.TryParse(Text(row",product,StringComparison.Ordinal);
        Assert.Contains("VariantCaption(Text(p,\"productVariantId\"))",product,StringComparison.Ordinal);
        Assert.DoesNotContain("@Text(p,\"productVariantId\")</td>",product,StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1399/12/30","2021/03/20")]
    [InlineData("1400/01/01","2021/03/21")]
    [InlineData("1403/12/30","2025/03/20")]
    public void JalaliLeapAndYearBoundariesRoundTrip(string jalali,string gregorian)
    {
        LocaleDateService dates=new();Assert.True(dates.TryParse(jalali,"fa",out DateOnly canonical));Assert.Equal(gregorian,dates.Format(canonical,"en"));Assert.True(dates.TryParse(gregorian,"ar",out DateOnly fromArabic));Assert.Equal(canonical,fromArabic);Assert.Equal(jalali,dates.Format(fromArabic,"fa"));
    }

    [Fact]public void RecoveryClaimIsSeparateFromCompanySelection(){Assert.NotEqual(AuthPolicies.CompanySelected,AuthPolicies.CompanyRecovery);Assert.NotEqual(AuthClaimTypes.CompanyId,AuthClaimTypes.SystemAdministrator);}

    [Fact]
    public void CompanySelectionSubmitsAndMapsEveryServerRejectionToLocalizedFeedback()
    {
        string root=FindRepositoryRoot();
        string page=File.ReadAllText(Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Components","Pages","Company.razor"));
        string endpoints=File.ReadAllText(Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Auth","BackofficeAuthEndpoints.cs"));
        Assert.Contains("type=\"submit\"",page);
        Assert.Contains("CompanyInactiveReason",page);
        Assert.Contains("CompanyNoAccessReason",page);
        Assert.Contains("CompanyRestrictedReason",page);
        Assert.Contains("security.company.inactive",endpoints);
        Assert.Contains("security.company_access.denied",endpoints);
        Assert.Contains("SelectCompanyAsync",endpoints);
    }

    [Fact]
    public void CrudLocalizationResourcesAreCompleteAndUniqueForAllSupportedLanguages()
    {
        string root=FindRepositoryRoot();string folder=Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Localization");
        string[] files=["CrudStrings.resx","CrudStrings.en.resx","CrudStrings.ar.resx"];
        string[][] keys=files.Select(file=>XDocument.Load(Path.Combine(folder,file)).Root!.Elements("data").Select(x=>(string)x.Attribute("name")!).ToArray()).ToArray();
        Assert.All(keys,x=>Assert.Equal(x.Length,x.Distinct(StringComparer.Ordinal).Count()));
        Assert.Equal(keys[0].Order(),keys[1].Order());Assert.Equal(keys[0].Order(),keys[2].Order());
    }

    [Fact]
    public void AppLocalizationResourcesHaveNoDuplicateKeysAndCoverInfrastructureStrings()
    {
        string root=FindRepositoryRoot();string folder=Path.Combine(root,"01_CORE","src","BARANG.Backoffice","Localization");
        string[] files=["AppStrings.resx","AppStrings.en.resx","AppStrings.ar.resx"];
        string[] required=["Module_foundation","Admin_settings","Admin_units","Admin_warehouses","Branch","ValueType","Parent","Classification","Text","Boolean","WholeNumber","DecimalNumber","RawMaterials","WorkInProcess","FinishedGoods","Shipping","Returns","Scrap","Choose","AuthoritativeCoreData"];
        foreach(string file in files){string[] keys=XDocument.Load(Path.Combine(folder,file)).Root!.Elements("data").Select(x=>(string)x.Attribute("name")!).ToArray();Assert.Equal(keys.Length,keys.Distinct(StringComparer.Ordinal).Count());Assert.All(required,key=>Assert.Contains(key,keys));}
    }

    private static string FindRepositoryRoot(){DirectoryInfo? current=new(AppContext.BaseDirectory);while(current is not null&&!Directory.Exists(Path.Combine(current.FullName,"01_CORE")))current=current.Parent;return current?.FullName??throw new DirectoryNotFoundException("Repository root not found.");}
}
