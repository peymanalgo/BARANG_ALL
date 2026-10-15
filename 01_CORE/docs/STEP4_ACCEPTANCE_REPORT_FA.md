# گزارش پذیرش STEP 4 — issue / release / change / return

## دامنه و provenance

Checkout `step2-clean/01_CORE`، شاخه `codex/step2-inventory-unit-contracts`. commit شروع این نوبت `a0b8041bffe9a018fdd3d0366a178ad61343e394` و commit برنامهٔ گزارش‌شده `1a88ef344fb19dcf2eb83a43353726b8559c4e72` بود. `.keys` محلیِ untracked دست‌نخورده باقی ماند.

## اصلاحات جاری

- endpointهای واقعی chart assignment (`POST/PUT/activation/DELETE /api/v1/organization/chart/assignments`) که UI از آن‌ها استفاده می‌کند اضافه شد و از سرویس canonical سازمان با authorization موجود استفاده می‌کند.
- صفحه `/workspace/notifications` heading قابل دسترس گرفت.
- assertion قدیمی User Access که View/Edit icon مستقل می‌خواست با قرارداد فعلی action menu سه‌نقطه‌ای هم‌راستا شد و بازشدن menu را بررسی می‌کند.

## تست‌های جاری

| تست | نتیجه |
|---|---|
| Release build API | PASS، ۰ warning / ۰ error |
| Release build Backoffice | PASS، ۰ warning / ۰ error |
| Unit | PASS، 222/222 |
| Backoffice | PASS، 184/184 |
| STEP4 issue isolated PostgreSQL | PASS، `WarehouseIssueIsAtomicAndRetrySafeWithoutDuplicatingLedgerEntries`، 1/1؛ دو OrderItem هم‌کلید، issue اتمیک، retry بدون duplicate، changed retry با conflict، cancel قبل از issue بدون افزایش stock |

### full Integration جاری

با PostgreSQL ایزولهٔ `barang_step4_test`، container `barang-step4-acceptance-postgres` و port `55432`، full suite failureهای زیر را نشان داد:

- `PricingE2ETests.AuthenticatedPricingFlowCalculatesFinalizesAndPreservesHistory`: `POST /api/v1/security/bootstrap`، 404.
- `UserAccessManagementTests.ExistingPersonCreatesOneIdentityAccountWithHashedPasswordAndSafeApi`: bootstrap در `BootstrapAndLogin`، 404.
- `UserAccessManagementTests.CompanySelectionRejectsMissingAccessAndInactiveMembershipWithExplicitProblemCodes`: bootstrap، 404.
- `UserAccessManagementTests.CriticalSystemAdministratorAndCrossCompanyTargetsAreProtected`: bootstrap، 404.
- `UserAccessManagementTests.IdentityPasswordResetDisableAndLockoutUseSecureAccountOperations`: bootstrap، 404.
- `UserAccessManagementTests.DirectInheritedAndEffectivePermissionsMatchRuntimeAuthorization`: bootstrap، 404.
- `ArtworkFileE2ETests.CompleteArtworkFileFlowUsesRealMinioAndEnforcesSecurity`: bootstrap، 404.
- `Step4InventoryIssueE2ETests.WarehouseIssueIsAtomicAndRetrySafeWithoutDuplicatingLedgerEntries`: bootstrap، 404 در full suite؛ اجرای filter مستقل PASS شد.
- `SupplierSourcingApiTests.InactiveReferenceRemainsHistoricalButCannotBeNewlySelectedOrPublished`: bootstrap، 404.
- `SupplierSourcingApiTests.RelationshipCrudPersistsExactProductSkuReferencesAndPlanningHints(False)`: bootstrap، 404.
- `SupplierSourcingApiTests.RelationshipCrudPersistsExactProductSkuReferencesAndPlanningHints(True)`: setup، 409.
- `SupplierSourcingApiTests.PostgreSqlEnforcesHistoryOverlapPreferenceReferenceScopeAndRls`: bootstrap، 404.

