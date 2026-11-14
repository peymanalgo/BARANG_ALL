using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Files;
using BARANG.Core.Api.Errors;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Application.SiteContent;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BARANG.Core.Api.SiteContent;

// ─── CMS سایت B2C — اندپوینت‌های مدیریتی (ERP) و عمومی (فروشگاه) ───
public static class SiteContentEndpoints
{
    public static IEndpointRouteBuilder MapSiteContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // ── مدیریتی: نیاز به مجوز b2c.site.read / b2c.site.manage ──
        RouteGroupBuilder admin = endpoints.MapGroup("/api/v1/site");
        RouteGroupBuilder read = admin.MapGroup("").RequireAuthorization(SecurityPermissions.B2CSiteRead);
        read.MapGet("/settings", async (ISiteContentAdministration s, CancellationToken ct) => Map(await s.GetSettingsAsync(ct)));
        read.MapGet("/pages", async (ISiteContentAdministration s, CancellationToken ct) => Map(await s.ListPagesAsync(ct)));
        read.MapGet("/pages/{id:guid}", async (Guid id, ISiteContentAdministration s, CancellationToken ct) => Map(await s.GetPageAsync(id, ct)));
        read.MapGet("/footer", async (ISiteContentAdministration s, CancellationToken ct) => Map(await s.ListFooterAsync(ct)));
        read.MapGet("/menu", async (ISiteContentAdministration s, CancellationToken ct) => Map(await s.ListMenuAsync(ct)));
        read.MapGet("/blog", async (int? page, int? pageSize, bool? includeUnpublished, ISiteContentAdministration s, CancellationToken ct) => Map(await s.ListBlogAsync(page ?? 1, pageSize ?? 20, includeUnpublished ?? false, ct)));
        read.MapGet("/blog/{id:guid}", async (Guid id, ISiteContentAdministration s, CancellationToken ct) => Map(await s.GetPostAsync(id, ct)));

        RouteGroupBuilder manage = admin.MapGroup("").RequireAuthorization(SecurityPermissions.B2CSiteManage).DisableAntiforgery();
        manage.MapPut("/settings", async (SiteSettingsCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.UpdateSettingsAsync(r, ct)));
        manage.MapPost("/pages", async (SitePageCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.CreatePageAsync(r, ct), true));
        manage.MapPut("/pages/{id:guid}", async (Guid id, SitePageCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.UpdatePageAsync(id, r, ct)));
        manage.MapDelete("/pages/{id:guid}", async (Guid id, ISiteContentAdministration s, CancellationToken ct) => MapVoid(await s.DeletePageAsync(id, ct)));
        manage.MapPost("/footer", async (FooterWidgetCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.CreateWidgetAsync(r, ct), true));
        manage.MapPut("/footer/{id:guid}", async (Guid id, FooterWidgetCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.UpdateWidgetAsync(id, r, ct)));
        manage.MapDelete("/footer/{id:guid}", async (Guid id, ISiteContentAdministration s, CancellationToken ct) => MapVoid(await s.DeleteWidgetAsync(id, ct)));
        manage.MapPost("/menu", async (MainMenuItemCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.CreateMenuItemAsync(r, ct), true));
        manage.MapDelete("/menu/{id:guid}", async (Guid id, ISiteContentAdministration s, CancellationToken ct) => MapVoid(await s.DeleteMenuItemAsync(id, ct)));
        manage.MapPost("/blog", async (BlogPostCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.CreatePostAsync(r, ct), true));
        manage.MapPut("/blog/{id:guid}", async (Guid id, BlogPostCommand r, ISiteContentAdministration s, CancellationToken ct) => Map(await s.UpdatePostAsync(id, r, ct)));
        manage.MapDelete("/blog/{id:guid}", async (Guid id, ISiteContentAdministration s, CancellationToken ct) => MapVoid(await s.DeletePostAsync(id, ct)));
        manage.MapPost("/images", async (HttpRequest request, ISiteContentAdministration s, CancellationToken ct) =>
        {
            IFormCollection form = await request.ReadFormAsync(ct);
            IFormFile? file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return Results.BadRequest(new { code = "site.upload_invalid" });
            await using Stream stream = file.OpenReadStream();
            return Map(await s.UploadImageAsync(stream, file.Length, file.ContentType, file.FileName, ct), true);
        });

