# پرامپت ایمپورت بانک طرح روی سرور VPS (پروداکشن — بدون داکر)

> **راهنمای کاربر:** این پرامپت را به Z AI روی سرور VPS بده.
> قبل از آن این‌ها را به VPS کپی کن:
> 1. عکس‌ها به `C:\IMAGE` — هر تعداد پوشه دسته‌بندی که خواستی (زیرمجموعه‌ای از ۳۳۰ دسته، با همان ساختار پوشه‌ها + نسخه‌های JPEG)
> 2. `D:\00_BARANG\tools\design-import` → `C:\IMAGE\design-import`

---

## متن پرامپت (از اینجا به بعد را کپی کن):

من روی این سرور VPS سیستم BARANG را **بدون داکر و مستقیم (bare-metal)** بالا آورده‌ام (API و Backoffice به‌صورت پروسه/سرویس در حال اجرا هستند). تصاویر بانک طرح را در `C:\IMAGE` کپی کرده‌ام — تعدادی پوشه دسته‌بندی (زیرمجموعه‌ای از ۳۳۰ دسته) همراه نسخه‌های JPEG. می‌خواهم **همان استاندارد محیط توسعه** اینجا هم اعمال شود و همه عکس‌های موجود در `C:\IMAGE` ایمپورت شوند. مراحل را دقیق و به‌ترتیب انجام بده:

### ۱) به‌روزرسانی کد سیستم (شرط اول)
آخرین کد از گیت‌هاب لازم است (شامل ستون‌های طبقه‌بندی طرح، موتور جستجوی مفهومی، ویزارد، تصویر گونه و …). در پوشه مخزن `BARANG_ALL`:
```
git pull origin main
```
سپس پروژه API را بیلد و پروسه در حال اجرا را جایگزین کن (پورت API را از appsettings/پروسه فعال پیدا کن؛ معمولاً `http://localhost:8080`). پس از ری‌استارت `GET /health` باید 200 بدهد.

