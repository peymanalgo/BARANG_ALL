# گزارش پذیرش داشبورد مدیریت BARANG

## نتیجه

داشبورد عملیاتی مدیریت در محیط ایزولهٔ populated با دادهٔ واقعیِ مجاز پذیرش شد. این تغییر فقط read-model و UI داشبورد را اضافه/اصلاح می‌کند؛ migration، reset داده، replay اعلان، تغییر credential یا تغییر workflow انجام نشده است.

## تعریف‌های مصوب و دامنه

رجیستری کامل metric در [MANAGEMENT_DASHBOARD_METRIC_REGISTRY_FA.md](MANAGEMENT_DASHBOARD_METRIC_REGISTRY_FA.md) ثبت شده است. کارت‌ها فقط از وضعیت‌های persisted و مجاز استفاده می‌کنند: سفارش باز، وظیفه overdue، shortage باز و دریافتنی به تفکیک currency. stageهای workflow هم‌پوشان‌اند و trendها در بازهٔ UTC با مرز `[from,to)` محاسبه می‌شوند.

Revenue/profit/cash، valuation موجودی، forecast، target/SLA جدید، تبدیل currency و جمع UOM ناسازگار عمداً live نشده‌اند؛ برای این موارد policy/source مصوب وجود ندارد.

## تغییرات فنی

- API bounded: `src/BARANG.Core.Api/BusinessAnalysis/ManagementDashboardEndpoints.cs`، با حداکثر بازهٔ ۳۶۶ روز، permission gating، فیلتر صریح CompanyId و transaction-local `barang.company_id` برای RLS.
- UI: `src/BARANG.Backoffice/Components/Pages/ManagementDashboard.razor` و `src/BARANG.Backoffice/wwwroot/management-dashboard.css`.
- یک مسیر canonical برای drill-down کمبود اصلاح شد: `/workspace/inventory?section=shortages`؛ مسیر قبلی `/workspace/inventory/purchasing` صفحهٔ موجودی را پوشش نمی‌داد.
- خطای نمایشی `}` در Needs Attention اصلاح شد؛ failure/loading/empty از هم جدا هستند و Retry در حالت خطا باقی می‌ماند.
- هیچ migration لازم نبود.

## محیط پذیرش ایزوله

| مورد | مقدار |
|---|---|
| Compose project | `dashboard-accept` |
| API / Backoffice | `http://127.0.0.1:5818` / `http://127.0.0.1:5822` |
| PostgreSQL | port `5579`, database `barang_dashboard` |
| Redis / MinIO | `5689` / `5910` |
| داده | volumeهای `dashboard-accept_barang_*`، جدا از `barang_core` |
| company | `9d1b5b2a-9e1a-4d5f-a5f3-6a1bb9fe7132` |
| fixture | سه Order باز، یک shortage فعال با available=0 و missing=2؛ stage خالی به‌علت نبود task باز واقعی |

زنجیرهٔ migration رسمی با `dotnet ef database update` روی این database اعمال شد. ساخت company پایه برای این disposable environment تنها bootstrap زیرساختی بود؛ user، membership، سفارش، کالا، warehouse و shortage از مسیرهای مجاز setup/API ساخته شدند. هیچ رکورد development دست‌کاری نشد.

## شواهد تست current-source

| بررسی | نتیجه | evidence |
|---|---:|---|
| API Release build | PASS، ۰ warning/۰ error | build جاری source |
| Backoffice Release build | PASS، ۰ warning/۰ error | build جاری source |
| Unit | 233/233 PASS | `tests/BARANG.Core.UnitTests/TestResults/management-dashboard-unit-final.trx` |
| Backoffice | 184/184 PASS | `tests/BARANG.Backoffice.Tests/TestResults/management-dashboard-backoffice-dashboard-final.trx` |
| Integration ایزوله | 136/136 PASS، ۴m۱۸s | `tests/BARANG.Core.IntegrationTests/TestResults/management-dashboard-integration-final.trx` |
| Browser populated | PASS | `tests/Acceptance/ManagementDashboardBrowserRunner/Program.cs` |

Integration با setup رسمی PostgreSQL و role initialization اجرا شد؛ سکوت بخشی از اجرا stall نبود و summary کامل 136/136 تولید شد.

## پذیرش مرورگر و تصاویر

Runner با system Chrome، login، company selection، route واقعی، و assertions اجرا شد:

`PASS management-dashboard ... metricCards=4 stages=0 attention=1`

- فارسی desktop populated: `.artifacts/management-dashboard-fa-desktop.png`
- فارسی mobile: `.artifacts/management-dashboard-fa-mobile.png`
- English: `.artifacts/management-dashboard-en.png`
- العربية: `.artifacts/management-dashboard-ar.png`

بررسی شد: کارت‌های populated، shortage واقعی در Needs Attention، click و navigation به `/workspace/inventory?section=shortages`، reload، fa/en/ar، RTL/LTR، mobile layout و نبود console error. stage خالی، empty state واقعی است نه صفر ساختگی. تصاویر task-local هستند و به Git commit نشده‌اند.

## عیب‌های یافته‌شده و اصلاح‌شده

1. داشبورد در queryهای مستقیم، transaction-local RLS context نداشت و با وجود رکورد مجاز `openOrders=0` می‌داد؛ با transaction و `set_config('barang.company_id', ..., true)` و CompanyId predicates اصلاح شد.
2. `WorkflowStage.ToString()` و `DateTimeOffset.Date` در EF به SQL ترجمه نمی‌شدند؛ grouping قابل ترجمه و تبدیل محدود در memory جایگزین شد.
3. Needs Attention به route غیرواقعی می‌رفت؛ به route موجود با query section اصلاح شد.

## استقرار development

پس از commit نهایی، API/Backoffice با همان source SHA و build provenance به compose project `barang-core` روی `localhost:8080` و `localhost:5100` deploy می‌شوند. volumeهای PostgreSQL/MinIO/Redis، keyها، business data و admin حفظ می‌شوند. نتیجهٔ image digest و smoke نهایی در همین report و manifest تکمیل می‌شود.

## محدودیت‌های باقی‌مانده

این dashboard جایگزین BI نیست. برای taskهای واقعی باید workflow/task fixture منتشر و assign شده باشد؛ نبود task در این fixture با empty state نشان داده شد. Metricهای نامشخص STEP14 همچنان عمداً نمایش داده نمی‌شوند.
