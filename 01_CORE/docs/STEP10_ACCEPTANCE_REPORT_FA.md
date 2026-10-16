# گزارش پذیرش STEP ۱۰ — Quality inspection، گیت Workflow و reproduction

تاریخ اجرا: ۱۴۰۵/۰۷/۲۴  
شاخه: `codex/step2-inventory-unit-contracts`  
مبنای شروع: `62944e8220563f9ee0cb6b1ddf5fd35359659b65` (گزارش STEP ۹)؛ source نهایی بررسی‌شده: `4c7828cddf9c2894cef8590f66af305786762cdb`

## نتیجهٔ اجرایی

STEP ۱۰ از نظر پیاده‌سازی، پذیرش ایزوله و استقرار توسعهٔ محلی **PASS** است. هیچ production deployment یا mutation کسب‌وکاری روی دادهٔ توسعه انجام نشد؛ فقط migration غیرمخرب پس از backup روی schema توسعه اعمال شد.

## رفتار پیاده‌سازی‌شده

- شروع inspection فقط برای `ProductionJob` تکمیل‌شده با `QuantityCompleted` مثبت، template فعال و job-type منطبق انجام می‌شود.
- هویت inspection شامل CompanyId، OrderId، OrderItemId، ProductionJobId، TemplateId و TemplateVersion است؛ دو OrderItem هم‌SKU مستقل باقی می‌مانند.
- جدول جدید `qc_inspection_dispositions` با unique `(CompanyId, InspectionId)`، UOM canonical، مقدار inspectable، مقادیر accepted/rejected/reproduction-required، reason، actor، زمان و optimistic Version اضافه شد.
- مقادیر disposition متقابلاً مستقل نیستند اما هم‌پوشان اجازه ندارند: همه غیرمنفی، جمع دقیقاً برابر inspectable، و برای هر مقدار غیرصفر reason لازم است. defectهای ثبت‌شده نیز از سقف inspectable عبور نمی‌کنند.
- Approval فقط با disposition کامل، تمام criteria اجباری و بدون rejected/reproduction quantity موفق می‌شود. Rejection برای disposition شامل rejected یا reproduction quantity و reason معتبر است.
- `WorkflowDefinitionRuntime` برای nodeهای کیفیت، تکمیل را به inspection نهایی و disposition معتبر برای jobهای تکمیل‌شدهٔ سفارش گره می‌زند؛ outcome اشتباه، evidence ناقص، generic/legacy/administrative completion را جلو نمی‌برد.
- reproduction به inspection و source Job و root lineage متصل است و از ایجاد دوباره در retry جلوگیری می‌کند. کیفیت به‌تنهایی stock movement، payment، refund یا return ایجاد نمی‌کند؛ job بازتولید باید دوباره گیت‌های Production و warehouse issue را طی کند.
- صفحهٔ `/workspace/quality` اکنون start/load inspection، disposition، criterion result، approve/reject، reproduction و history را از API canonical صدا می‌زند و بعد از reload وضعیت persisted را می‌خواند. برچسب‌های جدید از کلیدهای localization موجود استفاده می‌کنند؛ UI فرم‌های inventory/finance/order تغییر نکرد.

## migration و امنیت

Migration غیرمخرب `20261016113647_AddQualityInspectionDispositions` جدول، FK، index، RLS policy، grant و عدم‌دسترسی delete برای app/test role را اضافه می‌کند. زنجیرهٔ migration روی دیتابیس ایزولهٔ تازه/موجود اعمال شد و تست ساختار QC آن را تأیید کرد. backup پیش از migration توسعه در `D:\00_BARANG\artifacts\barang_core_step10_quality_pre_migration_20261016.dump` حفظ شد و backup قبلی STEP ۹ در `D:\00_BARANG\artifacts\barang_core_step9_delivery_20261016.dump` نیز باقی است؛ migration با `dotnet ef database update` روی schema توسعه اعمال و سپس health/smoke بررسی شد.

## محیط و شواهد پذیرش

محیط: Compose project `barang-step9b-acceptance`، PostgreSQL روی `127.0.0.1:5569`، API روی `5869`، Backoffice روی `5560`، DB `barang_step10_quality_test` و شرکت تست فعلی `fb915a6a-f989-4992-a4eb-6b7b3cf4d8ca`. این volume و identity مستقل از `barang-core` است. graph واقعی `BARANG.ORDER` از fixture sanitized وارد شد، با positionهای company-scoped نگاشت، validate و در isolation publish شد: candidate version `4`، شامل node `InventoryIssue` بین `DECIDE_WAREHOUSE` و `PRINT`.

