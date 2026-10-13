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

## بازیابی Workflow Designer و Cartable — اجرای فعلی

### پیاده‌سازی

در target checkout، قراردادهای واقعی endpointهای Designer و runtime به implementationهای پایگاه‌داده‌ای متصل شدند؛ source canonical dirty دست‌نخورده ماند. فایل‌های مرتبط عبارت‌اند از:

- `src/BARANG.Core.Application/Workflow/WorkflowDesignerContracts.cs`: command/view و interfaceهای Designer، runtime و saved-filter.
- `src/BARANG.Core.Infrastructure/Workflow/WorkflowDesignerAdministration.cs`: خواندن graph/version، ذخیرهٔ Draft با optimistic version، validation ساختاری و publish نسخهٔ Draft؛ نسخهٔ Active هنگام ویرایش mutate نمی‌شود.
- `src/BARANG.Core.Infrastructure/Workflow/WorkflowDefinitionRuntime.cs`: شروع idempotent بر اساس company+order، اتصال instance به DefinitionVersion، ایجاد task از graph، claim/release/complete با version، مسیر outcome در gateway، history، personal state و attachment reference/download.
- `src/BARANG.Core.Infrastructure/Workflow/CartableSavedFilterAdministration.cs`: CRUD فیلترهای شخصی با scope شرکت/کاربر و version.
- `src/BARANG.Core.Api/Workflow/WorkflowEndpoints.cs` و `src/BARANG.Core.Infrastructure/DependencyInjection.cs`: route و DI registration واقعی.
- `src/BARANG.Core.Infrastructure/Orders/OrderAdministration.cs`: مسیر Submit از runtime جدید استفاده می‌کند و start تکراری را idempotent نگه می‌دارد.
- `src/BARANG.Backoffice/Components/Pages/WorkflowWorkspace.razor`: دکمهٔ Publish برای نسخهٔ Draft و مسیرهای Save/Validate موجود.
- `tests/BARANG.Backoffice.E2ETests/WorkflowDesignerE2ETests.cs`: انتظار تعداد actionها با Publish/Undo/Redo جدید هم‌تراز شد.

### علت‌های runtime که در استقرار پیدا و اصلاح شدند

1. `set_config` مقدار UUID را به‌عنوان آرگومان uuid می‌فرستاد؛ PostgreSQL فقط text می‌پذیرد. هر سه implementation به `cast(@company as text)` اصلاح شدند؛ این علت 500 Cartable و شکست انتخاب context بود.
2. query graph در join transition ستون `version` را بدون qualifier می‌خواند؛ به `tr.version` اصلاح شد.
3. JsonElementهای nullable از `JsonDocument` پس از dispose در پاسخ نگه داشته می‌شدند؛ `Clone()` برای operator/title اضافه شد.
4. response transition فاقد `id` بود، درحالی‌که Backoffice آن را می‌خواند؛ شناسهٔ واقعی `tr.id` اکنون برمی‌گردد.

### شواهد پذیرش

| عمل | نتیجهٔ فعلی | شواهد |
|---|---|---|
| Designer load/version | موفق | پاسخ authenticated با status `Active`، نسخهٔ 17، 23 node و 41 transition |
| Designer validate | موفق | E2E deployed بعد از fix: validation action بدون circuit error |
| Save Draft/reload graph | موفق | E2E deployed؛ graph و connection پس از reload باقی ماند |
| Cartable list/detail shell | موفق در API و UI route | runtime task/referrals هر دو 200 با company context؛ E2E محلی Cartable در fa/en/ar و navigation: 4/4 موفق |
| claim/release/complete و gateway routing | implementation موجود، acceptance با رکورد isolated | تست مستقیم mutation در test host به‌علت failure قدیمی create order اجرا نشد |
| Order Submit → exactly-one workflow | implementation idempotent، runtime test blocked | OrderE2E و PricingE2E روی DB ایزوله در create order با core.unexpected شکست خوردند؛ این failure خارج از graph runtime است و هنوز رفع نشده |
| attachment/saved filter | persistence path موجود | endpoint code review و schema موجود؛ browser/API mutation end-to-end هنوز pending |
| delegation/forward/substitution | تکمیل نشده | implementation فعلاً خطای دقیق `workflow.forward.not_available`/`workflow.substitution.not_available` برمی‌گرداند و success جعلی ندارد |

### تست و deployment این نوبت

