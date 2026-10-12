# گزارش بازیابی regression سراسری BARANG

تاریخ: ۱۴۰۵/۰۷/۲۰

## محدوده و baseline

این recovery روی checkout مستقر `D:\00_BARANG\step2-clean\01_CORE` و branch
`codex/step2-inventory-unit-contracts` انجام شد. commit مبنای قبل از این اصلاحات
`977b7d942ef78db013fd30be2e30ec2e7a06f227` بود. source canonical دیگری در
`D:\00_BARANG\01_CORE` وجود دارد، اما working tree آن dirty است و برای جلوگیری از
انتقال تغییرات نامرتبط دست‌نخورده باقی ماند. مقایسهٔ source، routeها، APIها، migrationها
و history نشان داد که بعضی صفحات Backoffice از generation جدیدتری از API استفاده
می‌کنند که در این checkout مستقر وجود ندارد؛ کپی‌کردن ناقص آن generation باعث حذف
یا stub کردن functionality می‌شد و انجام نشد.

قبل از deployment، backup توسعه در مسیر خارج از repository ایجاد و با
`pg_restore --list` و restore به PostgreSQL ایزولهٔ `barang_recovery_stopplan_test`
اعتبارسنجی شد. هیچ reset، migration داده‌ای، تغییر admin، یا تغییر schema در این
مرحله انجام نشد.

## علت‌های تأییدشده و اصلاحات

| صفحه/مسیر | علت مشاهده‌شده | اصلاح انجام‌شده | نتیجهٔ فعلی |
|---|---|---|---|
| Users and Permissions | صفحه `/security/users/search` و `/security/roles/search` را صدا می‌زد، ولی API مستقر فقط root list دارد؛ 404 | `UserAccessManagement.razor` برای بارگذاری اولیه از endpointهای واقعی root استفاده می‌کند و envelope/array هر دو را می‌خواند | API list کاربران و نقش‌ها 200؛ dialogهای advanced هنوز نیازمند API متناظر هستند |
| Suppliers | صفحه `/suppliers/core` را صدا می‌زد، ولی API موجود `/suppliers/` است؛ 404 و empty misleading | `SupplierManager.razor` به list واقعی و activation واقعی وصل شد و شکل object/array را تحمل می‌کند | list API 200؛ editor/core CRUD در این checkout هنوز اثبات نشده است |
| Design Bank | UI منتظر `{items,totalCount}` بود، ولی API موجود array برمی‌گرداند؛ lifecycle خطا را نیز کامل نمی‌کرد | `DesignBank.razor` هر دو شکل پاسخ را می‌خواند و با `try/finally` loading را پایان می‌دهد | API `GET /api/v1/artworks` برابر 200؛ browser/circuit و modal CRUD به‌دلیل نبود browser automation تأیید نشده |
| Pricing | UI مسیر `/pricing/tariffs/search` را می‌خواست (405)، API موجود list با slash پایانی است؛ contract قدیمی/جدید مخلوط بود | `PricingWorkspace.razor` از list واقعی استفاده می‌کند، فیلتر صفحه‌ای را حفظ می‌کند و پاسخ قدیمی numeric را parse می‌کند | API tariff list 200؛ create/edit پیشرفتهٔ generation جدید هنوز اثبات نشده |
| Workflow Designer | UI به `/workflows/definition` نیاز دارد، ولی API مستقر این route را ندارد (404). در checkout فعلی implementation معتبر `WorkflowDefinitionRuntime`/designer administration پیدا نشد؛ فقط contract یا stub ساختن امن نیست | تغییری برای پنهان‌کردن خطا یا جعل definition انجام نشد | **blocker باقی‌مانده**؛ load definition/nodeها قابل تأیید نیست |

## شواهد API و persistence

با login واقعی local admin و company مجاز، بدون ثبت token/credential، این پاسخ‌ها
بعد از deployment ثبت شدند:

| درخواست | نتیجه |
|---|---|
| `GET /api/v1/security/users/` | 200، آرایهٔ واقعی |
| `GET /api/v1/security/roles/` | 200، آرایهٔ واقعی |
| `GET /api/v1/suppliers/?search=&pageNumber=1&pageSize=20&activeOnly=true` | 200، object واقعی |
| `GET /api/v1/artworks?search=&limit=20` | 200، آرایهٔ واقعی |
| `GET /api/v1/pricing/tariffs/?includeInactive=false` | 200، آرایهٔ واقعی |
| `GET /api/v1/workflows/definition` | 404؛ در لاگ API با company context ثبت شد |

