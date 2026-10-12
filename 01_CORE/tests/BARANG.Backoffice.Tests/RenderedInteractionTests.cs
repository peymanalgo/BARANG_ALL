using System.Security.Claims;
using System.Text.Json;
using BARANG.Backoffice.Api;
using BARANG.Backoffice.Auth;
using BARANG.Backoffice.Components;
using BARANG.Backoffice.Components.Pages;
using BARANG.Backoffice.Localization;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace BARANG.Backoffice.Tests;

// Component-level bUnit coverage only. Deployed browser/circuit coverage lives in BARANG.Backoffice.E2ETests.
public sealed class ComponentInteractionTests
{
    private static readonly Guid CompanyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid UnitId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid PersonId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    [Fact]
    public void OrganizationalChartButtonsDispatchDomClicksAndMutateRenderedState()
    {
        using BunitContext context = CreateContext();
        IRenderedComponent<OrganizationalChart> rendered = context.Render<OrganizationalChart>();
        rendered.WaitForElement(".company-node");

        rendered.Find(".company-node .org-expander").Click();
        rendered.Find(".branch-node .org-expander").Click();
        rendered.Find(".unit-node .org-expander").Click();

        rendered.Find("button[title='Add Branch']").Click();
        Assert.Contains("Branch", rendered.Find(".org-dialog h2").TextContent);
        rendered.Find(".org-dialog .dialog-close").Click();

        rendered.Find("button[title='Add Person']").Click();
        Assert.Contains("Assignment", rendered.Find(".org-dialog h2").TextContent);
        rendered.Find(".org-dialog .dialog-close").Click();

        rendered.Find(".company-node button[title='View']").Click();
        Assert.NotNull(rendered.Find(".org-dialog"));
        rendered.Find(".org-dialog footer .btn-primary").Click();
        Assert.Contains(rendered.FindAll(".org-dialog input"), input => !input.HasAttribute("disabled"));
    }

    [Fact]
    public void PersonsButtonsAndTabsDispatchDomClicksAndMutateRenderedState()
    {
        using BunitContext listContext = CreateContext();
        IRenderedComponent<Persons> list = listContext.Render<Persons>();
        list.WaitForElement(".data-grid");
        list.Find(".crud-header button").Click();
        Assert.NotNull(list.Find(".dialog-backdrop"));
        list.Find(".crud-dialog footer .btn-secondary").Click();
        Assert.Empty(list.FindAll(".dialog-backdrop"));

        using BunitContext detailContext = CreateContext();
        IRenderedComponent<Persons> detail = detailContext.Render<Persons>(parameters => parameters.Add(x => x.Id, PersonId));
        detail.WaitForElement(".tabs");
        var tabs = detail.FindAll(".tabs button");
        tabs[1].Click();
        Assert.NotNull(detail.Find(".org-tree"));
        tabs = detail.FindAll(".tabs button");
        tabs[2].Click();
        Assert.Contains("User Information", detail.Find(".person-tab").TextContent);
    }

    [Fact]
    public void PersonEditCancelExitsTheEditorAndPersonModalHasNoRedundantCloseButton()
    {
        using BunitContext context = CreateContext();
        IRenderedComponent<Persons> detail = context.Render<Persons>(parameters => parameters.Add(x => x.Id, PersonId));
        detail.WaitForElement(".crud-form");
        detail.Find(".crud-form input:not([disabled])").Change("Unsaved value");
        detail.Find(".crud-form footer .btn-secondary").Click();
        Assert.Single(detail.FindAll(".barang-confirm-message"));
        detail.Find(".barang-modal.modal-small footer .btn-danger").Click();

        NavigationManager navigation = context.Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/master/persons", navigation.Uri, StringComparison.Ordinal);

        using BunitContext createContext = CreateContext();
        IRenderedComponent<Persons> create = createContext.Render<Persons>();
        create.WaitForElement(".data-grid");
        create.Find(".crud-header button").Click();
        Assert.Empty(create.FindAll(".crud-dialog > header .barang-icon-button"));
        Assert.Single(create.FindAll(".crud-form footer .btn-secondary"));
    }

    [Fact]
    public void BirthDateCalendarButtonOpensRenderedJalaliPicker()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fa");
            using BunitContext context = CreateContext();
            IRenderedComponent<Persons> rendered = context.Render<Persons>();
            rendered.WaitForElement(".data-grid");
            rendered.Find(".crud-header button").Click();
            rendered.Find(".date-picker-button").Click();
            Assert.NotNull(rendered.Find(".locale-calendar"));
            Assert.Contains("شمسی", rendered.Find(".locale-date-control input").GetAttribute("placeholder"));
            Assert.Equal(2, rendered.FindAll(".calendar-period-button").Count);
            rendered.FindAll(".calendar-period-button")[1].Click();
            Assert.Equal(12, rendered.FindAll(".calendar-years button").Count);
            rendered.Find(".calendar-years button").Click();
            Assert.Equal(12, rendered.FindAll(".calendar-months button").Count);
            rendered.Find(".calendar-months button").Click();
            Assert.Equal(42, rendered.FindAll(".calendar-days button").Count);
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    [Fact]
    public void ExistingCanonicalBirthDateRendersAsJalaliWithoutDrift()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fa");
            using BunitContext context = CreateContext();
            IRenderedComponent<LocaleDateInput> rendered = context.Render<LocaleDateInput>(parameters => parameters
                .Add(component => component.Value, new DateOnly(1985, 6, 8)));
            Assert.Equal("1364/03/18", rendered.Find(".locale-date-control input").GetAttribute("value"));
            rendered.Find(".date-picker-button").Click();
            Assert.Equal("1364", rendered.FindAll(".calendar-period-button")[1].TextContent);
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    [Fact]
    public void KnownSalesPriceCanonicalDateRendersAsExpectedJalali()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fa");
            using BunitContext context = CreateContext();
            IRenderedComponent<LocaleDateInput> rendered = context.Render<LocaleDateInput>(parameters => parameters
                .Add(component => component.Value, new DateOnly(2026, 9, 26)));

