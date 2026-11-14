// ─── فاز ۶: تخصیص انبوه اقلام اطلاعاتی به طرح‌ها ────────────────────
// برای هر دسته (acronym): زیرگروه(s) سبک(y) موضوع(t) مخاطب(a)
// مجوز همه: LIC2 استاندارد · کاربرد همه: USG1 چاپ پوشاک
// نوع: EPS→TYP1 برداری · JPG بومی→TYP2 پیکسلی · برچسب‌ها: TAG2 جدید + TAG8 تیراژ
import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { Api } from "./api.mjs";

const SRC = process.env.IMPORT_SRC ?? "D:/Image/Original Files With 4 Letters Acronym Codes - Copy";
const PSQL_CONTAINER = process.env.PSQL_CONTAINER ?? "barang-core-postgres-1";
const api = await new Api().login();
const C = api.companyId;
const log = (...a) => console.log(new Date().toISOString().slice(11, 19), ...a);

const M = {
ERRO:["SGR16","STY17","THM18","AUD10"],ABFA:["SGR20","STY17","THM23","AUD10"],ABVI:["SGR20","STY17","THM23","AUD10"],
AFRI:["SGR10","STY17","THM15","AUD10"],ALUF:["SGR12","STY11","THM23","AUD10"],AMBU:["SGR17","STY1","THM21","AUD5"],
AMIN:["SGR10","STY5","THM15","AUD10"],ANCH:["SGR16","STY5","THM11","AUD10"],ANGE:["SGR9","STY7","THM16","AUD5"],
ANIM:["SGR1","STY11","THM19","AUD10"],ANBR:["SGR1","STY11","THM19","AUD3"],ANFA:["SGR1","STY11","THM22","AUD3"],
ANOR:["SGR1","STY15","THM19","AUD10"],ANSK:["SGR1","STY14","THM19","AUD10"],ANIE:["SGR2","STY11","THM23","AUD4"],
ASTR:["SGR11","STY1","THM9","AUD10"],ATHE:["SGR17","STY15","THM7","AUD1"],BABY:["SGR2","STY11","THM2","AUD6"],
BACK:["SGR4","STY1","THM11","AUD10"],BANA:["SGR15","STY11","THM20","AUD3"],BAWI:["SGR16","STY17","THM7","AUD10"],
BARC:["SGR16","STY14","THM18","AUD10"],BARI:["SGR8","STY11","THM20","AUD10"],BASK:["SGR7","STY1","THM12","AUD10"],
BATT:["SGR4","STY1","THM18","AUD10"],BEHO:["SGR1","STY11","THM20","AUD3"],BEAR:["SGR1","STY11","THM19","AUD3"],
BEMA:["SGR2","STY5","THM22","AUD1"],BIKE:["SGR17","STY1","THM11","AUD10"],BILL:["SGR7","STY1","THM12","AUD10"],
BICL:["SGR4","STY3","THM23","AUD10"],BLHO:["SGR11","STY17","THM9","AUD10"],BODY:["SGR7","STY15","THM12","AUD1"],
BOSC:["SGR4","STY3","THM23","AUD10"],BOLI:["SGR4","STY1","THM17","AUD9"],BOWL:["SGR7","STY1","THM12","AUD10"],
BRAI:["SGR11","STY17","THM17","AUD10"],BRWA:["SGR20","STY17","THM23","AUD10"],BRCA:["SGR17","STY1","THM13","AUD6"],
BRID:["SGR2","STY1","THM13","AUD2"],BROT:["SGR2","STY11","THM2","AUD1"],BUDD:["SGR9","STY7","THM16","AUD5"],
BUFF:["SGR1","STY15","THM19","AUD10"],BULL:["SGR1","STY15","THM12","AUD1"],BURG:["SGR15","STY11","THM20","AUD10"],
BUWB:["SGR20","STY17","THM23","AUD10"],BUSS:["SGR17","STY1","THM11","AUD10"],CACT:["SGR3","STY11","THM8","AUD10"],
CAGE:["SGR4","STY7","THM23","AUD10"],CAKE:["SGR15","STY11","THM13","AUD3"],CALC:["SGR4","STY3","THM17","AUD9"],
CALI:["SGR18","STY5","THM15","AUD10"],CALE:["SGR4","STY3","THM23","AUD10"],CAND:["SGR4","STY6","THM13","AUD6"],
CARI:["SGR17","STY1","THM23","AUD10"],CAWA:["SGR8","STY1","THM21","AUD10"],CATA:["SGR4","STY5","THM10","AUD10"],
CAPA:["SGR19","STY5","THM15","AUD10"],CAVM:["SGR2","STY11","THM15","AUD10"],CEBT:["SGR20","STY17","THM23","AUD10"],
CHPU:["SGR15","STY11","THM20","AUD3"],CHEF:["SGR8","STY11","THM21","AUD10"],CHES:["SGR7","STY1","THM17","AUD10"],
CHOC:["SGR15","STY11","THM20","AUD3"],CHRI:["SGR16","STY5","THM13","AUD6"],CIGA:["SGR16","STY5","THM23","AUD1"],
CIGR:["SGR16","STY5","THM23","AUD1"],CINE:["SGR19","STY5","THM23","AUD10"],CITY:["SGR18","STY17","THM23","AUD10"],
CISK:["SGR18","STY7","THM23","AUD10"],CLCA:["SGR17","STY5","THM15","AUD1"],CLOW:["SGR2","STY11","THM25","AUD3"],
CLON:["SGR1","STY11","THM19","AUD3"],COLO:["SGR18","STY15","THM15","AUD10"],CODI:["SGR4","STY5","THM10","AUD10"],
COPR:["SGR8","STY11","THM18","AUD10"],CONS:["SGR11","STY14","THM9","AUD10"],CRAB:["SGR1","STY11","THM19","AUD3"],
CROC:["SGR19","STY6","THM23","AUD2"],CROO:["SGR1","STY11","THM19","AUD3"],CROS:["SGR9","STY7","THM16","AUD5"],
CRYP:["SGR11","STY17","THM18","AUD10"],CTZZ:["SGR16","STY3","THM18","AUD9"],CUPP:["SGR4","STY3","THM20","AUD10"],
CUPC:["SGR15","STY11","THM20","AUD3"],CUBB:["SGR1","STY11","THM19","AUD3"],CUCA:["SGR1","STY11","THM19","AUD3"],
CUFO:["SGR1","STY11","THM19","AUD3"],DDJJ:["SGR19","STY17","THM10","AUD10"],DANC:["SGR19","STY7","THM10","AUD10"],
DAWS:["SGR16","STY1","THM23","AUD10"],DEER:["SGR1","STY7","THM19","AUD10"],DICE:["SGR4","STY3","THM18","AUD10"],
DINO:["SGR1","STY11","THM23","AUD3"],DOGG:["SGR1","STY11","THM19","AUD3"],DOLA:["SGR16","STY1","THM21","AUD10"],
DOLP:["SGR1","STY15","THM19","AUD10"],DOMI:["SGR7","STY3","THM12","AUD10"],DOOR:["SGR4","STY7","THM23","AUD10"],
DOUG:["SGR15","STY11","THM20","AUD3"],DRAC:["SGR12","STY17","THM24","AUD10"],EAGL:["SGR1","STY7","THM4","AUD10"],
EART:["SGR11","STY17","THM8","AUD10"],ELWA:["SGR20","STY17","THM18","AUD10"],ELEP:["SGR1","STY7","THM19","AUD3"],
EMOJ:["SGR16","STY11","THM25","AUD10"],ESKI:["SGR2","STY11","THM15","AUD10"],FAKE:["SGR16","STY17","THM25","AUD10"],
FASH:["SGR2","STY15","THM22","AUD2"],FATH:["SGR2","STY11","THM2","AUD6"],FIAI:["SGR17","STY15","THM7","AUD1"],
FIRF:["SGR8","STY11","THM21","AUD10"],FLAM:["SGR1","STY7","THM23","AUD2"],FLAS:["SGR16","STY17","THM23","AUD10"],
FOOT:["SGR7","STY1","THM12","AUD10"],FORK:["SGR4","STY3","THM20","AUD10"],FOON:["SGR17","STY15","THM12","AUD1"],
FRFR:["SGR15","STY11","THM20","AUD3"],FROG:["SGR1","STY11","THM19","AUD3"],FUMO:["SGR12","STY11","THM25","AUD3"],
GASS:["SGR11","STY17","THM9","AUD10"],GAMR:["SGR2","STY11","THM18","AUD8"],GESH:["SGR6","STY14","THM23","AUD10"],
GIBO:["SGR4","STY11","THM13","AUD6"],GIRA:["SGR1","STY11","THM19","AUD3"],GLAS:["SGR4","STY3","THM22","AUD10"],
GOLF:["SGR7","STY1","THM12","AUD1"],GRAD:["SGR2","STY1","THM13","AUD9"],GRAR:["SGR19","STY9","THM23","AUD10"],
GRAM:["SGR4","STY5","THM10","AUD10"],GUCA:["SGR15","STY11","THM20","AUD3"],GUBS:["SGR16","STY15","THM7","AUD1"],
HAST:["SGR8","STY11","THM22","AUD2"],HAMS:["SGR1","STY11","THM19","AUD3"],HAND:["SGR2","STY7","THM23","AUD10"],
HANC:["SGR16","STY5","THM7","AUD1"],HAWA:["SGR18","STY5","THM11","AUD10"],HEAD:["SGR4","STY3","THM10","AUD10"],
HEAR:["SGR16","STY17","THM1","AUD10"],HEDG:["SGR1","STY11","THM19","AUD3"],HEVI:["SGR2","STY11","THM23","AUD10"],
HIHE:["SGR4","STY1","THM22","AUD2"],HOBI:["SGR9","STY5","THM16","AUD5"],HOBE:["SGR1","STY11","THM19","AUD3"],
HOOA:["SGR4","STY5","THM23","AUD1"],HOPS:["SGR7","STY11","THM12","AUD3"],HODO:["SGR15","STY11","THM20","AUD3"],
HUBO:["SGR11","STY17","THM17","AUD10"],HUBR:["SGR2","STY11","THM17","AUD9"],ICII:["SGR15","STY11","THM20","AUD3"],
ICSK:["SGR7","STY7","THM12","AUD2"],INCE:["SGR9","STY17","THM16","AUD5"],INFI:["SGR16","STY17","THM23","AUD10"],
JELL:["SGR1","STY17","THM8","AUD10"],JECH:["SGR9","STY15","THM16","AUD5"],JESK:["SGR17","STY15","THM11","AUD10"],
KANG:["SGR1","STY11","THM19","AUD3"],KARA:["SGR7","STY7","THM12","AUD10"],KELO:["SGR4","STY3","THM23","AUD10"],
LASH:["SGR1","STY11","THM19","AUD3"],LAMP:["SGR4","STY3","THM23","AUD10"],LEMO:["SGR15","STY11","THM20","AUD10"],
LIGT:["SGR4","STY3","THM23","AUD1"],LIGO:["SGR18","STY7","THM11","AUD10"],LIPS:["SGR2","STY17","THM22","AUD2"],
LIPT:["SGR4","STY1","THM22","AUD2"],LLAM:["SGR1","STY11","THM19","AUD3"],LOLL:["SGR15","STY11","THM20","AUD3"],
LOVE:["SGR16","STY17","THM1","AUD10"],MAFI:["SGR2","STY5","THM24","AUD1"],MAWI:["SGR16","STY6","THM23","AUD2"],
MAPH:["SGR8","STY11","THM21","AUD10"],MABA:["SGR20","STY17","THM23","AUD10"],MARR:["SGR2","STY5","THM13","AUD6"],
MATC:["SGR4","STY3","THM23","AUD10"],MPGA:["SGR11","STY14","THM17","AUD9"],MAYA:["SGR10","STY17","THM15","AUD10"],
MEDO:["SGR8","STY11","THM21","AUD10"],MERM:["SGR12","STY15","THM23","AUD2"],MEBA:["SGR4","STY15","THM23","AUD1"],
MICR:["SGR11","STY3","THM17","AUD10"],MONE:["SGR1","STY11","THM19","AUD3"],MOTH:["SGR2","STY11","THM2","AUD6"],
MOTO:["SGR17","STY15","THM18","AUD10"],MOAR:["SGR1","STY11","THM23","AUD10"],MOMR:["SGR2","STY15","THM23","AUD1"],
MOWR:["SGR2","STY15","THM23","AUD2"],MOUN:["SGR3","STY7","THM8","AUD10"],MOUS:["SGR1","STY11","THM19","AUD3"],
MUGG:["SGR4","STY3","THM20","AUD10"],MUSH:["SGR3","STY11","THM23","AUD10"],MUIN:["SGR19","STY3","THM10","AUD10"],
MUST:["SGR16","STY5","THM25","AUD10"],NFTT:["SGR11","STY17","THM18","AUD10"],NEGU:["SGR16","STY13","THM6","AUD10"],
NELB:["SGR16","STY13","THM6","AUD10"],NEWP:["SGR4","STY5","THM23","AUD10"],NURS:["SGR8","STY11","THM21","AUD2"],
OCTO:["SGR1","STY11","THM19","AUD3"],OFRC:["SGR17","STY15","THM11","AUD1"],OWLL:["SGR1","STY7","THM19","AUD10"],
PAIT:["SGR8","STY11","THM19","AUD10"],PAII:["SGR19","STY6","THM23","AUD10"],PAND:["SGR1","STY11","THM19","AUD3"],
PACL:["SGR4","STY3","THM21","AUD10"],PACU:["SGR4","STY3","THM20","AUD10"],PARR:["SGR1","STY11","THM19","AUD3"],
PAAI:["SGR17","STY7","THM11","AUD10"],PASA:["SGR15","STY11","THM20","AUD10"],PAFL:["SGR3","STY6","THM8","AUD2"],
PENN:["SGR4","STY3","THM17","AUD9"],PENC:["SGR4","STY3","THM17","AUD9"],PENG:["SGR1","STY11","THM19","AUD3"],
PETA:["SGR11","STY14","THM17","AUD9"],PECA:["SGR5","STY20","THM14","AUD5"],PECR:["SGR19","STY20","THM14","AUD5"],
PEMI:["SGR19","STY20","THM14","AUD5"],PECC:["SGR17","STY15","THM7","AUD1"],PEPA:["SGR2","STY11","THM23","AUD3"],
PIGG:["SGR1","STY11","THM19","AUD3"],PIGE:["SGR1","STY7","THM5","AUD10"],PINN:["SGR16","STY3","THM23","AUD10"],
PINE:["SGR15","STY11","THM20","AUD10"],PIPO:["SGR7","STY1","THM12","AUD10"],PIST:["SGR16","STY15","THM7","AUD1"],
PIZZ:["SGR15","STY11","THM20","AUD10"],POLI:["SGR8","STY11","THM21","AUD1"],POPU:["SGR2","STY17","THM23","AUD10"],
POSE:["SGR9","STY15","THM16","AUD10"],PONO:["SGR4","STY3","THM21","AUD9"],POST:["SGR8","STY11","THM21","AUD10"],
POTT:["SGR3","STY3","THM8","AUD2"],POTA:["SGR15","STY11","THM20","AUD3"],POCH:["SGR15","STY11","THM20","AUD3"],
POJO:["SGR19","STY6","THM14","AUD2"],PRET:["SGR15","STY11","THM20","AUD3"],PROP:["SGR2","STY17","THM1","AUD10"],
QURA:["SGR9","STY20","THM16","AUD5"],RABB:["SGR1","STY11","THM19","AUD3"],RADI:["SGR4","STY5","THM10","AUD10"],
RADO:["SGR16","STY1","THM17","AUD10"],REBI:["SGR16","STY1","THM8","AUD10"],RETI:["SGR11","STY17","THM17","AUD10"],
ROSI:["SGR16","STY3","THM23","AUD10"],ROPS:["SGR7","STY11","THM25","AUD10"],ROCH:["SGR4","STY5","THM23","AUD2"],
ROFC:["SGR19","STY17","THM15","AUD10"],RULE:["SGR4","STY3","THM17","AUD9"],SAIL:["SGR17","STY7","THM11","AUD10"],
SAIO:["SGR2","STY11","THM21","AUD10"],SACL:["SGR2","STY11","THM13","AUD3"],SCHO:["SGR18","STY11","THM17","AUD3"],
SCIS:["SGR4","STY3","THM23","AUD2"],SELF:["SGR2","STY11","THM23","AUD10"],SHAR:["SGR1","STY15","THM19","AUD10"],
SIAL:["SGR2","STY11","THM14","AUD3"],SING:["SGR19","STY11","THM10","AUD10"],SIST:["SGR2","STY11","THM2","AUD2"],
SKEL:["SGR11","STY17","THM24","AUD10"],SKII:["SGR7","STY7","THM12","AUD10"],SNAK:["SGR1","STY15","THM19","AUD10"],
SNLA:["SGR7","STY11","THM12","AUD3"],SNEA:["SGR4","STY1","THM22","AUD10"],SNOM:["SGR2","STY11","THM23","AUD3"],
SOCK:["SGR4","STY11","THM25","AUD10"],SOSS:["SGR16","STY17","THM23","AUD10"],SPAR:["SGR1","STY7","THM19","AUD10"],
SPOO:["SGR4","STY3","THM20","AUD10"],SQUI:["SGR1","STY11","THM19","AUD3"],STSE:["SGR16","STY3","THM21","AUD10"],
STAP:["SGR4","STY3","THM21","AUD9"],STOO:["SGR4","STY3","THM23","AUD10"],STWA:["SGR16","STY17","THM5","AUD10"],
STRA:["SGR15","STY11","THM20","AUD3"],SUMR:["SGR9","STY20","THM14","AUD5"],SUIT:["SGR4","STY5","THM11","AUD10"],
SUTR:["SGR18","STY6","THM11","AUD10"],SUNF:["SGR3","STY6","THM8","AUD2"],SUBO:["SGR7","STY17","THM11","AUD10"],
SUSH:["SGR15","STY11","THM20","AUD10"],SWAN:["SGR1","STY7","THM23","AUD2"],SWIN:["SGR4","STY11","THM2","AUD3"],
TSMO:["SGR4","STY3","THM22","AUD10"],TAEK:["SGR7","STY7","THM12","AUD10"],TANK:["SGR17","STY15","THM7","AUD1"],
TEPO:["SGR4","STY3","THM20","AUD10"],TELE:["SGR11","STY3","THM9","AUD10"],TELV:["SGR4","STY5","THM23","AUD10"],
TENN:["SGR7","STY1","THM12","AUD10"],THLP:["SGR2","STY6","THM23","AUD3"],THOB:["SGR16","STY17","THM6","AUD10"],
THOU:["SGR2","STY7","THM23","AUD5"],TITT:["SGR7","STY3","THM12","AUD3"],TICK:["SGR4","STY5","THM11","AUD10"],
TILE:["SGR6","STY14","THM14","AUD10"],TOPI:["SGR16","STY5","THM23","AUD1"],TOPA:["SGR4","STY11","THM25","AUD10"],
TOCR:["SGR17","STY3","THM21","AUD10"],TRLI:["SGR16","STY3","THM23","AUD10"],TRSI:["SGR16","STY3","THM23","AUD10"],
TRAS:["SGR4","STY11","THM25","AUD10"],TRAR:["SGR3","STY7","THM8","AUD10"],TRUC:["SGR17","STY1","THM21","AUD1"],
TURT:["SGR1","STY11","THM19","AUD3"],UNIC:["SGR12","STY17","THM23","AUD2"],UNAV:["SGR11","STY15","THM18","AUD10"],
VEGA:["SGR16","STY3","THM20","AUD10"],VITA:["SGR4","STY5","THM23","AUD10"],VOLL:["SGR7","STY1","THM12","AUD10"],
WALM:["SGR4","STY5","THM10","AUD10"],WALN:["SGR15","STY15","THM20","AUD10"],WATC:["SGR4","STY3","THM22","AUD10"],
WAGU:["SGR4","STY11","THM25","AUD3"],WAPA:["SGR19","STY6","THM23","AUD2"],WATR:["SGR15","STY11","THM20","AUD3"],
WASM:["SGR16","STY5","THM23","AUD10"],WERI:["SGR16","STY3","THM13","AUD6"],WHAL:["SGR1","STY7","THM8","AUD10"],
WHIS:["SGR2","STY7","THM23","AUD5"],WIFI:["SGR16","STY3","THM18","AUD10"],WINO:["SGR18","STY3","THM23","AUD10"],
WINT:["SGR3","STY6","THM23","AUD6"],WOPH:["SGR8","STY11","THM21","AUD2"],WOBA:["SGR4","STY5","THM23","AUD10"],
WOMA:["SGR18","STY17","THM11","AUD10"],WRES:["SGR7","STY15","THM12","AUD1"],WWWW:["SGR16","STY3","THM18","AUD10"],
YIYA:["SGR16","STY14","THM23","AUD10"],YOGA:["SGR7","STY7","THM23","AUD2"],ZOBS:["SGR11","STY14","THM9","AUD10"],
};

