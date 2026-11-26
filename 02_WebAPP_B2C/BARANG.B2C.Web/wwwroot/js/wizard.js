// ویزارد سفارش چاپ نسخه ۳ — استودیوی طراحی: تیشرت همیشه مرکز، ابزارها آیکن‌های شناور
// دور صحنه که با کلیک پنل می‌دهند؛ لایه‌های طرح/متن مستقل با درگ و سنترینگ؛ موتور ممیزی
// ERP (قوانین ممیزی) قبل از ادامه اجرا می‌شود؛ همه داده‌ها و قیمت‌ها از Core.
(() => {
    "use strict";
    const $ = (id) => document.getElementById(id);
    const root = document.getElementById("wz");
    if (!root) return;
    const I18N = JSON.parse($("wz-i18n").textContent);
    const BOOT = JSON.parse($("wz-bootstrap").textContent);
    const T = (k) => I18N[k] ?? k;
    const CULTURE = root.dataset.culture || "fa";
    const FA = CULTURE === "fa";
    const fa = (n) => String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[d]);
    const num = (n) => (CULTURE === "fa" || CULTURE === "ar" ? fa(n) : String(n));
    const SEP = (FA || CULTURE === "ar") ? "\u066C" : ",";
    const money = (n) => num(String(Math.round(Number(n) || 0)).replace(/\B(?=(\d{3})+(?!\d))/g, SEP));
    const AF = (() => {
        const el = document.querySelector("#wz-af input[name='__RequestVerificationToken']");
        return el ? el.value : "";
    })();
    const postJson = (url, body) => fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": AF },
        body: JSON.stringify(body),
    });

    // ابعاد فیزیکی/نمایشی ناحیه چاپ؛ قیمت‌ها از سرور
    const SZ = { A3: { w: 30, h: 42 }, A4: { w: 21, h: 29.7 }, A5: { w: 14, h: 21 } };
    const szCm = (code) => SZ[(code || "").toUpperCase()] || SZ.A4;
    const SZPX = { A3: { w: 116, h: 164 }, A4: { w: 82, h: 116 }, A5: { w: 56, h: 79 } };
    const szPx = (code) => SZPX[(code || "").toUpperCase()] || SZPX.A4;
    // لبه بالایی ناحیه چاپ: حدود ۴ انگشت (۸ سانت) زیر یقه — یکسان در جلو و پشت
    const COLLAR_TOP = 108;
    const HEX = [
        [/سفید|أبيض|white|blanco/i, "#f2f3f1"], [/مشکی|سیاه|أسود|black|negro/i, "#1a1a1a"], [/شیری|کرم|cream|beige|crema/i, "#e8ddc8"],
        [/قرمز|زرشکی|أحمر|red|crimson|burgundy|rojo/i, "#8e2323"], [/آبی|أزرق|blue|azul/i, "#3f6d9e"], [/سبز|أخضر|green|verde/i, "#2d5a3d"],
        [/زرد|أصفر|yellow|amarillo/i, "#dfb545"], [/نارنجی|برتقالي|orange|naranja/i, "#d97b3f"], [/خاکستری|طوسی|رمادي|gray|grey|gris/i, "#8a8f98"],
        [/صورتی|وردي|pink|rosa/i, "#d98ba3"], [/بنفش|بنفسجي|purple|violet|morado/i, "#6d5a9e"],
    ];
    const hexOf = (name, code) => {
        for (const [re, hex] of HEX) if (re.test(name || "") || re.test(code || "")) return hex;
        return "#9aa2ad";
    };
    const COLOR_NAMES = {
        black: { fa: "مشکی", ar: "أسود", en: "Black", es: "Negro" }, blue: { fa: "آبی", ar: "أزرق", en: "Blue", es: "Azul" },
        red: { fa: "قرمز", ar: "أحمر", en: "Red", es: "Rojo" }, white: { fa: "سفید", ar: "أبيض", en: "White", es: "Blanco" },
        cream: { fa: "شیری", ar: "كريمي", en: "Cream", es: "Crema" }, green: { fa: "سبز", ar: "أخضر", en: "Green", es: "Verde" },
        yellow: { fa: "زرد", ar: "أصفر", en: "Yellow", es: "Amarillo" }, orange: { fa: "نارنجی", ar: "برتقالي", en: "Orange", es: "Naranja" },
        gray: { fa: "خاکستری", ar: "رمادي", en: "Gray", es: "Gris" }, grey: { fa: "خاکستری", ar: "رمادي", en: "Gray", es: "Gris" },
        pink: { fa: "صورتی", ar: "وردي", en: "Pink", es: "Rosa" }, purple: { fa: "بنفش", ar: "بنفسجي", en: "Purple", es: "Morado" },
    };
    const colorName = (code) => {
        const c = (CONFIG.colors || []).find((x) => (x.code || "").toUpperCase() === String(code || "").toUpperCase());
        if (c && c.name && c.name.trim() && c.name.trim().toUpperCase() !== String(code || "").trim().toUpperCase()) return c.name;
        const m = COLOR_NAMES[String(code || "").trim().toLowerCase()];
        if (m) return m[CULTURE] || m.en || m.fa;
        return (c && c.name) || code || "—";
    };
    const TEXT_COLORS = ["#1c1917", "#ffffff", "#f6b23c", "#e11d48", "#2563eb", "#16a34a", "#8b5cf6", "#e879f9"];

    // ── وضعیت ──
    let CONFIG = null;
    const W = {
        phase: 1,
        mode: null,             // bank | upload | text
        bank: null,             // {id,title,preview,code,widthPx,heightPx}
        file: null, fileMeta: null, // fileMeta {url,width,height}
        text: "", font: 1, textColor: "#1c1917", textSize: 100,
        textX: 0.5, textY: 0.5, textPos: "mid",
        audit: null,            // null=بررسی‌نشده | {passed,results} | {running:true} | {empty:true}
        position: "FRONT", printSize: null,
        product: null, variant: null, colorCode: null, sizeCode: null,
        packaging: null, addressId: null, newAddress: false, gift: false,
        orderId: null, paidWith: null,
    };
    try {
        const q = new URLSearchParams(location.search);
        if (q.get("new") === "1" || (!q.get("design") && !location.pathname.includes("design"))) sessionStorage.removeItem("wz.state");
        else Object.assign(W, JSON.parse(sessionStorage.getItem("wz.state") || "{}"));
    } catch { /* شروع تازه */ }
    if (BOOT.flags) {
        if (BOOT.flags.upload === false && W.mode === "upload") W.mode = null;
        if (BOOT.flags.gift === false) W.gift = false;
    }
    const save = () => {
        try { sessionStorage.setItem("wz.state", JSON.stringify(W)); } catch { /* private mode */ }
        const chip = $("w-autosave");
        if (chip) {
            chip.hidden = false;
            clearTimeout(window.__wzAutosave);
            window.__wzAutosave = setTimeout(() => { chip.hidden = true; }, 1600);
        }
    };
    const invalidateAudit = () => { W.audit = null; updateAuditUI(); };

    // ── تصاویر واقعی تیشرت ──
    const TEE_IMAGES = {
        black: { FRONT: "/images/tee/black-front.jpg", BACK: "/images/tee/black-back.jpg", teeC: "#1a1a1a" },
        white: { FRONT: "/images/tee/white-front.jpg", BACK: "/images/tee/white-back.jpg", teeC: "#f2f3f1" },
        red: { FRONT: "/images/tee/red-front.jpg", BACK: "/images/tee/red-back.jpg", teeC: "#8e2323" },
        blue: { FRONT: "/images/tee/blue-front.jpg", BACK: "/images/tee/blue-back.jpg", teeC: "#2c4e7e" },
    };
    const teeKey = () => { if (!CONFIG) return null; const n = colorName(W.colorCode) + " " + (W.colorCode || ""); const m = /آبی|أزرق|blue|azul/i.test(n) ? "blue" : /سفید|أبيض|white|blanco/i.test(n) ? "white" : /قرمز|سرخ|أحمر|red|rojo/i.test(n) ? "red" : /مشکی|سیاه|أسود|black|negro/i.test(n) ? "black" : null; return m; };
    function tee(side) {
        const curVariant = (CONFIG && (CONFIG.products || []).flatMap(p => p.variants || []).find(v => v.id === (W.variant && W.variant.id)));
        if (side === "FRONT" && curVariant && curVariant.imageUrl) return '<img class="tee-img" src="' + curVariant.imageUrl + '" alt="" draggable="false">';
        const k = teeKey();
        if (k) return '<img class="tee-img" src="' + TEE_IMAGES[k][side] + '" alt="" draggable="false">';
        const col = side === "FRONT" ? "#151515" : "#d7d2c4";
        const collar = side === "FRONT" ? "M96 24 Q150 62 204 24" : "M96 24 Q150 40 204 24";
        const seams = '<g stroke="' + col + '" stroke-width="1.6" stroke-dasharray="4 4" opacity=".4" fill="none"><path d="M74 108 L74 314"/><path d="M226 108 L226 314"/><path d="M78 316 Q150 334 222 316"/></g>';
        const band = '<path d="' + collar + '" fill="none" stroke="' + col + '" stroke-width="6" opacity=".55" stroke-linecap="round"/>';
        const seam = side === "BACK" ? '<line x1="150" y1="44" x2="150" y2="326" stroke="' + col + '" stroke-width="1.4" stroke-dasharray="3 5" opacity=".3"/>' : "";
        return '<svg viewBox="0 0 300 340" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="' + collar + ' L236 36 L294 94 L256 130 L228 108 L228 318 Q150 342 72 318 L72 108 L44 130 L6 94 L64 36 Z" fill="var(--teeC)" stroke="' + col + '" stroke-width="2.4"/>' + band + seams + seam + '</svg>';
    }
    const EMPTY_HTML = '<div class="empty"><span class="eic"></span><br>' + T("emptyStage") + '</div>';

    function buildFace(el, side) {
        el.innerHTML = tee(side) + '<div class="prect" data-lbl=""></div>' +
            '<div class="laywrap lay-design" data-lay="design"></div>' +
            '<div class="laywrap lay-text" data-lay="text"></div>' +
            '<div class="selbox" data-sel hidden><span class="tag"></span></div>';
        return { el, side, rect: el.querySelector(".prect"), dWrap: el.querySelector(".lay-design"), tWrap: el.querySelector(".lay-text"), sel: el.querySelector(".selbox") };
    }
    const fF = buildFace($("w-ff"), "FRONT"), fB = buildFace($("w-fb"), "BACK");
    const setTeeColor = (hex) => { $("w-ff").style.setProperty("--teeC", hex); $("w-fb").style.setProperty("--teeC", hex); };
    const refreshTee = () => {
        applyGarmTransform();
        for (const f of [fF, fB]) {
            const cur = f.el.querySelector(".tee-img") || f.el.querySelector("svg");
            if (!cur) continue;
            const t = document.createElement("template");
            t.innerHTML = tee(f.side).trim();
            const next = t.content.firstChild;
            if (next) cur.replaceWith(next);
        }
    };
    const SIZE_SCALE = { s: .82, m: .9, l: 1, xl: 1.08, xxl: 1.16 };
    const sizeScale = () => SIZE_SCALE[String((W.variant && W.variant.sizeCode) || "").trim().toLowerCase()] || 1;
    let zoomLevel = 1;
    const applyGarmTransform = () => {
        const g = $("w-garm");
        if (!g) return;
        const s = sizeScale();
        g.style.width = (82 * s) + "%";
        g.style.maxWidth = Math.round(380 * s) + "px";
        const flip = W.position === "BACK" ? "rotateY(180deg) " : "";
        g.style.transform = flip + "scale(" + zoomLevel + ")";
        g.style.transition = "transform .3s cubic-bezier(.25,1,.3,1)";
    };
    window.addEventListener("resize", () => { clearTimeout(window.__wrz); window.__wrz = setTimeout(() => { applyGarmTransform(); renderPreview(); }, 150); });

    const activeFace = () => W.position === "FRONT" ? fF : fB;
    const hasDesign = () => W.mode === "bank" ? !!W.bank : W.mode === "upload" ? !!W.fileMeta : false;
    const hasText = () => (W.text || "").trim().length > 0;
    const overlayText = () => W.mode !== "text" && hasText();
    function designName() {
        if (W.mode === "bank" && W.bank) return shortTitle(W.bank.title);
        if (W.mode === "upload" && W.fileMeta) return T("yourDesign");
        if (W.mode === "text" && W.text) return "«" + W.text.slice(0, 14) + "»";
        return null;
    }
    function renderPreview() {
        const S = szPx(W.printSize), C = szCm(W.printSize);
        const short = FA ? fa(C.w) + "×" + fa(C.h) : C.w + "×" + C.h;
        for (const f of [fF, fB]) {
            const active = f.side === W.position;
            f.el.style.visibility = active ? "visible" : "hidden";
            if (!active) continue;
            f.rect.style.left = (150 - S.w / 2) + "px";
            f.rect.style.top = COLLAR_TOP + "px";
            f.rect.style.width = S.w + "px";
            f.rect.style.height = S.h + "px";
            f.rect.setAttribute("data-lbl", (W.printSize || "") + " · " + short + " " + T("cm"));
            // لایه طرح
            const d = f.dWrap;
            if (hasDesign()) {
                const src = W.mode === "bank" ? W.bank.preview : W.fileMeta.url;
                d.style.display = "";
                d.style.left = f.rect.style.left; d.style.top = f.rect.style.top;
                d.style.width = S.w + "px"; d.style.height = S.h + "px";
                d.innerHTML = '<div class="artbox bare"><img src="' + src + '" alt="" draggable="false"></div>';
            } else { d.style.display = "none"; d.innerHTML = ""; }
            // لایه متن
            const t = f.tWrap;
            if (overlayText()) {
                const sizePx = Math.max(12, Math.round(W.textSize / 100 * S.w * 0.30));
                const style = "color:" + W.textColor + ";font-size:" + sizePx + "px;" +
                    (W.font === 3 ? "font-style:italic;" : "") + (W.font === 2 ? "letter-spacing:.08em;font-weight:500;" : W.font === 4 ? "letter-spacing:.25em;" : "font-weight:900;");
                t.style.display = "";
                t.style.left = (150 - S.w / 2 + W.textX * S.w) + "px";
                t.style.top = (COLLAR_TOP + W.textY * S.h) + "px";
                t.style.width = S.w * 0.92 + "px";
                t.innerHTML = '<div class="txt" dir="auto" style="' + style + '"><span class="tt">' + escapeHtml(W.text) + "</span></div>";
            } else { t.style.display = "none"; t.innerHTML = ""; }
        }
        renderSelection();
        renderLayers();
    }
    function renderSelection() {
        for (const f of [fF, fB]) {
            if (f.side !== W.position) { f.sel.hidden = true; continue; }
            const sel = window.__psSel;
            const wrap = sel === "design" ? f.dWrap : sel === "text" ? f.tWrap : null;
            if (!wrap || wrap.style.display === "none") { f.sel.hidden = true; continue; }
            f.sel.hidden = false;
            f.sel.style.left = wrap.style.left; f.sel.style.top = wrap.style.top;
            f.sel.style.width = wrap.style.width; f.sel.style.height = wrap.style.height;
            f.sel.querySelector(".tag").textContent = sel === "text" ? T("layersText") : T("layersDesign");
        }
    }

    // ── درگ لایه متن با سنترینگ به مرکز ──
    (function () {
        let drag = null;
        root.addEventListener("pointerdown", (e) => {
            const wrap = e.target.closest(".laywrap");
            if (!wrap || wrap.style.display === "none") return;
            const kind = wrap.classList.contains("lay-text") ? "text" : "design";
            const face = wrap.parentElement === fF.el ? fF : fB;
            if (face.side !== W.position) return;
            window.__psSel = kind;
            renderSelection();
            if (kind !== "text") return;
            drag = { face, sx: e.clientX, sy: e.clientY, ox: W.textX, oy: W.textY, moved: false };
            e.preventDefault();
        });
        window.addEventListener("pointermove", (e) => {
            if (!drag) return;
            const S = szPx(W.printSize);
            const stage = drag.face.rect.getBoundingClientRect();
            if (stage.width < 10) return;
            let nx = drag.ox + (e.clientX - drag.sx) / stage.width;
            let ny = drag.oy + (e.clientY - drag.sy) / stage.height;
            nx = Math.max(0.08, Math.min(0.92, nx));
            ny = Math.max(0.08, Math.min(0.92, ny));
            if (Math.abs(nx - 0.5) < 0.03) nx = 0.5;
            const snap = [textYFor("top"), textYFor("mid"), textYFor("bot")];
            for (const sy of snap) if (Math.abs(ny - sy) < 0.035) ny = sy;
            W.textX = nx; W.textY = ny;
            renderPreview();
            drag.moved = true;
        });
        window.addEventListener("pointerup", () => {
            if (drag && drag.moved) { save(); invalidateAudit(); }
            drag = null;
        });
    })();

    // ── پیکربندی از سرور ──
    async function loadConfig() {
        const r = await fetch("/order/wizard?handler=Config", { headers: { "Accept": "application/json" } });
        CONFIG = await r.json();
        if (!CONFIG || !Array.isArray(CONFIG.products)) CONFIG = { products: [], printSizes: [], packaging: [], colors: [], shipping: 0, currencyCode: "IRT" };
        if (!W.product && CONFIG.products.length) {
            W.product = CONFIG.products[0].id;
            const colors = productVariants(CONFIG.products[0]).map((v) => v.colorCode).filter(Boolean);
            if (colors.length) W.colorCode = colors[0];
        }
        if (!W.packaging && CONFIG.packaging.length) W.packaging = CONFIG.packaging[0].code;
        pickVariant();
        renderPrintSizes(); renderPackaging(); renderProducts(); renderSizes(); renderQty();
        if (!W.printSize && CONFIG.printSizes.length) W.printSize = CONFIG.printSizes[Math.min(1, CONFIG.printSizes.length - 1)].code;
        renderPrintSizes();
        invalidateAudit();
        renderBar(); renderPreview(); quote();
    }

    function renderPrintSizes() {
        const box = $("w-code");
        if (!box) return;
        box.innerHTML = "";
        (CONFIG.printSizes || []).forEach((s) => {
            const b = document.createElement("button");
            b.type = "button"; b.textContent = s.name || s.code; b.title = s.description || "";
            if (s.code === W.printSize) b.classList.add("on");
            b.addEventListener("click", () => {
                W.printSize = s.code;
                box.querySelectorAll("button").forEach((x) => x.classList.remove("on"));
                b.classList.add("on");
                renderPreview(); renderBar(); quote(); invalidateAudit();
            });
            box.appendChild(b);
        });
    }
    function renderPackaging() {
        const box = $("w-pack");
        if (!box) return;
        box.innerHTML = "";
        (CONFIG.packaging || []).forEach((p) => {
            const c = document.createElement("div");
            c.className = "pick" + (W.packaging === p.code ? " on" : "");
            c.innerHTML = "<b></b><small>" + (p.code === "GIFT" || p.code === "gift" ? T("giftDetails") : T("packaging")) + "</small>" +
                (p.price > 0 ? '<div class="pr">+' + money(p.price) + " " + T("toman") + "</div>" : '<div class="pr" style="color:#8ff0c4">' + T("free") + "</div>");
            c.querySelector("b").textContent = p.name;
            c.addEventListener("click", () => { W.packaging = p.code; box.querySelectorAll(".pick").forEach((x) => x.classList.remove("on")); c.classList.add("on"); });
            box.appendChild(c);
        });
    }
    function productVariants(p) { return (p && p.variants) ? p.variants : []; }
    function pickVariant() {
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        if (!p) { W.variant = null; return; }
        let vs = productVariants(p);
        if (W.colorCode) {
            const byColor = vs.filter((v) => (v.colorCode || "").toUpperCase() === W.colorCode.toUpperCase());
            if (byColor.length) vs = byColor;
        }
        if (W.sizeCode) {
            const bySize = vs.filter((v) => (v.sizeCode || "").toUpperCase() === W.sizeCode.toUpperCase());
            if (bySize.length) vs = bySize;
        }
        W.variant = vs[0] || null;
        if (W.variant && W.variant.colorCode) W.colorCode = W.variant.colorCode;
        setTeeColor(hexOf(colorName(W.colorCode), W.colorCode));
        refreshTee();
    }
    const SHIRT_PATH = "M96 24 Q150 62 204 24 L236 36 L294 94 L256 130 L228 108 L228 318 Q150 342 72 318 L72 108 L44 130 L6 94 L64 36 Z";
    const shirtIcon = (hex) => '<svg viewBox="0 0 300 340" class="teeic" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
        '<path d="' + SHIRT_PATH + '" fill="' + hex + '" stroke="rgba(255,255,255,.30)" stroke-width="8"/>' +
        '<path d="M96 24 Q150 62 204 24" fill="none" stroke="rgba(0,0,0,.30)" stroke-width="10" stroke-linecap="round" opacity=".45"/></svg>';
    const productHex = (p) => {
        const v = productVariants(p)[0];
        const c = v && (CONFIG.colors || []).find((x) => x.code === v.colorCode);
        return hexOf(c ? c.name : "", v ? v.colorCode : "");
    };
    function renderProducts() {
        const box = $("w-products");
        if (!box) return;
        box.innerHTML = "";
        (CONFIG.products || []).forEach((p) => {
            const prices = productVariants(p).map((v) => v.price).filter((x) => x != null);
            const price = p.salesPrice ?? (prices.length ? Math.min(...prices) : null);
            const c = document.createElement("div");
            c.className = "pcard" + (W.product === p.id ? " on" : "");
            c.innerHTML = '<span class="ic">' + shirtIcon(productHex(p)) + "</span><b></b>" +
                '<div class="pr">' + (price != null ? money(price) + " " + T("toman") : "—") + "</div>";
            c.querySelector("b").textContent = p.name;
            c.addEventListener("click", () => {
                W.product = p.id;
                W.sizeCode = null;
                const colors = productVariants(p).map((v) => v.colorCode).filter(Boolean);
                if (colors.length) W.colorCode = colors[0];
                pickVariant(); renderSizes(); renderProducts(); renderBar(); quote(); invalidateAudit();
            });
            box.appendChild(c);
        });
    }
    function renderSizes() {
        const stockOf = (sz) => { const p = (CONFIG.products || []).find(x => x.id === W.product); const v = (p && p.variants || []).find(v => (v.sizeCode || "").toUpperCase() === String(sz).toUpperCase()); return v ? (v.stock ?? null) : null; };
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        const box = $("w-ssize");
        if (!box) return;
        box.innerHTML = "";
        let vs = productVariants(p);
        if (W.colorCode) { const byColor = vs.filter((v) => (v.colorCode || "").toUpperCase() === W.colorCode.toUpperCase()); if (byColor.length) vs = byColor; }
        const ORDER = ["S", "M", "L", "XL", "XXL"];
        const sizes = [...new Set(vs.map((v) => (v.sizeCode || "").trim()).filter(Boolean))]
            .sort((a, b) => {
                const ia = ORDER.indexOf(a.toUpperCase()), ib = ORDER.indexOf(b.toUpperCase());
                if (ia >= 0 && ib >= 0) return ia - ib;
                if (ia >= 0) return -1;
                if (ib >= 0) return 1;
                return a.localeCompare(b);
            });
        sizes.forEach((s) => {
            const b = document.createElement("button");
            b.type = "button"; b.textContent = s;
            const st = stockOf(s);
            if (st != null) { b.title = (st > 0 ? T("stockAvail") + ": " + num(st) + " " + T("qtyUnit") : T("stockOut")); if (st <= 0) { b.disabled = true; b.style.opacity = ".45"; } }
            if (s === (W.variant && W.variant.sizeCode)) b.classList.add("on");
            b.addEventListener("click", () => { if (b.disabled) return; W.sizeCode = s; pickVariant(); renderSizes(); renderQty(); renderBar(); quote(); invalidateAudit(); });
            box.appendChild(b);
        });
        const chip = $("w-stockchip");
        const cur = W.variant;
        if (chip) {
            chip.className = "stock-chip" + (cur && cur.stock === 0 ? " out" : "");
            chip.hidden = !cur || cur.stock == null;
            chip.textContent = cur && cur.stock != null ? (cur.stock > 0 ? T("stockAvail") + ": " + num(cur.stock) + " " + T("qtyUnit") : T("stockOut")) : "";
        }
    }
    W.qty = W.qty && W.qty > 0 ? Math.min(W.qty, 50) : 1;
    const qtyMax = () => { const st = W.variant && W.variant.stock; return st != null && st > 0 ? Math.min(st, 50) : 50; };
    function renderQty() {
        const out = $("w-qty-out"), hint = $("w-qty-stock");
        if (!out) return;
        W.qty = Math.max(1, Math.min(W.qty, qtyMax()));
        out.textContent = num(W.qty);
        const st = W.variant && W.variant.stock;
        hint.textContent = st != null && st > 0 ? "(" + num(st) + " " + T("qtyUnit") + ")" : "";
    }
    const qtyMinus = $("w-qty-minus"), qtyPlus = $("w-qty-plus");
    if (qtyMinus) qtyMinus.addEventListener("click", () => { W.qty--; renderQty(); renderBar(); quote(); save(); });
    if (qtyPlus) qtyPlus.addEventListener("click", () => { W.qty++; renderQty(); renderBar(); quote(); save(); });

    // ── قیمت زنده (سمت سرور) ──
    let quoteTimer = null, quoteSeq = 0;
    function quote() {
        clearTimeout(quoteTimer);
        const priceEl = $("w-price");
        if (!W.variant || !W.printSize) { if (priceEl) priceEl.textContent = "—"; return; }
        quoteTimer = setTimeout(async () => {
            const seq = ++quoteSeq;
            try {
                const r = await postJson("/order/wizard?handler=Quote", { productVariantId: W.variant.id, printSizeCode: W.printSize, printPosition: W.position, packagingTypeCode: W.packaging, quantity: W.qty || 1 });
                const data = await r.json();
                if (seq !== quoteSeq) return;
                if (!data.ok) { if (seq === quoteSeq) priceEl.textContent = "—"; return; }
                priceEl.textContent = money(data.quote.total);
            } catch { if (seq === quoteSeq) priceEl.textContent = "—"; }
        }, 320);
    }
    const shortTitle = (raw) => {
        const str = String(raw || "");
        const m = str.match(/^(.*?)\s*[—–-]+\s*طرح\s*(\d{4,})$/);
        if (!m) return str;
        const digits = m[2];
        const tail = digits.length > 4 ? digits.slice(-4) : digits;
        return m[1].trim() + " · " + num("#" + tail);
    };
    const escapeHtml = (v) => String(v).replace(/</g, "&lt;").replace(/&/g, "&amp;");
    // سه گزینه موقعیت متن: با طرح = بالای طرح / روی طرح (وسط طرح) / پایین طرح؛ بدون طرح = بالا/وسط/پایین ناحیه چاپ
    function textYFor(pos) {
        const S = szPx(W.printSize);
        if (!hasDesign()) return pos === "top" ? 0.16 : pos === "bot" ? 0.82 : 0.5;
        const iw = W.mode === "upload" ? (W.fileMeta ? W.fileMeta.width : 1) : (W.bank ? W.bank.widthPx || 1 : 1);
        const ih = W.mode === "upload" ? (W.fileMeta ? W.fileMeta.height : 1) : (W.bank ? W.bank.heightPx || 1 : 1);
        const sc = Math.min(S.w / Math.max(iw, 1), S.h / Math.max(ih, 1));
        const dh = S.h * sc, dy = (S.h - dh) / 2;
        if (pos === "top") return Math.max(0.06, (dy / 2) / S.h);
        if (pos === "bot") return Math.min(0.94, (dy + dh + (S.h - dh) / 2) / S.h);
        return (dy + dh / 2) / S.h;
    }
    document.querySelectorAll("#w-txpos button").forEach((b) => {
        b.addEventListener("click", () => {
            document.querySelectorAll("#w-txpos button").forEach((x) => x.classList.remove("on"));
            b.classList.add("on");
            W.textPos = b.dataset.pos;
            W.textY = textYFor(W.textPos || "mid");
            if (!W.text) { W.text = T("sampleText"); $("w-textin").value = W.text; }
            invalidateAudit(); save(); renderPreview(); renderBar(); quote();
        });
    });
    function renderBar() {
        const sum = $("w-sum");
        if (!sum) return;
        const parts = [];
        const dn = designName();
        if (dn) parts.push(T("noDesign") + ": <b>" + escapeHtml(dn) + "</b>");
        if (overlayText()) parts.push(T("layersText") + ": <b>«" + escapeHtml(W.text.slice(0, 12)) + "»</b>");
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        if (p) parts.push(T("product") + ": <b>" + escapeHtml(p.name) + "</b>");
        if (W.variant && W.variant.sizeCode) parts.push(T("shirtSize") + ": <b>" + W.variant.sizeCode + "</b>");
        if (W.colorCode) parts.push(T("color") + ": <b>" + escapeHtml(colorName(W.colorCode)) + "</b>");
        if ((W.qty || 1) > 1) parts.push(T("qty") + ": <b>" + num(W.qty) + " " + T("qtyUnit") + "</b>");
        sum.innerHTML = parts.length ? parts.join('<span class="v3sep">·</span>') : T("sumEmpty");
        $("w-next1").disabled = !(dn && W.product && W.variant && W.printSize);
        updateAuditUI();
    }

    function measureImage(url) {
        return new Promise((resolve) => {
            const im = new Image();
            im.onload = () => resolve({ w: im.naturalWidth, h: im.naturalHeight });
            im.onerror = () => resolve({ w: 0, h: 0 });
            im.src = url;
        });
    }
    function pickBankDesign(d) {
        W.mode = "bank";
        W.bank = { id: d.id, title: d.title || d.code, preview: d.previewUrl || d.thumbnailUrl, code: d.code };
        invalidateAudit();
        save(); renderPreview(); renderBar(); quote();
        measureImage(W.bank.preview).then((dims) => { W.bank.widthPx = dims.w; W.bank.heightPx = dims.h; save(); });
    }

    async function runSearch() {
        const q = (($("w-q").value || "").trim());
        const pulse = $("w-pulse");
        if (pulse) pulse.hidden = false;
        const grid = $("w-grid");
        grid.innerHTML = "";
        for (let i = 0; i < 6; i++) { const sk = document.createElement("div"); sk.className = "sk"; grid.appendChild(sk); }
        $("w-status").textContent = T("searching");
        try {
            const r = await fetch("/order/wizard?handler=Concept&q=" + encodeURIComponent(q) + "&limit=18", { headers: { "Accept": "application/json" } });
            const res = await r.json();
            const items = (res && (Array.isArray(res) ? res : res.items)) || [];
            if (pulse) pulse.hidden = true;
            $("w-status").textContent = items.length ? num(items.length) + " " + T("searchOf") + " — " + (q ? "«" + q + "»" : T("entryStore")) : T("noResults");
            const byCat = {};
            const diverse = items.filter((d) => {
                const c = d.categoryName || d.categoryCode || "";
                byCat[c] = (byCat[c] || 0) + 1;
                return byCat[c] <= 6;
            });
            grid.innerHTML = "";
            diverse.forEach((d) => {
                const pct = Math.max(0, Math.min(100, Number(d.score) || 0));
                const mm = pct <= 30 ? "mm-low" : pct <= 70 ? "mm-mid" : "mm-high";
                const fig = document.createElement("figure");
                fig.className = "cardart" + (W.bank && W.bank.id === d.id ? " on" : "");
                fig.innerHTML = '<div class="a">' + (d.thumbnailUrl || d.previewUrl ? '<img loading="lazy" src="' + (d.thumbnailUrl || d.previewUrl) + '" alt="">' : "") + "</div><figcaption></figcaption>" +
                    '<span class="match-meter ' + mm + '"><span class="bar" aria-hidden="true"><i style="width:' + pct + '%"></i></span><b class="pct">' + num(pct) + (CULTURE === "fa" || CULTURE === "ar" ? "٪" : "%") + "</b></span>";
                fig.querySelector("figcaption").textContent = shortTitle(d.title || d.code || "");
                fig.addEventListener("click", () => {
                    pickBankDesign(d);
                    grid.querySelectorAll(".cardart").forEach((x) => x.classList.remove("on"));
                    fig.classList.add("on");
                });
                grid.appendChild(fig);
            });
        } catch { if (pulse) pulse.hidden = true; $("w-status").textContent = T("noResults"); }
    }
    let stimer = null;
    $("w-q").addEventListener("input", () => { clearTimeout(stimer); stimer = setTimeout(runSearch, 380); });

    async function preselectEntryDesign() {
        const id = BOOT.entryDesign || "";
        if (!id) return;
        try {
            const r = await fetch("/order/wizard?handler=Design&id=" + id, { headers: { "Accept": "application/json" } });
            const d = await r.json();
            if (d && d.id) {
                pickBankDesign(d);
                const note = $("w-entrynote");
                if (note) note.hidden = false;
                save();
            }
        } catch { /* طرح ورودی در دسترس نیست */ }
    }

    // ── آپلود ──
    function filePicked(file) {
        if (!file || !/^image\//.test(file.type)) return;
        W.file = file;
        W.fileMeta = { url: URL.createObjectURL(file), width: 0, height: 0 };
        const im = new Image();
        im.onload = () => {
            W.fileMeta.width = im.naturalWidth;
            W.fileMeta.height = im.naturalHeight;
            let msg, bp, best;
            if (im.naturalWidth >= 1400) { msg = T("qExcellent"); bp = 100; best = "A3"; }
            else if (im.naturalWidth >= 800) { msg = T("qGood"); bp = 72; best = "A4"; }
            else { msg = T("qLow"); bp = 42; best = "A5"; }
            $("w-check").hidden = false; $("w-checkmsg").textContent = msg;
            $("w-meter").style.width = bp + "%"; $("w-best").textContent = best;
            W.mode = "upload";
            invalidateAudit();
            save(); renderPreview(); renderBar(); quote();
        };
        im.src = W.fileMeta.url;
    }
    $("w-file").addEventListener("change", (e) => filePicked(e.target.files[0]));
    ["dragover", "dragleave", "drop"].forEach((ev) => {
        $("w-drop").addEventListener(ev, (e) => {
            e.preventDefault();
            $("w-drop").classList.toggle("over", ev === "dragover");
            if (ev === "drop") { const f = e.dataTransfer.files[0]; if (f) filePicked(f); }
        });
    });

    // ── هوش مصنوعی ──
    let aiUrl = null;
    const aiMsg = (text, kind) => {
        const el = $("w-ai-msg");
        if (!text) { el.hidden = true; el.textContent = ""; return; }
        el.hidden = false;
        el.className = "ai-msg" + (kind ? " " + kind : "");
        el.textContent = text;
    };
    const aiQuotaLabelOf = (d) => d && d.remaining > 0
        ? T("aiQuotaLabel").replace("{n}", num(d.remaining)).replace("{m}", num(d.quotaLimit))
        : T("aiQuotaNone");
    const aiQuotaRender = (d) => {
        const q = $("w-ai-quota");
        if (!d) return;
        q.classList.toggle("none", !(d.remaining > 0));
        q.textContent = aiQuotaLabelOf(d);
    };
    async function aiQuota() {
        try {
            const r = await fetch("/order/wizard?handler=AiQuota", { headers: { "Accept": "application/json" } });
            const d = await r.json();
            if (d && typeof d.quotaLimit === "number") { aiQuotaRender(d); return d; }
        } catch { /* اختیاری */ }
        return null;
    }
    const aiBusy = (on) => {
        const btn = $("w-ai-gen");
        btn.disabled = on;
        btn.querySelector(".spin").hidden = !on;
        btn.querySelector(".glabel").textContent = on ? T("aiGenerating") : T("aiGenerate");
    };
    const aiOpen = async () => { $("w-ai").hidden = false; aiMsg(null); await aiQuota(); };
    const aiClose = () => { $("w-ai").hidden = true; };
    const aiBtn = $("w-ai-dock"); if (aiBtn) aiBtn.addEventListener("click", aiOpen);
    $("w-ai-close").addEventListener("click", aiClose);
    $("w-ai").addEventListener("click", (e) => { if (e.target === $("w-ai")) aiClose(); });
    window.addEventListener("keydown", (e) => { if (e.key === "Escape") { if (!$("w-ai").hidden) aiClose(); else closeAllPo(); } });

    async function aiGenerate() {
        const prompt = ($("w-ai-prompt").value || "").trim();
        if (!prompt) { aiMsg(T("aiPromptRequired"), "info"); return; }
        $("w-ai-use").disabled = true;
        aiBusy(true);
        aiMsg(T("aiGenerating"), "info");
        try {
            const r = await postJson("/order/wizard?handler=AiDesign", { prompt });
            const d = await r.json();
            if (d && d.ok && d.imageUrl) {
                aiUrl = d.imageUrl;
                $("w-ai-img").src = d.imageUrl;
                $("w-ai-result").hidden = false;
                $("w-ai-use").disabled = false;
                aiMsg(d.message || "", "ok");
                aiQuotaRender(d);
            } else if (d && d.quotaExceeded) {
                aiMsg(d.message || T("aiErrorGeneric"), "");
                aiQuotaRender(d);
            } else if (d && d.problem === "auth") {
                aiClose();
                location.href = "/account/login?returnUrl=%2Forder%2Fwizard";
                return;
            } else if (d && d.problem === "prompt-required") {
                aiMsg(T("aiPromptRequired"), "info");
            } else {
                aiMsg((d && (d.message || d.problem)) || T("aiErrorGeneric"), "");
            }
        } catch { aiMsg(T("aiErrorGeneric"), ""); }
        aiBusy(false);
    }
    $("w-ai-gen").addEventListener("click", aiGenerate);

    function aiDesignApply(imageUrl) {
        return fetch(imageUrl)
            .then((r) => { if (!r.ok) throw new Error("image fetch failed"); return r.blob(); })
            .then((blob) => {
                const file = new File([blob], "ai-design.png", { type: "image/png" });
                filePicked(file);
            });
    }
    $("w-ai-use").addEventListener("click", () => {
        if (!aiUrl) return;
        $("w-ai-use").disabled = true;
        aiDesignApply(aiUrl).catch(() => aiMsg(T("aiErrorGeneric"), "")).finally(() => { $("w-ai-use").disabled = false; aiClose(); });
    });

    // ── متن ──
    const COLOR_ARIA = ["مشکی", "سفید", "نارنجی", "قرمز", "آبی", "سبز", "بنفش", "صورتی"];
    const COLOR_ARIA_I18N = {
        fa: COLOR_ARIA,
        ar: ["أسود", "أبيض", "برتقالي", "أحمر", "أزرق", "أخضر", "بنفسجي", "وردي"],
        en: ["Black", "White", "Orange", "Red", "Blue", "Green", "Purple", "Pink"],
        es: ["Negro", "Blanco", "Naranja", "Rojo", "Azul", "Verde", "Morado", "Rosa"],
    };
    TEXT_COLORS.forEach((c, i) => {
        const d = document.createElement("button");
        d.type = "button"; d.className = "dot" + (i === 0 ? " on" : "");
        d.style.background = c; d.style.boxShadow = "inset 0 0 0 1px rgba(0,0,0,.25)";
        const names = COLOR_ARIA_I18N[CULTURE] || COLOR_ARIA_I18N.fa;
        d.setAttribute("aria-label", T("textColor") + " — " + names[i]);
        d.title = names[i];
        d.addEventListener("click", () => {
            $("w-tdots").querySelectorAll(".dot").forEach((x) => x.classList.remove("on"));
            d.classList.add("on");
            W.textColor = c;
            if (!W.text) { W.text = T("sampleText"); $("w-textin").value = W.text; }
            invalidateAudit(); save(); renderPreview(); renderBar(); quote();
        });
        $("w-tdots").appendChild(d);
    });
    document.querySelectorAll(".fbtn").forEach((b) => {
        b.addEventListener("click", () => {
            document.querySelectorAll(".fbtn").forEach((x) => x.classList.remove("on"));
            b.classList.add("on");
            W.font = +b.dataset.f;
            if (!W.text) { W.text = T("sampleText"); $("w-textin").value = W.text; }
            invalidateAudit(); save(); renderPreview(); renderBar(); quote();
        });
    });
    $("w-textin").addEventListener("input", (e) => { W.text = e.target.value; invalidateAudit(); save(); renderPreview(); renderBar(); quote(); });
    $("w-tsz").addEventListener("input", (e) => { W.textSize = +e.target.value; $("w-tsout").textContent = num(W.textSize); renderPreview(); invalidateAudit(); });

    // ── سوییچر جلو/پشت + زوم ──
    $("w-side").addEventListener("click", (e) => {
        const b = e.target.closest("button"); if (!b) return;
        W.position = b.dataset.side;
        $("w-side").querySelectorAll("button").forEach((x) => x.classList.remove("on"));
        b.classList.add("on");
        applyGarmTransform(); save(); renderPreview(); renderBar(); quote(); invalidateAudit();
    });
    document.querySelectorAll("#w-zoom button").forEach((b) => {
        b.addEventListener("click", () => {
            document.querySelectorAll("#w-zoom button").forEach((x) => x.classList.remove("on"));
            b.classList.add("on");
            zoomLevel = parseFloat(b.dataset.zoom) || 1;
            applyGarmTransform();
        });
    });

    // ── داک‌ها و پاپ‌اورها ──
    const POPS = ["w-po-bank", "w-po-upload", "w-po-text", "w-po-audit", "w-po-layers", "w-po-product", "w-po-info"];
    function closeAllPo() {
        POPS.forEach((pid) => { const el = $(pid); if (el) { el.hidden = true; el.classList.remove("open"); } });
        document.querySelectorAll(".v3ic.on").forEach((b) => b.classList.remove("on"));
    }
    function openPo(id) {
        const target = $(id);
        if (!target) return;
        const wasOpen = target.classList.contains("open");
        closeAllPo();
        if (!wasOpen) {
            target.hidden = false;
            target.classList.add("open");
            const btn = document.querySelector('.v3ic[data-po="' + id + '"]');
            if (btn) btn.classList.add("on");
            if (id === "w-po-bank" && !$("w-grid").children.length) runSearch();
            if (id === "w-po-layers") renderLayers();
        }
    }
    document.querySelectorAll(".v3ic[data-po], .v3auditpill[data-po]").forEach((b) => {
        b.addEventListener("click", (e) => {
            const id = b.getAttribute("data-po");
            if (id && id.startsWith("w-po")) openPo(id);
        });
    });
    document.querySelectorAll("[data-po-close]").forEach((x) => x.addEventListener("click", closeAllPo));

    // ── ممیزی طراحی ──
    const I_CHECK = '<svg viewBox="0 0 24 24" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m4 12.5 5 5L20 6.5"/></svg>';
    const I_X = '<svg viewBox="0 0 24 24" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6l12 12M18 6 6 18"/></svg>';
    const I_WAND = '<svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M15 4V2M15 16v-2M8 9h2M20 9h2M17.8 11.8 19 13M17.8 6.2 19 5M3 21l9-9M12.2 6.2 11 5"/></svg>';
    function auditPayload() {
        let dw = 0, dh = 0;
        if (W.mode === "upload" && W.fileMeta) { dw = W.fileMeta.width; dh = W.fileMeta.height; }
        else if (W.mode === "bank" && W.bank) { dw = W.bank.widthPx || 0; dh = W.bank.heightPx || 0; }
        let text = null;
        if (overlayText() && W.printSize) {
            const textEl = activeFace().tWrap.querySelector(".txt .tt");
            const prect = activeFace().rect.getBoundingClientRect();
            if (textEl && prect.width > 10) {
                const trect = textEl.getBoundingClientRect();
                text = {
                    cxPct: +((trect.left + trect.width / 2 - prect.left) / prect.width).toFixed(3),
                    cyPct: +((trect.top + trect.height / 2 - prect.top) / prect.height).toFixed(3),
                    wPct: +(trect.width / prect.width).toFixed(3),
                    hPct: +(trect.height / prect.height).toFixed(3),
                    hex: W.textColor,
                };
            }
        }
        return {
            printSizeCode: W.printSize || "",
            fabricHex: hexOf(colorName(W.colorCode), W.colorCode),
            source: W.mode || "",
            artworkId: W.mode === "bank" && W.bank ? W.bank.id : null,
            designWidthPx: dw || null,
            designHeightPx: dh || null,
            text,
            culture: CULTURE,
        };
    }
    async function runAudit() {
        if (!hasDesign() && !hasText()) {
            W.audit = { empty: true };
            updateAuditUI();
            return W.audit;
        }
        W.audit = { running: true };
        updateAuditUI();
        try {
            const r = await postJson("/order/wizard?handler=Audit", auditPayload());
            const data = await r.json();
            if (!data || !data.ok) { W.audit = null; updateAuditUI(); return null; }
            const res = data.result || {};
            W.audit = { passed: !!res.passed, results: res.results || [] };
        } catch { W.audit = null; }
        updateAuditUI();
        save();
        return W.audit;
    }
    function renderAudit() {
        const list = $("w-audit-list");
        if (!list) return;
        if (!W.audit || W.audit.running) { list.innerHTML = ""; return; }
        if (W.audit.empty) { list.innerHTML = '<p class="status">' + T("auditEmpty") + "</p>"; return; }
        list.innerHTML = (W.audit.results || []).map((r) =>
            '<div class="ps3rule' + (r.passed ? " ok" : " bad") + '"><span class="ri">' + (r.passed ? I_CHECK : I_X) + '</span><span><b>' + escapeHtml(r.title || "") + "</b><small>" + escapeHtml(r.message || "") + "</small></span>" +
            (!r.passed && r.fixHint ? '<button type="button" class="v3fix" data-code="' + escapeHtml(r.code || "") + '" title="' + escapeHtml(r.fixHint) + '">' + I_WAND + T("fix") + "</button>" : "") + "</div>"
        ).join("");
        list.querySelectorAll(".v3fix").forEach((b) => b.addEventListener("click", () => {
            const code = b.getAttribute("data-code") || "";
            if (code.startsWith("text") || code.startsWith("contrast")) openPo("w-po-text");
            else openPo(hasDesign() && W.mode === "upload" ? "w-po-upload" : "w-po-bank");
        }));
    }
    function updateAuditUI() {
        const cls = !W.audit || W.audit.running || W.audit.empty ? "none" : W.audit.passed ? "ok" : "bad";
        for (const led of [$("w-audit-led"), $("w-audit-led2")]) if (led) led.className = "v3led " + cls;
        let label = T("auditPending");
        let badCount = 0;
        if (W.audit && !W.audit.running && !W.audit.empty && W.audit.results) {
            badCount = W.audit.results.filter((x) => !x.passed).length;
            label = W.audit.passed ? T("auditOk") : T("auditBad") + " (" + num(badCount) + ")";
        }
        const pill = $("w-audit-pill-t");
        if (pill) pill.textContent = label;
        const title = $("w-audit-title");
        if (title) title.textContent = label;
        const sub = $("w-audit-sub");
        if (sub) sub.textContent = cls === "ok" ? T("auditOkHint") : cls === "bad" ? T("auditBadHint") : T("auditPendingHint");
        const sh = $("w-audit-sh");
        if (sh) sh.className = "v3aud-sh " + cls;
        // دکمه ادامه: ممیزی قرمز = قفل
        const next = $("w-next1");
        if (next) next.disabled = !(designName() && W.product && W.variant && W.printSize) || cls === "bad";
        const runBtn = $("w-audit-run");
        if (runBtn) runBtn.disabled = !!(W.audit && W.audit.running);
        renderAudit();
    }
    const auditRunBtn = $("w-audit-run");
    if (auditRunBtn) auditRunBtn.addEventListener("click", async () => {
        if (!hasDesign() && !hasText()) { W.audit = { empty: true }; updateAuditUI(); return; }
        await runAudit();
    });

    // ── لایه‌ها ──
    function renderLayers() {
        const list = $("w-layers-list");
        if (!list) return;
        const eyeSvg = '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/></svg>';
        const rows = [];
        if (hasDesign()) rows.push({ kind: "design", th: '<span class="th g"></span>', name: T("layersDesign") + ": " + escapeHtml(designName() || "") });
        if (overlayText()) rows.push({ kind: "text", th: '<span class="th">T</span>', name: T("layersText") + ": «" + escapeHtml(W.text.slice(0, 12)) + "»" });
        const n = $("w-layer-n");
        if (n) { n.hidden = rows.length === 0; if (rows.length) n.textContent = num(rows.length); }
        if (!rows.length) { list.innerHTML = '<p class="status">' + T("layersEmpty") + "</p>"; return; }
        list.innerHTML = rows.map((r) =>
            '<div class="ps3layer' + (window.__psSel === r.kind ? " sel" : "") + '" data-kind="' + r.kind + '">' + r.th + "<b>" + r.name + "</b>" +
            '<button type="button" data-eye aria-label="eye">' + eyeSvg + "</button>" +
            '<button type="button" data-del aria-label="del">' + I_X + "</button></div>").join("");
        list.querySelectorAll(".ps3layer").forEach((row) => {
            row.addEventListener("click", (e) => {
                if (e.target.closest("button")) return;
                window.__psSel = row.getAttribute("data-kind");
                renderSelection(); renderLayers();
            });
        });
        list.querySelectorAll("[data-eye]").forEach((b) => b.addEventListener("click", () => {
            const kind = b.closest(".ps3layer").getAttribute("data-kind");
            if (kind === "text") { W.textHidden = !W.textHidden; renderPreview(); }
            save();
        }));
        list.querySelectorAll("[data-del]").forEach((b) => b.addEventListener("click", () => {
            const kind = b.closest(".ps3layer").getAttribute("data-kind");
            if (kind === "text") { W.text = ""; $("w-textin").value = ""; }
            else { W.bank = null; W.file = null; W.fileMeta = null; if (W.mode !== "text") W.mode = null; }
            window.__psSel = null;
            invalidateAudit(); save(); renderPreview(); renderBar(); renderLayers(); quote();
        }));
    }

    // ── ناوبری فازها ──
    function renderSteps() {
        const steps = I18N.steps || [];
        $("w-steps").innerHTML = steps.map((s, i) => "<li" + (i + 1 === W.phase ? ' class="on"' : i + 1 < W.phase ? ' class="done"' : "") + "></li>").join("");
        $("w-steps").querySelectorAll("li").forEach((li, i) => { li.textContent = steps[i] || ""; });
    }
    function goPhase(n) {
        W.phase = n;
        [1, 2, 3, 4].forEach((i) => { $("w-p" + i).hidden = i !== n; });
        renderSteps(); save();
        if (n === 2) loadAddresses();
        if (n === 3) void checkout();
        if (n === 4) renderPay();
        window.scrollTo({ top: 0, behavior: "smooth" });
    }
    $("w-next1").addEventListener("click", async () => {
        if ($("w-next1").disabled) return;
        const res = await runAudit();
        if (res && !res.empty && !res.passed) { openPo("w-po-audit"); return; }
        goPhase(2);
    });
    $("w-back2").addEventListener("click", () => goPhase(1));
    $("w-back3").addEventListener("click", () => goPhase(2));
    $("w-back4").addEventListener("click", () => goPhase(3));
    const terms = $("w-terms"), termsMsg = $("w-terms-msg");
    if (terms) {
        terms.addEventListener("change", () => {
            $("w-confirm").disabled = !terms.checked;
            if (terms.checked && termsMsg) termsMsg.hidden = true;
        });
    }
    $("w-confirm").addEventListener("click", () => {
        if (terms && !terms.checked) {
            if (termsMsg) { termsMsg.textContent = T("termsRequired"); termsMsg.className = "ai-msg"; termsMsg.hidden = false; }
            terms.focus();
            return;
        }
        goPhase(4);
    });
    $("w-next2").addEventListener("click", () => {
        if (!W.packaging) { alert(T("pickPackFirst")); return; }
        goPhase(3);
    });

    // ── فاز ۳ ──
    let lastTotal = 0;
    function checkoutState() {
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        const size = (CONFIG.printSizes || []).find((s) => s.code === W.printSize);
        const gift = W.gift ? {
            name: $("w-g-name").value.trim(),
            phone: $("w-g-phone").value.trim(),
            address: $("w-g-addr").value.trim(),
            message: $("w-g-msg").value.trim(),
        } : null;
        return {
            previousOrderId: W.orderId,
            mode: W.mode,
            designId: W.bank ? W.bank.id : null,
            designCode: W.bank ? W.bank.code : null,
            designTitle: W.bank ? W.bank.title : null,
            designShortTitle: W.bank ? shortTitle(W.bank.title) : null,
            text: W.mode === "text" || overlayText() ? W.text : null,
            overlay: overlayText(),
            textX: W.textX, textY: W.textY,
            font: W.font, textColor: W.textColor, textSize: W.textSize,
            productId: W.variant ? W.variant.productId || W.product : W.product,
            productVariantId: W.variant ? W.variant.id : null,
            quantity: W.qty || 1,
            printSizeCode: W.printSize, printSizeName: size ? size.name : W.printSize,
            printPosition: W.position,
            shirtColorCode: W.colorCode, shirtColorName: W.colorCode ? colorName(W.colorCode) : null,
            productName: p ? p.name : null,
            packagingTypeCode: W.packaging,
            addressId: W.addressId,
            newAddress: W.newAddress ? { addressLine1: $("w-na-line").value.trim(), city: $("w-na-city").value.trim(), postalCode: $("w-na-postal").value.trim() } : null,
            gift,
        };
    }

    // کامپوزیت طرح + لایه متن — متن در موقعیت انتخابی کاربر کشیده می‌شود (مطابق پیش‌نمایش)
    function bankTextCompositeFile() {
        return new Promise(function (resolve) {
            const c = document.createElement("canvas");
            c.width = 1240; c.height = 1754;
            const ctx = c.getContext("2d");
            ctx.fillStyle = "#ffffff"; ctx.fillRect(0, 0, c.width, c.height);
            const img = new Image();
            img.crossOrigin = "anonymous";
            img.onload = function () {
                const maxW = c.width * 0.78, maxH = c.height * 0.52;
                const sc = Math.min(maxW / img.width, maxH / img.height);
                const dw = img.width * sc, dh = img.height * sc;
                const dx = (c.width - dw) / 2, dy = c.height * 0.06;
                ctx.drawImage(img, dx, dy, dw, dh);
                if (overlayText()) {
                    const sizePx = Math.max(48, Math.round(W.textSize / 100 * dw * 0.26));
                    ctx.fillStyle = W.textColor; ctx.textAlign = "center"; ctx.textBaseline = "middle";
                    ctx.font = (W.font === 3 ? "italic " : W.font === 2 ? "500 " : "700 ") + sizePx + "px Vazirmatn, Tahoma, sans-serif";
                    const words = String(W.text).split(/\s+/); const lines = []; let line = "";
                    words.forEach(function (w2) { const t = line ? line + " " + w2 : w2; if (ctx.measureText(t).width > dw * 0.9 && line) { lines.push(line); line = w2; } else line = t; });
                    lines.push(line);
                    const cx = dx + dw * Math.max(0.08, Math.min(0.92, W.textX));
                    const cy = dy + dh * Math.max(0.08, Math.min(0.92, W.textY));
                    const lh = sizePx * 1.35, y0 = cy - (lines.length - 1) * lh / 2;
                    lines.forEach(function (l, i) { ctx.fillText(l, cx, y0 + i * lh); });
                }
        // نویز یکتا در گوشه بوم — هش هر فایل یکتا بماند (Core تکراری را duplicate رد می‌کند)
        const nz = ctx.createImageData(2, 2);
        for (let i = 0; i < 8; i++) nz.data[i] = 200 + Math.floor(Math.random() * 55);
        nz.data[3] = 8; nz.data[7] = 8;
        ctx.putImageData(nz, c.width - 2, c.height - 2);
                c.toBlob(function (b) { resolve(b ? new File([b], "bank-text-design.png", { type: "image/png" }) : null); }, "image/png");
            };
            img.onerror = function () { resolve(null); };
            img.src = W.bank.preview || "";
        });
    }


    // ماکاپ تیشرت (پیش‌نمایش محل چاپ) — با سفارش به ERP می‌رود تا در کارتابل‌ها دیده شود
    async function shirtMockupFile() {
        const c = document.createElement("canvas");
        c.width = 600; c.height = 680;
        const ctx = c.getContext("2d");
        const side = W.position;
        const k = teeKey();
        const loadImg = (src) => new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = src; });
        let shirtImg = null;
        if (k) { try { shirtImg = await loadImg(TEE_IMAGES[k][side]); } catch { shirtImg = null; } }
        const hex = hexOf(colorName(W.colorCode), W.colorCode);
        if (shirtImg) ctx.drawImage(shirtImg, 0, 0, c.width, c.height);
        else {
            const svg = tee(side).replace("var(--teeC)", hex);
            try { const si = await loadImg("data:image/svg+xml;utf8," + encodeURIComponent(svg)); ctx.drawImage(si, 0, 0, c.width, c.height); }
            catch { ctx.fillStyle = hex; ctx.fillRect(0, 0, c.width, c.height); }
        }
        // ناحیه چاپ (مطابق صحنه: لبه بالایی ۱۰۸ از ۳۴۰)
        const S = szPx(W.printSize);
        const sc = c.width / 300;
        const px = S.w * sc, py = S.h * sc;
        const px0 = (150 - S.w / 2) * sc, py0 = COLLAR_TOP * sc;
        let dsrc = null;
        if (W.mode === "bank" && W.bank) dsrc = W.bank.preview;
        else if (W.mode === "upload" && W.fileMeta) dsrc = W.fileMeta.url;
        if (dsrc) {
            try {
                const di = await loadImg(dsrc);
                const msc = Math.min(px / di.width, py / di.height);
                const dw = di.width * msc, dh2 = di.height * msc;
                ctx.drawImage(di, px0 + (px - dw) / 2, py0 + (py - dh2) / 2, dw, dh2);
            } catch { /* طرح بی‌صدا حذف می‌شود */ }
        }
        if (overlayText()) {
            const sizePx = Math.max(12, Math.round(W.textSize / 100 * px * 0.30));
            ctx.fillStyle = W.textColor; ctx.textAlign = "center"; ctx.textBaseline = "middle";
            ctx.font = (W.font === 3 ? "italic " : W.font === 2 ? "500 " : "700 ") + sizePx + "px Vazirmatn, Tahoma, sans-serif";
            const cx = px0 + px * W.textX, cy = py0 + py * W.textY;
            const words = String(W.text).split(/\s+/); const lines = []; let line = "";
            words.forEach(function (w2) { const t = line ? line + " " + w2 : w2; if (ctx.measureText(t).width > px * 0.92 && line) { lines.push(line); line = w2; } else line = t; });
            lines.push(line);
            const lh = sizePx * 1.3, y0 = cy - (lines.length - 1) * lh / 2;
            lines.forEach(function (l, i) { ctx.fillText(l, cx, y0 + i * lh); });
        }
        // نویز یکتا در گوشه بوم — هش هر فایل یکتا بماند (Core تکراری را duplicate رد می‌کند)
        const nz2 = ctx.createImageData(2, 2);
        for (let i = 0; i < 8; i++) nz2.data[i] = 200 + Math.floor(Math.random() * 55);
        nz2.data[3] = 8; nz2.data[7] = 8;
        ctx.putImageData(nz2, c.width - 2, c.height - 2);
        // نویز یکتا در گوشه بوم (ماکاپ)
        const nz = ctx.createImageData(2, 2);
        for (let i = 0; i < 8; i++) nz.data[i] = 200 + Math.floor(Math.random() * 55);
        nz.data[3] = 8; nz.data[7] = 8;
        ctx.putImageData(nz, c.width - 2, c.height - 2);
        const blob = await new Promise((res) => c.toBlob(res, "image/png"));
        return blob ? new File([blob], "shirt-mockup.png", { type: "image/png" }) : null;
    }

    function textDesignFile() {
        return new Promise(function (resolve) {
            const c = document.createElement("canvas");
            c.width = 1240; c.height = 1754;
            const ctx = c.getContext("2d");
            ctx.fillStyle = "#ffffff"; ctx.fillRect(0, 0, c.width, c.height);
            const sizePx = Math.max(48, Math.round(W.textSize / 100 * c.width * 0.14));
            ctx.fillStyle = W.textColor; ctx.textAlign = "center"; ctx.textBaseline = "middle";
            ctx.font = (W.font === 3 ? "italic " : W.font === 2 ? "500 " : "700 ") + sizePx + "px Vazirmatn, Tahoma, sans-serif";
            const words = String(W.text).split(/\s+/); const lines = []; let line = "";
            words.forEach(function (w2) { const t = line ? line + " " + w2 : w2; if (ctx.measureText(t).width > c.width * 0.82 && line) { lines.push(line); line = w2; } else line = t; });
            lines.push(line);
            const lh = sizePx * 1.45; const y0 = c.height / 2 - (lines.length - 1) * lh / 2;
            lines.forEach(function (l, i) { ctx.fillText(l, c.width / 2, y0 + i * lh); });
            c.toBlob(function (b) { resolve(b ? new File([b], "text-design.png", { type: "image/png" }) : null); }, "image/png");
        });
    }

    async function checkout() {
        const list = $("w-revlist"), lines = $("w-pricelines"), totalEl = $("w-total");
        list.innerHTML = '<p class="status">' + T("loading") + "</p>"; lines.innerHTML = ""; totalEl.textContent = "—";
        const confirmBtn = $("w-confirm");
        if (confirmBtn) confirmBtn.disabled = true;
        try {
            let r;
            if ((W.mode === "upload" && W.file) || (W.mode === "text" && W.text) || (W.mode === "bank" && W.bank && overlayText())) {
                const file = W.mode === "text" ? await textDesignFile() : W.mode === "bank" ? await bankTextCompositeFile() : W.file;
                const fd = new FormData();
                fd.append("state", JSON.stringify(checkoutState()));
                fd.append("file", file, file.name || "text-design.png");
                try { const mp = await shirtMockupFile(); if (mp) fd.append("filePreview", mp, mp.name); } catch { /* ماکاپ اختیاری است */ }
                fd.append("__RequestVerificationToken", AF);
                r = await fetch("/order/wizard?handler=Checkout", { method: "POST", body: fd });
            } else {
                r = await postJson("/order/wizard?handler=Checkout", checkoutState());
            }
            const data = await r.json();
            if (!data.ok) {
                if (data.problem === "auth") { save(); location.href = "/account/login?returnUrl=%2Forder%2Fwizard"; return; }
                list.innerHTML = '<p class="status">' + T("errorGeneric") + " (" + (data.problem || "") + ")</p>";
                return;
            }
            W.orderId = data.orderId;
            save();
            if (typeof window.barangRefreshCart === "function") window.barangRefreshCart();
            renderReview(data.pricing);
            if (confirmBtn) confirmBtn.disabled = !!(terms && !terms.checked);
        } catch { list.innerHTML = '<p class="status">' + T("errorGeneric") + "</p>"; }
    }
    function renderReview(pricing) {
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        const size = (CONFIG.printSizes || []).find((s) => s.code === W.printSize);
        const pack = (CONFIG.packaging || []).find((x) => x.code === W.packaging);
        const rw = Math.round(230 * sizeScale());
        $("w-revtee").innerHTML = (W.position === "BACK"
            ? '<div class="garm back" style="width:' + rw + 'px;margin:0 auto"><div class="face">' + tee("BACK") + '</div><div class="face bk">' + tee("FRONT") + "</div></div>"
            : '<div class="garm" style="width:' + rw + 'px;margin:0 auto"><div class="face">' + tee("FRONT") + '</div><div class="face bk">' + tee("BACK") + "</div></div>");
        const rtee = $("w-revtee").querySelector(".garm");
        if (W.colorCode) rtee.style.setProperty("--teeC", hexOf(colorName(W.colorCode), W.colorCode));
        const holder = document.createElement("div");
        holder.style.cssText = "position:absolute;left:50%;top:40%;transform:translate(-50%,-50%);width:120px;height:150px;display:flex;align-items:center;justify-content:center";
        const dh = designPreviewHTML();
        holder.innerHTML = dh || '<span style="color:#94a1b8;font-size:12px">—</span>';
        $("w-revtee").appendChild(holder);

        const designLine = W.mode === "bank"
            ? T("sourceBank") + ": " + (W.bank ? shortTitle(W.bank.title) : "—") + (overlayText() ? " + «" + W.text + "»" : "")
            : W.mode === "upload" ? T("yourFile") + (overlayText() ? " + «" + W.text + "»" : "")
            : T("textQuote") + ": «" + W.text + "»";
        const kv = [
            [T("noDesign"), designLine],
            [T("printSide"), W.position === "FRONT" ? T("front") : T("back")],
            [T("printSize"), (size ? size.name : W.printSize) + (size && size.description ? " · " + size.description : "")],
            [T("product"), p ? p.name : "—"],
            [T("shirtSize"), (W.variant && W.variant.sizeCode) || "—"],
            [T("color"), W.colorCode ? colorName(W.colorCode) : "—"],
            [T("packaging"), pack ? pack.name : "—"],
            [T("shipTime"), T("shipTimeValue")],
        ];
        if (W.gift) kv.push([T("gift"), T("giftDetails")]);
        if ((W.qty || 1) > 1) kv.push([T("qty"), num(W.qty) + " " + T("qtyUnit")]);
        $("w-revlist").innerHTML = kv.map((x) => '<div class="kv"><span></span><b dir="auto"></b></div>').join("");
        $("w-revlist").querySelectorAll(".kv").forEach((el, i) => {
            el.querySelector("span").textContent = kv[i][0];
            el.querySelector("b").textContent = kv[i][1];
        });

        const P = pricing || {};
        const rows = [
            [T("priceProduct"), P.rawProductTotal], [T("pricePrint"), P.printTotal],
            [T("pricePackaging"), P.packagingTotal], [T("priceShipping"), P.shippingTotal],
        ].filter((x) => x[1] != null && x[1] !== 0);
        if (P.discountTotal > 0) rows.push([T("priceDiscount"), -P.discountTotal]);
        if (P.taxTotal > 0) rows.push([T("priceTax"), P.taxTotal]);
        $("w-pricelines").innerHTML = rows.map((x) => '<div class="pline"><span></span><b></b></div>').join("");
        $("w-pricelines").querySelectorAll(".pline").forEach((el, i) => {
            el.querySelector("span").textContent = rows[i][0];
            el.querySelector("b").textContent = rows[i][1] === 0 ? T("free") : money(rows[i][1]);
        });
        const total = P.finalTotal ?? 0;
        lastTotal = total;
        $("w-total").textContent = money(total) + " " + T("toman");
        $("w-paytotal").textContent = money(total);
    }
    function designPreviewHTML() {
        if (W.mode === "bank" && W.bank) {
            const txt = overlayText()
                ? '<div class="t" dir="auto" style="position:absolute;left:' + (W.textX * 100) + '%;top:' + (W.textY * 100) + '%;transform:translate(-50%,-50%);width:88%;text-align:center;line-height:1.3;color:' + W.textColor + ';font-size:' + Math.max(11, Math.round(W.textSize / 100 * 19)) + 'px;' + (W.font === 3 ? "font-style:italic;" : "") + 'font-weight:900">' + escapeHtml(W.text) + "</div>"
                : "";
            return '<div style="position:relative;width:100%;height:100%;display:flex;align-items:center;justify-content:center"><img src="' + W.bank.preview + '" alt="" style="max-width:100%;max-height:100%;object-fit:contain">' + txt + "</div>";
        }
        if (W.mode === "upload" && W.fileMeta) return '<img src="' + W.fileMeta.url + '" alt="" style="max-width:100%;max-height:100%;object-fit:contain">';
        if (W.mode === "text" && W.text) {
            const size = Math.round(W.textSize / 100 * 24);
            return '<div class="t" dir="auto" style="color:' + W.textColor + ';font-size:' + size + 'px;font-weight:900;text-align:center">' + escapeHtml(W.text) + "</div>";
        }
        return null;
    }

    // ── فاز ۴ ──
    async function renderPay() {
        $("w-payarea").hidden = false;
        $("w-success").hidden = true;
        try {
            const r = await fetch("/order/wizard?handler=Wallet", { headers: { "Accept": "application/json" } });
            const data = await r.json();
            $("w-walletbal").textContent = data.ok ? T("balance") + ": " + money(data.balance) + " " + T("toman") : "—";
        } catch { $("w-walletbal").textContent = "—"; }
    }
    async function pay(method) {
        if (!W.orderId) return;
        $("w-payarea").hidden = true;
        try {
            const r = await postJson("/order/wizard?handler=Pay", { orderId: W.orderId, method });
            const data = await r.json();
            if (!data.ok) { alert(T("errorGeneric") + " (" + (data.problem || "") + ")"); $("w-payarea").hidden = false; return; }
            W.paidWith = data.paidWith;
            const order = data.order && data.order.order ? data.order.order : data.order;
            const number = (order && (order.orderNumber || (order.order && order.orderNumber))) || "";
            $("w-ordno").textContent = T("orderNumber") + ": " + (number || W.orderId);
            $("w-successinfo").textContent =
                money(lastTotal) + " " + T("toman") + " " +
                (data.paidWith === "wallet" ? T("paidViaWallet") : T("paidViaGateway")) + " · " + T("successNote");
            $("w-success").hidden = false;
            sessionStorage.removeItem("wz.state");
            if (typeof window.barangRefreshCart === "function") window.barangRefreshCart();
        } catch { alert(T("errorGeneric")); $("w-payarea").hidden = false; }
    }
    $("w-paygw").addEventListener("click", () => { $("w-gwamt").textContent = money(lastTotal) + " " + T("toman"); $("w-gw").hidden = false; });
    $("w-gwcancel").addEventListener("click", () => { $("w-gw").hidden = true; });
    $("w-gwpay").addEventListener("click", () => { $("w-gw").hidden = true; pay("gateway"); });
    $("w-paywallet").addEventListener("click", () => pay("wallet"));
    $("w-track").addEventListener("click", () => { location.href = "/account/order/" + (W.orderId || ""); });
    $("w-again").addEventListener("click", () => { location.href = "/order/wizard?new=1"; });

    // ── فاز ۲: آدرس‌ها ──
    async function loadAddresses() {
        try {
            const r = await fetch("/order/wizard?handler=Addresses", { headers: { "Accept": "application/json" } });
            const data = await r.json();
            if (!data.ok) return;
            const box = $("w-addrs");
            box.innerHTML = "";
            data.addresses.forEach((a) => {
                const c = document.createElement("div");
                c.className = "arow" + (W.addressId === a.id ? " on" : "");
                c.innerHTML = "<b>📍 </b><small>" + a.line + "</small>";
                c.querySelector("b").append(a.title);
                c.addEventListener("click", () => {
                    W.addressId = a.id; W.newAddress = false;
                    box.querySelectorAll(".arow,.pick").forEach((x) => x.classList.remove("on"));
                    c.classList.add("on");
                    $("w-newaddr").hidden = true;
                    save();
                });
                box.appendChild(c);
            });
            const nu = document.createElement("div");
            nu.className = "pick";
            nu.innerHTML = "<b>" + T("newAddress") + "</b><small>" + T("newAddressHint") + "</small>";
            nu.addEventListener("click", () => {
                W.addressId = null; W.newAddress = true;
                box.querySelectorAll(".arow,.pick").forEach((x) => x.classList.remove("on"));
                nu.classList.add("on");
                $("w-newaddr").hidden = false;
                save();
            });
            box.appendChild(nu);
            if (!W.addressId && !W.newAddress && data.addresses.length) {
                const def = data.addresses.find((a) => a.isDefault) || data.addresses[0];
                W.addressId = def.id;
                box.querySelector(".arow").classList.add("on");
            }
        } catch { /* بعداً تلاش می‌شود */ }
    }
    const giftToggle = $("w-gift"); if (giftToggle) giftToggle.addEventListener("change", (e) => { W.gift = e.target.checked; $("w-giftbox").hidden = !W.gift; save(); });

    // ── راه‌اندازی ──
    goPhase(W.phase || 1);
    if (W.text) $("w-textin").value = W.text;
    document.querySelectorAll("#w-txpos button").forEach((b) => b.classList.toggle("on", b.dataset.pos === (W.textPos || "mid")));
    $("w-tsout").textContent = num(W.textSize);
    applyGarmTransform();
    document.querySelectorAll("#w-side button").forEach((b) => b.classList.toggle("on", b.dataset.side === W.position));
    document.querySelectorAll("#w-zoom button").forEach((b) => b.classList.toggle("on", parseFloat(b.dataset.zoom) === zoomLevel));
    if (W.gift && $("w-gift") && $("w-giftbox")) { $("w-gift").checked = true; $("w-giftbox").hidden = false; }

    (async () => {
        await loadConfig();
        if (BOOT.entryDesign && !(W.bank && W.bank.id === BOOT.entryDesign)) await preselectEntryDesign();
        if (W.bank && !W.bank.widthPx) { const dims = await measureImage(W.bank.preview); W.bank.widthPx = dims.w; W.bank.heightPx = dims.h; save(); }
        renderBar(); renderPreview(); renderLayers(); updateAuditUI(); quote();
    })();
})();
