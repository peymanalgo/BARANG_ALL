# STEP ۱۶ — Handover Evidence Index

این فایل فقط نمایهٔ evidence است و تکمیل user guide یا handover نهایی را ادعا نمی‌کند.

## Reports and contracts

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

TRXهای Unit/Backoffice/Integration/browser در manifest و report بالا ثبت شده‌اند. backup و diagnostics زیر `.artifacts/` task-local هستند و به‌دلیل احتمال شناسه/اطلاعات حساس commit نشده‌اند. provenance استقرار از image digest و `BARANG_BUILD_COMMIT` containerها قابل بررسی است.

## Rollout and open actions

نسخهٔ workflow برای instanceهای موجود تغییر داده نشده است؛ graph candidate در این run فقط در environment ایزوله ساخته/منتشر شد و برای instanceهای جدید پس از configure/validate جداگانه قابل rollout است. Exact-image identity/company, migration, workflow, artwork-backed production fixture, Pricing→Submit و browser evidence در manifest و STEP15 report ثبت شده‌اند. full cross-module business path از STEP2–14 به‌عنوان unchanged evidence reuse شده و اجرای یکپارچهٔ دوبارهٔ آن به‌عنوان optional release evidence باقی است؛ این فایل همچنان user guide یا handover نهایی نیست.
