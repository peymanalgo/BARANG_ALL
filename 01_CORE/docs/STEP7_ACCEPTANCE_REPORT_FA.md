# گزارش پذیرش STEP ۷ — Finance، Invoice و Payment

تاریخ شروع: ۲۰۲۶-۱۰-۱۶  
شاخه: `codex/step2-inventory-unit-contracts`

## نتیجه

وضعیت فعلی: **CONDITIONAL GO** تا اجرای browser acceptance احراز‌شدهٔ Finance در محیط disposable تکمیل شود. مسیرهای API/persistence و gateهای server-side پیاده‌سازی و با تست‌های ایزوله بررسی شده‌اند؛ این گزارش هنوز PASS نهایی UI را ادعا نمی‌کند.

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
| Browser authenticated Finance mutation acceptance | **NOT RUN**؛ این session هیچ browser surface قابل‌کنترل نداشت و in-app browser نیز available نبود؛ اجرای جعلی یا جایگزینی با health check انجام نشد |

## تصمیم‌های خارج از scope یا unresolved

- automatic در برابر explicit invoice issuance: implementation موجود explicit `/finance/invoices/{type}` است؛ trigger خودکار جدید اضافه نشده است.
- invoice numbering فعلی PostgreSQL sequence با prefix مستند را reuse می‌کند؛ accounting/statutory e-invoicing خارج از STEP ۷ است.
- prepayment/full settlement، approval threshold، allocation به چند invoice، credit-limit enforcement و currency conversion سیاست مصوب این STEP نیستند.
- STEP ۸ procurement و Finance/Accounting integrationهای بعدی اجرا نشده‌اند.

## provenance و delivery

## provenance فعلی و مانع پذیرش

- source testها پس از اصلاح gate nodeهای `FinancialApproval` و نام canonical گراف `FINANCE` اجرا شدند؛ commit تست‌شده/تحویل‌شده: `b26f38fc8506835b3e3525e3b2a3ef19ee4f1050`.
- PostgreSQL ایزوله: `barang-step6-postgres-1`، `127.0.0.1:5546`، database `barang_step6_test`، role migration `barang`؛ fixture در طول اجرا role محدود تصادفی `barang_test_app` ساخت. هیچ اتصال تستی به `barang_core` انجام نشد.
- backup توسعه قبل از deployment: `D:\00_BARANG\barang_core_step7_predeploy_20261016.backup`؛ backup خارج از Git و حاوی داده حساس است.
- local development deployment در compose project `barang-core` انجام شد؛ API image digest `sha256:cb9f3ab35e5121665da6446ac714d8d93d34e72a6a2b09301a784f96be300ff3` و Backoffice image digest `sha256:e866b16e6790b4bc9166df91c52917c0b505c57532ed1108817df24f9a85c3a2`، هر دو با `BARANG_BUILD_COMMIT=b26f38f`؛ migration `20261015223227_AddFinancialApprovalSnapshotReference` روی `barang_core` اعمال شد.
- محیط UI ایزولهٔ قبلی `barang-step6` در دسترس Docker بود، اما کنترل‌گر browser این جلسه هیچ `browser` یا `iab` قابل استفاده‌ای ارائه نکرد؛ بنابراین approval/rejection/payment/invoice UI به‌صورت احراز‌شده اجرا نشده است.
- smoke HTTP/health و login shell-level پاسخ‌گو هستند، اما این جایگزین browser acceptance نیست؛ تا اجرای authenticated Finance browser، نتیجهٔ STEP ۷ **CONDITIONAL GO** است، نه PASS.

secrets، backup و trace حساس نباید commit شوند.
