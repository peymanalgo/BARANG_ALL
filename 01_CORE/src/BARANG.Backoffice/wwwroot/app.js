document.addEventListener('click',event=>{const button=event.target.closest('.password-toggle');if(!button)return;const input=button.parentElement.querySelector('input');const showing=input.type==='text';input.type=showing?'password':'text';button.setAttribute('aria-label',showing?button.dataset.show:button.dataset.hide);});
document.addEventListener('submit',event=>{const form=event.target.closest('.submitting-form');if(!form||form.classList.contains('submitting')){if(form)event.preventDefault();return}form.classList.add('submitting');form.querySelectorAll('button').forEach(button=>button.disabled=true)});
window.barangFocusCalendarDay = date => requestAnimationFrame(() => document.querySelector(`[data-calendar-date="${date}"]`)?.focus());
window.barangGetDocumentCulture = () => document.documentElement.lang || 'fa';

window.barangPopupManager = (() => {
    let active = null;
    let nextId = 1;
    const entries = new Map();

    const isInside = (entry, event) => {
        const path = typeof event.composedPath === 'function' ? event.composedPath() : [];
        return path.includes(entry.trigger) || path.includes(entry.popup)
            || entry.trigger?.contains?.(event.target) || entry.popup?.contains?.(event.target);
    };

    const closeEntry = (entry, restoreFocus = true) => {
        if (!entry) return;
        if (active === entry) active = null;
        const closeNotification = entry.trigger?.isConnected
            ? entry.dotnet?.invokeMethodAsync('CloseFromPopupManager')
            : Promise.resolve();
        Promise.resolve(closeNotification).catch(() => { }).finally(() => {
            if (restoreFocus && entry.trigger?.isConnected) requestAnimationFrame(() => entry.trigger.focus());
        });
    };

    const removeDetached = () => {
        for (const [id, entry] of entries) {
            if (entry.trigger?.isConnected && entry.popup?.isConnected) continue;
            if (active === entry) active = null;
            entries.delete(id);
        }
    };

    document.addEventListener('pointerdown', event => {
        if (active && !isInside(active, event)) closeEntry(active, true);
    }, true);

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || !active) return;
        event.preventDefault();
        event.stopPropagation();
        closeEntry(active, true);
    }, true);

    return {
        register(trigger, popup, dotnet) {
            removeDetached();
            let entry = [...entries.values()].find(item => item.dotnet === dotnet);
            if (!entry) {
                entry = { id: `popup-${nextId++}`, trigger, popup, dotnet };
                entries.set(entry.id, entry);
            } else {
                entry.trigger = trigger;
                entry.popup = popup;
            }
            return entry.id;
        },
        open(id) {
            removeDetached();
            const entry = entries.get(id);
            if (!entry) return;
            if (active && active !== entry) closeEntry(active, true);
            active = entry;
        },
        close(id, restoreFocus = true) {
            const entry = entries.get(id);
            if (entry && active === entry) closeEntry(entry, restoreFocus);
        },
        unregister(id) {
            const entry = entries.get(id);
            if (active === entry) active = null;
            entries.delete(id);
        }
    };
})();

window.barangActionMenu = {
    watchers: new Map(),
    focusFirst(popup) { requestAnimationFrame(() => popup?.querySelector('button:not([disabled]),a[href],input:not([disabled])')?.focus()); },
    position(trigger, popup) {
        if (!trigger || !popup || !trigger.isConnected || !popup.isConnected) return false;
        const gap = 6, margin = 8, rect = trigger.getBoundingClientRect();
        const width = popup.offsetWidth || 180, height = popup.offsetHeight || 120;
        const rtl = document.documentElement.dir === 'rtl';
        let left = rtl ? rect.right - width : rect.left;
        let top = rect.bottom + gap;
        if (top + height > window.innerHeight - margin && rect.top - gap - height >= margin) top = rect.top - gap - height;
        left = Math.max(margin, Math.min(left, window.innerWidth - width - margin));
        popup.style.position = 'fixed';
        for (const property of ['inset', 'inset-inline-start', 'inset-inline-end', 'inset-block-start', 'inset-block-end']) popup.style.removeProperty(property);
        popup.style.setProperty('left', `${Math.round(left)}px`, 'important');
        popup.style.setProperty('top', `${Math.round(top)}px`, 'important');
        popup.style.setProperty('right', 'auto', 'important');
        popup.style.setProperty('bottom', 'auto', 'important');
        return true;
    },
    watch(id, trigger, popup) {
        this.unwatch(id);
        const placeholder = document.createComment(`barang-action-menu-${id}`);
        const originalParent = popup?.parentNode;
        if (popup && originalParent && popup.parentNode !== document.body) {
            originalParent.insertBefore(placeholder, popup);
            document.body.appendChild(popup);
        }
        const update = () => this.position(trigger, popup);
        update();
        const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(update) : null;
        resizeObserver?.observe(trigger);
        resizeObserver?.observe(popup);
        window.addEventListener('resize', update, { passive: true });
        window.addEventListener('scroll', update, { passive: true, capture: true });
        this.watchers.set(id, { update, resizeObserver, popup, placeholder });
    },
    unwatch(id) {
        const watcher = this.watchers.get(id);
        if (!watcher) return;
        window.removeEventListener('resize', watcher.update);
        window.removeEventListener('scroll', watcher.update, true);
        watcher.resizeObserver?.disconnect();
        if (watcher.popup?.isConnected && watcher.placeholder?.isConnected) {
            watcher.placeholder.parentNode.insertBefore(watcher.popup, watcher.placeholder.nextSibling);
            watcher.placeholder.remove();
        }
        this.watchers.delete(id);
    }
};

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
