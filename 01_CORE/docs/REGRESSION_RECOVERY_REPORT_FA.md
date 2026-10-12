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
