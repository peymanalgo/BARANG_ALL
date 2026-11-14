# پرامپت ایمپورت بانک طرح روی سرور VPS (پروداکشن)

> **راهنمای کاربر:** این پرامپت را بعد از تایید نهایی محیط توسعه، به Z AI روی سرور VPS بده.
> قبل از آن، این سه پوشه را به `C:\IMAGE` در VPS کپی کن:
> 1. `D:\Image\Original Files With 4 Letters Acronym Codes - Copy` → `C:\IMAGE\Original Files With 4 Letters Acronym Codes - Copy`
> 2. `D:\Image\SSTN ALL JPEG` → `C:\IMAGE\SSTN ALL JPEG`
> 3. `D:\00_BARANG\tools\design-import` → `C:\IMAGE\design-import`

---

## متن پرامپت (از اینجا به بعد را کپی کن):

من روی این سرور VPS سیستم BARANG را با docker compose بالا آورده‌ام. تصاویر بانک طرح را کپی کرده‌ام:

- `C:\IMAGE\Original Files With 4 Letters Acronym Codes - Copy` — ۳۳۰ پوشه دسته‌بندی (فایل‌های EPS/JPG مرجع برای دسته‌بندی)
- `C:\IMAGE\SSTN ALL JPEG` — نسخه‌های JPEG همه طرح‌ها (۲۲۰۹۹ فایل، همین‌ها آپلود می‌شوند)
- `C:\IMAGE\design-import` — اسکریپت‌های آماده ایمپورت (Node.js، بدون وابستگی)

از تو می‌خواهم داده‌های پایه و تمام تصاویر را دقیقاً مطابق محیط توسعه به سیستم ایمپورت کنی تا دو محیط یکسان باشند. مراحل:

### ۱) بررسی پیش‌نیازها
- Node نسخه ۲۰ یا بالاتر نصب باشد (`node -v`)؛ اگر نیست، نصبش کن.
- API سیستم بالاست؛ آدرس و پورت را از `docker ps` / سرویس‌های compose پیدا کن (معمولاً `http://localhost:8080` روی خود سرور).
- اعتبارنامه ادمین پروداکشن را از من می‌گیری (نام کاربری و رمز را از من بپرس — از تو درخواست کن).

### ۲) اجرای اسکریپت‌ها به ترتیب
از پوشه `C:\IMAGE\design-import`:

```bash
# متغیرها را تنظیم کن (رمز را از من بگیر)
export BARANG_API="http://localhost:8080"   # اگر پورت متفاوت است اصلاح کن
export BARANG_USER="<admin پروداکشن>"
export BARANG_PASS="<رمز>"
export IMPORT_SRC="C:/IMAGE/Original Files With 4 Letters Acronym Codes - Copy"
export IMPORT_JPEG="C:/IMAGE/SSTN ALL JPEG"

node 00-folders.mjs            # باید بگوید: پوشه‌ها: 330 · تکراری: هیچ
node 01-reference-data.mjs     # داده‌های پایه (زیرگروه‌ها تا برچسب‌ها) — idempotent
node 02-categories.mjs         # ۳۳۰ دسته‌بندی طرح — idempotent
node 03-normalize-existing.mjs # فقط اگر طرح‌های قدیمی با کد LIKE 'AMBULANCE-SSTN-%' وجود دارد؛ در غیر این صورت ۰ رکورد به‌روز می‌کند و بی‌ضرر است
node 04-import-artworks.mjs    # ایمپورت ۲۲۰۹۹ طرح — حدوداً ۱ تا ۲ ساعت
node 05-uppercase-codes.mjs    # یکدست‌سازی بزرگی کدهای دسته‌بندی/داده پایه — حتماً بعد از ۰۱ و ۰۲
node 06-classify.mjs           # تخصیص طبقه‌بندی (زیرگروه/نوع/سبک/موضوع/مخاطب/مجوز/کاربرد) + برچسب به همه طرح‌ها
```

