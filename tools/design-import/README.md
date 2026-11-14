# ایمپورت بانک طرح (داده‌های پایه + دسته‌بندی‌ها + تصاویر)

مجموعه اسکریپت‌های ایمپورت بانک طرح برای سیستم BARANG. با Node 20+ و بدون وابستگی اجرا می‌شوند.

## پیش‌نیازها
- دسترسی به API (پیش‌فرض `http://localhost:8080`)
- اعتبارنامه ادمین: `BARANG_USER` / `BARANG_PASS`
- پوشه تصاویر دسته‌بندی‌شده (`IMPORT_SRC`) و پوشه JPEGهای آماده (`IMPORT_JPEG`)
- برای عملیات‌های DB (فاز ۳): دسترسی `docker exec` به کانتینر Postgres

## متغیرهای محیطی
| متغیر | پیش‌فرض | توضیح |
|---|---|---|
| `BARANG_API` | `http://localhost:8080` | آدرس API |
| `BARANG_USER` / `BARANG_PASS` | `admin` / `BarangDev@2026` | اعتبارنامه |
| `IMPORT_SRC` | `D:/Image/Original Files With 4 Letters Acronym Codes - Copy` | پوشه مرجع دسته‌بندی |
| `IMPORT_JPEG` | `D:/Image/SSTN ALL JPEG` | پوشه فایل‌های JPEG |
| `IMPORT_WORKERS` | `4` | تعداد کارگران همزمان |
| `IMPORT_LIMIT` | `0` (همه) | محدود کردن تعداد برای تست |
| `PSQL_CONTAINER` | `barang-core-postgres-1` | کانتینر Postgres |

## ترتیب اجرا
```bash
node 00-folders.mjs            # تولید folders.json از ساختار پوشه‌ها
node 01-reference-data.mjs     # داده‌های پایه: زیرگروه/نوع/سبک/موضوع/مخاطب/مجوز/کاربرد/برچسب
node 02-categories.mjs         # ۳۳۰ دسته‌بندی طرح (کد = acronym چهارحرفی پوشه)
node 03-normalize-existing.mjs # (فقط اگر ایمپورت قدیمی با پیشوند نام پوشه وجود دارد)
node 04-import-artworks.mjs    # ایمپورت همه تصاویر (قابل ازسرگیری)
node 05-uppercase-codes.mjs    # یکدست‌سازی بزرگی کدها (بعد از ۰۱ و ۰۲؛ idempotent)
node 06-classify.mjs           # تخصیص انبوه طبقه‌بندی + برچسب به همه طرح‌ها (بعد از ۰۴ و ۰۵)
```

## پیش‌نیاز کد (مهم برای پروداکشن)
فاز ۶ به تغییرات کد نیاز دارد: ستون‌های طبقه‌بندی روی `artworks` (subcategory_id و…) + فیلدهای
`subcategoryId/typeId/styleId/themeId/audienceId/licenseId/usageId` در قرارداد `POST/PUT /api/v1/artworks`
+ دو endpoint جدید `GET/DELETE /api/v1/artworks/{id}/tags[/{tagId}]`. این تغییرات در مخزن
(`AddArtworkClassification` و فایل‌های Domain/Application/Infrastructure/Api) باید از طریق git روی سرور
deploy شده باشند. اگر مهاجرت خودکار هنگام استارت API با خطای `must be owner of table artworks` شکست خورد،
مهاجرت را دستی با کاربر مالک DB اجرا کنید (۷ ستون + ثبت ردیف در `barang.__ef_migrations_history`).

## نکته‌ها
- کد طرح‌ها: `<ACRONYM>-SSTN-<عدد>` (مثل `AMBU-SSTN-1028213668`)
- پس از آپلود، thumbnail (PNG 320) و نسخه‌های Web (1024) و PublicWeb (واترمارک‌دار) به‌صورت خودکار توسط پردازشگر پس‌زمینه ساخته می‌شوند — نیازی به آپلود جداگانه بندانگشتی نیست.
- EPS قابل آپلود نیست؛ JPEG معادل از `IMPORT_JPEG` آپلود می‌شود.
- فایل‌های بدون JPEG معادل در `skipped-no-jpeg.json` ثبت می‌شوند (۱۰ فایل EPS فقط).
- فایل وضعیت `import-state.json` بعد از هر آیتم به‌روز می‌شود؛ اجرای مجدد از همان‌جا ادامه می‌دهد.
- خطاها در `import-errors.json` ثبت می‌شوند.
