# بازبینی آمادگی STEP 3 — BARANG

تاریخ بازبینی: ۱۴۰۵/۰۷/۲۲ برابر با ۲۰۲۶-۱۰-۱۴

## نتیجهٔ اجرایی

**STEP 3: CONDITIONAL GO**

کد و schema فعلیِ checkout reviewed از نظر build، unit، Backoffice و integration ایزوله سبز است. blockerهای deployment/browser رفع شدند: API و Backoffice از یک checkout و commit کامل ساخته و با provenance قابل‌مشاهده deploy شدند و چهار browser check اکنون سبز هستند. وضعیت همچنان **Conditional** است چون تصمیم‌های business مربوط به reservation در بخش جداگانه هنوز تصویب نشده‌اند؛ این گزارش آن‌ها را تأیید یا پیاده‌سازی نمی‌کند.

خطای `helper_unknown_error: setup refresh had errors` در این بازبینی مشاهده نشد.

## بررسی read-only محیط

| بررسی | نتیجه |
|---|---|
| دسترسی به `D:\00_BARANG` | موفق |
| دسترسی به `D:\00_BARANG\step2-clean\01_CORE` | موفق |
| Git | `git version 2.55.0.windows.3` |
| .NET | SDK `10.0.400` |
| checkout reviewed | شاخه `codex/step2-inventory-unit-contracts`، commit نهایی `618a34c91a098a73d09d04f01993be120dd2c604` |
| working tree checkout | فقط `src/BARANG.Backoffice/.keys/` به‌صورت untracked؛ تغییرات این review فقط commitهای مستندشده هستند |
| repository root | `D:\00_BARANG`، شاخه `main`، commit `09afbe3a60ba7e12bdb2e01edf5228e9f5269a37` و از قبل بسیار dirty؛ دست‌نخورده ماند |
| branch relation | پس از push، branch remote با commit `618a34c...` هم‌راستا می‌شود؛ branch از main جلوتر و ۲ commit عقب‌تر است |

## منابع بازبینی‌شده

این موارد کامل خوانده و با source/tests تطبیق داده شدند:

- `docs/ORDER_LIFECYCLE_INTEGRATION_CONTRACT_FA.md`
- `docs/INTEGRATION_IMPLEMENTATION_PLAN_FA.md`
- `docs/REGRESSION_RECOVERY_REPORT_FA.md`
- migrationهای EF، مدل‌های Order/Inventory/Purchasing، API و Backoffice، و تست‌های جاری checkout.

گزارش recovery صراحتاً browser visual/circuit و بخشی از Workflow/Cartable و endpointهای generationهای مختلف را در زمان خود pending یا blocker اعلام کرده است؛ نتایج تاریخی آن برای جایگزینی verification جاری استفاده نشد.

## تست‌های جاری و failureهای دقیق

### Build

پس از اجرای sequential برای جلوگیری از file-lock ناشی از build موازی:

- `dotnet build BARANG.Core.slnx --configuration Release --no-restore`: **موفق، ۰ warning، ۰ error**.
- `dotnet build BARANG.Backoffice.slnx --configuration Release --no-restore`: **موفق، ۰ warning، ۰ error**.

یک اجرای موازی اولیه با `MSB3554` روی فایل‌های `.resources` fail شد؛ علت file-lock هم‌زمان بین دو build بود و اجرای sequential آن را بازتولید نکرد.

### Unit و Backoffice

- Core solution: Architecture **۴/۴** و Unit **۲۲۲/۲۲۲ موفق**.
- Backoffice tests: **۱۸۴/۱۸۴ موفق**.

### PostgreSQL integration ایزوله

database جدید و ایزولهٔ `barang_step3_review_20261014_test` ساخته شد و fixture آن full migration chain را اعمال و reset کرد. `barang_core` target تست/reset نبود.

- `BARANG.Core.IntegrationTests`: **۱۲۶/۱۲۶ موفق**.
- اجرای قبلی بدون `BARANG_TEST_DB_CONNECTION`: **۱۰۶ failure از ۱۲۶**، همگی با علت مشترک `Set BARANG_TEST_DB_CONNECTION to an isolated PostgreSQL test database` در `PostgreSqlDatabaseFixture.InitializeAsync:22`. این failure پیکربندی تست بود و با اجرای واقعی روی DB ایزوله رفع شد.

### Browser E2E — failure اولیه و rerun پس از رفع blocker

فقط سناریوهای read-only مربوط به login، shell، زبان/RTL و responsive اجرا شدند؛ سناریوهای create/edit روی دادهٔ توسعه اجرا نشدند.

