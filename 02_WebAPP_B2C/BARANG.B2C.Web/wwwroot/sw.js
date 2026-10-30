// پیرنگ — Service Worker
// استاتیک‌ها: stale-while-revalidate | صفحات: network-first با fallback آفلاین
// API عمومی کاتالوگ: network-first با کش (مرور آفلاین کاتالوگ)
const VERSION = "pirang-v1";
const STATIC_CACHE = `${VERSION}-static`;
const PAGE_CACHE = `${VERSION}-pages`;
const API_CACHE = `${VERSION}-api`;
const OFFLINE_URL = "/offline.html";

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE).then((cache) =>
            cache.addAll([OFFLINE_URL, "/css/store.css", "/js/store.js", "/icons/icon-192.png", "/icons/icon-512.png"])
        ).then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.filter((k) => !k.startsWith(VERSION)).map((k) => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", (event) => {
    const request = event.request;
    if (request.method !== "GET") return;
    const url = new URL(request.url);
    if (url.origin !== location.origin) return;

    // صفحات (ناوبری): شبکه اول، fallback آفلاین
    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).then((response) => {
                const copy = response.clone();
                caches.open(PAGE_CACHE).then((c) => c.put(request, copy));
                return response;
            }).catch(() =>
                caches.match(request).then((hit) => hit || caches.match(OFFLINE_URL))
            )
        );
        return;
    }

    // API عمومی کاتالوگ: network-first + کش
        // wallet and order data must always be fresh (balance, payment state)
    if (url.pathname.includes('/wallet') || url.pathname.includes('/client/orders')) {
        event.respondWith(fetch(request));
        return;
    }
if (url.pathname.startsWith("/api/v1/public") || url.pathname.startsWith("/api/v1/lookups")) {
        event.respondWith(
            fetch(request).then((response) => {
                if (response.ok) {
                    const copy = response.clone();
                    caches.open(API_CACHE).then((c) => c.put(request, copy));
                }
                return response;
            }).catch(() => caches.match(request))
        );
        return;
    }

    // استاتیک‌ها (css/js/font/icon/image): stale-while-revalidate
    if (/\.(css|js|woff2?|png|jpe?g|webp|svg|ico)$/i.test(url.pathname) || url.pathname.startsWith("/icons/") || url.pathname.startsWith("/images/")) {
        event.respondWith(
            caches.match(request).then((hit) => {
                const network = fetch(request).then((response) => {
                    if (response.ok) {
                        const copy = response.clone();
                        caches.open(STATIC_CACHE).then((c) => c.put(request, copy));
                    }
                    return response;
                }).catch(() => hit);
                return hit || network;
            })
        );
    }
});
