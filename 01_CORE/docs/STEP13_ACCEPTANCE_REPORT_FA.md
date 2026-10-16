# گزارش پذیرش STEP ۱۳ — Master Data consistency, traceability و safe cleanup

تاریخ: ۱۴۰۵/۰۷/۲۴ (۲۰۲۶-۱۰-۱۶)  
شاخه: `codex/step2-inventory-unit-contracts`  
Commit مبنای بررسی پیش از تغییر: `b09e2a07b943c8d49f5aa82e18a99d5beb3fb5f4`  
Commit منبع تست/استقرار فعلی: `58e3538fad517f494c9da5be03144eb288058a55`
تغییر STEP ۱۳ در این گزارش: metadata catalog، تست invariant و اسناد؛ migration schema ندارد.

## نتیجهٔ اجرایی

وضعیت STEP ۱۳: **PASS با تصمیم‌های cleanup باز و بدون تغییر مخرب**.

همهٔ catalogهای canonical، شامل ۴۳ نوع Lookup و دو adapter ساختاریافته (`artwork.tag` و `defect.type`)، در `ReferenceConsumerTraceability` مصرف‌کنندهٔ مشخص دارند: **۴۵/۴۵**. وضعیت‌های داخلی state machine به‌عنوان Lookup گزارش نشده‌اند. هیچ جدول، field، رکورد، status، permission، credential یا business data حذف/غیرفعال/پنهان نشد.

## تغییرات واقعی

1. `DomainReferenceCatalog` برای `equipment.type` و `equipment.status` دامنهٔ canonical `equipment` را اضافه می‌کند و alias سازگار `inventory` را حفظ می‌کند؛ بنابراین مسیر قبلی `/master/reference-data/inventory/...` نمی‌شکند و مسیر مالک equipment هم metadata صحیح دارد.
2. `ReferenceConsumerTraceability` مصرف `equipment.status` را دقیقاً برای هر دو فیلد مستقل ثبت می‌کند: `OperationalStatusCode` برای eligibility و `LifecycleStatusCode` برای lifecycle/history. این دو همچنان state و validation جدا دارند.
3. تست `MasterDataTraceabilityTests` برابری catalog/consumer map و سازگاری equipment domain/field map را قفل می‌کند.
4. `docs/STEP13_MASTER_DATA_CONSUMER_CATALOG_FA.md` فهرست route/source/API/model/scope/hierarchy/localization/CRUD/active policy/consumer و classification همهٔ دامنه‌ها را ثبت می‌کند. lifecycle contract و implementation plan نیز الحاقیهٔ STEP ۱۳ دارند.

## ماتریس پوشش

| خانواده | شواهد canonical | مصرف‌کننده‌های بررسی‌شده | نتیجه |
|---|---|---|---|
| Customer / Supplier | Domain models، administration، Customer/Supplier endpoints | گروه، tax/payment، آدرس/مخاطب/بانک، sourcing/lifecycle | PASS |
| Product / Variant / UOM | Catalog models/config، options endpoints | Order، Inventory، Purchasing، Production | PASS |
| Design Bank / category / tag | ArtworkFile administration، category lookup، adapter API | Order print، file/version، Production/Quality lineage | PASS |
| Equipment | Equipment model/config/endpoints، EquipmentManager | Production capability/status، branch/unit/warehouse/person | PASS؛ metadata اصلاح شد |
| Organization / Workflow | Organization/Workflow models و runtime/designer endpoints | company، warehouse responsibility، positions، Cartable | PASS؛ statuses fixed contracts |
| Pricing / Finance | Pricing/Finance models/services/endpoints | tariff، customer/price group، tax status، snapshots، invoice/payment | PASS؛ policy تغییر نکرد |
| Inventory / Purchasing | Inventory/Purchasing services/endpoints | UOM، warehouse، shortage/proposal/PR/PO/receipt | PASS؛ policy تغییر نکرد |
| Quality / Logistics | Quality/Logistics services/endpoints | defect/template/inspection، package/shipment/delivery/return | PASS؛ policy تغییر نکرد |
| Notifications | Outbox/Notification/Background endpoints | event source، recipient company/permission، template/read state | PASS؛ provider خارجی خارج scope |

## آزمون‌ها و نتایج واقعی