- failure اولیهٔ `GlobalDesignSystemShellUsesLoginPaletteAndRemainsCompactResponsive`: timeout ۳۰ ثانیه در `Locator("a.primary-link")`; source جاری روی Home از `a.home-shortcut` استفاده می‌کند. علت: stale test expectation، نه authentication/company failure.
- failure اولیهٔ `GlobalLanguageSwitchUpdatesLocalizedShellAndDirection(en)`: انتظار `Dashboard`، مقدار واقعی `Hello, پیمان خوشه چین`.
- همان تست برای `ar`: انتظار `لوحة المعلومات`، مقدار واقعی `مرحباً، پیمان خوشه چین`.
- همان تست برای `fa`: انتظار `داشبورد`، مقدار واقعی `سلام، پیمان خوشه چین`.
- بررسی source نشان داد `Home.razor` contract فعلی greeting شخصی‌سازی‌شده دارد؛ بنابراین انتظار Dashboard stale بود. assertionها به greeting محلی‌شدهٔ non-empty و `home-shortcut` هم‌تراز شدند، بدون حذف assertionهای direction، language، shell، responsive یا no-overflow.
- URL هر چهار تست: `http://localhost:5100/culture?culture={fa|ar|en}&returnUrl=%2F`.
- evidence تصویری/trace پس از deploy معتبر: `D:\00_BARANG\step2-clean\artifacts\step3-readiness-browser\shell.png/.trace.zip` و فایل‌های `culture-fa|ar|en.png/.trace.zip`. این تست diagnostics فقط read-only است.
- rerun پس از deploy coherent: `GlobalDesignSystemShell...` **موفق**؛ `GlobalLanguageSwitch...` برای fa/ar/en **۳/۳ موفق**؛ مجموع **۴/۴ موفق**.

در اجرای قبلی E2E بدون target صریح نیز runner با `Test process did not return valid JSON (non-object)` و `No test is available` برگشت؛ این نتیجه جایگزین اجرای جاری محسوب نشد.

## source در برابر deployed API/Backoffice و migration

### source reviewed

- branch/HEAD: `codex/step2-inventory-unit-contracts` / `618a34c91a098a73d09d04f01993be120dd2c604`.
- branch remote با همین HEAD موجود است.
- source شامل migration `20261012100553_Step2InventoryUnitContracts` و migrationهای بعدی تا `20261013070735_EquipmentMappingMetadataAlignment` است.

### deployment تأییدشدهٔ فعلی

- Compose project: `barang-core`; context/config: `D:\00_BARANG\step2-clean\01_CORE\compose.yaml`.
- API: `barang-core-api-1`, image `sha256:a420f95884743be4ff526d573ecdfec575236607535c77f2c4ce78ccb4d515ef`, build `2026-10-14T08:05:50Z`.
- Backoffice: `barang-core-backoffice-1`, image `sha256:298c84de7da08716a5f40b7205af94e1aff2531d82701999eba006da2562eee9`, build `2026-10-14T08:05:22Z`.
- هر دو image دارای `BARANG_BUILD_COMMIT=618a34c91a098a73d09d04f01993be120dd2c604` هستند و همین SHA در log startup هر دو سرویس ثبت شده است.
- API روی `127.0.0.1:8080` و Backoffice روی `0.0.0.0:5100` منتشر است؛ Backoffice از `CoreApi__BaseUrl=http://api:8080` استفاده می‌کند.
- PostgreSQL/Redis/MinIO و volumeهای `barang-core_barang_postgres_data`، `barang-core_barang_api_keys` و `barang-core_barang_backoffice_keys` حفظ شدند؛ فقط API و Backoffice با `up -d --no-deps` recreate شدند.
- API `/health/ready` و `/health`: HTTP 200 / `Healthy`؛ Backoffice `/login`: HTTP 200. این فقط readiness است، نه پذیرش UI.
- migration history توسعه، read-only، تا `20261013070735_EquipmentMappingMetadataAlignment` است و شامل `20261012100553_Step2InventoryUnitContracts` و `20261012115004_AlignTariffCompatibilityColumns` است.

نتیجهٔ تطبیق: source، imageها، startup logs، context Compose، address داخلی API و browser runtime اکنون هم‌راستا و قابل‌اثبات هستند. migration history توسعه read-only همچنان تا `20261013070735_EquipmentMappingMetadataAlignment` است؛ دادهٔ توسعه reset نشد و شمارش جاری `orders=60` و `order_items=65` باقی ماند.

