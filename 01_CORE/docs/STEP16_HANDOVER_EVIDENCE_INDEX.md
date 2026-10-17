# STEP ۱۶ — Handover Evidence Index

این فایل فقط نمایهٔ evidence است و تکمیل user guide یا handover نهایی را ادعا نمی‌کند.

## Reports and contracts

- Notification Center implementation: `src/BARANG.Backoffice/Components/Pages/NotificationCenter.razor`, `src/BARANG.Backoffice/Components/NotificationBell.razor`, `src/BARANG.Backoffice/wwwroot/notification-center.css`, `src/BARANG.Backoffice/wwwroot/notification-bell.css`
- Notification query contract/filter evidence: `src/BARANG.Core.Application/Background/BackgroundContracts.cs`, `src/BARANG.Core.Api/Background/BackgroundEndpoints.cs`, `src/BARANG.Core.Infrastructure/Background/NotificationAdministration.cs`, `tests/BARANG.Core.IntegrationTests/Database/NotificationOutboxAcceptanceTests.cs`
- Deployed Notification Center screenshots: `.artifacts/step12-notification-browser-fa-desktop.png`, `.artifacts/step12-notification-browser-fa-mobile.png`, `.artifacts/step12-notification-browser-en.png`, `.artifacts/step12-notification-browser-ar.png`

- `docs/STEP3_READINESS_REVIEW_FA.md`
- `docs/STEP4_ACCEPTANCE_REPORT_FA.md`
- `docs/STEP4_5_GRID_ACTION_ACCEPTANCE_FA.md`
- `docs/STEP5_ACCEPTANCE_REPORT_FA.md` تا `docs/STEP14_ACCEPTANCE_REPORT_FA.md`
- `docs/STEP13_MASTER_DATA_CONSUMER_CATALOG_FA.md`
- `docs/STEP15_FINAL_ACCEPTANCE_REPORT_FA.md`
- `docs/INTEGRATION_IMPLEMENTATION_PLAN_FA.md`

## Reproducible tooling

- `compose.yaml`
- `docker/postgres/init-app-role.sh`
- `tests/Acceptance/fixtures/barang-order-workflow.v17.sanitized.json`
- `tests/Acceptance/Step4WorkflowSetup.ps1`
- `tests/Acceptance/Step9ProductionFixture.ps1`
- `tests/Acceptance/Step10QualityFixture.ps1`
- `tests/BARANG.Backoffice.E2ETests/`

## Evidence paths

TRXهای current-source در manifest و report بالا ثبت شده‌اند. candidate قابل‌بازسازی `792a3e0f7097d2f43c700c8f09d92f51835d884d` است؛ SHA گزارش‌شدهٔ `792a3e0e...` resolve نشد و نباید به‌عنوان provenance استفاده شود. image IDهای API/Backoffice و `BARANG_BUILD_COMMIT` containerها در manifest ثبت شده‌اند؛ metadata به‌تنهایی proof source نیست و اینجا با clean detached worktree و diff source corroborate شده است. backup و diagnostics زیر `.artifacts/` task-local هستند و commit نشده‌اند.

## Rollout and open actions

نسخهٔ workflow برای instanceهای موجود تغییر داده نشده است؛ graph candidate در این run فقط در environment ایزوله ساخته/منتشر شد و برای instanceهای جدید پس از configure/validate جداگانه قابل rollout است. Exact-image identity/company, migration, workflow, artwork-backed production fixture, Pricing→Submit و browser evidence در manifest و STEP15 report ثبت شده‌اند. full cross-module business path از STEP2–14 به‌عنوان unchanged evidence reuse شده و اجرای یکپارچهٔ دوبارهٔ آن به‌عنوان limitation evidentiary ثبت است؛ این فایل همچنان user guide یا handover نهایی نیست.
