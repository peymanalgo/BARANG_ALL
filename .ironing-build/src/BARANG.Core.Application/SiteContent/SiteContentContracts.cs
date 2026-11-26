using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.SiteContent;

namespace BARANG.Core.Application.SiteContent;

// ─── قراردادهای CMS سایت B2C ───

public interface ISiteContentAdministration
{
    Task<Result<SiteSettingsView>> GetSettingsAsync(CancellationToken ct);
    Task<Result<SiteSettingsView>> UpdateSettingsAsync(SiteSettingsCommand command, CancellationToken ct);

    Task<Result<IReadOnlyList<SitePageView>>> ListPagesAsync(CancellationToken ct);
    Task<Result<SitePageView>> GetPageAsync(Guid id, CancellationToken ct);
    Task<Result<SitePageView>> CreatePageAsync(SitePageCommand command, CancellationToken ct);
    Task<Result<SitePageView>> UpdatePageAsync(Guid id, SitePageCommand command, CancellationToken ct);
    Task<Result> DeletePageAsync(Guid id, CancellationToken ct);

    Task<Result<IReadOnlyList<FooterWidgetView>>> ListFooterAsync(CancellationToken ct);
    Task<Result<FooterWidgetView>> CreateWidgetAsync(FooterWidgetCommand command, CancellationToken ct);
    Task<Result<FooterWidgetView>> UpdateWidgetAsync(Guid id, FooterWidgetCommand command, CancellationToken ct);
    Task<Result> DeleteWidgetAsync(Guid id, CancellationToken ct);

    Task<Result<IReadOnlyList<MainMenuItemView>>> ListMenuAsync(CancellationToken ct);
    Task<Result<MainMenuItemView>> CreateMenuItemAsync(MainMenuItemCommand command, CancellationToken ct);
    Task<Result> DeleteMenuItemAsync(Guid id, CancellationToken ct);

    Task<Result<PagedResult<BlogPostView>>> ListBlogAsync(int page, int pageSize, bool includeUnpublished, CancellationToken ct);
    Task<Result<BlogPostView>> GetPostAsync(Guid id, CancellationToken ct);
    Task<Result<BlogPostView>> CreatePostAsync(BlogPostCommand command, CancellationToken ct);
    Task<Result<BlogPostView>> UpdatePostAsync(Guid id, BlogPostCommand command, CancellationToken ct);
    Task<Result> DeletePostAsync(Guid id, CancellationToken ct);

    Task<Result<SiteImageView>> UploadImageAsync(Stream content, long length, string mediaType, string fileName, CancellationToken ct);
    Task<Result> EnsureSeededAsync(CancellationToken ct);
}

public sealed record SiteSettingsView(Guid Id, string SiteNameFa, string SiteNameEn, string SiteNameAr, string SiteNameEs, string? LogoUrl, string? FaviconUrl, string? LogoStorageKey, string? FaviconStorageKey, string? SloganFa, string? SloganEn, string? SloganAr, string? SloganEs, string? ContactEmail, string? ContactPhone, long Version);
public sealed record SiteSettingsCommand(string SiteNameFa, string SiteNameEn, string SiteNameAr, string SiteNameEs, string? LogoStorageKey, string? FaviconStorageKey, string? SloganFa, string? SloganEn, string? SloganAr, string? SloganEs, string? ContactEmail, string? ContactPhone, long Version);

public sealed record SitePageView(Guid Id, string Slug, string TitleFa, string TitleEn, string TitleAr, string TitleEs, string BodyFa, string BodyEn, string BodyAr, string BodyEs, string? CoverUrl, string? SeoDescriptionFa, string? SeoDescriptionEn, string? SeoDescriptionAr, string? SeoDescriptionEs, string? SeoKeywordsFa, string? SeoKeywordsEn, string? SeoKeywordsAr, string? SeoKeywordsEs, string Placement, int? FooterColumn, bool IsPublished, int DisplayOrder, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record SitePageCommand(string Slug, string TitleFa, string TitleEn, string TitleAr, string TitleEs, string BodyFa, string BodyEn, string BodyAr, string BodyEs, string? CoverStorageKey, string? SeoDescriptionFa, string? SeoDescriptionEn, string? SeoDescriptionAr, string? SeoDescriptionEs, string? SeoKeywordsFa, string? SeoKeywordsEn, string? SeoKeywordsAr, string? SeoKeywordsEs, string Placement, int? FooterColumn, bool IsPublished, int DisplayOrder, long Version);

public sealed record FooterWidgetView(Guid Id, int ColumnIndex, string WidgetType, Guid? PageId, string? PageSlug, string? PageTitleFa, string? TitleFa, string? TitleEn, string? TitleAr, string? TitleEs, string? TextFa, string? TextEn, string? TextAr, string? TextEs, string? Html, string? ImageUrl, string? ImageStorageKey, string? Url, int DisplayOrder, long Version);
public sealed record FooterWidgetCommand(int ColumnIndex, string WidgetType, Guid? PageId, string? TitleFa, string? TitleEn, string? TitleAr, string? TitleEs, string? TextFa, string? TextEn, string? TextAr, string? TextEs, string? Html, string? ImageStorageKey, string? Url, int DisplayOrder, long Version);

public sealed record MainMenuItemView(Guid Id, string ItemType, Guid? PageId, string? PageSlug, string? PageTitleFa, string? PageTitleEn, string? PageTitleAr, string? PageTitleEs, string? TitleFa, string? TitleEn, string? TitleAr, string? TitleEs, string? Url, bool IsPinned, int DisplayOrder, long Version);
public sealed record MainMenuItemCommand(string ItemType, Guid? PageId, string? TitleFa, string? TitleEn, string? TitleAr, string? TitleEs, string? Url, int DisplayOrder);

public sealed record BlogPostView(Guid Id, string Slug, string TitleFa, string TitleEn, string TitleAr, string TitleEs, string ExcerptFa, string ExcerptEn, string ExcerptAr, string ExcerptEs, string BodyFa, string BodyEn, string BodyAr, string BodyEs, string? ThumbnailUrl, string? ThumbnailStorageKey, string? SeoDescriptionFa, string? SeoDescriptionEn, string? SeoDescriptionAr, string? SeoDescriptionEs, string? SeoKeywordsFa, string? SeoKeywordsEn, string? SeoKeywordsAr, string? SeoKeywordsEs, bool IsPublished, DateTimeOffset? PublishedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record BlogPostCommand(string Slug, string TitleFa, string TitleEn, string TitleAr, string TitleEs, string ExcerptFa, string ExcerptEn, string ExcerptAr, string ExcerptEs, string BodyFa, string BodyEn, string BodyAr, string BodyEs, string? ThumbnailStorageKey, string? SeoDescriptionFa, string? SeoDescriptionEn, string? SeoDescriptionAr, string? SeoDescriptionEs, string? SeoKeywordsFa, string? SeoKeywordsEn, string? SeoKeywordsAr, string? SeoKeywordsEs, bool IsPublished, long Version);

public sealed record SiteImageView(string StorageKey, string Url);