- API و Backoffice Release build: موفق، صفر warning/error.
- PostgreSQL ایزولهٔ `barang_workflow_test`: full migration chain اجرا شد؛ `WorkflowFoundationTests` برابر 3/3 موفق.
- Browser E2E با Playwright روی `http://localhost:5100` و admin محلی، با اجازهٔ صریح shared development DB: تست‌های Designer/Cartable برابر **6/6 موفق** پس از اصلاح parser و انتظار تست. این تست‌ها save draft را روی محیط توسعه اجرا کردند؛ backup قبلی حفظ شده و migration جدیدی اعمال نشد.
- imageهای جاری از آخرین deploy این checkout: API manifest `sha256:a566d719613a95d88b1015b5faca25c0e368d7bbeb04d0183cdd2cce2d79da5a` و Backoffice manifest `sha256:43ffc4574e43bae50c98992e4ac4609c1e4e0f24ec9d76bb2572c13c1e5c35ac`. هر دو از `step2-clean/01_CORE/compose.yaml` اجرا شدند و API healthy است.
- browser automation تصویری مستقل در سطح tool در دسترس نبود؛ Playwright headless واقعی و component loading استفاده شد. بنابراین مشاهدهٔ دستی screenshot/visual layout همچنان جداگانه pending است.

### باقی‌مانده و تصمیم وضعیت

این پیاده‌سازی Designer load/validate/save/reload و هستهٔ runtime/Cartable را از contract تا SQL و API متصل می‌کند، اما recovery کامل baseline هنوز قابل اعلام نیست: forward/delegation/substitution success path، workload/history/assignment filtering کامل، attachment mutation و Order Submit end-to-end نیازمند تست/کار بیشتر هستند. همچنین 3 integration test سفارش/قیمت در setup ایزوله با `POST /orders` و یک تست BusinessProfile با ستون قدیمی `equipment.AcquisitionMethodCode` شکست خوردند؛ این موارد پیش از اعلام resume امن STEP 2 باید جداگانه رفع یا با شواهد به baseline مالک خود نسبت داده شوند. هیچ Inventory/Finance/Quality/Logistics integration جدیدی در این کار فعال نشده است.

Visual browser verification فرم‌های هر منو در این محیط انجام نشد؛ شواهد فعلی شامل build، persistence/API integration و authenticated deserialization-level پاسخ‌هاست. WorkflowDefinitionRuntime/Designer و Cartable کامل عمداً پیاده‌سازی نشده و این recovery آن را حل نمی‌کند.

### اصلاحات تکمیلی این نوبت

- علت دقیق خطای POST سفارش/قیمت مشخص شد: `WorkflowDefinitionRuntime.CmdTx` tupleهای strongly-typed را به پارامتر SQL تبدیل نمی‌کرد؛ PostgreSQL نام‌هایی مثل `company` را ستون می‌دید. helper اکنون هر `ITuple` دوبخشی را به‌درستی bind می‌کند.
- runtime اتصال EF-owned را dispose نمی‌کند؛ پیش‌تر `GetDbConnection()` در Submit اتصال context را می‌بست و پاسخ نهایی سفارش با `ObjectDisposedException` شکست می‌خورد.
- نبودن definition سفارش در test baseline با یک definition/version معتبر و حداقلی پوشش داده شد؛ در صورت نبود definition در شرکت، runtime آن را یک‌بار با کد canonical `BARANG.ORDER` ایجاد و publish می‌کند و start همچنان بر اساس `company + order` idempotent است.
- `EquipmentExtendedConfiguration` ستون‌های جزئیات تجهیزات را به ستون‌های موجود snake_case نگاشت می‌کند. migration metadata-only `20261013070735_EquipmentMappingMetadataAlignment` snapshot را با schema موجود هم‌تراز می‌کند و عملیات rename/drop ندارد.
- `EfTransactionExecutor` rollback را در transactionهای تکمیل‌شده امن می‌کند تا خطای اصلی پنهان نشود.
- Cartable runtime اکنون forward هدف‌مند، substitutionهای دارای بازه/هم‌پوشانی/عضویت شرکت، فهرست کاربران واجد شرایط، فیلتر assignment، workload و history را از persistence واقعی می‌خواند. forward برای attribution از `claimed_on_behalf_of_user_id` و history استفاده می‌کند.

نتیجهٔ تست ایزولهٔ جاری: `OrderE2ETests`، `PricingE2ETests` و `BusinessProfileApiTests` برابر **3/3 موفق**؛ E2E Designer/Cartable برابر **6/6 موفق**. isolation در تست‌های فعلی با پاسخ `404 NotFound` برای دادهٔ شرکت دیگر (به‌جای `403`) تأیید می‌شود و داده نمایش داده نمی‌شود.

موارد هنوز خارج از پذیرش کامل: mutation حذف attachment endpoint مستقل ندارد و باید پیش از اعلام baseline کامل، با policy مالکیت/retention طراحی و تست شود؛ delegation مستقل از forward نیز در قرارداد فعلی endpoint جداگانه ندارد. این موارد در این نوبت به success جعلی تبدیل نشده‌اند.

### تصحیح شواهد نهایی این نوبت