این failureها با نتیجهٔ تاریخی جایگزین نشده‌اند. اجرای مستقل feature test موفق است؛ علت full-suite هنوز باید از نظر fixture isolation/parallel reset/registration بررسی و اثبات شود.

پس از deploy commit `84437b15151d9545fd0fa5c5adf78db8ee8fc24f`، rerun چهار browser check نتیجهٔ **3/4 PASS** داشت: navigation/heading و Organization/Persons PASS؛ User Access menu تا بازشدن action menu پیش رفت اما assertion قدیمی indicator sort (`svg[data-icon='arrow-up/down']`) با marker فعلی `barang-sort-indicator` failure شد. این آخرین blocker تست مرورگر است.

## وضعیت requirementها

- issue، deduction on-hand، کاهش reservation، idempotent reference و release هنگام cancel در backend موجود و feature-test مستقل است.
- graph منتشرشدهٔ فعلی node واقعی `InventoryIssue` ندارد. Designer/versioning از مسیر draft پشتیبانی می‌شود، اما routing/assignment این stage در policy تأیید نشده است؛ publish حدسی انجام نشد.
- adjustment اتمیک pre-issue شامل variant/warehouse/quantity، pricing revision، stale version و rollback اکنون در endpoint canonical و تست isolated موجود است.
- return API و return-document form واقعی در Backoffice اکنون انتخاب issue/item، disposition صریح، quantity limit و reload persisted را پوشش می‌دهند.

## الحاقیهٔ اجرای فعلی — ۱۴ اکتبر ۲۰۲۶

- adjustment canonical اضافه شد: endpoint `POST /api/v1/inventory/orders/{orderId}/adjustments` با قفل سفارش، قفل deterministic balance/reservation، version، ownership، variant/product، warehouse، UOM، aggregate stock-key، rollback transaction و idempotency key. issue قبلی با `inventory.adjustment.issued_item_immutable` رد می‌شود. برای Submitted order، snapshot finalized قبلی immutable می‌ماند و pricing revision جدید در همان transaction ساخته می‌شود.
- `InventoryReturnSection.razor` به workspace اضافه شد: انتخاب issue، مقدار باقی‌مانده، UOM، disposition صریح، reference، duplicate-submit prevention و reload. فقط `USABLE` به stock برمی‌گردد؛ `DAMAGED/UNKNOWN` خودکار usable نمی‌شوند.
- API و Backoffice Release build: PASS، صفر warning/error. Unit: 222/222. `WarehouseIssueIsAtomicAndRetrySafeWithoutDuplicatingLedgerEntries`: PASS، 1/1 روی `barang-step4-impl-postgres` / `barang_step4_impl_test` / port 55433.
- `PreIssueAdjustmentIsAtomicVersionedAndRollsBackOnShortage`: PASS، 2/2 پس از افزودن pricing revision داخل همان transaction؛ snapshot finalized قبلی immutable باقی می‌ماند، revision current/non-finalized ساخته می‌شود، stale version رد می‌شود و shortage هیچ تغییر reservation/stock باقی نمی‌گذارد.
- Full Integration current-source در این نوبت خروجی نهایی نداد و PASS اعلام نمی‌شود. آخرین browser evidence همچنان ۳/۴ با sort assertion ناسازگار با `barang-sort-indicator` است.
- endpoint `GET /api/v1/inventory/returnable-issues` اکنون issue، OrderId/OrderItemId، issued/returned/remaining و UOM را از ledger canonical برمی‌گرداند و UI return از آن استفاده می‌کند.
- گروه‌های failure تاریخی در اجرای جداگانهٔ current-source روی همان host/role پاس شدند: Pricing 1/1، UserAccess 6/6، Artwork 1/1، SupplierSourcing 8/8. بنابراین 404/409های قبلی به fixture/full-suite execution context مربوط‌اند، نه regression endpoint در اجرای منفرد.
- اجرای full Integration current-source با `--blame-hang-timeout 90s` قبل از خروجی نهایی طولانی/بی‌خروجی ماند و متوقف شد؛ full suite هنوز PASS محسوب نمی‌شود.
- گراف published هنوز `InventoryIssue` و routing/assignment نهایی ندارد؛ publish حدسی انجام نشد. تصمیم لازم: warehouse manager، position مشخص، یا assignment مبتنی بر warehouse.

