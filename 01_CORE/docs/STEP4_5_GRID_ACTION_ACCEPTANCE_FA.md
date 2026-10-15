# گزارش پذیرش STEP 4.5 — استانداردسازی منوی عملیات Grid

## وضعیت جاری

- منبع بررسی‌شده: commit جاری checkout روی branch `codex/step2-inventory-unit-contracts`؛ SHA دقیق در انتهای این گزارش ثبت می‌شود.
- Release build برای solution و Backoffice/API موفق شد؛ صفر warning و error.
- محیط توسعه، volumeها، کلیدها، داده‌ها و حساب مدیر تغییر نکردند. فایل محلی `.keys/` untracked است و commit نمی‌شود.
- برای تست PostgreSQL از container جداگانه `barang-step45-postgres`، port `5545`، database `barang_step45_test` و volume `barang_step45_pgdata` استفاده شد؛ database توسعه `barang_core` و volumeهای Compose توسعه target نبودند.

## قرارداد مشترک

row actionهای مهاجرت‌یافته از `BarangActionMenu` استفاده می‌کنند: trigger عمودی سه‌نقطه، popup پورت‌شده با برخوردسنجی viewport، outside-click، Escape و بازگردانی focus، single-open، RTL/LTR و keyboard navigation. actionهای popup تمام‌عرض و دارای icon/text محلی هستند. Add/Create/New مستقل و دارای plus باقی می‌ماند.

## inventory source

در source فعلی ۵۰ فایل/خانواده با markerهای table/data-grid شناسایی شد. خانواده‌های اصلی audited عبارت‌اند از:

Base Data و lookupها؛ organization/warehouses/persons/equipment؛ User/RBAC؛ Supplier و child grids؛ Customer و child grids؛ Design Bank؛ Orders و Order Wizard؛ Cartable؛ Pricing؛ Inventory، reservations، adjustments، purchasing، receipts و transfers؛ Finance، Quality، Logistics، Notifications و گزارش‌ها؛ و gridهای nested داخل modal/tab/wizard.

برای هر خانواده، source file و route از Razor inventory ثبت شد؛ row actionهای tabular موجود در ۲۸ فایل به menu مشترک منتقل شدند یا از قبل از آن استفاده می‌کردند. nested PurchaseRequest/PurchaseOrder/InventoryPurchasing و nested Equipment هم اکنون `BarangActionMenu` دارند. OrganizationalChartTree و DesignCategoryTree نیز برای operationهای node از menu واحد استفاده می‌کنند؛ expander/selection کنترل مستقل باقی مانده است. gridهای read-only و nodeهای بدون مجوز menu خالی ندارند. Addهای مشاهده‌شده در toolbar/header به‌صورت standalone باقی ماندند.

## کنترل‌های خارج از پوشش row-grid

- modal close، password toggle، chip removal، toolbar و workflow-canvas کنترل row action نیستند.
- expanderهای tree و دکمه‌های فرم/toolbar/chip عمداً row action نیستند و migrate نشده‌اند. DesignCategoryTree عملیات را از RenderTreeBuilder با `BarangActionMenu` می‌سازد تا مالکیت DOM/expand حفظ شود.
- هیچ `class="icon-button"` یا `chart-action-menu` خام در سه nested purchasing هدف باقی نماند؛ `EquipmentManager` نیز برای child grids به menu مشترک منتقل شد.
- runner مرورگر اکنون در محیط ایزوله company مشخص‌شده با `BARANG_E2E_COMPANY_ID` را انتخاب می‌کند؛ اجرای publish نیز با content root خود artifact انجام می‌شود تا static-web-assets قدیمی از checkout سرو نشود.

## تست‌های جاری

