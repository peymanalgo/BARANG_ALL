# BARANG B2C Store (02_WebAPP_B2C)

فروشگاه عمومی طرح‌های چاپی برنگ برای وب‌سایت B2C و موبایل‌وب.

## Stack
- **ASP.NET Core 10 — Razor Pages (SSR)**: سئوی کامل برای کاتالوگ عمومی، بدون ابزار Node، استقرار ساده روی ویندوز سرور (IIS / Windows Service)
- **BFF pattern**: توکن مشتری هرگز به مرورگر نمی‌رود؛ صفحات از طریق `CoreApiClient` سمت سرور با Core API حرف می‌زنند
- OutputCache 60 ثانیه برای کاتالوگ عمومی، فونت متغیر محلی (Noto Sans Arabic + Inter)، RTL-first با پشتیبانی fa/en/ar

## اجرا (dev)
```
dotnet run --project BARANG.B2C.Web
```
پیش‌فرض روی http://localhost:5170 بالا می‌آید (پورت از launchSettings). Core API باید روی `CoreApi:BaseUrl` (پیش‌فرض http://localhost:8080) در دسترس باشد.

## استقرار روی VPS ویندوزی
1. `dotnet publish BARANG.B2C.Web -c Release -o publish`
2. IIS + ASP.NET Core Hosting Bundle (ANCM) — یا اجرا به‌صورت Windows Service با Kestrel
3. تنظیم `CoreApi:BaseUrl` به آدرس داخلی API روی همان سرور
4. تصاویر عمومی از MinIO public bucket سرو می‌شوند (`MINIO_PUBLIC_ENDPOINT` در API)

## نقشه راه
- فاز 1 ✅: اسکلت SSR + خانه/کاتالوگ/جزئیات طرح + ورود/خروج + تغییر رمز
- فاز 2: پنل مشتری (نشانی‌ها، سفارش‌های من، رهگیری)
- فاز 3: سبد خرید و ویزارد سفارش (آیتم + عملیات چاپ + قیمت + validate + submit)
- فاز 4: اعلان‌ها، جستجوی پیشرفته، بهینه‌سازی تصویر
- فاز 5: استقرار نهایی VPS + TLS + دامنه
- فاز 6 (آینده): ثبت‌نام خودکار مشتری، لینک کاربر↔مشتری، درگاه پرداخت
