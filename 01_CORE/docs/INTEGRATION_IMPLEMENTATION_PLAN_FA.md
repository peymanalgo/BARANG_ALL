# برنامه پیاده‌سازی یکپارچگی BARANG — مراحل ۲ تا ۱۵

این برنامه بر اساس `docs/ORDER_LIFECYCLE_INTEGRATION_CONTRACT_FA.md` و شواهد source/runtime فعلی نوشته شده است. STEP 1 فقط قرارداد و validation است؛ هیچ‌یک از مراحل زیر در این commit پیاده‌سازی نمی‌شوند.

## اصول مشترک

- business rule در Core و owner module باقی می‌ماند؛ UI فقط command/feedback است.
- company context، PostgreSQL RLS، same-company FK، optimistic concurrency و audit موجود حفظ می‌شود.
- هیچ Draft به‌صورت ضمنی stock را کم نمی‌کند.
- هر mutation باید `Result/Error`، actor، UTC time، correlation، idempotency و audit داشته باشد.
- outbox فقط در همان transaction business event نوشته می‌شود؛ dispatcher/consumer retry و dead-letter دارد.
- تست‌های DB فقط با fixture/database ایزوله اجرا می‌شوند؛ توسعه/production database reset یا mutate نمی‌شود.

## نقشه مراحل