علت‌های قبلی که در commit مبنا حفظ و در source تأیید شدند نیز عبارت‌اند از mapping
`Person` به جدول lowercase `barang.persons`، پشتیبانی `TariffType.Design`، و routeهای
سازمانی. normalized Customer، login، role/permission admin و pricing/unit fixes
حفظ شده‌اند.

## تغییرات این recovery

- `src/BARANG.Backoffice/Components/Pages/PricingWorkspace.razor`: استفاده از route
  واقعی tariff و parser سازگار با پاسخ array/numeric.
- `src/BARANG.Backoffice/Components/Pages/DesignBank.razor`: سازگاری با پاسخ array،
  پایان قطعی loading و جلوگیری از باقی‌ماندن state در خطا.
- `src/BARANG.Backoffice/Components/Pages/UserAccessManagement.razor`: list واقعی
  user/role و parser سازگار با DTO موجود.
- `src/BARANG.Backoffice/Components/Pages/SupplierManager.razor`: list و activation
  واقعی supplier، بدون fabricated empty data.

چهار migration/config تغییرکردهٔ قبلی در working tree عمداً stage نشدند و متعلق به
این recovery نیستند. هیچ فایل canonical dirty خارج از checkout تغییر نکرد.

## build، deployment و verification

- API و Backoffice با Release/no-restore build شدند: هر دو ۰ warning و ۰ error.
- compose context: `D:\00_BARANG\step2-clean\01_CORE`؛ فقط `api` و `backoffice`
  rebuild/up شدند، PostgreSQL reset نشد.
- API image: `sha256:5505cb9112ec90007d8c3665ba58d02cc10b2dba67e64a7a7f49c164b3cf7398`
- Backoffice image: `sha256:0360e04f063bc42124ea5360fbff893c7efa969392ffc54aee781b431f9000f2`
- API و Backoffice پس از deploy healthy بودند؛ migration history تغییر جدیدی ندارد.
- backup توسعه: `D:\00_BARANG\barang_core_recovery_stopplan_20261012162450.backup`؛
  archive فهرست‌پذیر و restore ایزوله موفق بود.
- browser automation در این محیط browser/tab در دسترس نداشت؛ بنابراین visual layout،
  Blazor circuit واقعی، modal CRUD و screenshot verification **pending** است. HTTP 200
  به‌تنهایی به‌عنوان تأیید UI ادعا نمی‌شود.

### اجرای تست در این recovery

- `BARANG.Core.UnitTests`: **۱۸۹/۱۸۹ موفق**.

## بازیابی انتخابی Supplier و User/RBAC — ۱۴۰۵/۰۷/۲۰

در reconciliation بعدی، source canonical `D:\00_BARANG\01_CORE` با target
`D:\00_BARANG\step2-clean\01_CORE` مقایسه شد. source اصلی دست‌نخورده ماند و
snapshot قابل‌بازگشت در `D:\00_BARANG\recovery-snapshots\20261012170107`
ثبت شد. bundleهای واقعی و قابل‌بازیابی عبارت‌اند از Supplier Core، Supplier
Lifecycle، Supplier Sourcing و نسل پیشرفتهٔ User/RBAC؛ Workflow Designer و
`WorkflowDefinitionRuntime` در هیچ‌یک از دو checkout وجود نداشت و عمداً پیاده‌سازی
یا stub نشد.

### پیاده‌سازی انجام‌شده

- routeهای واقعی `SupplierCoreEndpoints`، `SupplierLifecycleEndpoints` و
  `SupplierSourcingEndpoints` به `MapCatalogEndpoints` متصل شدند؛ registrationهای
  `ISupplierCoreAdministration`، `ISupplierLifecycleAdministration` و
  `ISupplierSourcingAdministration` به DI افزوده شد.
- مدل‌های domain، قراردادها، administrations، configurations، تست‌ها و workspaceهای
  Supplier از source canonical بازیابی شدند؛ DbSet و query filterهای شرکت‌محور برای
  childها، lifecycle و sourcing افزوده شد. Supplier migrationهای موجود target
  دوباره اعمال نشدند.
