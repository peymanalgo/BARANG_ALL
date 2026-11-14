// ─── فاز ۲: ساخت دسته‌بندی‌های طرح از پوشه‌های تصاویر ───────────────
import fs from "node:fs";
import { Api } from "./api.mjs";
import { CATEGORY_TRANSLATIONS } from "./reference-data.mjs";

const folders = JSON.parse(fs.readFileSync(new URL("./folders.json", import.meta.url), "utf8"));
const api = await new Api().login();
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

const existing = await api.json("GET", "/api/v1/lookups/types/artwork.category/values?activeOnly=false");
const byCode = new Map(existing.map((v) => [v.code.toUpperCase(), v]));
log(`موجود: ${existing.length} دسته‌بندی`);

let created = 0, reused = 0, order = 500; // ترتیب نمایش بعد از SHUT-ها
for (const folder of folders) {
  const tr = CATEGORY_TRANSLATIONS[folder.acronym];
  if (!tr) { log(`!! ترجمه ندارد: ${folder.acronym}`); continue; }
  const code = folder.acronym;
  if (byCode.has(code)) { reused++; continue; }
  await api.json("POST", "/api/v1/lookups/types/artwork.category/values", {
    code,
    displayOrder: order++,
    parentId: null,
    translations: { fa: tr.fa, en: tr.en, ar: tr.ar },
  });
  created++;
  if (created % 50 === 0) log(`  … ${created} ساخته شد`);
}
log(`دسته‌بندی جدید: ${created} · موجود از قبل: ${reused}`);

// گزارش
const after = await api.json("GET", "/api/v1/lookups/types/artwork.category/values?activeOnly=false");
const missing = folders.filter((f) => !after.some((v) => v.code.toUpperCase() === f.acronym));
log(`جمع کل دسته‌بندی‌ها: ${after.length} · جامانده: ${missing.length ? missing.map((m) => m.acronym).join(",") : "هیچ"}`);
log("فاز ۲ کامل شد.");
