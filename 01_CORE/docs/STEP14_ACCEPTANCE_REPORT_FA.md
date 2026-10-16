# گزارش پذیرش STEP ۱۴ — Audit trails و operational reports

تاریخ بررسی: ۱۴۰۵/۰۷/۲۵ (۲۰۲۶-۱۰-۱۷)  
شاخه: `codex/step2-inventory-unit-contracts`  
مبنای پذیرفته‌شدهٔ STEP ۱۳: application `58e3538fad517f494c9da5be03144eb288058a55`، report `68f5080e139595dc677317260862982154f6a9d6`.

## نتیجهٔ اجرایی

وضعیت STEP ۱۴: **CONDITIONAL PASS**.

زیرساخت canonical audit، RLS و transaction از قبل وجود داشت و اصلاحات این مرحله فقط روی خواندن امن audit، semantics بازهٔ زمانی و صفحهٔ موجود Audit اعمال شد. هیچ audit history، business record، credential، volume یا master data حذف یا تغییر داده نشد. دو تصمیم گزارش‌دهی کسب‌وکاری در بخش آخر باز هستند؛ این تصمیم‌ها مانع قابلیت audit و گزارش‌های موجود نیستند.

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

## آزمون و شواهد

| بررسی | نتیجه |
|---|---:|
| Audit unit (`AuditFoundationTests`) | **3/3 PASS** |
| Audit Integration روی PostgreSQL ایزوله (`AuditFoundationTests`) | **4/4 PASS**؛ شامل RLS، company isolation، redaction، rollback و inclusive/exclusive boundary |
| Unit کامل current source | **233/233 PASS**؛ TRX: `tests/BARANG.Core.UnitTests/TestResults/step14-unit.trx` |
| Backoffice کامل current source | **184/184 PASS**؛ TRX: `tests/BARANG.Backoffice.Tests/TestResults/step14-backoffice.trx` |
| Integration کامل current source | **135/135 PASS**؛ `BARANG_TEST_DB_CONNECTION` به DB ایزوله `barang_step10_quality_test` روی port 5569؛ TRX: `tests/BARANG.Core.IntegrationTests/TestResults/step14-integration.trx` |
| Release build | PASS، ۰ warning و ۰ error |
| Browser acceptance | پس از deployment current source باید با system Chrome اجرا شود؛ سناریوی اختصاصی Audit صفحه، فیلتر، table/empty state و نبود Blazor error در E2E اضافه شده است |

تست rollback نشان می‌دهد audit موفق داخل transaction شکست‌خورده باقی نمی‌ماند. تست security نیز actor/company/correlation، failed-login redaction، denied access و عدم مشاهدهٔ شرکت دیگر را بررسی می‌کند.

## Migration، backup و deployment

این تغییر migration schema ندارد. تست‌ها با migration chain رسمی روی PostgreSQL ایزوله اجرا شدند؛ volume و business data توسعه دست‌نخورده است. قبل از deployment هیچ reset، delete، deactivation، remap یا replay تاریخی انجام نشد.

پس از سبز شدن browser و build، API/Backoffice فعلی با Compose پروژهٔ `barang-core` از همین source deploy می‌شود و provenance/image digest نهایی در همین گزارش ثبت خواهد شد. deployment فقط local development است؛ production، workflow definition، keys و admin permissions تغییر نمی‌کنند.

## تصمیم‌های باز و محدودیت‌ها

| موضوع | شواهد فعلی | تصمیم موردنیاز |
|---|---|---|
| KPIهای Management Dashboard | صفحه وجود دارد اما metric contract و definition رسمی برای workload، SLA، stock یا finance aggregate مستقل یافت نشد | تعیین metricها، source، timezone، currency/UOM و permission پیش از تکمیل dashboard |
| Audit retention/archive | سند فعلی retention را hard-code نمی‌کند و فقط رشد index/table را operationally قابل مشاهده می‌داند | policy قانونی/سازمانی retention و archive؛ در این STEP هیچ purge انجام نشد |
| Report export | Audit API pagination/read دارد اما export اختصاصی موجود نیست و export contract مستقلی تصویب نشده | تعیین فرمت، سقف، masking و permission export؛ تا آن زمان API read/pagination مرجع است |

## GitHub و وضعیت STEP

پس از commit نهایی، SHA تست و deployment، ref branch، divergence نسبت به `origin/main` و وضعیت PR/merge با refهای Git ثبت می‌شوند. merge فقط در صورت evidence authenticated اعلام خواهد شد؛ 404 بدون authentication proof نیست.

STEP ۱۵ شروع نشده است.
