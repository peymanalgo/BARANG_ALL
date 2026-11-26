# گزارش پذیرش STEP ۱۴ — Audit trails و operational reports

تاریخ بررسی: ۱۴۰۵/۰۷/۲۵ (۲۰۲۶-۱۰-۱۷)  
شاخه: `codex/step2-inventory-unit-contracts`  
مبنای پذیرفته‌شدهٔ STEP ۱۳: application `58e3538fad517f494c9da5be03144eb288058a55`، report `68f5080e139595dc677317260862982154f6a9d6`.

## نتیجهٔ اجرایی

وضعیت STEP ۱۴: **PASS**.

زیرساخت canonical audit، RLS و transaction از قبل وجود داشت و اصلاحات این مرحله فقط روی خواندن امن audit، semantics بازهٔ زمانی و صفحهٔ موجود Audit اعمال شد. reconciliation عملیاتی نیز روی read modelهای موجود با fixtureهای deterministic و expectedهای مستقل انجام شد. هیچ audit history، business record، credential، volume یا master data حذف یا تغییر داده نشد. تصمیم‌های آیندهٔ KPI/retention/export در بخش آخر عمداً خارج از پذیرش این STEP نگه داشته شده‌اند و مانع scope فعلی نیستند.

## ماتریس پوشش واقعی

| حوزه | نویسنده/منبع canonical | هویت ثبت‌شده | وضعیت و شواهد |
|---|---|---|---|
| Identity/RBAC | `SecurityAdministration`، `PermissionAuthorizationHandler`، `SecurityEndpoints` | actor، company، action، outcome، correlation | integrated؛ login/logout، role، permission، company access و denied security event در `AuditFoundationTests` |
| Master data / Lookup | `LookupAdministration`، `CatalogAdministration`، `CustomerAdministration`، `SupplierSourcingAdministration` | entity type/id، change kind، version در منبع | integrated؛ Product/Variant/Supplier/Equipment/Lookup و Organization writerهای صریح دارند |
| Customers/Suppliers | Customer/Supplier administration و profile services | company و exact entity identity | integrated؛ reference protection و company scope از سرویس مالک پیروی می‌کند |
| Orders | Order/Order360 timeline و audit calls موجود | order/order-item identity و workflow correlation | partial؛ گزارش Order 360 از timeline/owner read model می‌خواند؛ KPI مستقل جدید ساخته نشده است |
| Pricing/Finance | `PricingAdministration`، `FinanceAdministration` | pricing/finance entity، revision/snapshot در owner history و audit | integrated برای mutationها؛ مبالغ از snapshot/ledger خوانده می‌شوند و currency مخلوط نمی‌شود |
| Inventory/Purchasing | `InventoryAdministration` و Purchasing history/audit | warehouse، variant، reservation/movement/PR/PO identity | integrated؛ stock و receipt از ledger canonical می‌آیند، نه از جمع متن یا SKU |
| Workflow | `WorkflowAdministration`، runtime history | instance/task، actor و event | integrated؛ task history منبع وضعیت است و audit تغییرات workflow را تکمیل می‌کند |
| Production | `ProductionAdministration` و job history | job/order-item/operation identity | integrated؛ history عملیاتی جدا از audit immutable باقی می‌ماند |
| Quality | `QualityAdministration` و QC history | inspection/job/order-item identity | integrated؛ outcome و lineage از Quality owner خوانده می‌شود |
| Logistics | Logistics history و shipment/return audit | package/shipment/order-item identity | integrated؛ delivery و customer return جدا و traceable هستند |
| Notifications | `NotificationAdministration` و Outbox worker | outbox/notification/dead-letter identity | integrated؛ queue/read/retry audit شده و delivery منبع business state نیست |

اصل معماری حفظ شد: `IAuditWriter` opt-in است و SaveChanges interceptor عمومی وجود ندارد؛ بنابراین رویداد تکراری یا recursive برای هر entity خودکار ساخته نمی‌شود. سرویس‌هایی که owner history دارند به‌عنوان audit دوم و متناقض تکثیر نشدند.

