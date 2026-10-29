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


// ثبت Service Worker (PWA)
if ("serviceWorker" in navigator) {
    window.addEventListener("load", () => {
        navigator.serviceWorker.register("/sw.js").catch(() => {});
    });
}