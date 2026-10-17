# STEP ۱۵ — Release Manifest

## Candidate

- source SHA: `792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85`
- branch: `codex/step2-inventory-unit-contracts`
- accepted parent: `86def3f69276d25e74c531866ca8828a17106314`
- acceptance/report commit: `030195e844d1d5c9f0b143b0c4565a0b5253d219`
- build: `dotnet build .\BARANG.Backoffice.slnx -c Release --no-restore`

## Local deployment

Project `barang-core` با حفظ volumeهای PostgreSQL/Redis/MinIO rebuild شد. API روی `127.0.0.1:8080` و Backoffice روی `:5100` است. هر دو container مقدار `BARANG_BUILD_COMMIT` برابر candidate دارند.

- API image manifest: `sha256:e97007c51c3a4fa82f2366b7db479a7ea6f3c0daa59d660da95e30237a846733`
- Backoffice image manifest: `sha256:b9d13fbd902aa98172aa22ec1821b1ffbd10ded2c6158230d2ab1c26b4892f53`

API root، `/health/ready` و Backoffice `/login` همگی موفق بودند؛ این‌ها جایگزین feature acceptance نیستند.

## Test evidence

- Unit: `tests/BARANG.Core.UnitTests/TestResults/step15-unit-final.trx` — 233/233
- Backoffice: `tests/BARANG.Backoffice.Tests/TestResults/step15-backoffice-final.trx` — 184/184
- Integration: `tests/BARANG.Core.IntegrationTests/TestResults/step15-integration-final.trx` — 135/135
- migration: `tests/BARANG.Core.IntegrationTests/TestResults/step15-isolated-migration-final.trx` — 10/10
- browser equivalent candidate: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-browser-final.trx` — 18/18
- readiness: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-readiness-browser.trx` — 4/4
- exact-image Pricing→Submit: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-exact-pricing-built.trx` — 1/1
- exact-image interactivity: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-exact-browser-built.trx` — 18/18
- exact-image readiness: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-exact-readiness-built.trx` — 4/4

Exact isolated setup: project `barang-step15-acceptance`; API `5875`, Backoffice `5575`, PostgreSQL `5576`, Redis `6376`, MinIO `9075/9076`; final migration `20261016140407_AlignShipmentReturnIdempotency`. Bootstrap/login/company selection succeeded. Candidate graph version 4 was created/published only in isolation. Production fixture used real uploaded `artworkVersionId` and `fileVersionId`; no fake IDs or SQL-fabricated order state were used.

## Recovery and release decision

Backup توسعه `.artifacts/step15-backup/barang_core.dump` خارج از Git نگه داشته می‌شود و restore آن verify شد. Candidate از نظر STEP15 acceptance PASS است؛ rollout graph همچنان برای instanceهای موجود خودکار انجام نمی‌شود و graph ایزوله باید برای instanceهای جدید از مسیر change-control جداگانه configure شود.