- User/RBAC پیشرفته، `UserPermission`، IdentityAccountService، UserStore و
  registrationهای `UserManager` از implementation واقعی source بازیابی شدند؛
  `IProductSalesPriceStore` نیز برای حفظ Catalog/SalesPrice contract ثبت شد.
- مقادیر فایل‌های Supplier contract/document به `FileLinkEntityType` افزوده شد؛ این
  تغییر enum است و migration جدیدی اجرا نشد.
- تست‌های تاریخی Organization/Cartable که به مدل‌های Workflow مفقود متکی بودند در
  این bundle نگه‌داری نشدند؛ حذف آن‌ها از source application انجام نشده است.

### شواهد اجرای فعلی

- API Release build: موفق، ۰ warning و ۰ error.
- Backoffice Release build: موفق، ۰ warning و ۰ error.
- تست واحد فیلترشدهٔ Supplier/User: **۲۱/۲۱ موفق**.
- تست‌های integration روی PostgreSQL ایزوله compile شدند، اما اجرای migration chain
  به‌صورت صحیح با خطای `PendingModelChangesWarning` متوقف شد؛ snapshot target با
  مدل بازیابی‌شده هم‌تراز نیست و snapshot تاریخی همچنین entityهای Workflow را دارد
  که source فعلی آن‌ها را ندارد. این هشدار نادیده گرفته نشد و migration جدید نیز
  روی توسعه اعمال نشد.
- تست‌های Backoffice: build موفق، اما **۱۶ شکست از ۱۸۴**؛ شکست‌ها مربوط به
  asset/document pathهای موجود نبودن در clean checkout، assertionهای generationهای
  متفاوت و تست‌هایی است که صریحاً `WorkflowDefinitionRuntime` و
  `CartableSavedFilterAdministration` مفقود را می‌خوانند. این موارد هنوز completion
  کامل recovery را ثابت نمی‌کنند.
- پس از اصلاح DI، login واقعی local admin با status 200، `/auth/me` با company
  مجاز، و `GET /api/v1/suppliers/core?pageNumber=1&pageSize=5` با status 200
  تأیید شد. مسیرهای UI و dialogهای Supplier/User با browser automation بصری تست
  نشده‌اند.

### deployment و محدودیت باقی‌مانده

از compose همین target، فقط API و Backoffice از نو build/up شدند. imageهای در حال
اجرا:

- API: `sha256:8e625b09dec49e1a37366be0a8330d6d2117e768e7953b21468eff45b3007d0c`
- Backoffice: `sha256:9b1f8b5b4518e53c2d7ccf62940426e27f76fd1c53d6ee6dbffb3384474aa344`

هر دو container healthy و `/health/ready` و `/login` با status 200 هستند. backup
فشردهٔ PostgreSQL توسعه در خارج از repository در
`D:\00_BARANG\recovery-snapshots\20261012170707\barang_core.dump` ساخته و با
`pg_restore --list` دارای ۱۷۳۵ entry بررسی شد؛ دادهٔ توسعه، admin و migration
history تغییر داده نشدند. restore کامل این backup در این نوبت اجرا نشده است.

مانع اصلی برای ادعای recovery کامل، هم‌ترازی migration snapshot/مدل و نبود
implementation واقعی Workflow/Designer است. مرحلهٔ بعد باید با مقایسهٔ schema
تازه و restored، snapshot repair نسخه‌دار و بدون حذف جدول‌های تاریخی انجام شود؛
تا آن زمان Supplier/User در سطح build و API smoke قابل مشاهده‌اند، اما end-to-end
پایداری PostgreSQL و browser visual verification تأیید نشده است.
- `CustomerNormalizedApiTests`: **موفق**.
- `BusinessProfileApiTests`: یک تست موفق و یک تست ناموفق؛ submit سفارش در test host
  با `core.unexpected`/HTTP 500 شکست خورد و این مسیر هنوز recovery-complete محسوب
  نمی‌شود.
- `DatabaseFoundationTests`: قابل استناد به‌عنوان pass نیستند؛ assertion قدیمی عدد
  migration را ۲۳ فرض می‌کند، درحالی‌که chain فعلی ۷۶–۷۸ migration دارد. این mismatch
  در کد recovery تغییر داده نشد.

