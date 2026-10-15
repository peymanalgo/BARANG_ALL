# گزارش پذیرش STEP ۷ — Finance، Invoice و Payment

تاریخ شروع: ۲۰۲۶-۱۰-۱۶  
شاخه: `codex/step2-inventory-unit-contracts`

## نتیجه

وضعیت فعلی: **CONDITIONAL GO**. مسیرهای API/persistence و UI Finance در محیط disposable با Chrome احراز‌شده اجرا شدند و gateهای فعلی source pass هستند. تصمیم کسب‌وکاری automatic در برابر explicit invoice issuance هنوز صادر نشده است؛ این گزارش آن را اختراع نمی‌کند.

## سیاست‌های reused

- `docs/FINANCE_AND_INVOICE.md` قرارداد موجود برای decision، invoice، payment/reversal و balance است.
- approval انسانی با permission `finance.approve` انجام می‌شود؛ threshold، accounting ledger، gateway و prepayment policy اختراع نشده‌اند.
- Invoice نهایی فقط پس از Order Submitted، finalized pricing snapshot و approval صادر می‌شود؛ Proforma شرط approval ندارد.
- Payment و reversal رکوردهای immutable جدا هستند؛ invoice به‌تنهایی paid نیست. `Net paid = payments - reversals` و balance طبق سند موجود محاسبه می‌شود.
- correction/reversal به‌جای ویرایش مستقیم invoice/payment است.

## اصلاحات STEP ۷

- `OrderFinancialState.ApprovedPriceSnapshotId` اضافه شد؛ approval دقیقاً به snapshot finalized فعلی متصل می‌شود.
- approval مجدد فقط برای snapshot جدید مجاز است؛ snapshot قدیمی دوباره یا با همان revision approve نمی‌شود.
- invoice نهایی، علاوه بر Approved بودن state، تطبیق `ApprovedPriceSnapshotId` با current finalized snapshot را الزام می‌کند.
- تکمیل FinancialApproval در `WorkflowBusinessCompletionAdapter` دیگر auto-approve نمی‌کند؛ فقط decision persisted و snapshot منطبق را می‌پذیرد.
- تکمیل definition-runtime FinancialApproval نیز همین evidence را در سرور کنترل می‌کند؛ generic/legacy completion بدون decision معتبر advance نمی‌شود.
- invoice generation برای همان order/type با lock روی order و unique constraint موجود idempotent/concurrency-safe باقی می‌ماند.
- Payment، reversal، reference uniqueness، currency/amount validation و derived balance implementation موجود حفظ شد.

## تکمیل Finance UI در این پذیرش

- `FinanceEvidencePanel.razor` به `/finance/{OrderId}` اضافه شد: وضعیت تصمیم، شناسهٔ snapshot تأییدشده، زمان تصمیم، total/currency، paid و outstanding balance را از API رسمی می‌خواند.
- invoiceها به‌جای JSON خام به کارت‌های خوانا با شماره، نوع، وضعیت، مشتری، currency، subtotal، discount، tax و total نمایش داده می‌شوند.
- بعد از payment/approval/invoice، panel با کلید refresh دوباره از persistence بارگذاری می‌شود؛ handler و permissionهای canonical تغییر نکردند.
- متن‌های جدید fa/en/ar به resource رسمی اضافه شدند.

## migration و محیط

- migration غیرمخرب: `AddFinancialApprovalSnapshotReference`
- تغییر: nullable `order_financial_states.approved_price_snapshot_id`، index و FK composite به finalized price snapshot.
- زنجیرهٔ migration در PostgreSQL ایزولهٔ `barang_step6_test` با port `5546` اعمال شد؛ توسعه با backup قبلی `D:\00_BARANG\barang_core_step6_predeploy.backup` محافظت شده است.
- هیچ reset یا mutation روی دادهٔ توسعه در این مرحله انجام نشده است.

## شواهد آزمون فعلی

| سناریو | نتیجه |
|---|---:|
| Finance unit tests، شامل optimistic concurrency و snapshot-bound reapproval | **9/9 PASS** |
| Finance integration، شامل payment/reversal/balance/approval/invoice/isolation | **3/3 PASS** |
| Invoice قبل از approval | **422، rejected** |
| Approval response شامل snapshot ID | **PASS** |
| Invoice retry | **موجود در `FinanceE2ETests` و PASS** |
| Finance unit tests | **9/9 PASS** |
| Finance integration (`PaymentReversalApprovalInvoiceHistoryAndIsolationFlow`) | **1/1 PASS** |
| Full Unit | **225/225 PASS** |
| Full Backoffice | **184/184 PASS** |
| Full Integration روی `barang_step6_test` با اتصال admin صحیح | **130/130 PASS، 0 failure، 0 skipped، 3:29** |
| API/Backoffice Release build | **PASS، 0 warning / 0 error** |
| System Chrome discovery | **PASS**؛ `C:\Program Files\Google\Chrome\Application\chrome.exe` با Playwright اجرا شد |
| Finance UI browser، login/company selection، snapshot/balance/invoice evidence | **PASS**؛ `Step7FinanceUiAcceptanceE2ETests.FinanceEvidenceAndOperationsAreRenderedForPersistedOrder`، 1/1 |
| Finance UI payment از فرم و persisted reload | **PASS**؛ POST payment با 201 و GET balance/evidence بعد از reload، payment idempotency با reference ایزوله |
| Invoice قبل از approval از UI | **PASS**؛ action اجرا شد و inline error نمایش داده شد؛ invoice جدیدی ساخته نشد |
| Isolated migration chain | **PASS**؛ fresh `barang_step7_test` روی port 5547، همهٔ migrationها تا `20261015223227_AddFinancialApprovalSnapshotReference` |
| Current full Integration rerun | **130/130 PASS، 0 failure، 0 skipped، 3:48**؛ `barang_step6_test` و API/backoffice ایزوله قبل از run متوقف شدند تا Hangfire رقابت نکند |

