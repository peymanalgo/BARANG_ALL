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
