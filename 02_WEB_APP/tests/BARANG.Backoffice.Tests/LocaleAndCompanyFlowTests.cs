using BARANG.Backoffice.Auth;
using BARANG.Backoffice.Localization;

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
}
