# گزارش پذیرش STEP ۱۱ — Logistics

تاریخ اجرای این گزارش: ۲۰۲۶-۱۰-۱۶

## دامنه و تصمیم‌های مورد استفاده

- Packaging/shipping/delivery با سرویس canonical Logistics و evidence persisted انجام می‌شود؛ state machine موازی یا carrier خارجی اضافه نشده است.
- Packaging برای هر OrderItem فقط تا مقدار QC پذیرفته‌شده، بدون rejected یا reproduction-required، allocation را می‌پذیرد.
- Dispatch و Delivery اقدام‌های جداگانه و مجاز هستند؛ tracking قبل از dispatch لازم است.
- Delivery مقدار تجمعی shipmentهای تحویل‌شده را بر اساس OrderItemId محاسبه می‌کند و Order را فقط پس از تکمیل همه‌ی اقلام Delivered می‌کند.
- `ShipmentReturn` بازگشت مشتری را با لینک دقیق shipment/order/item، UOM، مقدار، علت، actor و idempotency ثبت می‌کند. بازگشت به‌طور خودکار stock، refund، payment یا balance ایجاد نمی‌کند.

تصمیم‌های خارج از دامنه: carrier خارجی، refund/payment خودکار، تبدیل بازگشت به stock قابل‌استفاده، و سیاست جدید QC برای receipt. برای آثار نهایی بازگشت بر status سفارش، قرارداد موجود سیاست مستقلی ندارد؛ در این مرحله status تحویل تاریخی تغییر داده نمی‌شود و disposition بعدی باید صریح باشد.

## تغییرات پیاده‌سازی

| ناحیه | نتیجه |
|---|---|
| Domain | `OrderStatus.Delivered` و انتقال idempotent؛ `ShipmentReturn` immutable با idempotency key |
| Logistics service | کنترل accepted-QC quantity، tracking اجباری، cumulative delivery، customer-return limit و company/order/item/UOM validation |
| Workflow | Packaging/Shipping/Delivery در adapter عمومی بدون evidence معتبر تکمیل نمی‌شوند؛ rejection عمومی این سه stage bypass نیست |
| API | `POST /api/v1/logistics/shipments/{id}/returns` با `delivery.manage` |
| Backoffice | مسیر `/workspace/logistics/returns` برای ثبت بازگشت traceable با مقدار، UOM، علت و کلید idempotency |
| Database | migration `20261016135708_AddShipmentReturns`; جدول company-scoped با FKهای restrictive و unique company/idempotency |

## شواهد اجرا

- Release build: PASS، `dotnet build BARANG.Core.slnx -c Release --no-restore`، ۰ warning/۰ error.
- Migration chain: از database خالی `barang_step11_test` روی PostgreSQL ایزوله port 5569 تا `AddShipmentReturns` با موفقیت اعمال شد؛ development database در این اجرا تغییر نکرد.
- Unit: پس از build نهایی باید با summary ثبت شود؛ تست جدید `Customer_return_requires_traceable_identity_and_is_idempotency_ready` به `LogisticsFoundationTests` افزوده شد.
- Backoffice: شمارش route به‌علت صفحه‌ی بازگشت از 46 به 47 به‌روزرسانی شد؛ تمام صفحات همچنان button type صریح دارند.
- API/browser feature matrix: اجرای authenticated disposable UI برای package→shipment→delivery و return باید پیش از اعلام PASS ثبت شود؛ smoke/health به‌تنهایی پذیرش feature محسوب نمی‌شود.

## ماتریس پذیرش

| سناریو | وضعیت فعلی | شاهد لازم |
|---|---|---|
| QC unresolved blocks packaging | Implemented | isolated accepted/rejected quantities + API response |
| same-SKU OrderItems independent | Implemented by OrderItemId/package allocation | API/DB quantity assertions |
| partial shipment/delivery | Implemented | two shipment/delivery calls and Order status |
| duplicate dispatch/delivery | Existing idempotent transitions preserved | repeated authenticated action |
| invalid address/actor/company | Existing company/permission gates retained | authenticated negative matrix |
| generic completion bypass | Implemented gate in WorkflowBusinessCompletionAdapter | direct completion negative test |
| partial/full customer return | Implemented API/UI | isolated delivered shipment and retry |
| return no stock/finance side effect | Implemented by separate ShipmentReturn ledger | independent stock/balance assertions |
| concurrency/rollback | Existing transaction/concurrency architecture retained; return unique key added | dedicated isolated concurrent run |