## اصلاحات پیاده‌شده

1. `AuditReader` اکنون همیشه `CompanyId == ICompanyContext.RequireCompanyId()` را در query اعمال می‌کند؛ RLS همچنان defense-in-depth است. جزئیات یک audit خارج از شرکت جاری نیز با همین predicate محافظت می‌شود.
2. `ToUtc` در `AuditQuery` و API از این پس **exclusive** و `FromUtc` **inclusive** است. این قرارداد در UI نمایش داده و با بازه‌های مجاور تست می‌شود تا event دوبار شمرده نشود.
3. `AuditTrail.razor` از نمایش خام JSON به جدول خوانا با action/entity/actor/outcome، جزئیات correlation/source/failure، فیلتر action/entity/time، empty/loading/error state و pagination تبدیل شد. ورودی زمان UTC به‌صورت متن ISO است تا با قرارداد مرکزی تقویم Backoffice تعارض نداشته باشد.
4. UI از هیچ mutation audit پشتیبانی نمی‌کند؛ link یا داده‌ای هم دسترسی شرکت را دور نمی‌زند. redaction canonical برای password/token/secret/API key حفظ شد.

## گزارش‌ها و semantics

- گزارش‌های موجود از owner read model یا persisted history استفاده می‌کنند: Order 360 از snapshot/timeline، Finance از invoice/payment/history، Inventory از balance/reservation/transaction، Purchasing از receipt/PO، Production از job history، Quality از inspection history، Logistics از shipment/delivery/return و Notifications از outbox/notification/dead-letter.
- هیچ محاسبهٔ جدیدی برای جمع چند currency یا چند UOM اضافه نشد. چنین totalsی باید بر اساس currency/UOM جدا نمایش داده شود مگر policy conversion تصویب شود.
- `DateTimeOffset` در audit UTC ذخیره می‌شود. بازهٔ API از شروع شامل و پایان غیرشامل استفاده می‌کند؛ DateOnlyهای کسب‌وکاری نباید از timezone عبور کنند.
- Audit access با `audit.read`، company context و RLS محدود است؛ endpoint update/delete ندارد و payload خام request یا credential ذخیره نمی‌شود.
- `ManagementDashboard.razor` در source فعلی placeholder است و operational KPI contract مستقل و مستندی برای آن پیدا نشد؛ این مرحله BI/dashboard جدیدی اختراع نکرد.

## reconciliation عملیاتی با fixtureهای مستقل

در این بررسی، expectedها از مقادیر ثبت‌شده در fixture و قواعد persisted هر domain محاسبه و با پاسخ API/read model مقایسه شدند؛ production resolver یا خروجی همان report به‌عنوان expected دوباره استفاده نشد. تست‌های زیر روی PostgreSQL ایزوله با source جاری اجرا شدند و نتیجهٔ هر ردیف PASS بود:

| گزارش/read model و کنترل | fixture و منبع authoritative | کنترل مستقل و نتیجه |
|---|---|---|
| Order و Order 360 | `OrderE2ETests.DraftItemSubmitSearchTimelineConcurrencyCancellationAndIsolationFlow` و `Step5WorkflowInventoryGateTests.RuntimeInventoryStagesRequireCompleteEvidenceAndAreRetrySafe`؛ `orders`, `order_items`, timeline و task state | هویت `OrderItemId` مبنای محاسبه است؛ در سناریوی دو item با variant/SKU یکسان، مقادیر ۲ و ۳ مستقل باقی ماندند، Order 360 هر دو ID/quantity را جدا برگرداند و issueها دو movement جدا داشتند؛ هیچ جمع‌زدن بر اساس SKU انجام نشد. PASS |
| Invoice/payment/reversal | `FinanceE2ETests.PaymentReversalApprovalInvoiceHistoryAndIsolationFlow` و `ExplicitInvoiceIssuanceDoesNotAutoIssueAndRemainsUnpaidUntilPayment`؛ invoice snapshot، `payments` و reversal records | مبلغ مستقل fixture برابر ۲۰۰ بود؛ پرداخت ۵۰ و ۱۷۰، overpaid=۲۰ و reversal=۲۰، سپس `NetPaid=200` و تاریخچهٔ invoice/payment/reversal بدون join multiplication تأیید شد. invoice پس از issuance تا پرداخت، unpaid و `Remaining=FinalTotal` ماند. PASS |
| Stock/reservation/issue/receipt | `Step4InventoryIssueE2ETests`، `Step5WorkflowInventoryGateTests` و `Step8PurchasingProposalE2ETests`; stock balances/transactions/reservations/shortages/receipts | fixture با دو OrderItem هم‌کلید، on-hand=10 و تقاضای ۲+۳، `Reserved=5` و پس از issue `OnHand=5`, `Reserved=0`, دو `WAREHOUSE_ISSUE` داشت. receipt مقدار ۵، shortage را از ۸ به available=5 و missing=3 رساند و recheck ثبت شد؛ receipt خودکار reserve/task completion نکرد. PASS |
| Production / Quality / reproduction | `NotificationProducerPersistenceTests.QualityLogisticsAndSlaTransitionsPersistExactNotificationEvents` و STEP9/10 feature evidence recorded in the prior accepted report; `production_jobs`, `qc_inspections`, `reproduction_requests`, history | source identity به Job/Inspection/OrderItem و lineage متصل است؛ Quality rejection و reproduction event با همان identity و بدون stock/finance mutation ثبت شد. PASS |
| Partial/full delivery و customer return | Logistics feature evidence and `Order360Endpoints` read model; `shipments`, `shipment_packages`, `package_items`, `shipment_returns` | delivered quantity از delivered shipment allocations و return quantity جداگانه از `ShipmentReturn` جمع می‌شود؛ `remainingReturnable=max(0,delivered-returned)` و statusهای None/Partial/Full مستقل از Delivered هستند. دو item هم‌SKU از طریق `OrderItemId` جدا می‌مانند. PASS |
| Workflow workload/SLA | `NotificationProducerPersistenceTests.QualityLogisticsAndSlaTransitionsPersistExactNotificationEvents`; `human_tasks`, `workflow_sla_policies`, `workflow_escalations`, workflow history | SLA escalation فقط برای task واجد شرایط ساخته شد و اجرای تکراری دوم مقدار صفر داشت؛ workload KPI جدید تعریف نشد. PASS |

کنترل‌های فیلتر نیز بررسی شد: `AuditQuery.FromUtc` شامل و `ToUtc` غیرشامل است، بنابراین دو بازهٔ مجاور یک event را دوبار نمی‌شمارند؛ currency و UOM در read modelهای مالک جدا نگه داشته شده‌اند و تبدیل اختراعی وجود ندارد. Order 360 برای delivery/return، finance، inventory، production، quality و workflow از queryهای مستقل و company-scoped استفاده می‌کند؛ جمع‌زدن‌های one-to-many در پاسخ‌های عملیاتی به‌صورت cross-join انجام نشده است.

## آزمون و شواهد

| بررسی | نتیجه |
|---|---:|
| Audit unit (`AuditFoundationTests`) | **3/3 PASS** |
| Audit Integration روی PostgreSQL ایزوله (`AuditFoundationTests`) | **4/4 PASS**؛ شامل RLS، company isolation، redaction، rollback و inclusive/exclusive boundary |
| Unit کامل current source | **233/233 PASS**؛ TRX: `tests/BARANG.Core.UnitTests/TestResults/step14-unit.trx` |
| Backoffice کامل current source | **184/184 PASS**؛ TRX: `tests/BARANG.Backoffice.Tests/TestResults/step14-backoffice.trx` |
| Integration کامل current source | **135/135 PASS**؛ `BARANG_TEST_DB_CONNECTION` به DB ایزوله `barang_step10_quality_test` روی port 5569؛ TRX: `tests/BARANG.Core.IntegrationTests/TestResults/step14-integration.trx` |
| Release build | PASS، ۰ warning و ۰ error |
| Operational reconciliation tests (current source, isolated PostgreSQL) | **9/9 PASS**: Order 1، Finance 2، Inventory Issue 2، Workflow gate/Order 360 1، Purchasing proposal/receipt 1، Purchasing approval 1، Quality/Logistics/SLA producer 1؛ TRXهای `step14-*` و `step14-order360-reconciliation.trx` در `tests/BARANG.Core.IntegrationTests/TestResults/` |
| Browser acceptance | **7/7 PASS** با standalone xUnit v3 و system Chrome روی `http://localhost:5100`؛ route shell، fa/en/ar، RTL/LTR، sorting/action menu و Audit filters/table/empty state/hidden Blazor error؛ TRX: `.artifacts/step14-browser/step14-ui-final.trx` |

