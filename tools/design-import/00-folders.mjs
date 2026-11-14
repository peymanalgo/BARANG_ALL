// ─── فاز ۰: تولید folders.json از ساختار پوشه‌های تصاویر ───────────
// هر پوشه به شکل «Name (ACRO), Original Files» است → {folder, acronym, en}
import fs from "node:fs";
import path from "node:path";

const SRC = process.env.IMPORT_SRC ?? "D:/Image/Original Files With 4 Letters Acronym Codes - Copy";
const OUT = new URL("./folders.json", import.meta.url);

const out = [];
for (const f of fs.readdirSync(SRC).sort()) {
  const full = path.join(SRC, f);
  if (!fs.statSync(full).isDirectory()) continue;
  const m = f.match(/^(.*?)\s*\(([A-Z0-9]{4})\)/);
  if (!m) { console.log("UNPARSED:", f); continue; }
  out.push({ folder: f, acronym: m[2], en: m[1].trim() });
}
fs.writeFileSync(OUT, JSON.stringify(out, null, 1));
const acro = {};
out.forEach((o) => { acro[o.acronym] = (acro[o.acronym] || 0) + 1; });
const dup = Object.entries(acro).filter(([, n]) => n > 1);
console.log(`پوشه‌ها: ${out.length} · تکراری: ${dup.length ? JSON.stringify(dup) : "هیچ"}`);