## بررسی رفتار حوزه‌ها

| حوزه | شواهد جاری | نتیجه |
|---|---|---|
| Base Data و Lookup | `LookupFoundationTests` در suite integration؛ API/authorization/RLS و catalogهای جاری | API/persistence سبز؛ browser readiness pair سبز |
| Login/Admin و Customer/Supplier | `IdentityAndRbacTests`، `UserAccessManagementTests`، `CustomerNormalizedApiTests`، `SupplierCoreApiTests` و Backoffice suite | source/test host و browser login/shell سبز |
| Design Bank | `ArtworkFileFoundationTests` و `ArtworkFileE2ETests` | API/persistence ایزوله سبز؛ browser visual/modal CRUD جاری تأیید نشد |
| Order/Print/Pricing/Submit | `OrderFoundationTests`، `OrderE2ETests`، `PricingFoundationTests`، `PricingE2ETests` و business-profile tests | current isolated suite سبز؛ browser blocker deployment رفع شد |
| Workflow/Cartable | `WorkflowFoundationTests` و unit workflow tests | foundation/schema/auth سبز؛ owner-service completion، full graph journey و deployed Cartable browser هنوز evidence کافی ندارد |
| Inventory/Purchasing | `InventoryFoundationTests` و تست‌های sourcing/purchase-related در suite | identity/unit/schema و API foundation سبز؛ reservation lifecycle و shortage-to-purchasing end-to-end هنوز تصمیم/پذیرش نشده |

## پذیرش baseline recovery

**قبول baseline recovery: بله، با شرط تصمیم‌های business STEP 3.**

بخش source recovery فعلی قابل‌ساخت و قابل‌آزمون است و تست‌های ایزوله و browser readiness جاری سبز هستند. شرط باقی‌مانده خارج از deployment/browser، تصویب تصمیم‌های reservation است:

1. deployment pair اکنون به commit reviewed قابل انتساب است؛
2. browser E2E پایه اکنون ۴/۴ سبز است؛
3. trace/screenshot واقعی از همین deployment ثبت شده است؛
4. فقط health/HTTP 200 مبنای پذیرش نیست و به‌همراه browser و test evidence استفاده شد.

## وضعیت STEP 2 — هویت و UOM

### معیارهای جداگانه

- **Exact `OrderItemId`:** در `StockReservation` نگهداری می‌شود و unit test جاری distinct بودن دو item و persistence مقدار را اثبات می‌کند.
- **Company/Warehouse/Variant identity:** مدل و migration ستون‌های company، warehouse و product variant را نگه می‌دارند؛ integration suite ایزولاسیون شرکت، FK/شناسه و RLS را سبز کرده است.
- **Unit persistence:** `uom_code` در reservation، purchase request item و purchase order item اضافه/پر شده و در migration `20261012100553_Step2InventoryUnitContracts` non-null می‌شود؛ snapshot محصول نیز `snapshot_uom_code` دارد.
- **Mismatch rejection:** domain/API validation واحد canonical محصول را الزام می‌کند؛ unit/integration coverage جاری سبز است و conversion ضمنی ایجاد نشده است.
- **Schema consistency:** fresh isolated DB با full chain migrate شد و ۱۲۶ integration سبز شد؛ توسعه نیز migration history شامل migration STEP 2 است.

### نتیجهٔ STEP 2

از نظر source، contract، migration و verification ایزوله: **تکمیل‌شده**.

از نظر operational acceptance روی deployment تأییدشده: **تکمیل‌شده**؛ image provenance و browser baseline تأیید شدند. این نتیجه به معنی تصویب تصمیم‌های reservation STEP 3 نیست.

## تصمیم‌های باز STEP 3

برای شروع STEP 3، بدون الزام به پیاده‌سازی integrationهای later phase، این تصمیم‌های business/contract باید صریحاً ثبت شوند:

1. نقطهٔ رسمی reservation: در Submit یا task مستقل `InventoryReservation`؛ پیشنهاد contract فعلی task مستقل است.
2. reservation کامل اجباری است یا partial reserve مجاز است؛ shortage باید structured و بدون oversell/negative availability باشد.
3. `UomCode` در reservation immutable snapshot بماند و تغییر master بعدی روی historical reservation اثر نگذارد.
4. idempotency scope و retry semantics برای هر `OrderItemId`، شرکت، warehouse و variant تعیین شود.
5. warehouse inactive، variant mismatch، company mismatch و تغییر quantity بعد از reservation دقیقاً چه error/transitionی دارند.
6. release در cancel/reject/expiry و تغییر quantity چگونه انجام شود؛ پس از consume، اصلاح از مسیر return جدا باشد.
7. consume در چه issue point و با چه evidence مالکانه انجام شود؛ STEP 3 نباید production/QC/logistics integration را جعل یا implicitly activate کند.
8. shortage فقط record شود یا پیشنهاد/درخواست خرید دستی و approval داشته باشد؛ auto-PO فعلاً مجاز نیست.
9. اتصال workflow به owner service باید idempotent و evidence-based باشد؛ graph transition تنها بعد از موفقیت owner command مجاز است.

