using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.SiteContent;

/// <summary>ستون‌های مشترک زمان/نسخه برای نگاشت EF.</summary>
public interface ISiteContentRow
{
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset UpdatedAtUtc { get; }
    long Version { get; }
}

// ─── مدیریت محتوای سایت B2C از داخل ERP (CMS) ───
// همه‌چیز per-company است و از طریق API عمومی به فروشگاه B2C تحویل می‌شود؛
// B2C خودش هیچ دیتابیسی ندارد.

/// <summary>تنظیمات کلی سایت (یک ردیف برای هر شرکت).</summary>
public sealed class SiteSettings : ISiteContentRow
{
    private SiteSettings() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string SiteNameFa { get; private set; } = "";
    public string SiteNameEn { get; private set; } = "";
    public string SiteNameAr { get; private set; } = "";
    public string? LogoStorageKey { get; private set; }
    public string? FaviconStorageKey { get; private set; }
    public string? SloganFa { get; private set; }
    public string? SloganEn { get; private set; }
    public string? SloganAr { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<SiteSettings> Create(Guid id, Guid companyId, DateTimeOffset now)
        => id == Guid.Empty || companyId == Guid.Empty
            ? Result.Failure<SiteSettings>(SiteContentErrors.SettingsInvalid)
            : Result.Success(new SiteSettings { Id = id, CompanyId = companyId, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });

    public Result Update(string nameFa, string nameEn, string nameAr, string? logo, string? favicon, string? sloganFa, string? sloganEn, string? sloganAr, string? email, string? phone, long version, DateTimeOffset now)
    {
        if (version != Version) return Result.Failure(SiteContentErrors.Concurrency);
        string fa = nameFa.Trim();
        if (fa.Length == 0) return Result.Failure(SiteContentErrors.SettingsInvalid);
        SiteNameFa = fa; SiteNameEn = nameEn.Trim(); SiteNameAr = nameAr.Trim();
        LogoStorageKey = NormalizeKey(logo); FaviconStorageKey = NormalizeKey(favicon);
        SloganFa = Text(sloganFa); SloganEn = Text(sloganEn); SloganAr = Text(sloganAr);
        ContactEmail = Text(email); ContactPhone = Text(phone);
        UpdatedAtUtc = now; Version++;
        return Result.Success();
    }
    private static string? Text(string? v) { string t = v?.Trim() ?? ""; return t.Length == 0 ? null : t; }
    private static string? NormalizeKey(string? v) { string t = v?.Trim() ?? ""; return t.Length == 0 ? null : t; }
}

/// <summary>صفحات محتوایی (درباره ما، تماس، چاپ و بسته‌بندی و صفحات جدید). بدنه HTML خروجی ادیتور است.</summary>
public sealed class SitePage : ISiteContentRow
{
    private SitePage() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Slug { get; private set; } = "";
    public string TitleFa { get; private set; } = "";
    public string TitleEn { get; private set; } = "";
    public string TitleAr { get; private set; } = "";
    public string BodyFa { get; private set; } = "";
    public string BodyEn { get; private set; } = "";
    public string BodyAr { get; private set; } = "";
    public string? CoverStorageKey { get; private set; }
    public string? SeoDescriptionFa { get; private set; }
    public string? SeoDescriptionEn { get; private set; }
    public string? SeoDescriptionAr { get; private set; }
    public string? SeoKeywordsFa { get; private set; }
    public string? SeoKeywordsEn { get; private set; }
    public string? SeoKeywordsAr { get; private set; }
    /// <summary>main | footer | none — صفحه در منوی اصلی یا یکی از ستون‌های فوتر به‌صورت خودکار ظاهر می‌شود.</summary>
    public string Placement { get; private set; } = "none";
    /// <summary>وقتی Placement=footer است: شماره ستون ۱..۴.</summary>
    public int? FooterColumn { get; private set; }
    public bool IsPublished { get; private set; } = true;
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<SitePage> Create(Guid id, Guid companyId, string slug, string titleFa, string titleEn, string titleAr, string bodyFa, string bodyEn, string bodyAr, string? coverKey, string? seoFa, string? seoEn, string? seoAr, string? keywordsFa, string? keywordsEn, string? keywordsAr, string placement, int? footerColumn, int order, DateTimeOffset now)
    {
        string s = slug.Trim().ToLowerInvariant();
        if (id == Guid.Empty || companyId == Guid.Empty || s.Length == 0 || !s.All(c => char.IsLetterOrDigit(c) || c == '-') || s.All(char.IsDigit) || titleFa.Trim().Length == 0)
            return Result.Failure<SitePage>(SiteContentErrors.PageInvalid);
        Result placementCheck = ValidatePlacement(placement, footerColumn);
        if (placementCheck.IsFailure) return Result.Failure<SitePage>(placementCheck.Error);
        return Result.Success(new SitePage { Id = id, CompanyId = companyId, Slug = s, TitleFa = titleFa.Trim(), TitleEn = (titleEn ?? "").Trim(), TitleAr = (titleAr ?? "").Trim(), BodyFa = bodyFa ?? "", BodyEn = bodyEn ?? "", BodyAr = bodyAr ?? "", CoverStorageKey = SiteContentClean.Clean(coverKey), SeoDescriptionFa = SiteContentClean.Clean(seoFa), SeoDescriptionEn = SiteContentClean.Clean(seoEn), SeoDescriptionAr = SiteContentClean.Clean(seoAr), SeoKeywordsFa = SiteContentClean.Clean(keywordsFa), SeoKeywordsEn = SiteContentClean.Clean(keywordsEn), SeoKeywordsAr = SiteContentClean.Clean(keywordsAr), Placement = placement, FooterColumn = footerColumn, IsPublished = true, DisplayOrder = order, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result Update(string titleFa, string titleEn, string titleAr, string bodyFa, string bodyEn, string bodyAr, string? coverKey, string? seoFa, string? seoEn, string? seoAr, string? keywordsFa, string? keywordsEn, string? keywordsAr, string placement, int? footerColumn, bool published, int order, long version, DateTimeOffset now)
    {
        if (version != Version) return Result.Failure(SiteContentErrors.Concurrency);
        if (titleFa.Trim().Length == 0) return Result.Failure(SiteContentErrors.PageInvalid);
        Result placementCheck = ValidatePlacement(placement, footerColumn);
        if (placementCheck.IsFailure) return placementCheck;
        TitleFa = titleFa.Trim(); TitleEn = (titleEn ?? "").Trim(); TitleAr = (titleAr ?? "").Trim();
        BodyFa = bodyFa ?? ""; BodyEn = bodyEn ?? ""; BodyAr = bodyAr ?? "";
        if (!string.IsNullOrWhiteSpace(coverKey)) CoverStorageKey = SiteContentClean.Clean(coverKey);
        SeoDescriptionFa = SiteContentClean.Clean(seoFa); SeoDescriptionEn = SiteContentClean.Clean(seoEn); SeoDescriptionAr = SiteContentClean.Clean(seoAr);
        SeoKeywordsFa = SiteContentClean.Clean(keywordsFa); SeoKeywordsEn = SiteContentClean.Clean(keywordsEn); SeoKeywordsAr = SiteContentClean.Clean(keywordsAr);
        Placement = placement; FooterColumn = footerColumn; IsPublished = published; DisplayOrder = order;
        UpdatedAtUtc = now; Version++;
        return Result.Success();
    }
    internal static Result ValidatePlacement(string placement, int? footerColumn)
        => placement is "main" or "footer" or "none"
            ? placement == "footer" && footerColumn is not (>= 1 and <= 4) ? Result.Failure(SiteContentErrors.FooterColumnInvalid) : Result.Success()
            : Result.Failure(SiteContentErrors.PlacementInvalid);
}

/// <summary>آیتم یک ستون فوتر — صفحه، لینک منو، متن ساده، HTML یا تصویر بندانگشتی.</summary>
public sealed class FooterWidget : ISiteContentRow
{
    private FooterWidget() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public int ColumnIndex { get; private set; } // 1..4
    /// <summary>page | menu | text | html | image</summary>
    public string WidgetType { get; private set; } = "text";
    public Guid? PageId { get; private set; }
    public string? TitleFa { get; private set; }
    public string? TitleEn { get; private set; }
    public string? TitleAr { get; private set; }
    public string? Text { get; private set; }
    public string? Html { get; private set; }
    public string? ImageStorageKey { get; private set; }
    public string? Url { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<FooterWidget> Create(Guid id, Guid companyId, int column, string type, Guid? pageId, string? titleFa, string? titleEn, string? titleAr, string? text, string? html, string? imageKey, string? url, int order, DateTimeOffset now)
    {
        if (id == Guid.Empty || companyId == Guid.Empty || column is < 1 or > 4) return Result.Failure<FooterWidget>(SiteContentErrors.FooterColumnInvalid);
        if (type is not ("page" or "menu" or "text" or "html" or "image")) return Result.Failure<FooterWidget>(SiteContentErrors.WidgetTypeInvalid);
        if (type == "page" && pageId is null) return Result.Failure<FooterWidget>(SiteContentErrors.WidgetPageRequired);
        if (type == "image" && string.IsNullOrWhiteSpace(imageKey) && string.IsNullOrWhiteSpace(url)) return Result.Failure<FooterWidget>(SiteContentErrors.WidgetImageRequired);
        if (type == "menu" && string.IsNullOrWhiteSpace(url)) return Result.Failure<FooterWidget>(SiteContentErrors.WidgetUrlRequired);
        return Result.Success(new FooterWidget { Id = id, CompanyId = companyId, ColumnIndex = column, WidgetType = type, PageId = pageId, TitleFa = SiteContentClean.Clean(titleFa), TitleEn = SiteContentClean.Clean(titleEn), TitleAr = SiteContentClean.Clean(titleAr), Text = SiteContentClean.Clean(text), Html = SiteContentClean.Clean(html), ImageStorageKey = SiteContentClean.Clean(imageKey), Url = SiteContentClean.Clean(url), DisplayOrder = order, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result Update(int column, string type, Guid? pageId, string? titleFa, string? titleEn, string? titleAr, string? text, string? html, string? imageKey, string? url, int order, long version, DateTimeOffset now)
    {
        if (version != Version) return Result.Failure(SiteContentErrors.Concurrency);
        Result<FooterWidget> made = Create(Id, CompanyId, column, type, pageId, titleFa, titleEn, titleAr, text, html, imageKey, url, order, now);
        if (made.IsFailure) return Result.Failure(made.Error);
        ColumnIndex = column; WidgetType = type; PageId = pageId;
        TitleFa = SiteContentClean.Clean(titleFa); TitleEn = SiteContentClean.Clean(titleEn); TitleAr = SiteContentClean.Clean(titleAr);
        Text = SiteContentClean.Clean(text); Html = SiteContentClean.Clean(html); ImageStorageKey = SiteContentClean.Clean(imageKey); Url = SiteContentClean.Clean(url);
        DisplayOrder = order; UpdatedAtUtc = now; Version++;
        return Result.Success();
    }
    private static string? Clean(string? v) { string t = v?.Trim() ?? ""; return t.Length == 0 ? null : t; }
}

/// <summary>آیتم منوی اصلی سایت. آیتم «وبلاگ» IsPinned است و حذف نمی‌شود.</summary>
public sealed class MainMenuItem : ISiteContentRow
{
    private MainMenuItem() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    /// <summary>page | blog | custom</summary>
    public string ItemType { get; private set; } = "page";
    public Guid? PageId { get; private set; }
    public string? TitleFa { get; private set; }
    public string? TitleEn { get; private set; }
    public string? TitleAr { get; private set; }
    public string? Url { get; private set; }
    public bool IsPinned { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<MainMenuItem> Create(Guid id, Guid companyId, string type, Guid? pageId, string? titleFa, string? titleEn, string? titleAr, string? url, int order, DateTimeOffset now, bool pinned = false)
    {
        if (id == Guid.Empty || companyId == Guid.Empty) return Result.Failure<MainMenuItem>(SiteContentErrors.MenuItemInvalid);
        if (type is not ("page" or "blog" or "custom")) return Result.Failure<MainMenuItem>(SiteContentErrors.MenuItemInvalid);
        if (type == "page" && pageId is null) return Result.Failure<MainMenuItem>(SiteContentErrors.MenuItemInvalid);
        if (type == "custom" && string.IsNullOrWhiteSpace(url)) return Result.Failure<MainMenuItem>(SiteContentErrors.MenuItemInvalid);
        return Result.Success(new MainMenuItem { Id = id, CompanyId = companyId, ItemType = type, PageId = pageId, TitleFa = titleFa?.Trim(), TitleEn = titleEn?.Trim(), TitleAr = titleAr?.Trim(), Url = url?.Trim(), IsPinned = pinned, DisplayOrder = order, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result Update(string? titleFa, string? titleEn, string? titleAr, string? url, int order, long version, DateTimeOffset now)
    {
        if (version != Version) return Result.Failure(SiteContentErrors.Concurrency);
        TitleFa = titleFa?.Trim(); TitleEn = titleEn?.Trim(); TitleAr = titleAr?.Trim(); Url = url?.Trim(); DisplayOrder = order; UpdatedAtUtc = now; Version++;
        return Result.Success();
    }
}

/// <summary>پست وبلاگ — در B2C به‌صورت کارت با تصویر بندانگشتی نمایش داده می‌شود.</summary>
public sealed class BlogPost : ISiteContentRow
{
    private BlogPost() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Slug { get; private set; } = "";
    public string TitleFa { get; private set; } = "";
    public string TitleEn { get; private set; } = "";
    public string TitleAr { get; private set; } = "";
    public string ExcerptFa { get; private set; } = "";
    public string ExcerptEn { get; private set; } = "";
    public string ExcerptAr { get; private set; } = "";
    public string BodyFa { get; private set; } = "";
    public string BodyEn { get; private set; } = "";
    public string BodyAr { get; private set; } = "";
    public string? ThumbnailStorageKey { get; private set; }
    public string? SeoDescriptionFa { get; private set; }
    public string? SeoDescriptionEn { get; private set; }
    public string? SeoDescriptionAr { get; private set; }
    public string? SeoKeywordsFa { get; private set; }
    public string? SeoKeywordsEn { get; private set; }
    public string? SeoKeywordsAr { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<BlogPost> Create(Guid id, Guid companyId, string slug, string titleFa, string titleEn, string titleAr, string excerptFa, string excerptEn, string excerptAr, string bodyFa, string bodyEn, string bodyAr, string? thumbnailKey, string? seoFa, string? seoEn, string? seoAr, string? keywordsFa, string? keywordsEn, string? keywordsAr, DateTimeOffset now)
    {
        string s = slug.Trim().ToLowerInvariant();
        if (id == Guid.Empty || companyId == Guid.Empty || s.Length == 0 || !s.All(c => char.IsLetterOrDigit(c) || c == '-') || s.All(char.IsDigit) || titleFa.Trim().Length == 0)
            return Result.Failure<BlogPost>(SiteContentErrors.PostInvalid);
        return Result.Success(new BlogPost { Id = id, CompanyId = companyId, Slug = s, TitleFa = titleFa.Trim(), TitleEn = (titleEn ?? "").Trim(), TitleAr = (titleAr ?? "").Trim(), ExcerptFa = excerptFa ?? "", ExcerptEn = excerptEn ?? "", ExcerptAr = excerptAr ?? "", BodyFa = bodyFa ?? "", BodyEn = bodyEn ?? "", BodyAr = bodyAr ?? "", ThumbnailStorageKey = string.IsNullOrWhiteSpace(thumbnailKey) ? null : thumbnailKey.Trim(), SeoDescriptionFa = SiteContentClean.Clean(seoFa), SeoDescriptionEn = SiteContentClean.Clean(seoEn), SeoDescriptionAr = SiteContentClean.Clean(seoAr), SeoKeywordsFa = SiteContentClean.Clean(keywordsFa), SeoKeywordsEn = SiteContentClean.Clean(keywordsEn), SeoKeywordsAr = SiteContentClean.Clean(keywordsAr), IsPublished = false, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result Update(string titleFa, string titleEn, string titleAr, string excerptFa, string excerptEn, string excerptAr, string bodyFa, string bodyEn, string bodyAr, string? thumbnailKey, string? seoFa, string? seoEn, string? seoAr, string? keywordsFa, string? keywordsEn, string? keywordsAr, bool published, long version, DateTimeOffset now)
    {
        if (version != Version) return Result.Failure(SiteContentErrors.Concurrency);
        if (titleFa.Trim().Length == 0) return Result.Failure(SiteContentErrors.PostInvalid);
        TitleFa = titleFa.Trim(); TitleEn = (titleEn ?? "").Trim(); TitleAr = (titleAr ?? "").Trim();
        ExcerptFa = excerptFa ?? ""; ExcerptEn = excerptEn ?? ""; ExcerptAr = excerptAr ?? "";
        BodyFa = bodyFa ?? ""; BodyEn = bodyEn ?? ""; BodyAr = bodyAr ?? "";
        if (!string.IsNullOrWhiteSpace(thumbnailKey)) ThumbnailStorageKey = thumbnailKey.Trim();
        SeoDescriptionFa = SiteContentClean.Clean(seoFa); SeoDescriptionEn = SiteContentClean.Clean(seoEn); SeoDescriptionAr = SiteContentClean.Clean(seoAr);
        SeoKeywordsFa = SiteContentClean.Clean(keywordsFa); SeoKeywordsEn = SiteContentClean.Clean(keywordsEn); SeoKeywordsAr = SiteContentClean.Clean(keywordsAr);
        if (published && !IsPublished) PublishedAtUtc = now;
        IsPublished = published; UpdatedAtUtc = now; Version++;
        return Result.Success();
    }
}

file static class SiteContentClean
{
    internal static string? Clean(string? v) { string t = v?.Trim() ?? ""; return t.Length == 0 ? null : t; }
}

public sealed partial class SiteContentErrors
{
    public static readonly Error SettingsInvalid = Error.Validation("site.settings_invalid");
    public static readonly Error Concurrency = Error.Validation("site.concurrency");
    public static readonly Error PageInvalid = Error.Validation("site.page_invalid");
    public static readonly Error PageNotFound = Error.NotFound("site.page_not_found");
    public static readonly Error PageDuplicateSlug = Error.Conflict("site.page_duplicate_slug");
    public static readonly Error PlacementInvalid = Error.Validation("site.placement_invalid");
    public static readonly Error FooterColumnInvalid = Error.Validation("site.footer_column_invalid");
    public static readonly Error WidgetTypeInvalid = Error.Validation("site.widget_type_invalid");
    public static readonly Error WidgetPageRequired = Error.Validation("site.widget_page_required");
    public static readonly Error WidgetImageRequired = Error.Validation("site.widget_image_required");
    public static readonly Error WidgetUrlRequired = Error.Validation("site.widget_url_required");
    public static readonly Error MenuItemInvalid = Error.Validation("site.menu_item_invalid");
    public static readonly Error MenuItemPinned = Error.Conflict("site.menu_item_pinned");
    public static readonly Error PostInvalid = Error.Validation("site.post_invalid");
    public static readonly Error PostNotFound = Error.NotFound("site.post_not_found");
    public static readonly Error PostDuplicateSlug = Error.Conflict("site.post_duplicate_slug");
    public static readonly Error UploadInvalid = Error.Validation("site.upload_invalid");
}
