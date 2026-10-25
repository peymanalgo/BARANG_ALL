# گزارش پذیرش STEP 5 — گیت‌های واقعی عملیات Inventory در Workflow/Cartable

وضعیت این گزارش: اجرای جاری از سورس همین checkout؛ STEP 6 و integrationهای Finance/Quality/Logistics در scope نیستند.

## baseline و provenance

- checkout: `codex/step2-inventory-unit-contracts`
- baseline پذیرفته‌شدهٔ UI: `ea08cdf13b54830ec310470229224bb5a3ff1112`
- گزارش baseline: `e5de12a30c4802daa11fa7cf4bc1e27f597c945f`
- tested source / final application commit: `604c0f3c3d37b6bc648597609825ede6e0254158`
- working tree قبل از کار فقط artifact/keyهای untracked داشت؛ تغییرات آن‌ها stage نشده‌اند.
- deployment اولیه هنگام شروع: Compose project `barang-core`، فایل `compose.yaml`، API روی `localhost:8080` و Backoffice روی `localhost:5100`؛ API provenance `71d88a9790ac5af3a3ab70c4a8e3e9316a6b9e75` و Backoffice provenance `ea08cdf13b54830ec310470229224bb5a3ff1112` بود.
- volumeها و credential/keyهای development دست‌نخورده ماندند. migration جدید لازم نشد؛ زنجیره migration فقط در PostgreSQL ایزوله اعمال شد.

## تضمین‌های موجود و gapهای بسته‌شده

پیاده‌سازی canonical `InventoryAdministration` همچنان مالک رزرو، issue، release، adjustment و return است و workflow عملیات موازی ایجاد نمی‌کند. مسیر `WorkflowDefinitionRuntime` قبلاً سرویس را صدا می‌زد، اما:

1. گیت reservation فقط از نتیجهٔ عملیات استفاده می‌کرد و کامل‌بودن همهٔ OrderItemهای فعال، variant/UOM/warehouse و وضعیت `Active` را مستقل تأیید نمی‌کرد.
2. گیت issue فقط reservationهای اقلام ارسال‌شده در request را می‌دید؛ request ناقص می‌توانست همهٔ اقلام سفارش را پوشش ندهد.
3. visibility/claim برای InventoryIssue وجود یک reservation را کافی می‌دانست.
4. adapter قدیمی `IWorkflowBusinessCompletionAdapter` برای reservation فقط وجود یک رکورد را می‌سنجید.

اکنون gate مشترک در Inventory، در همان transaction عملیات و transition، برای تمام OrderItemهای فعال بررسی می‌کند:

- reservation: تعداد دقیق itemها، `OrderItemId`، variant، warehouse، quantity، UOM canonical، `ReservedQuantity/RemainingQuantity` و وضعیت Active؛ shortage یا replay روی reservation آزادشده completion را جلو نمی‌برد.
- issue: برای هر OrderItem جاری reservation معتبر و مجموع persisted `WAREHOUSE_ISSUE` movement کافی لازم است؛ return compensating، evidence تاریخی issue را حذف نمی‌کند و task تکمیل‌شده را reopen نمی‌کند.
- Claim و Cartable visibility برای InventoryIssue دیگر با یک reservation کافی نیستند و مسئولیت فعال همان warehouse را برای همهٔ itemها می‌خواهند.
- مسیر legacy workflow نیز به همان persisted evidence gate وصل شد؛ هیچ endpoint generic دیگری operation را bypass نمی‌کند.
- transaction موجود حفظ شده است: failure gate باعث rollback movementهای همان request و عدم تغییر task می‌شود؛ reference/idempotency canonical سرویس حفظ شده است.

## تغییرات

- `src/BARANG.Core.Infrastructure/Inventory/InventoryWorkflowGates.cs`: گیت‌های authoritative و projection نتیجه‌های pending در transaction.
- `WorkflowDefinitionRuntime.cs`: اجرای gate بعد از canonical reservation/issue و اصلاح claim/list SQL برای پوشش همهٔ itemها.
- `WorkflowBusinessCompletionAdapter.cs`: اصلاح bypass قدیمی reservation.
- `tests/BARANG.Core.IntegrationTests/Database/Step5WorkflowInventoryGateTests.cs`: fixture ایزولهٔ graph واقعی Start → InventoryReservation → InventoryIssue → End، دو OrderItem هم‌کلید، shortage/rollback، full issue و retry.

