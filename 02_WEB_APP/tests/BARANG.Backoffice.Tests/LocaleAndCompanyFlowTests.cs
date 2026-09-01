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

    [Fact]
    public void CentralDateServiceRoundTripsJalaliAndGregorian()
    {
        LocaleDateService dates=new();DateOnly canonical=new(2026,3,21);
        Assert.Equal("1405/01/01",dates.Format(canonical,"fa"));Assert.Equal("2026/03/21",dates.Format(canonical,"en"));
        Assert.True(dates.TryParse("1405/01/01","fa",out DateOnly parsed));Assert.Equal(canonical,parsed);
    }
}
