# پرامپت همگام‌سازی کامل VPS (همه داده‌ها به‌جز تصاویر ۲۲هزارتایی طرح)

> **کاربر — قبل از دادن پرامپت:** پوشه تازه `C:\MIGRATE` را (شامل `barang_core_noartworks.sql.gz` جدید + `minio-export/`) به VPS کپی کن (مثلاً `C:\MIGRATE`). گیت هم تا کامیت `5978f2a` کامل پوش شده است.

---

## متن پرامپت (کپی کن):

روی این سرور VPS سیستم BARANG بدون داکر اجرا می‌شود. باندل تازه همگام‌سازی را در `C:\MIGRATE` گذاشته‌ام: دامپ کامل دیتابیس **بدون جدول‌های طرح** (`artworks/artwork_translations/artwork_versions/artwork_tag_assignments/file_versions/file_derivatives` — طرح‌ها جداگانه ایمپورت می‌شوند و دست نمی‌خورند) + اشیای MinIO غیر-طرح (تصاویر گونه‌ها و سایت). می‌خواهم **تمام داده‌های سرور توسعه جایگزین داده‌های فعلی این سرور شود، به‌جز تصاویر و ردیف‌های بانک طرح**. مراحل:

### ۱) اطلاعات لازم را از من بگیر (حدس نزن)
اعتبارنامه PostgreSQL با کاربر **مالک** دیتابیس + نام دیتابیس؛ MINIO_ROOT_USER/PASSWORD و آدرس/پورت MinIO؛ نام سرویس‌های API و فروشگاه (BarangB2C).

### ۲) حفاظت از طرح‌های موجود
قبل از هر کاری، طرح‌های فعلی این سرور را امن کن:
```
psql -U <owner> -d <db> -At -c "SELECT count(*) FROM barang.artworks;"
```
این عدد را یادداشت کن و در پایان دوباره بگیر — **باید یکسان باشد** (دامپ شامل جدول طرح نیست، ولی اسکیمای barang کامل DROP می‌شود؛ پس اول طرح‌ها را جدا کن):
```
pg_dump -U <owner> -d <db> --no-owner --no-privileges -t barang.artworks -t barang.artwork_translations -t barang.artwork_versions -t barang.artwork_tag_assignments -t barang.file_versions -t barang.file_derivatives -t barang.artwork_tags | gzip > C:\MIGRATE\backup-vps-artworks.sql.gz
```
همچنین بکاپ کامل وضعیت فعلی: `pg_dump -U <owner> -d <db> --no-owner | gzip > C:\MIGRATE\backup-vps-full-before.sql.gz`

### ۳) ریستور دامپ جدید
```
gunzip -k C:\MIGRATE\barang_core_noartworks.sql.gz   ← نام دقیق: barang_core_noartworks.sql.gz
psql -U <owner> -d <db> -c "DROP SCHEMA IF EXISTS barang CASCADE; DROP SCHEMA IF EXISTS hangfire CASCADE;"
psql -U <owner> -d <db> -f C:\MIGRATE\barang_core_noartworks.sql   ← همان فایل gunzip شده
```
سپس طرح‌های جدا شده را برگردان:
```
psql -U <owner> -d <db> -f C:\MIGRATE\backup-vps-artworks.sql
```
(جدول‌های طرح با داده‌های قبلی خودشان برمی‌گردند — شمارش نهایی باید با شمارش مرحله ۲ یکی باشد.)

### ۴) اشیای MinIO
با mc (اگر نیست نصبش کن):
```
mc alias set vps http://<minio>:<port> <user> <pass>
mc mb --ignore-existing vps/barang-public
mc anonymous set download vps/barang-public
mc mirror --overwrite C:\MIGRATE\minio-export\barang-public vps/barang-public
```

### ۵) کد و سرویس‌ها
مخزن `git pull origin main` (باید `5978f2a` باشد؛ اگر نه، بکاپ بگیر و pull کن) → API و Backoffice را با روش دیپلوی استاندارد این سرور بازساخت/ری‌استارت کن → سرویس فروشگاه (BarangB2C) را ری‌استارت کن. `GET /health` و سایت باید 200 بدهند.

### ۶) صحت‌سنجی (همه را انجام بده و گزارش بده)
1. شمارش طرح‌ها = عدد مرحله ۲ (تصاویر دست‌نخورده).
2. `SELECT count(*) FROM barang.lookup_value_translations WHERE culture='es';` = ۲۵۶۲ و `count(*) FROM barang.artwork_translations WHERE culture='es';` = ۲۲۱۰۴.
3. `SELECT count(*) FROM barang.product_variants WHERE image_storage_key IS NOT NULL;` = ۷۷ و `SELECT count(*) FROM barang.stock_balances;` = ۲۱.
4. `curl "http://localhost:<api-port>/api/v1/public/wizard/config?culture=es"` → محصولات «تیشرت خام» با imageUrl سالم (تصویر از MinIO همین سرور 200).
5. سایت در چهار زبان: fa منو فارسی؛ es منو «Inicio · Empezar a diseñar · Diseños · Impresión y embalaje · Acerca de Peyrang · Contacto · Blog» و دسته‌بندی‌های صفحه اول اسپانیایی و متن فوتر ستون ۱ اسپانیایی.
6. بک‌آفیس: انبار RAWTEE موجودی، منوی مدیریت سایت B2C، داده پایه چهارجانبه (فیلد اسپانیایی).
7. در پایان **رمز ادمین را عوض نکن** — این دامپ همان اعتبارنامه سرور توسعه را دارد که قبلاً روی VPS فعال بود؛ اگر فرق دارد از من بپرس.

گزارش نهایی: هر صحت‌سنجی + شمارش طرح قبل/بعد + تعداد اشیای MinIO + هر خطا و راه‌حل.
