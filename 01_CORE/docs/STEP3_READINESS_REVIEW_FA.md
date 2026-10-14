# بازبینی آمادگی STEP 3 — BARANG

تاریخ بازبینی: ۱۴۰۵/۰۷/۲۲ برابر با ۲۰۲۶-۱۰-۱۴

## نتیجهٔ اجرایی

**STEP 3: NO-GO**

کد و schema فعلیِ checkout reviewed از نظر build، unit، Backoffice و integration ایزوله سبز است؛ اما baseline عملیاتی و deployment فعلی با source reviewed هم‌نسخه‌بودنِ قابل‌اثبات ندارد و چهار سناریوی browser E2E read-only روی deployment فعلی شکست خوردند. بنابراین health/HTTP 200 جای پذیرش runtime را نمی‌گیرد و STEP 3 نباید آغاز/پذیرفته شود تا checkout و imageهای API/Backoffice هم‌نسخه شوند و E2E پایه دوباره سبز شود.

خطای `helper_unknown_error: setup refresh had errors` در این بازبینی مشاهده نشد.

## بررسی read-only محیط

| بررسی | نتیجه |
|---|---|
| دسترسی به `D:\00_BARANG` | موفق |
| دسترسی به `D:\00_BARANG\step2-clean\01_CORE` | موفق |
| Git | `git version 2.55.0.windows.3` |
| .NET | SDK `10.0.400` |
| checkout reviewed | شاخه `codex/step2-inventory-unit-contracts`، commit `d811e3140a9edddd91a9ae41c82ae6653015243c` |
| working tree checkout | فقط `src/BARANG.Backoffice/.keys/` به‌صورت untracked؛ بدون تغییر کد |
| repository root | `D:\00_BARANG`، شاخه `main`، commit `09afbe3a60ba7e12bdb2e01edf5228e9f5269a37` و از قبل بسیار dirty؛ دست‌نخورده ماند |
| branch relation | `origin/main...HEAD = 2 behind, 29 ahead`؛ branch remote وجود دارد |

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

### Browser E2E روی deployment فعلی

فقط سناریوهای read-only مربوط به login، shell، زبان/RTL و responsive اجرا شدند؛ سناریوهای create/edit روی دادهٔ توسعه اجرا نشدند.

- `GlobalDesignSystemShellUsesLoginPaletteAndRemainsCompactResponsive`: **failure**؛ timeout ۳۰ ثانیه در `Locator("a.primary-link")`.
- `GlobalLanguageSwitchUpdatesLocalizedShellAndDirection(en)`: **failure**؛ انتظار `Dashboard`، مقدار واقعی `Hello, پیمان خوشه چین`.
- همان تست برای `ar`: انتظار `لوحة المعلومات`، مقدار واقعی `مرحباً، پیمان خوشه چین`.
- همان تست برای `fa`: انتظار `داشبورد`، مقدار واقعی `سلام، پیمان خوشه چین`.

در اجرای قبلی E2E بدون target صریح نیز runner با `Test process did not return valid JSON (non-object)` و `No test is available` برگشت؛ این نتیجه جایگزین اجرای جاری محسوب نشد.

## source در برابر deployed API/Backoffice و migration

### source reviewed

- branch/HEAD: `codex/step2-inventory-unit-contracts` / `d811e3140a9edddd91a9ae41c82ae6653015243c`.
- branch remote با همین HEAD موجود است.
- source شامل migration `20261012100553_Step2InventoryUnitContracts` و migrationهای بعدی تا `20261013070735_EquipmentMappingMetadataAlignment` است.

### deployment فعلی

- API container image: `sha256:c1bff03c8ba6edb2ae3934cb135bd7acfe17e53330a9172d1df680dedd9e54f3`، ساخته‌شده `2026-10-13T14:08:42Z`.
- Backoffice container image: `sha256:cb3f9706023d84f5151077048540ba073fd4f020a2c1e4568f92c91b5877eba1`، ساخته‌شده `2026-10-13T14:07:36Z`.
- هر دو container از compose همین مسیر اجرا می‌شوند، ولی imageهای موجود OCI revision/commit label ندارند؛ بنابراین انتساب قطعی آن‌ها به `d811e314` ممکن نیست. زمان build نیز اندکی پیش از commit فعلی است.
- API `/health/ready` و `/health`: HTTP 200 / `Healthy`؛ Backoffice `/login`: HTTP 200. این فقط readiness است، نه پذیرش UI.
- migration history توسعه، read-only، تا `20261013070735_EquipmentMappingMetadataAlignment` است و شامل `20261012100553_Step2InventoryUnitContracts` و `20261012115004_AlignTariffCompatibilityColumns` است.

نتیجهٔ تطبیق: migration history با source فعلی هم‌راستا به‌نظر می‌رسد، ولی provenance imageهای deployed قابل‌اثبات نیست و browser مشاهده‌شده با contract/testهای checkout reviewed منطبق نیست. این mismatch blocker پذیرش baseline است.