## شواهد تست

محیط: PostgreSQL disposable موجود `barang-step45-postgres`، DB `barang_step45_test`، host port `5545`، role test application مطابق fixture؛ development DB/volume استفاده نشد.

| تست | نتیجه |
|---|---:|
| `Step5WorkflowInventoryGateTests.RuntimeInventoryStagesRequireCompleteEvidenceAndAreRetrySafe` | PASS؛ issue ناقص 422، task باز، on-hand=10/reserved=5؛ issue کامل task را یک‌بار جلو برد، on-hand=5/reserved=0، دو ledger issue؛ retry بدون duplicate |
| Full current Integration (`BARANG_TEST_DB_CONNECTION`, Release، hang diagnostics 5m) | **130/130 PASS، 3:26** |
| Unit | **222/222 PASS** |
| Backoffice | **184/184 PASS** |
| Architecture | **4/4 PASS** |
| Release Core/Backoffice build | PASS، 0 warning/0 error |

اجرای اشتباه اولیهٔ solution بدون `BARANG_TEST_DB_CONNECTION` عمداً به‌عنوان نتیجهٔ تست ثبت نشد؛ تمام نتیجهٔ نهایی Integration با connection صریح ایزولهٔ بالا تکرار شد. Backoffice E2E عمومی همچنان runner قدیمی xUnit/VSTest با `Test process did not return valid JSON` دارد؛ این blocker زیرساختی سابق است و به‌عنوان PASS feature browser گزارش نمی‌شود.

## تصمیم‌های باقی‌مانده

- رفتار مصوب return حفظ شد: return usable compensating movement است و issue evidence تاریخی را برای taskی که قبلاً complete شده باطل نمی‌کند؛ return damaged/unknown وارد usable stock نمی‌شود. reopen/reissue یک policy آینده است و در STEP 5 ساخته نشد.
- taskهای قدیمی و inconsistent بازنویسی یا auto-migrate نمی‌شوند؛ completion جدید آن‌ها فقط با evidence کامل ممکن است و reconciliation موجود می‌تواند inconsistency را گزارش کند.
- در snapshot اولیه، browser acceptance اختصاصی Cartable با fixture واقعی STEP 3/4 هنوز اجرا نشده بود؛ addendum انتهای گزارش آن را در runner پشتیبانی‌شده و محیط disposable پوشش می‌دهد.

## deployment و delivery

deployment محلی development با همان Compose project `barang-core` و بدون reset انجام شد:

- API: `BARANG_BUILD_COMMIT=604c0f3`، image `sha256:0dd67a54836bb9335277c2730811235f2d975394736d74cb9913876fd1c00b52`
- Backoffice: `BARANG_BUILD_COMMIT=604c0f3`، image `sha256:fb00d581b9ad52bf02a5e3878c340dae897dc4ef4371629f033fea9c6bb07b71`
- API `localhost:8080` و Backoffice `localhost:5100` healthy؛ Postgres/MinIO/Redis و volumeهای development باقی ماندند.
- deployed browser evidence: `SharedActionMenuItemsKeepIconAndLabelOnOneHorizontalRow` fa/ar/en = 3/3 PASS؛ `EveryExposedNavigationPageRendersTheSharedShellWithoutRuntimeFailure` = PASS؛ `GlobalLanguageSwitchUpdatesLocalizedShellAndDirection` fa/en/ar = 3/3 PASS. تست‌ها با Chrome نصب‌شده و `BARANG_E2E_ALLOW_SHARED_DEV_DB=true` فقط smoke/read-only UI بودند.

## جمع‌بندی پذیرش

از نظر backend و completion-gate، STEP 5 **PASS** است: هر دو Inventory stage و legacy adapter به evidence کامل canonical متصل‌اند و 130/130 Integration، Unit، Backoffice و build سبز هستند. در snapshot اولیهٔ این گزارش browser mutation acceptance اختصاصی Cartable هنوز اجرا نشده بود؛ این gap در addendum زیر با runner پشتیبانی‌شده و fixture ایزوله بسته شد. اجرای عمومی E2E به‌علت خطای شناخته‌شدهٔ xUnit/VSTest `Test process did not return valid JSON` قابل اتکا نیست؛ آن خطای runner قدیمی با acceptance اختصاصی جایگزین نشد و به‌عنوان نتیجهٔ feature گزارش نمی‌شود.

