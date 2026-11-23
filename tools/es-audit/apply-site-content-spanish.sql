-- AddSiteContentSpanish — applied manually (docker migrate may lack table ownership)
ALTER TABLE barang.site_settings ADD COLUMN IF NOT EXISTS site_name_es varchar(120) NOT NULL DEFAULT '';
ALTER TABLE barang.site_settings ADD COLUMN IF NOT EXISTS slogan_es varchar(300);
ALTER TABLE barang.site_pages ADD COLUMN IF NOT EXISTS title_es varchar(200) NOT NULL DEFAULT '';
ALTER TABLE barang.site_pages ADD COLUMN IF NOT EXISTS body_es text NOT NULL DEFAULT '';
ALTER TABLE barang.site_pages ADD COLUMN IF NOT EXISTS seo_description_es varchar(600);
ALTER TABLE barang.site_pages ADD COLUMN IF NOT EXISTS seo_keywords_es varchar(400);
ALTER TABLE barang.site_main_menu_items ADD COLUMN IF NOT EXISTS title_es varchar(120);
ALTER TABLE barang.site_footer_widgets ADD COLUMN IF NOT EXISTS title_es varchar(200);
ALTER TABLE barang.site_blog_posts ADD COLUMN IF NOT EXISTS title_es varchar(250) NOT NULL DEFAULT '';
ALTER TABLE barang.site_blog_posts ADD COLUMN IF NOT EXISTS excerpt_es varchar(600) NOT NULL DEFAULT '';
ALTER TABLE barang.site_blog_posts ADD COLUMN IF NOT EXISTS body_es text NOT NULL DEFAULT '';
ALTER TABLE barang.site_blog_posts ADD COLUMN IF NOT EXISTS seo_description_es varchar(600);
ALTER TABLE barang.site_blog_posts ADD COLUMN IF NOT EXISTS seo_keywords_es varchar(400);

INSERT INTO barang.__ef_migrations_history ("MigrationId", "ProductVersion")
SELECT '20261123121436_AddSiteContentSpanish', '10.0.11'
WHERE NOT EXISTS (SELECT 1 FROM barang.__ef_migrations_history WHERE "MigrationId" = '20261123121436_AddSiteContentSpanish');

-- ── seed es for existing rows (idempotent) ──
UPDATE barang.site_settings SET
  site_name_es = CASE WHEN site_name_es = '' THEN 'Peyrang' ELSE site_name_es END,
  slogan_es = COALESCE(slogan_es, '¡Peyrang Khan tiene los diseños para ti!');

UPDATE barang.site_main_menu_items SET title_es = 'Inicio' WHERE url = '/' AND title_es IS NULL;
UPDATE barang.site_main_menu_items SET title_es = 'Empezar a diseñar' WHERE url = '/order/wizard' AND title_es IS NULL;
UPDATE barang.site_main_menu_items SET title_es = 'Diseños' WHERE url = '/catalog' AND item_type = 'custom' AND title_es IS NULL;
UPDATE barang.site_main_menu_items SET title_es = 'Blog' WHERE item_type = 'blog' AND title_es IS NULL;

UPDATE barang.site_pages SET
  title_es = 'Acerca de Peyrang',
  body_es = '<h2>Acerca de Peyrang Khan</h2><p>Peyrang Khan es el hogar de miles de diseños listos para imprimir en prendas y regalos promocionales. Desde nuestro rico banco de diseños hasta tu propia ilustración personalizada, todo se imprime en tu camiseta, polo o sudadera en pocos clics.</p><p>Desde elegir el diseño hasta la entrega, todo es en línea y totalmente rastreable.</p>'
WHERE slug = 'about' AND title_es = '';
UPDATE barang.site_pages SET
  title_es = 'Contacto',
  body_es = '<h2>Contacto con Peyrang Khan</h2><p>¿Preguntas o sugerencias? Nos encantaría escucharte:</p><ul><li>Correo: salam@pirang.ir</li><li>Teléfono: 021-00000000</li></ul><p>Respondemos en días laborables, de 9:00 a 18:00.</p>'
WHERE slug = 'contact' AND title_es = '';
UPDATE barang.site_pages SET
  title_es = 'Impresión y embalaje',
  body_es = '<h2>Impresión y embalaje de Peyrang Khan</h2><p>Impresión de máxima calidad en prendas en tres tamaños estándar — A3, A4 y A5 — con opciones de frente y espalda.</p><h3>Embalaje</h3><ul><li>Embalaje estándar — para uso diario</li><li>Envoltorio de regalo — con tarjeta de mensaje personal</li><li>Caja de lujo — para regalos corporativos y especiales</li></ul><p>Tiempo de entrega: 3–5 días laborables.</p>'
WHERE slug = 'printing-packaging' AND title_es = '';

UPDATE barang.site_footer_widgets SET title_es = 'Todos los diseños' WHERE url = '/catalog' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Seguir pedido' WHERE url = '/account/orders' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Preguntas frecuentes' WHERE url = '/faq' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Mi cuenta' WHERE url = '/account' AND widget_type = 'menu' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Enlace 1' WHERE widget_type = 'menu' AND url = '/' AND column_index = 3 AND title_en = 'Link 1' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Enlace 2' WHERE widget_type = 'menu' AND url = '/' AND column_index = 3 AND title_en = 'Link 2' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Enlace 3' WHERE widget_type = 'menu' AND url = '/' AND column_index = 3 AND title_en = 'Link 3' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Enlace 4' WHERE widget_type = 'menu' AND url = '/' AND column_index = 3 AND title_en = 'Link 4' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Enlace 5' WHERE widget_type = 'menu' AND url = '/' AND column_index = 3 AND title_en = 'Link 5' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Licencia de muestra 1' WHERE title_en = 'Sample license 1' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Licencia de muestra 2' WHERE title_en = 'Sample license 2' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Licencia de muestra 3' WHERE title_en = 'Sample license 3' AND title_es IS NULL;
UPDATE barang.site_footer_widgets SET title_es = 'Licencia de muestra 4' WHERE title_en = 'Sample license 4' AND title_es IS NULL;

UPDATE barang.site_blog_posts SET
  title_es = 'El primer artículo del blog de Peyrang',
  excerpt_es = 'Este es el resumen del primer artículo de muestra, sembrado para previsualizar la tienda.',
  body_es = '<p>Este es el cuerpo del primer artículo de muestra del blog de la tienda Peyrang. Aquí se mostrarán las publicaciones reales de la empresa: novedades de diseños, guías de impresión e ideas para regalos personalizados.</p>'
WHERE slug = 'hello-pirang' AND title_es = '';
UPDATE barang.site_blog_posts SET
  title_es = 'Guía para elegir el tamaño de impresión',
  excerpt_es = '¿A3, A4 o A5? Una guía breve para elegir el tamaño de impresión adecuado para tu diseño.',
  body_es = '<p>¿A3, A4 o A5? Elegir el tamaño de impresión correcto depende del diseño y de la prenda: A3 ofrece el máximo impacto para ilustraciones grandes, A4 es el equilibrio perfecto para la mayoría de los diseños y A5 funciona muy bien para logos pequeños y discretos.</p>'
WHERE slug = 'print-guide' AND title_es = '';
