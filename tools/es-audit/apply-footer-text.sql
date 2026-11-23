-- AddSiteContentLocalizedFooterText — manual apply (role barang_app is not table owner)
ALTER TABLE barang.site_footer_widgets ADD COLUMN IF NOT EXISTS text_fa varchar(2000);
ALTER TABLE barang.site_footer_widgets ADD COLUMN IF NOT EXISTS text_en varchar(2000);
ALTER TABLE barang.site_footer_widgets ADD COLUMN IF NOT EXISTS text_ar varchar(2000);
ALTER TABLE barang.site_footer_widgets ADD COLUMN IF NOT EXISTS text_es varchar(2000);

INSERT INTO barang.__ef_migrations_history ("MigrationId", "ProductVersion")
SELECT '20261123123401_AddSiteContentLocalizedFooterText', '10.0.11'
WHERE NOT EXISTS (SELECT 1 FROM barang.__ef_migrations_history WHERE "MigrationId" = '20261123123401_AddSiteContentLocalizedFooterText');

-- ── backfill + seed per-language text for the column-1 text widget (idempotent) ──
UPDATE barang.site_footer_widgets SET text_fa = text WHERE widget_type = 'text' AND (text_fa IS NULL OR text_fa = '') AND text IS NOT NULL;
UPDATE barang.site_footer_widgets SET
  text_en = 'Ready-to-print designs on apparel and promotional gifts; order online, tracking to delivery.' || chr(10) || 'Peyrang Khan has the designs for you!',
  text_ar = 'تصاميم جاهزة للطباعة على الملابس والهدايا الدعائية؛ اطلب عبر الإنترنت وتابع حتى التسليم.' || chr(10) || 'پی‌رنگ خان لديه التصاميم من أجلك!',
  text_es = 'Diseños listos para imprimir en prendas y regalos publicitarios; pedido en línea, seguimiento hasta la entrega.' || chr(10) || 'پی‌رنگ خان ¡tiene los diseños para ti!'
WHERE widget_type = 'text' AND column_index = 1 AND (text_es IS NULL OR text_es = '');
