using System.Security.Claims;
using BARANG.Backoffice.Api;
using BARANG.Backoffice.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Backoffice.Auth;

public static class BackofficeAuthEndpoints
{
    public static IEndpointRouteBuilder MapBackofficeAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync).AllowAnonymous();
        endpoints.MapPost("/auth/company", SelectCompanyAsync).RequireAuthorization();
        endpoints.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();
        endpoints.MapGet("/culture", SetCulture).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync([FromForm] LoginForm form, HttpContext http, ICoreApiClient core, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Username) || string.IsNullOrEmpty(form.Password)) return LoginFailure("validation");
        ApiResult<LoginResult> login = await core.LoginAsync(form.Username.Trim(), form.Password, ct);
        if (!login.IsSuccess || login.Value is null) return LoginFailure(Message(login.Failure));

        ApiResult<CurrentUserView> me = await core.GetMeAsync(login.Value.AccessToken, null, ct);
        ApiResult<IReadOnlyList<MyCompanyView>> companies = await core.GetMyCompaniesAsync(login.Value.AccessToken, ct);
        if (!me.IsSuccess || !companies.IsSuccess)
        {
            await core.LogoutAsync(login.Value.AccessToken, ct);
            return LoginFailure(Message(!me.IsSuccess ? me.Failure : companies.Failure));
        }

        CompanySelectionView? selected = null;
        if (CompanyFlowResolver.Resolve(companies.Value!.Count) == CompanyFlow.AutoSelect)
        {
            ApiResult<CompanySelectionView> selection = await core.SelectCompanyAsync(login.Value.AccessToken, companies.Value[0].CompanyId, ct);
            if (!selection.IsSuccess) return LoginFailure(Message(selection.Failure));
            selected = selection.Value;
        }

        await SignInAsync(http, login.Value, selected);
        string destination = selected is null ? "/company" : SafeReturnUrl(form.ReturnUrl);
        return Results.LocalRedirect(destination);
    }

    private static async Task<IResult> SelectCompanyAsync([FromForm] CompanyForm form, HttpContext http, ICoreApiClient core, CancellationToken ct)
    {
        string? token = http.User.FindFirstValue(AuthClaimTypes.AccessToken);
        if (string.IsNullOrEmpty(token)) return await ExpireAsync(http);
        ApiResult<IReadOnlyList<MyCompanyView>> companies = await core.GetMyCompaniesAsync(token, ct);
        if (companies.Failure == ApiFailure.Unauthorized) return await ExpireAsync(http);
        if (!companies.IsSuccess || companies.Value is null || !companies.Value.Any(c => c.CompanyId == form.CompanyId)) return Results.LocalRedirect("/company?message=forbidden");
        ApiResult<CompanySelectionView> selected = await core.SelectCompanyAsync(token, form.CompanyId, ct);
        if (selected.Failure == ApiFailure.Unauthorized) return await ExpireAsync(http);
        if (!selected.IsSuccess || selected.Value is null) return Results.LocalRedirect($"/company?message={Message(selected.Failure)}");

        LoginResult identity = new(token, GetExpiry(http.User), new UserView(
            Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            http.User.Identity?.Name ?? string.Empty, null,
            http.User.FindFirstValue(AuthClaimTypes.DisplayName) ?? string.Empty));
        await SignInAsync(http, identity, selected.Value);
        return Results.LocalRedirect("/");
    }

    private static async Task<IResult> LogoutAsync(HttpContext http, ICoreApiClient core, CancellationToken ct)
    {
        string? token = http.User.FindFirstValue(AuthClaimTypes.AccessToken);
        if (!string.IsNullOrEmpty(token)) await core.LogoutAsync(token, ct);
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/login?message=loggedout");
    }

    private static IResult SetCulture(string culture, string? returnUrl, HttpContext http)
    {
        LocaleDefinition locale = LocalePolicy.Resolve(culture);
        http.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(locale.Code)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = true, SameSite = SameSiteMode.Lax });
        return Results.LocalRedirect(SafeReturnUrl(returnUrl));
    }

    private static async Task SignInAsync(HttpContext http, LoginResult login, CompanySelectionView? company)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, login.User.Id.ToString()),
            new(ClaimTypes.Name, login.User.Username),
            new(AuthClaimTypes.DisplayName, login.User.DisplayName),
            new(AuthClaimTypes.AccessToken, login.AccessToken),
            new(ClaimTypes.Expiration, login.ExpiresAtUtc.ToUnixTimeSeconds().ToString())
        ];
        if (company is not null)
        {
            claims.Add(new(AuthClaimTypes.CompanyId, company.CompanyId.ToString()));
            claims.Add(new(AuthClaimTypes.CompanyName, company.DisplayName));
            claims.Add(new(AuthClaimTypes.CompanyCode, company.Code));
            claims.AddRange(company.EffectiveRoleCodes.Select(role => new Claim(ClaimTypes.Role, role)));
        }
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = login.ExpiresAtUtc, AllowRefresh = false });
    }

    private static DateTimeOffset GetExpiry(ClaimsPrincipal user) => DateTimeOffset.FromUnixTimeSeconds(long.Parse(user.FindFirstValue(ClaimTypes.Expiration)!));
    private static async Task<IResult> ExpireAsync(HttpContext http) { await http.SignOutAsync(); return Results.LocalRedirect("/login?message=expired"); }
    private static IResult LoginFailure(string message) => Results.LocalRedirect($"/login?message={message}");
    private static string SafeReturnUrl(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//") ? value : "/";
    private static string Message(ApiFailure failure) => failure switch { ApiFailure.InvalidCredentials => "invalid", ApiFailure.Unauthorized => "expired", ApiFailure.Forbidden => "unauthorized", ApiFailure.Validation => "validation", ApiFailure.Unavailable => "unavailable", _ => "error" };

    public sealed record LoginForm(string Username, string Password, string? ReturnUrl);
    public sealed record CompanyForm(Guid CompanyId);
}