// ── ساخت CSV طرح→دسته‌بندی ──
const folders = JSON.parse(fs.readFileSync(new URL("./folders.json", import.meta.url), "utf8"));
const rows = [["code","sgr","sty","thm","aud","typ"]];
let noMapping = [];
for (const folder of folders) {
  const m = M[folder.acronym];
  if (!m) { noMapping.push(folder.acronym); continue; }
  for (const file of fs.readdirSync(path.join(SRC, folder.folder))) {
    const f = file.match(/^(SSTN\s+(\d+))\.(eps|jpe?g)$/i);
    if (!f) continue;
    rows.push([`${folder.acronym}-SSTN-${f[2]}`, m[0], m[1], m[2], m[3], f[3].toLowerCase().startsWith("e") ? "TYP1" : "TYP2"]);
  }
}
if (noMapping.length) { console.error("نگاشت ناقص:", noMapping.join(",")); process.exit(1); }
const csv = rows.map(r => r.join(",")).join("\n");
fs.writeFileSync("classify.csv", csv);
log(`CSV: ${rows.length - 1} طرح`);

// کپی CSV داخل کانتینر و اجرای SQL
const psql = (sql) => execFileSync("docker", ["exec", "-i", PSQL_CONTAINER, "psql", "-U", "barang", "-d", "barang_core", "-v", "ON_ERROR_STOP=1"], { input: sql, encoding: "utf8", maxBuffer: 1024 * 1024 * 64 });
execFileSync("docker", ["cp", "classify.csv", `${PSQL_CONTAINER}:/tmp/classify.csv`]);