## وضعیت نهایی

تا زمان اجرای authenticated browser matrix، وضعیت STEP ۱۱ **CONDITIONAL / NOT ACCEPTED** است. هیچ deployment توسعه‌ای یا mutation روی business data توسعه در این مرحله از گزارش انجام نشده است. پس از اجرای matrix و regression کامل، همین فایل باید با commit/imageهای واقعی و نتیجه‌ی PASS/FAIL نهایی تکمیل شود.

## شواهد اجرای business-flow در محیط ایزوله

- محیط: Compose project `barang-step11-flow`، PostgreSQL `barang_step11_flow_test` روی port 5571، API روی 5871 و Backoffice روی 5570؛ جدا از `barang_core` و volumeهای development. company ایزوله: `7d8cf5fd-7c6c-4c10-9d95-9b3a6f8d5c11`.
- Backup توسعه: `D:\00_BARANG\20261016_step11_predeploy.dump`، PostgreSQL custom archive، 5,057,718 bytes؛ `pg_restore --list` با 1,750 TOC entries معتبر بود.
- Migration chain از DB خالی تا `20261016140407_AlignShipmentReturnIdempotency` با مسیر EF رسمی موفق شد؛ development data و volumeها در این acceptance دست‌نخورده ماندند.
- Release build: PASS، 0 warning/0 error. Unit `231/231` و Backoffice `184/184` PASS. Integration current-source `133/133` PASS در `4m 03s` با `BARANG_TEST_DB_CONNECTION` و hang diagnostics پنج‌دقیقه‌ای؛ TRX: `tests/BARANG.Core.IntegrationTests/TestResults/step11-integration-final.trx`.
- Browser runner مستقل Playwright با Chrome سیستم و login/company selection واقعی PASS: workspace بازگشت، فرم ۸ ورودی و Order 360؛ screenshotها `.artifacts/step11-browser/logistics-return-workspace.png` و `.artifacts/step11-browser/order360-return-status.png`.
- جریان mutation اول: shipment `28161e2c-4701-4935-b43e-13ec944e9681` با delivery و returnهای ۱ و ۲ واحدی؛ retry کلید دوم duplicate نساخت و over-return با HTTP 422 / `logistics.over_packaged` رد شد.
- جریان mutation UI: shipment `c86fc941-63df-4aa2-a932-eb9fab5a7fdc` برای item مستقل `3c0e3686-81f0-4779-8237-ae1616458175` با مقدار ۳ به Delivered رسید. ثبت return مقدار ۱ با `UI-RETURN-SUCCESS-001`، retry همان کلید، و over-return با `UI-RETURN-OVER-002` به‌ترتیب PASS، PASS بدون duplicate، و PASS rejection شدند.
- Persistence مستقل: `shipment_returns` سه ردیف و مجموع ۴ واحد دارد؛ هر ردیف shipment/order/item/UOM و idempotency key دارد. هر دو shipment Delivered/version 5 باقی ماندند، اما orderها به‌علت اقلام تحویل‌نشده Submitted ماندند؛ return وضعیت delivery/history را overwrite نکرد.
- اثر جانبی: `stock_transactions` فقط Adjustment و `WAREHOUSE_ISSUE` داشت و هیچ movement بازگشت ایجاد نشد؛ `payments` برای company ایزوله صفر بود.
- خطای قبلی `Test process did not return valid JSON` مربوط به discovery زیرساختی xUnit بود؛ runner مستقل با assertions واقعی UI اجرا شد. خطای اول UI نیز به selector input مخفی antiforgery مربوط بود و با `input:not([type=hidden])` اصلاح شد.

## نتیجه‌ی پذیرش

جریان کامل isolated package→shipment→delivery→customer return با assertions مستقل انجام شد و policy مصوب customer return **PASS** است. deployment توسعه‌ای باید پس از commit حاضر با provenance جدید انجام شود؛ این گزارش ادعای production deployment ندارد. خارج از scope: carrier خارجی، replacement، refund/payment خودکار، usable-stock disposition و receipt-QC hold.