### ۲) مایگریشن‌های دیتابیس
اگر API هنگام استارت خودش migrate کرد، گذر. اگر با خطای `must be owner of table ...` متوقف شد: با psql و کاربر **مالک** دیتابیس (اعتبارنامه از من بگیر) این SQL ها را اجرا کن و بعد هر مایگریشن را در `barang.__ef_migrations_history` (ستون‌های `"MigrationId"`,`"ProductVersion"` با مقدار `10.0.11`) ثبت کن تا EF رد شود:
```sql
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS subcategory_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS type_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS style_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS theme_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS audience_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS license_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS usage_id uuid;
ALTER TABLE barang.individual_customer_profiles DROP COLUMN IF EXISTS birth_certificate_number;
CREATE TABLE IF NOT EXISTS barang.site_settings (id uuid NOT NULL, company_id uuid NOT NULL, site_name_fa varchar(120) NOT NULL, site_name_en varchar(120) NOT NULL, site_name_ar varchar(120) NOT NULL, logo_storage_key varchar(400), favicon_storage_key varchar(400), slogan_fa varchar(300), slogan_en varchar(300), slogan_ar varchar(300), contact_email varchar(320), contact_phone varchar(64), created_at_utc timestamptz NOT NULL, updated_at_utc timestamptz NOT NULL, version bigint NOT NULL, CONSTRAINT pk_site_settings PRIMARY KEY (id));
CREATE UNIQUE INDEX IF NOT EXISTS ix_site_settings_company_id ON barang.site_settings(company_id);
CREATE TABLE IF NOT EXISTS barang.site_pages (id uuid NOT NULL, company_id uuid NOT NULL, slug varchar(120) NOT NULL, title_fa varchar(200) NOT NULL, title_en varchar(200) NOT NULL, title_ar varchar(200) NOT NULL, body_fa text NOT NULL, body_en text NOT NULL, body_ar text NOT NULL, cover_storage_key varchar(400), seo_description_fa varchar(600), seo_description_en varchar(600), seo_description_ar varchar(600), seo_keywords_fa varchar(400), seo_keywords_en varchar(400), seo_keywords_ar varchar(400), placement varchar(20) NOT NULL, footer_column int, is_published boolean NOT NULL, display_order int NOT NULL, created_at_utc timestamptz NOT NULL, updated_at_utc timestamptz NOT NULL, version bigint NOT NULL, CONSTRAINT pk_site_pages PRIMARY KEY (id));
CREATE UNIQUE INDEX IF NOT EXISTS ix_site_pages_company_id_slug ON barang.site_pages(company_id, slug);
CREATE TABLE IF NOT EXISTS barang.site_footer_widgets (id uuid NOT NULL, company_id uuid NOT NULL, column_index int NOT NULL, widget_type varchar(20) NOT NULL, page_id uuid, title_fa varchar(200), title_en varchar(200), title_ar varchar(200), text varchar(2000), html text, image_storage_key varchar(400), url varchar(500), display_order int NOT NULL, created_at_utc timestamptz NOT NULL, updated_at_utc timestamptz NOT NULL, version bigint NOT NULL, CONSTRAINT pk_site_footer_widgets PRIMARY KEY (id));
CREATE INDEX IF NOT EXISTS ix_site_footer_widgets_company_id_column_index ON barang.site_footer_widgets(company_id, column_index);
CREATE TABLE IF NOT EXISTS barang.site_main_menu_items (id uuid NOT NULL, company_id uuid NOT NULL, item_type varchar(20) NOT NULL, page_id uuid, title_fa varchar(120), title_en varchar(120), title_ar varchar(120), url varchar(500), is_pinned boolean NOT NULL, display_order int NOT NULL, created_at_utc timestamptz NOT NULL, updated_at_utc timestamptz NOT NULL, version bigint NOT NULL, CONSTRAINT pk_site_main_menu_items PRIMARY KEY (id));
CREATE INDEX IF NOT EXISTS ix_site_main_menu_items_company_id_display_order ON barang.site_main_menu_items(company_id, display_order);
CREATE TABLE IF NOT EXISTS barang.site_blog_posts (id uuid NOT NULL, company_id uuid NOT NULL, slug varchar(160) NOT NULL, title_fa varchar(250) NOT NULL, title_en varchar(250) NOT NULL, title_ar varchar(250) NOT NULL, excerpt_fa varchar(600) NOT NULL, excerpt_en varchar(600) NOT NULL, excerpt_ar varchar(600) NOT NULL, body_fa text NOT NULL, body_en text NOT NULL, body_ar text NOT NULL, thumbnail_storage_key varchar(400), seo_description_fa varchar(600), seo_description_en varchar(600), seo_description_ar varchar(600), seo_keywords_fa varchar(400), seo_keywords_en varchar(400), seo_keywords_ar varchar(400), is_published boolean NOT NULL, published_at_utc timestamptz, created_at_utc timestamptz NOT NULL, updated_at_utc timestamptz NOT NULL, version bigint NOT NULL, CONSTRAINT pk_site_blog_posts PRIMARY KEY (id));
CREATE UNIQUE INDEX IF NOT EXISTS ix_site_blog_posts_company_id_slug ON barang.site_blog_posts(company_id, slug);
ALTER TABLE barang.product_variants ADD COLUMN IF NOT EXISTS image_storage_key varchar(400);
INSERT INTO barang.permissions (id, code, module, technical_description) VALUES ('00000000-0000-0000-0000-000000000089','b2c.site.read','b2c','b2c.site.read'), ('00000000-0000-0000-0000-000000000090','b2c.site.manage','b2c','b2c.site.manage') ON CONFLICT (code) DO NOTHING;
```
نام مایگریشن‌ها برای ثبت در تاریخچه (به‌ترتیب، هرکدام که اجرا شد): `20261112173721_AddArtworkClassification`، `20261112194414_RemoveBirthCertificateNumber`، `20261113072059_AddSiteContent`، `20261113073802_SiteContentIdColumns`، `20261113083006_SiteContentSeo`، `20261114104148_AddVariantImage`.