بخش‌های قدیمی‌تر این گزارش که شکست Order/Pricing و تجهیزات را ثبت کرده‌اند، با اجرای اصلاحات این نوبت supersede می‌شوند. نتیجهٔ نهایی روی checkout فعلی:

- `OrderE2ETests`، `PricingE2ETests` و `BusinessProfileApiTests`: **۳/۳ موفق** روی PostgreSQL ایزوله؛ شامل ایجاد سفارش، اقلام/چاپ، قیمت‌گذاری و نگاشت تجهیزات.
- `WorkflowFoundationTests`: **۳/۳ موفق**؛ ایجاد تعریف پیش‌فرض سفارش فقط در شرکتی که تعریف ندارد انجام می‌شود و start بر اساس order idempotent است.
- unit tests: **۲۲۲/۲۲۲ موفق**.
- Playwright headless پس از rebuild بدون cache: Designer/Cartable **۶/۶ موفق** (load/validate/save/reload و ناوبری Cartable در fa/en/ar). build مجدد کل پروژهٔ E2E به دلیل خطای analyzer قدیمی در `SupplierLifecycleE2ETests.criterionLabel` انجام نشد؛ باینری Release موجود با `--no-build` اجرا و موفق شد.
- migration metadata-only تجهیزات پس از backup توسعه اعمال شد؛ migration chain تازه و database توسعه بدون drop/rename و بدون حذف داده عبور کردند. backup تأییدشده در `recovery-snapshots/20261013104656/barang_core_pre_recovery.dump` خارج از commit نگه داشته شد.
- ایمیج‌های واقعاً در حال اجرا از rebuild نهایی: API `sha256:d5ad3c5f1d8b1c04a8e9212711edf8ee56e0562a3ac92903f8a06fe2841c194a` و Backoffice `sha256:b741d22f41c9e665d8183141d03b734fb97b8b96981ab4b4597577abff0ed8cd`.

هنوز نباید recovery را کاملاً تمام‌شده تلقی کرد: endpoint مستقل delegation و حذف attachment در قرارداد فعلی وجود ندارند؛ این دو capability و تست mutation مستقل باید با policy مالکیت/retention مصوب تکمیل شوند. قابلیت forward و substitution موجود در قرارداد فعلی پیاده‌سازی و build شده‌اند، ولی برای mutation end-to-end مستقل، fixture/تست اختصاصی لازم است.

### بستن scope و شواهد نهایی baseline

- `SupplierLifecycleE2ETests.criterionLabel` حذف شد چون فقط مقداردهی می‌شد و در هیچ assertion یا مسیر UI مصرف نمی‌شد. build source با analyzerهای فعال انجام شد؛ خطای این فیلد دیگر وجود ندارد. پروژهٔ E2E هنوز warningهای مستقل قدیمی مانند CA1707/CA1305/CA18xx دارد که در این کار suppress یا غیرفعال نشدند. اجرای acceptance با `TreatWarningsAsErrors=false` صرفاً برای عبور از این warningهای خارج از تغییر انجام شد و خود تست‌ها disabled نشدند.
- تعریف `BARANG.ORDER` دیگر در runtime به‌صورت خودکار ایجاد نمی‌شود. نبود definition یا active version اکنون `workflow.definition.no_published_version` برمی‌گرداند؛ definition موجود و version انتخاب‌شده هرگز جایگزین نمی‌شوند. fixtureهای Order/Pricing یک definition منتشرشدهٔ صریح و کامل برای تست isolated seed می‌کنند و RLS company context را تنظیم می‌کنند.
- مرور UI/API/قرارداد نشان داد قابلیت مستقل delegation و حذف attachment ثبت‌شده وجود ندارد. UI فقط forward و substitution را expose می‌کند؛ حذف موقت upload با `/discard` بخشی از composer است و deletion تاریخی نیست. این دو مورد future scope هستند، نه blocker recovery فعلی؛ هیچ control نمایشی با `not_available` باقی نمانده است.
- اصلاح prerender Cartable: `DisposeAsync` فقط پس از موفقیت initialization جاوااسکریپت dispose را فراخوانی می‌کند؛ خطای `JavaScript interop calls cannot be issued at this time` دیگر از این مسیر ایجاد نمی‌شود.
- شواهد جاری: Order/Pricing/Workflow integration **۵/۵**، unit **۲۲۲/۲۲۲**، Designer/Cartable E2E source **۶ تست موفق از ۸ تست انتخاب‌شده**؛ دو تست SupplierLifecycle در setup browser به timeout ناوبری login رسیدند، در حالی‌که API login و `auth/me/company` با ۲۰۰ ثبت شدند و علت آن در assertion/fixture ناوبری تست باقی است. این limitation مستقل از `criterionLabel` است و به‌عنوان تست ناموفق پنهان نشده است.

