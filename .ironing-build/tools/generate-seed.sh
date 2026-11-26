#!/usr/bin/env bash
# Generates sql/seed-vps.sql: idempotent INSERT statements that seed a fresh
# BARANG database with the company, its users, RBAC catalog, lookup tree
# (incl. the artwork categories) and basic settings. Run AFTER the API has
# applied its migrations (schema must exist).
set -euo pipefail
PSQL="docker exec -i barang-core-postgres-1 psql -U barang -d barang_core -Atv ON_ERROR_STOP=1"
OUT="$(dirname "$0")/../sql/seed-vps.sql"
COMPANY="${1:-42f0e459-6095-45bb-aa13-f38126d91d9c}"
mkdir -p "$(dirname "$OUT")"
: > "$OUT"

emit() {
  local TBL="$1" WHERE="$2"
  local COLS VALS PK
  COLS=$($PSQL -c "SELECT string_agg(quote_ident(column_name),',' ORDER BY ordinal_position) FROM information_schema.columns WHERE table_schema='barang' AND table_name='$TBL'")
  VALS=$($PSQL -c "SELECT string_agg('quote_nullable('||quote_ident(column_name)||')', '||chr(44)||' ORDER BY ordinal_position) FROM information_schema.columns WHERE table_schema='barang' AND table_name='$TBL'")
  PK=$($PSQL -c "SELECT string_agg(quote_ident(a.attname),',' ORDER BY a.attnum) FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN unnest(con.conkey) AS k(k) ON true JOIN pg_attribute a ON a.attrelid=con.conrelid AND a.attnum=k.k WHERE con.contype='p' AND c.relname='$TBL'")
  echo "-- $TBL" >> "$OUT"
  $PSQL -c "SELECT 'INSERT INTO barang.$TBL ($COLS) VALUES ('||$VALS||') ON CONFLICT ($PK) DO NOTHING;' FROM barang.$TBL WHERE $WHERE" >> "$OUT"
}

{
  echo "-- BARANG VPS seed: company=$COMPANY"
  echo "-- Usage on the VPS (schema must already exist via API auto-migration):"
  echo "--   docker exec -i barang-core-postgres-1 psql -U barang -d barang_core < sql/seed-vps.sql"
  echo "-- Idempotent: safe to run repeatedly (ON CONFLICT DO NOTHING)."
  echo "BEGIN;"
  echo "-- The company-creation triggers seed placeholder catalogs; this file"
  echo "-- carries the real catalogs, so the placeholders would only collide."
  echo "ALTER TABLE barang.companies DISABLE TRIGGER USER;" >> "$OUT"
} > "$OUT"

emit "companies"                     "id='$COMPANY' OR id IN (SELECT DISTINCT company_id FROM barang.lookup_types WHERE company_id IS NOT NULL AND id IN (SELECT lookup_type_id FROM barang.lookup_values WHERE company_id='$COMPANY'))"
emit "company_settings"              "company_id='$COMPANY'"
emit "branches"                      "company_id='$COMPANY'"
emit "lookup_types"                  "company_id IS NULL OR company_id='$COMPANY' OR id IN (SELECT lookup_type_id FROM barang.lookup_values WHERE company_id='$COMPANY')"
emit "lookup_values"                 "company_id IS NULL OR company_id='$COMPANY'"
emit "lookup_value_translations"     "company_id IS NULL OR company_id='$COMPANY'"
emit "work_shifts"                   "company_id='$COMPANY'"
emit "persons"                       "id IN (SELECT person_id FROM barang.application_users WHERE person_id IS NOT NULL AND id IN (SELECT user_id FROM barang.user_companies WHERE company_id='$COMPANY'))"
emit "application_users"             "id IN (SELECT user_id FROM barang.user_companies WHERE company_id='$COMPANY')"
emit "user_companies"                "company_id='$COMPANY'"
emit "roles"                         "TRUE"
emit "permissions"                   "TRUE"
emit "role_permissions"              "TRUE"
emit "user_roles"                    "company_id='$COMPANY'"
emit "artwork_tags"                  "company_id='$COMPANY'"
emit "stored_files"                  "company_id='$COMPANY'"
emit "file_versions"                 "company_id='$COMPANY'"
emit "file_derivatives"              "company_id='$COMPANY'"
emit "artworks"                      "company_id='$COMPANY'"
emit "artwork_versions"              "company_id='$COMPANY'"
emit "file_links"                    "company_id='$COMPANY'"
emit "artwork_tag_assignments"       "company_id='$COMPANY'"
emit "user_permissions"              "company_id='$COMPANY'"

echo "COMMIT;" >> "$OUT"
echo "Generated $OUT ($(grep -c 'INSERT INTO' "$OUT") inserts)."