**STEP 4 فعلی: FAIL / NOT READY**؛ full Integration evidence، browser sort و workflow assignment blocker هستند. adjustment backend اکنون implemented/tested است. STEP 5 آغاز نشده است.

## الحاقیهٔ پذیرش assignment — ۱۵ اکتبر ۲۰۲۶

- رابطهٔ پایدار `warehouse_responsibilities` با کلیدهای `CompanyId + WarehouseId + ApplicationUserId`، unique constraint، active/version و migration غیرمخرب `20261015062226_WarehouseResponsibilities` اضافه شد. endpointهای مدیریت آن زیر مجوز `organization.warehouse.manage` و صفحهٔ مدیریتی `/workspace/inventory/warehouse-responsibility` فراهم است؛ کاربر باید فعال و عضو معتبر همان شرکت و انبار باید فعال و متعلق به همان شرکت باشد.
- `ClaimTaskAsync`، فهرست eligible task و `CompleteTaskAsync` برای nodeهای `InventoryIssue` مسئول فعال همان warehouse رزرو‌شده را server-side بررسی می‌کنند. administrator، مسئول warehouse دیگر، کاربر غیرفعال، عضویت منقضی و cross-company fallback پذیرفته نمی‌شود. خطای نبود پیکربندی `inventory.issue.warehouse_responsible_not_configured` است. رقابت claim با version/row update موجود کنترل می‌شود.
- Designer اکنون فقط برای nodeهای InventoryIssue اجازهٔ position تهی می‌دهد؛ سایر UserTaskها همچنان position اجباری دارند. گراف published توسعه عمداً تغییر نکرده و candidate هنوز باید با graph واقعی موجود در disposable deployment ساخته/اعتبارسنجی و سپس طبق policy منتشر شود؛ instanceهای جاری به version خود bind می‌مانند.
- backup توسعه در `artifacts/barang_core_before_step4_assignment_20261015.dump` گرفته شد؛ archive با `pg_restore --exit-on-error` در database موقت `barang_step4_restore_verify` restore و سپس database موقت حذف شد. volume، records، keyها و credentials توسعه دست‌نخورده ماندند.
- تست migration و feature روی PostgreSQL ایزولهٔ `barang-step4-assignment-pg`، database `barang_assignment_test`، port `55435` انجام شد. `Step4InventoryIssueE2ETests` برابر ۲/۲ PASS بود. full Integration current-source برابر **128/128 PASS** در ۳:۱۸ با roleهای `barang_app`/`barang_test_app` بود؛ توقف قبلی ۹۰ثانیه‌ای hang نبود.
- Unit برابر ۲۲۲/۲۲۲ PASS، Backoffice برابر ۱۸۴/۱۸۴ PASS پس از به‌روزرسانی شمارش مستند route جدید، و هر دو Release build صفر warning/error هستند. Browser E2E علیه image توسعهٔ قدیمی هنوز rerun نشده است؛ sort failure قبلی نتیجهٔ mismatch deployment بوده و باید بعد از deploy coherent جدید دوباره ثبت شود.

**وضعیت فعلی:** هستهٔ assignment server-side و management UI تحویل شده، اما STEP 4 تا ساخت/اعتبارسنجی candidate InventoryIssue واقعی، feature browser acceptance روی deployment جدید، و deploy provenance جدید PASS نهایی نیست. STEP 5 و STEP 4.5 آغاز نشده‌اند.

## نتیجهٔ اجرای نهایی current-source — ۱۵ اکتبر ۲۰۲۶

