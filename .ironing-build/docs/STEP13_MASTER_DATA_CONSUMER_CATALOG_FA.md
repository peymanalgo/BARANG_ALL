# کاتالوگ مصرف‌کنندگان Master Data — STEP 13

تاریخ بازبینی: ۱۴۰۵/۰۷/۲۴ (۲۰۲۶-۱۰-۱۶)  
Checkout: `codex/step2-inventory-unit-contracts`  
Commit بازبینی: `b09e2a07b943c8d49f5aa82e18a99d5beb3fb5f4`

این کاتالوگ از `DomainReferenceCatalog` و `ReferenceConsumerTraceability` به‌عنوان فهرست canonical انواع Lookup و مصرف‌کنندهٔ آن‌ها، و از مدل‌ها، endpointها، صفحات و تست‌های جاری به‌عنوان شواهد مصرف تهیه شده است. وضعیت‌های داخلی Workflow، Order، Inventory، Finance، Quality و Logistics در این فهرست Lookup نیستند؛ آن‌ها قراردادهای state machine هستند. این سند داده‌ای را حذف، غیرفعال یا remap نمی‌کند.

## شمارش و روش

| دامنه | تعداد |
|---|---:|
| دامنه‌های UI/API تعریف‌شده | ۱۴ |
| catalogهای Lookup عمومی/شرکتی | ۴۳ |
| adapterهای موجودیت ساختاریافته | ۲ |
| مجموع catalogهای قابل ردیابی | ۴۵ |
| تعریف‌های مصرف در `ReferenceConsumerTraceability` | ۴۵ |
| مسیرهای Backoffice دارای route | ۵۱ route در فایل‌های Pages؛ workspaceهای master-data و مصرف‌کننده جداگانه شمرده شده‌اند |

تست `MasterDataTraceabilityTests` برابری کامل catalog و consumer map را قفل می‌کند؛ بنابراین اضافه‌کردن catalog بدون ثبت مصرف‌کننده ممکن نیست.

## قرارداد مشترک Lookup

منبع: `src/BARANG.Core.Domain/Lookups/LookupModels.cs`، `DomainReferenceCatalog.cs`، `ReferenceConsumerTraceability.cs`؛ API: `src/BARANG.Core.Api/Lookups/LookupEndpoints.cs`؛ store/application: `LookupStore.cs` و `LookupAdministration.cs`؛ UI: `DomainReferenceWorkspace.razor`، `ReferenceValueGrid.razor` و `ReferenceSelect.razor`.

- کد فنی پس از normalize شدن هویت پایدار است؛ caption هویت نیست.
- نوع Global دارای `CompanyId = null` و نوع Company دارای `CompanyId` اجباری است؛ RLS و EF filter دامنه را اعمال می‌کنند.
- `GET /api/v1/lookups/{typeCode}/options?culture=...&activeOnly=true` گزینه‌های جدید را فعال و localized برمی‌گرداند؛ fallback برابر culture درخواستی، سپس `fa`، سپس code است.
- `ReferenceSelect` مقدار واقعی ID را ذخیره می‌کند و فقط در قراردادهایی که `UseCode=true` است code را ذخیره می‌کند؛ placeholder خالی persisted نمی‌شود. مقدار inactive موجود در edit/history با `IncludeInactive` قابل مشاهده است.
- حذف عمومی Lookup به‌صورت قراردادی وجود ندارد؛ حذف مقدار از API فقط وقتی بدون reference و با version صحیح است. برای category دارای child و مقدار referenced خطای دقیق برمی‌گردد؛ deactivate مسیر حفظ history است.
- `Version` concurrency token است و تغییر stale با `lookup.concurrency_conflict` رد می‌شود. ترجمه‌های `fa/en/ar` در جدول translation با uniqueness به ازای value/culture نگه‌داری می‌شوند.

## فهرست کامل catalogهای configurable

### تأمین‌کننده و مشتری

