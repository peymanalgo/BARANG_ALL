# استقرار فروشگاه B2C روی VPS ویندوزی — runbook

این runbook برای سرور **Windows Server** است. فروشگاه یک اپ ASP.NET Core 10
(SSR Razor Pages) است که با Core API (روی همان سرور، Docker پورت 8080) حرف می‌زند.

---

## ۰) پیش‌نیازهای سرور
- **.NET 10 ASP.NET Core Hosting Bundle** (شامل runtime + ANCM برای IIS):
  از dotnet.microsoft.com دانلود و نصب؛ سپس `net stop was /y && net start w3svc` اگر IIS هست.
- دسترسی Administrator و PowerShell.
- API مرکزی در دسترس: `curl http://localhost:8080/health` → 200.
- DNS: یک رکورد A/CNAME برای دامنه فروشگاه (مثلاً `store.barang.ir`) به IP سرور.
- فایروال: پورت‌های 80/443 باز؛ پورت داخلی سرویس (5180) فقط localhost.

## ۱) بیلد و publish (روی ماشین توسعه)
```powershell
cd D:\00_BARANG\02_WebAPP_B2C\deploy
./publish.ps1            # خروجی: deploy\publish\store
```

## ۲) انتقال به سرور
پوشه `deploy\publish\store` را به سرور کپی کنید (RDP/robocopy/SCP). مثال:
```powershell
robocopy .\publish\store \\VPS\c$\BARANG\store /MIR
```

## ۳) پیکربندی production
در سرور فایل `C:\BARANG\store\appsettings.Production.json`:
```json
{
  "CoreApi": {
    "BaseUrl": "http://localhost:8080/",        // API روی همان سرور (Docker)
    "CompanyId": "<شناسه شرکت عمومی فروشگاه>"
  },
  "Store": {
    "BaseUrl": "https://store.barang.ir"        // برای sitemap و آدرس‌های مطلق
  }
}
```
> CompanyId همان شرکتی است که در API با `Catalog:PublicCompanyId` تنظیم شده —
> همان شرکتی که `GET /api/v1/public/catalog` طرح‌هایش را می‌دهد.

## ۴) اجرا — دو گزینه
### گزینه A) Windows Service (پیشنهادی؛ بدون IIS)
```powershell
cd <پوشه deploy روی سرور>
./install-service.ps1 -InstallDir C:\BARANG\store -Port 5180
# تست:
curl http://127.0.0.1:5180/health
```
سرویس `BARANG.Store` با بازیابی خودکار نصب می‌شود.

### گزینه B) IIS
1. IIS Manager → سایت جدید (مثلاً store.barang.ir) → Physical path = پوشه publish.
2. فایل `web.config.iis.sample` را به `web.config` تغییر نام دهید (در همان پوشه هست).
3. App Pool: No Managed Code، Identity = ApplicationPoolIdentity.
4. تست: `https://store.barang.ir/health`.

## ۵) HTTPS (TLS)
- **win-acme (پیشنهادی):** `wacs.exe` → دامنه را انتخاب → certificate به IIS bind شود
  (اگر Windows Service + Kestrel مستقیم است، وین‌اکت‌می سرت را در Certificate Store می‌گذارد؛
  ساده‌تر: IIS به‌عنوان reverse proxy).
- **الگوی پیشنهادی نهایی:** IIS فقط به‌عنوان reverse proxy/TLS terminator →
  URL Rewrite/ARR به `http://127.0.0.1:5180`؛ اپ خودش `X-Forwarded-Proto` را
  می‌فهمد (ForwardedHeaders فعال است) و لینک‌ها https می‌شوند.

## ۶) به‌روزرسانی نسخه‌های بعدی
```powershell
./update.ps1 -PublishDir <پوشه publish جدید> -InstallDir C:\BARANG\store
```
(سرویس را می‌بندد، کپی می‌کند، `appsettings.Production.json` سرور را حفظ می‌کند و بالا می‌آورد.)

## ۷) چک‌لیست پس از استقرار
- [ ] `/health` → `{"status":"ok"}`
- [ ] `/` طرح‌ها را نشان می‌دهد (اتصال API + تصاویر MinIO public)
- [ ] تصاویر از آدرس عمومی MinIO باز می‌شوند (اگر تصاویر خالی‌اند:
      `MINIO_PUBLIC_ENDPOINT` در API باید دامنه/پورت عمومی MinIO باشد)
- [ ] ورود مشتری + ساخت پروفایل + ثبت یک سفارش آزمایشی
- [ ] `/sitemap.xml` با دامنه نهایی (Store:BaseUrl)؛ robots.txt
- [ ] ثبت sitemap در Google Search Console
- [ ] لاگ‌ها: `C:\BARANG\store\logs` (IIS) یا Event Viewer (سرویس)

## ۸) معماری نهایی روی سرور
```
Internet ──443──> IIS (TLS + reverse proxy) ──5180──> BARANG.Store (Windows Service)
                                                        │ HTTP (Bearer + X-Company-ID)
                                                        ▼
                                              Core API (Docker :8080) ──> PostgreSQL / MinIO / Redis
```
