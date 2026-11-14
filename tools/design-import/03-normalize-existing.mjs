// ─── فاز ۳-الف: نرمال‌سازی طرح‌های موجود به الگوی acronym ───────────
// کد «AMBULANCE-SSTN-1028213668» → «AMBU-SSTN-1028213668» + دسته‌بندی AMBU
// فقط در محیطی اجرا شود که ایمپورت آزمایشی قبلی با پیشوند نام پوشه داشته باشد.
import fs from "node:fs";
import { execFileSync } from "node:child_process";
import { Api } from "./api.mjs";

const folders = JSON.parse(fs.readFileSync(new URL("./folders.json", import.meta.url), "utf8"));
const api = await new Api().login();
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

const PSQL_CONTAINER = process.env.PSQL_CONTAINER ?? "barang-core-postgres-1";
const PSQL_USER = process.env.PSQL_USER ?? "barang";
const PSQL_DB = process.env.PSQL_DB ?? "barang_core";

// پیشوندهای ایمپورت‌شدهٔ قبلی → acronym پوشه
const prefixToAcronym = new Map();
for (const f of folders) prefixToAcronym.set(f.en.toUpperCase(), f.acronym);

const psql = (sql) =>
  execFileSync("docker", [
    "exec", PSQL_CONTAINER, "psql", "-U", PSQL_USER, "-d", PSQL_DB,
    "-v", "ON_ERROR_STOP=1", "-c", sql,
  ], { encoding: "utf8" });

const SET = `SET app.company_id='${api.companyId}'; SET search_path TO barang;`;

const before = psql(`${SET} SELECT split_part(code,'-SSTN-',1) AS prefix, category_code, count(*) FROM artworks WHERE code LIKE '%-SSTN-%' GROUP BY 1,2 ORDER BY 1;`);
log("پیش از نرمال‌سازی:\n" + before);

let total = 0;
for (const [prefix, acronym] of prefixToAcronym) {
  const oldPattern = `${prefix}-SSTN-%`;
  const out = psql(`${SET} UPDATE artworks SET code = '${acronym}' || substring(code from ${prefix.length + 1}), category_code = '${acronym}' WHERE code LIKE '${oldPattern}' AND category_code LIKE 'SHUT-%';`);
  const n = Number((out.match(/UPDATE (\d+)/) || [0, 0])[1]);
  if (n > 0) log(`${prefix}-SSTN-* → ${acronym}-SSTN-* : ${n} رکورد`);
  total += n;
}
log(`جمع بازنویسی‌شده: ${total}`);

const check = await api.json("GET", "/api/v1/artworks?search=SSTN&pageSize=1");
log(`artworks مطابق SSTN: ${check.total}`);
log("فاز ۳-الف کامل شد.");
