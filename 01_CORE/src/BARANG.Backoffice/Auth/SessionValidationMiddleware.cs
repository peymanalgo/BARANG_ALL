using System.Security.Claims;
using BARANG.Backoffice.Api;
using Microsoft.AspNetCore.Authentication;

namespace BARANG.Backoffice.Auth;

public sealed class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICoreApiClient core)
    {
        bool guardedPage = HttpMethods.IsGet(context.Request.Method) && (context.Request.Path == "/" || context.Request.Path == "/company" || context.Request.Path.StartsWithSegments("/recovery") || context.Request.Path.StartsWithSegments("/workspace") || context.Request.Path.StartsWithSegments("/orders") || context.Request.Path.StartsWithSegments("/manage") || context.Request.Path.StartsWithSegments("/master") || context.Request.Path.StartsWithSegments("/admin"));
        if (!guardedPage || context.User.Identity?.IsAuthenticated != true) { await next(context); return; }

        string? token = context.User.FindFirstValue(AuthClaimTypes.AccessToken);
        Guid? companyId = Guid.TryParse(context.User.FindFirstValue(AuthClaimTypes.CompanyId), out Guid parsed) ? parsed : null;
        if (string.IsNullOrEmpty(token)) { await RejectAsync(context, "expired"); return; }
        context.Response.Headers.CacheControl = "no-store";
        bool selectingCompany = context.Request.Path == "/company";
        ApiResult<CurrentUserView> result = await core.GetMeAsync(token, selectingCompany ? null : companyId, context.RequestAborted);
        if (result.Failure == ApiFailure.Unauthorized) { await RejectAsync(context, "expired"); return; }
        if (result.Failure == ApiFailure.Forbidden) { context.Response.Redirect("/company?message=forbidden"); return; }
        if (result.Failure == ApiFailure.NotFound && companyId is not null) { context.Response.Redirect("/company?message=inactive"); return; }
        if (!result.IsSuccess) { await RejectAsync(context, result.Failure == ApiFailure.Unavailable ? "unavailable" : "error"); return; }
        if(context.Request.Path.StartsWithSegments("/recovery")&&!result.Value!.IsSystemAdministrator){context.Response.Redirect("/company");return;}
        if (context.Request.Path == "/" && companyId is null) { context.Response.Redirect(result.Value!.IsSystemAdministrator?"/recovery/companies":"/company"); return; }
        await next(context);
    }

    private static async Task RejectAsync(HttpContext context, string message)
    {
        await context.SignOutAsync();
        context.Response.Redirect($"/login?message={message}");
    }
}