## matrix پوشش و remaining

| حوزه | وضعیت |
|---|---|
| Base Data، Customers، Products، Orders، Inventory/Purchasing | smokeهای API قبلی حفظ شده‌اند؛ browser و CRUD mutation کامل در این اجرا تأیید نشده |
| Users/Roles | list واقعی تأیید شد؛ advanced permission/substitution dialogs نیازمند endpointهای نسل جدید هستند |
| Suppliers | list/activation تأیید شد؛ SupplierEditor که هنوز `/suppliers/core` می‌خواهد blocker CRUD است |
| Design Bank | API و lifecycle اصلاح شد؛ visual/circuit و فرم‌های جزئیات pending |
| Pricing | list API و parsing اصلاح شد؛ فرم‌های tariff پیشرفته pending |
| Cartable/Workflow | مسیرهای runtime نسل جدید در API مستقر وجود ندارند؛ Workflow Designer و Cartable کامل **قابل قبول اعلام نمی‌شوند** |

### نتیجه

بخشی از regression مشترکِ contract mismatch بازیابی و deploy شده است، اما recovery
هنوز کامل نیست. نبود implementation معتبر و قابل انتساب برای Workflow Designer/
WorkflowDefinitionRuntime و باقی‌ماندن endpointهای advanced برای SupplierEditor و
dialogهای مدیریت دسترسی، مانع اعلام «بازگشت کامل functionality» است. این موارد باید
از source canonical کامل یا artifact معتبر بازیابی شوند؛ در این مرحله placeholder،
empty handler، permissive authorization یا rollback کور انجام نشد.

## ادامهٔ recovery پس از commit 8384bbb

- provenance بازبینی شد: canonical working tree نیز build کامل نداشت و به
  `DecisionAttachmentView` و implementationهای مفقود runtime ارجاع می‌داد؛ بنابراین به‌عنوان
  baseline کامل کپی نشد. migrationها و schema واقعی workflow در DB توسعه وجود دارند.
- Order Submit با exception دقیق `Npgsql 42501: permission denied for sequence order_number_seq`
  شکست می‌خورد؛ role ایزولهٔ تست با grant حداقلی sequence اصلاح شد. منطق production یا
  مجوزهای business تغییر نکرد.
- migration tests از عدد ثابت ۲۳ به بررسی کل migrationهای مدل، نبود pending migration
  و وجود migrationهای workflow/inventory تغییر کردند؛ روی DB ایزوله restore‌شده
  تست‌های مربوطه و BusinessProfile submit در مجموع **۳/۳ موفق** شدند.
- routeهای واقعی `GET /api/v1/workflows/definition` و positions به جداول موجود و
  RLS company context متصل شدند و `POST /definition/validate` validation پایهٔ graph
  را انجام می‌دهد. authenticated response شامل **۲۳ node و ۱۷ version** تأیید شد.
- save draft، Cartable runtime کامل، SupplierEditor/core CRUD و dialogهای advanced User
  Management هنوز پیاده‌سازی/تأیید نشده‌اند؛ recovery کامل اعلام نمی‌شود.
## اصلاحات تکمیلی پس از استقرار recovery

در checkout شاخه `codex/step2-inventory-unit-contracts` دو ناسازگاری قطعی دیگر اصلاح شد:

- `SecurityEndpoints` اکنون مسیرهای واقعی `GET /api/v1/security/users/search` و `GET /api/v1/security/roles/search` را با همان سرویس‌های canonical و پاسخ envelope شامل `items` ارائه می‌کند؛ فیلتر فقط روی داده‌های واقعی انجام می‌شود و authorization قبلی حفظ شده است.
- `SupplierManager` برای تغییر وضعیت از `POST /api/v1/suppliers/{id}/activation` استفاده می‌کند؛ قرارداد واقعی API همین است و استفاده‌ی قبلی از PUT باعث شکست عملیات فعال‌سازی می‌شد.

Build محلی Release برای API و Backoffice با صفر warning/error موفق شد. هر دو image از همین checkout بازسازی و deploy شدند. بررسی authenticated API در محیط محلی: Workflow definition با HTTP 200 و 23 node/17 نسخه، جستجوی کاربران و نقش‌ها با HTTP 200. browser visual verification در این محیط در دسترس نبود.