## تصمیم‌های خارج از scope یا unresolved

- automatic در برابر explicit invoice issuance: implementation موجود explicit `/finance/invoices/{type}` است؛ trigger خودکار جدید اضافه نشده است. Business approval باید مشخص کند آیا همین دکمه/endpoint trigger رسمی است یا invoice باید پس از approval خودکار صادر شود؛ این بخش تا آن تصمیم **مشروط** است.
- invoice numbering فعلی PostgreSQL sequence با prefix مستند را reuse می‌کند؛ accounting/statutory e-invoicing خارج از STEP ۷ است.
- prepayment/full settlement، approval threshold، allocation به چند invoice، credit-limit enforcement و currency conversion سیاست مصوب این STEP نیستند.
- STEP ۸ procurement و Finance/Accounting integrationهای بعدی اجرا نشده‌اند.

## provenance و delivery

## provenance فعلی و شواهد محیط

- source testها پس از اصلاح gate nodeهای `FinancialApproval` و نام canonical گراف `FINANCE` اجرا شدند؛ commit تست‌شده/تحویل‌شده: `b26f38fc8506835b3e3525e3b2a3ef19ee4f1050`.
- PostgreSQL ایزوله: `barang-step6-postgres-1`، `127.0.0.1:5546`، database `barang_step6_test`، role migration `barang`؛ fixture در طول اجرا role محدود تصادفی `barang_test_app` ساخت. هیچ اتصال تستی به `barang_core` انجام نشد.
- backup توسعه قبل از deployment: `D:\00_BARANG\barang_core_step7_predeploy_20261016.backup`؛ backup خارج از Git و حاوی داده حساس است.
- local development deployment در compose project `barang-core` انجام شد؛ API image digest `sha256:cb9f3ab35e5121665da6446ac714d8d93d34e72a6a2b09301a784f96be300ff3` و Backoffice image digest `sha256:e866b16e6790b4bc9166df91c52917c0b505c57532ed1108817df24f9a85c3a2`، هر دو با `BARANG_BUILD_COMMIT=b26f38f`؛ migration `20261015223227_AddFinancialApprovalSnapshotReference` روی `barang_core` اعمال شد.
- محیط disposable نهایی: Compose project `barang-step7`، database `barang_step7_test` روی `127.0.0.1:5547`، API `8087` و Backoffice `5107`، volumes با prefix `barang-step7_`; fixture شرکت `42f0e459-6095-45bb-aa13-f38126d91d9c` و سفارش ایزوله `b4170a86-61fa-4dac-b774-d764b65be294` بودند. graph واقعی `BARANG.ORDER` از fixture sanitized import و در isolation publish شد؛ نسخهٔ published `1`.
- browser runner ابتدا به‌علت login fixture ناموجود روی `barang-step6` با 401/timeout شکست خورد؛ محیط تازه با migration کامل، bootstrap رسمی، انتخاب company و System Chrome آماده شد. یک assertion نخست نیز به‌علت culture فارسی setup بود و با انتخاب `en` اصلاح شد؛ assertion کسب‌وکاری ضعیف نشد.
- full Integration یک run بدون app و بدون lock حدود چهار دقیقه ساکت ماند؛ process/PG locks بررسی شد و lock/query فعال وجود نداشت. اجرای مجدد با `--blame-hang-timeout 5m` کامل شد و 130/130 PASS داد. اجرای آزمایشی روی database بدون suffix `_test` به‌طور مستقل با setup validation (`database named barang_*_test`) fail شد و در نتیجهٔ نهایی شمرده نشده است.
- در این تغییر migration جدیدی لازم نبود؛ UI-only است. development backup قبلی همچنان `D:\00_BARANG\barang_core_step7_predeploy_20261016.backup` است و business data توسعه در پذیرش UI mutation نشده است.
- source فعلی پس از commit نهایی باید با image جدید به `barang-core` deploy شود؛ تا قبل از آن port 5100 همچنان image تاریخی `b26f38f` را سرو می‌کند.

secrets، backup و trace حساس نباید commit شوند.
