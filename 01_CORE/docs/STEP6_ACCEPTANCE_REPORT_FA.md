# گزارش پذیرش STEP ۶ — Pricing canonical

تاریخ بررسی: ۲۰۲۶-۱۰-۱۶  
شاخه: `codex/step2-inventory-unit-contracts`

## نتیجه

وضعیت: **PASS برای دامنهٔ STEP ۶**. محاسبهٔ canonical، انتخاب گروه مشتری/قیمت، tax status، snapshot نهایی و Order Wizard در محیط ایزولهٔ احراز‌شده آزمون شد. سیاست‌های خارج از این STEP همچنان تصمیم‌گیری‌نشده یا خارج از دامنه هستند و مجوز شروع STEP ۷ محسوب نمی‌شوند.

## منشأ سیاست و حدود approval

منشأهای repository:

- `docs/PRICING.md`، بخش‌های Discount precedence، Tax resolution و Price group: ترتیب تخفیف دقیقاً **Customer > CustomerGroup > Source/Partner > General** است؛ فقط یک rule با بالاترین specificity انتخاب می‌شود و stacking مجاز نیست.
- `docs/INTEGRATION_IMPLEMENTATION_PLAN_FA.md`، بخش Pricing policy، و `docs/ORDER_LIFECYCLE_INTEGRATION_CONTRACT_FA.md`، الحاقیهٔ STEP ۶، همین ترتیب و این قاعده را به‌عنوان قرارداد فعلی تکرار می‌کنند.
- همان اسناد تصریح می‌کنند که `CustomerFinancialProfile.TaxStatusCode` بر rule با همان status منطبق می‌شود و فقط در نبود آن `DEFAULT` fallback است.
- همان اسناد `Customer.PriceGroupId` را از `CustomerGroupId` مستقل می‌دانند: tariff گروه قیمت منطبق بر tariff بدون گروه مقدم است؛ در نبود tariff منطبق، fallback عمومی استفاده می‌شود.
- `tests/BARANG.Core.UnitTests/PricingFoundationTests.cs` scope گروه مشتری، price group و tax status را بررسی می‌کند؛ `tests/BARANG.Core.IntegrationTests/Database/PricingE2ETests.cs` مسیر محاسبه، finalization، snapshot تغییرناپذیر و cross-company را آزمون می‌کند.

این‌ها approval موجود repository هستند، نه سیاست جدید تولیدشده در این اجرا. هنوز برای stacking چند تخفیف، exemption مستقل از TaxStatusCode و تبدیل ارز تصمیم approval جداگانه وجود ندارد؛ این رفتارها اختراع نشده‌اند.

## رفتار و اصلاحات تأییدشده

- Customer و CustomerGroup به resolver canonical می‌رسند؛ `CustomerGroupId` با customer دیگر اشتباه نمی‌شود.
- `PriceGroupId` tariff گروه قیمت را انتخاب می‌کند و از customer group جداست.
- مقایسهٔ `TaxStatusCode` به‌صورت case-insensitive canonical انجام می‌شود؛ تفاوت casing بین profile و rule باعث افتادن ناخواسته به DEFAULT نمی‌شود.
- estimate و finalization از resolver مشترک استفاده می‌کنند؛ total ارسالی client مرجع نیست و محاسبهٔ مبلغ با decimal و rounding دامنه انجام می‌شود.
- snapshot شامل currency، component breakdown، rule id/version، discount، tax و total است و با تغییر master data تغییر نمی‌کند.
- `OrderStep7Panel` از endpoint canonical `/submit` استفاده می‌کند؛ endpoint جعلی `/finalize` حذف شد.
- هیچ migration جدیدی برای این اصلاحات لازم نبود؛ migration کامل در دیتابیس ایزوله اعمال شد.

## محیط ایزوله و provenance

- Compose project: `barang-step6`
- PostgreSQL: `127.0.0.1:5546`, database `barang_step6_test`
- API: `http://localhost:8086`
- Backoffice: `http://localhost:5106`
- volumes جدا از توسعه برای PostgreSQL/Redis/MinIO و key storage؛ `barang_core` و داده‌های محیط توسعه استفاده نشدند.
- `BARANG_BUILD_COMMIT` ایزوله: `working-tree-step6-tax-fix-v2`؛ imageهای اجراشده:
  - API: `sha256:d87409b13585f647d8de368d901536652e1fd0d9a0d22d246d55a6fced033d8b`
  - Backoffice: `sha256:15f56cba46b229e4aaa33591e13676a285869900ec5fca1e84690cef42b55624`
- fixture فقط با company/customer/group/price-group/tax/tariff/orderهای ایزوله و identityهای تست ساخته شد؛ secrets در گزارش نیامده‌اند.
- backup توسعهٔ ثبت‌شده: `D:\00_BARANG\barang_core_step6_predeploy.backup`؛ فهرست restore با `pg_restore --list` بررسی شده است.

## پذیرش واقعی Order Wizard