این اصلاحات به معنی تکمیل recovery نیستند. هنوز مسیرهای `workflow/definition/runtime/*`، ذخیره‌ی draft/publish طراح، پیاده‌سازی کامل Cartable، و قرارداد کامل `suppliers/core` در API موجود نیستند؛ بنابراین SupplierEditor پیشرفته، Cartable و چرخه‌ی Designer save/publish هنوز acceptance کامل ندارند. این مسیرها عمداً با پاسخ جعلی یا stub پوشانده نشدند.

## هم‌ترازی EF مدل/اسکیما و راستی‌آزمایی Supplier/User — ۱۴۰۵/۰۷/۲۰

### علت دقیق PendingModelChanges

snapshot قبلی (`20261012124313_ProbeModelAlignment`) مدل runtime بازیابی‌شده را نمایندگی نمی‌کرد. مقایسهٔ object-level، ۱۱۸ جدول در مدل runtime و ۹۹ جدول قابل مشاهده در snapshot قبلی را نشان داد. اشیای غایب شامل جدول‌های Supplier child/sourcing/lifecycle، `user_permissions`، `product_sales_prices` و بخشی از Organization بودند؛ ستون‌های Supplier و Security مانند `suppliers.entity_type`, `supplier_number`, `first_name`, `last_name`, `is_blocked`, `is_preferred` و `application_users.access_failed_count`, `lockout_end_utc`, `security_stamp`, `person_id`, `last_login_at_utc` نیز در مدل runtime وجود داشتند.

یک mapping واقعی هم پیدا شد: `ProductSalesPrice` بدون configuration به جدول convention یعنی `ProductSalesPrices` می‌رفت، در حالی‌که migration رسمی جدول `product_sales_prices` را ایجاد می‌کند. همچنین rename پیشنهادی `Persons` فقط اختلاف snapshot تاریخی بود؛ migration رسمی از ابتدا `persons` را می‌سازد و اجرا نشد.

### اصلاح نسخه‌دار

- `ProductSalesPriceConfiguration.cs` اضافه شد و نام جدول/ستون‌ها، precision، روابط Product/Variant، index resolution و check constraintها با migration رسمی هم‌تراز شد.
- migration `20261012135357_ReconcileRecoveredSupplierUserModel` با `Up/Down` خالی اضافه شد تا snapshot فعلی را ثبت کند؛ هیچ Drop/Rename/DropColumn تولیدشده از snapshot قدیمی وارد آن نشد.
- جدول‌های تاریخی Workflow که در schema هستند ولی entity متناظر در `CoreDbContext` فعلی ندارند حفظ شدند؛ Workflow runtime در این کار پیاده‌سازی نشده است.

### پایگاه‌داده و شواهد

backup توسعه در `D:\00_BARANG\recovery-snapshots\20261012170707\barang_core.dump` با `pg_restore --list` بررسی و در DB ایزوله restore شد. fresh DB کل migration chain را تا `20261012135357_ReconcileRecoveredSupplierUserModel` با موفقیت اجرا کرد و PendingModelChanges ایجاد نشد. روی restored DB نیز migration جدید بدون تغییر داده اعمال شد. نمونه شمارش دادهٔ restored: `companies=5`, `customers=1`, `suppliers=65`, `products=56`, `orders=35`, `application_users=12`.

پس از migration، اختلاف fresh/restored فقط ۲۸ metadata row مربوط به defaultهای قدیمی Equipment (برای `lifecycle_status_code` و ستون‌های active/version جداول child) بود؛ این اختلاف تاریخی backup است، نه Supplier/User و نه reconciliation migration. هیچ رکورد business حذف یا backfill نشد.

### تست‌ها و محدودیت‌ها

