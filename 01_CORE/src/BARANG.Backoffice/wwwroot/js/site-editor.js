/* ─── ویرایشگر متن حرفه‌ای CMS ب2B — تولبار کامل روی contenteditable ───
   امکانات: undo/redo، فونت، اندازه، B/I/U/S، زیر/بالانویس، رنگ متن و پس‌زمینه،
   چینش (چپ/وسط/راست/جاستیفای)، لیست ترتیبی/نقطه‌ای، تورفتگی، سرفصل‌ها،
   نقل‌قول، کد، خط افقی، لینک، تصویر (آپلود از Blazor)، جدول، پاک‌سازی فرمت، RTL/LTR */
(function () {
    'use strict';
    var api = {};
    var FONTS = ['Vazirmatn', 'Tahoma', 'Iranian Sans', 'Arial', 'Georgia', 'Times New Roman', 'Courier New', 'Verdana'];
    var SIZES = [
        ['1', 'کمترین'], ['2', 'کوچک'], ['3', 'معمولی'], ['4', 'متوسط'], ['5', 'درشت'], ['6', 'بزرگ'], ['7', 'بزرگ‌ترین']
    ];

    function btn(bar, label, title, fn, keepFocus) {
        var b = document.createElement('button');
        b.type = 'button'; b.className = 'site-ed-btn'; b.innerHTML = label; b.title = title;
        b.addEventListener('mousedown', function (e) { e.preventDefault(); });
        b.addEventListener('click', function (e) { e.preventDefault(); fn(); });
        bar.appendChild(b);
        return b;
    }
    function sep(bar) {
        var s = document.createElement('span');
        s.className = 'site-ed-sep';
        bar.appendChild(s);
    }
    function select(bar, options, title, onPick, current) {
        var w = document.createElement('label');
        w.className = 'site-ed-select';
        var sl = document.createElement('select');
        sl.title = title;
        var head = document.createElement('option');
        head.value = ''; head.textContent = title; sl.appendChild(head);
        options.forEach(function (o) {
            var op = document.createElement('option');
            op.value = o[0]; op.textContent = o[1];
            if (o[0] === current) op.selected = true;
            sl.appendChild(op);
        });
        sl.addEventListener('change', function () { if (sl.value) { onPick(sl.value); sl.value = ''; } });
        w.appendChild(sl);
        bar.appendChild(w);
    }
    function colorInput(bar, label, title, onPick) {
        var w = document.createElement('label');
        w.className = 'site-ed-color'; w.title = title;
        w.innerHTML = label;
        var c = document.createElement('input');
        c.type = 'color'; c.value = '#1c1917';
        c.addEventListener('input', function () { onPick(c.value); });
        w.appendChild(c);
        bar.appendChild(w);
    }

    api.scan = function (root) {
        (root || document).querySelectorAll('[data-site-editor]').forEach(function (el) {
            if (el.dataset.ready) return;
            el.dataset.ready = '1';
            el.classList.add('site-ed');
            var bar = document.createElement('div');
            bar.className = 'site-ed-bar';

            function run(cmd, val) {
                el.focus();
                document.execCommand(cmd, false, val === undefined ? null : val);
                api.sync(el.id);
            }
            function block(tag) { run('formatBlock', tag); }
            function table() {
                var rc = prompt((document.documentElement.dir === 'rtl' ? 'سطرها,ستون‌ها (مثال 3,4):' : 'rows,cols (e.g. 3,4):'), '3,4');
                if (!rc) return;
                var parts = rc.split(',').map(function (x) { return parseInt(x.trim(), 10); });
                var rows = Math.min(Math.max(parts[0] || 2, 1), 30), cols = Math.min(Math.max(parts[1] || 2, 1), 10);
                var html = '<table class="site-ed-table"><tbody>';
                for (var r = 0; r < rows; r++) {
                    html += '<tr>';
                    for (var c = 0; c < cols; c++) html += '<td>' + (r === 0 ? '<b>' + (c + 1) + '</b>' : '&nbsp;') + '</td>';
                    html += '</tr>';
                }
                html += '</tbody></table><p><br></p>';
                el.focus();
                document.execCommand('insertHTML', false, html);
                api.sync(el.id);
            }

            // ── ردیف ۱: ابزارهای پایه ──
            btn(bar, '↶', 'واگرد (Ctrl+Z)', function () { run('undo'); });
            btn(bar, '↷', 'ازنو (Ctrl+Y)', function () { run('redo'); });
            sep(bar);
            btn(bar, '<b>B</b>', 'بولد (Ctrl+B)', function () { run('bold'); });
            btn(bar, '<i>I</i>', 'ایتالیک (Ctrl+I)', function () { run('italic'); });
            btn(bar, '<u>U</u>', 'زیرخط (Ctrl+U)', function () { run('underline'); });
            btn(bar, '<s>S</s>', 'خط‌خورده', function () { run('strikeThrough'); });
            btn(bar, 'x²', 'بالانویس', function () { run('superscript'); });
            btn(bar, 'x₂', 'زیرنویس', function () { run('subscript'); });
            sep(bar);
            select(bar, FONTS.map(function (f) { return [f, f]; }), 'فونت', function (v) { run('fontName', v); });
            select(bar, SIZES, 'اندازه', function (v) { run('fontSize', v); });
            colorInput(bar, '🎨', 'رنگ متن', function (v) { run('foreColor', v); });
            colorInput(bar, '🖌', 'رنگ پس‌زمینه', function (v) { run('hiliteColor', v); });
            sep(bar);
            btn(bar, '🗑⃰', 'پاک‌سازی فرمت', function () { run('removeFormat'); });

            // ── ردیف ۲: چینش و ساختار ──
            bar.appendChild(document.createElement('br'));
            btn(bar, '⬅', 'چپ‌چین', function () { run('justifyLeft'); });
            btn(bar, '↔', 'وسط‌چین', function () { run('justifyCenter'); });
            btn(bar, '➡', 'راست‌چین', function () { run('justifyRight'); });
            btn(bar, '☰', 'تراز دوطرفه', function () { run('justifyFull'); });
            sep(bar);
            btn(bar, '• ≡', 'لیست نقطه‌ای', function () { run('insertUnorderedList'); });
            btn(bar, '1. ≡', 'لیست ترتیبی', function () { run('insertOrderedList'); });
            btn(bar, '⇤', 'کاهش تورفتگی', function () { run('outdent'); });
            btn(bar, '⇥', 'افزایش تورفتگی', function () { run('indent'); });
            sep(bar);
            select(bar, [['p', '¶ متن'], ['h1', 'سرصفحه 1'], ['h2', 'سرصفحه 2'], ['h3', 'سرصفحه 3'], ['h4', 'سرصفحه 4'], ['blockquote', '❝ نقل‌قول'], ['pre', '⌨ کد']], 'قالب', function (v) { block('<' + v + '>'); });
            sep(bar);
            btn(bar, '🔗', 'درج لینک', function () {
                var u = prompt('URL:', 'https://');
                if (u) run('createLink', u);
            });
            btn(bar, '⛓✂', 'حذف لینک', function () { run('unlink'); });
            btn(bar, '―', 'خط افقی', function () { run('insertHorizontalRule'); });
            btn(bar, '▦', 'درج جدول', table);
            sep(bar);
            btn(bar, 'RTL', 'راست به چپ', function () { run('formatBlock', 'div'); var n = window.getSelection().anchorNode; var d = n && (n.nodeName === 'DIV' ? n : n.parentElement && n.parentElement.closest('div')); if (d) { d.setAttribute('dir', 'rtl'); d.setAttribute('style', 'text-align:right'); api.sync(el.id); } });
            btn(bar, 'LTR', 'چپ به راست', function () { run('formatBlock', 'div'); var n = window.getSelection().anchorNode; var d = n && (n.nodeName === 'DIV' ? n : n.parentElement && n.parentElement.closest('div')); if (d) { d.setAttribute('dir', 'ltr'); d.setAttribute('style', 'text-align:left'); api.sync(el.id); } });

            el.parentNode.insertBefore(bar, el);
            el.addEventListener('input', function () { api.sync(el.id); });
            if (!el.innerHTML || el.innerHTML === '<br>') el.innerHTML = '<p><br></p>';
        });
    };

    api.sync = function (id) {
        var el = document.getElementById(id);
        if (!el) return;
        var hidden = el.dataset.target ? document.getElementById(el.dataset.target) : null;
        if (hidden) hidden.value = el.innerHTML;
    };
    api.getHtml = function (id) { var el = document.getElementById(id); return el ? el.innerHTML : ''; };
    api.setHtml = function (id, html) {
        var el = document.getElementById(id);
        if (!el) return;
        el.innerHTML = html || '<p><br></p>';
        api.sync(id);
    };
    api.insertImage = function (id, url) {
        var el = document.getElementById(id);
        if (!el) return;
        el.focus();
        document.execCommand('insertImage', false, url);
        api.sync(id);
    };
    api.focus = function (id) { var el = document.getElementById(id); if (el) el.focus(); };

    window.barangSiteEditor = api;
    document.addEventListener('DOMContentLoaded', function () { api.scan(document); });
})();
