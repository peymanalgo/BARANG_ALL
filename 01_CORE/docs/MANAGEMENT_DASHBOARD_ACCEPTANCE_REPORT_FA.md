# گزارش پذیرش داشبورد مدیریت BARANG

## وضعیت

این تغییر Dashboard placeholder را به یک operational view bounded تبدیل کرد. هیچ migration schema، reset داده، replay notification یا تغییر credential انجام نشده است.

## پیاده‌سازی

- API canonical: `src/BARANG.Core.Api/BusinessAnalysis/ManagementDashboardEndpoints.cs`
- UI: `src/BARANG.Backoffice/Components/Pages/ManagementDashboard.razor`
- CSS scoped page: `src/BARANG.Backoffice/wwwroot/management-dashboard.css`
- رجیستری metric: `docs/MANAGEMENT_DASHBOARD_METRIC_REGISTRY_FA.md`
- company isolation از `ICompanyContext` و query filters؛ permission gating برای Order/Workflow/Inventory/Finance/Production/Quality/Logistics/Purchasing.
- خواندن‌ها bounded هستند؛ trend بازهٔ حداکثر ۳۶۶ روز دارد و دادهٔ کامل entity به browser دانلود نمی‌شود.

## عمداً خارج از live dashboard

Revenue/profit/cash، inventory valuation، forecast، KPI/target جدید، currency conversion، UOM aggregation و trend comparison بدون policy/source رسمی نمایش داده نمی‌شوند. این موارد همان تصمیم‌های تعریف‌نشدهٔ STEP۱۴ هستند، نه صفر یا placeholder.

## شواهد اجرای جاری

| بررسی | نتیجه |
|---|---|
| API Release build، current source | PASS؛ ۰ warning/۰ error |
| Backoffice Release build، current source | PASS؛ ۰ warning/۰ error |
| Full Unit/Backoffice/Integration | پس از deployment candidate باید با SHA جدید دوباره ثبت شود |
| Browser populated acceptance | پس از deployment candidate و fixture ایزوله ثبت می‌شود |

## قرارداد UI

صفحه حالت loading، error+Retry و empty واقعی را از هم جدا می‌کند؛ company و بازهٔ UTC را نشان می‌دهد؛ کارت‌های metric، stageهای workflow، attention list، trendهای order/delivery، summaries و quick links دارد. Finance card بدون مجوز حذف می‌شود. fa/en/ar و RTL/LTR با theme فعلی، focus و reduced-motion رعایت شده‌اند.