| مرحله | موضوع و وابستگی | تغییرات مورد انتظار Frontend | تغییرات مورد انتظار Backend/Domain | DB/Events | تصمیم کسب‌وکاری لازم | تست معنادار | استقرار و پذیرش |
|---:|---|---|---|---|---|---|---|
| ۲ | هویت inventory و واحدها؛ وابسته به OrderItem/ProductVariant/Warehouse | نمایش warehouse، variant، UOM و مقدار reservation در Order/Cartable/Inventory | قرارداد canonical برای Company+Order+OrderItem+Variant+Warehouse+Qty+UOM؛ جلوگیری از mismatch | snapshot/UOM در reservation یا allocation؛ same-company FK و index | آیا UOM در reservation immutable snapshot شود؟ **پیشنهاد: بله** | unit برای identity و conversion؛ isolated schema/FK/RLS | فقط پس از migration test از صفر و API contract pass؛ acceptance: هیچ reservation cross-company/variant نشود |
| ۳ | availability و reservation؛ وابسته به مرحله ۲ | نمایش availability، shortage و وضعیت reservation؛ Draft بدون deduction | `CheckAndReserveAsync` در owner Inventory؛ قفل balance، full reserve، structured shortage، idempotency | unique `(Company, IdempotencyKey)`؛ eventهای reserved/shortage | reserve در submit یا stage؟ **پیشنهاد: InventoryReservation task** | race دو reserve، insufficient, retry same key، warehouse inactive | deploy API/Backoffice هم‌زمان؛ acceptance: no oversell/no negative available |
| ۴ | consume/release/change؛ وابسته به reservation | disable ordinary item edit بعد از reservation؛ نمایش release/consume/reason | commandهای release delta، consume remaining، cancel compensation، quantity change policy | immutable issue ledger؛ reference/idempotency؛ audit | after-consume cancel چه کند؟ **return process جدا** | partial release, consume > remaining, expiry, cancel before/after consume | migration اگر lifecycle fields لازم شد؛ acceptance: balance/reservation/ledger consistent |
| ۵ | inventory workflow gates؛ وابسته به ۳ و ۴ و انتخاب workflow runtime | Cartable task نتیجه owner operation را نشان دهد؛ failure/shortage actionable باشد | `WorkflowBusinessCompletionAdapter` واقعی به definition runtime متصل شود؛ از direct DB `AnyAsync` به owner service منتقل شود | task completion + business event در transaction یا orchestration idempotent | task InventoryReservation فقط reserve کند یا purchasing branch بسازد؟ | task claim/version/outcome، owner failure prevents advance، retry | definition version compatibility؛ acceptance: task فقط با evidence معتبر advance شود |
| ۶ | pricing/customer master؛ وابسته به CustomerGroup/TaxStatus/Product | Customer group و pricing trace در review/Order 360؛ نمایش rule/component ID | final calculator به resolverهای canonical Discount/Tax وصل شود؛ customer group از Customer خوانده شود | price component snapshot شامل rule IDs؛ audit applied rule | precedence group vs customer/source؛ **پیشنهاد: documented specificity** | group discount، tax status، product/SKU precedence، finalized immutability | Backoffice/API deploy؛ acceptance: final total و components قابل بازسازی باشند |
| ۷ | Finance؛ وابسته به finalized price و workflow | task Finance از decision/payment/invoice state واقعی بخواند؛ actor مجاز صریحاً Issue Invoice را اجرا کند | completion به `IFinanceAdministration` واقعی؛ عدم auto-approve و عدم auto-issue؛ invoice فقط approved + finalized snapshot | financial history/invoice immutable؛ events finance.approved/invoice.issued | **مصوب:** approval به‌تنهایی invoice نمی‌سازد؛ صدور فقط با action صریح actor مجاز؛ invoice ≠ payment | reject/suspend reason، stale version، invoice retry، currency mismatch | API + workflow deploy؛ acceptance: task advance بدون Finance state و invoice بدون Issue Invoice ممنوع |
| ۸ | shortage/purchasing؛ وابسته به مرحله ۳ | لینک shortage به PR و نمایش requested/available؛ Add PR مستقل؛ approval/rejection و Issue/Send action صریح | shortage-to-PR command/link؛ approval canonical برای PR/PO با approver واجد شرایط همان شرکت؛ PO فقط پس از approval قابل ارسال؛ auto-PO ممنوع | `purchase_request` linkage، revision/version، history/audit، permission seed، no mutation of shortage evidence | **مصوب:** PR با purchasing officer، PO با purchasing manager؛ creator self-approve ممنوع؛ threshold ندارد؛ نبود approver خطای پیکربندی | shortage creates one PR link; idempotent conversion; submit/approve/reject/resubmit; stale version; explicit PO send; receipt/recheck | migrations on fresh/restored DB; acceptance: approval evidence exact revision، no generic bypass، receipt later rechecks demand |
| ۹ | production/equipment؛ وابسته به workflow gate و artwork | task/Order 360 job status، equipment assignment و retry پیام | definition runtime باید `ProductionAdministration` را call کند؛ generation idempotent؛ job evidence | job/order/item/print FKs، history، production events | generate at ProductionPlanning یا after Finance? source says stage; **پیشنهاد: stage completion** | one job per source operation، equipment assignment/concurrency، failure blocks workflow | deploy Core/Backoffice; acceptance: production jobs link exact item/op |
| ۱۰ | quality؛ وابسته به production و template/defect | inspection/defect/reproduction UI و task context | workflow completion به Quality owner service/approved inspection متصل شود؛ rejection/reproduction path | QC history immutable، reproduction linkage/events | QC receipt hold فعلاً deferred در سند؛ تصمیم جدا لازم | approve/reject reason، reproduction idempotency، no advance without approved inspection | acceptance with isolated order/job/inspection; no live DB mutation |
| ۱۱ | logistics/returns؛ وابسته به QC/package/inventory/finance policy | package allocations، handoff، shipment tracking/status/return reason | workflow gate به Logistics service؛ package complete after QC؛ return separate inventory/finance contracts | package/shipment history immutable؛ return warehouse FK؛ events | return refund/stock/accounting scope؛ **پیشنهاد: separate commands** | invalid transitions, duplicate delivery/return, version conflict | deploy Logistics/API; acceptance: package→shipment→delivery and explicit return |
| ۱۲ | notifications؛ وابسته به event/outbox schema | status/toast/inbox read-only؛ no business mutation from notification | producers for order/workflow/shortage/finance/QC/logistics; consumer dedupe/retry | outbox event payload with company/order/correlation; dead-letter | recipients/template/channel; provider external remains optional | event once, retry, dead-letter, notification read/mark-read | worker/background rollout; acceptance: business commit succeeds even if sender retry fails |
| ۱۳ | master-data cleanup؛ وابسته به ۶ و ۸ و ۱۲ | hide selector-only fields only after consumer inventory; preserve edit values | register TypeCode consumers; remove hard-coded currency/labels; no orphan catalog | no destructive delete; deactivation and migration audit | customer group/price group/category/tag ownership | traceability test each catalog to field/API/rule | docs + seed migration only after approval; acceptance: no translated caption persisted |
| ۱۴ | audit/reporting؛ وابسته به همه eventها | Order 360/Management/Audit show reservation, approval, production, QC, logistics correlation | standardized event names/change sets; read models query owner tables | indexes/read projections, retention, RLS; no credentials in payload | retention/report dimensions | audit completeness, correlation, tenant isolation, history immutability | migrate only with isolated upgrade test; acceptance: one order reconstructable end-to-end |
| ۱۵ | end-to-end acceptance؛ وابسته به ۲–۱۴ | browser journey all wizard steps, Cartable, Order 360, fa/en/ar | no new business code beyond prior accepted steps; verify permissions/retries | isolated PostgreSQL from zero + deployed API/Backoffice evidence | approve final lifecycle and exception matrix | Draft→Completed, shortage, cancel before/after consume, reject/reproduction, return, retry | release build/tests, deploy affected services, live browser and DB evidence; no claim from HTTP 200 only |

