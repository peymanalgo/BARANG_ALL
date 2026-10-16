# گزارش پذیرش STEP 8 — shortage تا proposal، خرید و receipt

## دامنه و provenance

این مرحله بر مبنای policy مصوب اجرا شد: shortage فقط یک purchase-request proposal پایدار می‌سازد؛ تبدیل به PR صریح و مجاز است؛ PO خودکار ساخته نمی‌شود؛ receipt می‌تواند جزئی باشد؛ و receipt فقط recheck کمبود را آشکار می‌کند و reservation یا task را خودکار کامل نمی‌کند.

baseline پذیرفته‌شدهٔ STEP 7، application `0bdf2d61a24f673c56e15cd7d9fd5eece507d7dd` و acceptance source `b20815c4b2267543eb78c2765621c70b33ab4f82` بود. source این بررسی از `d7c807a` شروع شد و تغییرات STEP 8 در commit تحویل نهایی ثبت می‌شوند.

## تغییرات پیاده‌سازی

- `StockShortage` اکنون وضعیت `Open/Converted/Resolved`، شناسهٔ PR، زمان آخرین recheck و پیوند durable به proposal را نگه می‌دارد.
- `POST /api/v1/purchasing/shortages/{shortageId}/convert` از مسیر canonical `InventoryAdministration`، با company scope و permission `purchasing.manage`، proposal را در transaction به یک PR تبدیل می‌کند.
- conversion با قفل deterministic ابتدا روی balance و سپس shortage انجام می‌شود؛ کمبود resolved یا موجودی کافی تبدیل نمی‌شود؛ duplicate retry همان PR را برمی‌گرداند.
- uniqueness روی `CompanyId + SourceShortageId` از duplicate proposal جلوگیری می‌کند.
- receipt موجود در همان transaction ledger/balance، `RecheckShortagesAsync` را اجرا می‌کند؛ reservation و workflow task خودکار تغییر نمی‌کنند.
- Backoffice در grid کمبودها action مشترک تبدیل proposal به PR و status persisted را مصرف می‌کند؛ Add و action-menu استاندارد حفظ شده‌اند.

## migration و recovery

دو migration غیرمخرب ایجاد شد:

1. `20261016003059_Step8ShortageProposalState`
2. `20261016003308_Step8PurchaseRequestSourceUniqueness`

اولی ستون‌های proposal/recheck/link را با default `Open` و backfill رکوردهای قدیمیِ خالی اضافه می‌کند. قبل از هر اعمال روی توسعه، backup custom در `.artifacts/step8_barang_core_before_migration.dump` ایجاد و با `pg_restore --list` قابل‌خواندن بودن آن بررسی شد. این artifact commit نمی‌شود.

## شواهد آزمون فعلی

| سناریو | نتیجه |
|---|---|
| lifecycle واحد proposal: Open→Converted، duplicate conversion رد می‌شود | PASS، `Step8PurchasingProposalTests`، 2/2 |
| API ایزوله: shortage خوانده شد، conversion اول PR ساخت، retry همان PR را برگرداند، PO خودکار صفر بود | PASS، `Step8PurchasingProposalE2ETests.ShortageConversionIsPersistedAndRetryIsIdempotentWithoutPo` |
| receipt جزئی: on-hand افزایش یافت، shortage به مقدار باقیمانده recheck شد و `LastRecheckedAtUtc` ثبت شد | PASS، همان تست ایزوله |
| current full Integration روی PostgreSQL ایزوله `barang_step6_test` در port 5546 | PASS، 132/132، 3m56s |
| current Unit | PASS، 227/227 |
| current Backoffice | PASS، 184/184 |
| API/Backoffice Release build | PASS، صفر warning/error |

تست ایزوله از fixture واقعی `PostgreSqlDatabaseFixture` استفاده کرد؛ migrations تا آخرین revision اعمال شدند و فقط company، warehouse، variant، supplier، stock balance، shortage، PR و PO/receipt تست ایجاد شد. `barang_core` برای تست reset نشد.

## قراردادهای موجود و تصمیم‌های باز

receipt canonical فعلی duplicate reference و over-receipt را رد می‌کند، quantity دریافتی را از receipt rows مشتق می‌کند و stock transaction را append-only ثبت می‌کند. policy شرکت/انبار/UOM و RLS همان قراردادهای قبلی باقی مانده‌اند.

اما اسناد فعلی Purchasing (`INVENTORY_AND_PURCHASING.md`) actor، threshold، assignment و graph مشخص برای approval واقعی PR/PO تعریف نمی‌کنند؛ مدل فعلی فقط `Draft → Submitted` دارد و `Submit` معادل approval انسانیِ مستند نیست. بنابراین این گزارش approval خرید را به‌طور نادرست PASS اعلام نمی‌کند. پیش از PASS نهایی STEP 8 باید owner/permission، threshold، supplier eligibility، شماره‌گذاری/منبع PR→PO و approval task/routing صریحاً تصویب و به canonical workflow متصل شوند. تا آن زمان status این مرحله **CONDITIONAL / NOT FINAL PASS** است.

## deployment و GitHub

تا عبور از gate پذیرش کاملِ approval خرید و browser acceptance، deployment توسعه با migration جدید انجام نشد. imageهای فعلی توسعه همان provenance قبلی STEP 7 را دارند؛ هیچ volume، credential، key یا business record توسعه تغییر نکرد.

branch نسبت به `origin/main` در شروع بررسی `90` commit جلو و `2` commit عقب بود؛ fetch انجام شد و merge/reset/force-push انجام نشد. وضعیت PR/merge باید با remote authenticated tooling بررسی شود و از 404 unauthenticated نتیجه‌گیری نشود.

## نتیجه

**STEP 8: CONDITIONAL — NOT FINAL PASS**

زنجیرهٔ shortage proposal، conversion idempotent، عدم ساخت خودکار PO، receipt جزئی و recheck persisted پیاده‌سازی و با تست فعلی verify شده است. blockers دقیق: approval واقعی و assignment/routing مستند PR/PO، و browser acceptance احراز هویت‌شدهٔ کامل برای proposal/PR/PO/receipt/recheck.
