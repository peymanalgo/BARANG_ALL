document.addEventListener('click',event=>{const button=event.target.closest('.password-toggle');if(!button)return;const input=button.parentElement.querySelector('input');const showing=input.type==='text';input.type=showing?'password':'text';button.setAttribute('aria-label',showing?button.dataset.show:button.dataset.hide);});
document.addEventListener('submit',event=>{const form=event.target.closest('.submitting-form');if(!form||form.classList.contains('submitting')){if(form)event.preventDefault();return}form.classList.add('submitting');form.querySelectorAll('button').forEach(button=>button.disabled=true)});
window.barangFocusCalendarDay = date => requestAnimationFrame(() => document.querySelector(`[data-calendar-date="${date}"]`)?.focus());
window.barangGetDocumentCulture = () => document.documentElement.lang || 'fa';

let barangDialogTrigger = null;
const barangFocusable = 'button:not([disabled]),a[href],input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';
const barangDialogObserver = new MutationObserver(() => {
    const dialog = document.querySelector('.dialog-backdrop:last-of-type [role="dialog"],.dialog-backdrop:last-of-type [role="alertdialog"]');
    if (dialog && !dialog.dataset.focusReady) {
        barangDialogTrigger = document.activeElement;
        dialog.dataset.focusReady = 'true';
        requestAnimationFrame(() => dialog.querySelector(barangFocusable)?.focus());
    }
    if (!dialog && barangDialogTrigger instanceof HTMLElement) {
        barangDialogTrigger.focus();
        barangDialogTrigger = null;
    }
});
barangDialogObserver.observe(document.body, { childList: true, subtree: true });
document.addEventListener('keydown', event => {
    if (event.key !== 'Tab') return;
    const dialog = document.querySelector('.dialog-backdrop:last-of-type [role="dialog"],.dialog-backdrop:last-of-type [role="alertdialog"]');
    if (!dialog) return;
    const focusable = [...dialog.querySelectorAll(barangFocusable)].filter(element => element.getClientRects().length > 0);
    if (!focusable.length) return;
    const first = focusable[0], last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
});

window.barangSetCulture = async code => {
    if (!['fa', 'ar', 'en'].includes(code)) throw new Error('Unsupported UI language');
    const root = document.documentElement;
    const previous = { lang: root.lang, dir: root.dir };
    root.lang = code;
    root.dir = code === 'en' ? 'ltr' : 'rtl';
    try {
        const response = await fetch('/culture?culture=' + encodeURIComponent(code) + '&live=true', {
            credentials: 'same-origin', cache: 'no-store'
        });
        if (response.status !== 204) throw new Error('UI language persistence failed');
    } catch (error) {
        root.lang = previous.lang;
        root.dir = previous.dir;
        throw error;
    }
};

window.barangDownloadFile = (fileName, contentType, base64) => {
    const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: contentType || 'application/octet-stream' }));
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = fileName || 'download'; anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};

window.barangCartableShortcuts = {
    init(dotnet) {
        this.dispose();
        this.dotnet = dotnet;
        this.handler = event => {
            const target = event.target;
            const typing = target && (['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName) || target.isContentEditable || target.closest?.('button,a,[role="button"]'));
            if (event.key === 'Escape') { event.preventDefault(); dotnet.invokeMethodAsync('HandleShortcut', 'escape'); return; }
            if (typing) return;
            const shortcut = ({ '/': 'focus-search', r: 'reload', R: 'reload', p: 'preview', P: 'preview', Enter: 'view' })[event.key];
            if (!shortcut) return;
            event.preventDefault();
            dotnet.invokeMethodAsync('HandleShortcut', shortcut);
        };
        document.addEventListener('keydown', this.handler);
    },
    focusSearch() { document.querySelector('.cartable-v2-search input')?.focus(); },
    dispose() { if (this.handler) document.removeEventListener('keydown', this.handler); this.handler = null; this.dotnet = null; }
};