- API و Backoffice Release build: هر دو ۰ warning و ۰ error.
- Unit تست‌های Supplier/User: **۲۱/۲۱ موفق**.
- Supplier root round-trip روی PostgreSQL تازه: **۲/۲ موفق**.
- اجرای کامل SupplierCore: **۴/۱۰ موفق، ۶ شکست**؛ شکست‌های باقی‌مانده 422/403/500 در lookup/sourcing fixture و authorization runtime هستند، نه EF migration.
- Backoffice: **۱۶/۱۸۴ شکست، ۱۶۸ موفق**. هفت تست مستقیماً به `WorkflowDefinitionRuntime.cs` مفقود، یک تست به `CartableSavedFilterAdministration.cs` مفقود، یک تست به asset `04_Document/1.png`، یک تست به سند UI/UX مفقود، و شش assertion نسل قدیمی Customer/Order/Cartable/Print وابسته‌اند. این تست‌ها بدون evidence تغییر یا غیرفعال نشدند.
- login/admin و readiness محلی: `/health/ready` و `/login` هر دو HTTP 200؛ browser visual verification فرم‌های Supplier/User انجام نشد.

### deployment فعلی

- API: `sha256:8f37b49116c15ed1f821b4da8030276c5bfc90bcddc3cab5605a43659ab4d041`
- Backoffice: `sha256:99726fad536f84a766395a1e17d60bf5df299b398f37977943831400aa5ec40f`

هر دو image از همین checkout با Compose محلی deploy شدند؛ production deploy انجام نشد. Supplier/User به‌دلیل شکست‌های قراردادی/fixture فوق هنوز functional end-to-end verified اعلام نمی‌شوند و Workflow runtime/Designer همچنان blocker مستقل است.

## بازیابی Base Data و lookupهای مشترک — ۱۴۰۵/۰۷/۲۰

### علت‌های قطعی

خطای واحدی برای همهٔ منوها وجود نداشت:

1. `DomainReferenceWorkspace.razor` از `GET /api/v1/lookups/domains?culture=...` استفاده می‌کرد، اما endpoint و قرارداد فهرست دامنه‌ها در checkout مستقر وجود نداشت.
2. `ReferenceSelect.razor` برای `artwork.tag` و `defect.type` به adapterهای `/api/v1/reference-entities/{type}/values` نیاز داشت؛ این routeها ثبت نشده بودند.
3. مسیرهای ایجاد/ویرایش/حذف مقدار lookup و فیلدهای `scope`, `parentId`, `descriptions` در قرارداد API و administration ناقص بودند؛ در نتیجه فرم‌های Base Data پیش از save پیام خطای عمومی نشان می‌دادند.
4. `CompanyContextMiddleware` دسترسی admin سیستم به شرکت انتخاب‌شده را فقط از membership مستقیم می‌سنجید. بررسی به `HasEffectiveCompanyAccessAsync` منتقل شد تا مجوز مؤثر و scope شرکت، بدون bypass مجوز، استفاده شود.
5. خطای persistence در Supplier sourcing ناشی از mapping مؤثر `SupplierProduct` بود: EF برای فیلدهای sourcing نام CLR مانند `Availability` تولید می‌کرد، درحالی‌که schema رسمی ستون snake_case (`availability`) دارد. configuration نهایی و snapshot/migration هم‌تراز شد.

### فایل‌ها و اصلاحات

- `src/BARANG.Core.Api/Lookups/LookupEndpoints.cs`: دامنه‌ها، گزینه‌ها، CRUD مقدار، editor/delete و adapterهای artwork/defect.
- `src/BARANG.Core.Application/Lookups/LookupContracts.cs` و `LookupAdministration.cs`: قراردادهای کامل، ترجمه، scope، parent/cycle، حفاظت رکوردهای سیستمی و بررسی ارجاع.
- `src/BARANG.Core.Infrastructure/Persistence/LookupStore.cs` و `DependencyInjection.cs`: actor/time، global-admin و registration administration.
- `src/BARANG.Core.Domain/Lookups/LookupModels.cs` و `Configurations/LookupConfigurations.cs`: نرمال‌سازی، parent و index/FKهای موجود.
- `src/BARANG.Core.Api/Organization/CompanyContextMiddleware.cs`: انتخاب شرکت با access مؤثر و حفظ authorization.
- `src/BARANG.Core.Infrastructure/Persistence/Configurations/SupplierSourcingConfigurations.cs`: mapping نهایی sourcing روی schema موجود.
- `20261012143435_SupplierSourcingMappingAlignment` و snapshot: migration فقط برای ثبت هم‌ترازی metadata است؛ `Up/Down` شیء business را حذف یا بازسازی نمی‌کند.
- fixtureهای `SupplierCoreApiTests`, `SupplierReferenceFoundationTests`, `SupplierSourcingApiTests`: اصلاح endpointهای obsolete ساخت شخص و sourcing و حفظ سناریوهای authorization/isolation.

