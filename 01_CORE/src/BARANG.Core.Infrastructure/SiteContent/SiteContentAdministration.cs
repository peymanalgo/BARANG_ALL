using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Domain.Common;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Files;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Application.SiteContent;
using BARANG.Core.Domain.SiteContent;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BARANG.Core.Infrastructure.SiteContent;

// ─── مدیریت محتوای سایت B2C (CMS) — همه‌چیز per-company و از طریق API عمومی به فروشگاه تحویل می‌شود ───
public sealed class SiteContentAdministration(CoreDbContext db, ITransactionExecutor tx, ICompanyContext company, TimeProvider time, IObjectStorage storage, IOptions<FileStorageOptions> fileOptions) : ISiteContentAdministration
{
    private Guid Company => company.RequireCompanyId();
    private DateTimeOffset Now => time.GetUtcNow();

    // ── تنظیمات ──
    public Task<Result<SiteSettingsView>> GetSettingsAsync(CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        SiteSettings settings = await EnsureSettingsAsync(t, ct);
        return Result.Success(View(settings));
    }, ct);

    public Task<Result<SiteSettingsView>> UpdateSettingsAsync(SiteSettingsCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        SiteSettings settings = await EnsureSettingsAsync(t, ct);
        Result changed = settings.Update(command.SiteNameFa, command.SiteNameEn, command.SiteNameAr, command.LogoStorageKey, command.FaviconStorageKey, command.SloganFa, command.SloganEn, command.SloganAr, command.ContactEmail, command.ContactPhone, command.Version, Now);
        return changed.IsFailure ? Result.Failure<SiteSettingsView>(changed.Error) : Result.Success(View(settings));
    }, ct);

    // ── صفحات ──
    public Task<Result<IReadOnlyList<SitePageView>>> ListPagesAsync(CancellationToken ct) => tx.ExecuteAsync(async t =>
        Result.Success<IReadOnlyList<SitePageView>>((await db.SitePages.AsNoTracking().Where(x => x.CompanyId == Company).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Slug).ToListAsync(ct)).Select(View).ToList()), ct);

    public Task<Result<SitePageView>> GetPageAsync(Guid id, CancellationToken ct) => tx.ExecuteAsync(async t =>
        await db.SitePages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct) is { } p
            ? Result.Success(View(p)) : Result.Failure<SitePageView>(SiteContentErrors.PageNotFound), ct);

    public Task<Result<SitePageView>> CreatePageAsync(SitePageCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (await db.SitePages.AnyAsync(x => x.CompanyId == Company && string.Equals(x.Slug, command.Slug.Trim().ToLowerInvariant(), System.StringComparison.Ordinal), ct)) return Result.Failure<SitePageView>(SiteContentErrors.PageDuplicateSlug);
        Result<SitePage> made = SitePage.Create(Guid.NewGuid(), Company, command.Slug, command.TitleFa, command.TitleEn, command.TitleAr, command.BodyFa, command.BodyEn, command.BodyAr, command.CoverStorageKey, command.SeoDescriptionFa, command.SeoDescriptionEn, command.SeoDescriptionAr, command.SeoKeywordsFa, command.SeoKeywordsEn, command.SeoKeywordsAr, NormalizePlacement(command.Placement), command.FooterColumn, command.DisplayOrder, Now);
        if (made.IsFailure) return Result.Failure<SitePageView>(made.Error);
        db.SitePages.Add(made.Value);
        await db.SaveChangesAsync(ct);
        await SyncPagePlacementAsync(made.Value, ct);
        return Result.Success(View(made.Value));
    }, ct);

    public Task<Result<SitePageView>> UpdatePageAsync(Guid id, SitePageCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        SitePage? p = await db.SitePages.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (p is null) return Result.Failure<SitePageView>(SiteContentErrors.PageNotFound);
        string normalizedSlug = command.Slug.Trim().ToLowerInvariant();
        if (await db.SitePages.AnyAsync(x => x.CompanyId == Company && x.Id != id && x.Slug == normalizedSlug, ct)) return Result.Failure<SitePageView>(SiteContentErrors.PageDuplicateSlug);
        Result changed = p.Update(command.TitleFa, command.TitleEn, command.TitleAr, command.BodyFa, command.BodyEn, command.BodyAr, command.CoverStorageKey, command.SeoDescriptionFa, command.SeoDescriptionEn, command.SeoDescriptionAr, command.SeoKeywordsFa, command.SeoKeywordsEn, command.SeoKeywordsAr, NormalizePlacement(command.Placement), command.FooterColumn, command.IsPublished, command.DisplayOrder, command.Version, Now);
        if (changed.IsFailure) return Result.Failure<SitePageView>(changed.Error);
        await db.SaveChangesAsync(ct);
        await SyncPagePlacementAsync(p, ct);
        return Result.Success(View(p));
    }, ct);

    public Task<Result> DeletePageAsync(Guid id, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        SitePage? p = await db.SitePages.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (p is null) return Result.Failure(SiteContentErrors.PageNotFound);
        db.SitePages.Remove(p);
        // آیتم‌های وابسته منو/فوتر که به این صفحه اشاره می‌کنند هم پاک شوند
        db.MainMenuItems.RemoveRange(await db.MainMenuItems.Where(x => x.PageId == id).ToListAsync(ct));
        db.FooterWidgets.RemoveRange(await db.FooterWidgets.Where(x => x.PageId == id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }, ct);

    // صفحه در منوی اصلی/ستون فوتر به‌صورت خودکار همگام می‌شود (یک منبع حقیقت برای هر ناحیه)
    private async Task SyncPagePlacementAsync(SitePage page, CancellationToken ct)
    {
        db.MainMenuItems.RemoveRange(await db.MainMenuItems.Where(x => x.PageId == page.Id).ToListAsync(ct));
        db.FooterWidgets.RemoveRange(await db.FooterWidgets.Where(x => x.PageId == page.Id).ToListAsync(ct));
        if (!page.IsPublished) { await db.SaveChangesAsync(ct); return; }
        if (page.Placement == "main")
            db.MainMenuItems.Add(MainMenuItem.Create(Guid.NewGuid(), Company, "page", page.Id, null, null, null, null, page.DisplayOrder, Now).Value);
        else if (page.Placement == "footer" && page.FooterColumn is int col)
            db.FooterWidgets.Add(FooterWidget.Create(Guid.NewGuid(), Company, col, "page", page.Id, null, null, null, null, null, null, null, page.DisplayOrder, Now).Value);
        await db.SaveChangesAsync(ct);
    }

    // ── فوتر ──
    public Task<Result<IReadOnlyList<FooterWidgetView>>> ListFooterAsync(CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        List<FooterWidget> widgets = await db.FooterWidgets.AsNoTracking().Where(x => x.CompanyId == Company).OrderBy(x => x.ColumnIndex).ThenBy(x => x.DisplayOrder).ToListAsync(ct);
        Dictionary<Guid, SitePage> pages = await db.SitePages.AsNoTracking().Where(x => x.CompanyId == Company).ToDictionaryAsync(x => x.Id, ct);
        return Result.Success<IReadOnlyList<FooterWidgetView>>(widgets.Select(w => View(w, pages.GetValueOrDefault(w.PageId ?? Guid.Empty))).ToList());
    }, ct);

    public Task<Result<FooterWidgetView>> CreateWidgetAsync(FooterWidgetCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        Result<FooterWidget> made = FooterWidget.Create(Guid.NewGuid(), Company, command.ColumnIndex, command.WidgetType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.Text, command.Html, command.ImageStorageKey, command.Url, command.DisplayOrder, Now);
        if (made.IsFailure) return Result.Failure<FooterWidgetView>(made.Error);
        db.FooterWidgets.Add(made.Value);
        await db.SaveChangesAsync(ct);
        SitePage? page = command.PageId is Guid pid ? await db.SitePages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == pid, ct) : null;
        return Result.Success(View(made.Value, page));
    }, ct);

    public Task<Result<FooterWidgetView>> UpdateWidgetAsync(Guid id, FooterWidgetCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        FooterWidget? w = await db.FooterWidgets.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (w is null) return Result.Failure<FooterWidgetView>(SiteContentErrors.PageNotFound);
        Result changed = w.Update(command.ColumnIndex, command.WidgetType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.Text, command.Html, command.ImageStorageKey, command.Url, command.DisplayOrder, command.Version, Now);
        if (changed.IsFailure) return Result.Failure<FooterWidgetView>(changed.Error);
        await db.SaveChangesAsync(ct);
        SitePage? page = command.PageId is Guid pid ? await db.SitePages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == pid, ct) : null;
        return Result.Success(View(w, page));
    }, ct);

    public Task<Result> DeleteWidgetAsync(Guid id, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        FooterWidget? w = await db.FooterWidgets.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (w is null) return Result.Failure(SiteContentErrors.PageNotFound);
        db.FooterWidgets.Remove(w);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }, ct);

    // ── منوی اصلی ──
    public Task<Result<IReadOnlyList<MainMenuItemView>>> ListMenuAsync(CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        List<MainMenuItem> items = await db.MainMenuItems.AsNoTracking().Where(x => x.CompanyId == Company).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        Dictionary<Guid, SitePage> pages = await db.SitePages.AsNoTracking().Where(x => x.CompanyId == Company).ToDictionaryAsync(x => x.Id, ct);
        return Result.Success<IReadOnlyList<MainMenuItemView>>(items.Select(i => View(i, pages.GetValueOrDefault(i.PageId ?? Guid.Empty))).ToList());
    }, ct);

    public Task<Result<MainMenuItemView>> CreateMenuItemAsync(MainMenuItemCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        Result<MainMenuItem> made = MainMenuItem.Create(Guid.NewGuid(), Company, command.ItemType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.Url, command.DisplayOrder, Now);
        if (made.IsFailure) return Result.Failure<MainMenuItemView>(made.Error);
        db.MainMenuItems.Add(made.Value);
        await db.SaveChangesAsync(ct);
        SitePage? page = command.PageId is Guid pid ? await db.SitePages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == pid, ct) : null;
        return Result.Success(View(made.Value, page));
    }, ct);

    public Task<Result> DeleteMenuItemAsync(Guid id, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        MainMenuItem? item = await db.MainMenuItems.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (item is null) return Result.Failure(SiteContentErrors.MenuItemInvalid);
        if (item.IsPinned) return Result.Failure(SiteContentErrors.MenuItemPinned); // وبلاگ ثابت است
        db.MainMenuItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }, ct);

    // ── وبلاگ ──
    public Task<Result<PagedResult<BlogPostView>>> ListBlogAsync(int page, int pageSize, bool includeUnpublished, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        int size = Math.Clamp(pageSize, 1, 60), number = Math.Max(1, page);
        IQueryable<BlogPost> query = db.BlogPosts.AsNoTracking().Where(x => x.CompanyId == Company);
        if (!includeUnpublished) query = query.Where(x => x.IsPublished);
        long total = await query.LongCountAsync(ct);
        List<BlogPostView> items = (await query.OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc).Skip((number - 1) * size).Take(size).ToListAsync(ct)).Select(View).ToList();
        return Result.Success(new PagedResult<BlogPostView>(items, number, size, total));
    }, ct);

    public Task<Result<BlogPostView>> GetPostAsync(Guid id, CancellationToken ct) => tx.ExecuteAsync(async t =>
        await db.BlogPosts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct) is { } p
            ? Result.Success(View(p)) : Result.Failure<BlogPostView>(SiteContentErrors.PostNotFound), ct);

    public Task<Result<BlogPostView>> CreatePostAsync(BlogPostCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        string slug = command.Slug.Trim().ToLowerInvariant();
        if (await db.BlogPosts.AnyAsync(x => x.CompanyId == Company && x.Slug == slug, ct)) return Result.Failure<BlogPostView>(SiteContentErrors.PostDuplicateSlug);
        Result<BlogPost> made = BlogPost.Create(Guid.NewGuid(), Company, slug, command.TitleFa, command.TitleEn, command.TitleAr, command.ExcerptFa, command.ExcerptEn, command.ExcerptAr, command.BodyFa, command.BodyEn, command.BodyAr, command.ThumbnailStorageKey, command.SeoDescriptionFa, command.SeoDescriptionEn, command.SeoDescriptionAr, command.SeoKeywordsFa, command.SeoKeywordsEn, command.SeoKeywordsAr, Now);
        if (made.IsFailure) return Result.Failure<BlogPostView>(made.Error);
        if (command.IsPublished) { Result published = made.Value.Update(command.TitleFa, command.TitleEn, command.TitleAr, command.ExcerptFa, command.ExcerptEn, command.ExcerptAr, command.BodyFa, command.BodyEn, command.BodyAr, command.ThumbnailStorageKey, command.SeoDescriptionFa, command.SeoDescriptionEn, command.SeoDescriptionAr, command.SeoKeywordsFa, command.SeoKeywordsEn, command.SeoKeywordsAr, true, 1, Now); if (published.IsFailure) return Result.Failure<BlogPostView>(published.Error); }
        db.BlogPosts.Add(made.Value);
        await db.SaveChangesAsync(ct);
        return Result.Success(View(made.Value));
    }, ct);

    public Task<Result<BlogPostView>> UpdatePostAsync(Guid id, BlogPostCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        BlogPost? p = await db.BlogPosts.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (p is null) return Result.Failure<BlogPostView>(SiteContentErrors.PostNotFound);
        string slug = command.Slug.Trim().ToLowerInvariant();
        if (slug != p.Slug && await db.BlogPosts.AnyAsync(x => x.CompanyId == Company && x.Slug == slug, ct)) return Result.Failure<BlogPostView>(SiteContentErrors.PostDuplicateSlug);
        Result changed = p.Update(command.TitleFa, command.TitleEn, command.TitleAr, command.ExcerptFa, command.ExcerptEn, command.ExcerptAr, command.BodyFa, command.BodyEn, command.BodyAr, command.ThumbnailStorageKey, command.SeoDescriptionFa, command.SeoDescriptionEn, command.SeoDescriptionAr, command.SeoKeywordsFa, command.SeoKeywordsEn, command.SeoKeywordsAr, command.IsPublished, command.Version, Now);
        return changed.IsFailure ? Result.Failure<BlogPostView>(changed.Error) : Result.Success(View(p));
    }, ct);

    public Task<Result> DeletePostAsync(Guid id, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        BlogPost? p = await db.BlogPosts.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == Company, ct);
        if (p is null) return Result.Failure(SiteContentErrors.PostNotFound);
        db.BlogPosts.Remove(p);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }, ct);

    // ── آپلود تصویر (لوگو/فاوآیکن/فوتر/وبلاگ) — در باکت عمومی MinIO ──
    public async Task<Result<SiteImageView>> UploadImageAsync(Stream content, long length, string mediaType, string fileName, CancellationToken ct)
    {
        if (length <= 0 || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return Result.Failure<SiteImageView>(SiteContentErrors.UploadInvalid);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".svg" or ".webp" or ".gif" or ".ico")) return Result.Failure<SiteImageView>(SiteContentErrors.UploadInvalid);
        string key = $"companies/{Company}/site/{Guid.NewGuid():N}{extension}";
        string? publicBucket = fileOptions.Value.PublicBucket;
        await storage.PutAsync(new(key, content, length, mediaType, "public,max-age=31536000,immutable", string.IsNullOrWhiteSpace(publicBucket) ? null : publicBucket), ct);
        return Result.Success(new SiteImageView(key, storage.GetPublicObjectUrl(key)));
    }

    // ── سیدر پیش‌فرض (idempotent) برای شرکت جاری — اولین بار محتوای فعلی سایت را می‌سازد ──
    public Task<Result> EnsureSeededAsync(CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        SiteSettings _ = await EnsureSettingsAsync(t, ct);
        if (await db.MainMenuItems.AnyAsync(x => x.CompanyId == Company, ct)) return Result.Success();
        foreach ((string slug, string fa, string en, string ar, string bodyFa, string bodyEn, string bodyAr, string placement, int? footerColumn, int order) in SiteSeedData.Pages)
        {
            if (await db.SitePages.AnyAsync(x => x.CompanyId == Company && x.Slug == slug, ct)) continue;
            Result<SitePage> page = SitePage.Create(Guid.NewGuid(), Company, slug, fa, en, ar, bodyFa, bodyEn, bodyAr, null, null, null, null, null, null, null, placement, footerColumn, order, Now);
            if (page.IsSuccess) db.SitePages.Add(page.Value);
        }
        await db.SaveChangesAsync(ct);
        foreach (var (type, pageSlug, titleFa, titleEn, titleAr, text, url, imageKey, column, order) in SiteSeedData.FooterWidgets)
        {
            Guid? pageId = pageSlug is null ? null : await db.SitePages.Where(x => x.CompanyId == Company && x.Slug == pageSlug).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
            Result<FooterWidget> w = FooterWidget.Create(Guid.NewGuid(), Company, column, type, pageId, titleFa, titleEn, titleAr, text, null, imageKey, url, order, Now);
            if (w.IsSuccess) db.FooterWidgets.Add(w.Value);
        }
        int menuOrder = 1;
        foreach (var (type, pageSlug, url, titleFa, titleEn, titleAr, pinned) in SiteSeedData.MenuItems)
        {
            Guid? pageId = pageSlug is null ? null : await db.SitePages.Where(x => x.CompanyId == Company && x.Slug == pageSlug).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
            Result<MainMenuItem> item = MainMenuItem.Create(Guid.NewGuid(), Company, type, pageId, titleFa, titleEn, titleAr, url, menuOrder++, Now, pinned);
            if (item.IsSuccess) db.MainMenuItems.Add(item.Value);
        }
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }, ct);

    private async Task<SiteSettings> EnsureSettingsAsync(CancellationToken t, CancellationToken ct)
    {
        SiteSettings? settings = await db.SiteSettings.SingleOrDefaultAsync(x => x.CompanyId == Company, ct);
        if (settings is not null) return settings;
        Result<SiteSettings> made = SiteSettings.Create(Guid.NewGuid(), Company, Now);
        if (made.IsFailure) throw new InvalidOperationException("site settings invalid");
        made.Value.Update("پی‌رنگ", "Peyrang", "بي‌رنگ", null, null, "پی‌رنگ خان، نقشه‌ها داره برات!", "Peyrang Khan has the designs for you!", "پی‌رنگ خان لديه التصاميم من أجلك!", "salam@pirang.ir", "02100000000", 1, Now);
        db.SiteSettings.Add(made.Value);
        await db.SaveChangesAsync(ct);
        return made.Value;
    }
    private static string NormalizePlacement(string placement) => placement is "main" or "footer" or "none" ? placement : "none";

    private string? Url(string? key) => string.IsNullOrWhiteSpace(key) ? null : storage.GetPublicObjectUrl(key);
    private SiteSettingsView View(SiteSettings s) => new(s.Id, s.SiteNameFa, s.SiteNameEn, s.SiteNameAr, Url(s.LogoStorageKey), Url(s.FaviconStorageKey), s.LogoStorageKey, s.FaviconStorageKey, s.SloganFa, s.SloganEn, s.SloganAr, s.ContactEmail, s.ContactPhone, s.Version);
    private SitePageView View(SitePage p) => new(p.Id, p.Slug, p.TitleFa, p.TitleEn, p.TitleAr, p.BodyFa, p.BodyEn, p.BodyAr, Url(p.CoverStorageKey), p.SeoDescriptionFa, p.SeoDescriptionEn, p.SeoDescriptionAr, p.SeoKeywordsFa, p.SeoKeywordsEn, p.SeoKeywordsAr, p.Placement, p.FooterColumn, p.IsPublished, p.DisplayOrder, p.UpdatedAtUtc, p.Version);
    private FooterWidgetView View(FooterWidget w, SitePage? page) => new(w.Id, w.ColumnIndex, w.WidgetType, w.PageId, page?.Slug, page?.TitleFa, w.TitleFa, w.TitleEn, w.TitleAr, w.Text, w.Html, Url(w.ImageStorageKey), w.Url, w.DisplayOrder, w.Version);
    private static MainMenuItemView View(MainMenuItem i, SitePage? page) => new(i.Id, i.ItemType, i.PageId, page?.Slug, page?.TitleFa, page?.TitleEn, page?.TitleAr, i.TitleFa, i.TitleEn, i.TitleAr, i.Url, i.IsPinned, i.DisplayOrder, i.Version);
    private BlogPostView View(BlogPost p) => new(p.Id, p.Slug, p.TitleFa, p.TitleEn, p.TitleAr, p.ExcerptFa, p.ExcerptEn, p.ExcerptAr, p.BodyFa, p.BodyEn, p.BodyAr, Url(p.ThumbnailStorageKey), p.ThumbnailStorageKey, p.SeoDescriptionFa, p.SeoDescriptionEn, p.SeoDescriptionAr, p.SeoKeywordsFa, p.SeoKeywordsEn, p.SeoKeywordsAr, p.IsPublished, p.PublishedAtUtc, p.UpdatedAtUtc, p.Version);
}
