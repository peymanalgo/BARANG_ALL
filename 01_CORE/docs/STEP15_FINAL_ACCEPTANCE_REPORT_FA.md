# گزارش نهایی پذیرش STEP ۱۵

تاریخ اجرا: ۱۴۰۵/۰۷/۲۵ — branch: `codex/step2-inventory-unit-contracts`

## نتیجه

**STEP ۱۵: PASS برای release candidate قابل‌بازسازی و rollout محلی.**

محیط ایزوله با imageهای دقیق candidate از نو ساخته شد. هویت، عضویت شرکت و انتخاب شرکت با bootstrap/login رسمی تأیید شد؛ زنجیرهٔ migration کامل اعمال شد؛ graph واقعی سفارش با `InventoryIssue` از مسیر Designer/versioning ساخته و در همان محیط منتشر شد؛ و fixture تولید با artwork/file version واقعی از مسیر upload و Artwork API ساخته شد. سناریوی Pricing→Submit و browser matrix دقیق candidate با build جاری runner موفق شدند. هیچ migration یا business-data توسعه برای این پذیرش تغییر نکرد.

## provenance و تغییرات

- source/application candidate: `792a3e0f7097d2f43c700c8f09d92f51835d884d` (قابل resolve)
- reported source `792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85` قابل resolve نیست؛ `git cat-file`, `fsck`, reflog، worktree و remote refs بررسی شد. `792a3e0f` در reflog به candidate واقعی بالا resolve شد.
- candidate worktree: clean detached worktree از همین SHA؛ `git diff <candidate>..HEAD -- src` خالی است. تنها تفاوت source/test بعدی، fixture تست `tests/Acceptance/Step9ProductionFixture.ps1` است و در application image وارد نشده.
- previous acceptance/report commit: `b02259cc1e7a7486ec33b74b72789688c19044cd`; this finalized provenance update is delivered in the pushed commit containing this report.
- parent accepted baseline: `86def3f69276d25e74c531866ca8828a17106314`
- اصلاح واقعی: `SearchableSelect.razor` دیگر هنگام پاسخ async با `@key` بازسازی نمی‌شود و listbox را باز نگه می‌دارد.
- selectorهای E2E سازمان/افراد با action menu سه‌نقطه‌ای استاندارد منطبق شدند؛ assertionها حذف نشدند.
- `tests/Acceptance/Step9ProductionFixture.ps1` برای نبود override، فایل واقعی را upload و Artwork/ArtworkVersion معتبر می‌سازد و همان شناسهٔ version را به print operation می‌دهد؛ بنابراین fixture دیگر با fake artwork به Submit نمی‌رسد.
- migration یا تغییر business data توسعه برای این اصلاح انجام نشد.

## گیت‌های فعلی

| گیت | نتیجه | evidence |
|---|---:|---|
| Release build | PASS، 0 warning / 0 error | `dotnet build .\BARANG.Backoffice.slnx -c Release --no-restore` |
| Unit | PASS 233/233 | `tests/BARANG.Core.UnitTests/TestResults/step15-current-unit-built.trx` |
| Backoffice | PASS 184/184 | `tests/BARANG.Backoffice.Tests/TestResults/step15-current-backoffice-built.trx` |
| Integration | PASS 135/135، ۳:۵۲، build-aware | `tests/BARANG.Core.IntegrationTests/TestResults/step15-current-integration-built.trx` |
| isolated migration foundation | PASS 10/10 | `tests/BARANG.Core.IntegrationTests/TestResults/step15-isolated-migration-final.trx` |
| shared interactivity، هم‌محتوا با candidate | PASS 18/18 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-browser-final.trx` |
| readiness browser، هم‌محتوا با candidate | PASS 4/4 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-readiness-browser.trx` |
| replacement-image Pricing→Submit | PASS 1/1، با identity/company و artwork واقعی | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-repro-pricing-2.trx` |
| replacement-image interactivity/readiness matrix | PASS 22/22 | `tests/BARANG.Backoffice.E2ETests/TestResults/step15-repro-browser.trx` |

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

- branch نسبت به `origin/main` در زمان تحویل: ۱۳۹ commit جلو و ۲ commit عقب؛ `origin/main` ancestor نیست و main ادغام نشد.
- development local بعد از پذیرش replacement candidate deploy شد؛ API و Backoffice هر دو `BARANG_BUILD_COMMIT=792a3e0f7097d2f43c700c8f09d92f51835d884d` دارند.
- API `:8080` image ID `sha256:75c2ae35a8ce506dc027e5280b928df831ce38f0cb68202c12d683328728545f`؛ Backoffice `:5100` image ID `sha256:598fa5a95bd40b56a823707f7042da0d453a4a8fa354d99e4dcf40e5acec18f2`.
- non-mutating checks: API root=200، readiness=200، Backoffice `/login`=200.
- PR/merge status: وضعیت merge از refs محلی به‌تنهایی قابل اثبات نیست؛ authenticated PR evidence در این اجرا موجود نبود و merge فرض نشده است.

## blockerهای دقیق و scope آینده

برای STEP ۱۵ blocker فنی/پذیرشی بازتولیدشده باقی نماند. سناریوی کامل واحد Customer تا Return/Notification در این run دوباره اجرا نشد؛ انتقال‌های آن در matrix شواهد STEP۲–۱۴ و regressionهای جاری پوشش دارد و به‌عنوان limitation evidentiary صریحاً ثبت شده است، نه ادعای اجرای end-to-end جدید.

## evidence اجرای replacement candidate

- Compose ایزولهٔ نهایی: `barang-step15-repro`؛ API `5885`، Backoffice `5585`، PostgreSQL `5586`، database `barang_step15_browser_test`، Redis `6386` و MinIO `9085/9086`. database دوم پس از تشخیص آلودگی fixture قبلی با migration کامل و role رسمی ساخته شد.
- migration history تا `20261016140407_AlignShipmentReturnIdempotency` رسید؛ schema migration test `DatabaseFoundationTests` برابر 10/10 بود. full Integration پس از آن روی DB ایزولهٔ `_test` برابر 135/135 شد.
- setup رسمی: company/identity با development-admin recovery فقط در DB ایزوله، company selection، مسئولیت‌های organization، graph واقعی 24 node/42 transition با `InventoryIssue` و workflow version 4، و fixture تولید با upload واقعی و `artworkVersionId`/`fileVersionId` واقعی. هیچ runtime record یا graph توسعه copy نشد.
- سناریوی browser `Step6PricingAcceptanceE2ETests` با customer/group، variant/tariff و artwork واقعی 1/1 موفق شد؛ matrix `DeployedInteractivityTests` و readiness در مجموع 22/22 موفق شدند.
- deployment محلی `barang-core` با حفظ containerهای PostgreSQL/Redis/MinIO، volumeها، کلیدها، داده‌ها و credentialهای administrator به imageهای بالا recreate شد. API login برای `ADMIN`، `/health/ready`، API root و Backoffice `/login` همگی موفق شدند.
- گزارش قدیمی با source typo و image digestهای قدیمی به‌عنوان provenance مستقل کنار گذاشته شد؛ imageهای فعلی از clean source worktree قابل‌حل ساخته و سپس با digest/ID بالا deploy شدند.

ManagementDashboard KPI، retention/archive، export جدید، currency conversion، refund/replacement و provider خارجی اعلان‌ها optional/deferred هستند و در STEP ۱۵ پیاده‌سازی نشده‌اند.

جزئیات manifest و index در `docs/STEP15_RELEASE_MANIFEST.md` و `docs/STEP16_HANDOVER_EVIDENCE_INDEX.md` است.