            Assert.Equal("1405/07/04", rendered.Find(".locale-date-control input").GetAttribute("value"));
            Assert.DoesNotContain("2647/11/22", rendered.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("2646/", rendered.Markup, StringComparison.Ordinal);
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    [Fact]
    public void NewOrderWizardOrderDateUsesJalaliPresentationForCanonicalToday()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fa");
            using BunitContext context = CreateContext();
            IRenderedComponent<OrderWizard> rendered = context.Render<OrderWizard>();
            rendered.WaitForElement("input[name='order-date']");

            Assert.Equal(new LocaleDateService().Format(DateOnly.FromDateTime(DateTime.Today), "fa"), rendered.Find("input[name='order-date']").GetAttribute("value"));
            rendered.Find("input[name='order-date']").ParentElement!.ParentElement!.QuerySelector(".date-picker-button")!.Click();
            Assert.Contains("1405", rendered.FindAll(".calendar-period-button")[1].TextContent);
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    private static BunitContext CreateContext()
    {
        BunitContext context = new();
        context.Services.AddLocalization();
        context.Services.AddSingleton<LocaleDateService>();
        context.Services.AddSingleton<ILocaleDateService>(services => services.GetRequiredService<LocaleDateService>());
        context.Services.AddSingleton<ICoreApiClient>(new FakeCoreApiClient());
        context.Services.AddSingleton<AuthenticationStateProvider>(new AuthState());
        return context;
    }

    private sealed class AuthState : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "tester"), new Claim(AuthClaimTypes.AccessToken, "token"), new Claim(AuthClaimTypes.CompanyId, CompanyId.ToString()),
            new Claim(AuthClaimTypes.SystemAdministrator, "true")
        ], "test"))));
    }

    private sealed class FakeCoreApiClient : ICoreApiClient
    {
        public Task<ApiResult<JsonElement>> GetAsync(string path, string token, Guid companyId, CancellationToken cancellationToken)
        {
            string json = path.StartsWith("api/v1/organization/chart", StringComparison.Ordinal) ? $$"""{"companies":[{"id":"{{CompanyId}}","code":"CO","displayName":"Company","legalName":"Company","isActive":true,"version":1}],"branches":[{"id":"{{BranchId}}","companyId":"{{CompanyId}}","code":"BR","displayName":"Branch","isActive":true,"version":1}],"units":[{"id":"{{UnitId}}","companyId":"{{CompanyId}}","branchId":"{{BranchId}}","parentId":null,"code":"UN","displayName":"Unit","isActive":true,"version":1}],"assignments":[]}""" : path.Contains(PersonId.ToString(), StringComparison.OrdinalIgnoreCase) ? $$"""{"id":"{{PersonId}}","personnelCode":"P1","firstName":"Test","lastName":"Person","displayName":"Test Person","nationalId":null,"birthDate":"1985-06-08","email":null,"phone":null,"address":null,"notes":null,"isActive":true,"version":1}""" : path.Contains("by-person", StringComparison.Ordinal) ? "{}" : $$"""[{"id":"{{PersonId}}","personnelCode":"P1","firstName":"Test","lastName":"Person","displayName":"Test Person","email":null,"phone":null,"isActive":true,"version":1}]""";
            return Task.FromResult(ApiResult<JsonElement>.Success(JsonDocument.Parse(json).RootElement.Clone()));
        }
        public Task<ApiResult<JsonElement>> SendAsync(HttpMethod method, string path, object? body, string token, Guid companyId, CancellationToken cancellationToken) => Task.FromResult(ApiResult<JsonElement>.Success(JsonDocument.Parse("{}").RootElement.Clone()));
        public Task<ApiResult<LoginResult>> LoginAsync(string username,string password,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<CurrentUserView>> GetMeAsync(string token,Guid? companyId,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<IReadOnlyList<MyCompanyView>>> GetMyCompaniesAsync(string token,CancellationToken cancellationToken)=>Task.FromResult(ApiResult<IReadOnlyList<MyCompanyView>>.Success([new(CompanyId,"CO","Company")])); public Task<ApiResult<CompanySelectionView>> SelectCompanyAsync(string token,Guid companyId,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<IReadOnlyList<RecoveryCompanyView>>> GetRecoveryCompaniesAsync(string token,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<RecoveryCompanyView>> ActivateRecoveryCompanyAsync(string token,Guid companyId,long version,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<bool>> LogoutAsync(string token,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<JsonElement>> UploadAsync(string path,string fileName,string contentType,Stream content,long length,string token,Guid companyId,CancellationToken cancellationToken)=>throw new NotSupportedException(); public Task<ApiResult<FileDownload>> DownloadAsync(string path,string token,Guid companyId,CancellationToken cancellationToken)=>throw new NotSupportedException();
    }
}
