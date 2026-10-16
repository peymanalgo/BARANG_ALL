# گزارش پذیرش STEP ۱۳ — Master Data consistency, traceability و safe cleanup

تاریخ: ۱۴۰۵/۰۷/۲۴ (۲۰۲۶-۱۰-۱۶)  
شاخه: `codex/step2-inventory-unit-contracts`  
Commit مبنای بررسی پیش از تغییر: `b09e2a07b943c8d49f5aa82e18a99d5beb3fb5f4`  
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

در این تغییر، هیچ Razor/JS/DTO یا رفتار business/UI تغییر نکرده است. شواهد browser معتبر قبلی برای DomainReference، Equipment، Product، Customer، Supplier، Organization و Notification Center روی source/deploymentهای پذیرفته‌شده حفظ شد. اجرای standalone E2E از checkout جاری با system Chrome به دلیل نبود محیط browser ایزولهٔ current-source و fixture credentials جدید در این نوبت دوباره انجام نشد؛ اجرای E2E روی `localhost:5100` توسعه برای master-data mutation مجاز نبود. بنابراین browser این نوبت **reused evidence، نه اجرای mutation جدید** است. آزمون‌های route/component و Backoffice 184/184 سبز هستند.

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
- backup توسعه و volumeهای `barang-core` در این task دست‌نخورده ماندند؛ هیچ reset، delete، deactivate، remap یا business mutation روی `barang_core` انجام نشد.
- چون تغییر source در metadata و تست/اسناد است، deployment application در این پذیرش لازم نیست و image جاری توسعه همچنان provenance STEP12 یعنی application `4b6105072f92df02fecec0bdacd0ae5395565b25` را دارد. این image شامل اصلاح STEP13 نیست؛ rollout آن فقط پس از commit/push و gateهای فوق مجاز است.

## GitHub و تصمیم‌های باز

وضعیت divergence و refهای remote باید پس از commit نهایی با `git rev-list --left-right --count origin/main...HEAD` و `git ls-remote` ثبت شود. PR/merge فقط با ref یا tooling authenticated اعلام می‌شود؛ 404 unauthenticated proof نیست.

تصمیم‌های باز STEP ۱۳ فقط cleanup اختیاری جدول بالا و تفکیک احتمالی equipment status است. هیچ‌کدام مجوز حذف/پنهان‌سازی یا تغییر policy قیمت، موجودی، مالی، approval یا workflow نیستند. STEP ۱۴ شروع نشده است.
