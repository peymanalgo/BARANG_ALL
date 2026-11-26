#!/usr/bin/env bash
# VPS-side: import design-bank storage objects from the repo archives into
# MinIO. Run AFTER `docker compose up -d` (the minio container must be up).
#   bash tools/import-storage.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MINIOContainer="${MINIO_CONTAINER:-barang-core-minio-1}"
PUBLIC_BUCKET="${MINIO_PUBLIC_BUCKET:-barang-public}"
MAIN_BUCKET="${MINIO_BUCKET:-barang-files}"
USER="${MINIO_ROOT_USER:-barang_minio_dev}"
PASS="${MINIO_ROOT_PASSWORD:-barang_minio_dev_only_12345}"

cd "$ROOT/storage"
echo "Reassembling and extracting public derivatives archive..."
cat barang-public.tar.gz.part-* > barang-public.tar.gz
rm -rf .import-public && mkdir -p .import-public
tar -xzf barang-public.tar.gz -C .import-public

echo "Copying into the MinIO container..."
docker cp .import-public/barang-public "$MINIOContainer:/tmp/import-public"

echo "Mirroring into bucket $PUBLIC_BUCKET..."
docker exec "$MINIOContainer" sh -c "mc alias set local http://127.0.0.1:9000 '$USER' '$PASS' >/dev/null && \
  mc mb --ignore-existing local/$PUBLIC_BUCKET && \
  mc mirror --overwrite local/import-public local/$PUBLIC_BUCKET && \
  mc anonymous set download local/$PUBLIC_BUCKET && \
  rm -rf /tmp/import-public && \
  mc ls --recursive local/$PUBLIC_BUCKET | wc -l"

rm -rf .import-public barang-public.tar.gz
echo "Public derivatives import complete."

# Originals (~2.9GB) are too large for git. Transfer them out-of-band first:
#   dev:      bash tools/export-originals.sh        (creates storage/barang-originals.tar.gz)
#   transfer: scp 01_CORE/storage/barang-originals.tar.gz vps:/path/to/repo/01_CORE/storage/
# then uncomment the block below:
# echo "Importing originals..."
# tar -xzf barang-originals.tar.gz -C .
# docker cp ./barang-files "$MINIOContainer:/tmp/import-originals"
# docker exec "$MINIOContainer" sh -c "mc mirror --overwrite local/import-originals local/$MAIN_BUCKET && rm -rf /tmp/import-originals"
# rm -rf ./barang-files
echo "Done."
