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

branch نسبت به `origin/main` در شروع بررسی `90` commit جلو و `2` commit عقب بود؛ اکنون پس از سه commit تحویل STEP 8، `93` جلو و `2` عقب است. `origin/codex/step2-inventory-unit-contracts` دقیقاً روی `df34913e0ecc1bdabf1eb48e0c72786fc94ffaf9` است و push بدون force موفق شد. `gh` در محیط نصب نیست، پس PR/merge status از authenticated tooling قابل‌خواندن نبود؛ از نبود ابزار یا 404 unauthenticated نتیجه‌گیری دربارهٔ merge نشده است.

## نتیجه

**STEP 8: CONDITIONAL — NOT FINAL PASS**

زنجیرهٔ shortage proposal، conversion idempotent، عدم ساخت خودکار PO، receipt جزئی و recheck persisted پیاده‌سازی و با تست فعلی verify شده است. blockers دقیق: approval واقعی و assignment/routing مستند PR/PO، browser acceptance کامل برای proposal/PR/PO/receipt/recheck، و failure مشخص UI در انتخاب Variant که باید با trace/DOM اصلاح شود؛ این failure به‌دلیل API نبودن SKU یا کمبود داده ادعا نشده است.

## پیگیری پذیرش approval خرید — ۱۴۰۵/۰۷/۲۴

در این پیگیری policy خرید به‌صورت صریح اعمال شد: PR فقط با تصمیم persisted یک purchasing officer واجد شرایط همان شرکت تأیید می‌شود؛ PO فقط با تصمیم purchasing manager واجد شرایط همان شرکت تأیید و سپس با action جداگانه به supplier ارسال می‌شود. threshold مالی وجود ندارد، سازنده نمی‌تواند سند خود را تأیید کند، Admin fallback ممنوع است و نبود approver خطای `purchasing.approver_not_configured` می‌دهد. تغییر approval-relevant یا ارسال با revision قدیمی، approval قبلی را معتبر نمی‌داند.

تغییرات فعلی در commit مورد آزمون شامل مدل/مهاجرت approval در `InventoryModels.cs`، endpointهای approve/reject/send، سرویس canonical `PurchasingApprovalAdministration`، history/audit، سطح دسترسی‌های `purchasing.request.approve` و `purchasing.order.approve`، و بخش Backoffice `PurchaseApprovalSection` است. migration جدید permission seed را با `ON CONFLICT (code) DO NOTHING` نصب می‌کند تا روی database تازه و databaseای که قبلاً permission را دارد، تکرارپذیر و غیرمخرب باشد.

| سناریو | نتیجهٔ فعلی |
|---|---|
| non-creator واجد شرایط، submit→approve برای PR | PASS، `Step8PurchasingApprovalE2ETests.EligibleNonCreatorApprovesRequestAndOrderBeforeSend` |
| non-creator واجد شرایط، approve برای PO | PASS، همان تست |
| ارسال PO قبل از approval | PASS در domain/unit؛ `ApprovalRequired` |
| ارسال PO بعد از approval با action صریح | PASS، همان تست؛ وضعیت `Sent` |
| self-approval | PASS، API `403` (`purchasing.creator_cannot_approve`) |
| ثبت actor، version و زمان تصمیم در persistence/history | PASS، همان تست و migration جاری |
| SKU8 در فرم Purchase Request | PASS، `PurchaseRequestsE2ETests.PurchaseRequestGridAndSameSessionMultilineDraftWork`، `1/1` پس از اصلاح refresh گزینه‌های variant |
| Unit فعلی | PASS، `229/229` |
| Integration کامل پس از migration جدید | PASS، `133/133` روی `barang_step6_test`/port 5546 با `--blame-hang-timeout 30s -m:1`، 3m43s؛ شامل approval جدید |
| Backoffice unit/component suite | PASS، `184/184` |
| SKU browser acceptance | PASS، `PurchaseRequestsE2ETests.PurchaseRequestGridAndSameSessionMultilineDraftWork`، `1/1` روی API `5821`/Backoffice `5820` با Chrome نصب‌شده و fixture `W8/P8/SKU8` |
| Purchasing approval browser acceptance | PASS، `PurchaseRequestsE2ETests.PurchasingApprovalWorkspaceShowsSubmittedRequestAndRecordsDecision`، `1/1`; approver غیرسازنده از workspace واقعی تصمیم را ثبت کرد و reload/status persisted تأیید شد |

علت دقیق SKU failure قبلی: پاسخ API شامل SKU8 بود، اما callback وابستهٔ Product→Variant قبل از refresh render به‌صورت قطعی اجرا نمی‌شد؛ selector با تغییر value/options کلیددار و preload از endpoint رسمی variant اصلاح شد. SKU hard-code، wait مصنوعی یا bypass UI استفاده نشده است.