| check | result |
|---|---|
| Backoffice Release build | PASS — 0 warning/error |
| API Release build | PASS — 0 warning/error |
| git diff --check | PASS |
| Unit current source | PASS — 222/222 |
| Backoffice current source | PASS — 184/184 |
| Integration current source | PASS — 128/128 on isolated PostgreSQL; fixture provisions migration roles before migration and enables the direct test roles with the documented test passwords |
| STEP 3/4 baseline | preserved as historical accepted evidence; not substituted for current STEP 4.5 browser acceptance |
| E2E discovery | PASS after explicit `IsTestProject`/private runner configuration; prior invalid-JSON was traced to the module initializer guard |
| Browser: shared sort/action standard | PASS — current published source, isolated API `5511` / Backoffice `5512`; `SharedSortHeadersUserGridAndActionIconsMatchTheGlobalStandard` passed after the scoped sort-header reset |
| Browser: exposed navigation smoke | PASS — `EveryExposedNavigationPageRendersTheSharedShellWithoutRuntimeFailure` passed on the same isolated pair; no `#blazor-error-ui` or `.blazor-error-boundary` observed |
| Browser setup/provenance | PASS — published outputs were served from their own content roots; isolated PostgreSQL `barang_step45_test` on `5545`, company `42f0e459-6095-45bb-aa13-f38126d91c9c`, system Chrome |
| Browser: readiness evidence | PASS — `ReadinessBrowserEvidenceE2ETests`: 4/4 (fa/ar/en shell + navigation), current isolated pair; screenshots/traces در `.artifacts/step45-evidence/` تولید شد |
| Browser: Base/System representative | PASS — shared sort/action test و exposed-navigation test؛ menu trigger، popup و shell بدون Blazor error |
| Browser: Inventory family | PASS — `InventoryBalanceGridLocalizesAndSwitchesDirection`; read-only grid بدون menu خالی و RTL/LTR صحیح |
| Browser: Orders family | FAIL (fixture) — `OrdersWorkspaceIsCanonicalAndExposesOperationalActions`; با انتخاب شرکت ایزوله به Orders رسید، اما fixture ایزوله order ندارد و assertion `Order 360` روی ردیف نخست قابل اجرا نیست؛ application regression اثبات نشد |
| Browser: Design Bank family | NOT COMPLETED — اجرای قبلی به company selector ثابت وابسته بود؛ helper انتخاب company بر اساس `BARANG_E2E_COMPANY_ID` اضافه شد و تست کامل پس از rebuild این ماتریس هنوز باید اجرا شود |
| Browser: Pricing family | NOT COMPLETED — همان fixture/setup selector ثابت در اجرای گروهی؛ اجرای مستقل پس از اصلاح setup هنوز نتیجه نهایی ندارد |
| Browser: nested purchasing | NOT COMPLETED — PurchaseRequest/PurchaseOrder fixtureهای business-specific به داده‌های development وابسته‌اند و در این run mutation آن‌ها اجرا نشد |
| Browser: STEP 3 reservation | FAIL (fixture) — صفحه task به‌جای `#reservation-warehouse` پیام «دریافت کارتابل انجام نشد» برای task شناسه ثابت داد؛ داده/runtime task معتبر در ایزوله وجود نداشت |
| Browser: STEP 4 return | FAIL (fixture) — فرم authenticated هیچ issue قابل برگشتی expose نکرد؛ «No returnable issue was exposed by the authenticated form» |
| Browser mutation matrix | NOT READY — شواهد فعلی برای PASS کامل کافی نیست؛ تست‌های شکست‌خورده setup/fixture هستند اما هنوز acceptance feature را ثابت نمی‌کنند |

## نتیجه

**STEP 4.5 = FAIL / NOT ACCEPTED.** The two previously reproduced browser defects are fixed and pass on a current-source isolated deployment. Current Release, Unit `222/222`, Backoffice `184/184`, and isolated Integration `128/128` pass. Source coverage audit reports ۵۰ family/file entries and no remaining raw row-action marker in the audited source; however, the required authenticated per-family mutation matrix and STEP 3/4 browser regressions are not green because the isolated fixture lacks orders, a valid reservation task, and a returnable issue. Therefore no deployment was made to development port `5100`; it still serves the old UI assets. STEP 5 شروع نشده است.

## شواهد اجرای فعلی و استقرار

- tested source: commit جاری پس از اعمال فقط اصلاح setup تست browser؛ `git status`/SHA در تحویل نهایی ثبت می‌شود.
- isolated API/Backoffice: `127.0.0.1:5511` / `127.0.0.1:5512`، PostgreSQL `barang-step45-postgres:5545`، DB `barang_step45_test`، شرکت `STEP45`، Chrome سیستم. volume و database توسعه استفاده نشد.
- screenshot/trace: `.artifacts/step45-evidence/culture-fa.png/.trace.zip`, `culture-ar`, `culture-en`, `shell`.
- deployment comparison: پورت `5100` هنوز assetهای قدیمی (`barang-design-system.3jyrdzij42.css` و `BARANG.Backoffice.rj0owjlmhu.styles.css`) را سرو می‌کند؛ محیط ایزوله assetهای current publish (`barang-design-system.b9shrc4ic5.css` و `BARANG.Backoffice.r4weykjsws.styles.css`) را سرو می‌کند. بنابراین پورت ۵۱۰۰ عمداً deploy نشد.
- remaining exact blocker: provision کردن fixtureهای واقعیِ ایزوله برای order/task/issue و اجرای دوبارهٔ Design Bank، Pricing، nested purchasing و STEP 3/4 feature browser tests؛ این blocker تست/دادهٔ ایزوله است، نه مجوز تغییر دادهٔ توسعه.

