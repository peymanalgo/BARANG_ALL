# Runbook پشتیبان‌گیری و بازیابی BARANG Core (محلی/Docker)

**نسخه:** 1.0 — 2026-10-25
**دامنه:** استک `barang-core` روی `D:\00_BARANG\01_CORE\compose.yaml`
**وضعیت خودکارسازی:** هیچ زمان‌بند خودکار بکاپی پیکربندی نشده است؛ این سند رویه دستی است و باید پیش از هر تغییر مخرب (migration سنگین، تغییر schema، ارتقاء image) به‌صورت دستی اجرا شود.

## 1. داده‌های ماندگار (named volumes)

| Volume | سرویس | محتوا | اهمیت |
|---|---|---|---|
| `barang_postgres_data` | postgres | کل داده‌های کسب‌وکار + `barang.__ef_migrations_history` + Hangfire | حیاتی |
| `barang_minio_data` | minio | فایل‌ها/اسناد متصل به موجودیت‌ها | حیاتی |
| `barang_redis_data` | redis | کش و صف‌های زیرساختی با AOF (`--appendonly yes`) | متوسط (بازساخت‌پذیر) |
| `barang_api_keys` | api | کلیدهای data-protection ASP.NET | مهم (اعتبار کوکی/توکن) |
| `barang_backoffice_keys` | backoffice | کلیدهای data-protection Backoffice | مهم |

**ممنوعیت دائمی:** `docker compose down -v` و حذف/prune هر volume نام‌دار بدون اجازه صریح کاربر.

## 2. بکاپ PostgreSQL (رویه استاندارد)

```bash
# بکاپ کامل فرمت custom (فشرده، قابل pg_restore) — الگوی فایل‌های *_predeploy.backup در ریشه مخزن
docker exec barang-core-postgres-1 pg_dump -U barang -d barang_core -Fc \
  > barang_core_predeploy_$(date +%Y%m%d%H%M).backup
```

- گزینه پیشنهادی پیش از migration: `--schema=barang` کافی نیست؛ همیشه dump کامل (شامل hangfire و history) بگیرید.
- برای بکاپ متنی قابل diff: `-Fc` را با `-Fp` جایگزین کنید.

## 3. بازیابی PostgreSQL

```bash
# فقط با تأیید کاربر؛ بازیابی روی داده موجود مخرب است.
cat barang_core_predeploy_YYYYMMDDHHMM.backup | \
  docker exec -i barang-core-postgres-1 pg_restore -U barang -d barang_core --clean --if-exists
```

- در صورت خرابی کامل volume: کانتینر postgres را با volume تازه بالا بیاورید، سپس `pg_restore` بدون `--clean` اجرا کنید.
- پس از بازیابی: `SELECT count(*) FROM barang.__ef_migrations_history;` را با وضعیت مورد انتظار مقایسه کنید و `/health/ready` و ورود به Backoffice را کنترل کنید.

## 4. بکاپ MinIO

```bash
docker run --rm --network barang-core_default -v "%cd%":/backup \
  minio/mc:latest sh -c \
  "mc alias set local http://minio:9000 \$MINIO_ROOT_USER \$MINIO_ROOT_PASSWORD && mc mirror --overwrite local /backup/minio-snapshot"
```

- متغیرهای `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` از `.env` استک خوانده می‌شوند (مقادیر پیش‌فرض dev در compose؛ secret چاپ نشود).
- بکاپ PostgreSQL بدون بکاپ MinIO ناقص است: رکوردهای فایل به objectهای MinIO ارجاع می‌دهند.

## 5. بکاپ Redis و کلیدهای data-protection

- Redis با AOF ماندگار است؛ بکاپ روزانه لازم نیست. برای بکاپ نقطه‌ای: `docker exec barang-core-redis-1 redis-cli BGSAVE` و سپس کپی `appendonly.aos/dump.rdb` از volume.
- کلیدهای data-protection: اگر از دست بروند، همه نشست‌ها/کوکی‌ها باطل می‌شوند (بدون از دست رفتن داده). در صورت نیاز، volumeها را با `docker run --rm -v barang-core_barang_api_keys:/data alpine tar czf - /data` بایگانی کنید.

## 6. چک‌لیست پیش از عملیات مخرب

1. بکاپ PostgreSQL (بخش ۲) + بکاپ MinIO (بخش ۴) در مسیری خارج از مخزن.
2. `git status` تمیز یا تغییرات روی برنچ محافظت‌شده.
3. ثبت بکاپ در گزارش کار با نام فایل دقیق.
4. پس از عملیات: health check + یک سناریوی مرورگری واقعی + بررسی migration history.