**وضعیت جاری:** approval backend/domain/UI و اصلاح SKU با اجرای فعلی API/persistence و browser fixture تأیید شدند. مسیر shortage proposal، conversion idempotent، partial receipt/recheck نیز در full Integration سبز است. با این حال full browser matrix همهٔ سناریوهای receipt/warehouse mutation در این اجرای نهایی تکرار نشده است؛ بنابراین وضعیت release-level STEP 8 را **CONDITIONAL** نگه می‌دارم، نه PASS کاذب. blocker اجرایی approval یا SKU باقی نمانده؛ پذیرش نهایی گستردهٔ receipt/recheck و deployment development باید در gate مستقل ثبت شود.

## ثبت نهایی اجرای فعلی — ۱۴۰۵/۰۷/۲۴

این بخش جایگزین provenance و نتیجهٔ پیگیری قبلی است و به commit کامل `d2deee14ed9729d6d843b3a5b3da49c9f09cc5ee` مربوط است.

- Unit: `229/229` PASS؛ Backoffice unit/component: `184/184` PASS؛ Release buildهای Core و Backoffice PASS.
- Integration ایزوله PostgreSQL: `133/133` PASS در `barang_step6_test` روی port `5546`، با `--blame-hang-timeout 30s -m:1` و migration chain کامل.
- browser SKU: `PurchaseRequestsE2ETests.PurchaseRequestGridAndSameSessionMultilineDraftWork`، `1/1` PASS با system Chrome، API `5821` و Backoffice `5820`.
- browser approval: `PurchaseRequestsE2ETests.PurchasingApprovalWorkspaceShowsSubmittedRequestAndRecordsDecision`، `1/1` PASS؛ approver غیرسازنده از workspace واقعی PR را تأیید کرد، نتیجه و status پس از reload persisted بود.
- علت SKU قبلی: API مقدار `SKU8` را برمی‌گرداند اما refresh وابستهٔ Product→Variant قبل از render قطعی نبود؛ preload رسمی variant و refresh/key اصلاح شد. hard-code یا wait مصنوعی استفاده نشد.
- علت خطای approval قبلی: query روی property محاسباتی unmapped `ApplicationUser.IsActive` به‌جای `AccountState` ترجمه می‌شد و HTTP 500 می‌داد؛ query به وضعیت persisted تغییر کرد.

### توسعه و migration

- قبل از تغییر schema، dump معتبر `D:\00_BARANG\step2-clean\01_CORE\.artifacts\step8_dev_before_purchasing_approval.dump` با حجم `5,043,003` بایت و `1742` TOC entry تهیه و داخل کانتینر با `pg_restore --list` بررسی شد.
- migrationهای `20261016012212_Step8PurchasingApproval` و `20261016014209_Step8PurchasingApprovalPermissions` با EF Core روی `barang_core` اعمال شدند؛ دو migration آخر در `barang.__ef_migrations_history` ثبت‌اند. migration permission seed idempotent است و `ON CONFLICT (code) DO NOTHING` دارد.
- deployment فقط API و Backoffice پروژهٔ Compose `barang-core` را recreate کرد؛ PostgreSQL/Redis/MinIO، volumeها، کلیدها، داده‌ها و credentials حفظ شدند. `/health/ready` و `/login` روی توسعه HTTP `200` هستند.
- API build provenance در log: `BARANG API build commit d8be370546fd469669df4d59795931e38735b8ce`؛ image API: `sha256:6dbd253bfaadd012e890357fec1e93d468058796f42bd33798ab8806e1d61f0a`؛ image Backoffice: `sha256:b0705435e708b795a71de59a44f1241505ee16ce43529e4ec18d9cd12a7c6917`.

### محدودیت پذیرش

نتایج browser متمرکز approval و SKU سبز است و full Integration سبز است، اما اجرای کامل browser matrix برای همهٔ receipt/warehouse mutationها در این نوبت انجام نشده است؛ بنابراین release-level نتیجه **CONDITIONAL، نه PASS نهایی** باقی می‌ماند. هیچ blocker بازتولیدشده‌ای در approval endpoint/UI یا SKU selector باقی نیست. این محدودیت به‌عنوان عدم اجرای شواهد گزارش شده و به‌عنوان موفقیت تاریخی جایگزین نشده است.

## ثبت تکمیل receipt→recheck→reservation retry — ۱۴۰۵/۰۷/۲۴

این بخش نتیجهٔ اجرای واقعی acceptance روی source فعلی است و محدودیت browser receipt ثبت‌شده در بخش‌های قبلی را supersede می‌کند. application source آزموده‌شده پس از commit زیر است:

- tested application commit: `09f5a65` (`fix(step8): complete receipt workflow acceptance`)
- isolated Compose project: `barang-step8-test`
- isolated PostgreSQL: `barang_step8_test`، port `5549`؛ API `5831`؛ Backoffice `5830`
- company fixture: `55596178-ba6c-4f9b-a819-7b0d1498d2b1`
- browser: installed system Chrome، اجرای Playwright با `BARANG_E2E_ISOLATED=true` و fixture سفارش خرید `PO-2026-0000000007`
- development `barang-core` و volumeهای آن در این acceptance استفاده یا mutation نشدند.

### سناریوهای authenticated UI و persistence