### ماتریس پذیرش منوهای Base Data

فهرست دامنه‌ها از `DomainReferenceCatalog` با ۱۴ دامنه و ۴۳ نوع catalog تولید می‌شود؛ adapterهای artwork/defect جداگانه هستند. آزمون authenticated API با DTO واقعی API انجام شد:

| منوی Base Data | مسیر/خانواده | نتیجهٔ بارگذاری API | محدودیت فعلی |
|---|---|---:|---|
| مشترک | `/master/reference-data/shared` | ۱۴ دامنه، HTTP 200 در fa/en/ar | visual browser در این اجرا انجام نشد |
| مشتریان | `/master/reference-data/customers` | HTTP 200، دامنهٔ customer | visual browser pending |
| تأمین‌کنندگان | `/master/reference-data/suppliers` | HTTP 200، دامنه‌های supplier و fixtureهای supplier موفق | visual browser pending |
| محصولات | `/master/reference-data/products` | HTTP 200، دامنهٔ product | visual browser pending |
| بانک طراحی | `/master/reference-data/artwork` | HTTP 200؛ `artwork.tag` نیز HTTP 200 | Workflow/Designer خارج از این scope است |
| سفارش‌ها | `/master/reference-data/orders` | HTTP 200، دامنهٔ order | visual browser pending |
| انبار | `/master/reference-data/inventory` | HTTP 200، دامنهٔ inventory و unit options | reservation/consumption اجرا نشد |
| تجهیزات | `/master/reference-data/equipment` | HTTP 200، دامنهٔ equipment | visual browser pending |
| تولید | `/master/reference-data/production` | HTTP 200، دامنهٔ production | Workflow runtime خارج از scope است |
| کیفیت | `/master/reference-data/quality` | HTTP 200؛ `defect.type` نیز HTTP 200 | visual browser pending |
| لجستیک | `/master/reference-data/logistics` | HTTP 200، دامنهٔ logistics | visual browser pending |
| گردش‌کار | `/master/reference-data/workflow` | catalog endpoint عمومی HTTP 200 است | runtime/Designer کامل در پروژه موجود نیست |

مقادیر واقعی فهرست‌شده از جمله `price.group`, `customer.group`, انواع artwork، `print.method`، `unit.of.measure`، `packaging.type` و `shipping.carrier` از ID/code پایدار و caption ترجمه‌شده استفاده می‌کنند؛ گزینهٔ ساختگی برای رفع خطا اضافه نشد.

### شواهد آزمون و استقرار

- fresh isolated PostgreSQL (`barang_base_lookup_20261012_test`): زنجیرهٔ migration و `LookupFoundationTests` **۳/۳ موفق**.
- `SupplierCoreApiTests`: **۱۰/۱۰ موفق**؛ شش شکست قبلی به route/payload obsolete، company access و mapping `SupplierProduct` مربوط بودند.
- `SupplierReferenceFoundationTests` و `SupplierSourcingApiTests`: مجموعاً **۲۳/۲۳ موفق**، شامل lookup، scope، company isolation، role permission و sourcing.
- API و Backoffice Release build: هر دو با **۰ warning و ۰ error**.
- authenticated runtime API پس از deploy: دامنه‌ها در fa/en/ar، unit options، `artwork.tag` و `defect.type` همگی HTTP 200 و قابل parse هستند. login admin و انتخاب company نیز موفق است.
- imageهای جاری: API `sha256:2c09a75a487edd54ba84ec3cc9f5504221184eef51834d2f358dab1aa31c56fb` و Backoffice `sha256:71f3f1fed0d6f1715ef3b2ae4f5672a65b26db727559a939a5854c0c8361eab`؛ Compose از `step2-clean/01_CORE/compose.yaml` استفاده می‌کند.

Visual browser verification فرم‌های هر منو در این محیط انجام نشد؛ شواهد فعلی شامل build، persistence/API integration و authenticated deserialization-level پاسخ‌هاست. WorkflowDefinitionRuntime/Designer و Cartable کامل عمداً پیاده‌سازی نشده و این recovery آن را حل نمی‌کند.
