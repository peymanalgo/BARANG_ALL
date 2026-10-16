# گزارش پذیرش STEP ۹ — Production jobs و Equipment

## دامنه و baseline

این گزارش برای checkout `step2-clean/01_CORE` و branch `codex/step2-inventory-unit-contracts` تهیه شد. baseline پذیرفته‌شدهٔ STEP ۸: application `09f5a656dc7fb25a95929ae8d2fe3a1564989e99` و report `c29755c8e2ce5e0e7cabdbe2da411bc13d632487` است. source tested شامل تغییرات delivery commit `71b61c0` (`71b61c0`؛ full SHA در Git ثبت شده) است. تغییرات این گام فقط مسیر canonical Production، gateهای workflow و نمایش workspace تولید را درگیر می‌کند؛ STEP ۱۰ Quality/QC پیاده‌سازی نشده است.

## پیاده‌سازی

- generation از `ProductionAdministration` استفاده می‌کند و برای هر OrderItem job مستقل DesignPreparation/HeatPress و برای هر PrintOperation job مستقل Print می‌سازد. `GenerationKey` شامل شناسهٔ دقیق OrderItem و PrintOperation است؛ قفل row سفارش در generation، retry و رقابت هم‌زمان را سریال می‌کند.
- artwork/file version چاپ از همان `OrderPrintOperation` frozen می‌شود. runtime و adapter legacy پیش از عبور از DesignPreparation، Printing و HeatPress همهٔ jobهای لازم، OrderItem/PrintOperation و نسخه‌های artwork/file و quantity کافی را بررسی می‌کنند.
- `Start` در سرویس canonical operator فعال و دارای `production.execute`، شرکت، تجهیز فعال و وضعیت `AVAILABLE/READY`، capability صریح نوع تجهیز و issue انبار برای همان OrderItem را دوباره بررسی می‌کند. `Resume` همین کنترل‌ها را تکرار می‌کند. `Complete` مقدار persisted حداقل برابر مقدار OrderItem را الزام می‌کند.
- runtime واقعی `WorkflowDefinitionRuntime` در ProductionPlanning generation اتمیک/قابل تکرار را اجرا می‌کند و completion عمومی نمی‌تواند نتیجهٔ ساختگی تولید کند. taskهای جاری و published definitionها خودکار migrate نشده‌اند.
- UI workspace تولید شناسهٔ OrderItem، Operation، Artwork/File version، quantity، بازهٔ برنامه‌ریزی و تاریخچه را نمایش می‌دهد و quantity/بازهٔ معتبر را به API می‌فرستد.

## پایگاه داده و migration

business field جدید برای Production لازم نبود. اختلاف model/snapshot موجود در زنجیرهٔ migration با migration `20261016085258_AddStep9ProductionWorkflowGates` به‌صورت snapshot-alignment بدون عملیات schema ثبت شد؛ migration قبلی `20261016080912_AlignReceiptModel` همچنان مالک تغییر `receipt_number` است. زنجیرهٔ migration در DB ایزولهٔ تازه تا آخرین migration اجرا شد. هیچ migration روی `barang_core` توسعه یا دادهٔ عملیاتی اعمال نشد و backup توسعه در این اجرا ایجاد/بازنویسی نشد چون schema business جدیدی برای این گام لازم نبود.

## آزمون‌های اجراشده

| آزمون | نتیجه |
|---|---|
| API Release build از source فعلی | PASS |
| Backoffice Release build از source فعلی | PASS |
| Unit | 229/229 PASS |
| Backoffice tests | 184/184 PASS |
| `ProductionFoundationTests` در PostgreSQL ایزولهٔ `barang_step9b_test` روی port 5545 | 2/2 PASS |
| `Step5WorkflowInventoryGateTests` در PostgreSQL ایزوله | 1/1 PASS |
| `Step4InventoryIssueE2ETests` + `Step8PurchasingApprovalE2ETests` در PostgreSQL ایزوله | 3/3 PASS |

یک full Integration run با credential اشتباه ابتدا 113 failure bootstrap داد (`28P01`)، که علت آن password اشتباه برای کانتینر ایزوله بود. پس از اصلاح credential، DB قدیمی به‌دلیل migration history متفاوت با `42701` شکست خورد؛ DB تازه ساخته شد و migration chain تا مرحلهٔ تست گذشت. اجرای full suite روی DB تازه پس از bootstrap در runner بیش از ۹۰ ثانیه بدون query فعال و با connectionهای idle متوقف ماند؛ process و `pg_stat_activity/pg_locks` بررسی شد و runner متوقف شد. بنابراین full Integration جاری **کامل و سبز اثبات نشده است** و این گزارش آن را جایگزین 133/133 تاریخی نمی‌کند.

## پذیرش feature و browser

تست browser احراز هویت‌شدهٔ Production برای generation، assignment، issue prerequisite، start/complete، artwork/file binding و retry در این اجرا هنوز کامل اجرا نشده است. UI build شده، اما feature browser acceptance و workflow graph واقعی با ProductionPlanning/Design/Print/HeatPress باید در disposable deployment با fixture تولیدی تکمیل شود. تعریف policy assignment بر اساس capability صریح موجود است؛ هیچ whitelist بر مبنای نام تجهیز اضافه نشده است.

## وضعیت

به‌دلیل نبود full Integration سبز جاری و نبود browser feature acceptance کامل، STEP ۹ در این commit **CONDITIONAL / NOT ACCEPTED** است، نه PASS. blockerهای دقیق:

1. full Integration current-source باید با runner/fixture ایزولهٔ پایدار و summary کامل 133 تست اجرا شود.
2. browser acceptance واقعی باید برای jobهای جداگانهٔ same-SKU، artwork/file version، equipment rejection/disabled-after-assignment، warehouse issue gate، start/complete persistence و generic completion bypass اجرا و ثبت شود.
3. اگر graph published فعلی nodeهای ProductionPlanning/DesignPreparation/Printing/HeatPress را ندارد، candidate version باید با Designer/versioning در isolation ساخته و قبل از هر rollout بررسی شود؛ running instances نباید migrate شوند.

این مرحله هیچ رفتار STEP ۱۰، QC policy، procurement یا production deployment واقعی را تغییر نمی‌دهد.