| سناریو | نتیجه و evidence |
|---|---|
| shortage→proposal و conversion به یک PR، بدون PO خودکار | PASS؛ شواهد قبلی `Step8PurchasingProposalE2ETests.ShortageConversionIsPersistedAndRetryIsIdempotentWithoutPo` و full Integration جاری حفظ شد. |
| approval مجاز PR/PO و ارسال صریح PO | PASS؛ `PurchaseRequestsE2ETests.PurchasingApprovalWorkspaceShowsSubmittedRequestAndRecordsDecision` و approval browser قبلی حفظ شد. |
| ثبت partial receipt از فرم واقعی | PASS؛ `GoodsReceiptsE2ETests.GoodsReceiptPartialPostOverreceiptAndCompletionAreRealWorkflow`، مرحلهٔ اول `3 PCS` با مرجع یکتا persisted شد. |
| over-receipt در UI | PASS؛ ثبت `6` برای سفارش 8 پس از دریافت 3، alert محلی‌شدهٔ `مقدار رسید از مقدار باقیمانده بیشتر است` را نگه داشت و receipt جدید ایجاد نکرد. |
| ثبت remainder از UI | PASS؛ مرحلهٔ دوم `5 PCS` persisted شد و PO به `Received` رسید. |
| duplicate receipt | PASS؛ درخواست تکراری با همان reference، همان نتیجهٔ persisted را برگرداند و movement دوم نساخت. |
| over-receipt از API | PASS؛ درخواست مستقل با مقدار بیشتر از remainder با HTTP `422` و code `purchasing.over_receipt` رد شد. |
| تغییر خودکار reservation/task پس از receipt | PASS؛ پس از receipt، reservation فعال `0` و `workflow_runtime_tasks` خالی ماند؛ receipt فقط shortage را برای recheck به‌روزرسانی کرد. |
| shortage recheck | PASS؛ ردیف `Resolved` با `required=8`, `available=8`, `shortage=0` و `last_rechecked_at_utc` ثبت شد. |

برای سفارش `PO-2026-0000000007`، دو receipt با مقادیر `3` و `5`، هر دو `PCS`، company یکسان و `purchase_order_id` یکسان ثبت شدند. دو `stock_transactions` متناظر با `transaction_type=Receipt`, `source_type=PURCHASE_ORDER`, و deltaهای `+3` و `+5` وجود دارد؛ `stock_balances` پس از fixture مقدار `on_hand=51`, `reserved=0` نشان داد. این مقادیر مستقل از resolver با queryهای persistence بررسی شدند.

### علت defect و اصلاحات

پیش‌نیاز receipt در UI به endpointهای قدیمی و موجود نبود (`/api/v1/purchasing/receipts` و GET detail) ارجاع می‌داد، پاسخ list را به‌عنوان object parse می‌کرد، و وضعیت `Sent` را eligible نمی‌دانست. UI به endpoint canonical سفارش (`POST /api/v1/purchasing/orders/{id}/receipts`) منتقل شد، list/detail loading با قرارداد واقعی هم‌تراز شد، `Sent/Submitted/PartiallyReceived` پشتیبانی شد، و `ReceiptNumber` durable و یکتا به مدل/پیکربندی/migration هم‌ترازکننده افزوده شد. مقدار ورودی receipt نیز با `@bind:event="oninput"` به‌صورت پایدار به state متصل شد.

در اجرای اول browser، failure از application assertion پنهان نشده بود: مرجع ثابت `UI-STEP8-PARTIAL` قبلاً در همان company استفاده شده بود و idempotency عمداً نتیجهٔ قبلی را برمی‌گرداند. fixture به سفارش مشخص و reference یکتا تغییر کرد؛ پس از آن همان تست کامل `1/1 PASS` شد. این اصلاح نه idempotency را تضعیف می‌کند و نه assertion over-receipt را حذف می‌کند.

### gateهای جاری

- Full Integration روی PostgreSQL تازهٔ ایزوله، با migration chain کامل، `-m:1 --blame-hang-timeout 90s`: **133/133 PASS**.
- Unit: **229/229 PASS**.
- Backoffice: **184/184 PASS**.
- Core API و Backoffice Release build: **PASS، صفر warning/error**.
- Receipt browser feature: **1/1 PASS** با current source، system Chrome و isolated API/Backoffice.

### deployment و نتیجه

درخواست جاری فقط acceptance ایزوله را تکمیل کرد؛ `barang-core` توسعه deploy یا mutate نشد. deployed application commit/images توسعه در این نوبت تغییر نکردند و provenance قبلی همان است. migration جدید `20261016080912_AlignReceiptModel` در محیط ایزوله idempotent و با schema موجود سازگار verify شد؛ backup توسعه و credentials حفظ شدند.

با توجه به approval/SKU browser evidence قبلی، full Integration جاری، و receipt-to-recheck browser evidence بالا، receipt acceptance باقی‌مانده بسته شد. وضعیت نهایی این scope: **STEP 8 PASS**. هیچ تغییر STEP 9 انجام نشد.
