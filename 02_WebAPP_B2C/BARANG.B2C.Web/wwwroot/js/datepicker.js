/* barang datepicker — mirrors the Core LocaleDateService calendar policy:
   Jalali (PersianCalendar) for fa, Gregorian otherwise. Month and year are
   selectable dropdowns; the wire value is always canonical yyyy-MM-dd. */
(function () {
    'use strict';
    var JALALI_MONTHS = ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
    var GREG_MONTHS_FA = ['ژانویه', 'فوریه', 'مارس', 'آپریل', 'مه', 'ژوئن', 'ژوئیه', 'اوت', 'سپتامبر', 'اکتبر', 'نوامبر', 'دسامبر'];
    var GREG_MONTHS_EN = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    var GREG_MONTHS_AR = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
    var JALALI_DOW = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
    var GREG_DOW = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

    function toJalali(d) {
        var f = new Intl.DateTimeFormat('en-u-ca-persian', { year: 'numeric', month: 'numeric', day: 'numeric', timeZone: 'UTC' });
        var parts = {};
        f.formatToParts(new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()))).forEach(function (p) { parts[p.type] = p.value; });
        return { y: parseInt(parts.year, 10), m: parseInt(parts.month, 10), d: parseInt(parts.day, 10) };
    }
    function fromJalali(y, m, day) {
        // Gregorian year ≈ Jalali year + 621; scan the whole overlap window.
        var guess = Date.UTC(y + 621, 0, 1);
        var best = null;
        for (var offset = -30; offset <= 400; offset += 1) {
            var probe = new Date(guess + offset * 86400000);
            var j = toJalali(probe);
            if (j.y === y && j.m === m && j.d === day) { best = probe; break; }
        }
        return best;
    }
    function jalaliMonthLength(y, m) {
        var start = fromJalali(y, m, 1);
        var next = m === 12 ? fromJalali(y + 1, 1, 1) : fromJalali(y, m + 1, 1);
        if (!start || !next) return 30;
        return Math.round((next - start) / 86400000);
    }
    function pad(n) { return (n < 10 ? '0' : '') + n; }
    function digits(value, culture) {
        if (culture !== 'fa') return String(value);
        return String(value).replace(/[0-9]/g, function (c) { return '۰۱۲۳۴۵۶۷۸۹'[c]; });
    }

    function Calendar(input) {
        this.input = input;
        this.culture = input.getAttribute('data-culture') || 'fa';
        this.jalali = this.culture === 'fa';
        this.popup = null;
        this.target = input.getAttribute('data-target') ? document.querySelector(input.getAttribute('data-target')) : null;
        // the wire value is always canonical Gregorian yyyy-MM-dd
        var initial = (this.target ? this.target.value : input.value).trim();
        var parsed = initial ? initial.split('-').map(Number) : null;
        var greg = parsed && parsed.length === 3 && parsed.every(function (n) { return !isNaN(n); }) ? parsed : null;
        if (greg) {
            this.selected = this.jalali ? (function () { var j = toJalali(new Date(Date.UTC(greg[0], greg[1] - 1, greg[2]))); return [j.y, j.m, j.d]; })() : greg;
            this.display(greg);
        } else { this.selected = null; }
        var viewBase = this.selected;
        if (!viewBase) {
            var todayJ = toJalali(new Date());
            viewBase = this.jalali ? [todayJ.y, todayJ.m, todayJ.d] : [new Date().getFullYear(), new Date().getMonth() + 1, new Date().getDate()];
        }
        this.view = { y: viewBase[0], m: viewBase[1] };
        this.today = new Date();
        var self = this;
        input.addEventListener('focus', function () { self.open(); });
        input.addEventListener('click', function () { self.open(); });
        document.addEventListener('click', function (e) {
            if (self.popup && !self.popup.contains(e.target) && e.target !== input) self.close();
        });
    }

    Calendar.prototype.open = function () {
        if (this.popup) return;
        var self = this;
        var pop = document.createElement('div');
        pop.className = 'barang-dp-popup';
        pop.innerHTML =
            '<div class="barang-dp-head">' +
            '<select class="dp-month" aria-label="month"></select>' +
            '<select class="dp-year" aria-label="year"></select>' +
            '</div>' +
            '<div class="barang-dp-grid"></div>' +
            '<div class="barang-dp-actions">' +
            '<button type="button" class="dp-clear">' + (this.jalali ? 'پاک کردن' : (this.culture === 'ar' ? 'مسح' : 'Clear')) + '</button>' +
            '<button type="button" class="dp-today">' + (this.jalali ? 'امروز' : (this.culture === 'ar' ? 'اليوم' : 'Today')) + '</button>' +
            '</div>';
        this.input.parentElement.appendChild(pop);
        this.popup = pop;

        var monthSel = pop.querySelector('.dp-month');
        var yearSel = pop.querySelector('.dp-year');
        var months = this.jalali ? JALALI_MONTHS : (this.culture === 'ar' ? GREG_MONTHS_AR : (this.culture === 'en' ? GREG_MONTHS_EN : GREG_MONTHS_FA));
        months.forEach(function (name, i) {
            var o = document.createElement('option');
            o.value = i + 1;
            o.textContent = name + (self.jalali ? '' : ' ' + digits(i + 1, self.culture));
            monthSel.appendChild(o);
        });
        var years = this.jalali ? [1300, 1500] : [1925, 2100];
        for (var y = years[0]; y <= years[1]; y += 1) {
            var oy = document.createElement('option');
            oy.value = y;
            oy.textContent = digits(y, this.culture);
            yearSel.appendChild(oy);
        }
        monthSel.value = String(this.view.m);
        yearSel.value = String(this.view.y);
        monthSel.addEventListener('change', function () { self.view.m = parseInt(monthSel.value, 10); self.renderGrid(); });
        yearSel.addEventListener('change', function () { self.view.y = parseInt(yearSel.value, 10); self.renderGrid(); });
        pop.querySelector('.dp-clear').addEventListener('click', function () { self.setClear(); });
        pop.querySelector('.dp-today').addEventListener('click', function () {
            var todayJ = toJalali(self.today);
            self.set(self.jalali ? [todayJ.y, todayJ.m, todayJ.d] : [self.today.getFullYear(), self.today.getMonth() + 1, self.today.getDate()]);
        });
        this.renderGrid();
    };

    Calendar.prototype.close = function () { if (this.popup) { this.popup.remove(); this.popup = null; } };

    Calendar.prototype.renderGrid = function () {
        var self = this;
        var pop = this.popup;
        pop.querySelector('.dp-month').value = String(this.view.m);
        pop.querySelector('.dp-year').value = String(this.view.y);
        var grid = pop.querySelector('.barang-dp-grid');
        grid.innerHTML = '';
        var dows = this.jalali ? JALALI_DOW : GREG_DOW;
        dows.forEach(function (name) {
            var d = document.createElement('span');
            d.className = 'barang-dp-dow';
            d.textContent = name;
            grid.appendChild(d);
        });
        var firstDow, length;
        if (this.jalali) {
            var first = fromJalali(this.view.y, this.view.m, 1);
            if (!first) return;
            firstDow = (first.getUTCDay() + 1) % 7;
            length = jalaliMonthLength(this.view.y, this.view.m);
        } else {
            var firstDate = new Date(Date.UTC(this.view.y, this.view.m - 1, 1));
            firstDow = firstDate.getUTCDay();
            length = new Date(Date.UTC(this.view.y, this.view.m, 0)).getUTCDate();
        }
        for (var i = 0; i < firstDow; i += 1) {
            var empty = document.createElement('button');
            empty.type = 'button';
            empty.className = 'barang-dp-day is-empty';
            grid.appendChild(empty);
        }
        var todayJ = toJalali(this.today);
        for (var day = 1; day <= length; day += 1) {
            (function (d) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'barang-dp-day';
                btn.textContent = digits(d, self.culture);
                var isSel = self.selected && self.selected[0] === self.view.y && self.selected[1] === self.view.m && self.selected[2] === d;
                var isToday = self.jalali
                    ? todayJ.y === self.view.y && todayJ.m === self.view.m && todayJ.d === d
                    : self.today.getFullYear() === self.view.y && self.today.getMonth() + 1 === self.view.m && self.today.getDate() === d;
                if (isSel) btn.classList.add('is-selected');
                if (isToday) btn.classList.add('is-today');
                btn.addEventListener('click', function () { self.set([self.view.y, self.view.m, d]); });
                grid.appendChild(btn);
            })(day);
        }
    };

    Calendar.prototype.toCanonical = function (parts) {
        if (!this.jalali) return parts[0] + '-' + pad(parts[1]) + '-' + pad(parts[2]);
        var g = fromJalali(parts[0], parts[1], parts[2]);
        if (!g) return '';
        return (g.getUTCFullYear()) + '-' + pad(g.getUTCMonth() + 1) + '-' + pad(g.getUTCDate());
    };

    Calendar.prototype.display = function (gregParts) {
        if (this.jalali) {
            var j = toJalali(new Date(Date.UTC(gregParts[0], gregParts[1] - 1, gregParts[2])));
            this.input.value = digits(j.y, this.culture) + '/' + digits(pad(j.m), this.culture) + '/' + digits(pad(j.d), this.culture);
        } else {
            this.input.value = gregParts[0] + '-' + pad(gregParts[1]) + '-' + pad(gregParts[2]);
        }
    };

    Calendar.prototype.set = function (parts) {
        this.selected = parts;
        this.view = { y: parts[0], m: parts[1] };
        var canonical = this.toCanonical(parts);
        this.display(this.jalali ? (function () { var g = fromJalali(parts[0], parts[1], parts[2]); return [g.getUTCFullYear(), g.getUTCMonth() + 1, g.getUTCDate()]; })() : parts);
        if (this.target) this.target.value = canonical;
        else this.input.value = canonical;
        this.input.dispatchEvent(new Event('change', { bubbles: true }));
        this.close();
    };

    function enhance(input) {
        if (input.dataset.dpReady) return;
        input.dataset.dpReady = '1';
        var wrap = document.createElement('span');
        wrap.className = 'barang-dp';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);
        new Calendar(input);
        input.setAttribute('readonly', 'readonly');
    }

    function scan(root) {
        (root || document).querySelectorAll('input[data-dp]').forEach(enhance);
    }

    document.addEventListener('DOMContentLoaded', function () { scan(document); });
    window.barangDatepicker = { scan: scan };
})();