- full Integration پس از آخرین تغییرات runtime/Designer: **128/128 PASS**، ۳:۱۸، روی PostgreSQL ایزولهٔ `barang-step4-assignment-pg`/`barang_assignment_test`/port `55435`. بنابراین failureهای bootstrap تاریخی جایگزین نتیجهٔ جاری نیستند و در اجرای کامل فعلی مشاهده نشدند.
- Release API و Backoffice: هر دو PASS، ۰ warning و ۰ error؛ Unit **222/222**؛ Backoffice **184/184**؛ feature issue/adjustment **2/2**.
- browser check `SharedSortHeadersUserGridAndActionIconsMatchTheGlobalStandard`: **PASS** با Chrome واقعی علیه `http://localhost:5100` پس از deploy جدید. marker فعال `barang-sort-indicator` و فلش `arrow-up/down` در runtime حاضر بود؛ assertion تضعیف نشد.
- deployment تأییدشده: Compose project `barang-core`، API `barang-core-api:latest` digest `sha256:26672766f24fa1b94c5703e7f12e9f6d5357aa6bf264acf9d40155eee1c87b52`، Backoffice `barang-core-backoffice:latest` digest `sha256:438dbc6bdbfad5dcc1c16b1396d1f48e931c78802f40511dd6b608d1a67ff4e2`، هر دو provenance کامل `7166ac79f690f43c73a1ba65621798518b474d35`. volumeهای business/key و credentials تغییر نکردند؛ migration `20261015062226_WarehouseResponsibilities` روی توسعه با backup/restore verification قبلی اعمال شد.
- browser mutation acceptance کامل issue/adjustment/return روی development انجام نشد تا business data موجود mutate نشود؛ این سناریوها همچنان باید در disposable authenticated deployment اجرا شوند. بنابراین با وجود regression gate سبز، PASS نهایی STEP 4 برای این نوبت صادر نمی‌شود تا candidate graph و feature acceptance ایزولهٔ مذکور ثبت شود.

## نتیجه

**STEP 4: FAIL / NOT READY**

مسدودکننده‌ها: graph/routing InventoryIssue، adjustment اتمیک، return UI و failureهای توضیح‌داده‌نشدهٔ full Integration. STEP 5 آغاز نشده است. داده‌ها، volumeها، credentials و deployment توسعه reset یا mutate نشده‌اند.

## الحاقیهٔ تلاش پذیرش ایزوله — ۱۵ اکتبر ۲۰۲۶

این نوبت با هدف بستن دو مورد باقی‌مانده اجرا شد؛ نتیجهٔ آن PASS نیست.

- checkout/source دقیق: `8c010c4a49ceea6fae452a2d4c9a50db7b96c965`.
- محیط مستقل: Compose project `barang-step4-acceptance`، API `localhost:18080`، Backoffice `localhost:15100`، PostgreSQL `localhost:55436`، Redis `localhost:16379` و MinIO `localhost:19000/19001`. volumeها با پیشوند `barang-step4-acceptance_` ساخته شدند و هیچ volume توسعه یا `barang_core` mount نشد.
- imageهای ساخته‌شده از همین checkout: API digest `sha256:9fcb0a4645d74686c5f47cecf44ed3a89c1b235e1a71975bcf340db3d15e447f` و Backoffice digest `sha256:606838ff4c5fcf44d0222e0b57d4cefc0c2ac7dfba43f9f38719ac8bae2dcf3d`. لاگ API provenance همین SHA را چاپ کرد.
- migration chain روی PostgreSQL ایزوله پس از build صحیح Debug ابزار EF کامل اعمال شد و `20261015062226_WarehouseResponsibilities` نیز اعمال‌شده ثبت شد. در شروع نخست، init script با CRLF اجرا نشد و roleهای `barang_app`/`barang_test_app` ساخته نشدند؛ این failure محیط تست بود، با ساخت roleهای محدود و اجرای دوباره migration برطرف شد. برای Hangfire نیز schema/privilege فقط در محیط ایزوله تکمیل شد.
- bootstrap هویت ایزوله با `step4-admin` موفق شد و API پس از اصلاح privilege ایزوله healthy شد. این هویت/داده در محیط توسعه وجود نداشت.
- گراف واقعی توسعه فقط‌خواندنی از PostgreSQL بررسی شد: `BARANG.ORDER`، نسخهٔ Active شمارهٔ 17، نسخه `ba83e320-e43f-49d9-82d8-7f5c50ca3f9b`، 23 node و 41 transition؛ node `InventoryIssue` در آن وجود ندارد. تلاش API برای دریافت همان گراف از deployment توسعه نیز به‌دلیل `security.company_access.denied` نشست مدیریتی شکست خورد. برای جلوگیری از نقض policy، graph tableها در محیط ایزوله برای ساختن candidate دستکاری نشدند و گراف minimal/test-only نیز ساخته نشد.
- در نتیجه، مسیر supported Designer برای ساخت draft واقعی، publish ایزوله، start instance جدید و مقایسهٔ binding instance قبلی هنوز به‌طور معتبر اجرا نشده است.
- به‌دلیل نبود candidate graph معتبر، browser feature acceptance authenticated برای responsibility/claim، issue/retry/partial issue، cancel، adjustment، return و same-stock-key OrderItems قابل اجرا و قابل نسبت‌دادن نبود. هیچ نتیجهٔ مرورگری به‌عنوان PASS ثبت نمی‌شود؛ smoke/health جایگزین acceptance نشد.
- dedicated concurrency evidence موجود قبلی: issue atomic/retry و adjustment atomic روی PostgreSQL ایزوله PASS است؛ برای این نوبت نتیجهٔ جدیدی برای issue-vs-cancel، adjustment-vs-issue و return-limit در deployment candidate ثبت نشد.
- محیط `barang-step4-acceptance` پس از جمع‌آوری این شواهد حذف شد؛ development Compose، volumeها، records، keys، credentials و admin account دست‌نخورده ماندند.

