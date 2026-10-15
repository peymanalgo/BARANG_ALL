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