        // ── عمومی: ناشناس + کش — مصرف مستقیم توسط B2C ──
        RouteGroupBuilder pub = endpoints.MapGroup("/api/v1/public/site").RequireRateLimiting("client-read").CacheOutput("public-catalog");
        pub.MapGet("/settings", async (string? culture, IConfiguration configuration, CompanyContext companies, ISiteContentAdministration s, CancellationToken ct)
            => await PublicAsync(companies, configuration, s, (admin, token) => admin.GetSettingsAsync(token), ct));
        pub.MapGet("/footer", async (string? culture, IConfiguration configuration, CompanyContext companies, ISiteContentAdministration s, CancellationToken ct)
            => await PublicAsync(companies, configuration, s, (admin, token) => admin.ListFooterAsync(token), ct));
        pub.MapGet("/menu", async (string? culture, IConfiguration configuration, CompanyContext companies, ISiteContentAdministration s, CancellationToken ct)
            => await PublicAsync(companies, configuration, s, (admin, token) => admin.ListMenuAsync(token), ct));
        pub.MapGet("/pages/{slug}", async (string slug, string? culture, IConfiguration configuration, CompanyContext companies, ITransactionExecutor tx, CoreDbContext db, ISiteContentAdministration s, IObjectStorage storage, CancellationToken ct) =>
        {
            string normalizedSlug = slug.Trim().ToLowerInvariant();
            Guid? companyId = ResolvePublicCompany(configuration);
            if (companyId is null) return Results.StatusCode(503);
            using IDisposable scope = companies.BeginScope(companyId.Value);
            await s.EnsureSeededAsync(ct);
            Result<Domain.SiteContent.SitePage?> page = await tx.ExecuteAsync(async t => Result.Success(await db.SitePages.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Slug == normalizedSlug && x.IsPublished, ct)), ct);
            return page.Value is null ? Results.NotFound() : Results.Json(new PublicPageView(page.Value.Slug, Pick(page.Value.TitleFa, page.Value.TitleEn, page.Value.TitleAr, culture), Pick(page.Value.BodyFa, page.Value.BodyEn, page.Value.BodyAr, culture), string.IsNullOrWhiteSpace(page.Value.CoverStorageKey) ? null : storage.GetPublicObjectUrl(page.Value.CoverStorageKey), PickNull(page.Value.SeoDescriptionFa, page.Value.SeoDescriptionEn, page.Value.SeoDescriptionAr, culture), JoinKeywords(page.Value, culture), page.Value.UpdatedAtUtc));
        });
        pub.MapGet("/blog", async (int? page, int? pageSize, string? culture, IConfiguration configuration, CompanyContext companies, ISiteContentAdministration s, CancellationToken ct)
            => await PublicAsync(companies, configuration, s, (admin, token) => admin.ListBlogAsync(page ?? 1, Math.Min(pageSize ?? 12, 24), false, token), ct));
        pub.MapGet("/blog/{slug}", async (string slug, string? culture, IConfiguration configuration, CompanyContext companies, ITransactionExecutor tx, CoreDbContext db, ISiteContentAdministration s, IObjectStorage storage, CancellationToken ct) =>
        {
            string normalizedSlug = slug.Trim().ToLowerInvariant();
            Guid? companyId = ResolvePublicCompany(configuration);
            if (companyId is null) return Results.StatusCode(503);
            using IDisposable scope = companies.BeginScope(companyId.Value);
            await s.EnsureSeededAsync(ct);
            Result<Domain.SiteContent.BlogPost?> post = await tx.ExecuteAsync(async t => Result.Success(await db.BlogPosts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Slug == normalizedSlug && x.IsPublished, ct)), ct);
            if (post.Value is null) return Results.NotFound();
            Domain.SiteContent.BlogPost v = post.Value;
            return Results.Json(new PublicPostView(v.Id, v.Slug, Pick(v.TitleFa, v.TitleEn, v.TitleAr, culture), Pick(v.ExcerptFa, v.ExcerptEn, v.ExcerptAr, culture), Pick(v.BodyFa, v.BodyEn, v.BodyAr, culture), string.IsNullOrWhiteSpace(v.ThumbnailStorageKey) ? null : storage.GetPublicObjectUrl(v.ThumbnailStorageKey), PickNull(v.SeoDescriptionFa, v.SeoDescriptionEn, v.SeoDescriptionAr, culture), JoinKeywords(v, culture), v.PublishedAtUtc));
        });
        return endpoints;
    }

    // اجرای یک فراخوانی ادمین در سوپ شرکت عمومی (RLS) + سیدر یک‌باره
    private static async Task<IResult> PublicAsync<TResult>(CompanyContext companies, IConfiguration configuration, ISiteContentAdministration service, Func<ISiteContentAdministration, CancellationToken, Task<Result<TResult>>> call, CancellationToken ct)
    {
        Guid? companyId = ResolvePublicCompany(configuration);
        if (companyId is null) return Results.StatusCode(503);
        using IDisposable scope = companies.BeginScope(companyId.Value);
        await service.EnsureSeededAsync(ct);
        return Map(await call(service, ct));
    }
    private static Guid? ResolvePublicCompany(IConfiguration configuration)
        => configuration.GetValue<string?>("Catalog:PublicCompanyId") is { } raw && Guid.TryParse(raw, out Guid id) && id != Guid.Empty ? id : null;
    private static string? PickNull(string? fa, string? en, string? ar, string? culture) => (culture ?? "fa") switch { "en" => string.IsNullOrWhiteSpace(en) ? fa : en, "ar" => string.IsNullOrWhiteSpace(ar) ? (string.IsNullOrWhiteSpace(en) ? fa : ar) : ar, _ => fa };
    private static string? JoinKeywords(BARANG.Core.Domain.SiteContent.SitePage p, string? culture) => PickNull(p.SeoKeywordsFa, p.SeoKeywordsEn, p.SeoKeywordsAr, culture);
    private static string? JoinKeywords(BARANG.Core.Domain.SiteContent.BlogPost p, string? culture) => PickNull(p.SeoKeywordsFa, p.SeoKeywordsEn, p.SeoKeywordsAr, culture);
    private static string Pick(string fa, string en, string ar, string? culture) => (culture ?? "fa") switch { "en" => string.IsNullOrWhiteSpace(en) ? fa : en, "ar" => string.IsNullOrWhiteSpace(ar) ? string.IsNullOrWhiteSpace(en) ? fa : ar : ar, _ => fa };
    private sealed record PublicPageView(string Slug, string Title, string Body, string? CoverUrl, string? SeoDescription, string? SeoKeywords, DateTimeOffset UpdatedAtUtc);
    private sealed record PublicPostView(Guid Id, string Slug, string Title, string Excerpt, string Body, string? ThumbnailUrl, string? SeoDescription, string? SeoKeywords, DateTimeOffset? PublishedAtUtc);
    private static SiteResult<T> Map<T>(Result<T> result, bool created = false) => new(result, created);
    private static SiteVoidResult MapVoid(Result result) => new(result);
    private sealed class SiteResult<T>(Result<T> result, bool created) : IResult { public Task ExecuteAsync(HttpContext c) { if (result.IsSuccess) { c.Response.StatusCode = created ? 201 : 200; return c.Response.WriteAsJsonAsync(result.Value, c.RequestAborted); } return Results.Problem(ApiProblemDetailsMapper.Create(result.Error, c.TraceIdentifier, c.Request.Path)).ExecuteAsync(c); } }
    private sealed class SiteVoidResult(Result result) : IResult { public Task ExecuteAsync(HttpContext c) { if (result.IsSuccess) { c.Response.StatusCode = 204; return Task.CompletedTask; } return Results.Problem(ApiProblemDetailsMapper.Create(result.Error, c.TraceIdentifier, c.Request.Path)).ExecuteAsync(c); } }
}
