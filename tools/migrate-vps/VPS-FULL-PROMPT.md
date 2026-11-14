# پرامپت یکجا: انتقال کامل سرور توسعه به VPS + ایمپورت تصاویر بانک طرح

> **راهنمای کاربر — این‌ها را قبل از دادن پرامپت به VPS کپی کن:**
> 1. `C:\MIGRATE` (باندل دیتابیس + اشیای MinIO غیر-طرح — روی سرور توسعه همین حالا آماده است) → مثلاً `C:\MIGRATE` در VPS
> 2. `D:\00_BARANG\tools\design-import` → `C:\IMAGE\design-import` در VPS
> 3. پوشه‌های دسته‌بندی عکس (هر تعداد که می‌خواهی، زیرمجموعه ۳۳۰ دسته + نسخه‌های JPEG) → داخل `C:\IMAGE`
>
> **ترتیب حیاتی است:** اول دیتابیس منتقل می‌شود، بعد عکس‌ها ایمپورت می‌شوند (اگر برعکس شود، ریستور دیتابیس طرح‌های ایمپورت‌شده را پاک می‌کند!).

---

## متن پرامپت (از اینجا به بعد را کپی کن):

من روی این سرور VPS سیستم BARANG را **بدون داکر (bare-metal)** اجرا می‌کنم (PostgreSQL، MinIO، API، Backoffice و سایت B2C هرکدام سرویس/پروسه مستقل). می‌خواهم در **یک نشست** دو کار را به‌ترتیب انجام بدهی:
- **فاز A:** انتقال کامل داده‌های سرور توسعه (کاربران، مشتری‌ها، سفارش‌ها، محصولات، انبار، CMS و …) — **به‌جز تصاویر بانک طرح**
- **فاز B:** ایمپورت تصاویر بانک طرح از `C:\IMAGE` (هر تعداد پوشه دسته که هست)

### ۰) اطلاعات لازم را از من بگیر (حدس نزن، بپرس)
- اعتبارنامه PostgreSQL (کاربر **مالک** دیتابیس) + نام دیتابیس (معمولاً `barang_core`)
- MINIO_ROOT_USER و MINIO_ROOT_PASSWORD و آدرس/پورت MinIO (از config سرویس فعلی)
- اعتبارنامه ادمین سرور توسعه (داخل دامپ) و رمز جدیدی که بعد از انتقال برای ادمین می‌خواهم
- مسیر مخزن سورس (`BARANG_ALL`) و نحوه ری‌استارت API

### فاز A — انتقال داده‌های سرور توسعه

**A1) به‌روزرسانی کد (شرط ادامه):** در مخزن `git pull origin main`، سپس API را بیلد و پروسه را جایگزین کن (فعلاً ری‌استارت نکن — اول A2/A3).

