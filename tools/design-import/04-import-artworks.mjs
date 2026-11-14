// ─── فاز ۳-ب: ایمپورت انبوه تصاویر به بانک طرح ──────────────────────
// هر فایل: ساخت طرح (fa/en/ar) → آپلود JPEG اصلی → پیوست نسخه (thumbnail/web خودکار ساخته می‌شود)
// قابل ازسرگیری (checkpoint)، مدیریت تکرار هش، و گزارش پیشرفت
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { execFileSync } from "node:child_process";
import { Api } from "./api.mjs";
import { CATEGORY_TRANSLATIONS } from "./reference-data.mjs";

// ── تنظیمات ──
const SRC = process.env.IMPORT_SRC ?? "D:/Image/Original Files With 4 Letters Acronym Codes - Copy";
const JPEG_DIR = process.env.IMPORT_JPEG ?? "D:/Image/SSTN ALL JPEG";
const WORKERS = Number(process.env.IMPORT_WORKERS ?? 4);
const LIMIT = Number(process.env.IMPORT_LIMIT ?? 0); // 0 = همه
const STATE_FILE = new URL("./import-state.json", import.meta.url);
const SKIP_FILE = new URL("./skipped-no-jpeg.json", import.meta.url);
const PSQL_CONTAINER = process.env.PSQL_CONTAINER ?? "barang-core-postgres-1";

const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

// ── ساخت فهرست کار ──
const folders = JSON.parse(fs.readFileSync(new URL("./folders.json", import.meta.url), "utf8"));
const tasks = [];
const skippedNoJpeg = [];
for (const folder of folders) {
  const dir = path.join(SRC, folder.folder);
  for (const file of fs.readdirSync(dir)) {
    const m = file.match(/^(SSTN\s+(\d+))\.(eps|jpe?g)$/i);
    if (!m) continue;
    const base = m[1], num = m[2];
    const jpg = path.join(JPEG_DIR, `${base}.jpg`);
    if (!fs.existsSync(jpg)) { skippedNoJpeg.push(`${folder.acronym}/${file}`); continue; }
    tasks.push({ code: `${folder.acronym}-SSTN-${num}`, acronym: folder.acronym, num, jpg });
  }
}
fs.writeFileSync(SKIP_FILE, JSON.stringify(skippedNoJpeg, null, 1));
log(`فایل‌ها: ${tasks.length} قابل ایمپورت · ${skippedNoJpeg.length} بدون JPEG (رد شد)`);

// ── وضعیت ازسرگیری ──
let state = { done: {}, hashes: {} };
if (fs.existsSync(STATE_FILE)) {
  try { state = JSON.parse(fs.readFileSync(STATE_FILE, "utf8")); } catch { /* شروع تازه */ }
}
let stateDirty = false;
const saveState = (force = false) => {
  if (!stateDirty && !force) return;
  fs.writeFileSync(STATE_FILE, JSON.stringify(state));
  stateDirty = false;
};
setInterval(() => saveState(true), 5000).unref();

// ── API و کدهای موجود ──
const api = await new Api().login();
const existing = new Set();
for (let page = 1; ; page++) {
  const r = await api.json("GET", `/api/v1/artworks?includeArchived=true&pageSize=100&pageNumber=${page}`);
  for (const it of r.items) existing.add(it.code.toUpperCase());
  if (page * 100 >= r.total) break;
}
log(`طرح‌های موجود: ${existing.size}`);

// جست‌وجوی طرح بر اساس کد (برای رزومهٔ ناتمام)
async function findArtwork(code) {
  const r = await api.json("GET", `/api/v1/artworks?search=${encodeURIComponent(code)}&includeArchived=true&pageSize=50`);
  return r.items.find((x) => x.code.toUpperCase() === code.toUpperCase()) ?? null;
}

