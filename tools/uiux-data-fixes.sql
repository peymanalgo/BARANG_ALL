-- ============================================================
-- اصلاح داده‌ای ممیزی UI/UX پی‌رنگ (۱۴۲۵/۰۹/۰۵)
-- 1) بازنامی دسته «خطای ۴۰۴» (کد ERRO) به «متفرقه» ×۴ زبان
-- 2) فوتر CMS: حذف «لینک ۱-۵»، نمادهای «مجوز نمونه»، تلفن صفر repeate؛ لینک‌های واقعی
-- 3) شعار غیرحرفه‌ای «نقشه‌ها داره برات!!!» → متن رسمی (فوتر + تنظیمات)
-- 4) شهر «TEHRAN» → «تهران» در نشانی‌های مشتری
-- اجرا: docker exec -i barang-core-postgres-1 psql -U barang -d barang_core < tools/uiux-data-fixes.sql
-- ترتیب روی VPS: اول این اسکریپت (دیتابیس)، بعد آپدیت کد — ترتیب حیاتی نیست ولی توصیه می‌شود اول دیتابیس.
-- ============================================================
SET search_path TO barang;

-- ── 1) دسته ERRO → متفرقه ──
UPDATE lookup_value_translations t
SET display_name = CASE t.culture
      WHEN 'fa' THEN 'متفرقه'
      WHEN 'ar' THEN 'متفرقات'
      WHEN 'es' THEN 'Varios'
      ELSE 'Miscellaneous' END,
    updated_at_utc = NOW(), version = t.version + 1
FROM lookup_values lv
WHERE lv.id = t.lookup_value_id AND lv.code = 'ERRO';

-- ── 2) فوتر CMS ──
-- ستون ۳: «لینک ۱ تا ۵» → لینک‌های واقعی سایت (title فارسی + معادل‌های دیگر زبان‌ها)
UPDATE site_footer_widgets SET
    title_fa='درباره پی‌رنگ', title_en='About Peyrang', title_ar='عن پی‌رنگ', title_es='Acerca de Peyrang',
    url='/about', updated_at_utc=NOW(), version=version+1
WHERE column_index=3 AND widget_type='menu' AND title_fa='لینک 1';
UPDATE site_footer_widgets SET
    title_fa='تماس با ما', title_en='Contact us', title_ar='اتصل بنا', title_es='Contacto',
    url='/contact', updated_at_utc=NOW(), version=version+1
WHERE column_index=3 AND widget_type='menu' AND title_fa='لینک 2';
UPDATE site_footer_widgets SET
    title_fa='وبلاگ', title_en='Blog', title_ar='المدونة', title_es='Blog',
    url='/blog', updated_at_utc=NOW(), version=version+1
WHERE column_index=3 AND widget_type='menu' AND title_fa='لینک 3';
UPDATE site_footer_widgets SET
    title_fa='سوالات متداول', title_en='FAQ', title_ar='الأسئلة الشائعة', title_es='Preguntas frecuentes',
    url='/faq', updated_at_utc=NOW(), version=version+1
WHERE column_index=3 AND widget_type='menu' AND title_fa='لینک 4';
UPDATE site_footer_widgets SET
    title_fa='سفارش چاپ', title_en='Order printing', title_ar='طلب طباعة', title_es='Pedir impresión',
    url='/order/wizard', updated_at_utc=NOW(), version=version+1
WHERE column_index=3 AND widget_type='menu' AND title_fa='لینک 5';

-- ستون ۲: ردیف تلفن صفر → ایمیل پشتیبانی واقعی (تلفن واقعی بعداً از CMS ERP قابل ویرایش است)
UPDATE site_footer_widgets SET
    title_fa='salam@pirang.ir', title_en='salam@pirang.ir', title_ar='salam@pirang.ir', title_es='salam@pirang.ir',
    url='mailto:salam@pirang.ir', updated_at_utc=NOW(), version=version+1
WHERE column_index=2 AND url='tel:02100000000';

-- ستون ۴: نمادهای «مجوز نمونه ۱-۴» + ردیف متن خالی حذف می‌شوند (نماد واقعی اینماد/ساماندهی بعد از ثبت، از CMS اضافه می‌شود)
DELETE FROM site_footer_widgets WHERE column_index=4 AND (widget_type='image' OR (widget_type='text' AND COALESCE(text_fa,'')='' AND COALESCE(text_en,'')=''));

-- ستون ۱: شعار غیرحرفه‌ای → متن رسمی (فارسی و معادل‌ها)
UPDATE site_footer_widgets SET
    text_fa = REPLACE(text_fa, 'پی‌رنگ خان، نقشه‌ها داره برات!!!', 'پی‌رنگ؛ طرح دلخواهت را انتخاب کن، ما با چند کلیک چاپش می‌کنیم.'),
    text_en = REPLACE(text_en, 'Peyrang Khan has the designs for you!!!', 'Peyrang: pick any design and we print it in a few clicks.'),
    text_ar = REPLACE(text_ar, 'پی‌رنگ خان لديه التصاميم من أجلك!!!', 'بي‌رنگ: اختر أي تصميم ونطبعه لك بضغوطات.'),
    text_es = REPLACE(text_es, '¡¡¡Peyrang Khan tiene los diseños para ti!', 'Peyrang: elige cualquier diseño y lo imprimimos en unos clics.'),
    updated_at_utc=NOW(), version=version+1
WHERE column_index=1 AND widget_type='text';

-- ── 3) شعار در تنظیمات سایت (بنر ماسکات صفحه اول) ──
UPDATE site_settings SET
    slogan_fa = 'طرح دلخواهت را انتخاب کن، ما با چند کلیک چاپش می‌کنیم.',
    slogan_en = COALESCE(NULLIF(slogan_en,''), 'Pick any design and we print it in a few clicks.'),
    slogan_ar = COALESCE(NULLIF(slogan_ar,''), 'اختر أي تصميم ونطبعه لك بضغوطات.'),
    slogan_es = COALESCE(NULLIF(slogan_es,''), 'Elige cualquier diseño y lo imprimimos en unos clics.'),
    updated_at_utc = NOW(), version = version + 1
WHERE slogan_fa LIKE '%نقشه‌ها داره برت%' OR slogan_fa LIKE '%نقشه‌ها داره برات%';

-- ── 4) شهر TEHRAN → تهران ──
UPDATE customer_addresses SET district='تهران', updated_at_utc=NOW(), version=customer_addresses.version+1
WHERE UPPER(COALESCE(district,''))='TEHRAN';