## GitHub و merge status

- `origin/codex/step2-inventory-unit-contracts` پس از push باید به HEAD `618a34c91a098a73d09d04f01993be120dd2c604` اشاره کند.
- `origin/main` روی `09afbe3...` است؛ branch برابر ۲۹ commit جلو و ۲ commit عقب است.
- remote: `https://github.com/peymanalgo/BARANG_ALL.git`.
- `gh` نصب نیست و token احراز‌شدهٔ GitHub در محیط موجود نبود؛ Git refs نشان می‌دهند branch remote وجود دارد و merge به main انجام نشده/قابل ادعا نیست. API unauthenticated قبلی 404 داده بود، اما آن پاسخ به‌تنهایی proof نیست. **Merge status: از refs قابل‌تأیید نیست؛ merged فرض نمی‌شود.**

## محدودیت و اقدام بعدی مجاز

منطق business application و operational data/migration تغییر نکرد. دو Dockerfile برای provenance، assertionهای stale E2E و تست diagnostics evidence تغییر کردند. deployment فقط pair محلی API/Backoffice را با volumeهای موجود recreate کرد؛ credentialها حفظ شدند. database ایزولهٔ `barang_step3_review_20261014_test` برای تست استفاده شد.

برای تبدیل CONDITIONAL GO به GO، فقط تصمیم‌های بخش STEP 3 باید توسط مالک business تصویب و ثبت شوند؛ هیچ reservation integration جدیدی در این کار پیاده‌سازی نشد.

## الحاقیه اجرای STEP 3 — وضعیت جاری این checkout

### پیاده‌سازی واقعی

- `InventoryAdministration.ReserveOrderAsync` و `ReserveOrderInsideTransactionAsync` به implementation canonical reservation اضافه شدند؛ سیستم موازی ساخته نشد.
- `WorkflowDefinitionRuntime` در node دارای کلید `InventoryReservation` ابتدا warehouse و idempotency را از command می‌خواند، سپس همان سرویس را داخل transaction جاری اجرا می‌کند و فقط پس از موفقیت task را complete/advance می‌کند.
- demand برای stock keyهای همسان تجمیع و lockها به ترتیب variant انجام می‌شوند؛ reservationهای distinct `OrderItemId` جدا هستند. کمبود با `inventory.reservation.shortage` و شرح ساختاریافتهٔ required/available/missing/UOM برمی‌گردد و هیچ row جدیدی ایجاد نمی‌شود.
- retry با کلید durable به reservationهای قبلی بازمی‌گردد؛ warehouse، quantity یا variant ناسازگار با `inventory.reservation.request_mismatch` رد می‌شود. physical stock در reservation تغییر نمی‌کند.
- Backoffice Cartable در stage رزرو، warehouse فعال را از API همان شرکت نشان می‌دهد و command را با loading/duplicate prevention موجود ارسال می‌کند؛ خطای shortage task را باز نگه می‌دارد. Order 360 از همان reservation read model استفاده می‌کند.
- migration جدید اضافه نشد؛ زنجیرهٔ schema فعلی و UOM/identity STEP 2 بدون تغییر باقی ماند.

### بررسی و تست جاری

این نتایج مربوط به source جاری است، نه baseline تاریخی: branch `codex/step2-inventory-unit-contracts` در شروع روی `a35017aae256e5617768dd65725942673d92b4d0` بود و working tree فقط `.keys` نامرتبط و تغییرات این implementation را داشت. Release buildهای API و Backoffice هر دو ۰ warning/۰ error شدند؛ Unit **۲۲۲/۲۲۲** و Backoffice **۱۸۴/۱۸۴** سبز شدند. Integration با `BARANG_TEST_DB_CONNECTION` صریح روی `barang_step3_review_20261014_test` اجرا شد و **۱۲۶/۱۲۶** سبز شد؛ مدت اجرا ۳:۰۸ بود. اجرای بدون متغیر اتصال که پیش‌تر ۱۰۶ failure داده بود، failure پیکربندی محسوب می‌شود و جایگزین این نتیجه نیست.