**نتیجهٔ این نوبت: STEP 4 = FAIL / NOT READY.** تنها blocker باقی‌ماندهٔ قابل اقدام برای دو requirement درخواست‌شده: دسترسی/مسیر معتبر برای import یا ایجاد baseline واقعی `BARANG.ORDER` در محیط ایزوله و سپس ساخت candidate فقط با Designer، پس از آن اجرای authenticated browser feature matrix. STEP 4.5 و STEP 5 آغاز نشده‌اند.

## الحاقیهٔ unblock گراف و اجرای پذیرش — ۱۵ اکتبر ۲۰۲۶

### تشخیص access-context

درخواست read واقعی از deployment توسعه با `http://localhost:8080/api/v1/workflows/definition` و هویت `ADMIN` (UserId: `2226b36f-32bb-405e-aa8a-0be102f9d434`) انجام شد. مسیر عادی `POST /api/v1/auth/me/company` بدون header شرکت اجرا و شرکت صحیح انتخاب شد: `DOCKER_A_1787662611` با CompanyId=`42f0e459-6095-45bb-aa13-f38126d91d9c`. خطای قبلی از ارسال CompanyId اشتباه `42f0e459-6095-45bb-aa13-f38126d91c9c` ناشی بود؛ بنابراین `security.company_access.denied` نشانهٔ نبود عضویت نبود. توکن یا credential در گزارش ثبت نشده است و هیچ membership، داده یا workflow توسعه تغییر نکرد.

گراف صادرشدهٔ معتبر `BARANG.ORDER`، Active version شمارهٔ 17 با Id=`ba83e320-e43f-49d9-82d8-7f5c50ca3f9b`، شامل 23 node و 41 transition بود و `InventoryIssue` نداشت.

### محیط disposable و candidate واقعی

