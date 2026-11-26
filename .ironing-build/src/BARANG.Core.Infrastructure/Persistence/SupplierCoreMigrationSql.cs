namespace BARANG.Core.Infrastructure.Persistence;

// Additive supplier aggregate migration. Existing shared reference identities are never rewritten.
internal static class SupplierCoreMigrationSql
{
    internal const string AfterSchema = """
CREATE TABLE barang.supplier_number_counters(
 company_id uuid PRIMARY KEY REFERENCES barang.companies(id) ON DELETE RESTRICT,
 last_number bigint NOT NULL CHECK(last_number>0));
WITH numbered AS(
 SELECT id,row_number() OVER(PARTITION BY company_id ORDER BY created_at_utc,id) AS number FROM barang.suppliers)
UPDATE barang.suppliers s SET supplier_number=n.number,
 entity_type=CASE WHEN NULLIF(trim(s.legal_name),'') IS NULL THEN 'Individual' ELSE 'LegalEntity' END
FROM numbered n WHERE s.id=n.id;
INSERT INTO barang.supplier_number_counters(company_id,last_number)
 SELECT company_id,max(supplier_number) FROM barang.suppliers GROUP BY company_id;
ALTER TABLE barang.suppliers ADD CONSTRAINT ck_suppliers_number_positive CHECK(supplier_number>0);
CREATE FUNCTION barang.supplier_number_allocate() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
BEGIN
 IF TG_OP='UPDATE' THEN
  IF NEW.supplier_number IS DISTINCT FROM OLD.supplier_number THEN RAISE EXCEPTION 'Supplier number is immutable' USING ERRCODE='23514'; END IF;
 ELSE
  INSERT INTO barang.supplier_number_counters(company_id,last_number) VALUES(NEW.company_id,1)
  ON CONFLICT(company_id) DO UPDATE SET last_number=barang.supplier_number_counters.last_number+1
  RETURNING last_number INTO NEW.supplier_number;
 END IF;
 RETURN NEW;
END;$fn$;
CREATE TRIGGER supplier_number_allocate BEFORE INSERT OR UPDATE ON barang.suppliers FOR EACH ROW EXECUTE FUNCTION barang.supplier_number_allocate();

CREATE FUNCTION barang.supplier_reference_integrity() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
DECLARE n integer; value_id uuid; old_id uuid; item record;
BEGIN
 FOR n IN 0..TG_NARGS/2-1 LOOP
  value_id=NULLIF(to_jsonb(NEW)->>TG_ARGV[n*2],'')::uuid;
  IF value_id IS NULL THEN CONTINUE; END IF;
  old_id=CASE WHEN TG_OP='UPDATE' THEN NULLIF(to_jsonb(OLD)->>TG_ARGV[n*2],'')::uuid ELSE NULL END;
  SELECT v.company_id,t.scope,t.code type_code,v.is_active AND t.is_active active INTO item
  FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE v.id=value_id;
  IF NOT FOUND OR item.type_code<>TG_ARGV[n*2+1] OR
     (item.scope='Company' AND item.company_id IS DISTINCT FROM NEW.company_id) OR
     (item.scope='Global' AND TG_ARGV[n*2+1] NOT IN('bank','payment.method')) OR
     (NOT item.active AND value_id IS DISTINCT FROM old_id) THEN
   RAISE EXCEPTION 'Invalid Supplier reference scope, type or activation' USING ERRCODE='23503';
  END IF;
 END LOOP;
 RETURN NEW;
END;$fn$;
CREATE TRIGGER suppliers_supplier_references BEFORE INSERT OR UPDATE ON barang.suppliers FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('legal_entity_type_id','legal.entity.type','grade_id','supplier.grade','hold_reason_id','supplier.hold.reason');
CREATE TRIGGER supplier_financial_profiles_supplier_references BEFORE INSERT OR UPDATE ON barang.supplier_financial_profiles FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('payment_term_id','payment.term','payment_condition_id','payment.condition','payment_method_id','payment.method','shipping_method_id','shipping.method','freight_delivery_term_id','freight.delivery.term');
CREATE TRIGGER supplier_addresses_supplier_references BEFORE INSERT OR UPDATE ON barang.supplier_addresses FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('address_type_id','address.type');
CREATE TRIGGER supplier_contacts_supplier_references BEFORE INSERT OR UPDATE ON barang.supplier_contacts FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('contact_role_id','contact.role');
CREATE TRIGGER supplier_bank_accounts_supplier_references BEFORE INSERT OR UPDATE ON barang.supplier_bank_accounts FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('bank_id','bank','account_type_id','bank.account.type');
CREATE TRIGGER supplier_category_assignment_reference BEFORE INSERT OR UPDATE ON barang.supplier_category_assignments FOR EACH ROW EXECUTE FUNCTION barang.supplier_reference_integrity('category_id','supplier.category');

CREATE FUNCTION barang.supplier_has_protected_references(target_table text,target_id uuid) RETURNS boolean
LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
DECLARE target_company uuid; relation record; found boolean;
BEGIN
 IF target_table NOT IN('suppliers','supplier_addresses','supplier_contacts','supplier_bank_accounts','supplier_financial_profiles','supplier_category_assignments') THEN RETURN true; END IF;
 EXECUTE format('SELECT company_id FROM barang.%I WHERE id=$1',target_table) INTO target_company USING target_id;
 IF target_company IS NULL OR target_company IS DISTINCT FROM NULLIF(current_setting('barang.company_id',true),'')::uuid THEN RETURN true; END IF;
 FOR relation IN
  SELECT c.conrelid::regclass source_table,a.attname source_column
  FROM pg_constraint c CROSS JOIN LATERAL unnest(c.conkey,c.confkey) AS k(source_number,target_number)
  JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.source_number
  JOIN pg_attribute b ON b.attrelid=c.confrelid AND b.attnum=k.target_number
  WHERE c.contype='f' AND c.confrelid=to_regclass('barang.'||target_table) AND b.attname='id'
    AND NOT(target_table='suppliers' AND c.conrelid IN('barang.supplier_financial_profiles'::regclass,'barang.supplier_category_assignments'::regclass))
 LOOP
  EXECUTE format('SELECT EXISTS(SELECT 1 FROM %s WHERE %I=$1)',relation.source_table,relation.source_column) INTO found USING target_id;
  IF found THEN RETURN true; END IF;
 END LOOP;
 RETURN false;
END;$fn$;
CREATE FUNCTION barang.supplier_delete_guard() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
BEGIN
 IF barang.supplier_has_protected_references(TG_TABLE_NAME,OLD.id) THEN
  RAISE EXCEPTION 'Supplier record has protected references' USING ERRCODE='23503';
 END IF;
 RETURN OLD;
END;$fn$;
CREATE TRIGGER suppliers_protect_delete BEFORE DELETE ON barang.suppliers FOR EACH ROW EXECUTE FUNCTION barang.supplier_delete_guard();
CREATE TRIGGER supplier_addresses_protect_delete BEFORE DELETE ON barang.supplier_addresses FOR EACH ROW EXECUTE FUNCTION barang.supplier_delete_guard();
CREATE TRIGGER supplier_contacts_protect_delete BEFORE DELETE ON barang.supplier_contacts FOR EACH ROW EXECUTE FUNCTION barang.supplier_delete_guard();
CREATE TRIGGER supplier_bank_accounts_protect_delete BEFORE DELETE ON barang.supplier_bank_accounts FOR EACH ROW EXECUTE FUNCTION barang.supplier_delete_guard();
CREATE TRIGGER supplier_financial_profiles_protect_delete BEFORE DELETE ON barang.supplier_financial_profiles FOR EACH ROW EXECUTE FUNCTION barang.supplier_delete_guard();
CREATE TRIGGER supplier_category_assignments_protect_delete BEFORE DELETE ON barang.supplier_category_assignments FOR EACH ROW EXECUTE FUNCTION barang.supplier_delete_guard();
ALTER TABLE barang.supplier_number_counters ENABLE ROW LEVEL SECURITY;
ALTER TABLE barang.supplier_number_counters FORCE ROW LEVEL SECURITY;
CREATE POLICY supplier_number_counters_company ON barang.supplier_number_counters
 USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
 WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
ALTER TABLE barang.supplier_addresses ENABLE ROW LEVEL SECURITY;
ALTER TABLE barang.supplier_addresses FORCE ROW LEVEL SECURITY;
CREATE POLICY supplier_addresses_company ON barang.supplier_addresses
 USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
 WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
ALTER TABLE barang.supplier_contacts ENABLE ROW LEVEL SECURITY;
ALTER TABLE barang.supplier_contacts FORCE ROW LEVEL SECURITY;
CREATE POLICY supplier_contacts_company ON barang.supplier_contacts
 USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
 WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
ALTER TABLE barang.supplier_bank_accounts ENABLE ROW LEVEL SECURITY;
ALTER TABLE barang.supplier_bank_accounts FORCE ROW LEVEL SECURITY;
CREATE POLICY supplier_bank_accounts_company ON barang.supplier_bank_accounts
 USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
 WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
ALTER TABLE barang.supplier_financial_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE barang.supplier_financial_profiles FORCE ROW LEVEL SECURITY;
CREATE POLICY supplier_financial_profiles_company ON barang.supplier_financial_profiles
 USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
 WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
ALTER TABLE barang.supplier_category_assignments ENABLE ROW LEVEL SECURITY;
ALTER TABLE barang.supplier_category_assignments FORCE ROW LEVEL SECURITY;
CREATE POLICY supplier_category_assignments_company ON barang.supplier_category_assignments
 USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
 WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
DO $grants$ BEGIN
 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN
  GRANT SELECT,INSERT,UPDATE,DELETE ON barang.supplier_addresses,barang.supplier_contacts,barang.supplier_bank_accounts,barang.supplier_financial_profiles,barang.supplier_category_assignments TO barang_app;
  GRANT SELECT,INSERT,UPDATE ON barang.supplier_number_counters TO barang_app;
  GRANT DELETE ON barang.suppliers TO barang_app;
 END IF;
END;$grants$;

CREATE FUNCTION barang.seed_supplier_reference_catalogs(target_company uuid) RETURNS void
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
BEGIN
 INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
 SELECT gen_random_uuid(),target_company,d.code,d.caption,'Company',false,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
 FROM(VALUES ('supplier.category','Supplier Categories'),('supplier.grade','Supplier Grades'),('freight.delivery.term','Freight / Delivery Terms'),('supplier.hold.reason','Supplier Hold Reasons'))d(code,caption)
 ON CONFLICT DO NOTHING;
 INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
 SELECT gen_random_uuid(),t.id,target_company,d.code,d.sort,true,false,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
 FROM(VALUES ('raw_product','Raw Product Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0645\062d\0635\0648\0644 \062e\0627\0645',U&'\0645\0648\0631\062f \0627\0644\0645\0646\062a\062c\0627\062a \0627\0644\062e\0627\0645',10),
('printing_consumables','Printing Consumables Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0645\0648\0627\062f \0645\0635\0631\0641\06cc \0686\0627\067e',U&'\0645\0648\0631\062f \0645\0633\062a\0647\0644\0643\0627\062a \0627\0644\0637\0628\0627\0639\0629',20),
('equipment','Equipment / Machinery Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \062a\062c\0647\06cc\0632\0627\062a \0648 \0645\0627\0634\06cc\0646\200c\0622\0644\0627\062a',U&'\0645\0648\0631\062f \0627\0644\0645\0639\062f\0627\062a \0648\0627\0644\0622\0644\0627\062a',30),
('spare_parts','Spare Parts Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0642\0637\0639\0627\062a \06cc\062f\06a9\06cc',U&'\0645\0648\0631\062f \0642\0637\0639 \0627\0644\063a\064a\0627\0631',40),
('packaging','Packaging Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0628\0633\062a\0647\200c\0628\0646\062f\06cc',U&'\0645\0648\0631\062f \0645\0648\0627\062f \0627\0644\062a\063a\0644\064a\0641',50),
('service','Service Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \062e\062f\0645\0627\062a',U&'\0645\0648\0631\062f \0627\0644\062e\062f\0645\0627\062a',60),
('logistics','Logistics Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0644\062c\0633\062a\06cc\06a9',U&'\0645\0648\0631\062f \0627\0644\062e\062f\0645\0627\062a \0627\0644\0644\0648\062c\0633\062a\064a\0629',70))d(code,en,fa,ar,sort)
 JOIN barang.lookup_types t ON t.company_id=target_company AND t.code='supplier.category'
 ON CONFLICT(lookup_type_id,code) DO NOTHING;
 INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,created_at_utc,updated_at_utc,version)
 SELECT gen_random_uuid(),v.id,target_company,c.code,CASE c.code WHEN 'fa' THEN d.fa WHEN 'ar' THEN d.ar ELSE d.en END,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
 FROM(VALUES ('raw_product','Raw Product Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0645\062d\0635\0648\0644 \062e\0627\0645',U&'\0645\0648\0631\062f \0627\0644\0645\0646\062a\062c\0627\062a \0627\0644\062e\0627\0645',10),
('printing_consumables','Printing Consumables Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0645\0648\0627\062f \0645\0635\0631\0641\06cc \0686\0627\067e',U&'\0645\0648\0631\062f \0645\0633\062a\0647\0644\0643\0627\062a \0627\0644\0637\0628\0627\0639\0629',20),
('equipment','Equipment / Machinery Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \062a\062c\0647\06cc\0632\0627\062a \0648 \0645\0627\0634\06cc\0646\200c\0622\0644\0627\062a',U&'\0645\0648\0631\062f \0627\0644\0645\0639\062f\0627\062a \0648\0627\0644\0622\0644\0627\062a',30),
('spare_parts','Spare Parts Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0642\0637\0639\0627\062a \06cc\062f\06a9\06cc',U&'\0645\0648\0631\062f \0642\0637\0639 \0627\0644\063a\064a\0627\0631',40),
('packaging','Packaging Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0628\0633\062a\0647\200c\0628\0646\062f\06cc',U&'\0645\0648\0631\062f \0645\0648\0627\062f \0627\0644\062a\063a\0644\064a\0641',50),
('service','Service Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \062e\062f\0645\0627\062a',U&'\0645\0648\0631\062f \0627\0644\062e\062f\0645\0627\062a',60),
('logistics','Logistics Supplier',U&'\062a\0623\0645\06cc\0646\200c\06a9\0646\0646\062f\0647 \0644\062c\0633\062a\06cc\06a9',U&'\0645\0648\0631\062f \0627\0644\062e\062f\0645\0627\062a \0627\0644\0644\0648\062c\0633\062a\064a\0629',70))d(code,en,fa,ar,sort)
 JOIN barang.lookup_types t ON t.company_id=target_company AND t.code='supplier.category'
 JOIN barang.lookup_values v ON v.lookup_type_id=t.id AND v.code=d.code
 CROSS JOIN(VALUES('fa'),('ar'),('en'))c(code)
 ON CONFLICT(lookup_value_id,culture) DO NOTHING;
END;$fn$;
REVOKE ALL ON FUNCTION barang.seed_supplier_reference_catalogs(uuid) FROM PUBLIC;
CREATE FUNCTION barang.supplier_catalogs_company_created() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
BEGIN PERFORM barang.seed_supplier_reference_catalogs(NEW.id);RETURN NEW;END;$fn$;
CREATE TRIGGER supplier_catalogs_company_created AFTER INSERT ON barang.companies FOR EACH ROW EXECUTE FUNCTION barang.supplier_catalogs_company_created();
DO $seed$ DECLARE c uuid; BEGIN FOR c IN SELECT id FROM barang.companies LOOP PERFORM barang.seed_supplier_reference_catalogs(c);END LOOP;END;$seed$;

CREATE OR REPLACE FUNCTION barang.lookup_value_is_referenced(target_id uuid) RETURNS boolean
        LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
        DECLARE target record; mapping record; found boolean;
        BEGIN
         SELECT v.id,v.code,v.company_id,t.code type_code INTO target FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id
         WHERE v.id=target_id AND (v.company_id IS NULL OR v.company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
         IF NOT FOUND THEN RETURN false; END IF;
         FOR mapping IN SELECT * FROM (VALUES ('customer.group','customers','customer_group_code'),
        ('price.group','customers','price_group_code'),
        ('legal.entity.type','legal_customer_profiles','legal_entity_type_code'),
        ('payment.term','customer_financial_profiles','payment_term_code'),
        ('payment.condition','customer_financial_profiles','payment_condition_code'),
        ('payment.method','customer_financial_profiles','preferred_payment_method_code'),
        ('payment.method','payments','payment_method_code'),
        ('tax.status','customer_financial_profiles','tax_status_code'),
        ('address.type','customer_addresses','address_type_code'),
        ('contact.role','customer_contacts','contact_type_code'),
        ('bank','customer_bank_accounts','bank_code'),
        ('bank.account.type','customer_bank_accounts','account_type_code'),
        ('product.type','products','product_type_code'),
        ('product.size','product_variants','size_code'),
        ('product.color','product_variants','color_code'),
        ('unit.of.measure','products','unit_of_measure_code'),
        ('unit.of.measure','stock_transactions','uom_code'),
        ('product.brand','product_technical_profiles','brand_code'),
        ('product.material','product_technical_profiles','material_code'),
        ('product.gender','product_technical_profiles','gender_code'),
        ('product.fit','product_technical_profiles','fit_code'),
        ('product.style','product_technical_profiles','style_code'),
        ('artwork.category','artworks','category_code'),
        ('order.type','order_business_profiles','order_type_code'),
        ('inventory.adjustment.reason','stock_transactions','reason_code'),
        ('equipment.type','equipment','equipment_type_code'),
        ('equipment.status','equipment','operational_status_code'),
        ('print.method','order_print_operations','print_method_code'),
        ('print.direction','print_technical_profiles','direction_code'),
        ('print.size','print_technical_profiles','print_size_code'),
        ('defect.category','defect_types','category_code'),
        ('packaging.type','packages','package_type_code'),
        ('packaging.type','order_business_profiles','packaging_type_code'),
        ('shipping.method','shipments','shipping_method_code'),
        ('shipping.method','order_business_profiles','shipping_method_code'),
        ('shipping.carrier','shipments','carrier_code'),
        ('product.style','order_item_technical_snapshots','style_code'),
        ('product.fit','order_item_technical_snapshots','fit_code'),
        ('product.gender','order_item_technical_snapshots','gender_code'),
        ('product.material','order_item_technical_snapshots','material_code'),
        ('product.brand','order_item_technical_snapshots','brand_code'),
        ('unit.of.measure','order_items','snapshot_uom_code'),
        ('product.color','order_items','snapshot_color_code'),
        ('product.size','order_items','snapshot_size_code'),
        ('product.type','qc_templates','product_type_code')) m(type_code,table_name,column_name) WHERE m.type_code=target.type_code LOOP
          IF EXISTS(SELECT 1 FROM information_schema.columns c WHERE c.table_schema='barang' AND c.table_name=mapping.table_name AND c.column_name=mapping.column_name) THEN
           EXECUTE format('SELECT EXISTS(SELECT 1 FROM barang.%I WHERE lower(trim(%I))=$1 AND ($2 IS NULL OR company_id=$2))',mapping.table_name,mapping.column_name) INTO found USING target.code,target.company_id;
           IF found THEN RETURN true; END IF;
          END IF;
         END LOOP;
         -- Typed UUID references supplement the existing code-backed historical mappings.
         FOR mapping IN
          SELECT c.conrelid::regclass source_table,a.attname source_column
          FROM pg_constraint c CROSS JOIN LATERAL unnest(c.conkey,c.confkey) AS k(source_number,target_number)
          JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.source_number
          JOIN pg_attribute b ON b.attrelid=c.confrelid AND b.attnum=k.target_number
          WHERE c.contype='f' AND c.confrelid='barang.lookup_values'::regclass AND b.attname='id'
            AND c.conrelid<>'barang.lookup_value_translations'::regclass
         LOOP
          EXECUTE format('SELECT EXISTS(SELECT 1 FROM %s WHERE %I=$1)',mapping.source_table,mapping.source_column) INTO found USING target_id;
          IF found THEN RETURN true;END IF;
         END LOOP;
         RETURN false;
        END;
        $fn$;
""";
}
