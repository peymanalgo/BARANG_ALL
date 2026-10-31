// پنجره «طراحی تیشرت» — نقطه ورود واحد ثبت سفارش.
// فقط از دو دکمه صفحه اصلی («شروع طراحی تیشرت» و «شروع کن» در بنر اولین سفارش)
// باز می‌شود. کاربر داخل پنجره طرح را انتخاب می‌کند، رنگ تیشرت و رنگ طرح و
// محل قرار گرفتن تصویر را تنظیم می‌کند، طرح را می‌کشد و با «شروع سفارش»
// وارد مرحله تیشرت ویزارد می‌شود (مرحله «انتخاب طرح» در ویزارد وجود ندارد).
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
    // محل‌های چاپ — کد دقیق + کد فرم سفارش Core (FRONT/BACK/SLEEVE) + نمای تیشرت + ناحیه پیش‌فرض + ناحیه مجاز
    const Placements = [
        { code: "FRONT_CENTER", name: "جلو وسط", view: "front", core: "FRONT", zone: { x: 0.26, y: 0.24, w: 0.48, h: 0.42 }, allowed: { x: 0.20, y: 0.13, w: 0.60, h: 0.62 } },
        { code: "FRONT_UPPER", name: "جلو بالا", view: "front", core: "FRONT", zone: { x: 0.30, y: 0.13, w: 0.40, h: 0.16 }, allowed: { x: 0.22, y: 0.12, w: 0.56, h: 0.22 } },
        { code: "FRONT_LOWER", name: "جلو پایین", view: "front", core: "FRONT", zone: { x: 0.30, y: 0.55, w: 0.40, h: 0.27 }, allowed: { x: 0.22, y: 0.45, w: 0.56, h: 0.38 } },
        { code: "BACK_CENTER", name: "پشت وسط", view: "back", core: "BACK", zone: { x: 0.26, y: 0.24, w: 0.48, h: 0.42 }, allowed: { x: 0.20, y: 0.13, w: 0.60, h: 0.62 } },
        { code: "BACK_UPPER", name: "پشت بالا", view: "back", core: "BACK", zone: { x: 0.30, y: 0.13, w: 0.40, h: 0.16 }, allowed: { x: 0.22, y: 0.12, w: 0.56, h: 0.22 } },
        { code: "BACK_LOWER", name: "پشت پایین", view: "back", core: "BACK", zone: { x: 0.30, y: 0.55, w: 0.40, h: 0.27 }, allowed: { x: 0.22, y: 0.45, w: 0.56, h: 0.38 } },
        { code: "SLEEVE_LEFT", name: "آستین چپ", view: "front", core: "SLEEVE", zone: { x: 0.05, y: 0.28, w: 0.15, h: 0.19 }, allowed: { x: 0.03, y: 0.25, w: 0.19, h: 0.24 } },
        { code: "SLEEVE_RIGHT", name: "آستین راست", view: "front", core: "SLEEVE", zone: { x: 0.80, y: 0.28, w: 0.15, h: 0.19 }, allowed: { x: 0.78, y: 0.25, w: 0.19, h: 0.24 } }
    ];
    const T = {
        title: "طراحی تیشرت",
        pickDesign: "۱. طرح خود را انتخاب کنید",
        placement: "۲. محل قرار گرفتن تصویر",
        shirtColor: "۳. انتخاب رنگ تیشرت",
        designColor: "۴. انتخاب رنگ طرح",
        start: "شروع سفارش",
        close: "بستن",
        previewAlt: "طرح روی تیشرت",
        offShirt: "لطفاً تصویر را کاملاً بر روی تیشرت قرار دهید.",
        pickFirst: "اول یک طرح انتخاب کنید."
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
            <div class="dpv-picker" hidden>
                <p class="dpv-picker-hint">${T.pickDesign}</p>
                <div class="dpv-picker-grid"></div>
            </div>
            <div class="dpv-stage" hidden>
                <canvas class="dpv-canvas" width="${CanvasSize}" height="${CanvasSize}" aria-label="${T.previewAlt}" role="img"></canvas>
            </div>
            <div class="dpv-error" hidden role="alert"></div>
            <div class="dpv-config" hidden>
                <div class="dpv-row"><span class="dpv-label">${T.placement}:</span><div class="dpv-chips dpv-placements"></div></div>
                <div class="dpv-row"><span class="dpv-label">${T.shirtColor}:</span><div class="dpv-swatches dpv-shirt-colors"></div></div>
                <div class="dpv-row"><span class="dpv-label">${T.designColor}:</span><div class="dpv-swatches dpv-design-colors"></div></div>
                <button type="button" class="btn btn-primary btn-lg btn-block dpv-start">${T.start}</button>
            </div>
        </div>`;
        document.body.appendChild(root);
        root.querySelector(".dpv-close").addEventListener("click", close);
        root.addEventListener("click", (e) => { if (e.target === root) close(); });
        document.addEventListener("keydown", (e) => { if (e.key === "Escape" && root.classList.contains("open")) close(); });
        const canvas = root.querySelector(".dpv-canvas");
        canvas.addEventListener("pointerdown", pointerDown);
        canvas.addEventListener("pointermove", pointerMove);
        canvas.addEventListener("pointerup", pointerUp);
        canvas.addEventListener("pointercancel", pointerUp);
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

    function pickedPlacement() {
        const b = root.querySelector(".dpv-placements .picked");
        return Placements.find(p => p.code === (b ? b.dataset.code : "")) || Placements[0];
    }

    // اندازه طرح با مقیاس ناحیه پیش‌فرض محل انتخابی
    function designSize(cut, picked) {
        const zw = picked.zone.w * CanvasSize, zh = picked.zone.h * CanvasSize;
        const scale = Math.min(zw / cut.width, zh / cut.height);
        return { w: cut.width * scale, h: cut.height * scale };
    }

    // مرکز پیش‌فرض طرح = مرکز ناحیه پیش‌فرض محل
    function resetPosition(picked) {
        current.pos = {
            cx: (picked.zone.x + picked.zone.w / 2) * CanvasSize,
            cy: (picked.zone.y + picked.zone.h / 2) * CanvasSize
        };
        current.posFor = picked.code;
    }

    async function render() {
        const token = ++renderToken;
        const canvas = root.querySelector(".dpv-canvas");
        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, CanvasSize, CanvasSize);
        if (!current || !current.thumb) return;
        const picked = pickedPlacement();
        const shirtImg = await loadImage(current.shirtImg).catch(() => null);
        if (token !== renderToken) return;
        if (shirtImg) ctx.drawImage(shirtImg, 0, 0, CanvasSize, CanvasSize);
        const cut = await getDesignCut(current.thumb);
        if (!cut || token !== renderToken) return;
        const designSwatch = root.querySelector(".dpv-design-colors .picked");
        const useTint = designSwatch && designSwatch.dataset.hex;
        const layer = useTint ? tintCanvas(cut, designSwatch.dataset.hex) : cut;
        const size = designSize(cut, picked);
        if (!current.pos || current.posFor !== picked.code) resetPosition(picked);
        const dx = current.pos.cx - size.w / 2, dy = current.pos.cy - size.h / 2;
        ctx.save();
        ctx.globalAlpha = 0.96;
        ctx.drawImage(layer, dx, dy, size.w, size.h);
        ctx.restore();
        current.cut = cut;
        current.layer = layer;
        current.rect = { x: dx, y: dy, w: size.w, h: size.h };
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
            if (b.dataset.img && current) current.colorImg = b.dataset.img;
            updateShirtImage();
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
            if (current) { current.placement = p.code; updateShirtImage(); resetPosition(p); hideError(); }
            render();
        });
        return b;
    }

    // بر اساس نمای محل انتخابی (جلو/پشت)، عکس تیشرت مربوطه را برمی‌گرداند
    function updateShirtImage() {
        if (!current) return;
        const picked = pickedPlacement();
        const base = current.colorImg || ShirtColors[0].img;
        current.shirtImg = picked.view === "back" ? base.replace("/shirts/", "/shirts/back-") : base;
    }

    function showError(msg) {
        const el = root.querySelector(".dpv-error");
        el.textContent = msg;
        el.hidden = false;
    }
    function hideError() {
        if (root) root.querySelector(".dpv-error").hidden = true;
    }

    // ---- درگ طرح روی بوم ----
    function canvasPoint(e) {
        const c = root.querySelector(".dpv-canvas");
        const r = c.getBoundingClientRect();
        return {
            x: (e.clientX - r.left) * CanvasSize / r.width,
            y: (e.clientY - r.top) * CanvasSize / r.height
        };
    }

    function pointerDown(e) {
        if (!current || !current.rect) return;
        const p = canvasPoint(e);
        const r = current.rect;
        if (!(p.x >= r.x && p.x <= r.x + r.w && p.y >= r.y && p.y <= r.y + r.h)) return;
        e.preventDefault();
        hideError();
        current.drag = { ox: p.x - r.x, oy: p.y - r.y };
        try { e.target.setPointerCapture(e.pointerId); } catch { /* اشاره‌گر مصنوعی */ }
        e.target.style.cursor = "grabbing";
    }

    function pointerMove(e) {
        if (!current || !current.drag) return;
        e.preventDefault();
        const p = canvasPoint(e);
        current.pos = {
            cx: Math.max(0, Math.min(CanvasSize, p.x - current.drag.ox + current.rect.w / 2)),
            cy: Math.max(0, Math.min(CanvasSize, p.y - current.drag.oy + current.rect.h / 2))
        };
        render();
    }

    function pointerUp(e) {
        if (current && current.drag) {
            current.drag = null;
            const c = root.querySelector(".dpv-canvas");
            c.style.cursor = "default";
        }
    }

    function setMode(mode) {
        // "picker": انتخاب طرح | "design": طرح انتخاب شده، تیشرت و تنظیمات
        root.querySelector(".dpv-picker").hidden = mode !== "picker";
        root.querySelector(".dpv-stage").hidden = mode !== "design";
        root.querySelector(".dpv-config").hidden = mode !== "design";
    }

    // ورود به پنجره: با فهرست طرح‌ها (حالت انتخاب) یا یک طرح مشخص
    async function open(designs, addto) {
        current = { list: designs || [], addto: addto || "", colorImg: ShirtColors[0].img, placement: "FRONT_CENTER" };
        const r = ensureRoot();
        updateShirtImage();
        r.classList.add("open");
        document.body.style.overflow = "hidden";
        r.querySelector(".dpv-design-name").textContent = "";
        hideError();
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
        if (current.list.length > 0) {
            const grid = r.querySelector(".dpv-picker-grid");
            grid.innerHTML = "";
            for (const d of current.list) {
                const item = document.createElement("button");
                item.type = "button";
                item.className = "dpv-picker-item";
                const img = document.createElement("img");
                img.src = d.thumb || "";
                img.alt = d.title || "";
                img.loading = "lazy";
                item.appendChild(img);
                const label = document.createElement("span");
                label.textContent = d.title || "";
                item.appendChild(label);
                item.addEventListener("click", () => selectDesign(d));
                grid.appendChild(item);
            }
            setMode("picker");
        } else {
            setMode("design");
            await render();
        }
    }

    async function selectDesign(d) {
        current.id = d.id;
        current.title = d.title || "";
        current.thumb = d.thumb || "";
        root.querySelector(".dpv-design-name").textContent = current.title ? `— ${current.title}` : "";
        hideError();
        setMode("design");
        await render();
    }

    function close() {
        if (!root) return;
        root.classList.remove("open");
        document.body.style.overflow = "";
    }

    function startOrder() {
        if (!current || !current.thumb) return;
        // اعتبارسنجی: کل تصویر باید داخل ناحیه مجازِ محل انتخابی روی تیشرت باشد
        const picked = pickedPlacement();
        const r = current.rect;
        const allowed = picked.allowed;
        const fullyOn = r
            && r.x >= allowed.x * CanvasSize
            && r.y >= allowed.y * CanvasSize
            && r.x + r.w <= (allowed.x + allowed.w) * CanvasSize
            && r.y + r.h <= (allowed.y + allowed.h) * CanvasSize;
        if (!fullyOn) {
            showError(T.offShirt);
            return;
        }
        hideError();
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
        // ویزارد مرحله «انتخاب طرح» ندارد؛ مستقیم مرحله تیشرت (+ افزودن به سفارش موجود در صورت وجود)
        const addto = current.addto && current.addto !== "00000000-0000-0000-0000-000000000000"
            ? "?addToOrderId=" + current.addto
            : "";
        close();
        location.href = `/order/shirt/${current.id}` + addto;
    }

    // تنها ورودهای پنجره: دکمه‌های «شروع طراحی تیشرت» و «شروع کن» صفحه اصلی
    document.addEventListener("click", (e) => {
        const btn = e.target.closest(".start-design-btn");
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        let designs = [];
        try { designs = JSON.parse(btn.dataset.designs || "[]"); } catch { designs = []; }
        open(designs, btn.dataset.addto || "");
    });

    document.addEventListener("click", (e) => {
        const start = e.target.closest(".dpv-start");
        if (start) startOrder();
    });
})();
