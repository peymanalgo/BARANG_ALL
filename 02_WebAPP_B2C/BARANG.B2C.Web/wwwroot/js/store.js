// بستن منوی زبان با کلیک بیرون از آن
(() => {
    const details = document.querySelectorAll("details.lang-menu");
    document.addEventListener("click", (event) => {
        details.forEach((d) => {
            if (d.open && !d.contains(event.target)) d.removeAttribute("open");
        });
    });
})();
