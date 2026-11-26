namespace BARANG.Core.Infrastructure.Persistence;

internal static class SupplierSourcingMigrationSql
{
    internal const string AfterSchema="""
CREATE EXTENSION IF NOT EXISTS btree_gist;
ALTER TABLE barang.supplier_product_prices ADD CONSTRAINT ex_supplier_prices_active_period
 EXCLUDE USING gist(company_id WITH =,supplier_product_id WITH =,purchase_unit_id WITH =,currency_code WITH =,
 minimum_quantity WITH =,daterange(effective_from,COALESCE(effective_to,'infinity'::date),'[]') WITH &&)
 WHERE(is_active AND published_at_utc IS NOT NULL);

CREATE TRIGGER supplier_products_sourcing_references BEFORE INSERT OR UPDATE ON barang.supplier_products
 FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('purchase_unit_id','unit.of.measure','packaging_type_id','packaging.type');
CREATE TRIGGER supplier_product_prices_references BEFORE INSERT OR UPDATE ON barang.supplier_product_prices
 FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('purchase_unit_id','unit.of.measure','tax_status_id','tax.status');
CREATE TRIGGER supplier_price_list_lines_references BEFORE INSERT OR UPDATE ON barang.supplier_price_list_lines
 FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('purchase_unit_id','unit.of.measure','tax_status_id','tax.status');

CREATE FUNCTION barang.supplier_sourcing_history_guard() RETURNS trigger
LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
DECLARE header_published timestamptz; published_list uuid;
BEGIN
 IF TG_OP='UPDATE' THEN
  IF NEW.id IS DISTINCT FROM OLD.id OR NEW.company_id IS DISTINCT FROM OLD.company_id OR NEW.supplier_id IS DISTINCT FROM OLD.supplier_id THEN
   RAISE EXCEPTION 'Sourcing identity is immutable' USING ERRCODE='23514';
  END IF;
  IF TG_TABLE_NAME='supplier_products' THEN
   IF NEW.product_id IS DISTINCT FROM OLD.product_id OR NEW.product_variant_id IS DISTINCT FROM OLD.product_variant_id THEN
    RAISE EXCEPTION 'Sourcing item identity is immutable' USING ERRCODE='23514';
   END IF;
  ELSIF TG_TABLE_NAME='supplier_product_prices' THEN
   IF NEW.supplier_product_id IS DISTINCT FROM OLD.supplier_product_id OR NEW.price_list_id IS DISTINCT FROM OLD.price_list_id THEN
    RAISE EXCEPTION 'Price item identity is immutable' USING ERRCODE='23514';
   END IF;
   IF OLD.published_at_utc IS NOT NULL THEN
    IF (to_jsonb(NEW)-ARRAY['is_active','effective_to','updated_at_utc','version']) IS DISTINCT FROM
       (to_jsonb(OLD)-ARRAY['is_active','effective_to','updated_at_utc','version']) THEN
     RAISE EXCEPTION 'Published purchase price is immutable' USING ERRCODE='23514';
    END IF;
    IF NEW.effective_to IS DISTINCT FROM OLD.effective_to AND
       (NEW.effective_to IS NULL OR NEW.effective_to<CURRENT_DATE OR NEW.effective_to<NEW.effective_from OR
        (OLD.effective_to IS NOT NULL AND NEW.effective_to>OLD.effective_to)) THEN
     RAISE EXCEPTION 'Historical price periods cannot be rewritten' USING ERRCODE='23514';
    END IF;
   END IF;
  ELSIF TG_TABLE_NAME='supplier_price_lists' THEN
   IF NEW.code IS DISTINCT FROM OLD.code OR (OLD.published_at_utc IS NOT NULL AND
      (to_jsonb(NEW)-ARRAY['is_active','updated_at_utc','version']) IS DISTINCT FROM
      (to_jsonb(OLD)-ARRAY['is_active','updated_at_utc','version'])) THEN
    RAISE EXCEPTION 'Published price list is immutable' USING ERRCODE='23514';
   END IF;
  ELSIF TG_TABLE_NAME='supplier_price_list_lines' THEN
   IF NEW.price_list_id IS DISTINCT FROM OLD.price_list_id OR NEW.supplier_product_id IS DISTINCT FROM OLD.supplier_product_id THEN
    RAISE EXCEPTION 'Price list line identity is immutable' USING ERRCODE='23514';
   END IF;
   SELECT published_at_utc INTO header_published FROM barang.supplier_price_lists WHERE id=NEW.price_list_id;
   IF (header_published IS NOT NULL OR OLD.published_price_id IS NOT NULL) AND
      (to_jsonb(NEW)-ARRAY['published_price_id','updated_at_utc','version']) IS DISTINCT FROM
      (to_jsonb(OLD)-ARRAY['published_price_id','updated_at_utc','version']) THEN
    RAISE EXCEPTION 'Published price list line is immutable' USING ERRCODE='23514';
   END IF;
   IF OLD.published_price_id IS NOT NULL AND NEW.published_price_id IS DISTINCT FROM OLD.published_price_id THEN
    RAISE EXCEPTION 'Published price link is immutable' USING ERRCODE='23514';
   END IF;
  END IF;
 ELSIF TG_TABLE_NAME='supplier_price_list_lines' THEN
  SELECT published_at_utc INTO header_published FROM barang.supplier_price_lists WHERE id=NEW.price_list_id;
  IF header_published IS NOT NULL THEN RAISE EXCEPTION 'Published lists cannot receive new lines' USING ERRCODE='23514';END IF;
 END IF;
 IF TG_TABLE_NAME='supplier_price_list_lines' THEN
 IF NEW.published_price_id IS NOT NULL THEN
  SELECT price_list_id INTO published_list FROM barang.supplier_product_prices WHERE id=NEW.published_price_id;
  IF published_list IS DISTINCT FROM NEW.price_list_id THEN RAISE EXCEPTION 'Price list snapshot does not belong to its header' USING ERRCODE='23503';END IF;
 END IF;
 END IF;
 RETURN NEW;
END;$fn$;
CREATE TRIGGER supplier_products_sourcing_history BEFORE UPDATE ON barang.supplier_products FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_history_guard();
CREATE TRIGGER supplier_prices_history BEFORE INSERT OR UPDATE ON barang.supplier_product_prices FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_history_guard();
CREATE TRIGGER supplier_price_lists_history BEFORE INSERT OR UPDATE ON barang.supplier_price_lists FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_history_guard();
CREATE TRIGGER supplier_price_list_lines_history BEFORE INSERT OR UPDATE ON barang.supplier_price_list_lines FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_history_guard();

CREATE FUNCTION barang.supplier_sourcing_has_references(target_table text,target_id uuid) RETURNS boolean
LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
DECLARE target_company uuid; source_record jsonb; relation record; found boolean;
BEGIN
 IF target_table NOT IN('supplier_products','supplier_product_prices','supplier_price_lists','supplier_price_list_lines') THEN RETURN true;END IF;
 EXECUTE format('SELECT company_id,to_jsonb(t) FROM barang.%I t WHERE id=$1',target_table) INTO target_company,source_record USING target_id;
 IF target_company IS NULL OR target_company IS DISTINCT FROM NULLIF(current_setting('barang.company_id',true),'')::uuid THEN RETURN true;END IF;
 IF source_record->>'published_at_utc' IS NOT NULL OR source_record->>'published_price_id' IS NOT NULL THEN RETURN true;END IF;
 IF target_table='supplier_products' AND EXISTS(
  SELECT 1 FROM barang.purchase_orders po JOIN barang.purchase_order_items pi ON pi.purchase_order_id=po.id AND pi.company_id=po.company_id
  JOIN barang.product_variants v ON v.id=pi.product_variant_id AND v.company_id=pi.company_id
  WHERE po.company_id=target_company AND po.supplier_id=(source_record->>'supplier_id')::uuid AND v.product_id=(source_record->>'product_id')::uuid
   AND (source_record->>'product_variant_id' IS NULL OR v.id=(source_record->>'product_variant_id')::uuid)) THEN RETURN true;END IF;
 FOR relation IN
  SELECT c.conrelid::regclass source_table,a.attname source_column
  FROM pg_constraint c CROSS JOIN LATERAL unnest(c.conkey,c.confkey) AS k(source_number,target_number)
  JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.source_number
  JOIN pg_attribute b ON b.attrelid=c.confrelid AND b.attnum=k.target_number
  WHERE c.contype='f' AND c.confrelid=to_regclass('barang.'||target_table) AND b.attname='id'
   AND NOT(target_table='supplier_price_lists' AND c.conrelid='barang.supplier_price_list_lines'::regclass)
 LOOP
  EXECUTE format('SELECT EXISTS(SELECT 1 FROM %s WHERE %I=$1)',relation.source_table,relation.source_column) INTO found USING target_id;
  IF found THEN RETURN true;END IF;
 END LOOP;
 RETURN false;
END;$fn$;
CREATE FUNCTION barang.supplier_sourcing_delete_guard() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
BEGIN
 IF barang.supplier_sourcing_has_references(TG_TABLE_NAME,OLD.id) THEN RAISE EXCEPTION 'Sourcing record has protected history' USING ERRCODE='23503';END IF;
 IF TG_TABLE_NAME='supplier_price_list_lines' THEN
 IF EXISTS(SELECT 1 FROM barang.supplier_price_lists WHERE id=OLD.price_list_id AND published_at_utc IS NOT NULL) THEN
  RAISE EXCEPTION 'Published price list lines cannot be deleted' USING ERRCODE='23503';
 END IF;
 END IF;
 RETURN OLD;
END;$fn$;
CREATE TRIGGER supplier_products_protect_delete BEFORE DELETE ON barang.supplier_products FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_delete_guard();
CREATE TRIGGER supplier_product_prices_protect_delete BEFORE DELETE ON barang.supplier_product_prices FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_delete_guard();
CREATE TRIGGER supplier_price_lists_protect_delete BEFORE DELETE ON barang.supplier_price_lists FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_delete_guard();
CREATE TRIGGER supplier_price_list_lines_protect_delete BEFORE DELETE ON barang.supplier_price_list_lines FOR EACH ROW EXECUTE FUNCTION barang.supplier_sourcing_delete_guard();
DO $security$ DECLARE table_name text;role_name text;BEGIN
 FOREACH table_name IN ARRAY ARRAY['supplier_product_prices','supplier_price_lists','supplier_price_list_lines'] LOOP
  EXECUTE format('ALTER TABLE barang.%I ENABLE ROW LEVEL SECURITY',table_name);
  EXECUTE format('ALTER TABLE barang.%I FORCE ROW LEVEL SECURITY',table_name);
  EXECUTE format('CREATE POLICY %I ON barang.%I USING(company_id=NULLIF(current_setting(''barang.company_id'',true),'''')::uuid) WITH CHECK(company_id=NULLIF(current_setting(''barang.company_id'',true),'''')::uuid)',table_name||'_company_isolation',table_name);
 END LOOP;
 FOREACH role_name IN ARRAY ARRAY['barang_app','barang_test_app'] LOOP
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname=role_name) THEN
   EXECUTE format('GRANT SELECT,INSERT,UPDATE,DELETE ON barang.supplier_product_prices,barang.supplier_price_lists,barang.supplier_price_list_lines TO %I',role_name);
   EXECUTE format('GRANT DELETE ON barang.supplier_products TO %I',role_name);
  END IF;
 END LOOP;
END;$security$;
""";
    internal const string BeforeRollback="""
DROP TRIGGER IF EXISTS supplier_products_sourcing_references ON barang.supplier_products;
DROP TRIGGER IF EXISTS supplier_product_prices_references ON barang.supplier_product_prices;
DROP TRIGGER IF EXISTS supplier_price_list_lines_references ON barang.supplier_price_list_lines;
DROP TRIGGER IF EXISTS supplier_products_sourcing_history ON barang.supplier_products;
DROP TRIGGER IF EXISTS supplier_products_protect_delete ON barang.supplier_products;
DROP TRIGGER IF EXISTS supplier_prices_history ON barang.supplier_product_prices;
DROP TRIGGER IF EXISTS supplier_price_lists_history ON barang.supplier_price_lists;
DROP TRIGGER IF EXISTS supplier_price_list_lines_history ON barang.supplier_price_list_lines;
DROP TRIGGER IF EXISTS supplier_product_prices_protect_delete ON barang.supplier_product_prices;
DROP TRIGGER IF EXISTS supplier_price_lists_protect_delete ON barang.supplier_price_lists;
DROP TRIGGER IF EXISTS supplier_price_list_lines_protect_delete ON barang.supplier_price_list_lines;
DROP FUNCTION IF EXISTS barang.supplier_sourcing_history_guard() CASCADE;
DROP FUNCTION IF EXISTS barang.supplier_sourcing_delete_guard() CASCADE;
DROP FUNCTION IF EXISTS barang.supplier_sourcing_has_references(text,uuid);
""";
}