## تکمیل پذیرش مرورگری Cartable در محیط disposable

در اجرای جاری، browser acceptance روی یک محیط کاملاً جدا از `barang-core` انجام شد:

- PostgreSQL: `barang-step45-postgres`، دیتابیس `barang_step5_browser_test`، پورت `5545`، role برنامهٔ ایزوله؛ volume توسعه استفاده نشد.
- API: `http://127.0.0.1:58080`؛ Backoffice: `http://127.0.0.1:55100`؛ Redis ایزوله روی `6385` و MinIO ایزوله روی `5900`.
- شرکت، شعبه، مشتری، محصول، variant، انبار، مسئولیت warehouse و سفارش‌ها با مدل دامنه و APIهای رسمی ساخته شدند. گراف از fixture sanitize‌شدهٔ `tests/Acceptance/fixtures/barang-order-workflow.v17.sanitized.json` از مسیر Designer import/draft/validate/publish ساخته شد.
- گراف منتشرشدهٔ ایزوله version `2` است و مسیر واقعی آن `DECIDE_WAREHOUSE → InventoryReservation → InventoryIssue → PRINT` است. instanceهای قبلی به version قبلی bind ماندند؛ سفارش‌های acceptance به version 2 bind شدند.
- Chrome: `C:\Program Files\Google\Chrome\Application\chrome.exe`.

| سناریوی UI | نتیجهٔ جاری | شواهد persisted |
|---|---:|---|
| shortage برای دو OrderItem هم‌کلید | PASS | متن `Shortage; task remains open.`؛ task در همان URL باقی ماند؛ reservation جدید ایجاد نشد |
| رزرو موفق دو OrderItem مستقل | PASS | دو reservation مستقل با OrderItemIdهای `5dacdf7e-72b3-441f-b962-042e2072163a` و `05d60753-de2b-4ff3-a778-7731505745f4` |
| issue موفق از Cartable | PASS | UI به `/cartable` برگشت؛ سپس reload Cartable بدون خطای Blazor انجام شد |
| retry همان task | PASS | HTTP 200 idempotent؛ `WAREHOUSE_ISSUE` همچنان ۲ رکورد، بدون duplicate |
| ledger/balance پس از issue | PASS | `onHand=45`, `reserved=0`, `available=45` |
| issue بدون evidence کامل، generic/legacy bypass | PASS | تست `Step5WorkflowInventoryGateTests.RuntimeInventoryStagesRequireCompleteEvidenceAndAreRetrySafe`؛ issue ناقص 422 و task باز |
| Claim قبل از عملیات | PASS | warehouse user دارای responsibility توانست task را Claim کند؛ claim به evidence تکمیل‌شدهٔ inventory وابسته نشد |

تست‌های مرورگری reproducible در `tests/BARANG.Backoffice.E2ETests/Step5CartableMutationAcceptanceE2ETests.cs` اضافه شدند و با selectorهای فعلی UI اجرا شدند:

- `ReservationShortageKeepsTaskOpenWithPreciseUiError` — PASS، 5.99s
- `PostedIssueCompletesInventoryTaskAndReloadsToCartable` — PASS، 6.73s

اجرای قدیمی `Step3ReservationAcceptanceE2ETests` دو failure ظاهری داشت: یکی بعد از shortage هنوز redirect موفق را assert می‌کرد و دیگری بعد از completion مستقیم UI منتظر dialog منسوخ `.dialog-card .form-actions` می‌ماند. هر دو با شواهد UI/HTTP مشخصاً test expectation قدیمی بودند، نه failure برنامه؛ assertionهای جدید رفتار approved فعلی را می‌سنجند و تضعیف نشده‌اند.

## نتیجهٔ نهایی STEP 5

با تکمیل browser mutation acceptance، گیت‌های reservation/issue، shortage، claim، idempotency، persisted reload و عدم bypass در محیط ایزولهٔ واقعی تأیید شدند. development application تغییر نکرد و همان deployment قبلی با provenance `604c0f3` محفوظ ماند. STEP 5 **PASS** است؛ STEP 6 شروع نشده است.
