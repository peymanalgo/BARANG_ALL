# گزارش نهایی پذیرش STEP ۱۵

تاریخ اجرا: ۱۴۰۵/۰۷/۲۵ — branch: `codex/step2-inventory-unit-contracts`

## نتیجه

**STEP ۱۵: PASS مشروط به rollout عملیاتی عادی؛ شواهد business-flow ایزوله تکمیل شد.**

محیط ایزوله با imageهای دقیق candidate از نو ساخته شد. هویت، عضویت شرکت و انتخاب شرکت با bootstrap/login رسمی تأیید شد؛ زنجیرهٔ migration کامل اعمال شد؛ graph واقعی سفارش با `InventoryIssue` از مسیر Designer/versioning ساخته و در همان محیط منتشر شد؛ و fixture تولید با artwork/file version واقعی از مسیر upload و Artwork API ساخته شد. سناریوی Pricing→Submit و browser matrix دقیق candidate با build جاری runner موفق شدند. هیچ migration یا business-data توسعه برای این پذیرش تغییر نکرد.

## provenance و تغییرات

- source/application candidate: `792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85`
- parent accepted baseline: `86def3f69276d25e74c531866ca8828a17106314`
- اصلاح واقعی: `SearchableSelect.razor` دیگر هنگام پاسخ async با `@key` بازسازی نمی‌شود و listbox را باز نگه می‌دارد.
- selectorهای E2E سازمان/افراد با action menu سه‌نقطه‌ای استاندارد منطبق شدند؛ assertionها حذف نشدند.
- `tests/Acceptance/Step9ProductionFixture.ps1` برای نبود override، فایل واقعی را upload و Artwork/ArtworkVersion معتبر می‌سازد و همان شناسهٔ version را به print operation می‌دهد؛ بنابراین fixture دیگر با fake artwork به Submit نمی‌رسد.
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
| exact-image Pricing→Submit | PASS 1/1، با identity/company و artwork واقعی | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-exact-pricing-built.trx` |
| exact-image interactivity matrix | PASS 18/18 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-exact-browser-built.trx` |
| exact-image readiness | PASS 4/4 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-exact-readiness-built.trx` |

اجرای `--no-build` تست Pricing یک خطای runner با متن `Test process did not return valid JSON (non-object)` تولید کرد؛ این در build/runner پشتیبانی‌شده تکرار نشد. اجرای نهایی با build جاری انجام شد و به‌جای افزایش timeout یا حذف assertion، همان test واقعی 1/1 PASS شد.

Integration با `--blame-hang --blame-hang-timeout 5m` اجرا شد؛ testhost فعال بود، قفل انتظار PostgreSQL دیده نشد و خلاصه کامل 135/135 تولید شد. run صرفاً به‌علت سکوت ۹۰ ثانیه متوقف نشد.

## پوشش بین‌ماژولی، امنیت و recovery

گزارش‌های STEP ۲ تا ۱۴ و شواهد پذیرفته‌شدهٔ آن‌ها برای reservation، issue/release/adjustment/return، pricing snapshot، finance، purchasing/receipt/recheck، production/equipment، quality/reproduction، logistics/delivery/return، outbox، master-data و audit به‌عنوان unchanged evidence reuse شده‌اند. در اجرای فعلی نیز identity/company، workflow candidate، production fixture، Pricing→Submit و interactivity/readiness روی image دقیق candidate با authenticated UI تأیید شدند. full cross-module mutation path به‌صورت یک سناریوی واحد بازاجرا نشد؛ هر بخش آن با شواهد پذیرفته‌شدهٔ قبلی و تست‌های فعلی پوشش دارد و این محدودیت در manifest ثبت شده است.

## داده، migration و backup

- Compose development project: `barang-core`؛ volumeهای PostgreSQL/Redis/MinIO و کلیدها حفظ شدند.
- backup: `.artifacts/step15-backup/barang_core.dump`؛ restore با `pg_restore --exit-on-error` در database task-owned `barang_step15_restore` موفق شد.
- محیط ایزوله: project `barang-step15-acceptance` با portهای `5875/5575/5576/6376/9075/9076` و volumeهای جدا؛ credentials/secret در Git یا این گزارش ثبت نشده‌اند.
- migrationهای source تا `20261016140407_AlignShipmentReturnIdempotency` در دیتابیس ایزوله ثبت شدند؛ migration جدیدی برای STEP ۱۵ لازم نبود.
- graph ایزولهٔ منتشرشده: `9bc56805-6bea-4aff-b7fa-71b59e16b8bb`, version `4`, 24 node/42 transition؛ `InventoryIssue` با assignment مسئول warehouse بین `DECIDE_WAREHOUSE` و `PRINT`. این graph توسعه را جایگزین نکرد.
- fixture تولید ایزولهٔ نهایی: order `2963ac05-8c7c-4248-930b-0cb589b9c01c`، artworkVersion `813fe1c9-0943-4822-9950-1072d5661ed8`، fileVersion `5ba9251d-1c27-4412-8571-f2d015ae824c`؛ دو OrderItem مستقل و issue/reservation واقعی ثبت شدند.

## deployment و Git

- branch نسبت به `origin/main`: ۱۳۶ commit جلو و ۲ commit عقب؛ main ادغام نشد.
- development local بعد از build candidate deploy شد؛ API و Backoffice هر دو `BARANG_BUILD_COMMIT=792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85` دارند.
- API `:8080` image `sha256:e97007c51c3a4fa82f2366b7db479a7ea6f3c0daa59d660da95e30237a846733`؛ Backoffice `:5100` image `sha256:b9d13fbd902aa98172aa22ec1821b1ffbd10ded2c6158230d2ab1c26b4892f53`.
- non-mutating checks: API root=200، readiness=200، Backoffice `/login`=200.
- PR/merge status: وضعیت merge از refs محلی به‌تنهایی قابل اثبات نیست؛ authenticated PR evidence در این اجرا موجود نبود و merge فرض نشده است.

## blockerهای دقیق و scope آینده

برای STEP ۱۵ blocker فنی/پذیرشی بازتولیدشده باقی نماند. محدودیت evidentiary: اجرای یک سناریوی واحد از Customer تا Return/Notification به‌طور کامل در همین run تکرار نشد و با evidence معتبر STEP ۲–۱۴ reuse شد؛ این عدم تکرار، defect یا مجوز تغییر policy نیست.

ManagementDashboard KPI، retention/archive، export جدید، currency conversion، refund/replacement و provider خارجی اعلان‌ها optional/deferred هستند و در STEP ۱۵ پیاده‌سازی نشده‌اند.

جزئیات manifest و index در `docs/STEP15_RELEASE_MANIFEST.md` و `docs/STEP16_HANDOVER_EVIDENCE_INDEX.md` است.