- Compose project: `barang-step4-acceptance2`؛ API=`http://localhost:18081`، Backoffice=`http://localhost:15101`، PostgreSQL=`localhost:55437`، DB اصلی ایزوله=`barang_step4_acceptance2` و DB تست fixture=`barang_step4_acceptance2_test`.
- identity ایزوله: `step4-admin`، UserId=`a40b2469-5339-4a90-8482-87fd82afff40`؛ مسئول انبار فعال برای WarehouseId=`784f60bb-d6cb-44ac-877b-d868d1fb1954`. شناسهٔ Company برابر بودن با توسعه عمدی است، اما DB، volume و identity جدا هستند.
- baseline graph از graph واقعی صادرشده به‌عنوان seed metadata وارد شد؛ candidate و تغییرات آن فقط با Designer API پشتیبانی‌شده انجام شد، نه SQL runtime یا graph-table mutation. هیچ instance runtime توسعه کپی نشد.
- draft candidate شمارهٔ 19، Id=`672381c3-a4c9-4f05-8faa-bd4573efd7bb`، با حفظ graph اصلی و افزودن یک node `InventoryIssue` در handover `DECIDE_WAREHOUSE → InventoryIssue → PRINT` ساخته شد؛ transition موافقت قبلی حفظ شد و `positionId` تهی ماند تا warehouse responsibility مبنای assignment باشد.
- `GET /api/v1/workflows/definition/validate/{candidateId}` با HTTP 200 و body `[]` موفق شد. publish همان candidate در disposable با HTTP 200 موفق شد: Active version 19، 24 node، 42 transition و دقیقاً یک `InventoryIssue`. نسخهٔ 17 به `Superseded` رفت؛ نسخه‌های draft قبلی فقط در disposable باقی ماندند. توسعه publish نشد.
- در مسیر publish یک defect واقعی در `WorkflowDesignerAdministration.PublishAsync` پیدا و اصلاح شد: validation قبلی اتصال مشترک EF را dispose می‌کرد و publish معتبر را با 500 (`ObjectDisposedException`/`Connection already open`) شکست می‌داد. validation اکنون روی همان connection خوانده می‌شود. همچنین binding پارامترهای `ValueTuple` در draft save اصلاح شد و افزودن warehouse responsibility از `ApplicationUser.IsActive` unmapped به `AccountState == Active` تغییر یافت.

### آزمون‌ها و browser evidence

- Designer browser روی deployment ایزوله: `WorkflowDesignerE2ETests.DesignerLoadsValidatesAndKeepsTheActiveVersion` PASS و `DesignerCreatesAndPersistsAVisualTaskConnection` PASS؛ 2/2 با system Chrome. این شواهد save/reload/validate UI را ثابت می‌کند.
- Unit: 222/222 PASS؛ Backoffice: 184/184 PASS؛ Release build API و Backoffice: PASS، صفر warning/error.
- Full Integration current-source با `BARANG_TEST_DB_CONNECTION` روی DB ایزوله اجرا شد، اما PASS نیست. اجرای کامل failureهای واقعی و قابل تکرار داشت: Pricing (`AuthenticatedPricingFlowCalculatesFinalizesAndPreservesHistory`) HTTP 500 از `/api/v1/auth/me/company`؛ UserAccess چند تست HTTP 500 همان endpoint و `54001 stack depth limit exceeded` در `CompanyContextMiddleware`؛ Artwork HTTP 500؛ Supplier/SupplierReference چند تست `54001 stack depth limit exceeded`؛ و STEP4 issue هر دو test هنگام company selection HTTP 500. این اجرای فعلی جایگزین evidence تاریخی 128/128 نیست و assertionها تضعیف نشدند. علت غالب test-host/DB execution context در fixture فعلی است (stack depth در middleware/RLS path و company-selection 500)، اما تا isolation تمیز و علت root-cause اصلاح نشود regression gate سبز نیست.
- STEP4 browser feature matrix کامل برای issue، cancel، adjustment و return هنوز PASS نیست: فقط Designer browser و API publish/warehouse responsibility در این نوبت اجرا شد. هیچ smoke/health یا SQL read-only به‌جای فرم‌ها و actionهای واقعی ثبت نشده است.

### نتیجهٔ پذیرش