## الحاقیه عملیاتی فعلی STEP 2 — Customer و Pricing

این الحاقیه نتیجه‌ی اجرای واقعی روی branch `codex/step2-inventory-unit-contracts` است؛ گزارش‌های قبلیِ همین سند به‌عنوان شاهد کافی تلقی نشده‌اند.

### علت‌ها و اصلاحات تأییدشده

- خطای HTTP 500 در `POST /api/v1/pricing/tariffs` از `PricingAdministration` یا lookup چاپ نبود. جدول `barang.tariffs` به‌وسیله‌ی `20260919102002_SyncPhase3Model` ستون‌های سازگاری `logical_tariff_id`، `name` و `version_number` را `NOT NULL` داشت، ولی aggregate فعلی `Tariff` آن‌ها را map نمی‌کرد. درج خام با PostgreSQL خطای `null value in column "logical_tariff_id" of relation "tariffs" violates not-null constraint` می‌داد.
- `TariffConfiguration` اکنون این ستون‌های compatibility را به‌صورت shadow property نگاشت می‌کند و migration `20261012115004_AlignTariffCompatibilityColumns` با backfill authoritative از `id/code/1`، defaultهای `gen_random_uuid()`, `''`, `1` و بدون حذف داده، fresh و drifted schema را پوشش می‌دهد. whitelist ثابت برای `print.method` اضافه نشده است؛ اعتبارسنجی همچنان lookup فعال و company-scoped را در `OrderAdministration.ValidPrintMethod` مصرف می‌کند.
- خطای دوم در `OrderAdministration.ValidHeader` بود: query از `Customer.IsActive` استفاده می‌کرد، در حالی که در مدل نرمال‌شده این property computed و در EF عمداً unmapped است. query به `Customer.Status == CustomerStatus.Active` تغییر کرد؛ هیچ ستون `customers.first_name` یا dummy column اضافه نشد.
- برای حفظ رفتار قراردادی قبلی، اعتبارسنجی آدرس همچنان city بدون province را می‌پذیرد؛ country دوحرفی، نوع آدرس، خط آدرس و codeهای نامعتبر همچنان validate می‌شوند.

### شواهد و تست‌های جاری