fixture reproducible: `tests/Acceptance/Step10QualityFixture.ps1`؛ graph setup: `tests/Acceptance/Step4WorkflowSetup.ps1`؛ browser test: `tests/BARANG.Backoffice.E2ETests/Step10QualityAcceptanceE2ETests.cs`. fixture با APIهای پشتیبانی‌شده branch/warehouse/customer/product/variant/tariff/stock/order/دو OrderItem هم‌SKU/reservation/issue/production output/template را ساخت؛ هیچ SQL برای ایجاد دادهٔ کسب‌وکار استفاده نشد.

سناریوی authenticated UI با Chrome سیستم، از صفحهٔ واقعی `/workspace/quality`:

| سناریو | نتیجه | شاهد |
|---|---|---|
| Job تکمیل‌شده و template فعال → Start inspection | PASS | API 201، inspection `f98a7fd7-b743-49c1-b1b0-e76e9b671ca8` |
| ثبت disposition کامل با UOM و reason | PASS | persisted accepted=3، rejected=0، reproduction=0 |
| ثبت criterion اجباری و Approve از UI | PASS | `Step10QualityAcceptanceE2ETests.QualityInspectionIsRecordedAndReloadedThroughWorkspace` |
| reload و Load inspection | PASS | همان inspection ID و JSON persisted در UI |
| screenshot و TRX | PASS | `D:\00_BARANG\.artifacts\step10-quality-browser\step10-quality-browser.png` و `.artifacts/step10-quality-browser/step10-quality-browser.trx` |
| API failure mapping در Quality | FIXED/VERIFIED | خطای قبلی 500 با correlation خالی اصلاح شد؛ mapper اکنون correlation غیرخالی `quality` می‌دهد |
| disposition stale/overlap و isolation | PASS در domain/integration contracts | Unit disposition version/quantity و RLS/migration tests |

یک اجرای قبلی fixture با criterion result، به‌دلیل ارسال هم‌زمان `passFailValue` و `textValue`، 400 دقیق `qc.result_type_mismatch` داد؛ fixture اصلاح شد و اجرای نهایی با shape درست و از مسیر UI موفق شد. این خطای setup/contract به 500 تبدیل نمی‌شود.

## آزمون‌های جاری

- Release build فعلی: موفق، ۰ warning و ۰ error.
- Unit: `230/230` موفق.
- Backoffice: `184/184` موفق.
- Integration فعلی روی PostgreSQL ایزوله، با `BARANG_TEST_DB_CONNECTION` و runner تک‌پردازه، TRX و hang timeout پنج‌دقیقه‌ای: `133/133` موفق در `3m 41s`؛ `.artifacts/step10-integration-final/step10-integration-final.trx`.
- Quality foundation migration/API: `3/3` موفق قبل از full suite.
- Browser feature: `1/1` موفق با Chrome نصب‌شدهٔ سیستم.

پس از full Integration، همان runner password role ایزوله را تغییر داده بود و باعث unhealthy شدن API ایزوله شد؛ این فقط اثر fixture runner بود و به `barang-core` ارتباط نداشت. roleهای همان محیط در پایان برای cleanup/inspection مجدد دستکاری نشدند؛ development containers/volume/credential untouched باقی ماندند.

## محدودیت و تصمیم‌های باقی‌مانده

1. قرارداد mixed outcome مسیر routing خودکار تعریف نمی‌کند؛ پیاده‌سازی فقط evidence و تصمیم صریح Approve/Reject را می‌پذیرد و reproduction باید از مسیر explicit درخواست و generation canonical انجام شود.
2. reinspection برای همان Job در مدل فعلی inspection دوم مستقیم نیست؛ reproduction باید source inspection و source Job جدید بسازد و سپس با گیت‌های قبلی ادامه دهد. این تصمیم باید در قرارداد نهایی کسب‌وکار STEP ۱۶ تثبیت شود.
3. QC receipt hold، اصلاحات مالی و stock disposition خارج از این مرحله‌اند و intentionally پیاده‌سازی نشدند.

## provenance و استقرار توسعه

