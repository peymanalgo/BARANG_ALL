select "MigrationId" from barang.__ef_migrations_history order by 1 desc limit 2;
select column_index, widget_type, left(text_fa, 30) as fa, left(text_en, 30) as en, left(text_es, 30) as es from barang.site_footer_widgets order by column_index, display_order;