## checklist تفصیلی source/grid

| # | source file | row-action status | Add/status |
|---:|---|---|---|
| 1 | src/BARANG.Backoffice/Components/BarangDataGrid.razor | read-only/generic; no menu | verified in Razor source inventory |
| 2 | src/BARANG.Backoffice/Components/BarangJsonGrid.razor | read-only/generic; no menu | verified in Razor source inventory |
| 3 | src/BARANG.Backoffice/Components/CustomerChildWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 4 | src/BARANG.Backoffice/Components/GoodsReceiptSection.razor | migrated/shared | verified in Razor source inventory |
| 5 | src/BARANG.Backoffice/Components/InventorySection.razor | migrated/shared | verified in Razor source inventory |
| 6 | src/BARANG.Backoffice/Components/JsonDataView.razor | read-only/generic; no menu | verified in Razor source inventory |
| 7 | src/BARANG.Backoffice/Components/OrderPrintStep.razor | migrated/shared | verified in Razor source inventory |
| 8 | src/BARANG.Backoffice/Components/OrderStep4Panel.razor | migrated/shared | verified in Razor source inventory |
| 9 | src/BARANG.Backoffice/Components/OrderStep6Panel.razor | migrated/shared | verified in Razor source inventory |
| 10 | src/BARANG.Backoffice/Components/OrderStep7Panel.razor | migrated/shared | verified in Razor source inventory |
| 11 | src/BARANG.Backoffice/Components/Pages/AdminEntityManager.razor | migrated/shared | verified in Razor source inventory |
| 12 | src/BARANG.Backoffice/Components/Pages/Cartable.razor | migrated/shared | verified in Razor source inventory |
| 13 | src/BARANG.Backoffice/Components/Pages/CustomerManager.razor | migrated/shared | verified in Razor source inventory |
| 14 | src/BARANG.Backoffice/Components/Pages/DesignBank.razor | migrated/shared | verified in Razor source inventory |
| 15 | src/BARANG.Backoffice/Components/Pages/DiscountRulesPanel.razor | migrated/shared | verified in Razor source inventory |
| 16 | src/BARANG.Backoffice/Components/Pages/DomainReferenceWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 17 | src/BARANG.Backoffice/Components/Pages/EntityManager.razor | migrated/shared | verified in Razor source inventory |
| 18 | src/BARANG.Backoffice/Components/Pages/EquipmentManager.razor | migrated/shared | verified in Razor source inventory |
| 19 | src/BARANG.Backoffice/Components/Pages/FinanceWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 20 | src/BARANG.Backoffice/Components/Pages/InventoryPurchasingWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 21 | src/BARANG.Backoffice/Components/Pages/LogisticsWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 22 | src/BARANG.Backoffice/Components/Pages/LookupManager.razor | migrated/shared | verified in Razor source inventory |
| 23 | src/BARANG.Backoffice/Components/Pages/MasterEntityManager.razor | migrated/shared | verified in Razor source inventory |
| 24 | src/BARANG.Backoffice/Components/Pages/ModuleWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 25 | src/BARANG.Backoffice/Components/Pages/OrderDetails.razor | migrated/shared | verified in Razor source inventory |
| 26 | src/BARANG.Backoffice/Components/Pages/OrderWizard.razor | migrated/shared | verified in Razor source inventory |
| 27 | src/BARANG.Backoffice/Components/Pages/OrderWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 28 | src/BARANG.Backoffice/Components/Pages/Persons.razor | migrated/shared | verified in Razor source inventory |
| 29 | src/BARANG.Backoffice/Components/Pages/PricingWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 30 | src/BARANG.Backoffice/Components/Pages/ProductionWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 31 | src/BARANG.Backoffice/Components/Pages/ProductManager.razor | migrated/shared | verified in Razor source inventory |
| 32 | src/BARANG.Backoffice/Components/Pages/QualityCatalogs.razor | migrated/shared | verified in Razor source inventory |
| 33 | src/BARANG.Backoffice/Components/Pages/QualityWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 34 | src/BARANG.Backoffice/Components/Pages/ShortageList.razor | migrated/shared | verified in Razor source inventory |
| 35 | src/BARANG.Backoffice/Components/Pages/SupplierManager.razor | migrated/shared | verified in Razor source inventory |
| 36 | src/BARANG.Backoffice/Components/Pages/SupplierProductLifecycle.razor | migrated/shared | verified in Razor source inventory |
| 37 | src/BARANG.Backoffice/Components/Pages/TaxRulesPanel.razor | migrated/shared | verified in Razor source inventory |
| 38 | src/BARANG.Backoffice/Components/Pages/UserAccessManagement.razor | migrated/shared | verified in Razor source inventory |
| 39 | src/BARANG.Backoffice/Components/PurchaseHistorySection.razor | migrated/shared | verified in Razor source inventory |
| 40 | src/BARANG.Backoffice/Components/PurchaseOrderSection.razor | migrated/shared | verified in Razor source inventory |
| 41 | src/BARANG.Backoffice/Components/PurchaseRequestSection.razor | migrated/shared | verified in Razor source inventory |
| 42 | src/BARANG.Backoffice/Components/ReferenceValueGrid.razor | migrated/shared | verified in Razor source inventory |
| 43 | src/BARANG.Backoffice/Components/ReservationsShortagesSection.razor | migrated/shared | verified in Razor source inventory |
| 44 | src/BARANG.Backoffice/Components/StockAdjustmentSection.razor | migrated/shared | verified in Razor source inventory |
| 45 | src/BARANG.Backoffice/Components/SupplierChildWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 46 | src/BARANG.Backoffice/Components/SupplierLifecycleWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 47 | src/BARANG.Backoffice/Components/SupplierSourcingWorkspace.razor | migrated/shared | verified in Razor source inventory |
| 48 | src/BARANG.Backoffice/Components/WarehouseResponsibilitySection.razor | migrated/shared | verified in Razor source inventory |
| 49 | src/BARANG.Backoffice/Components/WarehouseSection.razor | migrated/shared | verified in Razor source inventory |
| 50 | src/BARANG.Backoffice/Components/WarehouseTransferSection.razor | migrated/shared | verified in Razor source inventory |