- fresh isolated PostgreSQL (`barang_pricing_test`) با full migration chain ساخته شد و تست‌های `CustomerNormalizedApiTests`, `BusinessProfileApiTests`, `PricingE2ETests`, `PricingFoundationTests`, `InventoryFoundationTests`, `CustomerFoundationTests`, `OrderE2ETests` و `OrderFoundationTests` در اجرای منتخب نهایی سبز شدند: ۱۲ تست integration؛ تست واحد Release نیز ۱۸۹ از ۱۸۹ موفق است.
- `CustomerNormalizedApiTests` اکنون create/read/update فرد و شرکت، profile persistence، آدرس و re-load، ارتباط هر دو نوع مشتری با order، جداسازی شرکت و رد کردن `sourceCustomerAddressId` متعلق به مشتری دیگر را در سطح API/persistence بررسی می‌کند.
- `PricingE2ETests` create/list/update tariff، محاسبه و finalize قیمت، print method فعال از lookup، تاریخچه immutable و company isolation را بررسی می‌کند. `BusinessProfileApiTests` نیز مسیر order/print واقعی را پوشش می‌دهد.
- قبل از migration توسعه backup سفارشی گرفته شد: `D:\00_BARANG\barang_core_pricing_fix_predeploy.backup`؛ `pg_restore --list` دارای ۱۷۲۵ TOC entry است. migration با EF روی `barang_core` اعمال شد؛ history شامل `20261012115004_AlignTariffCompatibilityColumns` است، سه ستون موجود `NOT NULL` و دارای default صحیح هستند و رکوردهای business حذف نشدند.

### وضعیت deployment و محدودیت

API و Backoffice از همین checkout با Release Docker build و deploy محلی شدند؛ readiness API `200 Healthy` و Backoffice health `healthy` است. image digestها: API `sha256:7c3874122039272a729e3dc6a0a35ddee28215e236585266c13ac1d628cfbcbc` و Backoffice `sha256:639c046096edfc82e761d17eb151a326bf8050b8673864f4e4ec22d130d2b788`.

این شواهد Pricing و Customer→Order را operationally تأیید می‌کند، اما STEP 2 در سطح برنامه هنوز کامل اعلام نمی‌شود: runtime کامل `WorkflowDefinitionRuntime`/Cartable و integrationهای workflow مالک‌محور همچنان blocker قبلی هستند و reservation/consumption عمداً خارج از این کارند. browser verification در این اجرا در دسترس نبود؛ API/persistence و health تأیید شده‌اند، نه رفتار بصری browser.

## ترتیب اجرایی و gates

### Gate A — قرارداد و مالکیت

قبل از مرحله ۲، owner واقعی workflow انتخاب شود. source فعلی دو مسیر دارد: `WorkflowAdministration`/`human_tasks` و `WorkflowDefinitionRuntime`/`workflow_runtime_tasks`. Order submit به دومی متصل است. تا این مسیر تثبیت نشود، اتصال Inventory/Finance/Production به adapter اشتباه ممکن است.

### Gate B — موجودی و pricing

مراحل ۲ تا ۶ باید قبل از gateهای عملیاتی ۷ تا ۱۱ انجام شوند. reservation باید exact OrderItem و UOM داشته باشد و final pricing باید customer group/tax resolver را مصرف کند؛ هیچ مرحله‌ای نباید از مقدار UI یا caption ترجمه‌شده به‌عنوان حقیقت استفاده کند.

### Gate C — workflow owner completion

در مرحله ۵، ۷، ۹، ۱۰ و ۱۱، graph transition تنها پس از موفقیت owner service مجاز است. direct table `AnyAsync` در orchestration حذف یا فقط به read-model validation محدود شود. هر owner command باید idempotent و version-aware باشد.

### Gate D — isolated acceptance

از `tests/BARANG.Core.IntegrationTests/Database/PostgreSqlDatabaseFixture.cs` و database تست اختصاصی استفاده شود. fixture فعلی مجوزها و delete/update restrictions را جدا تنظیم می‌کند؛ database توسعه `barang_core` نباید target تست migration/reset باشد.

## وابستگی‌های فنی مهم