تست `tests/BARANG.Backoffice.E2ETests/Step6PricingAcceptanceE2ETests.cs`، متد `IsolatedOrderWizardPersistsCustomerGroupBreakdownAndFinalSnapshot`، با System Chrome، fixture ایزوله و API/Backoffice همین source اجرا شد: **1/1 PASS**.

نتیجهٔ persisted پس از submit و reload:

- order: `2360d500-dba0-40d9-a331-d5dc41a22c7c`
- snapshot: `02bc424f-fd48-4bbe-8d01-69a72c75be54`
- currency: `IRR`
- subtotal: `53.00`
- discount: `5.30` از `STEP6-GROUP-DISCOUNT`، rule version 1
- taxable: `47.70`
- tax: `2.39` با rate 5% از `STEP6-TAX-RULE`، rule version 1
- final: `50.09`
- tariff raw گروه قیمت: `STEP6-GROUP-RAW` و id آن در component snapshot ثبت شد.

محاسبهٔ مستقل از resolver: `40 + 6 + 1 + 2 + 4 = 53`; ده درصد discount برابر `5.30`; پایهٔ tax برابر `47.70`; پنج درصد برابر `2.385` و با rounding دامنه `2.39`; total برابر `50.09`.

پذیرش شامل انتخاب و ماندگاری customer/group، breakdown قابل مشاهده، finalize با `/submit`، reload Order 360 و rule id/version است. قانون نامنطبق/منقضی، cross-company، تغییر tariff پس از finalize و حفظ snapshot نیز در `PricingE2ETests.AuthenticatedPricingFlowCalculatesFinalizesAndPreservesHistory` آزمون شد.

## آزمون‌ها و failureهای واقعی

| بررسی | نتیجه |
|---|---:|
| Unit current source | **224/224 PASS** |
| Backoffice current source | **184/184 PASS** |
| Integration کامل روی PostgreSQL ایزوله فعلی | **130/130 PASS**، 0 fail، 0 skip، 3m28s |
| Pricing integration منتخب (`FullyQualifiedName~Pricing`) | **4/4 PASS** |
| Order Wizard browser ایزوله و احراز‌شده | **1/1 PASS** |
| Release build API | PASS |
| Release build Backoffice | PASS |
| migration chain تازه/ایزوله | PASS |

Failureهای بررسی‌شده:

1. اجرای نخست Order Wizard به `POST /api/v1/orders/{id}/finalize` با 404 می‌رسید؛ API قرارداد `/submit` دارد. فقط `OrderStep7Panel` به endpoint موجود اصلاح شد.
2. tax profile با casing متفاوت از rule به DEFAULT 9% می‌افتاد؛ resolver و overlap validation به مقایسهٔ case-insensitive اصلاح شدند. پس از اصلاح، snapshot مقدار مورد انتظار 5% و `50.09` را ثبت کرد.
3. اجرای کامل Integration ابتدا در `UserAccessManagementTests.MigrationAddsDirectGrantsGranularCatalogRlsAndUniqueness` شکست خورد، چون fixture بعد از migration دسترسی UPDATE نقش `barang_app` را نرمال نمی‌کرد. این test-host defect با `REVOKE UPDATE ON barang.user_permissions FROM barang_app` در fixture اصلاح شد؛ assertion، authorization و migration تغییر نکردند. سپس همان تست **1/1 PASS** و کل suite **130/130 PASS** شد.
4. یک testhost قدیمی در اجرای موازی فایل خروجی را قفل کرده بود؛ فقط پردازش‌های همان test runner شناسایی و خاتمه داده شدند و اجرای bounded بعدی کامل شد. این failure application نبود.

## محدودیت‌ها و تصمیم‌های لازم

- discount stacking چند rule، exemptionهای tax مستقل از `TaxStatusCode` و currency conversion policy مصوب این STEP نیستند و نباید قبل از approval به سیستم اضافه شوند.
- Finance/invoice و STEP ۷ خارج از scope است.
- اجرای browserهای STEP ۳ تا STEP ۵ در این تغییر backend لازم نبود؛ گیت‌های current Unit/Backoffice/Integration سبز هستند و شواهد پذیرفته‌شدهٔ قبلی حفظ شده‌اند.

## deployment و GitHub

محیط ایزوله با imageهای بالا سالم بود. deployment محیط توسعه فقط پس از commit نهایی و در صورت نیاز باید با همان pair coherent و با حفظ volume، کلید، credential و داده انجام شود؛ در این گزارش image توسعهٔ جدید ادعا نمی‌شود.

SHA نهایی تست‌شده: `1edb804ae826e69e9ecfca81c016a054329f63f0`؛ این commit شامل اصلاحات API/UI، test-host و تست browser ایزوله است. پیش از آن، مبنای application `937454328c52e7f570eab190a03d0a181ad2fb41` بود. وضعیت push/PR/merge باید از Git ثبت شود؛ merge از 404 unauthenticated نتیجه‌گیری نمی‌شود.