| اجرا | محیط/دستور | نتیجه |
|---|---|---|
| Unit کامل | `dotnet test tests/BARANG.Core.UnitTests/BARANG.Core.UnitTests.csproj -c Release --no-restore` | **233/233 PASS**؛ شامل ۲ تست جدید STEP ۱۳؛ TRX: `tests/BARANG.Core.UnitTests/TestResults/step13-unit.trx` |
| Backoffice کامل | `dotnet test tests/BARANG.Backoffice.Tests/BARANG.Backoffice.Tests.csproj -c Release --no-restore` | **184/184 PASS** |
| Integration کامل | `BARANG_TEST_DB_CONNECTION` به PostgreSQL ایزوله `barang_step10_quality_test` روی port 5569؛ `dotnet test tests/BARANG.Core.IntegrationTests/BARANG.Core.IntegrationTests.csproj -c Release --no-restore --blame-hang --blame-hang-timeout 5m` | **135/135 PASS**، ۳:۴۹؛ TRX: `tests/BARANG.Core.IntegrationTests/TestResults/step13-integration.trx` |
| Release build | `dotnet build BARANG.Backoffice.slnx -c Release --no-restore` | **۰ warning / ۰ error** |
| Lookup/reference persistence | `LookupFoundationTests`, `SupplierReferenceFoundationTests` داخل Integration | scope/RLS، global/company، fa/en/ar، active/inactive، version، reference protection و parent/category checks PASS |

Integration به database توسعه `barang_core` وصل نشد؛ fixture رسمی نام database را به `barang_*_test` محدود می‌کند و نقش‌های migration/test app را خودش آماده کرد. volume ایزولهٔ آزمون مستقل از volume توسعه بود.

### Browser evidence

در این تغییر، هیچ Razor/JS/DTO یا رفتار business/UI تغییر نکرده است. برای current-source، standalone xUnit v3 in-process runner (به‌جای مسیر ناسازگار dotnet/vstest که خطای `Test process did not return valid JSON` می‌دهد) با system Chrome روی `http://localhost:5100` اجرا شد. نتیجهٔ جاری:

| سناریو | نتیجه و شواهد |
|---|---|
| `EveryExposedNavigationPageRendersTheSharedShellWithoutRuntimeFailure` | PASS؛ همهٔ routeهای permission-visible، shell، heading، نبود Blazor error و نبود overflow بررسی شد. |
| `GlobalDesignSystemShellUsesLoginPaletteAndRemainsCompactResponsive` | PASS؛ shell دسکتاپ/موبایل و نبود overflow بررسی شد. |
| `GlobalLanguageSwitchUpdatesLocalizedShellAndDirection` | PASS برای fa/rtl، ar/rtl و en/ltr؛ greeting و زبان انتخاب‌شده بررسی شد. |
| `SharedSortHeadersUserGridAndActionIconsMatchTheGlobalStandard` | PASS؛ sorting، منوی vertical action و ابعاد trigger بررسی شد. |

جمع اجرای جاری: **۶/۶ PASS، ۰ failure، ۰ error، ۰ skip**؛ TRX: `.artifacts/step13-browser/step13-readonly.trx`. اجرای قبلی dotnet/vstest به خطای زیر پیش از شروع test case متوقف می‌شد و defect محصول نیست: `Test process did not return valid JSON (non-object)`؛ به همین دلیل runner رسمی in-process xUnit v3 استفاده شد. این‌ها آزمون‌های read-only/route هستند؛ هیچ mutation روی دادهٔ توسعه انجام نشد. آزمون‌های mutation مربوط به master-data همچنان فقط با fixture ایزوله مجازند و شواهد پذیرفتهٔ قبلی آن‌ها حفظ شده است.

این محدودیت برای اصلاح metadata صرفاً مستنداتی است؛ اگر rollout آینده catalog equipment در مسیر جدید لازم باشد، یک acceptance browser read-only در محیط disposable باید قبل از تغییر دادهٔ master اجرا شود.

## حفاظت از reference و consistency

- گزینه‌های جدید از active values و ترجمهٔ requested culture → `fa` → code ساخته می‌شوند؛ مقدار inactive موجود در edit/history مطابق قرارداد قابل مشاهده است.
- Lookup deletion فقط با version صحیح و بدون reference ممکن است؛ category دارای child و مقدار referenced رد می‌شود. Deactivation جایگزین حذف برای حفظ history است.
- parent category در API از همان type و ancestry بررسی و cycle رد می‌شود؛ FK پایه نیز وجود parent را enforce می‌کند.
- شرکت/والد/رابطه‌ها در EF/RLS و composite FKهای موجود محدود می‌شوند؛ cross-company reference از مسیر API rejected است و تست‌های Customer/Supplier/Catalog/Artwork این مرز را پوشش می‌دهند.
- وضعیت‌های Workflow/Order/Stock/Invoice/Shipment/Job/Inspection و permissionها internal fixed contract هستند و به Lookup تبدیل نشدند.

