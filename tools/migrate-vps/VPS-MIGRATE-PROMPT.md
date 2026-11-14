# پرامپت انتقال کامل داده‌های سرور توسعه به VPS (بدون تصاویر بانک طرح)

> **راهنمای کاربر:** روی سرور توسعه باندل `C:\MIGRATE` آماده شده است (توضیح پایین). کل پوشه `C:\MIGRATE` را به VPS کپی کن (مثلاً `C:\MIGRATE`) و بعد متن پرامپت زیر را به Z AI روی VPS بده.
>
> **محتوای باندل:**
> - `barang_core_noartworks.sql.gz` — فول‌داپ دیتابیس (همه‌چیز: کاربران/نقش‌ها/دسترسی‌ها، مشتری‌ها، کیف پول، سفارش‌ها، محصولات و گونه‌ها و تصاویرشان، انبار RAWTEE و موجودی، تعرفه‌ها، CMS سایت، وبلاگ، داده‌های پایه و دسته‌ها) **بدون** جدول‌های طرح: `artworks / artwork_translations / artwork_versions / artwork_tag_assignments / file_versions / file_derivatives` — تصاویر بانک طرح بعداً جداگانه ایمپورت می‌شوند.
> - `minio-export/barang-public/...` — اشیای غیر-طرح MinIO (تصاویر گونه‌های محصول + تصاویر سایت CMS). اشیای طرح (`companies/*/files/*`) عمداً حذف شده‌اند.

---

## متن پرامپت (از اینجا به بعد را کپی کن):

من باندل انتقال را در `C:\MIGRATE` کپی کرده‌ام (داپ دیتابیس `barang_core_noartworks.sql.gz` + پوشه `minio-export`). سیستم BARANG روی این سرور **بدون داکر** اجراست (PostgreSQL و MinIO و API هرکدام پروسه/سرویس مستقل). می‌خواهم **تمام داده‌های سرور توسعه** را جایگزین داده‌های فعلی این سرور کنی — **به‌جز تصاویر بانک طرح** که بعداً جداگانه ایمپورت می‌شوند. مراحل:

### ۱) اطلاعات لازم را از من بگیر (حدس نزن)
- آدرس/پورت و اعتبارنامه PostgreSQL (کاربری که **مالک دیتابیس** است) و نام دیتابیس (معمولاً `barang_core`).
- آدرس/پورت و MINIO_ROOT_USER و MINIO_ROOT_PASSWORD سرور MinIO (از فایل env/config سرویس فعلی).
- مسیر سورس/سرویس API برای ری‌استارت.

### ۲) بکاپ ایمنی از وضعیت فعلی VPS
قبل از هر تغییری یک بکاپ کامل بگیر:
```
pg_dump -U <owner> -d <db> --no-owner --no-privileges | gzip > C:\MIGRATE\backup-vps-before.sql.gz
```
و از MinIO هم اگر غیرخالی است یک mirror به `C:\MIGRATE\minio-backup-before\` بگیر (با mc، پایین توضیح داده شده). اگر دیتابیس فعلی خالی/تازه است این مرحله را سریع رد کن ولی حتما انجامش بده.

### ۳) ریستور دیتابیس (جایگزینی کامل)
دیتابیس فعلی را خالی کن و دامپ را ریستور کن (psql روی خود سرور):
```
# بازکردن دامپ
gunzip -k C:\MIGRATE\barang_core_noartworks.sql.gz
# حذف کامل اسکیماهای موجود و بازسازی (با کاربر مالک)
psql -U <owner> -d <db> -c "DROP SCHEMA IF EXISTS barang CASCADE; DROP SCHEMA IF EXISTS hangfire CASCADE;"
psql -U <owner> -d <db> -f C:\MIGRATE\barang_core_noartworks.sql
```
- اگر چند خطاى FK جزئی دیدی (سطرهایی که به طرح‌های حذف‌شده اشاره داشتند) نگران نباش ولی **لیستشان را گزارش کن** — انتظار نمی‌رود خطایی باشد چون جدول‌های وابسته هم حذف شده‌اند.
- تاریخچه مایگریشن‌ها (`__ef_migrations_history`) داخل دامپ است؛ API هنگام استارت نباید مایگریشن جدیدی بخواهد — اگر خواست یعنی کد VPS قدیمی‌تر است: `git pull origin main` و بیلد مجدد API (مطابق پرامپت ایمپورت طرح، مرحله ۱).
- بعد از ریستور، API را ری‌استارت کن (`GET /health` → 200).

### ۴) ریستور اشیای MinIO (غیر از طرح‌ها)
ابزار `mc` لازم داری؛ اگر نصب نیست دانلودش کن (binary رسمی MinIO Client برای سیستم‌عامل این سرور). بعد:
```
mc alias set vps http://<minio-host>:<port> <MINIO_ROOT_USER> <MINIO_ROOT_PASSWORD>
mc mirror --overwrite C:\MIGRATE\minio-export\barang-public vps/barang-public
mc mirror --overwrite C:\MIGRATE\minio-export\barang-files   vps/barang-files   # اگر خالی بود مشکلی نیست
```
اگر باکت‌ها وجود ندارند، اول بساز: `mc mb vps/barang-public` و `mc mb vps/barang-files` (باکت عمومی باید public-read policy داشته باشد — با `mc anonymous set download vps/barang-public`).

### ۵) صحت‌سنجی (همه را انجام بده و گزارش بده)
1. `psql`: شمارش‌های کلیدی — `SELECT count(*) FROM barang.application_users;` (باید ≈۱۵ باشد)، `SELECT count(*) FROM barang.products;` (≈۸۰)، `SELECT count(*) FROM barang.stock_balances;` (۲۰ ردیف RAWTEE با on_hand)، `SELECT count(*) FROM barang.site_pages;` (۳) و `SELECT count(*) FROM barang.artworks;` (**باید ۰ باشد** — طرح‌ها جداگانه می‌آیند).
2. `GET /api/v1/auth/login` با اعتبارنامه ادمین سرور توسعه (از من بگیر) → توکن بگیر؛ بعد `GET /api/v1/public/wizard/config?culture=fa` با هدر `X-Company-ID` از appsettings → محصولات «تیشرت خام» ۴ رنگ با `imageUrl` سالم (URL تصویر از MinIO همین سرور، دانلودش 200).
3. بک‌آفیس (:5100 یا پورت واقعی) لاگین شود → منوی «مدیریت سایت B2C» و «بانک طرح» (خالی) و «انبار» با موجودی RAWTEE دیده شود.
4. سایت B2C: صفحه اول، منو/فوتر CMS، وبلاگ با ۲ پست.

### ۶) امنیت
بعد از تأیید نهایی، **رمز کاربر ادمین را عوض کن** (چون رمز محیط توسعه منتقل شده) و به من رمز جدید را تحویل بده؛ همچنین MINIO و PostgreSQL از بیرون قابل‌دسترسی نباشند (فقط localhost).

در پایان گزارش کامل بده: خروجی هر صحت‌سنجی، خطاهای FK (اگر بود)، تعداد اشیای MinIO ریستورشده، و وضعیت نتیجه.
