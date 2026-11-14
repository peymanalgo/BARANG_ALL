// ─── فاز ۱: داده‌های پایه استاندارد (زیرگروه‌ها تا برچسب‌ها) ────────
import { Api } from "./api.mjs";
import { REFERENCE_SETS, TAG_SET } from "./reference-data.mjs";

const api = await new Api().login();
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

// ۱) پاک‌سازی مقدار زباله زیرگروه با کد «1»
try {
  const values = await api.json("GET", "/api/v1/lookups/types/artwork.subcategory/values");
  for (const v of values.filter((x) => !/^[A-Z]{3}\d+$/.test(x.code))) {
    const r = await api.req("DELETE", `/api/v1/lookups/values/${v.id}?version=${v.version}`);
    log(`delete junk subcategory "${v.code}":`, r.status);
    if (!r.ok) log("  (deactivate fallback:", await api.json("POST", `/api/v1/lookups/values/${v.id}/activation`, { isActive: false, version: v.version }).then(() => "ok").catch((e) => e.message), ")");
  }
} catch (e) { log("junk subcategory cleanup:", e.message); }

// ۲) برچسب زباله TAG1 «برچسب1» (تخصیص‌هایش از قبل در DB حذف شده)
try {
  const tags = await api.json("GET", "/api/v1/reference-entities/artwork.tag/values");
  for (const t of tags.filter((x) => !/^[A-Z]{3}\d+$/.test(x.code))) {
    const r = await api.req("DELETE", `/api/v1/reference-entities/artwork.tag/values/${t.id}?version=${t.version}`);
    log(`delete junk tag "${t.code}":`, r.status, r.ok ? "" : (await r.text()).slice(0, 200));
  }
} catch (e) { log("junk tag cleanup:", e.message); }

// ۳) ساخت مقادیر استاندارد هر نوع
for (const [typeCode, values] of Object.entries(REFERENCE_SETS)) {
  const existing = await api.json("GET", `/api/v1/lookups/types/${typeCode}/values`);
  const existingCodes = new Set(existing.map((v) => v.code.toUpperCase()));
  let created = 0;
  for (const [i, v] of values.entries()) {
    if (existingCodes.has(v.code)) continue;
    await api.json("POST", `/api/v1/lookups/types/${typeCode}/values`, {
      code: v.code,
      displayOrder: i + 1,
      translations: { fa: v.fa, en: v.en, ar: v.ar },
    });
    created++;
  }
  log(`${typeCode}: +${created} (موجود: ${existingCodes.size})`);
}

// ۴) برچسب‌ها (reference-entity)
const tags = await api.json("GET", "/api/v1/reference-entities/artwork.tag/values");
const tagCodes = new Set(tags.map((t) => t.code.toUpperCase()));
let createdTags = 0;
for (const [i, t] of TAG_SET.entries()) {
  if (tagCodes.has(t.code)) continue;
  await api.json("POST", "/api/v1/reference-entities/artwork.tag/values", {
    code: t.code,
    displayOrder: i + 1,
    translations: { fa: t.fa, en: t.en, ar: t.ar },
    severity: "Minor",
    isBlocking: false,
  });
  createdTags++;
}
log(`artwork.tag: +${createdTags} (موجود: ${tagCodes.size})`);

// ۵) گزارش نهایی
for (const typeCode of [...Object.keys(REFERENCE_SETS)]) {
  const values = await api.json("GET", `/api/v1/lookups/types/${typeCode}/values`);
  log(`✓ ${typeCode}: ${values.length} مقدار`);
}
const finalTags = await api.json("GET", "/api/v1/reference-entities/artwork.tag/values");
log(`✓ artwork.tag: ${finalTags.length} برچسب`);
log("فاز ۱ کامل شد.");