### ۳) بررسی پیش‌نیازها
- Node.js ≥ 20 (`node -v`)؛ اگر نیست نصب کن.
- API بالا و سالم (`GET /health` → 200).
- MinIO اجرا و باکبت عمومی `barang-public` موجود باشد (اولین آپلود اگر مشکل باشد خطا می‌دهد).
- اعتبارنامه ادمین پروداکشن و اطلاعات psql (کاربر مالک دیتابیس) را از من بگیر — حدس نزن، بپرس.
- Company عمومی برای هدر `X-Company-ID` از کلید `Catalog:PublicCompanyId` در appsettings.

### ۴) اجرای اسکریپت‌ها به‌ترتیب (از `C:\IMAGE\design-import`)
```bash
export BARANG_API="http://localhost:8080"    # پورت واقعی API
export BARANG_USER="<admin>"                 # از من بگیر
export BARANG_PASS="<رمز>"                   # از من بگیر
export IMPORT_SRC="C:/IMAGE"                 # پوشه‌ای که پوشه‌های دسته‌بندی مستقیم داخلش هستند
export IMPORT_JPEG="C:/IMAGE"                # اگر JPEGها جای دیگری است اصلاح کن

node 00-folders.mjs            # ساختار پوشه‌ها — فقط پوشه‌های موجود
node 01-reference-data.mjs     # داده پایه (زیرگروه تا برچسب) — idempotent
node 02-categories.mjs         # دسته‌بندی‌های موجود در C:\IMAGE — idempotent
node 03-normalize-existing.mjs # فقط برای طرح‌های قدیمی با کد قدیمی؛ بی‌ضرر
node 04-import-artworks.mjs    # ایمپورت همه عکس‌ها — checkpoint (import-state.json)؛ قطع شد، دوباره اجرا کن
node 05-uppercase-codes.mjs    # یکدست‌سازی کدها — حتماً بعد از ۰۱ و ۰۲
node 06-classify.mjs           # تخصیص طبقه‌بندی + برچسب به همه طرح‌ها (نیازمند کد جدید از مرحله ۱)
```
نکته‌ها:
- ایمپورت idempotent و قابل ازسرگیری است؛ هر تعداد عکس که در `C:\IMAGE` باشد انجام می‌شود — لازم نیست ۲۲هزار تا باشد.
- فایل EPS بدون JPEG معادل رد می‌شود — در گزارش نهایی بیاور.
- برای تست سلامت مسیر، اول یکی-دو پوشه را در `IMPORT_SRC` بگذار و کامل اجرا کن؛ بعد بقیه پوشه‌ها را اضافه و ادامه بده.

### ۵) صحت‌سنجی نهایی (همه را انجام بده و گزارش بده)
1. `GET /api/v1/public/catalog?pageSize=1` → total = تعداد طرح‌های ایمپورت‌شده.
2. `GET /api/v1/public/catalog/concept-search?q=<واژه مرتبط>&culture=fa` → نتایج با `score` درصدی.
3. بک‌آفیس (ERP) → بانک طرح: چند طرح را باز کن — دسته، زیرگروه، نوع، سبک، موضوع، مخاطب، مجوز، کاربرد و برچسب‌ها انتخاب‌شده باشند؛ تصویر اصلی و بندانگشتی نمایش داده شود.
4. سایت B2C: کاتالوگ، جستجوی مفهومی و منو/فوتر سالم باشند (سیدر CMS بار اول خودکار است).
5. تعداد دسته‌های ساخته‌شده = تعداد پوشه‌های `C:\IMAGE`.

هر جا خطای مالکیت دیتابیس دیدی مرحله ۲ را کامل کن و ادامه بده. در پایان گزارش کامل بده: تعداد دسته‌ها، تعداد طرح‌های ایمپورت‌شده، ردشده‌ها با دلیل، و نتیجه صحت‌سنجی‌ها.
