// پیش‌نمایش «تیشرتت رو ببین» — مودال مشترک روی کارت‌های طرح.
// هر کارتی که دکمه .preview-shirt-btn داشته باشد، کلیک روی آن این مودال را باز می‌کند:
// طرح روی تیشرت سفید + انتخاب رنگ تیشرت + انتخاب رنگ طرح + دکمه شروع سفارش.
// «شروع سفارش» انتخاب‌ها را در کوکی barang.preview می‌گذارد و وارد ویزارد سفارش می‌شود.
(() => {
    const ShirtColors = [
        { name: "سفید", en: "white", hex: "#ffffff" },
        { name: "مشکی", en: "black", hex: "#1a1a1e" },
        { name: "نارنجی", en: "orange", hex: "#f97316" },
        { name: "قرمز", en: "red", hex: "#ef4444" },
        { name: "سبز", en: "green", hex: "#22c55e" },
        { name: "بنفش", en: "purple", hex: "#8b5cf6" },
        { name: "زرد", en: "yellow", hex: "#fbbf24" },
        { name: "آبی", en: "blue", hex: "#3b82f6" }
    ];
    const T = {
        title: "تیشرتت رو ببین",
        shirtColor: "انتخاب رنگ تیشرت",
        designColor: "انتخاب رنگ طرح",
        realColor: "رنگ واقعی طرح",
        start: "شروع سفارش",
        close: "بستن",
        previewAlt: "طرح روی تیشرت",
        designLabel: "طرح:"
    };

    let root = null, current = null;

    function ensureRoot() {
        if (root) return root;
        root = document.createElement("div");
        root.className = "dpv-backdrop";
        root.innerHTML = `
        <div class="dpv-modal" role="dialog" aria-modal="true" aria-label="${T.title}">
            <button type="button" class="dpv-close" aria-label="${T.close}">×</button>
            <h3 class="dpv-title">${T.title} <span class="dpv-design-name"></span></h3>
            <div class="dpv-stage">
                <div class="dpv-shirt-wrap">
                    <svg class="dpv-shirt" viewBox="0 0 300 340" fill="none" aria-hidden="true">
                        <path class="dpv-shirt-body" d="M95 18 L118 8 C126 20 174 20 182 8 L205 18 L262 52 L242 96 L216 82 L216 322 C216 330 210 336 202 336 L98 336 C90 336 84 330 84 322 L84 82 L58 96 L38 52 Z" fill="#ffffff" stroke="#d4d9e2" stroke-width="3"/>
                        <path class="dpv-shirt-collar" d="M118 8 C126 20 174 20 182 8 L188 11 C179 26 121 26 112 11 Z" fill="#eef2f7"/>
                    </svg>
                    <div class="dpv-design"><img alt="${T.previewAlt}" draggable="false" /></div>
                </div>
            </div>
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

    function swatch(color, group) {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "dpv-swatch" + (color.hex ? "" : " dpv-swatch-real");
        b.style.setProperty("--sw", color.hex || "");
        b.dataset.name = color.name;
        b.dataset.en = color.en || "";
        b.dataset.hex = color.hex || "";
        b.title = color.name;
        b.setAttribute("aria-label", color.name);
        b.addEventListener("click", () => {
            group.querySelectorAll(".dpv-swatch").forEach(x => x.classList.remove("picked"));
            b.classList.add("picked");
            apply();
        });
        return b;
    }

    function apply() {
        const shirt = root.querySelector(".dpv-shirt-colors .picked");
        const design = root.querySelector(".dpv-design-colors .picked");
        const shirtHex = shirt ? shirt.dataset.hex : "#ffffff";
        const body = root.querySelector(".dpv-shirt-body");
        const collar = root.querySelector(".dpv-shirt-collar");
        body.setAttribute("fill", shirtHex);
        // یقه و دوربازن بر اساس تیرگی رنگ تیشرت
        const dark = isDark(shirtHex);
        body.setAttribute("stroke", dark ? "#3a3a42" : "#d4d9e2");
        collar.setAttribute("fill", dark ? shade(shirtHex, -0.18) : shade(shirtHex, -0.08));
        root.querySelector(".dpv-design").dataset.mode = design && design.dataset.hex ? "tint" : "real";
        root.querySelector(".dpv-design").style.setProperty("--tint", design && design.dataset.hex ? design.dataset.hex : "transparent");
    }

    function isDark(hex) {
        const m = hex.replace("#", "");
        const r = parseInt(m.substr(0, 2), 16), g = parseInt(m.substr(2, 2), 16), b = parseInt(m.substr(4, 2), 16);
        return (0.299 * r + 0.587 * g + 0.114 * b) < 140;
    }
    function shade(hex, k) {
        const m = hex.replace("#", "");
        const f = (v) => Math.max(0, Math.min(255, Math.round(v * (1 + k))));
        const r = f(parseInt(m.substr(0, 2), 16)), g = f(parseInt(m.substr(2, 2), 16)), b = f(parseInt(m.substr(4, 2), 16));
        return "#" + [r, g, b].map(v => v.toString(16).padStart(2, "0")).join("");
    }

    // حذف پس‌زمینه سفید بندانگشتی با canvas تا طرح واقعاً روی رنگ تیشرت بنشیند؛
    // خروجی هم به‌عنوان تصویر برش‌خورده و هم به‌عنوان ماسک آلفا برای رنگی‌کردن استفاده می‌شود.
    function cutout(url) {
        return new Promise((resolve) => {
            const im = new Image();
            im.crossOrigin = "anonymous";
            im.onload = () => {
                try {
                    const c = document.createElement("canvas");
                    c.width = im.naturalWidth; c.height = im.naturalHeight;
                    const ctx = c.getContext("2d");
                    ctx.drawImage(im, 0, 0);
                    const d = ctx.getImageData(0, 0, c.width, c.height);
                    const px = d.data;
                    for (let i = 0; i < px.length; i += 4) {
                        if (px[i + 3] === 0) continue;
                        const lum = 0.299 * px[i] + 0.587 * px[i + 1] + 0.114 * px[i + 2];
                        if (lum > 228 && px[i + 3] > 200) px[i + 3] = 0;
                    }
                    ctx.putImageData(d, 0, 0);
                    resolve(c.toDataURL("image/png"));
                } catch { resolve(url); }
            };
            im.onerror = () => resolve(url);
            im.src = url;
        });
    }

    async function open(data) {
        current = data;
        const r = ensureRoot();
        r.classList.add("open");
        document.body.style.overflow = "hidden";
        r.querySelector(".dpv-design-name").textContent = data.title ? `— ${data.title}` : "";
        const cut = await cutout(data.thumb || "");
        const img = r.querySelector(".dpv-design img");
        img.src = cut;
        r.querySelector(".dpv-design").style.setProperty("--dpv-img", `url("${cut}")`);
        const shirtBox = r.querySelector(".dpv-shirt-colors");
        const designBox = r.querySelector(".dpv-design-colors");
        shirtBox.innerHTML = "";
        designBox.innerHTML = "";
        ShirtColors.forEach((c, i) => shirtBox.appendChild(swatch(c, shirtBox)));
        const real = { name: T.realColor, hex: "" };
        designBox.appendChild(swatch(real, designBox));
        ShirtColors.filter(c => c.en !== "white").forEach(c => designBox.appendChild(swatch(c, designBox)));
        shirtBox.children[0].classList.add("picked");
        designBox.children[0].classList.add("picked");
        apply();
        r.classList.add("open");
        document.body.style.overflow = "hidden";
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
        const payload = {
            designId: current.id,
            shirtColor: shirt ? shirt.dataset.name : "سفید",
            shirtHex: shirt ? shirt.dataset.hex : "#ffffff",
            designColor: design ? design.dataset.name : "",
            designHex: design && design.dataset.hex ? design.dataset.hex : ""
        };
        document.cookie = "barang.preview=" + encodeURIComponent(JSON.stringify(payload)) + "; path=/; max-age=86400; samesite=lax";
        // داخل ویزارد (مرحله انتخاب طرح): مستقیم برو مرحله تیشرت؛ بیرون ویزارد: از مرحله طرح شروع کن
        const inWizard = location.pathname.toLowerCase().startsWith("/order/select");
        close();
        location.href = inWizard
            ? `/order/shirt/${current.id}`
            : `/order/select?design=${current.id}`;
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

    document.addEventListener("click", (e) => {
        const start = e.target.closest(".dpv-start");
        if (start) startOrder();
    });
})();
