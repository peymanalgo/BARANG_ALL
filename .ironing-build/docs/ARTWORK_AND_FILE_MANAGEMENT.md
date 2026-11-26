# Artwork and File Management

Phase 10 adds a reusable, company-owned artwork library and secure object-storage metadata. It does not add Order behavior or store binary content in PostgreSQL.

## Domain model

`Artwork` has an immutable company-unique normalized code, mutable bounded title/description, optional `artwork.category` Lookup-compatible code and designer user, Draft/Active/Archived lifecycle, current-version pointer, active/archive flags, timestamps, and optimistic version. Archived artwork remains directly readable but is excluded from options and cannot receive a new version until restored.

`ArtworkVersion` is an immutable snapshot binding an artwork to one immutable `FileVersion`. Version numbers are allocated sequentially per artwork and old rows are never overwritten or deleted. Company-owned `ArtworkTag` records and same-company assignments provide a deliberately flat controlled tag system; inactive tags and assignments remain historical.

`StoredFile` is the mutable logical identity and archive boundary. `FileVersion` is the immutable physical revision containing server-derived filename metadata, MIME type, size, SHA-256, provider/bucket/key, author, processing state, and version number. Binary bytes live only in object storage. `FileLink` accepts only centrally defined entity types; Phase 10 enables Artwork, with future enum extensions reserved for reviewed modules.

## Storage and upload security

`IObjectStorage` is an Application boundary; the Infrastructure adapter uses the Apache-2.0 MinIO .NET SDK and can be replaced by another S3-compatible provider. Keys are generated server-side as company/file/version/random paths and never contain the uploaded filename. The MinIO bucket is private and created idempotently. No public or permanent object URL is returned.

Uploads are authenticated, company-scoped and protected by `file.upload`. ASP.NET and the application enforce a configurable byte limit (25 MiB locally). The service accepts PNG, JPEG, GIF, WebP, BMP, TIFF, and PDF only, and rejects empty content, traversal/rooted/control-character filenames, unsupported extensions, extension/MIME mismatches, and invalid file signatures. SVG and executable/script formats are intentionally unsupported. The client hash is ignored: SHA-256 is streamed server-side while content is copied to a bounded temporary file. Duplicate hashes are detected only inside the current company and return a deterministic conflict; no cross-company possession is revealed and physical deduplication is deferred.

The object is written before the PostgreSQL metadata transaction. Storage failure creates no database row. If the database transaction fails after the object write, the service attempts removal using the uncommitted key. Committed objects are never removed by normal APIs. This is compensation, not distributed ACID; future retention tooling may reconcile rare crash-window orphans.

Downloads require authentication, valid company context and `file.read`. EF filters and PostgreSQL RLS hide guessed/cross-company IDs, and Core streams bytes after authorization rather than exposing MinIO. Archive preserves historical downloads and metadata. There are no permanent DELETE routes.

## Processing and thumbnails

Every upload creates a durable `FileProcessingJob`. A small hosted worker is the Phase 10 execution adapter and leaves a clean boundary for later Hangfire integration. All six raster formats (PNG/JPEG/GIF/WebP/BMP/TIFF) are processed asynchronously with Magick.NET 14.16.0 (Apache-2.0, self-hosted, Linux-capable): metadata is stripped, a bounded PNG thumbnail is stored at a distinct randomized derivative key, and an immutable `FileDerivative` row records its size and SHA-256. PDF is explicitly NotApplicable. Failures store only a technical error code and attempt count, never stack traces or content. The original is never mutated. TIFF/BMP need not be previewable by the browser because the generated PNG derivative is the preview contract.

Magick.NET carries a sizeable native footprint but avoids the commercial/split ImageSharp license. MinIO and Magick.NET require no cloud account, payment, or region-specific service. Malware scanning is not claimed; the processing model is extensible for a future self-hosted ClamAV quarantine/scan job.

## Security, consistency, and audit

Permissions are `artwork.read`, `artwork.manage`, `file.read`, `file.manage`, and `file.upload`; system-administrator roles receive them idempotently. All nine Phase 10 tables have EF company filters, same-company composite foreign keys where relationships exist, and PostgreSQL RLS using transaction-local `barang.company_id`. The application role receives SELECT/INSERT/UPDATE only.

Mutable Artwork, ArtworkTag, StoredFile lifecycle, and processing jobs use optimistic versions; stale API changes return 409. Immutable artwork/file versions and derivatives have no update or delete endpoints. Audit covers artwork creation/update/archive/restore/version/tag actions and file upload/version/link/download/archive/processing/thumbnail actions without filenames, storage credentials, bytes, or secrets.

## Configuration and local development

`FileStorage` settings are `Enabled`, `Endpoint`, `AccessKey`, `SecretKey`, `Bucket`, `Secure`, `MaximumUploadBytes`, and `AllowedMediaTypes`. Compose supplies local-only defaults through environment substitution; real deployment secrets must come from the deployment secret mechanism and must not be committed.

- Core API: `http://localhost:8080`
- Backoffice: `http://localhost:5100`
- MinIO API: `http://localhost:9000` (private object API)
- MinIO Console: `http://localhost:9001`

Future Order, OrderItem, Customer, Product, QC, and Reproduction linking requires adding reviewed entity mappings and authorization rules; arbitrary client entity-type strings are never accepted.
