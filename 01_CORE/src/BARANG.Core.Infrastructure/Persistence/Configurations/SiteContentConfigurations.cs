using BARANG.Core.Domain.SiteContent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal static class SiteColumns
{
    internal static void Versioned<T>(EntityTypeBuilder<T> b) where T : class, BARANG.Core.Domain.SiteContent.ISiteContentRow
    {
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.ToTable("site_settings", "barang");
        b.HasKey(x => x.Id).HasName("pk_site_settings");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.SiteNameFa).HasColumnName("site_name_fa").HasMaxLength(120);
        b.Property(x => x.SiteNameEn).HasColumnName("site_name_en").HasMaxLength(120);
        b.Property(x => x.SiteNameAr).HasColumnName("site_name_ar").HasMaxLength(120);
        b.Property(x => x.LogoStorageKey).HasColumnName("logo_storage_key").HasMaxLength(400);
        b.Property(x => x.FaviconStorageKey).HasColumnName("favicon_storage_key").HasMaxLength(400);
        b.Property(x => x.SloganFa).HasColumnName("slogan_fa").HasMaxLength(300);
        b.Property(x => x.SloganEn).HasColumnName("slogan_en").HasMaxLength(300);
        b.Property(x => x.SloganAr).HasColumnName("slogan_ar").HasMaxLength(300);
        b.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(320);
        b.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(64);
        b.HasIndex(x => x.CompanyId).IsUnique();
        SiteColumns.Versioned(b);
    }
}

internal sealed class SitePageConfiguration : IEntityTypeConfiguration<SitePage>
{
    public void Configure(EntityTypeBuilder<SitePage> b)
    {
        b.ToTable("site_pages", "barang");
        b.HasKey(x => x.Id).HasName("pk_site_pages");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(120);
        b.Property(x => x.TitleFa).HasColumnName("title_fa").HasMaxLength(200);
        b.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(200);
        b.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(200);
        b.Property(x => x.BodyFa).HasColumnName("body_fa");
        b.Property(x => x.BodyEn).HasColumnName("body_en");
        b.Property(x => x.BodyAr).HasColumnName("body_ar");
        b.Property(x => x.CoverStorageKey).HasColumnName("cover_storage_key").HasMaxLength(400);
        b.Property(x => x.SeoDescriptionFa).HasColumnName("seo_description_fa").HasMaxLength(600);
        b.Property(x => x.SeoDescriptionEn).HasColumnName("seo_description_en").HasMaxLength(600);
        b.Property(x => x.SeoDescriptionAr).HasColumnName("seo_description_ar").HasMaxLength(600);
        b.Property(x => x.SeoKeywordsFa).HasColumnName("seo_keywords_fa").HasMaxLength(400);
        b.Property(x => x.SeoKeywordsEn).HasColumnName("seo_keywords_en").HasMaxLength(400);
        b.Property(x => x.SeoKeywordsAr).HasColumnName("seo_keywords_ar").HasMaxLength(400);
        b.Property(x => x.Placement).HasColumnName("placement").HasMaxLength(20);
        b.Property(x => x.FooterColumn).HasColumnName("footer_column");
        b.Property(x => x.IsPublished).HasColumnName("is_published");
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.HasIndex(x => new { x.CompanyId, x.Slug }).IsUnique();
        SiteColumns.Versioned(b);
    }
}

internal sealed class FooterWidgetConfiguration : IEntityTypeConfiguration<FooterWidget>
{
    public void Configure(EntityTypeBuilder<FooterWidget> b)
    {
        b.ToTable("site_footer_widgets", "barang");
        b.HasKey(x => x.Id).HasName("pk_site_footer_widgets");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ColumnIndex).HasColumnName("column_index");
        b.Property(x => x.WidgetType).HasColumnName("widget_type").HasMaxLength(20);
        b.Property(x => x.PageId).HasColumnName("page_id");
        b.Property(x => x.TitleFa).HasColumnName("title_fa").HasMaxLength(200);
        b.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(200);
        b.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(200);
        b.Property(x => x.Text).HasColumnName("text").HasMaxLength(2000);
        b.Property(x => x.Html).HasColumnName("html");
        b.Property(x => x.ImageStorageKey).HasColumnName("image_storage_key").HasMaxLength(400);
        b.Property(x => x.Url).HasColumnName("url").HasMaxLength(500);
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.HasIndex(x => new { x.CompanyId, x.ColumnIndex });
        SiteColumns.Versioned(b);
    }
}

internal sealed class MainMenuItemConfiguration : IEntityTypeConfiguration<MainMenuItem>
{
    public void Configure(EntityTypeBuilder<MainMenuItem> b)
    {
        b.ToTable("site_main_menu_items", "barang");
        b.HasKey(x => x.Id).HasName("pk_site_main_menu_items");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ItemType).HasColumnName("item_type").HasMaxLength(20);
        b.Property(x => x.PageId).HasColumnName("page_id");
        b.Property(x => x.TitleFa).HasColumnName("title_fa").HasMaxLength(120);
        b.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(120);
        b.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(120);
        b.Property(x => x.Url).HasColumnName("url").HasMaxLength(500);
        b.Property(x => x.IsPinned).HasColumnName("is_pinned");
        b.Property(x => x.DisplayOrder).HasColumnName("display_order");
        b.HasIndex(x => new { x.CompanyId, x.DisplayOrder });
        SiteColumns.Versioned(b);
    }
}

internal sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> b)
    {
        b.ToTable("site_blog_posts", "barang");
        b.HasKey(x => x.Id).HasName("pk_site_blog_posts");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(160);
        b.Property(x => x.TitleFa).HasColumnName("title_fa").HasMaxLength(250);
        b.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(250);
        b.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(250);
        b.Property(x => x.ExcerptFa).HasColumnName("excerpt_fa").HasMaxLength(600);
        b.Property(x => x.ExcerptEn).HasColumnName("excerpt_en").HasMaxLength(600);
        b.Property(x => x.ExcerptAr).HasColumnName("excerpt_ar").HasMaxLength(600);
        b.Property(x => x.BodyFa).HasColumnName("body_fa");
        b.Property(x => x.BodyEn).HasColumnName("body_en");
        b.Property(x => x.BodyAr).HasColumnName("body_ar");
        b.Property(x => x.ThumbnailStorageKey).HasColumnName("thumbnail_storage_key").HasMaxLength(400);
        b.Property(x => x.SeoDescriptionFa).HasColumnName("seo_description_fa").HasMaxLength(600);
        b.Property(x => x.SeoDescriptionEn).HasColumnName("seo_description_en").HasMaxLength(600);
        b.Property(x => x.SeoDescriptionAr).HasColumnName("seo_description_ar").HasMaxLength(600);
        b.Property(x => x.SeoKeywordsFa).HasColumnName("seo_keywords_fa").HasMaxLength(400);
        b.Property(x => x.SeoKeywordsEn).HasColumnName("seo_keywords_en").HasMaxLength(400);
        b.Property(x => x.SeoKeywordsAr).HasColumnName("seo_keywords_ar").HasMaxLength(400);
        b.Property(x => x.IsPublished).HasColumnName("is_published");
        b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.HasIndex(x => new { x.CompanyId, x.Slug }).IsUnique();
        SiteColumns.Versioned(b);
    }
}