- Reservation موجود: `InventoryAdministration.ReserveAsync`, `ReleaseAsync`, `IssueReservedAsync`, `ExpireAsync`.
- Lock/concurrency موجود: `LockBalance` با `FOR UPDATE`، `LockReservation`، optimistic `Version` و unique idempotency key.
- Price موجود: `PricingAdministration.CalculateInsideTransactionAsync`, `ResolveDiscountsAsync`, `ResolveTaxAsync`; دو مسیر باید یک contract واحد شوند.
- Workflow واقعی Order: `OrderAdministration` → `IWorkflowDefinitionRuntime`; `WorkflowDefinitionRuntime.CompleteTaskAsync` → `AdvanceAfterTask`/`Traverse`.
- Workflow adapter فعلی اما خارج از مسیر واقعی: `WorkflowBusinessCompletionAdapter`؛ باید یا به definition runtime متصل یا deprecate شود، نه اینکه دو orchestration موازی باقی بماند.
- Background: `BackgroundSchedulers` برای outbox/SLA/file؛ باید notification business producers به outbox اضافه شوند.

## مواردی که نباید در مراحل بعد انجام شوند

- کسر stock در Draft یا صرفاً هنگام باز شدن wizard.
- auto-PO یا auto-purchase بدون policy و approval مصوب.
- auto-approve کردن Finance برای عبور workflow.
- صدور invoice به‌عنوان معادل payment.
- direct update/delete روی ledger، receipt، price snapshot، QC history، package/shipment history یا workflow history.
- اتصال provider خارجی در Domain/Application یا افزودن microservice برای رفع شکاف‌های فعلی.
- اجرای migration/reset/test علیه `barang_core` توسعه یا هر محیط production.

## وضعیت برنامه

این برنامه برای مراحل ۲ تا ۱۵ آماده استفاده است، اما تصمیم‌های رزرو، consume، Finance approval، shortage-to-purchasing، QC receipt hold و return accounting باید قبل از implementation تأیید شوند. هیچ‌کدام از integrationهای فعلی در این STEP اصلاح نشده‌اند.

## اجرای STEP 3 — reservation از مسیر واقعی workflow

در این checkout policy رزرو اتمیک مصوب شد و پیاده‌سازی canonical آن در `InventoryAdministration` انجام شد. endpoint گروهی سفارش و action واقعی `WorkflowDefinitionRuntime.CompleteTaskAsync` از همان سرویس استفاده می‌کنند؛ task فقط پس از persistence موفق complete می‌شود. migration جدید لازم نشد، چون schema STEP 2 شامل `uom_code` و unique idempotency موجود بود. تست اتمیک باید روی PostgreSQL ایزوله، با دو order رقابتی، shortage چندقلمی، retry و عدم تغییر physical stock اجرا و در report ثبت شود.

## وضعیت اجرای STEP 2 — هویت و قرارداد واحد موجودی

STEP 2 از نظر تغییرات source، مدل و قرارداد API، migration قابل‌ردیابی و تست واحد در این شاخه پیاده‌سازی شده است. واحد مرجع `Product.UnitOfMeasureCode` است و تبدیل ضمنی یا generic conversion ایجاد نشده است. رزرو، Purchase Request و Purchase Order اکنون واحد را نگه می‌دارند و ورودی واحد ناسازگار را رد می‌کنند.

در snapshot پیش از repair، migration روی دیتابیس توسعه به‌علت drift قبلی در `LateWorkflowEvidenceSchema` اجرا نمی‌شد (`column ... claimed_on_behalf_of_user_id already exists`). migration عمداً bypass یا mark-as-applied نشد؛ repair نسخه‌بندی‌شده، آزمون fresh/restored و اجرای پشتیبانی‌شده روی development در الحاقیه عملیاتی انتهای این سند ثبت شده است. با وجود آن، STEP 2 هنوز به‌علت blockerهای compilation/runtime آزمون integration، **تکمیل عملیاتی کامل محسوب نمی‌شود**.

