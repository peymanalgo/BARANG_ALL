# STEP ۱۵ — Release Manifest

## Candidate

- source SHA: `792a3e0e3f9e8dd7af26bf9d65fe20d17baf4b85`
- branch: `codex/step2-inventory-unit-contracts`
- accepted parent: `86def3f69276d25e74c531866ca8828a17106314`
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
- exact image rerun: `tests/BARANG.Backoffice.E2ETests/TestResults/step15-browser-rc.trx` — fixture/auth failure

## Recovery and release decision

Backup توسعه `.artifacts/step15-backup/barang_core.dump` خارج از Git نگه داشته می‌شود و restore آن verify شد. این candidate به‌دلیل blockerهای browser fixture هنوز release-approved نیست؛ workflowهای موجود و instanceهای running نباید خودکار migrate شوند.