**A2) بکاپ ایمنی از وضعیت فعلی VPS:**
```
pg_dump -U <owner> -d <db> --no-owner --no-privileges | gzip > C:\MIGRATE\backup-vps-before.sql.gz
```
(اگر دیتابیس فعلی خالی/تازه است سریع رد شو ولی انجامش بده؛ از MinIO هم اگر غیرخالی است mirror به `C:\MIGRATE\minio-backup-before\` بگیر.)

**A3) ریستور دیتابیس (جایگزینی کامل):**
```
gunzip -k C:\MIGRATE\barang_core_noartworks.sql.gz
psql -U <owner> -d <db> -c "DROP SCHEMA IF EXISTS barang CASCADE; DROP SCHEMA IF EXISTS hangfire CASCADE;"
psql -U <owner> -d <db> -f C:\MIGRATE\barang_core_noartworks.sql
```
- دامپ شامل تاریخچه کامل مایگریشن‌هاست؛ پس API نبند مایگریشن بخواهد. اگر هنگام استارت خطای `must be owner` داد، طبق `tools/design-import/VPS-PROMPT.md` مرحله ۲ (SQL دستی + ثبت تاریخچه) را فقط برای مایگریشن‌های گفته‌شده انجام بده.
- حالا API را ری‌استارت کن → `GET /health` باید 200 بدهد.

**A4) ریستور اشیای MinIO (غیر-طرح):** ابزار `mc` را اگر نیست دانلود/نصب کن، بعد:
```
mc alias set vps http://<minio>:<port> <user> <pass>
mc mb --ignore-existing vps/barang-public
mc anonymous set download vps/barang-public
mc mirror --overwrite C:\MIGRATE\minio-export\barang-public vps/barang-public
```
(پوشه `barang-files` در باندل خالی است — اگر فایلی داشت همین‌طور mirror کن.)

**A5) چک‌پوینت فاز A:** با psql: `SELECT count(*) FROM barang.application_users;` (≈۱۵)، `count(*) FROM barang.products;` (≈۸۰)، `count(*) FROM barang.site_pages;` (۳)، `count(*) FROM barang.artworks;` (**باید ۰ باشد**). لاگین ادمین سرور توسعه به API باید توکن بدهد. اگر مشکلی هست همین‌جا متوقف شو و گزارش بده — **قبل از فاز B همه‌چیز باید سبز باشد.**

### فاز B — ایمپورت تصاویر بانک طرح از C:\IMAGE

**B0) پیش‌نیاز:** Node.js ≥ 20 (`node -v`؛ نیست نصب کن). Company عمومی از `Catalog:PublicCompanyId` در appsettings.

**B1) تست با یک پوشه:** اول فقط **یکی-دو پوشه دسته** را داخل `C:\IMAGE` بگذار و کل زنجیره را اجرا کن؛ بعد از تأیید، بقیه پوشه‌ها را اضافه کن و ادامه بده.

**B2) اجرای اسکریپت‌ها به‌ترتیب (از `C:\IMAGE\design-import`):**
```bash
export BARANG_API="http://localhost:8080"    # پورت واقعی API
export BARANG_USER="<admin توسعه>"           # از من بگیر
export BARANG_PASS="<رمز>"                   # از من بگیر
export IMPORT_SRC="C:/IMAGE"                 # جایی که پوشه‌های دسته مستقیم داخلش هستند
export IMPORT_JPEG="C:/IMAGE"                # اگر JPEGها جای دیگری است اصلاح کن

node 00-folders.mjs            # ساختار پوشه‌ها
node 01-reference-data.mjs     # داده پایه — idempotent (بیشترش از دامپ هست، رد می‌شود)
node 02-categories.mjs         # دسته‌های موجود در C:\IMAGE — idempotent
node 03-normalize-existing.mjs # بی‌ضرر
node 04-import-artworks.mjs    # ایمپورت عکس‌ها — checkpoint دارد (import-state.json)؛ قطع شد دوباره اجرا کن
node 05-uppercase-codes.mjs    # بعد از ۰۱ و ۰۲
node 06-classify.mjs           # تخصیص طبقه‌بندی + برچسب به همه طرح‌ها
```
- EPS بدون JPEG معادل رد می‌شود — در گزارش بیاور.

### صحت‌سنجی نهایی (هر دو فاز — گزارش کامل بده)
1. `GET /api/v1/public/catalog?pageSize=1` → total = تعداد طرح‌های ایمپورت‌شده.
2. `GET /api/v1/public/catalog/concept-search?q=<واژه>&culture=fa` → نتایج با `score`.
3. بک‌آفیس: بانک طرح — چند طرح با دسته/زیرگروه/نوع/سبک/موضوع/مخاطب/مجوز/کاربرد/برچسب کامل و تصویر+بندانگشتی؛ انبار RAWTEE با موجودی؛ منوی «مدیریت سایت B2C».
4. سایت B2C: کاتالوگ، جستجوی مفهومی، ویزارد (محصولات خام با تصویر و موجودی)، منو/فوتر/وبلاگ.
5. `count(*) FROM barang.artworks;` = تعداد عکس‌های `C:\IMAGE`.

### امنیت (آخر)
رمز کاربر ادمین را به رمز جدیدی که از من گرفتی تغییر بده (`POST /api/v1/security/...` یا مستقیم SQL طبق سیستم) و مطمئن شو PostgreSQL و MinIO فقط روی localhost قابل‌دسترسی‌اند.

در پایان گزارش یکجا بده: نتیجه فاز A (شمارش‌ها + اشیای MinIO)، نتیجه فاز B (تعداد دسته/طرح/ردشده‌ها با دلیل)، صحت‌سنجی‌ها، خطاها و راه‌حل‌هایشان.
