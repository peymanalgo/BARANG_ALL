// ─── فاز ۵: یکدست‌سازی بزرگی/کوچکی کدها (بعد از فازهای ۱ و ۲) ─────
// API مقدارهای lookup را lowercase ذخیره می‌کند؛ برای سازگاری با
// category_code طرح‌ها (بزرگ) و الگوی SHUT، کدها را UPPER می‌کنیم.
import { execFileSync } from "node:child_process";
import { Api } from "./api.mjs";

const api = await new Api().login();
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

const PSQL_CONTAINER = process.env.PSQL_CONTAINER ?? "barang-core-postgres-1";
const PSQL_USER = process.env.PSQL_USER ?? "barang";
const PSQL_DB = process.env.PSQL_DB ?? "barang_core";

const psql = (sql) =>
  execFileSync("docker", [
    "exec", PSQL_CONTAINER, "psql", "-U", PSQL_USER, "-d", PSQL_DB,
    "-v", "ON_ERROR_STOP=1", "-c", sql,
  ], { encoding: "utf8" });

const C = api.companyId;
const out = psql(`
SET search_path TO barang;
UPDATE lookup_values SET code = UPPER(code), updated_at_utc = now()
WHERE company_id='${C}' AND code ~ '^[a-z]{4}$'
  AND lookup_type_id IN (SELECT id FROM lookup_types WHERE code='artwork.category');
UPDATE lookup_values SET code = UPPER(code), updated_at_utc = now()
WHERE company_id='${C}' AND code ~ '^[a-z]{3}[0-9]+$'
  AND lookup_type_id IN (SELECT id FROM lookup_types WHERE code LIKE 'artwork.%');
`);
log("نتیجه:\n" + out.trim());
log("فاز ۵ کامل شد.");