## الحاقیه اجرای جاری در 2026-10-15

این الحاقیه نتایج همین checkout را ثبت می‌کند و شواهد تاریخی جدول بالا را جایگزین نمی‌کند.

### provenance و محیط ایزوله

- source checkout: `82205c17f6ee7426daa2b50e1b68099070a0bd47` در شروع اجرای جاری؛ اصلاحات جاری هنوز commit نشده‌اند.
- PostgreSQL ایزوله: پروژه/منبع `barang-step45-postgres`، پورت `5545`، پایگاه `barang_step45_test` و volume مستقل؛ `barang_core` و volumeهای توسعه استفاده نشدند.
- API جاری: `127.0.0.1:5513`، artifact خروجی `.artifacts/step45-current-api` با content-root خودش.
- Backoffice جاری: `127.0.0.1:5514`، artifact خروجی `.artifacts/step45-current-backoffice` با content-root خودش، system Chrome.
- شرکت fixture: `STEP45B` با شناسه‌ی `df2a30fb-5566-4d7f-8c7a-c35f86014f31`؛ اطلاعات محرمانه، token و password در repository/report ثبت نشده‌اند.

### علت دقیق defect مرورگر و اصلاح

در اجرای قبلی، Backoffice با content-root checkout اجرا شده بود نه publish output. مسیر static web asset مانند `Components/Layout/ReconnectModal.razor.js` با HTTP 200 و body صفر بایت تحویل می‌شد. Chrome آن را به علت SRI نامعتبر مسدود می‌کرد؛ در نتیجه circuit Blazor تشکیل نمی‌شد و پس از Claim، `#blazor-error-ui` و متن «خطایی غیرمنتظره رخ داد» ظاهر می‌شد. این deployment mismatch بود، نه خطای business action.

اجرای همان artifact با `--contentRoot .artifacts/step45-current-backoffice` body واقعی و digest صحیح را سرو کرد. سپس تست واقعی `Step3ReservationAcceptanceE2ETests.InventoryReservationTaskCanBeCompletedThroughAuthenticatedUi` سبز شد. تست `SharedSortHeadersUserGridAndActionIconsMatchTheGlobalStandard` نیز پس از scoped CSS correction سبز شد. درخواست پشتیبانی‌نشده‌ی `/api/v1/orders/{id}/messages` از `CartableTaskDetail` حذف شد؛ Cartable برای این مسیر در API جاری contract پیام ندارد.

### fixture و نتایج browser جاری