| Code | Scope | مصرف‌کننده، فیلد/رابطه | API / UI | وضعیت |
|---|---|---|---|---|
| `supplier.category` | Company | Supplier.CategoryCode | supplier core / Supplier > Core | integrated |
| `supplier.grade` | Company | Supplier.GradeCode | supplier core / Supplier > Evaluation | integrated |
| `freight.delivery.term` | Company | SupplierSourcing.DeliveryTermCode | supplier sourcing / Supplier > Sourcing | integrated |
| `supplier.hold.reason` | Company | SupplierLifecycle.HoldReasonCode | supplier lifecycle / Supplier > Lifecycle | integrated |
| `customer.group` | Company | Customer.CustomerGroupCode | customer administration / Customer > General | integrated؛ ورودی pricing |
| `price.group` | Company | Customer.PriceGroupCode | customer administration / Customer > Commercial | integrated؛ tariff selection |
| `legal.entity.type` | Company | LegalCustomerProfile.LegalEntityTypeCode | customer administration / Customer > General | integrated |
| `payment.term` | Company | CustomerFinancialProfile.PaymentTermCode | customer administration / Customer > Financial | integrated |
| `payment.condition` | Company | CustomerFinancialProfile.PaymentConditionCode | customer administration / Customer > Financial | integrated |
| `payment.method` | Global | CustomerFinancialProfile.PaymentMethodCode و Payment.PaymentMethodCode | customer/finance APIs / Customer > Financial، Finance | integrated |
| `tax.status` | Company | CustomerFinancialProfile.TaxStatusCode | customer/pricing / Customer > Financial، Pricing | integrated |
| `address.type` | Company | CustomerAddress.AddressTypeCode | customer children / Customer > Addresses | integrated |
| `contact.role` | Company | CustomerContact.ContactTypeCode | customer children / Customer > Contacts | integrated |
| `bank` | Global | CustomerBankAccount.BankCode | customer children / Customer > Bank Accounts | integrated |
| `bank.account.type` | Company | CustomerBankAccount.AccountTypeCode | customer children / Customer > Bank Accounts | integrated |

### محصول، واحد و Design Bank

| Code | Scope | مصرف‌کننده، فیلد/رابطه | API / UI | وضعیت |
|---|---|---|---|---|
| `product.type` | Company | Product.ProductTypeCode | catalog / Product > Classification | integrated |
| `product.size` | Company | ProductVariant.SizeCode | catalog / Product > SKUs | integrated |
| `product.color` | Company | ProductVariant.ColorCode | catalog / Product > SKUs | integrated |
| `unit.of.measure` | Company | Product.UnitOfMeasureCode و item/reservation/ledger contracts | catalog, order, inventory / Product، Order، Inventory | integrated؛ canonical UOM |
| `product.brand` | Company | ProductTechnicalProfile.BrandCode | catalog / Product > Classification | integrated |
| `product.material` | Company | ProductTechnicalProfile.MaterialCode | catalog / Product > Classification | integrated |
| `product.gender` | Company | ProductTechnicalProfile.GenderCode | catalog / Product > Classification | integrated |
| `product.fit` | Company | ProductTechnicalProfile.FitCode | catalog / Product > Classification | integrated |
| `product.style` | Company | ProductTechnicalProfile.StyleCode | catalog / Product > Classification | integrated |
| `artwork.category` | Company | Artwork.CategoryId | artwork API / Design > Classification > Category tree | integrated؛ parent hierarchy |
| `artwork.subcategory` | Company | Artwork.SubcategoryId | artworks / Design > Classification | integrated |
| `artwork.type` | Company | Artwork.TypeId | artworks / Design > Classification | integrated |
| `artwork.style` | Company | Artwork.StyleId | artworks / Design > Classification | integrated |
| `artwork.theme` | Company | Artwork.ThemeId | artworks / Design > Classification | integrated |
| `artwork.audience` | Company | Artwork.AudienceId | artworks / Design > Classification | integrated |
| `artwork.license` | Company | Artwork.LicenseId | artworks / Design > License | integrated |
| `artwork.usage` | Company | Artwork.UsageId | artworks / Design > Usage | integrated |
| `artwork.tag` | Company adapter | ArtworkTagAssignment.TagId | artwork tags / Design > Tags | adapter؛ entity API |

### سفارش، تولید، کیفیت، موجودی و لجستیک

