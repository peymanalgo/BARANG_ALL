# گزارش نهایی پذیرش STEP ۱۵

تاریخ اجرا: ۱۴۰۵/۰۷/۲۵ — branch: `codex/step2-inventory-unit-contracts`

## نتیجه

**STEP ۱۵: FAIL / release candidate هنوز قابل پذیرش نهایی نیست.**

گیت‌های کد و regression سبز هستند، اما system-wide acceptance فقط با service tests ثابت نشده است. اجرای نهایی browser روی image دقیق candidate پس از recreate، fixture هویت ایزوله را نداشت و `/api/v1/auth/login` با 401 پاسخ داد. سناریوی قیمت‌گذاری نیز پس از pricing موفق با `POST /api/v1/orders/{id}/submit` و status `422` متوقف شد، چون fixture چاپ `artworkVersionId` معتبر نداشت. این‌ها blockerهای setup قابل بازتولید هستند، نه قبولی یا نقص پنهان‌شده.

## provenance و تغییرات

- source/application candidate: `792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85`
- parent accepted baseline: `86def3f69276d25e74c531866ca8828a17106314`
- اصلاح واقعی: `SearchableSelect.razor` دیگر هنگام پاسخ async با `@key` بازسازی نمی‌شود و listbox را باز نگه می‌دارد.
- selectorهای E2E سازمان/افراد با action menu سه‌نقطه‌ای استاندارد منطبق شدند؛ assertionها حذف نشدند.
- migration یا تغییر business data توسعه برای این اصلاح انجام نشد.

## گیت‌های فعلی

| گیت | نتیجه | evidence |
|---|---:|---|
| Release build | PASS، 0 warning / 0 error | `dotnet build .\BARANG.Backoffice.slnx -c Release --no-restore` |
| Unit | PASS 233/233 | `tests/BARANG.Core.UnitTests/TestResults/step15-unit-final.trx` |
| Backoffice | PASS 184/184 | `tests/BARANG.Backoffice.Tests/TestResults/step15-backoffice-final.trx` |
| Integration | PASS 135/135، حدود ۳:۵۱ | `tests/BARANG.Core.IntegrationTests/TestResults/step15-integration-final.trx` |
| isolated migration foundation | PASS 10/10 | `tests/BARANG.Core.IntegrationTests/TestResults/step15-isolated-migration-final.trx` |
| shared interactivity، هم‌محتوا با candidate | PASS 18/18 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-browser-final.trx` |
| readiness browser، هم‌محتوا با candidate | PASS 4/4 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-readiness-browser.trx` |
| exact-image browser rerun | FAIL: 4 initialization failure به‌علت fixture/auth؛ 401 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-browser-rc.trx` |
| Step 6 pricing browser | FAIL: submit 422 پس از pricing 200 | logs/trace محیط ایزوله |

Integration با `--blame-hang --blame-hang-timeout 5m` اجرا شد؛ testhost فعال بود، قفل انتظار PostgreSQL دیده نشد و خلاصه کامل 135/135 تولید شد. run صرفاً به‌علت سکوت ۹۰ ثانیه متوقف نشد.

## پوشش بین‌ماژولی، امنیت و recovery

گزارش‌های STEP ۲ تا ۱۴ و شواهد پذیرفته‌شدهٔ آن‌ها برای reservation، issue/release/adjustment/return، pricing snapshot، finance، purchasing/receipt/recheck، production/equipment، quality/reproduction، logistics/delivery/return، outbox، master-data و audit به‌عنوان سابقهٔ unchanged reuse شده‌اند. اجرای فعلی Unit/Backoffice/Integration سبز است و تغییر STEP ۱۵ فقط selector و تست Backoffice را لمس می‌کند؛ بنابراین یک سناریوی authenticated end-to-end فعلی هنوز باید تکمیل شود.

## داده، migration و backup

- Compose development project: `barang-core`؛ volumeهای PostgreSQL/Redis/MinIO و کلیدها حفظ شدند.
- backup: `.artifacts/step15-backup/barang_core.dump`؛ restore با `pg_restore --exit-on-error` در database task-owned `barang_step15_restore` موفق شد.
- محیط ایزوله: project `barang-step15-acceptance` با portهای `5875/5575/5576/6376/9075/9076` و volumeهای جدا؛ credentials/secret در Git یا این گزارش ثبت نشده‌اند.
- migrationهای source تا `20261016140407_AlignShipmentReturnIdempotency` مشاهده شدند؛ migration جدیدی برای STEP ۱۵ لازم نبود.

## deployment و Git

- branch نسبت به `origin/main`: ۱۳۶ commit جلو و ۲ commit عقب؛ main ادغام نشد.
- development local بعد از build candidate deploy شد؛ API و Backoffice هر دو `BARANG_BUILD_COMMIT=792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85` دارند.
- API `:8080` image `sha256:e97007c51c3a4fa82f2366b7db479a7ea6f3c0daa59d660da95e30237a846733`؛ Backoffice `:5100` image `sha256:b9d13fbd902aa98172aa22ec1821b1ffbd10ded2c6158230d2ab1c26b4892f53`.
- non-mutating checks: API root=200، readiness=200، Backoffice `/login`=200.
- PR/merge status: وضعیت merge از refs محلی به‌تنهایی قابل اثبات نیست؛ authenticated PR evidence در این اجرا موجود نبود و merge فرض نشده است.

## blockerهای دقیق و scope آینده

۱. fixture ایزوله باید پس از recreate هویت و prerequisites را deterministic از مسیر پشتیبانی‌شده برقرار کند.
۲. fixture pricing باید artwork version معتبر را به print operation متصل کند؛ صرف upload فایل برای submit فعلی کافی نیست.
۳. پس از اصلاح setup، full authenticated cross-module happy path و browser matrix دقیق candidate باید دوباره اجرا شود.

ManagementDashboard KPI، retention/archive، export جدید، currency conversion، refund/replacement و provider خارجی اعلان‌ها optional/deferred هستند و در STEP ۱۵ پیاده‌سازی نشده‌اند.

جزئیات manifest و index در `docs/STEP15_RELEASE_MANIFEST.md` و `docs/STEP16_HANDOVER_EVIDENCE_INDEX.md` است.
