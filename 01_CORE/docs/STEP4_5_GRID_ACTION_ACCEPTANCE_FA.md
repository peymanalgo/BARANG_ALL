# گزارش پذیرش STEP 4.5 — استانداردسازی منوی عملیات Grid

## وضعیت جاری

- منبع بررسی‌شده: commit فعلی checkout، branch فعلی `codex/step2-inventory-unit-contracts`.
- Release build برای Backoffice و API موفق شد؛ صفر warning و error.
- محیط توسعه، volumeها، کلیدها، داده‌ها و حساب مدیر تغییر نکردند. فایل محلی `.keys/` untracked است و commit نمی‌شود.

## قرارداد مشترک

row actionهای مهاجرت‌یافته از `BarangActionMenu` استفاده می‌کنند: trigger عمودی سه‌نقطه، popup پورت‌شده با برخوردسنجی viewport، outside-click، Escape و بازگردانی focus، single-open، RTL/LTR و keyboard navigation. actionهای popup تمام‌عرض و دارای icon/text محلی هستند. Add/Create/New مستقل و دارای plus باقی می‌ماند.

## inventory source

در source فعلی ۵۰ فایل/خانواده با markerهای table/data-grid شناسایی شد. خانواده‌های اصلی audited عبارت‌اند از:

Base Data و lookupها؛ organization/warehouses/persons/equipment؛ User/RBAC؛ Supplier و child grids؛ Customer و child grids؛ Design Bank؛ Orders و Order Wizard؛ Cartable؛ Pricing؛ Inventory، reservations، adjustments، purchasing، receipts و transfers؛ Finance، Quality، Logistics، Notifications و گزارش‌ها؛ و gridهای nested داخل modal/tab/wizard.

برای هر خانواده، source file و route از Razor inventory ثبت شد؛ row actionهای tabular موجود در ۲۸ فایل به menu مشترک منتقل شدند یا از قبل از آن استفاده می‌کردند. gridهای read-only بدون action، menu خالی ندارند. Addهای مشاهده‌شده در toolbar/header به‌صورت standalone باقی ماندند.

## کنترل‌های خارج از پوشش row-grid

- modal close، password toggle، chip removal، toolbar و workflow-canvas کنترل row action نیستند.
- OrganizationalChartTree و DesignCategoryTree ساختار hierarchical tree دارند و هنوز tree-specific action rendering دارند؛ این مورد باید در صورت الزام محصول به treeها با shared menu جداگانه مهاجرت شود.
- چند nested grid در PurchaseRequest/PurchaseOrder/InventoryPurchasing هنوز raw `icon-button` دارند. fallback popup آن‌ها styling کامل دارد، اما تبدیل نهایی به `BarangIconButton` برای SVG و متن استاندارد باقی مانده است.

## تست‌های جاری

| check | result |
|---|---|
| Backoffice Release build | PASS — 0 warning/error |
| API Release build | PASS — 0 warning/error |
| git diff --check | PASS |
| Unit current source | PASS — 222/222 |
| Backoffice current source | PASS — 184/184 |
| Integration current source | BLOCKED — 108 failed / 20 passed / 128 total because `BARANG_TEST_DB_CONNECTION` was not set; all failures originated in `PostgreSqlDatabaseFixture.InitializeAsync` line 22, not application assertions |
| STEP 3/4 baseline | preserved as historical accepted evidence; not substituted for current STEP 4.5 browser acceptance |
| full current-source browser matrix | NOT RUN in this pass |

## نتیجه

**STEP 4.5 = FAIL / NOT READY.** Shared component and the majority of tabular row actions are standardized without backend/schema/data changes, but complete acceptance cannot be claimed while raw nested controls remain and the full current-source browser matrix has not passed. STEP 5 شروع نشده است.

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