- source و application deployed: `4c7828cddf9c2894cef8590f66af305786762cdb`
- Compose project: `barang-core`؛ فقط سرویس‌های `api` و `backoffice` بازسازی شدند؛ PostgreSQL/Redis/MinIO و volumeهای موجود حفظ شدند.
- API image: `barang-core-api@sha256:44408429171eb31ca83e06e9620abd4c6a9f577332a70e194aafba2450577ea3`
- Backoffice image: `barang-core-backoffice@sha256:ebe47b7410a8f64a70fad185a3bfaff4b63fc91c1e37782549288e88d98a8ad5`
- `BARANG_BUILD_COMMIT` در هر دو container برابر source بالا ثبت شد.
- `http://127.0.0.1:8080/health` و `http://127.0.0.1:5100/login` هر دو HTTP 200؛ این‌ها smoke/provenance هستند و جای پذیرش feature ایزوله را نمی‌گیرند.
- graph توسعه و instanceهای موجود تغییر نکردند؛ graph candidate ایزوله version `4` است و برای rollout آن به instanceهای جدید، انتشار جداگانه از Designer لازم است. instanceهای جاری به version خودشان bind می‌مانند.

## تکمیل پذیرش mixed outcome و reinspection

سیاست مصوب در این اجرا اعمال و با مسیرهای canonical سرویس/Workflow تکمیل شد: پذیرش کامل فقط با resolve شدن همهٔ obligationهای دقیق OrderItem/Job مجاز است؛ rejected یا reproduction-required unresolved، Quality را ناقص نگه می‌دارد و progression خودکار به shipping ندارد. reproduction به Job جدید با source inspection، source Job و lineage متصل ساخته شد و خروجی جایگزین جداگانه inspect و approve شد؛ فقط obligation متناظر را رفع کرد و inspection/job اصلی immutable ماند. reinspection مستقیم همان Job با خطای `qc.inspection.reinspection_not_allowed` رد شد.

اجرای reproducible API در محیط ایزوله با اسکریپت `tests/Acceptance/Step10QualityPolicyAcceptance.ps1` موفق شد: mixed disposition با accepted=1 و reproduction-required=1، reproduction request، replacement completion/inspection/approval، retry generation با همان `replacementJobId` و رد reinspection مستقیم ثبت شد. این ابزار پس از رفع دو نقص setup (enum عددی برای requestedStage و reload disposition پس از ایجاد inspection) و یک نقص واقعی در Production completion (خواندن quantity از `ReproductionRequest`) به نتیجه رسید. هیچ SQL برای ایجاد دادهٔ کسب‌وکار استفاده نشد.

| سناریوی نهایی | نتیجه | شاهد |
|---|---|---|
| mixed outcome و نگه‌داشتن obligation | PASS | policy runner؛ original status=5، accepted=1، reproduction-required=1 |
| reproduction Job مستقل و lineage | PASS | `reproductionRequestId` و `replacementJobId` در JSON runner |
| replacement output با quantity خودش | PASS | completion موفق پس از اصلاح `ProductionAdministration` |
| retry بدون Job تکراری | PASS | retry همان `replacementJobId` را برگرداند |
| reinspection مستقیم همان Job | PASS (rejected) | `sameJobReinspectionRejected=true` و کد دقیق سرویس |
| browser Quality workspace روی source فعلی | PASS 1/1 | `Step10QualityAcceptanceE2ETests.QualityInspectionIsRecordedAndReloadedThroughWorkspace`؛ `.artifacts/step10-quality-browser-final/step10-quality-browser-final-4.trx` و screenshot همان پوشه |

## گیت نهایی source فعلی

- Release solution build: PASS، ۰ warning و ۰ error.
- Unit: `230/230` PASS؛ `.artifacts/step10-mixed-unit/step10-mixed-unit.trx`.
- Backoffice: `184/184` PASS؛ `.artifacts/step10-mixed-backoffice/step10-mixed-backoffice.trx`.
- Full Integration روی PostgreSQL ایزوله، runner تک‌پردازه و hang timeout پنج‌دقیقه‌ای: `133/133` PASS در `3m 43s`؛ `.artifacts/step10-mixed-policy-final/step10-mixed-policy-final.trx`.
- focused browser runner روی source فعلی پس از بازسازی graph/identity و fixture معتبر: `1/1` PASS؛ failureهای قبلی فقط setup drift ناشی از reset Integration بودند و در run نهایی تکرار نشدند.

اصلاحات source فعلی migration جدید ندارد؛ backup و volumeهای توسعه untouched باقی ماندند. mixed/reproduction policy اکنون در `QUALITY_CONTROL_AND_REPRODUCTION.md` و قرارداد lifecycle ثبت شده است.

## وضعیت GitHub

پس از ثبت این به‌روزرسانی، commit گزارش push می‌شود. merge/PR فقط بر اساس ref یا ابزار احراز هویت‌شده گزارش خواهد شد؛ 404 unauthenticated به‌عنوان نبود repository یا merge تلقی نمی‌شود.