تست واحد موفق است؛ migrationهای جاافتاده‌ی بین شاخه و DB به‌صورت version-controlled به زنجیره برگشتند؛ `LateWorkflowEvidenceSchema` به no-op سازگار تبدیل شد چون ستون/جدول‌های آن را migrationهای مالک قبلی ساخته‌اند، و خطای overload در `AddColumn` نیز اصلاح شد. زنجیره روی DB تازه و کپی restore‌شده از backup موفق شد و روی `barang_core` نیز migration پایانی و STEP 2 با حفظ شمارش رکوردها اعمال شد. با این حال integration test فعلی پیش از print endpoint با ناسازگاری قدیمی fixture/model مشتری متوقف می‌شود (`customers.first_name` پس از migration جدید وجود ندارد)؛ بنابراین آزمون کامل API چاپ هنوز سبز نیست.

### وضعیت عملیاتی بازبینی‌شده STEP 2

### ممیزی baseline پس از commit 943a489

سه منبع با هم مقایسه شدند: `main` در `e26bf87`، branch فعلی در `943a489` و working tree canonical در `D:\00_BARANG\01_CORE`. نتیجه نشان داد checkout تمیز implementationهای جدید customer را ندارد و مدل قدیمی آن هنوز `customers.first_name` را map می‌کند، درحالی‌که migration رسمی `20260908122400_CompleteCustomerManagement` این داده را به profile نرمال‌شده منتقل و از جدول `customers` حذف می‌کند. working tree اصلی حاوی implementation نرمال‌شده‌ی customer است، اما مستقل از آن، هر دو منبع فاقد فایل/کلاس canonical `WorkflowDefinitionRuntime` و entityهای لازم runtime هستند؛ فقط endpointها و registrationهای آن وجود دارد. بنابراین contractهای صرفاً compile-time جایگزین runtime محسوب نمی‌شوند و baseline کامل هنوز قابل اعلام نیست.

Probe build روی snapshot working tree اصلی پس از قراردادن contractهای موجود، به خطاهای واقعی `IWorkflowDefinitionRuntime`، `WorkflowDecisionAttachment`، `WorkflowDefinition*`، `WorkflowRuntimeTask` و `CartableSavedFilter` رسید. این موارد از منبع authoritative موجود قابل restore نیستند؛ handler خالی یا type placeholder اضافه نشد.

اجرای integration نیز پیش از مسیر print با SQL مدل قدیمی به `customers.first_name` می‌رسد. اصلاح درست، انتقال کامل و هماهنگ مدل/customer store/application/API به schema profile است، نه اضافه‌کردن ستون dummy. تا زمان انتقال این implementation و بازیابی runtime واقعی Cartable، STEP 2 عملیاتی کامل نیست.

- backup توسعه: `artifacts/barang_core_before_step2_repair.dump`، فرمت custom، قابل فهرست‌برداری با `pg_restore --list`.
- fresh database و restored backup هر دو تا `20261012100553_Step2InventoryUnitContracts` migrate شدند؛ restored database ابتدا migrationهای pending یعنی `20261007170000` و STEP 2 را گرفت.
- توسعه قبل از migration دارای ۵۶ محصول، ۵۷ variant، ۴۹ قلم سفارش و صفر reservation/purchase item بود؛ پس از migration همین مقادیر حفظ شد و هر سه جدول واحدِ جدید صفر ردیف unresolved داشتند.
- قراردادهای compile برای Cartable در `CartableWorkbenchContracts.cs` بازگردانده شد و API/Backoffice Release build سبز است؛ این کار compilation را رفع می‌کند اما runtime implementationهای Cartable که در شاخه وجود ندارند، در STEP 2 پیاده‌سازی نشده‌اند.
- تست unit: ۱۸۹ موفق. تست integration منتخب فعلاً به علت ناسازگاری مدل/fixture مشتری قبل از print شکست می‌خورد؛ نباید آن را موفق تلقی کرد.
- API و Backoffice به‌صورت محلی build/deploy شدند و readiness هر دو HTTP 200 است؛ imageهای جدید با digestهای API `sha256:b97f29302a61f887818edcce2fa36239f6eb93198be62fcc21bd853fb1699500` و Backoffice `sha256:89f964723b7f6b0ad7a10d29e91c667cc3944532e626df31ba8c68ed41ccd01f` در حال اجرا هستند. با این حال به‌علت شکست integration پیش از print و نبود runtime کامل Cartable در این شاخه، STEP 2 هنوز **عملیاتی کامل نیست**.

