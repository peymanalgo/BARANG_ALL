# رجیستری metricهای داشبورد مدیریت

این رجیستری قرارداد عملیاتی Dashboard است؛ عددی که منبع canonical و scope مجاز ندارد در UI نمایش داده نمی‌شود.

| Metric | معنای دقیق و منبع | زمان/فیلتر | تفکیک و drill-down |
|---|---|---|---|
| سفارش‌های باز | `Orders` با وضعیت غیرنهایی؛ `Cancelled` و `Delivered` مستثنا | snapshot جاری؛ بازه روی این کارت اثر ندارد | `OrderRead`؛ `/workspace/orders` |
| وظایف معوق | `HumanTasks` با وضعیت `Open/Claimed` و `DueAtUtc < toUtc` | snapshot در لحظهٔ `toUtc`؛ شروع بازه اثر ندارد | `WorkflowRead`؛ `/workspace/cartable` |
| سفارش‌های دارای کمبود | تعداد `StockShortage` فعال با `ShortageQuantity > 0` | snapshot جاری؛ فقط کمبود unresolved | `InventoryRead`؛ `/workspace/inventory/purchasing` |
| دریافتنی معوق | مجموع `InvoiceSnapshot.FinalTotal` منهای `Payment` و به‌علاوه/کسر `Reversal` به ازای هر `CurrencyCode` | snapshot persisted؛ بدون تبدیل ارز | فقط `FinanceRead`؛ `/workspace/finance` |
| مراحل کارتابل | تعداد taskهای باز/گرفته‌شده به تفکیک `WorkflowStage`؛ مراحل موازی جمع mutually-exclusive نیستند | snapshot جاری | `WorkflowRead`؛ Cartable با stage filter |
| سفارش ثبت‌شده در روند | تعداد `Orders.CreatedAtUtc` در `[fromUtc,toUtc)` | activity؛ UTC و ابتدا شامل/انتها غیرشامل | `OrderRead`؛ Orders |
| تحویل ثبت‌شده در روند | تعداد رخدادهای `ShipmentStatusHistory.Delivered` در `[fromUtc,toUtc)` | activity؛ UTC و ابتدا شامل/انتها غیرشامل | `LogisticsRead`؛ Logistics |
| خلاصهٔ عملیات | Production jobهای non-terminal، QC inspectionهای non-final، PR+POهای non-closed و shipmentهای non-delivered/non-returned | snapshot جاری | فقط در صورت مجوز همان ماژول |

Currency و UOM هرگز با هم جمع نمی‌شوند. trendهای خالی یعنی «فعالیتی ثبت نشده»، نه صفر ساختگی یا unavailable. مقایسهٔ دوره‌ای، forecast، SLA target، سود، revenue، inventory valuation و KPIهای Dashboard مدیریت که در STEP۱۴ تعریف رسمی نداشتند عمداً در این نسخه live نشده‌اند.

قرارداد API: `GET /api/v1/management/dashboard?fromUtc=...&toUtc=...`، بازه حداکثر ۳۶۶ روز، UTC، inclusive-exclusive. پاسخ فقط company جاری و moduleهایی را برمی‌گرداند که actor مجوز read آن‌ها را دارد؛ نبود Finance permission حتی aggregate مالی را قابل استنتاج نمی‌کند.
