// پیش‌نمایش «تیشرتت رو ببین» — مودال مشترک روی کارت‌های طرح.
// موتور ترکیب: طرح (با حذف پس‌زمینه) روی عکس واقعی تیشرت در canvas کشیده می‌شود.
// - انتخاب رنگ تیشرت: عکس تیشرت همان رنگ (سفید/مشکی/قرمز/آبی)
// - انتخاب رنگ طرح: «رنگ واقعی طرح» یا هر رنگ دیگر (سیلوئت تک‌رنگ)
// - محل قرار گرفتن تصویر: جلو/پشت/آستین/سایر (کدهای PrintPosition هسته Core)
// - شروع سفارش: انتخاب‌ها در کوکی barang.preview و ورود به ویزارد سفارش
(() => {
    const ShirtColors = [
        { name: "سفید", en: "white", img: "/images/shirts/white.jpg" },
        { name: "مشکی", en: "black", img: "/images/shirts/black.jpg" },
        { name: "قرمز", en: "red", img: "/images/shirts/red.jpg" },
        { name: "آبی", en: "blue", img: "/images/shirts/blue.jpg" }
    ];
    const RealColor = "رنگ واقعی طرح";
    const TintColors = [
        { name: "قرمز", en: "red", hex: "#e23b34" },
        { name: "آبی", en: "blue", hex: "#1263dd" },
        { name: "سبز", en: "green", hex: "#1fab5c" },
        { name: "زرد", en: "yellow", hex: "#f7c62c" },
        { name: "بنفش", en: "purple", hex: "#7c5cd6" },
        { name: "نارنجی", en: "orange", hex: "#f97316" },
        { name: "مشکی", en: "black", hex: "#141418" }
    ];
    // کدهای PrintPosition سمت Core (فرم سفارش کلاینت: FRONT/BACK/SLEEVE/OTHER)
    // محل‌های چاپ — کد دقیق + کد فرم سفارش Core (FRONT/BACK/SLEEVE) و نمای تیشرت
    const Placements = [
        { code: 'FRONT_CENTER', name: 'جلو وسط', view: 'front', core: 'FRONT', zone: { x: 0.26, y: 0.25, w: 0.48, h: 0.42 } },
        { code: 'FRONT_UPPER', name: 'جلو بالا', view: 'front', core: 'FRONT', zone: { x: 0.30, y: 0.13, w: 0.40, h: 0.17 } },
        { code: 'FRONT_LOWER', name: 'جلو پایین', view: 'front', zone: { x: 0.30, y: 0.55, w: 0.40, h: 0.28 }, core: 'FRONT' },
        { code: 'BACK_CENTER', name: 'پشت وسط', view: 'back', core: 'BACK', zone: { x: 0.28, y: 0.27, w: 0.44, h: 0.40 } },
        { code: 'BACK_UPPER', name: 'پشت بالا', view: 'back', core: 'BACK', zone: { x: 0.30, y: 0.16, w: 0.40, h: 0.20 } },
        { code: 'BACK_LOWER', name: 'پشت پایین', view: 'back', core: 'BACK', zone: { x: 0.32, y: 0.56, w: 0.36, h: 0.26 } },
        { code: 'SLEEVE_LEFT', name: 'آستین چپ', view: 'front', core: 'SLEEVE', zone: { x: 0.045, y: 0.27, w: 0.17, h: 0.21 } },
        { code: 'SLEEVE_RIGHT', name: 'آستین راست', view: 'front', core: 'SLEEVE', zone: { x: 0.785, y: 0.27, w: 0.17, h: 0.21 } }
    ];
    const T = {
        title: "تیشرتت رو ببین",
        shirtColor: "انتخاب رنگ تیشرت",
        designColor: "انتخاب رنگ طرح",
        placement: "محل قرار گرفتن تصویر",
        start: "شروع سفارش",
        close: "بستن",
        previewAlt: "طرح روی تیشرت"
    };
    const CanvasSize = 640;

    let root = null, current = null, renderToken = 0;
    const designCache = new Map();

    function ensureRoot() {
        if (root) return root;
        root = document.createElement("div");
        root.className = "dpv-backdrop";
        root.innerHTML = `
        <div class="dpv-modal" role="dialog" aria-modal="true" aria-label="${T.title}">
            <button type="button" class="dpv-close" aria-label="${T.close}">×</button>
            <h3 class="dpv-title">${T.title} <span class="dpv-design-name"></span></h3>
            <div class="dpv-stage">
                <canvas class="dpv-canvas" width="${CanvasSize}" height="${CanvasSize}" aria-label="${T.previewAlt}" role="img"></canvas>
            </div>
            <div class="dpv-row"><span class="dpv-label">${T.placement}:</span><div class="dpv-chips dpv-placements"></div></div>
            <div class="dpv-row"><span class="dpv-label">${T.shirtColor}:</span><div class="dpv-swatches dpv-shirt-colors"></div></div>
            <div class="dpv-row"><span class="dpv-label">${T.designColor}:</span><div class="dpv-swatches dpv-design-colors"></div></div>
            <button type="button" class="btn btn-primary btn-lg btn-block dpv-start">${T.start}</button>
        </div>`;
        document.body.appendChild(root);
        root.querySelector(".dpv-close").addEventListener("click", close);
        root.addEventListener("click", (e) => { if (e.target === root) close(); });
        document.addEventListener("keydown", (e) => { if (e.key === "Escape" && root.classList.contains("open")) close(); });
        return root;
    }

    function loadImage(src) {
        return new Promise((resolve, reject) => {
            const im = new Image();
            im.onload = () => resolve(im);
            im.onerror = reject;
            im.src = src;
        });
    }

    // حذف پس‌زمینه با پرکردن سیلابی از لبه‌ها: سفیدیِ داخل خود طرح حفظ می‌ماند.
    function cutBackground(im) {
        const c = document.createElement("canvas");
        c.width = im.naturalWidth; c.height = im.naturalHeight;
        const ctx = c.getContext("2d", { willReadFrequently: true });
        ctx.drawImage(im, 0, 0);
        let d;
        try { d = ctx.getImageData(0, 0, c.width, c.height); } catch { return c; }
        const px = d.data, w = c.width, h = c.height;
        const isBg = (i) => px[i + 3] > 200 && px[i] > 222 && px[i + 1] > 222 && px[i + 2] > 222;
        const visited = new Uint8Array(w * h);
        const stack = [];
        for (let x = 0; x < w; x++) { stack.push(x, x + (h - 1) * w); }
        for (let y = 0; y < h; y++) { stack.push(y * w, y * w + w - 1); }
        while (stack.length) {
            const p = stack.pop();
            if (visited[p]) continue;
            visited[p] = 1;
            const i = p * 4;
            if (!isBg(i)) continue;
            px[i + 3] = 0;
            const x = p % w, y = (p / w) | 0;
            if (x > 0) stack.push(p - 1);
            if (x < w - 1) stack.push(p + 1);
            if (y > 0) stack.push(p - w);
            if (y < h - 1) stack.push(p + w);
        }
        // نرم‌کردن لبه‌های برش
        for (let p = 0; p < w * h; p++) {
            if (visited[p]) continue;
            const i = p * 4;
            if (px[i + 3] === 0) continue;
            const x = p % w, y = (p / w) | 0;
            let edge = false;
            if ((x > 0 && px[(p - 1) * 4 + 3] === 0) || (x < w - 1 && px[(p + 1) * 4 + 3] === 0) ||
                (y > 0 && px[(p - w) * 4 + 3] === 0) || (y < h - 1 && px[(p + w) * 4 + 3] === 0)) edge = true;
            if (edge && px[i] > 205 && px[i + 1] > 205 && px[i + 2] > 205) px[i + 3] = 110;
        }
        ctx.putImageData(d, 0, 0);
        return c;
    }

    async function getDesignCut(url) {
        if (designCache.has(url)) return designCache.get(url);
        // از پروکسی هم‌مبدأ می‌رویم تا canvas آلوده (tainted) نشود
        const proxied = "/design-image?u=" + encodeURIComponent(url);
        const cut = await loadImage(proxied).then(cutBackground).catch(() => null);
        designCache.set(url, cut);
        return cut;
    }

    function tintCanvas(src, color) {
        const c = document.createElement("canvas");
        c.width = src.width; c.height = src.height;
        const ctx = c.getContext("2d");
        ctx.drawImage(src, 0, 0);
        ctx.globalCompositeOperation = "source-in";
        ctx.fillStyle = color;
        ctx.fillRect(0, 0, c.width, c.height);
        return c;
    }

    // بر اساس نمای محل انتخابی (جلو/پشت)، عکس تیشرت مربوطه را برمی‌گرداند
    function updateShirtImage() {
        if (!current) return;
        const picked = Placements.find(p => p.code === current.placement) || Placements[0];
        const base = current.colorImg || ShirtColors[0].img;
        current.shirtImg = picked.view === "back" ? base.replace("/shirts/", "/shirts/back-") : base;
    }

    function pickedPlacement() {
        const b = root.querySelector(".dpv-placements .picked");
        return b ? b.dataset.code : "FRONT";
    }

    async function render() {
        const token = ++renderToken;
        const canvas = root.querySelector(".dpv-canvas");
        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, CanvasSize, CanvasSize);
        if (!current) return;
        const shirtImg = await loadImage(current.shirtImg).catch(() => null);
        if (token !== renderToken) return;
        if (shirtImg) ctx.drawImage(shirtImg, 0, 0, CanvasSize, CanvasSize);
        const cut = await getDesignCut(current.thumb || "");
        if (!cut || token !== renderToken) return;
        const designSwatch = root.querySelector(".dpv-design-colors .picked");
        const useTint = designSwatch && designSwatch.dataset.hex;
        const layer = useTint ? tintCanvas(cut, designSwatch.dataset.hex) : cut;
        const picked = Placements.find(p => p.code === pickedPlacement()) || Placements[0];
        const zone = picked.zone;
        const zx = zone.x * CanvasSize, zy = zone.y * CanvasSize;
        const zw = zone.w * CanvasSize, zh = zone.h * CanvasSize;
        const scale = Math.min(zw / cut.width, zh / cut.height);
        const dw = cut.width * scale, dh = cut.height * scale;
        ctx.save();
        ctx.globalAlpha = 0.96;
        ctx.drawImage(layer, zx + (zw - dw) / 2, zy + (zh - dh) / 2, dw, dh);
        ctx.restore();
    }

    function swatch(color, group) {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "dpv-swatch" + (color.hex ? "" : " dpv-swatch-real");
        b.style.setProperty("--sw", color.hex || "");
        b.dataset.name = color.name;
        b.dataset.en = color.en || "";
        b.dataset.hex = color.hex || "";
        b.dataset.img = color.img || "";
        b.title = color.name;
        b.setAttribute("aria-label", color.name);
        b.addEventListener("click", () => {
            group.querySelectorAll(".dpv-swatch").forEach(x => x.classList.remove("picked"));
            b.classList.add("picked");
            if (current) { current.placement = b.dataset.code; updateShirtImage(); }
            render();
        });
        return b;
    }

    function placementChip(p, group) {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "dpv-chip";
        b.dataset.code = p.code;
        b.dataset.name = p.name;
        b.textContent = p.name;
        b.addEventListener("click", () => {
            group.querySelectorAll(".dpv-chip").forEach(x => x.classList.remove("picked"));
            b.classList.add("picked");
            render();
        });
        return b;
    }

    async function open(data) {
        current = { ...data, addto: data.addto || "", colorImg: ShirtColors[0].img, placement: "FRONT_CENTER" };
        updateShirtImage();
        const r = ensureRoot();
        r.classList.add("open");
        document.body.style.overflow = "hidden";
        r.querySelector(".dpv-design-name").textContent = data.title ? `— ${data.title}` : "";
        const shirtBox = r.querySelector(".dpv-shirt-colors");
        const designBox = r.querySelector(".dpv-design-colors");
        const placementBox = r.querySelector(".dpv-placements");
        shirtBox.innerHTML = "";
        designBox.innerHTML = "";
        placementBox.innerHTML = "";
        ShirtColors.forEach((c, i) => {
            const b = swatch({ ...c, hex: c.en === "black" ? "#1a1a1e" : c.en === "red" ? "#e23b34" : c.en === "blue" ? "#1263dd" : "#f2f2f2" }, shirtBox);
            if (i === 0) b.classList.add("picked");
            shirtBox.appendChild(b);
        });
        designBox.appendChild(swatch({ name: RealColor, hex: "" }, designBox));
        TintColors.forEach(c => designBox.appendChild(swatch(c, designBox)));
        Placements.forEach((p, i) => {
            const b = placementChip(p, placementBox);
            if (i === 0) b.classList.add("picked");
            placementBox.appendChild(b);
        });
        render();
    }

    function close() {
        if (!root) return;
        root.classList.remove("open");
        document.body.style.overflow = "";
    }

    function startOrder() {
        if (!current) return;
        const shirt = document.querySelector(".dpv-shirt-colors .picked");
        const design = document.querySelector(".dpv-design-colors .picked");
        const placement = document.querySelector(".dpv-placements .picked");
        const payload = {
            designId: current.id,
            shirtColor: shirt ? shirt.dataset.name : "سفید",
            designColor: design ? design.dataset.name : "",
            placement: placement ? placement.dataset.code : "FRONT_CENTER",
            placementName: placement ? placement.dataset.name : "جلو وسط",
            corePosition: placement ? (Placements.find(p => p.code === placement.dataset.code)?.core || "FRONT") : "FRONT"
        };
        document.cookie = "barang.preview=" + encodeURIComponent(JSON.stringify(payload)) + "; path=/; max-age=86400; samesite=lax";
        // داخل ویزارد (مرحله انتخاب طرح): مستقیم برو مرحله تیشرت؛ بیرون ویزارد: از مرحله طرح شروع کن
        // ویزارد دیگر مرحله «انتخاب طرح» ندارد؛ همین‌جا مستقیم مرحله تیشرت شروع می‌شود
        const addto = current.addto && current.addto !== "00000000-0000-0000-0000-000000000000"
            ? "?addToOrderId=" + current.addto
            : "";
        location.href = `/order/shirt/${current.id}` + addto;
    }

    document.addEventListener("click", (e) => {
        const btn = e.target.closest(".preview-shirt-btn");
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        let data = {};
        try { data = JSON.parse(btn.dataset.design || "{}"); } catch { data = {}; }
        open(data);
    });

    // هر جا کاربر قصد شروع سفارش از روی یک طرح را دارد (کارت‌های data-preview-card)
    // اول این پاپ‌آپ باز می‌شود؛ ورود مستقیم به ویزارد ممکن نیست.
    document.addEventListener("click", (e) => {
        const card = e.target.closest("[data-preview-card]");
        if (!card) return;
        e.preventDefault();
        e.stopPropagation();
        let data = {};
        try { data = JSON.parse(card.dataset.previewCard || "{}"); } catch { data = {}; }
        open(data);
    });

    document.addEventListener("click", (e) => {
        const start = e.target.closest(".dpv-start");
        if (start) startOrder();
    });
})();
