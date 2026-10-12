# گزارش بازیابی regression سراسری BARANG

تاریخ بررسی: ۱۴۰۵/۰۷/۲۰  — checkout: `codex/step2-inventory-unit-contracts`

## نتیجه اجرایی

خطاها از یک خرابی واحد ناشی نبودند و به دو نقص مشترک در checkout/deployment فعلی و یک مسیر حذف‌شده تقسیم شدند:

1. mapping پیش‌فرض EF برای `Person` نام CLR یعنی `"Persons"` را به SQL می‌فرستاد، در حالی که schema canonical جدول `barang.persons` است. این موضوع `/api/v1/customers` و مصرف‌کنندگان سازمان/فروشنده را به `42P01 relation "barang.Persons" does not exist` و HTTP 500 می‌رساند.
2. دادهٔ معتبر قدیمی tariff با `tariff_type = 'Design'` وجود داشت، اما enum `TariffType` در API عضو `Design` نداشت؛ خواندن `/api/v1/pricing/tariffs` با `Cannot convert string value 'Design'` و HTTP 500 شکست می‌خورد. عضو enum به قرارداد canonical موجود اضافه شد؛ مقدارهای عددی بعدی حفظ شدند (`AdditionalService=5`).
3. checkout مستقر routeهای واقعی `/api/v1/organization/chart` و `/api/v1/organization/persons` را نداشت، در حالی که صفحات و migrationهای canonical آن‌ها در source/DB موجود بودند؛ این درخواست‌ها 404 می‌دادند.

هیچ authorization، query filter، حساب admin، دادهٔ کسب‌وکار یا رمز عبور reset نشد. برای هماهنگ‌کردن مدل EF و snapshot، migration `20261012124313_ProbeModelAlignment` فقط snapshot را ثبت می‌کند؛ migrationهای قبلی canonical قبلاً جدول‌های lowercase را می‌سازند و این migration عمدی هیچ rename/drop/data movement انجام نمی‌دهد.

## شواهد و اصلاحات

| ماژول/مسیر | قبل | استثناء/علت | اصلاح و نتیجهٔ runtime |
|---|---:|---|---|
| داده پایه، `GET /api/v1/lookups/types` | 200 | context شرکت معتبر | 200، پاسخ JSON معتبر |
| مشتری، `GET /api/v1/customers` | 500 | `CustomerStore.SearchAsync:59` → `Npgsql 42P01 barang.Persons` | `PersonConfiguration` با `ToTable("persons")`؛ 200 |
| محصولات، `GET /api/v1/products` | 200 | — | 200، داده نمایش داده شد |
| Design Bank، `GET /api/v1/artworks` | 200 | — | 200، داده نمایش داده شد |
| سفارش، `GET /api/v1/orders` | 200 | — | 200، داده نمایش داده شد |
| Cartable، `GET /api/v1/workflows/tasks/mine` | 200 | — | 200، لیست خالی واقعی |
| Pricing، `GET /api/v1/pricing/tariffs` | 500 | EF enum materialization روی `Design` | افزودن `TariffType.Design`؛ 200 |
| Inventory، `GET /api/v1/inventory/balances` | 200 | — | 200، empty state واقعی |
| Purchasing، `GET /api/v1/purchasing/orders` | 200 | — | 200، empty state واقعی |
| User Management، `GET /api/v1/security/users` | 200 | — | 200 |
| Organization chart، `GET /api/v1/organization/chart` | 404 | endpoint در checkout ناقص بود | بازیابی قرارداد/store/route واقعی؛ 200 |
| Persons، `GET /api/v1/organization/persons` | 404 | endpoint در checkout ناقص بود | route read/search واقعی؛ 200 |

مسیرهای API با login واقعی local admin و `X-Company-ID` شرکت مجاز اجرا شدند؛ token و credential در خروجی ثبت نشده‌اند. لاگ API برای customer correlationهای `bbac3c4128154715a7010d7ffb54b056` و `675deda5048f4fc8a090d37704652ce4` علت‌های فوق را نشان داد.