تشخیص دسترسی و ساخت candidate واقعی اکنون PASS است؛ candidate در disposable با policy warehouse responsibility منتشر و validate شد. با این حال STEP 4 نهایی **FAIL / NOT READY** باقی می‌ماند، چون full Integration current-source failureهای توضیح‌داده‌نشده دارد و browser feature matrix issue/adjustment/return/cancellation/claim به‌طور کامل اجرا نشده است. STEP 4.5 و STEP 5 آغاز نشده‌اند. محیط ایزولهٔ `barang-step4-acceptance2` پس از جمع‌آوری evidence حذف می‌شود؛ توسعه، volumeها، business records، keys، credentials و workflow منتشرشدهٔ توسعه حفظ می‌شوند.

**provenance تحویل:** tested source مبنا `2892323468d833252980dfe4bff73949af2f583a` با اصلاحات ثبت‌شده در commit نهایی `f352c27b421723eaf76245db39161661bc421260` بود. image API disposable digest=`sha256:d9d1daa6883cefb33ee3a66e380a8a8d11833275a64c97237c915db30ac77459` و image Backoffice digest=`sha256:a00281de55c027432c37d7a9ef2a4d639c49fefc0e64e60b48f55baaf4e31065`. imageها و منابع `barang-step4-acceptance2` پس از جمع‌آوری evidence حذف شدند؛ container/volumeهای `barang-core` توسعه باقی ماندند. commit روی remote branch push شد؛ وضعیت PR/merge از این محیط احراز نشد.

## الحاقیهٔ company-context recursion و اجرای جاری — ۱۵ اکتبر ۲۰۲۶

### بازتولید و علت دقیق

در Compose ایزولهٔ `barang-step4-diag` با PostgreSQL 17.6، database=`barang_step4_diag` و نقش درخواست=`barang_app`، درخواست authenticated به `POST /api/v1/auth/me/company` با کاربر فعال و شرکت انتخاب‌شده بازتولید شد. برای بازتولید شرایطی که در fixture مهاجرت رخ داده بود، مالک تابع `barang.reference_global_admin()` همان نقش اپلیکیشن قرار گرفت؛ پرس‌وجوی `SELECT id,user_id,company_id FROM barang.user_companies` با تنظیمات `barang.user_id` و `barang.company_id` شکست خورد:

`ERROR 54001: stack depth limit exceeded`، `HINT: Increase ... max_stack_depth` و PostgreSQL CONTEXT شامل تکرار متوالی `SQL function "reference_global_admin" during startup` و `SQL function "reference_global_admin" statement 1` بود. نقش، session role و context در همان اتصال به‌ترتیب `barang_app`، `barang_app`، user فعال و company انتخاب‌شده ثبت شد. SQL اجرایی middleware ابتدا `SELECT c.id FROM barang.companies ...` و سپس مسیر `CompanyContextMiddleware → SecurityStore.HasEffectiveCompanyAccessAsync → EfTransactionExecutor.SetSecurityContextAsync` بود.

علت، timeout یا کمبود عضویت نبود: policy `user_companies_system_administrator_management` تابع `reference_global_admin()` را برای `USING/WITH CHECK` صدا می‌زد؛ تابع قدیمی برای تشخیص مدیر، دوباره `barang.user_companies` را می‌خواند؛ جدول `user_companies` نیز `FORCE ROW LEVEL SECURITY` داشت و همان policy را دوباره ارزیابی می‌کرد. زنجیرهٔ دقیق `user_companies policy → reference_global_admin() → user_companies → policy` علت 54001 بود. policyهای `user_companies_authenticated_self_read` و `user_companies_company_isolation`، policyهای `user_roles`، owner function و `rolbypassrls` نیز با SQL read-only ثبت شدند؛ درخواست عادی هرگز با superuser/BYPASSRLS اجرا نشد.

### اصلاح محدود و ایمن

تابع همچنان `SECURITY DEFINER STABLE` با `SET search_path=pg_catalog,barang` است، اما دیگر جدول policy‌دارِ خودش را نمی‌خواند. مدیر فقط وقتی شناخته می‌شود که application user فعال باشد و یک `system.administrator` فعال با بازهٔ معتبر در `user_roles` داشته باشد؛ `user_roles_authenticated_self_read` این fact را برای همان actor قابل مشاهده می‌کند و وابستگی بازگشتی حذف شده است. migration forward-only `20261015110000_FixGlobalAdminRlsRecursion` همین تغییر را روی پایگاه‌های موجود اعمال می‌کند؛ RLS حذف/خاموش نشده و query filter عمومی هم bypass نشده است.