## پیشنهادهای cleanup — تصمیم کاربر لازم است

| مسیر منو | موجودیت | شواهد نبود مصرف | پیشنهاد حفظ/تکمیل/پنهان‌سازی | وابستگی و ریسک | تصمیم موردنیاز |
|---|---|---|---|---|---|
| Master Data → Equipment Reference Data | تفکیک `equipment.status` به operational/lifecycle | اکنون یک catalog دو فیلد مستقل را تغذیه می‌کند؛ consumer واقعی هر دو ثبت شده است | **حفظ فعلی** تا تصویب تفکیک؛ سپس تکمیل با دو catalog مستقل | تغییر کدهای موجود و migration/reference risk | آیا تفکیک دو catalog لازم است؟ |
| Master Data → Reference Data domains | دامنهٔ نمایشی `inventory` در کنار `equipment` | alias قدیمی برای route سازگاری حفظ شده و هر دو endpoint به یک catalog وصل‌اند | **حفظ compatibility alias**؛ پنهان‌سازی فقط پس از browser/route migration | شکستن bookmark و لینک‌های موجود | آیا پس از migration رسمی alias پنهان شود؟ |
| Master Data → Equipment child tabs | failure/repair/maintenance/inspection/document panels | مدل/route UI وجود دارد؛ مصرف تراکنشی کامل این مرحله در اسناد تولید/تجهیزات خارج از scope STEP ۱۳ است | **حفظ** و تکمیل در مرحلهٔ مالک خود؛ حذف نشود | دادهٔ تاریخی و future operational workflows | مالک محصول roadmap مصرف را تعیین کند |
| Master Data → shared catalog administration | catalogهایی که فعلاً دادهٔ کمی دارند | empty table یا text-search به‌تنهایی evidence unused نیست؛ تعریف canonical و selector موجود است | **حفظ**؛ فقط پس از consumer audit مستقل پیشنهاد cleanup | ریسک حذف reference و ترجمه | تصمیم حذف/غیرفعال‌سازی هیچ موردی در این STEP داده نشده است |

## Migration، backup و deployment

- تغییر STEP ۱۳ migration ندارد؛ fresh/restored migration chain با اجرای Integration روی PostgreSQL ایزولهٔ موجود تا آخرین migration موفق بود.
- backup توسعه و volumeهای `barang-core` در این task دست‌نخورده ماندند؛ هیچ reset، delete، deactivate، remap یا business mutation روی `barang_core` انجام نشد و migration جدیدی اعمال نشد.
- پس از gateها، جفت API/Backoffice جاری با Compose پروژهٔ `barang-core` از source همین checkout build/deploy شد؛ سرویس‌های postgres، redis و minio و volumeهایشان حفظ شدند. هر دو container مقدار provenance `BARANG_BUILD_COMMIT=58e3538fad517f494c9da5be03144eb288058a55` دارند:
  - API image: `sha256:0ff4b348cf30a2d6b3e66617a665eb0e4a004be7f8e34b3ab7a82a11c811a411`؛ `http://127.0.0.1:8080/health/ready` = 200.
  - Backoffice image: `sha256:6237384e38a4370b0fcecdd8c324301bb5fc7b55a317591122b7844fd3c3f74e`؛ `http://localhost:5100/login` = 200.
  این rollout فقط local development است؛ workflow definition، credentials، کلیدها و business data تغییر نکردند.

## GitHub و تصمیم‌های باز

در زمان ثبت این گزارش، branch جاری روی remote با SHA `58e3538fad517f494c9da5be03144eb288058a55` موجود است؛ `origin/main` برابر `09afbe3a60ba7e12bdb2e01edf5228e9f5269a37` و divergence برابر **۲ commit عقب / ۱۲۹ commit جلو** (`origin/main...HEAD`) است. ref branch push شده، اما وضعیت PR/merge با ابزار authenticated در این محیط قابل اثبات نیست؛ بنابراین merge را اعلام نمی‌کنم.

تصمیم‌های باز STEP ۱۳ فقط cleanup اختیاری جدول بالا و تفکیک احتمالی equipment status است. هیچ‌کدام مجوز حذف/پنهان‌سازی یا تغییر policy قیمت، موجودی، مالی، approval یا workflow نیستند. STEP ۱۴ شروع نشده است.