const sql = `
SET search_path TO barang;
CREATE TEMP TABLE classify(code text, sgr text, sty text, thm text, aud text, typ text);
\\copy classify FROM '/tmp/classify.csv' WITH (FORMAT csv, HEADER true)
WITH ids AS (
  SELECT
    (SELECT id FROM lookup_values WHERE company_id='${C}' AND code='LIC2' LIMIT 1) AS lic,
    (SELECT id FROM lookup_values WHERE company_id='${C}' AND code='USG1' LIMIT 1) AS usg
)
UPDATE artworks a SET
  subcategory_id=lv_s.id, style_id=lv_y.id, theme_id=lv_t.id, audience_id=lv_a.id, type_id=lv_p.id,
  license_id=ids.lic, usage_id=ids.usg, updated_at_utc=now()
FROM classify c
CROSS JOIN ids
JOIN lookup_values lv_s ON lv_s.code=c.sgr AND lv_s.company_id='${C}'
JOIN lookup_values lv_y ON lv_y.code=c.sty AND lv_y.company_id='${C}'
JOIN lookup_values lv_t ON lv_t.code=c.thm AND lv_t.company_id='${C}'
JOIN lookup_values lv_a ON lv_a.code=c.aud AND lv_a.company_id='${C}'
JOIN lookup_values lv_p ON lv_p.code=c.typ AND lv_p.company_id='${C}'
WHERE a.code=c.code AND a.company_id='${C}';
`;
const out = psql(sql);
log(out.trim());

// برچسب‌ها: TAG2 جدید + TAG8 مناسب چاپ تیراژ
const tagSql = `
SET search_path TO barang;
INSERT INTO artwork_tag_assignments (id, company_id, artwork_id, tag_id, created_by_user_id, created_at_utc, is_active)
SELECT gen_random_uuid(), a.company_id, a.id, t.id, '2226b36f-32bb-405e-aa8a-0be102f9d434', now(), true
FROM artworks a JOIN artwork_tags t ON t.company_id=a.company_id AND t.code IN ('TAG2','TAG8')
WHERE a.company_id='${C}' AND a.code LIKE '%-SSTN-%'
ON CONFLICT DO NOTHING;
`;
const tagOut = psql(tagSql);
log(tagOut.trim());
log("فاز ۶ کامل شد.");