همچنین SaveDraft Designer در مسیر خطای definition/version، reader را پیش از rollback می‌بندد. پیش از اصلاح، محیط final candidate با `NpgsqlOperationInProgressException` و 500 شکست می‌خورد؛ پس از اصلاح همان درخواست معتبر روی definition موجود به 404 دقیق `workflow.definition.not_found` تبدیل شد.

### شواهد تست جاری

- Integration کامل روی PostgreSQL تازه و migration جاری، DB=`barang_step4_full2_test`: **128/128 PASS** در 3:47؛ اجرای قبلی کامل نیز 128/128 در 3:46 بود.
- UserAccess روی DB تازه: **6/6 PASS**؛ Pricing/Artwork/Supplier: **20/20 PASS**؛ آزمون‌های STEP4 و concurrency انتخاب‌شده: **19/19 PASS**.
- Unit: **222/222 PASS**؛ Backoffice: **184/184 PASS**؛ Release solution build: **PASS** با صفر warning/error.
- SQL بازتولید پس از migration: خواندن `user_companies` و `user_roles` با app role بدون stack overflow انجام شد. مسیرهای active/inactive/locked، انتخاب شرکت، cross-company و محدودیت‌های administrator در UserAccess جاری سبز هستند.
- محیط پذیرش جدید `barang-step4-final`: API=`http://localhost:18091`، Backoffice=`http://localhost:15111`، PostgreSQL=`55447`، database=`barang_step4_final`، volumeها project-scoped؛ پس از commit نهایی image API digest=`sha256:41a6c1b53f0d2c9b5f7d97178f02abf06cd77a7e5d16ff5d0c78082870da729f` و Backoffice digest=`sha256:2977a4216f794e470f83fd29d37c6f189803b40e58f69eb5fa895e853f6211bb`، هر دو با `BARANG_BUILD_COMMIT=81559456d14a6e4d8ef99e8346fc2492b54c405f`.

### وضعیت candidate و browser

گراف واقعی توسعه با identity مجاز، company صحیح `42f0e459-6095-45bb-aa13-f38126d91d9c` و API read صادر شد؛ گراف 17 همان شواهد قبلی را دارد. candidate منتشرشدهٔ قبلی در `barang-step4-acceptance2` همچنان تنها candidate واقعی ثبت‌شده است: version 19، 24 node، 42 transition و `DECIDE_WAREHOUSE → InventoryIssue → PRINT`؛ publish توسعه انجام نشد.

در اجرای جدید، محیط مستقل `barang-step4-final` از migration تازه ساخته شد، اما schema فعلی endpoint پشتیبانی‌شدهٔ ایجاد/import definition ندارد: `PUT /api/v1/workflows/definition/draft` فقط definition موجود را ویرایش می‌کند و در محیط خالی پس از اصلاح، عمداً `404 workflow.definition.not_found` می‌دهد. ایجاد definition یا کپی graph با SQL برای دورزدن Designer مجاز نبود؛ بنابراین browser feature matrix واقعی issue/cancel/adjustment/return/claim در این run قابل انتساب نشد. اجرای پروژه Playwright نیز در discovery با `Test process did not return valid JSON` متوقف شد و آن failure به رفتار برنامه نسبت داده نشد. این دو مورد blocker باقی‌مانده‌اند، نه PASS پنهان‌شده.

**نتیجهٔ جاری:** recursion fix و regression gate **PASS**؛ STEP 4 کلی **FAIL / NOT READY** تا یک مسیر پشتیبانی‌شده برای import/create گراف واقعی در disposable و سپس browser acceptance کامل issue، cancellation، adjustment، return، retry و warehouse assignment اجرا شود. development database، published definition، credentials و business data تغییر نکردند. STEP 4.5 و STEP 5 آغاز نشده‌اند.