### ۲-ب) پیش‌نیاز کد برای فاز ۶
فاز ۶ (تخصیص طبقه‌بندی) نیاز دارد که تغییرات کد `AddArtworkClassification` (ستون‌های
`subcategory_id/type_id/style_id/theme_id/audience_id/license_id/usage_id` روی جدول `artworks`
+ فیلدهای طبقه‌بندی در قرارداد `POST/PUT /api/v1/artworks` + دو endpoint جدید
`GET /api/v1/artworks/{id}/tags` و `DELETE /api/v1/artworks/{id}/tags/{tagId}`) از طریق فرایند
عادی deploy (git) روی این سرور اعمال شده و API با نسخه جدید بالا باشد. اگر مطمئن نیستی، با ساختن
یک طرح آزمایشی همراه `subcategoryId` و خواندن مجدد آن از API بررسی کن که فیلد برمی‌گردد؛ اگر
برنگشت، متوقف شو و به من گزارش بده که اول کد deploy شود.

اگر مهاجرت خودکار EF هنگام استارت API با خطای `must be owner of table artworks` شکست خورد، این
مهاجرت را دستی با کاربر مالک دیتابیس اجرا کن و بعد API را ری‌استارت کن:
```sql
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS subcategory_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS type_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS style_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS theme_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS audience_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS license_id uuid;
ALTER TABLE barang.artworks ADD COLUMN IF NOT EXISTS usage_id uuid;
INSERT INTO barang.__ef_migrations_history ("MigrationId","ProductVersion")
VALUES ('20261112173721_AddArtworkClassification','10.0.11') ON CONFLICT DO NOTHING;
```

اگر `docker exec` برای کانتینر Postgres لازم شد، نام کانتینر را با `PSQL_CONTAINER` بده (پیش‌فرض `barang-core-postgres-1`).

### ۳) نکته‌های مهم
- همه اسکریپت‌ها **idempotent** هستند: مقادیر و کدهای تکراری را رد می‌کنند و دوباره اجرا شدنشان بی‌ضرر است.
- فاز ۴ قابل ازسرگیری است؛ اگر وسط کار قطع شد، دوباره همان دستور را بزن — از `import-state.json` ادامه می‌دهد.
- حدوداً ۱۰ فایل EPS نسخه JPEG ندارند و در `skipped-no-jpeg.json` ثبت می‌شوند — این انتظاری و درست است.
- EPS مستقیماً قابل آپلود نیست (API فقط png/jpg/gif/webp/bmp/tiff/pdf می‌پذیرد)؛ به همین دلیل JPEG معادل آپلود می‌شود.
- بعد از هر آپلود، خود سیستم به‌صورت خودکار thumbnail (PNG 320px)، نسخه Web (WebP 1024) و PublicWeb (WebP واترمارک‌دار برای فروشگاه) می‌سازد. این پردازش پس‌زمینه است و ممکن است ساعت‌ها پس از پایان ایمپورت ادامه یابد — طبیعی است. اگر خواستی صف را بررسی کنی، تعداد job های Pending/Failed را در جدول `barang.file_processing_jobs` ببین؛ پس از ری‌استارت API هم job های ناتمام دوباره صف می‌شوند.

### ۴) راستی‌آزمایی نهایی
بعد از پایان فازها، گزارش بده:
1. تعداد کل طرح‌ها (باید ≈ ۲۲۶۹۳ باشد: ۲۲۰۹۹ جدید + ۵۹۴ ایمپورت قبلی، منهای ۱۰ فایل بدون JPEG)
2. تعداد دسته‌بندی‌های `artwork.category` (باید ۸۲۵ باشد)
3. تعداد مقادیر هر نوع داده پایه (زیرگروه ۲۰، نوع ۸، سبک ۲۰، موضوع ۲۵، مخاطب ۱۰، مجوز ۶، کاربرد ۱۰، برچسب ۱۲)
4. چند طرح نمونه را با API بگیر و نشان بده که thumbnail و ترجمه‌های fa/en/ar دارند.
5. با کوئری DB نشان بده که همه طرح‌های SSTN هر ۷ فیلد طبقه‌بندی پر شده‌اند
   (`count(*) FILTER (WHERE subcategory_id IS NOT NULL)` و … روی `artworks WHERE code LIKE '%-SSTN-%'`)
   و تعداد تخصیص برچسب فعال ≈ ۴۴٬۱۹۸ است (هر طرح: TAG2 جدید + TAG8 مناسب چاپ تیراژ).
6. اگر `import-errors.json` مواردی دارد، فهرستشان کن.

هیچ تغییری در کد سیستم نده؛ فقط داده از طریق API وارد شود (به‌روزرسانی کد طرح‌های قدیمی در فاز ۳ و تخصیص انبوه طبقه‌بندی در فاز ۶ مستقیماً در DB انجام می‌شود که در خود اسکریپت‌ها هست).

---