### دروازهٔ اجباری جلوگیری از regression برای گام‌های ۲ تا ۱۵

### الحاقیه پذیرش STEP 4 جاری

endpointهای assignment مورد استفادهٔ Organization Chart تکمیل و صفحهٔ Notifications و assertion منوی User Access اصلاح شدند. با این حال graph منتشرشده هنوز مرحلهٔ واقعی `InventoryIssue` و policy routing آن را ندارد؛ adjustment اتمیک پیش از issue و return-document UI نیز هنوز end-to-end پذیرفته نشده‌اند. full Integration جاری نیز failureهای ثبت‌شده در `docs/STEP4_ACCEPTANCE_REPORT_FA.md` دارد، بنابراین STEP 4 بسته و STEP 5 آغاز نمی‌شود.

هر گام باید قبل و بعد از تغییر، checkout/commit، context کامپوز، imageهای API و Backoffice، تاریخچهٔ migration و backup قابل‌بازگردانی را ثبت کند. قراردادهای frontend/API، DTO و serializer، مدل EF و schema باید از یک baseline سازگار ساخته شوند. تست‌های ماژول تغییرکرده، تست‌های contract بین API و Backoffice و smokeهای cross-module باید اجرا شوند؛ هر تغییر schema باید روی PostgreSQL تازه و یک نسخهٔ restore‌شدهٔ نماینده اعتبارسنجی شود. فقط یک جفت Release سازگار deploy می‌شود و پس از آن acceptance احراز هویت‌شدهٔ واقعی اجرا می‌گردد؛ health check یا HTTP 200 به‌تنهایی کافی نیست. حساب مدیر توسعه، دادهٔ موجود و تغییرات نامرتبط حفظ می‌شوند و تا وقتی regression تأییدشده وجود دارد، گام بسته نمی‌شود.

### الحاقیه STEP 4 — issue، release و return

سیاست مصوب STEP 4 در source به همان `InventoryAdministration` و ledger موجود متصل شد: issue واقعی از انبار با `WAREHOUSE_ISSUE`، release رزرو outstanding هنگام Cancel، و return قابل‌ردیابی با `WAREHOUSE_RETURN`. مسیر issue چندقلمی تراکنش واحد دارد و workflow runtime پیش از complete کردن task آن را اجرا می‌کند؛ اگر issue کامل نباشد task تکمیل نمی‌شود. هیچ migration جدیدی لازم نشد، چون قراردادهای UOM، reservation و append-only ledger موجود کافی بود. تغییر مستقیم item پس از submit همچنان رد می‌شود؛ مسیر تغییر اتمیک قیمت/variant نیازمند تصمیم و integration بعدی pricing/change-request است و در STEP 4 به‌صورت silent پیاده‌سازی نشده است.
### الحاقیه STEP 4 — assignment مبتنی بر warehouse

مسئول InventoryIssue از رابطهٔ صریح `warehouse_responsibilities` در همان شرکت resolve می‌شود؛ مجوز inventory، عضویت شرکت و active بودن حساب مستقل و الزامی هستند. نبود مسئول پیکربندی‌شده خطای `inventory.issue.warehouse_responsible_not_configured` می‌دهد و administrator یا warehouse دیگر جایگزین نمی‌شود. این رابطه migration و UI مدیریتی دارد و candidate graph با node `InventoryIssue` position تهی را فقط برای resolve پویا بر اساس warehouse رزرو‌شده می‌پذیرد. انتشار candidate و feature browser acceptance باید پیش از PASS نهایی STEP 4 تکمیل شود.

### الحاقیه اجرای STEP ۶ — canonical pricing

