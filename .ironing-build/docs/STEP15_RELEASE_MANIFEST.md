# STEP ۱۵ — Release Manifest

## Candidate

- source SHA: `792a3e0f7097d2f43c700c8f09d92f51835d884d` (resolvable clean candidate)
- reported SHA `792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85`: unresolved object; not used as provenance
- branch: `codex/step2-inventory-unit-contracts`
- accepted parent: `86def3f69276d25e74c531866ca8828a17106314`
- source-difference assessment: `git diff 792a3e0f..f2f247db -- src` is empty; only the later acceptance fixture script differs, and it is not in the application images
- build: `dotnet build .\BARANG.Backoffice.slnx -c Release --no-restore`

## Local deployment

Project `barang-core` با حفظ volumeهای PostgreSQL/Redis/MinIO rebuild شد. API روی `127.0.0.1:8080` و Backoffice روی `:5100` است. هر دو container مقدار `BARANG_BUILD_COMMIT` برابر candidate دارند.

- API image ID: `sha256:75c2ae35a8ce506dc027e5280b928df831ce38f0cb68202c12d683328728545f`
- Backoffice image ID: `sha256:598fa5a95bd40b56a823707f7042da0d453a4a8fa354d99e4dcf40e5acec18f2`
- both deployed containers expose `BARANG_BUILD_COMMIT=792a3e0f7097d2f43c700c8f09d92f51835d884d`

API root، `/health/ready` و Backoffice `/login` همگی موفق بودند؛ این‌ها جایگزین feature acceptance نیستند.

## Test evidence

- Unit: `tests/BARANG.Core.UnitTests/TestResults/step15-current-unit-built.trx` — 233/233
- Backoffice: `tests/BARANG.Backoffice.Tests/TestResults/step15-current-backoffice-built.trx` — 184/184
- Integration: `tests/BARANG.Core.IntegrationTests/TestResults/step15-current-integration-built.trx` — 135/135
- migration: `tests/BARANG.Core.IntegrationTests/TestResults/step15-isolated-migration-final.trx` — 10/10
- browser equivalent candidate: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-browser-final.trx` — 18/18
- readiness: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-readiness-browser.trx` — 4/4
- replacement-image Pricing→Submit: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-repro-pricing-2.trx` — 1/1
- replacement-image interactivity/readiness: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-repro-browser.trx` — 22/22

Exact replacement setup: project `barang-step15-repro`; API `5885`, Backoffice `5585`, PostgreSQL `5586`, database `barang_step15_browser_test`, Redis `6386`, MinIO `9085/9086`; final migration `20261016140407_AlignShipmentReturnIdempotency`. Authenticated recovery/company selection, Designer graph v4, real uploaded artwork/file versions, reservation and issue setup succeeded. No fake IDs or SQL-fabricated business state were used.

## Recovery and release decision

Backup توسعه `.artifacts/step15-backup/barang_core.dump` خارج از Git نگه داشته می‌شود و restore آن verify شد. Candidate از نظر STEP15 acceptance PASS است؛ rollout graph برای instanceهای موجود خودکار انجام نمی‌شود. توسعه با application imageهای candidate deploy شده، اما graph ایزولهٔ v4 به development import/publish نشده است؛ برای instanceهای جدید باید از مسیر change-control جداگانه configure شود.
