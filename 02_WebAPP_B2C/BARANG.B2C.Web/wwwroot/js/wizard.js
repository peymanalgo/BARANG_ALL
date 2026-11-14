// ویزارد سفارش چاپ (۴ فاز) — پیاده‌سازی مطابق پروتوتایپ؛ همه داده‌ها و قیمت‌ها از Core.
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
    // digit policy shared with store.js: fa/ar → Persian digits, en → Latin
    const num = (n) => (CULTURE === "fa" || CULTURE === "ar" ? fa(n) : String(n));
    const money = (n) => num(String(Math.round(Number(n) || 0)).replace(/\B(?=(\d{3})+(?!\d))/g, ","));
    // توکن antiforgery برای POSTهای AJAX (هدر استاندارد RequestVerificationToken)
    const AF = (() => {
        const el = document.querySelector("#wz-af input[name='__RequestVerificationToken']");
        return el ? el.value : "";
    })();
    const postJson = (url, body) => fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": AF },
        body: JSON.stringify(body),
    });

    // ابعاد فیزیکی استاندارد کاغذ (برچسب‌ها و برش سفارش)؛ قیمت‌ها از سرور می‌آیند
    const SZ = { A3: { w: 30, h: 42 }, A4: { w: 21, h: 29.7 }, A5: { w: 14, h: 21 } };
    const szCm = (code) => SZ[(code || "").toUpperCase()] || SZ.A4;
    // ابعاد نمایشی ناحیه چاپ روی ویوباکس 300×340 تیشرت (مطابق پروتوتایپ)
    const SZPX = { A3: { w: 116, h: 164 }, A4: { w: 82, h: 116 }, A5: { w: 56, h: 79 } };
    const szPx = (code) => SZPX[(code || "").toUpperCase()] || SZPX.A4;
    // رنگ نمایشی تیشرت بر اساس نام رنگ (فقط ظاهر پیش‌نمایش)
    const HEX = [
        [/سفید|أبيض|white/i, "#f2f3f1"], [/مشکی|سیاه|أسود|black/i, "#1a1a1a"], [/شیری|کرم|cream|beige/i, "#e8ddc8"],
        [/قرمز|زرشکی|أحمر|red|crimson|burgundy/i, "#8e2323"], [/آبی|أزرق|blue/i, "#3f6d9e"], [/سبز|أخضر|green/i, "#2d5a3d"],
        [/زرد|أصفر|yellow/i, "#dfb545"], [/نارنجی|برتقالي|orange/i, "#d97b3f"], [/خاکستری|طوسی|رمادي|gray|grey/i, "#8a8f98"],
        [/صورتی|وردي|pink/i, "#d98ba3"], [/بنفش|بنفسجي|purple|violet/i, "#6d5a9e"],
    ];
    const hexOf = (name, code) => {
        for (const [re, hex] of HEX) if (re.test(name || "") || re.test(code || "")) return hex;
        return "#9aa2ad";
    };
    // نام نمایشی رنگ: کانفیگ برای برخی کدها فقط نام لاتین دارد (BLACK/BLUE…) — به زبان کاربر برمی‌گردانیم
    const COLOR_NAMES = {
        black: { fa: "مشکی", ar: "أسود", en: "Black" }, blue: { fa: "آبی", ar: "أزرق", en: "Blue" },
        red: { fa: "قرمز", ar: "أحمر", en: "Red" }, white: { fa: "سفید", ar: "أبيض", en: "White" },
        cream: { fa: "شیری", ar: "كريمي", en: "Cream" }, green: { fa: "سبز", ar: "أخضر", en: "Green" },
        yellow: { fa: "زرد", ar: "أصفر", en: "Yellow" }, orange: { fa: "نارنجی", ar: "برتقالي", en: "Orange" },
        gray: { fa: "خاکستری", ar: "رمادي", en: "Gray" }, grey: { fa: "خاکستری", ar: "رمادي", en: "Gray" },
        pink: { fa: "صورتی", ar: "وردي", en: "Pink" }, purple: { fa: "بنفش", ar: "بنفسجي", en: "Purple" },
    };
    const colorName = (code) => {
        const c = (CONFIG.colors || []).find((x) => (x.code || "").toUpperCase() === String(code || "").toUpperCase());
        if (c && c.name && c.name.trim() && c.name.trim().toUpperCase() !== String(code || "").trim().toUpperCase()) return c.name;
        const m = COLOR_NAMES[String(code || "").trim().toLowerCase()];
        if (m) return m[CULTURE === "ar" ? "ar" : CULTURE === "en" ? "en" : "fa"];
        return (c && c.name) || code || "—";
    };
    const TEXT_COLORS = ["#1c1917", "#ffffff", "#f6b23c", "#e11d48", "#2563eb", "#16a34a", "#8b5cf6", "#e879f9"];

    // ── وضعیت ویزارد ──
    let CONFIG = null;
    const W = {
        phase: 1,
        mode: null,            // bank | upload | text
        bank: null,            // {id,title,preview}
        file: null, fileMeta: null,
        text: "", font: 1, textColor: "#1c1917", textSize: 100,
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
    const save = () => { try { sessionStorage.setItem("wz.state", JSON.stringify(W)); } catch { /* private mode */ } };

    // ── تصاویر واقعی تیشرت (۴ رنگ × جلو/پشت) ──
    const TEE_IMAGES = {
        black: { FRONT: "/images/tee/black-front.jpg", BACK: "/images/tee/black-back.jpg", teeC: "#1a1a1a" },
        white: { FRONT: "/images/tee/white-front.jpg", BACK: "/images/tee/white-back.jpg", teeC: "#f2f3f1" },
        red: { FRONT: "/images/tee/red-front.jpg", BACK: "/images/tee/red-back.jpg", teeC: "#8e2323" },
        blue: { FRONT: "/images/tee/blue-front.jpg", BACK: "/images/tee/blue-back.jpg", teeC: "#2c4e7e" },
    };
    const teeKey = () => { if (!CONFIG) return null; const n = colorName(W.colorCode) + " " + (W.colorCode || ""); const m = /آبی|أزرق|blue|ابو/i.test(n) ? "blue" : /سفید|أبيض|white/i.test(n) ? "white" : /قرمز|سرخ|أحمر|red/i.test(n) ? "red" : /مشکی|سیاه|أسود|black/i.test(n) ? "black" : null; return m; };
    function tee(side) {
        // ترجیح اول: تصویر واقعی واریانت انتخاب‌شده از ERP (imageUrl در کانفیگ ویزارد)
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
    const EMPTY_HTML = '<div class="empty">✨<br>' + T("emptyStage") + '</div>';
    function buildFace(el, side) {
        el.innerHTML = tee(side) + '<div class="prect" data-lbl=""></div><div class="awrap">' + EMPTY_HTML + '</div>';
        return { el: el, side: side, rect: el.querySelector(".prect"), wrap: el.querySelector(".awrap") };
    }
    const fF = buildFace($("w-ff"), "FRONT"), fB = buildFace($("w-fb"), "BACK");
    const setTeeColor = (hex) => { $("w-ff").style.setProperty("--teeC", hex); $("w-fb").style.setProperty("--teeC", hex); };
    // با تغییر واریانت/رنگ، تصویر تیشرت روی صفحه تازه شود (تصویر واریانت از ERP یا استاتیک رنگ)
    const refreshTee = () => {
        applyGarmScale();
        for (const f of [fF, fB]) {
            const cur = f.el.querySelector(".tee-img") || f.el.querySelector("svg");
            if (!cur) continue;
            const t = document.createElement("template");
            t.innerHTML = tee(f.side).trim();
            const next = t.content.firstChild;
            if (next) cur.replaceWith(next);
        }
    };

    // مقیاس نمایشی تیشرت روی صحنه با سایز انتخاب‌شده (S کوچک‌تر … XXL بزرگ‌تر)
    const SIZE_SCALE = { s: .82, m: .9, l: 1, xl: 1.08, xxl: 1.16 };
    const sizeScale = () => SIZE_SCALE[String((W.variant && W.variant.sizeCode) || "").trim().toLowerCase()] || 1;
    const applyGarmScale = () => {
        const g = $("w-garm");
        if (!g) return;
        const s = sizeScale();
        g.style.width = (82 * s) + "%";                 // پایه ۸۲٪ عرض صحنه (مطابق CSS .garm)
        g.style.maxWidth = Math.round(380 * s) + "px";  // سقف ۳۸۰px پایه × مقیاس
    };

    function place(f, code, html) {
        const box = f.wrap.parentElement.getBoundingClientRect();
        if (box.width < 10) return;
        const G = { w: box.width / 300, h: box.height / 340 }, S = szPx(code), C = szCm(code), px = S.w * G.w, py = S.h * G.h;
        const L = (150 * G.w - px / 2) + "px", Tp = ((118 + S.h / 2) * G.h - py / 2) + "px";
        f.rect.style.left = L; f.rect.style.top = Tp; f.rect.style.width = px + "px"; f.rect.style.height = py + "px";
        f.rect.setAttribute("data-lbl", code + " · " + (FA ? fa(C.w) + "×" + fa(C.h) : C.w + "×" + C.h) + " " + T("cm"));
        f.wrap.style.left = L; f.wrap.style.top = Tp; f.wrap.style.width = px + "px"; f.wrap.style.height = py + "px";
        if (html !== undefined) {
            f.wrap.innerHTML = html || EMPTY_HTML;
            if (html) { f.wrap.classList.remove("pop"); void f.wrap.offsetWidth; f.wrap.classList.add("pop"); }
        }
    }
    function designHTML() {
        if (W.mode === "bank" && W.bank) return '<div style="width:100%;height:100%;display:flex;align-items:center;justify-content:center"><img src="' + W.bank.preview + '" alt="" style="max-width:100%;max-height:100%;object-fit:contain"></div>';
        if (W.mode === "upload" && W.fileMeta) return '<img src="' + W.fileMeta.url + '" alt="" style="max-width:100%;max-height:100%;object-fit:contain">';
        if (W.mode === "text" && W.text) {
            const size = Math.round(W.textSize / 100 * (szPx(W.printSize).w * 0.42));
            const style = "color:" + W.textColor + ";font-size:" + size + "px;" +
                (W.font === 3 ? "font-style:italic;" : "") + (W.font === 2 ? "letter-spacing:.08em;font-weight:500;" : W.font === 4 ? "letter-spacing:.25em;" : "font-weight:900;");
            return '<div class="t" dir="auto" style="' + style + '">' + W.text.replace(/</g, "&lt;") + '</div>';
        }
        return null;
    }
    function renderPreview() {
        const html = designHTML();
        const front = W.position === "FRONT" ? fF : fB;
        place(front, W.printSize, html !== null ? html : undefined);
        if (html === null) front.wrap.innerHTML = EMPTY_HTML;
        const other = W.position === "FRONT" ? fB : fF;
        place(other, W.printSize, undefined);
        // اندازه باکس متن با اندازه چاپ عوض می‌شود
        if (html !== null) place(front, W.printSize, html);
    }
    window.addEventListener("resize", () => { clearTimeout(window.__wrz); window.__wrz = setTimeout(() => { applyGarmScale(); renderPreview(); }, 150); });

    // درگ طرح روی ناحیه چاپ (از پروتوتایپ)
    (function () {
        let down = false, sx = 0, sy = 0, ox = 0, oy = 0, dx = 0, dy = 0;
        const wrapOf = () => (W.position === "FRONT" ? fF : fB).wrap;
        root.addEventListener("pointerdown", (e) => {
            const wrap = wrapOf();
            if (!wrap || wrap.querySelector(".empty")) return;
            if (!e.target.closest(".awrap")) return;
            down = true; sx = e.clientX; sy = e.clientY; ox = dx; oy = dy;
        });
        window.addEventListener("pointermove", (e) => {
            if (!down) return;
            dx = Math.max(-70, Math.min(70, ox + (e.clientX - sx)));
            dy = Math.max(-90, Math.min(90, oy + (e.clientY - sy)));
            wrapOf().style.transform = "translate(" + dx + "px," + dy + "px)";
        });
        window.addEventListener("pointerup", () => { down = false; });
    })();

    // ── پیکربندی از سرور ──
    async function loadConfig() {
        const r = await fetch("/order/wizard?handler=Config", { headers: { "Accept": "application/json" } });
        CONFIG = await r.json();
        if (!CONFIG || !Array.isArray(CONFIG.products)) CONFIG = { products: [], printSizes: [], packaging: [], colors: [], shipping: 0, currencyCode: "IRT" };
        // پیش‌انتخاب پیش‌فرض‌ها: اولین محصول و بسته‌بندی استاندارد تا جمع سفارش از ابتدا زنده باشد
        if (!W.product && CONFIG.products.length) {
            W.product = CONFIG.products[0].id;
            const colors = productVariants(CONFIG.products[0]).map((v) => v.colorCode).filter(Boolean);
            if (colors.length) W.colorCode = colors[0];
        }
        if (!W.packaging && CONFIG.packaging.length) W.packaging = CONFIG.packaging[0].code;
        pickVariant();
        renderPrintSizes(); renderPackaging(); renderProducts(); renderSizes();
        if (!W.printSize && CONFIG.printSizes.length) W.printSize = CONFIG.printSizes[Math.min(1, CONFIG.printSizes.length - 1)].code;
        renderPrintSizes();
        renderSum(); renderPreview(); quote();
    }

    function renderPrintSizes() {
        const box = $("w-code");
        box.innerHTML = "";
        (CONFIG.printSizes || []).forEach((s) => {
            const b = document.createElement("button");
            b.type = "button"; b.textContent = s.name || s.code; b.title = s.description || "";
            if (s.code === W.printSize) b.classList.add("on");
            b.addEventListener("click", () => {
                W.printSize = s.code;
                box.querySelectorAll("button").forEach((x) => x.classList.remove("on"));
                b.classList.add("on");
                renderPreview(); renderSum(); quote();
            });
            box.appendChild(b);
        });
    }
    function renderPackaging() {
        const box = $("w-pack");
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
    // رنگ تیشرت دیگر با دات‌های جدا انتخاب نمی‌شود — همیشه از واریانت انتخاب‌شده (محصول×سایز) می‌آید
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
    // آیکون تیشرت CSS/SVG برای کارت محصول — رنگ از اولین واریانت محصول می‌آید
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
        box.innerHTML = "";
        (CONFIG.products || []).forEach((p) => {
            const prices = productVariants(p).map((v) => v.price).filter((x) => x != null);
            const price = p.salesPrice ?? (prices.length ? Math.min(...prices) : null);
            const c = document.createElement("div");
            c.className = "pcard" + (W.product === p.id ? " on" : "");
            c.innerHTML = '<span class="ic">' + shirtIcon(productHex(p)) + "</span><b></b><small dir=\"auto\">" + (p.code || "") + "</small>" +
                '<div class="pr">' + (price != null ? money(price) + " " + T("toman") : "—") + "</div>";
            c.querySelector("b").textContent = p.name;
            c.addEventListener("click", () => {
                W.product = p.id;
                W.sizeCode = null;
                // رنگ از محصول انتخاب‌شده می‌آید: اولین رنگ موجود در واریانت‌ها
                const colors = productVariants(p).map((v) => v.colorCode).filter(Boolean);
                if (colors.length) W.colorCode = colors[0];
                pickVariant(); renderSizes(); renderProducts(); renderSum(); quote();
            });
            box.appendChild(c);
        });
    }
    function renderSizes() {
        const stockOf = (sz) => { const p = (CONFIG.products || []).find(x => x.id === W.product); const v = (p && p.variants || []).find(v => (v.sizeCode || "").toUpperCase() === String(sz).toUpperCase()); return v ? (v.stock ?? null) : null; };
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        const box = $("w-ssize");
        box.innerHTML = "";
        let vs = productVariants(p);
        if (W.colorCode) { const byColor = vs.filter((v) => (v.colorCode || "").toUpperCase() === W.colorCode.toUpperCase()); if (byColor.length) vs = byColor; }
        const sizes = [...new Set(vs.map((v) => v.sizeCode).filter(Boolean))];
        sizes.push("—");
        sizes.forEach((s) => {
            const b = document.createElement("button");
            b.type = "button"; b.textContent = s;
            const st = s === "—" ? null : stockOf(s);
            if (st != null) { b.title = (st > 0 ? T("stockAvail") + ": " + num(st) : T("stockOut")); if (st <= 0) { b.disabled = true; b.style.opacity = ".45"; } }
            if (s === (W.variant && W.variant.sizeCode)) b.classList.add("on");
            b.addEventListener("click", () => { if (b.disabled) return; W.sizeCode = s === "—" ? null : s; pickVariant(); renderSizes(); renderSum(); quote(); renderPreview(); });
            box.appendChild(b);
        });
        box.parentElement.querySelectorAll(".stock-chip").forEach(x => x.remove());
        const cur = W.variant;
        const chip = document.createElement("span");
        chip.className = "stock-chip" + (cur && cur.stock === 0 ? " out" : "");
        chip.textContent = !cur || cur.stock == null ? "" : (cur.stock > 0 ? "📦 " + T("stockAvail") + ": " + num(cur.stock) : "📦 " + T("stockOut"));
        if (cur && cur.stock != null) box.parentElement.appendChild(chip);
    }

    // ── خلاصه و قیمت زنده (سمت سرور) ──
    let quoteTimer = null, quoteSeq = 0;
    function quote() {
        clearTimeout(quoteTimer);
        const priceEl = $("w-price");
        if (!W.variant || !W.printSize) { priceEl.textContent = "—"; return; }
        quoteTimer = setTimeout(async () => {
            const seq = ++quoteSeq;
            const r = await postJson("/order/wizard?handler=Quote", { productVariantId: W.variant.id, printSizeCode: W.printSize, printPosition: W.position, packagingTypeCode: W.packaging, quantity: 1 });
            const data = await r.json();
            if (seq !== quoteSeq || !data.ok) { if (seq === quoteSeq) priceEl.textContent = "—"; return; }
            priceEl.textContent = money(data.quote.total);
        }, 320);
    }
    function designName() {
        if (W.mode === "bank" && W.bank) return W.bank.title;
        if (W.mode === "upload" && W.fileMeta) return T("yourDesign");
        if (W.mode === "text" && W.text) return "«" + W.text.slice(0, 14) + "»";
        return null;
    }
    function renderSum() {
        const chips = [];
        chips.push([T("noDesign"), designName() || "—"]);
        chips.push([T("printSide"), W.position === "FRONT" ? T("front") : T("back")]);
        chips.push([T("size"), W.printSize || "—"]);
        const p = (CONFIG.products || []).find((x) => x.id === W.product);
        chips.push([T("product"), p ? p.name : "—"]);
        chips.push([T("shirtSize"), (W.variant && W.variant.sizeCode) || "—"]);
        chips.push([T("color"), W.colorCode ? colorName(W.colorCode) : "—"]);
        $("w-sumchips").innerHTML = chips.map((c) => '<span class="schip"></span>').join("");
        $("w-sumchips").querySelectorAll(".schip").forEach((el, i) => {
            const [k, v] = chips[i]; el.textContent = ""; el.append(k + ": ");
            const b = document.createElement("b"); b.textContent = v; el.appendChild(b);
        });
        $("w-next1").disabled = !(designName() && W.product && W.variant && W.printSize);
    }

    // ── فاز ۱: بانک طرح‌ها (جستجوی مفهومی از Core) ──
    const CHIPS = FA ? ["شیر اسطوره‌ای", "گربه فضایی", "دریا و موج", "کوه دماوند", "کهکشان", "غروب نارنجی"]
        : CULTURE === "ar" ? ["أسد أسطوري", "قطة فضائية", "البحر والموج", "جبل", "مجرة", "غروب"] : ["mythical lion", "space cat", "sea and waves", "mountain", "galaxy", "sunset"];
    CHIPS.forEach((c) => {
        const b = document.createElement("button");
        b.type = "button"; b.textContent = c;
        b.addEventListener("click", () => { $("w-q").value = c; runSearch(); });
        $("w-chips").appendChild(b);
    });
    async function runSearch() {
        const q = ($("w-q").value || "").trim();
        $("w-pulse").hidden = false;
        const grid = $("w-grid");
        grid.innerHTML = "";
        for (let i = 0; i < 6; i++) { const sk = document.createElement("div"); sk.className = "sk"; grid.appendChild(sk); }
        $("w-status").textContent = T("searching");
        try {
            const r = await fetch("/order/wizard?handler=Concept&q=" + encodeURIComponent(q) + "&limit=18", { headers: { "Accept": "application/json" } });
            const res = await r.json();
            const items = (res && (Array.isArray(res) ? res : res.items)) || [];
            $("w-pulse").hidden = true;
            $("w-status").textContent = items.length ? num(items.length) + " " + T("searchOf") + " — " + (q ? "«" + q + "»" : T("entryStore")) : T("noResults");
            grid.innerHTML = "";
            items.forEach((d) => {
                const pct = Math.max(0, Math.min(100, Number(d.score) || 0));
                const mm = pct <= 30 ? "mm-low" : pct <= 70 ? "mm-mid" : "mm-high";
                const fig = document.createElement("figure");
                fig.className = "cardart" + (W.bank && W.bank.id === d.id ? " on" : "");
                fig.innerHTML = '<div class="a">' + (d.thumbnailUrl || d.previewUrl ? '<img loading="lazy" src="' + (d.thumbnailUrl || d.previewUrl) + '" alt="">' : "") + "</div><figcaption></figcaption>" +
                    '<span class="match-meter ' + mm + '"><span class="bar" aria-hidden="true"><i style="width:' + pct + '%"></i></span><b class="pct">' + num(pct) + (CULTURE === "en" ? "%" : "٪") + "</b></span>";
                fig.querySelector("figcaption").textContent = d.title || d.code || "";
                fig.addEventListener("click", () => {
                    W.mode = "bank";
                    W.bank = { id: d.id, title: d.title || d.code, preview: d.previewUrl || d.thumbnailUrl, code: d.code };
                    grid.querySelectorAll(".cardart").forEach((x) => x.classList.remove("on"));
                    fig.classList.add("on");
                    save(); renderPreview(); renderSum(); quote();
                });
                grid.appendChild(fig);
            });
        } catch { $("w-pulse").hidden = true; $("w-status").textContent = T("noResults"); }
    }
    let stimer = null;
    $("w-q").addEventListener("input", () => { clearTimeout(stimer); stimer = setTimeout(runSearch, 380); });

    // ورود از صفحه طرح: پیش‌انتخاب طرح
    async function preselectEntryDesign() {
        const id = BOOT.entryDesign || "";
        if (!id) return;
        try {
            const r = await fetch("/order/wizard?handler=Design&id=" + id, { headers: { "Accept": "application/json" } });
            const d = await r.json();
            if (d && d.id) {
                W.mode = "bank";
                W.bank = { id: d.id, title: d.title || d.code, preview: d.previewUrl || d.thumbnailUrl, code: d.code };
                $("w-entrynote").hidden = false;
                save();
            }
        } catch { /* طرح ورودی در دسترس نیست — حالت خنثی */ }
    }

    // ── فاز ۱: آپلود ──
    function filePicked(file) {
        if (!file || !/^image\//.test(file.type)) return;
        W.file = file;
        W.fileMeta = { url: URL.createObjectURL(file), width: 0 };
        const im = new Image();
        im.onload = () => {
            W.fileMeta.width = im.naturalWidth;
            let msg, bp, best;
            if (im.naturalWidth >= 1400) { msg = T("qExcellent"); bp = 100; best = "A3"; }
            else if (im.naturalWidth >= 800) { msg = T("qGood"); bp = 72; best = "A4"; }
            else { msg = T("qLow"); bp = 42; best = "A5"; }
            $("w-check").hidden = false; $("w-checkmsg").textContent = msg;
            $("w-meter").style.width = bp + "%"; $("w-best").textContent = best;
            W.mode = "upload";
            save(); renderPreview(); renderSum(); quote();
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

    // ── فاز ۱: متن ──
    TEXT_COLORS.forEach((c, i) => {
        const d = document.createElement("button");
        d.type = "button"; d.className = "dot" + (i === 0 ? " on" : "");
        d.style.background = c; d.style.boxShadow = "inset 0 0 0 1px rgba(0,0,0,.25)";
        d.setAttribute("aria-label", T("textColor"));
        d.addEventListener("click", () => {
            $("w-tdots").querySelectorAll(".dot").forEach((x) => x.classList.remove("on"));
            d.classList.add("on");
            W.mode = "text"; W.textColor = c;
            if (!W.text) W.text = T("sampleText");
            save(); renderPreview(); renderSum(); quote();
        });
        $("w-tdots").appendChild(d);
    });
    document.querySelectorAll(".fbtn").forEach((b) => {
        b.addEventListener("click", () => {
            document.querySelectorAll(".fbtn").forEach((x) => x.classList.remove("on"));
            b.classList.add("on");
            W.mode = "text"; W.font = +b.dataset.f;
            if (!W.text) W.text = T("sampleText");
            save(); renderPreview(); renderSum(); quote();
        });
    });
    $("w-textin").addEventListener("input", (e) => { W.mode = "text"; W.text = e.target.value; save(); renderPreview(); renderSum(); quote(); });
    $("w-tsz").addEventListener("input", (e) => { W.textSize = +e.target.value; $("w-tsout").textContent = num(W.textSize); if (W.mode === "text") renderPreview(); });

    // ── فاز ۱: کنترل‌ها ──
    document.querySelectorAll(".mode").forEach((b) => {
        b.addEventListener("click", () => {
            document.querySelectorAll(".mode").forEach((x) => x.classList.remove("on"));
            b.classList.add("on");
            W.mode = b.dataset.mode;
            ["bank", "upload", "text"].forEach((m) => { $("w-pan-" + m).hidden = m !== W.mode; });
            if (W.mode === "bank" && !$("w-grid").children.length) runSearch();
            if (W.mode !== "bank") W.bank = W.mode === "bank" ? W.bank : (W.mode === "text" ? null : W.bank);
            save(); renderPreview(); renderSum(); quote();
        });
    });
    $("w-side").addEventListener("click", (e) => {
        const b = e.target.closest("button"); if (!b) return;
        W.position = b.dataset.side;
        $("w-side").querySelectorAll("button").forEach((x) => x.classList.remove("on"));
        b.classList.add("on");
        $("w-garm").classList.toggle("back", W.position === "BACK");
        save(); renderPreview(); renderSum(); quote();
    });
    $("w-center").addEventListener("click", () => {
        (W.position === "FRONT" ? fF : fB).wrap.style.transform = "";
        renderPreview();
    });
    $("w-gift").addEventListener("change", (e) => { W.gift = e.target.checked; $("w-giftbox").hidden = !W.gift; save(); });

    // ── فاز ۲: آدرس‌ها (ورود از قبل تضمین شده — گیت در شروع ویزارد) ──
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
            nu.innerHTML = "<b>＋ " + T("newAddress") + "</b><small>" + T("newAddressHint") + "</small>";
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
    $("w-next1").addEventListener("click", () => { if (!$("w-next1").disabled) goPhase(2); });
    $("w-back2").addEventListener("click", () => goPhase(1));
    $("w-back3").addEventListener("click", () => goPhase(2));
    $("w-back4").addEventListener("click", () => goPhase(3));
    $("w-confirm").addEventListener("click", () => goPhase(4));
    $("w-next2").addEventListener("click", () => {
        if (!W.packaging) { alert(T("pickPackFirst")); return; }
        goPhase(3);
    });

    // ── فاز ۳: ساخت سفارش + قیمت قطعی سرور ──
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
            text: W.mode === "text" ? W.text : null,
            font: W.font, textColor: W.textColor, textSize: W.textSize,
            productId: W.variant ? W.variant.productId || W.product : W.product,
            productVariantId: W.variant ? W.variant.id : null,
            printSizeCode: W.printSize, printSizeName: size ? size.name : W.printSize,
            printPosition: W.position,
            shirtColorCode: W.colorCode, shirtColorName: W.colorCode ? colorName(W.colorCode) : null,
            productName: p ? p.name : null,
            packagingTypeCode: W.packaging,
            quantity: 1,
            addressId: W.addressId,
            newAddress: W.newAddress ? { addressLine1: $("w-na-line").value.trim(), city: $("w-na-city").value.trim(), postalCode: $("w-na-postal").value.trim() } : null,
            gift,
        };
    }
    async function checkout() {
        const list = $("w-revlist"), lines = $("w-pricelines"), totalEl = $("w-total");
        list.innerHTML = '<p class="status">' + T("loading") + "</p>"; lines.innerHTML = ""; totalEl.textContent = "—";
        $("w-confirm").disabled = true;
        try {
            let r;
            if (W.mode === "upload" && W.file) {
                const fd = new FormData();
                fd.append("state", JSON.stringify(checkoutState()));
                fd.append("file", W.file, W.file.name);
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
            // قلم جدید به سبد اضافه شد → بج هدر به‌روز شود
            if (typeof window.barangRefreshCart === "function") window.barangRefreshCart();
            renderReview(data.pricing);
            $("w-confirm").disabled = false;
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
        holder.innerHTML = designHTML() || '<span style="color:#94a1b8;font-size:12px">—</span>';
        $("w-revtee").appendChild(holder);

        const kv = [
            [T("noDesign"), W.mode === "bank" ? (T("sourceBank") + ": " + (W.bank ? W.bank.title : "—")) : W.mode === "upload" ? T("yourFile") : T("textQuote") + ": «" + W.text + "»"],
            [T("printSide"), W.position === "FRONT" ? T("front") : T("back")],
            [T("printSize"), (size ? size.name : W.printSize) + (size && size.description ? " · " + size.description : "")],
            [T("product"), p ? p.name : "—"],
            [T("shirtSize"), (W.variant && W.variant.sizeCode) || "—"],
            [T("color"), W.colorCode ? colorName(W.colorCode) : "—"],
            [T("packaging"), pack ? pack.name : "—"],
            [T("shipTime"), T("shipTimeValue")],
        ];
        if (W.gift) kv.push([T("gift"), T("giftDetails")]);
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

    // ── فاز ۴: پرداخت ──
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
            // بج سبد خرید هدر باید خالی شود (این سفارش از پیش‌نویس خارج شد)
            if (typeof window.barangRefreshCart === "function") window.barangRefreshCart();
        } catch { alert(T("errorGeneric")); $("w-payarea").hidden = false; }
    }
    $("w-paygw").addEventListener("click", () => { $("w-gwamt").textContent = money(lastTotal) + " " + T("toman"); $("w-gw").hidden = false; });
    $("w-gwcancel").addEventListener("click", () => { $("w-gw").hidden = true; });
    $("w-gwpay").addEventListener("click", () => { $("w-gw").hidden = true; pay("gateway"); });
    $("w-paywallet").addEventListener("click", () => pay("wallet"));
    $("w-track").addEventListener("click", () => { location.href = "/account/order/" + (W.orderId || ""); });
    $("w-again").addEventListener("click", () => { location.href = "/order/wizard?new=1"; });

    // ── راه‌اندازی ──
    goPhase(W.phase || 1); // فاز فعال را نمایان می‌کند (بخش‌ها به‌صورت پیش‌فرض hidden هستند)
    // حالت اولیه: ورود مستقیم (فاز ۱)؛ تب بانک فقط وقتی فعال شود که طرح ورودی یا انتخاب کاربر باشد
    const hasBankDesign = W.mode === "bank" && W.bank;
    document.querySelectorAll(".mode").forEach((b) => { if (W.mode && b.dataset.mode === W.mode) b.classList.add("on"); });
    if (W.mode) ["bank", "upload", "text"].forEach((m) => { $("w-pan-" + m).hidden = m !== W.mode; });
    if (W.text) $("w-textin").value = W.text;
    $("w-tsout").textContent = num(W.textSize);
    $("w-garm").classList.toggle("back", W.position === "BACK");
    document.querySelectorAll("#w-side button").forEach((b) => b.classList.toggle("on", b.dataset.side === W.position));
    if (W.gift) { $("w-gift").checked = true; $("w-giftbox").hidden = false; }

    (async () => {
        await loadConfig();
        if (!hasBankDesign && !BOOT.entryDesign) {
            // حالت خنثی: هیچ تب فعالی تا انتخاب کاربر — اما برای شروع، بانک طرح‌ها پیشنهاد می‌شود
            if (!W.mode) { /* خنثی: همه پنل‌ها بسته */ }
        }
        if (BOOT.entryDesign && !(W.bank && W.bank.id === BOOT.entryDesign)) await preselectEntryDesign();
        if (W.mode === "bank" && W.bank) {
            document.querySelector('.mode[data-mode="bank"]').classList.add("on");
            $("w-pan-bank").hidden = false;
            if (!$("w-grid").children.length) runSearch();
        }
        renderSum(); renderPreview(); quote();
    })();
})();