resolver canonical سفارش اکنون CustomerGroupId، PriceGroupId و TaxStatusCode را از دادهٔ company-scoped سفارش/مشتری می‌خواند. precedence تخفیف خودکار `customer > customer-group > source/partner > general` است و فقط یک rule اعمال می‌شود؛ tax-status منطبق بر `DEFAULT` مقدم است؛ تعرفهٔ price-group منطبق بر تعرفهٔ بدون گروه مقدم است. snapshot component برای ruleهای discount/tax شناسه و version را نگه می‌دارد و snapshotهای finalized تغییرناپذیر باقی می‌مانند.

stacking تخفیف، معافیت خارج از TaxStatusCode، تبدیل ارز و Finance/Invoice در این مرحله تعریف یا پیاده‌سازی نشده‌اند.
# الحاقیهٔ اجرای STEP 5

مسیر اجرای Workflow/Cartable باید به `InventoryAdministration` canonical متکی باشد: عملیات و transition در یک transaction، با idempotency و lockهای موجود. قبل از transition، gate authoritative پوشش کامل itemها و انطباق CompanyId/WarehouseId/ProductVariantId/OrderItemId/UOM/quantity را بررسی می‌کند. مسیرهای legacy و generic نیز حق completion بدون evidence را ندارند. تست ایزولهٔ گام پنجم graph واقعی reservation→issue، rollback issue ناقص، ledger/on-hand/reservation invariants و retry را پوشش می‌دهد. return usable فقط compensating movement است و damaged/unknown usable نمی‌شود؛ consumption/cancellation policyهای گام‌های قبل تغییر نمی‌کنند.

# الحاقیهٔ اجرای STEP ۸

shortage proposal باید بیرون از rollback رزرو durable باشد و با `SourceShortageId`، exact `OrderItemId`، warehouse، variant، UOM و required/available/missing ثبت شود. تبدیل proposal به PR صریح، مجاز، transaction-safe و idempotent است؛ PO خودکار ممنوع است. receipt جزئی فقط quantity واقعی را به ledger/balance وارد می‌کند و recheck را بر اساس موجودی فعلی منتشر می‌کند، بدون auto-reserve یا task completion. قبل از نهایی‌کردن این مرحله، actor/threshold/routing approval PR/PO و browser matrix باید مستند و verify شوند.

# الحاقیه STEP ۹ — production و equipment

تولید از `ProductionAdministration` canonical استفاده می‌کند. برای هر `OrderItem` job مستقل و برای هر `OrderPrintOperation` job چاپ جدا با `PrintOperationId` تولید می‌شود؛ `GenerationKey` و قفل ردیف سفارش، retry و درخواست هم‌زمان را idempotent می‌کند. job چاپ همان `ArtworkVersionId` و `FileVersionId` عملیات سفارش را frozen نگه می‌دارد.

شروع job فقط با operator فعال همان شرکت و مجوز `production.execute`، تجهیز فعال در وضعیت `AVAILABLE` یا `READY` و capability صریح `EquipmentTypeCode` منطبق با operation، و evidence معتبر issue انبار برای همان order/item مجاز است. وضعیت تجهیز در زمان start دوباره بررسی می‌شود. تکمیل فقط مقدار persisted حداقل برابر مقدار OrderItem را می‌پذیرد. adapter قدیمی و `WorkflowDefinitionRuntime` نیز برای DesignPreparation، Printing و HeatPress تمام jobهای لازم، هویت item/operation و artwork/file version را کنترل می‌کنند؛ completion عمومی نمی‌تواند outcome ساختگی بسازد.

در این مرحله business field جدیدی لازم نبود؛ migration snapshot-alignment بدون عملیات schema برای رفع اختلاف model/snapshot ثبت شد و migration قبلی `AlignReceiptModel` مالک تغییر receipt باقی ماند. published definitionها و instanceهای جاری مهاجرت داده نشدند؛ graph candidate و routing/assignment باید در acceptance ایزولهٔ workflow بررسی شوند.
