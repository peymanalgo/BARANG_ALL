# گزارش پذیرش اصلاحات موجودی

## نتیجه

این اصلاح در شاخه‌ی `codex/step2-inventory-unit-contracts` انجام شد. نقص اصلی، نبودن مسیر canonical فهرست اصلاحات در API بود؛ Backoffice مسیر `GET /api/v1/inventory/adjustments` را فراخوانی می‌کرد و پاسخ واقعی `404` می‌گرفت. در نتیجه‌ی پیاده‌سازی قبلی، خطا هم‌زمان با حالت موفقِ خالی نمایش داده می‌شد. مسیر فهرست به سرویس موجود Inventory متصل شد و stateهای loading، failure و successful-empty از هم جدا شدند.

## تغییرات

- مسیر `GET /api/v1/inventory/adjustments` با مجوز `InventoryRead` اضافه شد.
- قرارداد پاسخ شامل شناسه‌ی اصلاح، انبار، Variant/SKU، محصول، UOM، مقدار signed، دلیل، مرجع، زمان و actor است و از `StockTransactions` canonical با نوع `Adjustment` می‌خواند.
- فیلتر warehouse، جست‌وجوی reference/warehouse/product/SKU و pagination در همان سرویس اعمال می‌شود؛ scope شرکت از `ICompanyContext` و فیلترهای موجود persistence حفظ شده است.
- پاسخ‌های آرایه‌ای/صفحه‌ای موجود در selectorها و balances به‌درستی parse می‌شوند؛ این کار از استثنای Blazor هنگام انتخاب Variant جلوگیری کرد.
- خطای قطع/شکست API در component به پیام localized و Retry تبدیل می‌شود و Rows/TotalCount پاک می‌گردد؛ بنابراین failure هرگز `اصلاحی یافت نشد` را هم‌زمان نشان نمی‌دهد.
- گزینه‌ی navigation «برگشت warehouse» به `برگشت انبار` اصلاح شد.
- مسیر ثبت مثبت/منفی همان `POST /api/v1/inventory/adjustments/{increase}` باقی ماند؛ مسیر غیرموجود opening-balance دیگر برای ثبت استفاده نمی‌شود و قواعد ledger/UOM/audit سرویس canonical دست‌نخورده مانده‌اند.
- View از همان row-action menu عمودی استفاده می‌کند و Add به‌صورت standalone باقی است.

## بازتولید و علت‌ها

در استقرار توسعه‌ی اولیه با کاربر ادمین و شرکت فعال `42f0e459-6095-45bb-aa13-f38126d91d9c`، درخواست دقیق زیر ثبت شد:

`GET http://localhost:8080/api/v1/inventory/adjustments?search=&sort=date&descending=true&pageNumber=1&pageSize=20`

پاسخ `404` بود و لاگ API نیز `HTTP GET /api/v1/inventory/adjustments completed with 404` را ثبت کرد؛ خطای EF یا company-context وجود نداشت. علت، نبودن route/contract در API بود، نه مجوز یا داده‌ی خالی.

در seed ایزوله، یک `500` جداگانه نیز با بدنه‌ی نامعتبر `type: "RawMaterials"` برای endpoint انبار ایجاد شد. exception واقعی `JsonException` برای `CreateWarehouseRequest.Type` بود؛ seed با enum قراردادی `0` اصلاح شد و application code تغییر نکرد.

در پذیرش فرم، انتخاب Variant پاسخ آرایه‌ای `GET /api/v1/inventory/balances` را به‌عنوان object parse می‌کرد و exception زیر در circuit رخ داد: `The requested operation requires an element of type 'Object', but the target element has type 'Array'`. parser اصلاح شد و پاسخ‌های هر دو شکل پشتیبانی می‌شوند.

## محیط ایزوله

- Compose project: `barang-stock-adjust-acceptance`
- API: `http://localhost:5808`
- Backoffice: `http://localhost:5510`
- PostgreSQL: `127.0.0.1:5547`, database `barang_stock_adjust_browser_test`
- Redis: `6387`; MinIO: `9050/9051`
- volumeها با prefix همین project ساخته شدند و به `barang_core` یا volume توسعه متصل نبودند.
- شرکت: `42f0e459-6095-45bb-aa13-f38126d91d9c`
- identity: ادمین ایزوله‌ی `admin`؛ warehouse `ADJ-W`، product `ADJ-P`، variant `ADJ-SKU`، reason `cycle`.
- migration chain رسمی روی database ایزوله اعمال شد و role دسترسی فقط در همان database تنظیم شد.