// جایگزین DB برای هش تکراری (بهترین تلاش)
function fileVersionIdByHash(hash) {
  try {
    const out = execFileSync("docker", [
      "exec", PSQL_CONTAINER, "psql", "-U", "barang", "-d", "barang_core", "-t", "-A", "-c",
      `SET app.company_id='${api.companyId}'; SELECT id FROM barang.file_versions WHERE sha256='${hash}' LIMIT 1;`,
    ], { encoding: "utf8", timeout: 15000 });
    const id = out.trim().split("\n").pop();
    return /^[0-9a-f-]{36}$/.test(id) ? id : null;
  } catch { return null; }
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let processed = 0, imported = 0, reused = 0, failedCount = 0;
const startedAt = Date.now();

async function importOne(task, attempt = 1) {
  const tr = CATEGORY_TRANSLATIONS[task.acronym];
  const codeUp = task.code.toUpperCase();
  if (state.done[codeUp] || existing.has(codeUp)) return "skip";

  // ۱) آپلود فایل اصلی
  const buf = fs.readFileSync(task.jpg);
  const hash = crypto.createHash("sha256").update(buf).digest("hex");
  let fileVersionId = state.hashes[hash] ?? null;
  if (!fileVersionId) {
    const fd = new FormData();
    fd.append("file", new Blob([buf], { type: "image/jpeg" }), path.basename(task.jpg));
    fd.append("sha256", hash);
    try {
      const up = await api.json("POST", "/api/v1/files/upload", fd);
      fileVersionId = up.fileVersionId;
    } catch (e) {
      const dup = typeof e.message === "string" && e.message.includes("file.duplicate");
      if (dup) {
        fileVersionId = fileVersionIdByHash(hash);
        if (!fileVersionId) throw new Error(`duplicate hash بدون fileVersionId: ${task.code}`);
      } else throw e;
    }
    if (fileVersionId) { state.hashes[hash] = fileVersionId; stateDirty = true; }
  } else reused++;

  // ۲) ساخت طرح
  let artworkId;
  try {
    const created = await api.json("POST", "/api/v1/artworks", {
      code: task.code,
      title: `${tr.fa} — طرح ${task.num}`,
      description: `طرح ${tr.fa} آمادهٔ چاپ روی تیشرت و پوشاک؛ مناسب برای سفارش‌های تک و تیراژ.`,
      categoryCode: task.acronym,
      designerUserId: null,
      lifecycleStatus: "Active",
      version: 0,
      translations: [
        { culture: "fa", title: `${tr.fa} — طرح ${task.num}`, description: `طرح ${tr.fa} آمادهٔ چاپ روی تیشرت و پوشاک؛ مناسب برای سفارش‌های تک و تیراژ.` },
        { culture: "en", title: `${tr.en} design — ${task.num}`, description: `Print-ready ${tr.en} design for t-shirts and apparel.` },
        { culture: "ar", title: `${tr.ar} — تصميم ${task.num}`, description: `تصميم ${tr.ar} جاهز للطباعة على التيشيرت والملابس.` },
      ],
    });
    artworkId = created.id;
  } catch (e) {
    if (typeof e.message === "string" && e.message.includes("artwork.duplicate_code")) {
      const found = await findArtwork(task.code);
      if (!found) throw e;
      artworkId = found.id;
      if (found.currentVersionId) { state.done[codeUp] = "existed"; stateDirty = true; return "existed"; }
    } else throw e;
  }

  // ۳) پیوست نسخهٔ فایل
  await api.json("POST", `/api/v1/artworks/${artworkId}/versions`, {
    fileVersionId,
    notes: "ایمپورت گروهی بانک طرح",
  });
  state.done[codeUp] = "imported";
  stateDirty = true;
  return "imported";
}

const queue = tasks.slice(0, LIMIT || undefined);
const errors = [];
let cursor = 0;
async function worker() {
  while (true) {
    const i = cursor++;
    if (i >= queue.length) break;
    const task = queue[i];
    try {
      const result = await importOne(task);
      if (result === "imported") imported++;
    } catch (e) {
      // تا سه تلاش با وقفه‌های افزایشی
      let ok = false;
      for (let a = 0; a < 2 && !ok; a++) {
        await sleep(2000 + a * 3000);
        try { await importOne(task, a + 2); ok = true; imported++; } catch { /* تلاش بعدی */ }
      }
      if (!ok) {
        failedCount++;
        errors.push({ code: task.code, error: String(e.message).slice(0, 300) });
        console.error(`✗ ${task.code}: ${String(e.message).slice(0, 200)}`);
      }
    }
    processed++;
    if (processed % 25 === 0) {
      const rate = processed / ((Date.now() - startedAt) / 1000);
      const eta = ((queue.length - processed) / Math.max(rate, 0.01) / 60).toFixed(0);
      log(`… ${processed}/${queue.length} (${imported} ایمپورت, ${failedCount} خطا) · ${rate.toFixed(1)}/ث · ~${eta} دقیقه مانده`);
    }
  }
}
log(`شروع ایمپورت با ${WORKERS} کارگر…`);
await Promise.all(Array.from({ length: WORKERS }, worker));
saveState(true);
fs.writeFileSync(new URL("./import-errors.json", import.meta.url), JSON.stringify(errors, null, 1));
log(`پایان: ${imported} ایمپورت · ${reused} استفادهٔ مجدد هش · ${failedCount} خطا · ${skippedNoJpeg.length} بدون JPEG`);
if (errors.length) log("خطاها در import-errors.json");
process.exit(0);