## بررسی رفتار حوزه‌ها

| حوزه | شواهد جاری | نتیجه |
|---|---|---|
| Base Data و Lookup | `LookupFoundationTests` در suite integration؛ API/authorization/RLS و catalogهای جاری | API/persistence سبز؛ browser menu acceptance با deployment فعلی fail/ناقص |
| Login/Admin و Customer/Supplier | `IdentityAndRbacTests`، `UserAccessManagementTests`، `CustomerNormalizedApiTests`، `SupplierCoreApiTests` و Backoffice suite | source/test host سبز؛ browser shell deployment mismatch دارد |
| Design Bank | `ArtworkFileFoundationTests` و `ArtworkFileE2ETests` | API/persistence ایزوله سبز؛ browser visual/modal CRUD جاری تأیید نشد |
| Order/Print/Pricing/Submit | `OrderFoundationTests`، `OrderE2ETests`، `PricingFoundationTests`، `PricingE2ETests` و business-profile tests | current isolated suite سبز؛ deployed browser acceptance قابل قبول نیست |
| Workflow/Cartable | `WorkflowFoundationTests` و unit workflow tests | foundation/schema/auth سبز؛ owner-service completion، full graph journey و deployed Cartable browser هنوز evidence کافی ندارد |
| Inventory/Purchasing | `InventoryFoundationTests` و تست‌های sourcing/purchase-related در suite | identity/unit/schema و API foundation سبز؛ reservation lifecycle و shortage-to-purchasing end-to-end هنوز تصمیم/پذیرش نشده |

## پذیرش baseline recovery

**قبول کامل: خیر.**

بخش source recovery فعلی قابل‌ساخت و قابل‌آزمون است و تست‌های ایزوله جاری سبز هستند؛ اما recovery baseline عملیاتی پذیرفته نیست چون:

1. deployment pair به commit reviewed قابل انتساب قطعی نیست؛
2. browser E2E پایه روی deployment جاری چهار failure واقعی دارد؛
3. برخی گزارش‌های recovery تاریخی خودشان محدودیت browser و Workflow/Cartable را ثبت کرده‌اند؛
4. فقط health و HTTP 200، بدون runtime UI acceptance، کافی نیست.

## وضعیت STEP 2 — هویت و UOM

### معیارهای جداگانه

- **Exact `OrderItemId`:** در `StockReservation` نگهداری می‌شود و unit test جاری distinct بودن دو item و persistence مقدار را اثبات می‌کند.
- **Company/Warehouse/Variant identity:** مدل و migration ستون‌های company، warehouse و product variant را نگه می‌دارند؛ integration suite ایزولاسیون شرکت، FK/شناسه و RLS را سبز کرده است.
- **Unit persistence:** `uom_code` در reservation، purchase request item و purchase order item اضافه/پر شده و در migration `20261012100553_Step2InventoryUnitContracts` non-null می‌شود؛ snapshot محصول نیز `snapshot_uom_code` دارد.
- **Mismatch rejection:** domain/API validation واحد canonical محصول را الزام می‌کند؛ unit/integration coverage جاری سبز است و conversion ضمنی ایجاد نشده است.
- **Schema consistency:** fresh isolated DB با full chain migrate شد و ۱۲۶ integration سبز شد؛ توسعه نیز migration history شامل migration STEP 2 است.

### نتیجهٔ STEP 2

از نظر source، contract، migration و verification ایزوله: **تکمیل‌شده**.

از نظر operational acceptance روی deployment فعلی: **تکمیل عملیاتی کامل قابل اعلام نیست** تا image/source provenance و browser baseline اصلاح/تأیید شوند. این تفکیک به معنی failure معیارهای identity/UOM نیست؛ blocker مربوط به هم‌نسخه‌بودن deployment و UI acceptance است.

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

- `origin/codex/step2-inventory-unit-contracts` به HEAD `d811e314...` اشاره می‌کند.
- `origin/main` روی `09afbe3...` است؛ branch برابر ۲۹ commit جلو و ۲ commit عقب است.
- remote: `https://github.com/peymanalgo/BARANG_ALL.git`.
- GitHub REST API unauthenticated برای repository/PR/compare پاسخ 404 داد و `gh` روی محیط نصب نیست؛ بنابراین وجود/وضعیت merge یا PR با شواهد مستقیم GitHub قابل تأیید نیست. **Merge status: نامشخص، نه merged اعلام می‌شود و نه فرض می‌شود.**

## محدودیت و اقدام بعدی مجاز

هیچ application code، operational data، migration، deployment یا admin credential تغییر داده نشد. تنها تغییر محیطی، ساخت database ایزولهٔ `barang_step3_review_20261014_test` برای تست بود.

قبل از GO شدن STEP 3 باید checkout reviewed به imageهای deployed با revision قابل‌ردیابی deploy شود، همان browser baseline read-only دوباره اجرا و سبز شود، و تصمیم‌های بخش STEP 3 تصویب شوند. این گزارش تنها artifact مورد نظر برای commit/push است.