### Backup، migration و deployment

قبل از تغییر source، backup موجود recovery بررسی شد: archive `D:\00_BARANG\barang_core_recovery_stopplan_20261012162450.backup` با `pg_restore --list` و ۱۷۲۵ TOC entry معتبر بود. این implementation migration جدید ندارد؛ plan recovery همان restore به DB ایزوله و اجرای full chain تا `20261013070735_EquipmentMappingMetadataAlignment` است. deployment توسعه باید فقط با compose project `barang-core`، volumeهای موجود و یک pair واحد API/Backoffice انجام شود؛ `barang_core` reset نمی‌شود.

### تصمیم‌های باز

مصرف فیزیکی، release در cancellation/reject/expiry، change بعد از consume، اتصال shortage به procurement، و gateهای production/QC/logistics عمداً STEP 4 یا later phase هستند و در STEP 3 تأیید نشده‌اند. تا اجرای browser authenticated روی دادهٔ ایزوله و ثبت digest/provenance pair جدید، نتیجهٔ STEP 3 **CONDITIONAL GO** است.

## شواهد نهایی deployment و browser

- commit کامل source/deployed: `810b9c089cb2f2357c3e28cb31603a0009cfc5ba`؛ image API: `sha256:7aa15cc8f60c634dbf9ab4f0cbdad02bcc11df75335fe6636a9182486bf67e38`؛ image Backoffice: `sha256:c1e051f3c4591e08439dcb322e615a53fd42f28c5e2c849935b6258092990961`. هر دو container environment و startup log همین SHA را نشان دادند.
- Compose project/context: `barang-core` / `D:\00_BARANG\step2-clean\01_CORE\compose.yaml`. API واقعی `barang-core-api-1` روی `127.0.0.1:8080` و Backoffice واقعی `barang-core-backoffice-1` روی `0.0.0.0:5100` است؛ Backoffice به `http://api:8080` متصل است. postgres/redis/minio سالم و volumeهای موجود حفظ شدند.
- `GET /health/ready` API و `/login` Backoffice هر دو HTTP 200/healthy بودند. هیچ migration جدیدی اعمال نشد و history توسعه تا `20261013070735_EquipmentMappingMetadataAlignment` باقی ماند.
- browser readiness با `BARANG_E2E_BASE_URL=http://localhost:5100` و Chrome سیستم اجرا شد: shell **موفق**؛ culture برای fa، ar و en **۳/۳ موفق**؛ مجموع **۴/۴ موفق**. screenshot و traceهای هر چهار سناریو در `D:\00_BARANG\step2-clean\artifacts\step3-readiness-browser` ثبت شدند. اجرای اول با Chromium bundle ناموجود ۴ failure محیطی داشت؛ CDN آن HTTP 403 منطقه‌ای داد، اما اجرای Chrome path همان تست‌ها را سبز کرد.
- baseline regression gate پس از implementation: API/Backoffice Release build **۰ warning/۰ error**، Unit **۲۲۲/۲۲۲**، Backoffice **۱۸۴/۱۸۴**، isolated PostgreSQL integration **۱۲۶/۱۲۶**. هیچ assertion تضعیف نشد.

### نتیجهٔ پذیرش STEP 3

**CONDITIONAL GO** برای reservation اتمیک مصوب: مسیر actual workflow، rollback اتمیک، deterministic locking، retry identity، انتخاب warehouse و UI failure behavior پیاده شده و baseline regression ندارد. تأیید GO نهایی به acceptance authenticated داده‌محور روی رکوردهای کاملاً ایزوله برای success/shortage/retry نیاز دارد؛ برای جلوگیری از mutation رکوردهای موجود، این سناریو در DB توسعه اجرا نشد.

### اصلاح provenance نهایی پس از تکمیل نمایش shortage

آخرین commit و pair واقعاً running اکنون `cfc8e786e97d99b42172b1e776a195ab513929d6` است. digest نهایی API `sha256:ac02d5b4d5037b29b317c0d99fafea582885f6a2a6918a2511f5b8e07de39ccd` و Backoffice `sha256:67b578bac309a23636ed165f83c2d97404a212656c22e72d61109b50a710c059` است؛ هر دو `BARANG_BUILD_COMMIT` یکسان دارند و هر دو healthy هستند. browser gate دوباره روی همین pair اجرا شد و **۴/۴ موفق** ماند. نسخهٔ قبلی `810b9c...` فقط provenance intermediate بود و ملاک نهایی نیست.