## فایل‌ها و provenance

- `src/BARANG.Core.Infrastructure/Persistence/Configurations/OrganizationConfigurations.cs`: mapping canonical `Person` به `barang.persons` و mapping سه entity سازمانی.
- `src/BARANG.Core.Domain/Pricing/PricingModels.cs`: عضو canonical `TariffType.Design`؛ enumهای موجود بعد از آن جابه‌جا نشده‌اند.
- `src/BARANG.Core.Api/Organization/OrganizationEndpoints.cs`: routeهای chart/persons با permission موجود.
- `src/BARANG.Core.Application/Organization/OrganizationalChartContracts.cs` و `OrganizationalChartAdministration.cs`: قرارداد و service واقعی بازیابی‌شده از implementation موجود.
- `src/BARANG.Core.Domain/Organization/{OrganizationalAssignment,Position,PositionAssignment}.cs` و `src/BARANG.Core.Infrastructure/Persistence/OrganizationalChartStore.cs`: entity/store واقعی؛ بدون stub.
- `src/BARANG.Core.Infrastructure/Persistence/CoreDbContext.cs` و `DependencyInjection.cs`: DbSet، company filter و registrationهای لازم.
- `src/BARANG.Core.Domain/Organization/OrganizationErrors.cs` و `OrganizationAdministration.cs`: خطاها و mapperهای لازم برای همان service.
- `src/BARANG.Core.Infrastructure/Persistence/Migrations/20261012124313_ProbeModelAlignment.*`: migration/snapshot alignment؛ `Up/Down` بدون تغییر داده.
- `tests/BARANG.Core.IntegrationTests/Database/PricingE2ETests.cs`: fixture عددی stale با enum canonical هماهنگ شد (`AdditionalService=5`).

فایل‌های سازمانی از implementation موجود در checkout اصلی `D:\00_BARANG\01_CORE` provenance گرفتند؛ مدل فعلی feature branch با حذف وابستگی‌های ناسازگار مثل `ApplicationUser.PersonId` سازگار شد.

## verification

- build Release API: موفق، ۰ warning / ۰ error.
- build Release Backoffice: موفق، ۰ warning / ۰ error.
- unit tests Pricing/Organization: `23 passed`.
- Backoffice regression filters (`CoreApiClientTests`, login، route table): `22 passed`.
- isolated PostgreSQL database `barang_regression_test` (restored/migrated copy): customer/pricing/identity filters `12 passed`.
- fresh isolated PostgreSQL database `barang_regression_fresh_test`: full migration chain و همان ۱۲ تست `12 passed`.
- backup توسعه: `D:\00_BARANG\barang_core_regression_20261012161225.backup`، archive با `pg_restore --list` معتبر و حدود ۱۷۳۵ entry؛ backup خارج repository است و commit نمی‌شود. migration `20261012124313_ProbeModelAlignment` پس از بررسی backup با `dotnet ef database update` اعمال شد و فقط history/snapshot را ثبت کرد.
- local deployment از compose context `D:\00_BARANG\step2-clean\01_CORE`: API image digest `sha256:a5e1f6da92e021a85862c42e69bc0ac9a119a9a1d1c28e5cb9d2236f8c0a6d09` و Backoffice image digest `sha256:416fbff78105a4de6f862d8c7dd49942b31c6c90a17a96f688e73e82fd2e6f5e` در حال اجرا هستند.
- login واقعی Backoffice با فرم `/auth/login`، cookie/session، root authenticated و logout با HTTP بررسی شد؛ visual browser automation در این محیط در دسترس نبود.

## محدودیت‌ها و remaining

این گزارش recovery است، نه شروع reservation/consumption یا سایر گام‌های integration plan. پوشش browser بصری و CRUD mutation هر صفحه به‌صورت UI کامل انجام نشد؛ API/persistence و smoke مسیرهای نماینده بررسی شد. Cartable runtime کامل و workflow business integration همچنان خارج از این recovery و یک blocker مستقل برنامهٔ integration است.