تست rollback نشان می‌دهد audit موفق داخل transaction شکست‌خورده باقی نمی‌ماند. تست security نیز actor/company/correlation، failed-login redaction، denied access و عدم مشاهدهٔ شرکت دیگر را بررسی می‌کند.

## Migration، backup و deployment

این تغییر migration schema ندارد. تست‌ها با migration chain رسمی روی PostgreSQL ایزوله اجرا شدند؛ volume و business data توسعه دست‌نخورده است. قبل از deployment هیچ reset، delete، deactivation، remap یا replay تاریخی انجام نشد.

پس از سبز شدن browser و build، API/Backoffice با Compose پروژهٔ `barang-core` از source application commit `127b0f6` build/deploy شد؛ migration جدیدی اعمال نشد و volumeهای PostgreSQL/Redis/MinIO حفظ شدند. provenance هر دو container `BARANG_BUILD_COMMIT=127b0f6` است:

- API image: `sha256:497656f9fe07442125f4f1dba21ebcda450db1d01c58e622d36f5e6105e712c0`؛ readiness روی پورت 8080 سالم.
- Backoffice image: `sha256:002abdd446966c1c3ce1584bccd7afe967d66a2d54018c25d9834e644c4d659b`؛ UI روی `http://localhost:5100` با login و browser acceptance تأیید شد.

deployment فقط local development است؛ production، workflow definition، keys، admin permissions و business records تغییر نکردند. تست E2E assertion در commit بعدی `62f171e` اصلاح شد و روی همین application pair دوباره 7/7 سبز اجرا شد.

## تصمیم‌های آینده، خارج از scope پذیرش STEP ۱۴

| موضوع | شواهد فعلی | تصمیم موردنیاز |
|---|---|---|
| KPIهای Management Dashboard | صفحه وجود دارد اما metric contract و definition رسمی برای workload، SLA، stock یا finance aggregate مستقل یافت نشد | تعیین metricها، source، timezone، currency/UOM و permission پیش از تکمیل dashboard؛ تا تصویب، KPI نمایش داده نمی‌شود |
| Audit retention/archive | سند فعلی retention را hard-code نمی‌کند و فقط رشد index/table را operationally قابل مشاهده می‌داند | policy قانونی/سازمانی retention و archive؛ در این STEP هیچ purge انجام نشد |
| Report export | Audit API pagination/read دارد اما export اختصاصی موجود نیست و export contract مستقلی تصویب نشده | تعیین فرمت، سقف، masking و permission export؛ تا آن زمان API read/pagination مرجع است |

## GitHub و وضعیت STEP

Application tested/deployed: `127b0f6a322a7cb95b4bc58f9adadcc805f7f906`؛ browser/test source شامل test-only commit `62f171e` و report final commit `6170b2bce2adfe131707e18855fd78374e28a255` است و pair application همان application commit را serve می‌کند. remote branch push شده؛ divergence جاری و refهای نهایی در تحویل Git ثبت شده‌اند. merge فقط در صورت evidence authenticated اعلام خواهد شد؛ 404 بدون authentication proof نیست.

STEP ۱۵ شروع نشده است. با توجه به reconciliation و شواهد رفتاری فوق، STEP ۱۴ برای scope فعلی پذیرفته شد؛ KPIهای Dashboard، retention/archive و export contract تصمیم‌های آینده‌اند و blocker این پذیرش نیستند.
