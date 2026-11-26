#!/usr/bin/env bash
# Dev-side: export the ORIGINAL design files (barang-files bucket, versions
# only) into storage/barang-originals.tar.gz for one-time transfer to the VPS
# (scp). ~2.9GB - not committed to git. Derivatives ship via git (stage 3).
set -euo pipefail
MC="docker exec barang-core-minio-1 mc"
OUT="$(cd "$(dirname "$0")/.." && pwd)/storage"
mkdir -p "$OUT"

$MC alias set local http://127.0.0.1:9000 "${MINIO_ROOT_USER:-barang_minio_dev}" "${MINIO_ROOT_PASSWORD:-barang_minio_dev_only_12345}" >/dev/null

echo "Mirroring originals inside the MinIO container..."
docker exec barang-core-minio-1 sh -c "rm -rf /tmp/export-originals && mc mirror --exclude '**/derivatives/**' local/${MINIO_BUCKET:-barang-files} /tmp/export-originals"

echo "Copying out and packing..."
rm -rf "$OUT/.export-originals"
docker cp barang-core-minio-1:/tmp/export-originals "$OUT/.export-originals"
tar -czf "$OUT/barang-originals.tar.gz" -C "$OUT/.export-originals" barang-files
rm -rf "$OUT/.export-originals"
docker exec barang-core-minio-1 sh -c "rm -rf /tmp/export-originals"

echo "Created $OUT/barang-originals.tar.gz"