بنابراین baseline recovery برای قابلیت‌های موجود و تأییدشده بسته می‌شود؛ delegation مستقل و deletion تاریخی attachment به‌عنوان scope تأییدنشدهٔ آینده باقی می‌مانند. STEP 2 identity/UOM هنوز موضوع جداگانه است و فقط acceptanceهای قبلی آن که در گزارش ثبت شده‌اند معتبرند؛ تصمیم‌های reservation/consumption و سایر integrationهای STEP 3 به بعد همچنان برای business confirmation باقی هستند.

### شواهد نهایی verification ــ ۱۳ اکتبر ۲۰۲۶

این بخش، ادعاهای قدیمی‌تر دربارهٔ warningهای analyzer و timeout ورود SupplierLifecycle را supersede می‌کند:

- `BARANG.Backoffice.E2ETests.csproj` از سورس فعلی با `TreatWarningsAsErrors=true` build شد: **۰ warning / ۰ error**. CA1707، CA1305، CA18xx و xUnit diagnostics با اصلاح نام‌گذاری تست، culture صریح، allocationهای ثابت، APIهای جدید analyzer و cancellation token رفع شدند؛ analyzer غیرفعال یا suppress نشد.
- علت timeout مرورگر Login در SupplierLifecycle، آماده‌نبودن hydration قبل از fill/click و return URL قدیمی `/login` بود؛ session API مشکل نداشت. تست اکنون پس از hydration به `/master/suppliers` می‌رود و company selection واقعی را دنبال می‌کند.
- علت runtime منوی عملیات Supplier، حذف شدن implementationهای `barangPopupManager` و `barangActionMenu` از `src/BARANG.Backoffice/wwwroot/app.js` در checkout هدف بود، درحالی‌که `BarangActionMenu.razor` به آن‌ها وابسته است. implementation واقعی از checkout canonical به‌صورت انتخابی بازیابی شد؛ شامل outside-click، Escape/focus restoration، portal، anchoring و scroll/resize repositioning.
- چهار View action در `SupplierLifecycleWorkspace.razor` به‌دلیل markup ناقص به‌صورت متن خام render می‌شدند. هر چهار مورد به `BarangIconButton` واقعی اصلاح شد. این اصلاح، View/Edit/Download action-menu را با UI فعلی هم‌تراز کرد.
- caption واقعی دکمه‌های ایجاد در lifecycle، طبق shared standard، `Add` است؛ انتظار قدیمی `Add evaluation`/`Add incident` در تست به scope هر lifecycle اصلاح شد. assertionهای CRUD، status، reload، file upload/download و company isolation حفظ شدند؛ پاسخ شرکت غیرمجاز در قرارداد فعلی **403** است.
- `SupplierLifecycleE2ETests`: **۲/۲ موفق** از build و binary تازهٔ همان commit؛ هر دو سناریو به صفحهٔ authenticated رسیدند و assertionهای اصلی را کامل کردند.
- `BARANG.Core.UnitTests`: **۲۲۲/۲۲۲ موفق**.
- `BARANG.Backoffice.Tests`: **۱۷۰/۱۸۴ موفق**؛ ۱۴ failure باقی‌مانده source-contract/design هستند و به فایل‌های Cartable/مستندات/assetهای غایب در checkout هدف ارجاع می‌دهند، از جمله `CartablePersonalStateConfiguration.cs`، `OrderMessageAdministration.cs`، `D:\00_BARANG\step2-clean\04_Document\...` و assertionهای قدیمی متن source. این failureها suppress یا حذف نشدند و baseline کلی را هنوز برای پذیرش کامل محدود می‌کنند.
- `BARANG.Core.IntegrationTests`: اجرا شد اما بدون `BARANG_TEST_DB_CONNECTION` به isolated PostgreSQL، fixture عمداً با خطای صریح متوقف شد؛ هیچ تستی علیه development business DB اجرا نشد. بنابراین این نوبت ادعای integration persistence جدید ندارد.
- چون `wwwroot/app.js` و `SupplierLifecycleWorkspace.razor` تغییر application بودند، فقط Backoffice rebuild/deploy شد. image در حال اجرا: Backoffice manifest `sha256:6c811ce327909ba1fb2e2fa7e504f7a27a5952a03412f7018677308686350ef2`; API بدون تغییر باقی ماند. هر دو container healthy هستند.

نتیجه: دو blocker مشخص این نوبت (analyzer build و browser Login/SupplierLifecycle) رفع و با شواهد واقعی تأیید شدند؛ اما به‌علت ۱۴ شکست مستقل `Backoffice.Tests` و نبود connection string برای integration suite، baseline recovery را «کاملاً پذیرفته‌شده» اعلام نمی‌کنم. این محدودیت‌ها باید پیش از resume رسمی STEP 2 تعیین تکلیف شوند.
