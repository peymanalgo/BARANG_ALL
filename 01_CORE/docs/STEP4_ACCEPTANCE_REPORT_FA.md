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
- adjustment اتمیک pre-issue شامل variant/warehouse/quantity و pricing revision هنوز end-to-end کامل و پذیرفته نشده است.
- return API وجود دارد، اما return-document form واقعی با permission، انتخاب issue/item، disposition صریح و reload persisted در Backoffice کامل نیست.

## نتیجه

**STEP 4: FAIL / NOT READY**

مسدودکننده‌ها: graph/routing InventoryIssue، adjustment اتمیک، return UI و failureهای توضیح‌داده‌نشدهٔ full Integration. STEP 5 آغاز نشده است. داده‌ها، volumeها، credentials و deployment توسعه reset یا mutate نشده‌اند.