## شواهد آزمون

| آزمون | نتیجه | شواهد |
|---|---:|---|
| `StockAdjustmentListContractTests` | PASS 1/1 | `tests/BARANG.Core.IntegrationTests/TestResults/stock-adjustments-fresh-5.trx` |
| Integration کامل current source | PASS 136/136 | `tests/BARANG.Core.IntegrationTests/TestResults/stock-adjustments-full-integration.trx`؛ زمان 3m54s؛ database ایزوله |
| Unit کامل | PASS 233/233 | `tests/BARANG.Core.UnitTests/TestResults/stock-adjustments-unit.trx` |
| Release API | PASS | بدون warning/error |
| Release Backoffice | PASS | بدون warning/error |
| browser runner مستقل با system Chrome | PASS | `tests/Acceptance/StockAdjustmentsBrowserRunner`; فهرست، search، empty state، Add، warehouse/product/Variant/reason، ثبت مثبت، reload، row View، screenshot `.artifacts/stock-adjustments-browser.png` |
| UI state branch | PASS در کد/قرارداد | `LoadError` قبل از empty branch و Retry؛ endpoint failure از UI empty جداست |

دستور browser runner:

```powershell
$env:BARANG_E2E_BASE_URL='http://localhost:5510'
$env:BARANG_E2E_API_URL='http://localhost:5808'
$env:BARANG_E2E_COMPANY_ID='42f0e459-6095-45bb-aa13-f38126d91d9c'
$env:BARANG_ADJUSTMENT_WAREHOUSE_ID='c092f8e4-4bf8-4434-879c-38e27a09f27d'
dotnet run --project tests/Acceptance/StockAdjustmentsBrowserRunner/StockAdjustmentsBrowserRunner.csproj -c Release --no-build
```

ثبت موفق در browser با reference یکتا انجام شد و پس از reload در جدول و View جزئیات باقی ماند. تعداد، UOM، warehouse، SKU و reference از پاسخ API و فهرست UI مستقل از empty-state کنترل شدند. هیچ رکوردی در توسعه برای این پذیرش استفاده نشد.

`BARANG.Backoffice.E2ETests` با runner استاندارد VSTest همچنان پیش از discovery با خطای زیر متوقف می‌شود و این نقص runner است، نه شکست application: `Catastrophic failure: Test process did not return valid JSON (non-object)`. برای همین، browser evidence با runner مستقل `Microsoft.Playwright` و Chrome نصب‌شده جمع‌آوری شد.

## وضعیت استقرار

commit نهایی application/test/report این تغییر: `623051d5947002991db31a539ddc028107e5ade9`.

استقرار توسعه‌ی موجود با Compose project `barang-core` بدون reset یا migration جدید به‌روزرسانی شد. هر دو سرویس از همین SHA ساخته شدند:

- API image: `sha256:bd84ad0c674c7d49d40c2c23bd0e13b4c4eed4e9a37f780fa889925877b0410d`
- Backoffice image: `sha256:dd9ac38bc09bd7c16038627d106f8ef565dad8e6ce22c7f7ba9bec9d030225ff`
- runtime `BARANG_BUILD_COMMIT` در هر دو container: `623051d5947002991db31a539ddc028107e5ade9`
- API: `http://localhost:8080`, readiness HTTP 200 / `Healthy`
- Backoffice: `http://localhost:5100/login`, HTTP 200

با system Chrome و کاربر مجاز توسعه، مسیر `/workspace/inventory?section=adjustments` به‌صورت read-only بازخوانی شد: page smoke با rows=0، بدون load error و بدون console error، standalone Add حاضر و screenshot در `.artifacts/stock-adjustments-development-smoke.png` ثبت شد. هیچ mutation در توسعه انجام نشد.

volumeهای `barang-core`، database، keys، business records، admin credentials و permissions حفظ شدند. migration جدید لازم نبود.

## محدودیت باقی‌مانده

هیچ migration یا تغییر داده‌ی توسعه برای این fix لازم نیست. تست browser خطای سرویس قطع‌شده را عمداً با توقف API در runner اجرا نکرد، چون Backoffice server-side در نبود upstream کل circuit ابتدایی را نیز reset می‌کند؛ مسیر واقعی 404 و branch خطای HTTP در UI بازتولید و تفکیک شده و handling قطع اتصال نیز برای جلوگیری از circuit exception اضافه شده است. این موضوع به‌عنوان محدودیت runner ثبت شده، نه به‌عنوان موفقیت جعلی browser failure test.