fixture workflow از `tests/Acceptance/fixtures/barang-order-workflow.v17.sanitized.json` با APIهای Designer import/draft/validate/publish ساخته شد؛ nodeهای position با positionهای company-scoped map شدند و `InventoryIssue` به position `WHSE` وصل شد. fixture order در اجرای موفق قبلی این attempt دارای دو OrderItem مستقل با variant مشترک و task InventoryIssue بود؛ پس از reservation، reservationهای دو item persisted شدند و تست UI issue واقعی و retry را PASS کرد. شناسه‌ی task آن اجرای ایزوله `715fcd27-c20b-4dbd-a183-d7750aa1015a` بود.

نتایج قابل بازتولید همین source:

| سناریو/تست | نتیجه جاری | شواهد/علت |
|---|---|---|
| `Step3ReservationAcceptanceE2ETests.InventoryReservationTaskCanBeCompletedThroughAuthenticatedUi` | PASS | login، company selection، Claim، issue تمام اقلام، و retry از UI؛ بدون Blazor error در publish artifact صحیح |
| `DeployedInteractivityTests.SharedSortHeadersUserGridAndActionIconsMatchTheGlobalStandard` | PASS | border computed برای sort trigger برابر صفر؛ menu/action assertions جاری |
| Cartable diagnostic در artifact قبلی | FAIL | به علت static asset/SRI؛ بعد از content-root صحیح PASS شد و فایل diagnostic موقت حذف شد |
| Release solution build | PASS | 0 warning / 0 error |
| Unit | PASS | 222/222 |
| Backoffice | PASS | 184/184 |
| full Integration run با `BARANG_TEST_DB_CONNECTION` | FAIL/INCOMPLETE | یک اجرای کامل 129 تستی به 88 pass و 41 fail رسید؛ اجرای بعدی پس از 120 ثانیه بدون progress ماند و با timeout اجرایی متوقف شد. این شواهد جایگزین 128/128 تاریخی نیستند. |

نمونه failureهای full Integration جاری:

- `LookupFoundationTests.GlobalRowsAreVisibleWhileRlsIsolatesCompanyRowsAndPoolContextDoesNotLeak`: expected 61, actual 3؛ setup database پس از reset کامل fixture lookup را نداشت.
- `UserAccessManagementTests.IdentityPasswordResetDisableAndLockoutUseSecureAccountOperations`: `/api/v1/auth/login` HTTP 404؛ API factory/fixture در این run endpoint مورد انتظار را expose نکرد.
- `ArtworkFileE2ETests.CompleteArtworkFileFlowUsesRealMinioAndEnforcesSecurity`: login/bootstrap path HTTP 404.
- `Step4InventoryIssueE2ETests.WarehouseIssueIsAtomicAndRetrySafeWithoutDuplicatingLedgerEntries`: FK `stock_balances ... product_variants_company_id_product_variant...` در setup؛ test record/variant company context ناسازگار بود.
- `Step4InventoryIssueE2ETests.PreIssueAdjustmentIsAtomicVersionedAndRollsBackOnShortage`: endpoint setup HTTP 404.
- `SupplierSourcingApiTests` و `SupplierReferenceFoundationTests`: چند setup request با HTTP 404/409؛ این run تا بازسازی کامل test-host/fixture معتبر نیست.
- `Step45WorkflowPositionSetupTests` در اجرای اول به علت نبود `BARANG_ACCEPTANCE_COMPANY_ID` fail شد؛ test به‌صورت non-acceptance full-suite no-op شد و setup صریح با env جداگانه انجام می‌شود.

این failureها توضیح می‌دهند که full regression gate جاری هنوز green نیست؛ assertionها تضعیف نشده‌اند و testها حذف نشده‌اند.

### وضعیت deployment توسعه

به علت full Integration failure/hang و کامل نبودن ماتریس mutation browser، deployment به `http://localhost:5100` انجام نشد. بنابراین این گزارش ادعای تغییر UI واقعی پورت 5100 را ندارد؛ پورت 5100 همچنان باید جداگانه با `docker compose -p barang-core` و asset provenance بررسی شود. منابع توسعه، volume، credential و admin account دست‌نخورده ماندند.

## نتیجه جاری

**STEP 4.5 = FAIL / NOT ACCEPTED.** دو defect فنی بازتولیدشده (static asset/SRI در deployment artifact و sort-trigger border) رفع و با تست مرورگر جاری تأیید شدند. اما شرط کامل acceptance هنوز برقرار نیست: full Integration جاری fail/hang دارد و Orders/Design Bank/Pricing/nested Purchasing و STEP4 return با fixture معتبر و deployment واقعی پورت 5100 به‌صورت کامل اجرا نشده‌اند. STEP 5 همچنان paused است.