| Code | Scope | مصرف‌کننده، فیلد/رابطه | API / UI | وضعیت |
|---|---|---|---|---|
| `order.type` | Company | OrderBusinessProfile.OrderTypeCode | orders / Order Wizard > General | integrated |
| `inventory.adjustment.reason` | Company | InventoryAdjustment.ReasonCode | inventory / Inventory > Adjustments | integrated |
| `equipment.type` | Company | Equipment.EquipmentTypeCode | equipment / Equipment > General و Production assignment | integrated |
| `equipment.status` | Company | Equipment.OperationalStatusCode برای eligibility و Equipment.LifecycleStatusCode برای lifecycle/history | equipment / Equipment > General/Lifecycle | integrated با دو فیلد مستقل |
| `print.method` | Company | OrderPrintOperation.PrintMethodCode، PrintJob.PrintMethodCode | orders/production / Order Wizard، Production > Print | integrated |
| `print.direction` | Company | PrintTechnicalProfile.DirectionCode | production/order profile / Print | integrated |
| `print.size` | Company | PrintTechnicalProfile.SizeCode | production/order profile / Print | integrated |
| `defect.category` | Company | Defect.CategoryCode و DefectType category validation | quality / Quality > Defects | integrated |
| `defect.type` | Company adapter | QCDefect.DefectTypeId | defect types / Quality > Defects | adapter؛ entity API |
| `packaging.type` | Company | OrderBusinessProfile.PackagingTypeCode و Shipment.PackageTypeCode | orders/logistics / Order Wizard، Logistics > Packaging | integrated |
| `shipping.method` | Company | OrderBusinessProfile.ShippingMethodCode و Shipment.ShippingMethodCode | orders/logistics / Order Wizard، Logistics > Shipping | integrated |
| `shipping.carrier` | Company | Shipment.CarrierCode | logistics / Logistics > Shipping | integrated |

### نکتهٔ سازگاری Equipment

`equipment.type` و `equipment.status` اکنون در metadata دامنهٔ canonical `equipment` و alias قدیمی `inventory` دیده می‌شوند. مسیر قبلی `/master/reference-data/inventory/...` حفظ شده است؛ صفحهٔ مالک `/master/equipment` و مسیر `/master/reference-data/equipment/...` نیز همان catalog را مصرف می‌کنند. `OperationalStatusCode` و `LifecycleStatusCode` دو مفهوم و دو فیلد هستند؛ اشتراک catalog به معنی تبدیل آن‌ها به یک state machine نیست.

## موجودیت‌های master اختصاصی و رابطهٔ آن‌ها

| خانواده | مدل/منبع canonical | scope و hierarchy | مصرف/حفاظت |
|---|---|---|---|
| Organization | `Company`, `Branch`, `OrganizationalUnit`, `Position`, `Person`, `Warehouse`, `WarehouseResponsibility` در `Domain/Organization` و `OrganizationEndpoints.cs` | company؛ واحد/position hierarchy و warehouse branch | assignment، workflow routing، inventory/production eligibility؛ composite FK و company authorization |
| Customer | `Customer` و profile/children در `Domain/Customers`, `CustomerAdministration` | company؛ address/contact/bank children | order, pricing, finance؛ inactive historical references حفظ می‌شوند |
| Product | `Product`, `ProductVariant`, technical profile در `Domain/Catalog` و `CatalogEndpoints.cs` | company؛ Product→Variant | order, UOM, inventory, purchasing, production؛ SKU/code هویت مستقل |
| Supplier | core/sourcing/lifecycle models و سه endpoint family Catalog/Supplier* | company؛ supplier→product/variant | purchasing sourcing و lifecycle؛ تاریخچه و version |
| Design Bank | Artwork/File/Version، category lookup hierarchy، ArtworkTag adapter | company؛ category parent و file/version lineage | order print, production job, quality lineage؛ version snapshot immutable |
| Equipment | `Equipment` و capability/assignment relationships | company؛ branch/unit/warehouse/person refs | production eligibility، equipment CRUD؛ active/status/version و cross-company FK |
| Pricing | `Tariff`, `DiscountRule`, `TaxRule`, customer group/price group lookups | company؛ validity dates و rule references | order estimate/final snapshot؛ finalized snapshots immutable |
| Workflow | definitions, versions, nodes, positions, assignments, runtime tasks | company/global definition حسب مدل؛ version binding | Cartable و تمام completion gates؛ internal statuses fixed contracts |
| Inventory/Purchasing | warehouse/stock/reservation/issue/shortage/proposal/PR/PO/receipt | company؛ stock key warehouse+variant+UOM | ledger/reservation/purchasing؛ quantity و UOM authoritative |
| Finance | invoice/payment/balance و approval evidence | company/order/customer | finalized price snapshot، invoice/payment؛ state/status داخلی configurable lookup نیست |
| Quality | templates, defects, inspections, dispositions, reproduction lineage | company؛ Job→Inspection→replacement Job | quality gates و production lineage؛ accepted/rejected/reproduction quantities |
| Logistics | package/shipment/delivery/ShipmentReturn | company؛ exact OrderItem links | packaging/shipping/delivery/customer return؛ return از stock/refund مستقل است |
| Notifications | Outbox, Notification, DeadLetter و template codes | company؛ recipient membership/permission | business transitions؛ delivered/read state داخلی است و operation را approve نمی‌کند |

