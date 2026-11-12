// بستن منوی زبان با کلیک بیرون از آن + reveal اسکرول + سایه هدر
(() => {
    const details = document.querySelectorAll("details.lang-menu");
    document.addEventListener("click", (event) => {
        details.forEach((d) => {
            if (d.open && !d.contains(event.target)) d.removeAttribute("open");
        });
    });

    // reveal on scroll
    const observer = new IntersectionObserver((entries) => {
        entries.forEach((e) => {
            if (e.isIntersecting) { e.target.classList.add("in"); observer.unobserve(e.target); }
        });
    }, { threshold: 0.12 });
    document.querySelectorAll(".reveal").forEach((el) => observer.observe(el));

    // header shadow on scroll
    const header = document.querySelector(".store-header");
    if (header) {
        const onScroll = () => header.classList.toggle("scrolled", window.scrollY > 8);
        onScroll();
        window.addEventListener("scroll", onScroll, { passive: true });
    }
})();


// PWA: ثبت Service Worker + دکمه نصب + به‌روزرسانی خودکار
(() => {
    if (!("serviceWorker" in navigator)) return;

    let refreshing = false;
    // فقط وقتی صفحه از قبل تحت کنترل SW است، تحویل نسخه جدید → یک تازه‌سازی
    // (بازدید اول نباید ری‌لود شود)
    if (navigator.serviceWorker.controller) {
        navigator.serviceWorker.addEventListener("controllerchange", () => {
            if (refreshing) return;
            refreshing = true;
            window.location.reload();
        });
    }

    window.addEventListener("load", () => {
        navigator.serviceWorker.register("/sw.js").catch(() => {});
    });

    // ---- دکمه نصب (اندروید/کروم/اج/دسکتاپ) ----
    const I18N = {
        fa: { install: "نصب اپلیکیشن پی رنگ", iosHint: "برای نصب اپ، در سافاری دکمه Share و سپس «Add to Home Screen» را بزنید." },
        en: { install: "Install the Peyrang app", iosHint: "To install, tap the Share button in Safari, then “Add to Home Screen”." },
        ar: { install: "ثبّت تطبيق پی رنگ", iosHint: "للتثبيت، اضغط زر المشاركة في سفاري ثم «أضف إلى الشاشة الرئيسية»." }
    };
    const lang = (document.documentElement.lang || "fa").slice(0, 2);
    const t = I18N[lang] || I18N.fa;
    const standalone = window.matchMedia("(display-mode: standalone)").matches || navigator.standalone === true;
    if (standalone) return; // داخل اپ نصب‌شده نیازی به دکمه نیست

    let deferredPrompt = null;
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "pwa-install-btn";
    btn.hidden = true;
    btn.textContent = t.install;
    btn.addEventListener("click", async () => {
        if (!deferredPrompt) return;
        deferredPrompt.prompt();
        const { outcome } = await deferredPrompt.userChoice;
        if (outcome === "accepted") localStorage.setItem("pirang-install-done", "1");
        btn.hidden = true;
        deferredPrompt = null;
    });
    document.body.appendChild(btn);

    window.addEventListener("beforeinstallprompt", (e) => {
        if (localStorage.getItem("pirang-install-done") === "1") return;
        e.preventDefault();
        deferredPrompt = e;
        btn.hidden = false;
    });
    window.addEventListener("appinstalled", () => {
        localStorage.setItem("pirang-install-done", "1");
        btn.hidden = true;
    });

    // iOS سافاری beforeinstallprompt ندارد — راهنمای متنی یک‌باره
    const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
    if (isIos && !localStorage.getItem("pirang-ios-hint")) {
        const hint = document.createElement("div");
        hint.className = "pwa-ios-hint";
        hint.textContent = t.iosHint;
        const dismiss = () => { hint.remove(); localStorage.setItem("pirang-ios-hint", "1"); };
        hint.addEventListener("click", dismiss);
        document.body.appendChild(hint);
        setTimeout(dismiss, 15000);
    }
})();
// فلش‌های نوار دسته‌ها
document.querySelectorAll('[data-strip-scroll]').forEach((btn) => {
    btn.addEventListener('click', () => {
        const strip = btn.parentElement.querySelector('[data-strip]');
        if (strip) strip.scrollBy({ left: (btn.dataset.stripScroll === '1' ? 1 : -1) * 320 * (document.dir === 'ltr' ? 1 : -1), behavior: 'smooth' });
    });
});
