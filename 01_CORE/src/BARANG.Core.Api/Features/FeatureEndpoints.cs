using BARANG.Core.Application.Features;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Features;

/// <summary>Endpointهای فیچرهای سیستم (منوی «فیچرهای سیستم»):
/// GET/PUT /api/v1/features برای پنل مدیریت (مجوز features.manage) و
/// GET /api/v1/public/features/{scope} ناشناس با کش ۱۰ ثانیه‌ای برای مصرف پنل/فروشگاه بدون ورود.</summary>
public static class FeatureEndpoints
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder features = endpoints.MapGroup("/api/v1/features").RequireAuthorization();
        features.MapGet("/", async (string? scope, string? search, IFeatureAdministration admin, CancellationToken ct) =>
            Map(await admin.ListAsync(scope, search, ct))).RequireAuthorization(SecurityPermissions.FeaturesManage);
        features.MapPut("/{id:guid}", async (Guid id, ToggleFeatureRequest request, IFeatureAdministration admin, CancellationToken ct) =>
            Map(await admin.SetEnabledAsync(new(id, request.IsEnabled, request.Version), ct))).RequireAuthorization(SecurityPermissions.FeaturesManage);

        // عمومی/نیمه‌عمومی: ناشناس + کش کوتاه — پنل و فروشگاه بدون ورود وضعیت فیچرها را می‌خوانند
        RouteGroupBuilder pub = endpoints.MapGroup("/api/v1/public/features").RequireRateLimiting("client-read").CacheOutput("public-site");
        pub.MapGet("/{scope}", async (string scope, IConfiguration configuration, CompanyContext companies, IFeatureAdministration admin, CancellationToken ct) =>
        {
            Guid? companyId = ResolvePublicCompany(configuration);
            if (companyId is null) return Results.StatusCode(503);
            using IDisposable _ = companies.BeginScope(companyId.Value);
            Result<IReadOnlyList<FeatureFlagView>> result = await admin.ListPublicAsync(scope, ct);
            return result.IsSuccess
                ? Results.Json(result.Value)
                : Results.Json(new { error = result.Error.Code, description = result.Error.Description }, statusCode: StatusCodes.Status400BadRequest);
        });
        return endpoints;
    }

    private static Guid? ResolvePublicCompany(IConfiguration configuration)
        => configuration.GetValue<string?>("Catalog:PublicCompanyId") is { } raw && Guid.TryParse(raw, out Guid id) && id != Guid.Empty ? id : null;

    private static FeatureResult<T> Map<T>(Result<T> result) => new(result);

    private sealed class FeatureResult<T>(Result<T> result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
            object payload = result.IsSuccess
                ? result.Value!
                : new { error = result.Error.Code, description = result.Error.Description };
            return httpContext.Response.WriteAsJsonAsync(payload, httpContext.RequestAborted);
        }
    }
}

public sealed record ToggleFeatureRequest(bool IsEnabled, long Version);
