# گزارش پذیرش STEP 8 — shortage تا proposal، خرید و receipt

## دامنه و provenance

این مرحله بر مبنای policy مصوب اجرا شد: shortage فقط یک purchase-request proposal پایدار می‌سازد؛ تبدیل به PR صریح و مجاز است؛ PO خودکار ساخته نمی‌شود؛ receipt می‌تواند جزئی باشد؛ و receipt فقط recheck کمبود را آشکار می‌کند و reservation یا task را خودکار کامل نمی‌کند.

baseline پذیرفته‌شدهٔ STEP 7، application `0bdf2d61a24f673c56e15cd7d9fd5eece507d7dd` و acceptance source `b20815c4b2267543eb78c2765621c70b33ab4f82` بود. current tested implementation commit `df3a26144b95c176d0ff68e1cbc4d89c38f58bf3` است؛ report-only follow-up commit `4bfa43c7604ed80adfe96457919a9b5769a4c8d6` است و روی imageهای application اعمال نشده.

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
| authenticated Backoffice E2E در environment ایزوله | FAIL، `PurchaseRequestsE2ETests.PurchaseRequestGridAndSameSessionMultilineDraftWork`; پس از login/company selection و بازشدن فرم، selector `.searchable-select-list[role=listbox] [role=option]` برای `SKU8` در 5s صفر option دید. API مستقل `GET /api/v1/products/{P8}/variants/options?limit=100` همان لحظه `SKU8` را برگرداند؛ بنابراین این failure در UI runner/dispatch انتخاب Product→Variant باقی است و موفقیت گزارش نمی‌شود. |
| API/Backoffice Release build | PASS، صفر warning/error |

تست ایزوله از fixture واقعی `PostgreSqlDatabaseFixture` استفاده کرد؛ migrations تا آخرین revision اعمال شدند و فقط company، warehouse، variant، supplier، stock balance، shortage، PR و PO/receipt تست ایجاد شد. `barang_core` برای تست reset نشد.

## قراردادهای موجود و تصمیم‌های باز

receipt canonical فعلی duplicate reference و over-receipt را رد می‌کند، quantity دریافتی را از receipt rows مشتق می‌کند و stock transaction را append-only ثبت می‌کند. policy شرکت/انبار/UOM و RLS همان قراردادهای قبلی باقی مانده‌اند.

اما اسناد فعلی Purchasing (`INVENTORY_AND_PURCHASING.md`) actor، threshold، assignment و graph مشخص برای approval واقعی PR/PO تعریف نمی‌کنند؛ مدل فعلی فقط `Draft → Submitted` دارد و `Submit` معادل approval انسانیِ مستند نیست. بنابراین این گزارش approval خرید را به‌طور نادرست PASS اعلام نمی‌کند. پیش از PASS نهایی STEP 8 باید owner/permission، threshold، supplier eligibility، شماره‌گذاری/منبع PR→PO و approval task/routing صریحاً تصویب و به canonical workflow متصل شوند. تا آن زمان status این مرحله **CONDITIONAL / NOT FINAL PASS** است.

## deployment و GitHub

پس از build و gateهای backend، migrationهای Step8 با backup قبلی روی `barang_core` اعمال و فقط API/Backoffice همین Compose project `barang-core` recreate شدند. volumeهای PostgreSQL/Redis/MinIO و key volumeها حفظ شدند؛ شمارش کنترل‌شدهٔ توسعه پس از migration: companies=5، orders=60، stock_transactions=0، purchase_requests=0، purchase_orders=0. API image `sha256:283b703ac7ef7e2d86e98f718e8b103e18ef141efcedfd8b81b4192f66116671` و Backoffice image `sha256:fe81ab8a55a91c2c758bb163959d28173bad3ec1628ce4d83c33f5541e299314` هر دو `BARANG_BUILD_COMMIT=df3a26144b95c176d0ff68e1cbc4d89c38f58bf3` دارند؛ readiness و `/login` هر دو HTTP 200 هستند. این deployment business mutation انجام نداد.

branch نسبت به `origin/main` در شروع بررسی `90` commit جلو و `2` commit عقب بود؛ fetch انجام شد و merge/reset/force-push انجام نشد. وضعیت PR/merge باید با remote authenticated tooling بررسی شود و از 404 unauthenticated نتیجه‌گیری نشود.

## نتیجه

**STEP 8: CONDITIONAL — NOT FINAL PASS**

زنجیرهٔ shortage proposal، conversion idempotent، عدم ساخت خودکار PO، receipt جزئی و recheck persisted پیاده‌سازی و با تست فعلی verify شده است. blockers دقیق: approval واقعی و assignment/routing مستند PR/PO، browser acceptance کامل برای proposal/PR/PO/receipt/recheck، و failure مشخص UI در انتخاب Variant که باید با trace/DOM اصلاح شود؛ این failure به‌دلیل API نبودن SKU یا کمبود داده ادعا نشده است.