منابع مشترک این جدول: `CoreDbContext` و configurationهای `Persistence/Configurations` برای FK/index/RLS، endpointهای هم‌نام در `src/BARANG.Core.Api`، و صفحات `src/BARANG.Backoffice/Components/Pages` شامل CustomerManager، SupplierManager، ProductManager، DesignBank، DomainReferenceWorkspace، EquipmentManager، OrganizationChart، WorkflowWorkspace، PricingWorkspace، InventoryPurchasingWorkspace، FinanceWorkspace، QualityWorkspace، LogisticsWorkspace و NotificationCenter.

## دسته‌بندی مصرف

- **Integrated:** هر ۴۵ catalog در consumer map مصرف واقعی در persistence/API/UI یا validation دارند؛ ۲ مورد (`artwork.tag`, `defect.type`) adapter موجودیت هستند، نه جدول Lookup تکراری.
- **Partially integrated / compatibility:** خانوادهٔ Equipment دو دامنهٔ نمایشی و دو status field دارد؛ این STEP metadata/traceability را هم‌راستا کرد و رفتار legacy را نگه داشت. تفکیک دو catalog مستقل برای lifecycle و operational status نیازمند تصمیم محصول است و در این STEP اعمال نشد.
- **Informational/classification:** category/tag/brand/material/style و defect category در classification/filter/validation مصرف می‌شوند؛ نبودنشان در محاسبهٔ قیمت به معنی unused نیست.
- **Internal fixed contract:** statusهای Workflow/Order/Stock/Invoice/Shipment/Job/Inspection و permission codes در enum/contract می‌مانند؛ تبدیل آن‌ها به Lookup بدون business approval مجاز نیست.
- **Genuinely unused candidate:** در catalogهای canonical هیچ موردی با شواهد کافی برای «بی‌مصرف قطعی» پیدا نشد. برخی entity fields آینده‌گرا هستند و به‌عنوان candidate جداگانه ثبت شده‌اند، نه حذف‌شده.

## مسیرهای بررسی و شواهد

- navigation و routeها: `Components/Pages/*.razor`, `Components/ModuleCatalog.cs`؛ master routes شامل customers, suppliers, products, design bank/categories/tags, equipment, organization/persons, reference-data, workflow, pricing, inventory, finance, quality, logistics و notifications است.
- endpointها: `CatalogEndpoints`, `Supplier*Endpoints`, `CustomerEndpoints`, `LookupEndpoints`, `OrganizationEndpoints`, `ArtworkFileEndpoints`, `PricingEndpoints`, `InventoryEndpoints`, `FinanceEndpoints`, `ProductionEndpoints`, `QualityEndpoints`, `LogisticsEndpoints`, `WorkflowEndpoints`, `BackgroundEndpoints`.
- persistence: configurationهای Catalog/Customer/Lookup/Organization/Workflow/Pricing/Inventory/Finance/Quality/Logistics/Background و migration chain تا checkout جاری.
- مصرف generic: `ReferenceSelect`, `SearchableSelect`, `DomainReferenceWorkspace` و `ILookupOptionReader`; علاوه بر این، resolverهای Order/Pricing/Inventory/Production مستقیماً code/ID canonical را validate می‌کنند.
- آزمون: `LookupFoundationTests`, `SupplierReferenceFoundationTests`, `CatalogFoundationTests`, `ArtworkFileE2ETests`, `BusinessProfileApiTests`, `FinanceFoundationTests`, `OrderPrintStepTests`, و Backoffice design/route tests.

