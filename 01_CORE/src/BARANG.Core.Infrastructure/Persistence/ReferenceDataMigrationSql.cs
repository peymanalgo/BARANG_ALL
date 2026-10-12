namespace BARANG.Core.Infrastructure.Persistence;

/// <summary>Forward-only additive migration/backfill; no customer or historical reference is removed.</summary>
internal static class ReferenceDataMigrationSql
{
    internal const string Up = """
        
        -- Scope is explicit: a canonical type code is unique per company, or once globally.
        CREATE OR REPLACE FUNCTION barang.reference_global_admin() RETURNS boolean
        LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
        SELECT EXISTS(
         SELECT 1 FROM barang.application_users u
         JOIN barang.user_roles a ON a.user_id=u.id
         JOIN barang.user_companies access ON access.user_id=a.user_id AND access.company_id=a.company_id
         JOIN barang.roles r ON r.id=a.role_id
         WHERE u.id=NULLIF(current_setting('barang.user_id',true),'')::uuid
         AND u.account_state='Active' AND r.code='system.administrator' AND r.is_active
         AND a.valid_from_utc<=CURRENT_TIMESTAMP AND (a.valid_to_utc IS NULL OR a.valid_to_utc>CURRENT_TIMESTAMP)
         AND access.valid_from_utc<=CURRENT_TIMESTAMP AND (access.valid_to_utc IS NULL OR access.valid_to_utc>CURRENT_TIMESTAMP)
        );
        $fn$;
        
        DROP POLICY lookup_types_company_write ON barang.lookup_types;
        CREATE POLICY lookup_types_company_write ON barang.lookup_types FOR ALL
        USING (company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid OR (company_id IS NULL AND barang.reference_global_admin()))
        WITH CHECK (company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid OR (company_id IS NULL AND barang.reference_global_admin()));
        DROP POLICY lookup_values_company_write ON barang.lookup_values;
        CREATE POLICY lookup_values_company_write ON barang.lookup_values FOR ALL
        USING (company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid OR (company_id IS NULL AND barang.reference_global_admin()))
        WITH CHECK (company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid OR (company_id IS NULL AND barang.reference_global_admin()));
        DROP POLICY lookup_value_translations_company_write ON barang.lookup_value_translations;
        CREATE POLICY lookup_value_translations_company_write ON barang.lookup_value_translations FOR ALL
        USING (company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid OR (company_id IS NULL AND barang.reference_global_admin()))
        WITH CHECK (company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid OR (company_id IS NULL AND barang.reference_global_admin()));
        
        CREATE OR REPLACE FUNCTION barang.seed_company_reference_catalogs(target_company uuid) RETURNS void
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
        BEGIN
         INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
         SELECT gen_random_uuid(),CASE WHEN d.scope='Global' THEN NULL ELSE target_company END,d.code,d.name,d.scope,false,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
         FROM (VALUES ('customer.group','Customer Groups','Company'),
        ('price.group','Price Groups','Company'),
        ('legal.entity.type','Legal Entity Types','Company'),
        ('payment.term','Payment Terms','Company'),
        ('payment.condition','Payment Conditions','Company'),
        ('payment.method','Payment Methods','Global'),
        ('tax.status','Tax Statuses','Company'),
        ('address.type','Address Types','Company'),
        ('contact.role','Contact Roles','Company'),
        ('bank','Banks','Global'),
        ('bank.account.type','Bank Account Types','Company'),
        ('product.type','Product Types','Company'),
        ('product.size','Sizes','Company'),
        ('product.color','Colors','Company'),
        ('unit.of.measure','Units of Measure','Company'),
        ('product.brand','Brands','Company'),
        ('product.material','Materials / Fabrics','Company'),
        ('product.gender','Apparel Gender Classifications','Company'),
        ('product.fit','Fits','Company'),
        ('product.style','Styles','Company'),
        ('artwork.category','Artwork Categories','Company'),
        ('order.type','Order Types','Company'),
        ('inventory.adjustment.reason','Inventory Adjustment Reasons','Company'),
        ('equipment.type','Equipment Types','Company'),
        ('equipment.status','Equipment Operational Statuses','Company'),
        ('equipment.category','Equipment Categories','Company'),('equipment.subtype','Equipment Subtypes','Company'),('equipment.brand','Equipment Brands','Company'),('equipment.manufacturer','Equipment Manufacturers','Company'),('equipment.condition','Equipment Conditions','Company'),('equipment.ownership','Equipment Ownership Types','Company'),('equipment.acquisition','Equipment Acquisition Methods','Company'),('equipment.criticality','Equipment Criticality Levels','Company'),('equipment.service.type','Service / Maintenance Types','Company'),('equipment.failure.type','Failure Types','Company'),('equipment.failure.cause','Failure Causes','Company'),('equipment.repair.type','Repair Types','Company'),('equipment.inspection.type','Inspection Types','Company'),('equipment.calibration.type','Calibration Types','Company'),('equipment.retirement.reason','Retirement / Disposal Reasons','Company'),
        ('print.method','Print Methods','Company'),
        ('print.direction','Print Directions','Company'),
        ('print.size','Print Sizes','Company'),
        ('defect.category','Defect Categories','Company'),
        ('packaging.type','Packaging Types','Company'),
        ('shipping.method','Shipping Methods','Company'),
        ('shipping.carrier','Carriers','Company')) AS d(code,name,scope)
         WHERE NOT EXISTS(SELECT 1 FROM barang.lookup_types t WHERE t.code=d.code AND t.company_id IS NOT DISTINCT FROM CASE WHEN d.scope='Global' THEN NULL ELSE target_company END)
         ON CONFLICT DO NOTHING;
        

         -- Company catalogs inherit existing business choices without changing any existing global ID.
         INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
         SELECT gen_random_uuid(),target.id,target_company,source.code,source.display_order,source.is_active,false,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
         FROM barang.lookup_types target
         JOIN barang.lookup_types global_type ON global_type.code=target.code AND global_type.company_id IS NULL
         JOIN barang.lookup_values source ON source.lookup_type_id=global_type.id
         WHERE target.company_id=target_company AND target.code IN ('customer.group','price.group','legal.entity.type','payment.term','payment.condition','tax.status','address.type','contact.role','bank.account.type','product.type','product.size','product.color','unit.of.measure','product.brand','product.material','product.gender','product.fit','product.style','artwork.category','order.type','inventory.adjustment.reason','equipment.type','equipment.status','print.method','print.direction','print.size','defect.category','packaging.type','shipping.method','shipping.carrier')
         ON CONFLICT(lookup_type_id,code) DO NOTHING;
         INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,description,created_at_utc,updated_at_utc,version)
         SELECT gen_random_uuid(),target_value.id,target_company,caption.culture,caption.display_name,caption.description,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
         FROM barang.lookup_types target
         JOIN barang.lookup_types global_type ON global_type.code=target.code AND global_type.company_id IS NULL
         JOIN barang.lookup_values source ON source.lookup_type_id=global_type.id
         JOIN barang.lookup_values target_value ON target_value.lookup_type_id=target.id AND target_value.code=source.code
         JOIN barang.lookup_value_translations caption ON caption.lookup_value_id=source.id
         WHERE target.company_id=target_company
         ON CONFLICT(lookup_value_id,culture) DO NOTHING;

         -- These registered global catalogs are business data, not technical enums.
         UPDATE barang.lookup_types SET is_system=false,allow_custom_values=true WHERE code IN('payment.method','bank') AND company_id IS NULL;
         UPDATE barang.lookup_values SET is_system=false WHERE lookup_type_id IN(SELECT id FROM barang.lookup_types WHERE code IN('payment.method','bank') AND company_id IS NULL);
        
         INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
         SELECT gen_random_uuid(),t.id,t.company_id,d.code,d.display_order,true,false,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
         FROM (VALUES ('address.type','registered',10,'ثبتی','Registered','مسجل'),
        ('address.type','billing',20,'صورتحساب','Billing','الفوترة'),
        ('address.type','shipping',30,'ارسال','Shipping','الشحن'),
        ('address.type','warehouse',40,'انبار','Warehouse','المستودع'),
        ('address.type','other',50,'سایر','Other','أخرى'),
        ('contact.role','general',10,'عمومی','General','عام'),
        ('contact.role','finance',20,'مالی','Finance','المالية'),
        ('contact.role','purchasing',30,'خرید','Purchasing','المشتريات'),
        ('payment.method','cash',10,'نقدی','Cash','نقدي'),
        ('payment.method','bank_transfer',20,'انتقال بانکی','Bank Transfer','تحويل بنكي'),
        ('payment.method','card',30,'کارت','Card','بطاقة'),
        ('payment.method','online',40,'آنلاین','Online','إلكتروني'),
        ('payment.method','other',50,'سایر','Other','أخرى')) AS d(type_code,code,display_order,fa,en,ar)
         JOIN barang.lookup_types t ON t.code=d.type_code AND (t.company_id=target_company OR (t.company_id IS NULL AND t.code='payment.method'))
         ON CONFLICT(lookup_type_id,code) DO NOTHING;
        
         INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,created_at_utc,updated_at_utc,version)
         SELECT gen_random_uuid(),v.id,v.company_id,culture.code,CASE culture.code WHEN 'fa' THEN d.fa WHEN 'ar' THEN d.ar ELSE d.en END,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
         FROM (VALUES ('address.type','registered',10,'ثبتی','Registered','مسجل'),
        ('address.type','billing',20,'صورتحساب','Billing','الفوترة'),
        ('address.type','shipping',30,'ارسال','Shipping','الشحن'),
        ('address.type','warehouse',40,'انبار','Warehouse','المستودع'),
        ('address.type','other',50,'سایر','Other','أخرى'),
        ('contact.role','general',10,'عمومی','General','عام'),
        ('contact.role','finance',20,'مالی','Finance','المالية'),
        ('contact.role','purchasing',30,'خرید','Purchasing','المشتريات'),
        ('payment.method','cash',10,'نقدی','Cash','نقدي'),
        ('payment.method','bank_transfer',20,'انتقال بانکی','Bank Transfer','تحويل بنكي'),
        ('payment.method','card',30,'کارت','Card','بطاقة'),
        ('payment.method','online',40,'آنلاین','Online','إلكتروني'),
        ('payment.method','other',50,'سایر','Other','أخرى')) AS d(type_code,code,display_order,fa,en,ar)
         JOIN barang.lookup_types t ON t.code=d.type_code AND (t.company_id=target_company OR (t.company_id IS NULL AND t.code='payment.method'))
         JOIN barang.lookup_values v ON v.lookup_type_id=t.id AND v.code=d.code
         CROSS JOIN (VALUES('fa'),('en'),('ar')) culture(code)
         ON CONFLICT(lookup_value_id,culture) DO NOTHING;
        END;
        $fn$;
        -- Only the deployment trigger may invoke this seed routine.
        REVOKE ALL ON FUNCTION barang.seed_company_reference_catalogs(uuid) FROM PUBLIC;
        CREATE OR REPLACE FUNCTION barang.company_reference_catalogs_created() RETURNS trigger
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
        BEGIN PERFORM barang.seed_company_reference_catalogs(NEW.id); RETURN NEW; END;
        $fn$;
        CREATE TRIGGER company_reference_catalogs_created AFTER INSERT ON barang.companies
        FOR EACH ROW EXECUTE FUNCTION barang.company_reference_catalogs_created();
        
        DO $backfill$
        DECLARE company_row record; mapping record;
        BEGIN
         FOR company_row IN SELECT id FROM barang.companies LOOP PERFORM barang.seed_company_reference_catalogs(company_row.id); END LOOP;
         -- Preserve legacy named banks with stable generated codes before assigning reference IDs.
         UPDATE barang.customer_bank_accounts SET bank_code='legacy.'||md5(lower(trim(bank_name)))
         WHERE bank_code IS NULL AND bank_name IS NOT NULL AND trim(bank_name)<>'';
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
        ('product.type','qc_templates','product_type_code')) m(type_code,table_name,column_name) LOOP
          IF EXISTS(SELECT 1 FROM information_schema.columns c WHERE c.table_schema='barang' AND c.table_name=mapping.table_name AND c.column_name=mapping.column_name) THEN
           EXECUTE format(
            'INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
             SELECT gen_random_uuid(),t.id,t.company_id,source.code,0,false,false,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
             FROM (SELECT DISTINCT company_id,lower(trim(%1$I)) code FROM barang.%2$I WHERE %1$I IS NOT NULL AND trim(%1$I)<>'''') source
             JOIN barang.lookup_types t ON t.code=%3$L AND (t.company_id=source.company_id OR t.company_id IS NULL)
             ON CONFLICT(lookup_type_id,code) DO NOTHING',mapping.column_name,mapping.table_name,mapping.type_code);
          END IF;
         END LOOP;
        END;
        $backfill$;
        
        INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,created_at_utc,updated_at_utc,version)
        SELECT gen_random_uuid(),v.id,v.company_id,c.code,COALESCE(existing.display_name,(SELECT min(account.bank_name) FROM barang.customer_bank_accounts account JOIN barang.lookup_types bank_type ON bank_type.id=v.lookup_type_id WHERE bank_type.code='bank' AND lower(trim(account.bank_code))=v.code),v.code),CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
        FROM barang.lookup_values v
        CROSS JOIN (VALUES('fa'),('en'),('ar')) c(code)
        LEFT JOIN LATERAL(SELECT display_name FROM barang.lookup_value_translations WHERE lookup_value_id=v.id ORDER BY CASE culture WHEN 'fa' THEN 0 WHEN 'en' THEN 1 ELSE 2 END LIMIT 1) existing ON true
        ON CONFLICT(lookup_value_id,culture) DO NOTHING;
        
        UPDATE barang.customers owner SET customer_group_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='customer.group' AND lower(trim(owner.customer_group_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.customer_group_code IS NOT NULL;
        UPDATE barang.customers owner SET price_group_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='price.group' AND lower(trim(owner.price_group_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.price_group_code IS NOT NULL;
        UPDATE barang.legal_customer_profiles owner SET legal_entity_type_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='legal.entity.type' AND lower(trim(owner.legal_entity_type_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.legal_entity_type_code IS NOT NULL;
        UPDATE barang.customer_financial_profiles owner SET payment_term_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='payment.term' AND lower(trim(owner.payment_term_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.payment_term_code IS NOT NULL;
        UPDATE barang.customer_financial_profiles owner SET payment_condition_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='payment.condition' AND lower(trim(owner.payment_condition_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.payment_condition_code IS NOT NULL;
        UPDATE barang.customer_financial_profiles owner SET preferred_payment_method_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='payment.method' AND lower(trim(owner.preferred_payment_method_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.preferred_payment_method_code IS NOT NULL;
        UPDATE barang.customer_financial_profiles owner SET tax_status_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='tax.status' AND lower(trim(owner.tax_status_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.tax_status_code IS NOT NULL;
        UPDATE barang.customer_addresses owner SET address_type_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='address.type' AND lower(trim(owner.address_type_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.address_type_code IS NOT NULL;
        UPDATE barang.customer_contacts owner SET contact_role_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='contact.role' AND lower(trim(owner.contact_type_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.contact_type_code IS NOT NULL;
        UPDATE barang.customer_bank_accounts owner SET bank_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='bank' AND lower(trim(owner.bank_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.bank_code IS NOT NULL;
        UPDATE barang.customer_bank_accounts owner SET account_type_id=(SELECT v.id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE t.code='bank.account.type' AND lower(trim(owner.account_type_code))=v.code AND (v.company_id=owner.company_id OR v.company_id IS NULL) ORDER BY v.company_id IS NOT NULL DESC LIMIT 1) WHERE owner.account_type_code IS NOT NULL;
        
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
         RETURN false;
        END;
        $fn$;
        
        CREATE OR REPLACE FUNCTION barang.protect_referenced_lookup_value() RETURNS trigger
        LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
        BEGIN
         IF barang.lookup_value_is_referenced(OLD.id) THEN RAISE EXCEPTION 'Referenced lookup value: deactivate instead' USING ERRCODE='23503'; END IF;
         RETURN OLD;
        END;
        $fn$;
        CREATE TRIGGER lookup_value_restrict_delete BEFORE DELETE ON barang.lookup_values FOR EACH ROW EXECUTE FUNCTION barang.protect_referenced_lookup_value();
        
        CREATE OR REPLACE FUNCTION barang.customer_reference_integrity() RETURNS trigger
        LANGUAGE plpgsql SET search_path=pg_catalog,barang AS $fn$
        DECLARE index_value integer; value_id uuid; reference_code text; value_row record;
        BEGIN
         FOR index_value IN 0..(TG_NARGS/3-1) LOOP
          value_id=NULLIF(to_jsonb(NEW)->>TG_ARGV[index_value*3],'')::uuid;
          reference_code=to_jsonb(NEW)->>TG_ARGV[index_value*3+2];
          IF value_id IS NOT NULL THEN
           SELECT v.code,v.company_id,t.code type_code INTO value_row FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id WHERE v.id=value_id;
           IF NOT FOUND OR value_row.type_code<>TG_ARGV[index_value*3+1] OR (value_row.company_id IS NOT NULL AND value_row.company_id<>NEW.company_id) OR lower(trim(reference_code)) IS DISTINCT FROM value_row.code THEN
            RAISE EXCEPTION 'Invalid customer reference scope or type' USING ERRCODE='23503';
           END IF;
          ELSIF reference_code IS NOT NULL AND trim(reference_code)<>'' THEN
           SELECT v.id INTO value_id FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id
           WHERE t.code=TG_ARGV[index_value*3+1] AND v.code=lower(trim(reference_code)) AND (v.company_id=NEW.company_id OR v.company_id IS NULL)
           ORDER BY v.company_id IS NOT NULL DESC LIMIT 1;
           IF value_id IS NULL THEN RAISE EXCEPTION 'Customer reference ID is required' USING ERRCODE='23503'; END IF;
           NEW=jsonb_populate_record(NEW,jsonb_build_object(TG_ARGV[index_value*3],value_id));
          END IF;
         END LOOP;
         RETURN NEW;
        END;
        $fn$;
        CREATE TRIGGER customers_reference_integrity BEFORE INSERT OR UPDATE ON barang.customers FOR EACH ROW EXECUTE FUNCTION barang.customer_reference_integrity('customer_group_id','customer.group','customer_group_code','price_group_id','price.group','price_group_code');
        CREATE TRIGGER legal_customer_profiles_reference_integrity BEFORE INSERT OR UPDATE ON barang.legal_customer_profiles FOR EACH ROW EXECUTE FUNCTION barang.customer_reference_integrity('legal_entity_type_id','legal.entity.type','legal_entity_type_code');
        CREATE TRIGGER customer_financial_profiles_reference_integrity BEFORE INSERT OR UPDATE ON barang.customer_financial_profiles FOR EACH ROW EXECUTE FUNCTION barang.customer_reference_integrity('payment_term_id','payment.term','payment_term_code','payment_condition_id','payment.condition','payment_condition_code','preferred_payment_method_id','payment.method','preferred_payment_method_code','tax_status_id','tax.status','tax_status_code');
        CREATE TRIGGER customer_addresses_reference_integrity BEFORE INSERT OR UPDATE ON barang.customer_addresses FOR EACH ROW EXECUTE FUNCTION barang.customer_reference_integrity('address_type_id','address.type','address_type_code');
        CREATE TRIGGER customer_contacts_reference_integrity BEFORE INSERT OR UPDATE ON barang.customer_contacts FOR EACH ROW EXECUTE FUNCTION barang.customer_reference_integrity('contact_role_id','contact.role','contact_type_code');
        CREATE TRIGGER customer_bank_accounts_reference_integrity BEFORE INSERT OR UPDATE ON barang.customer_bank_accounts FOR EACH ROW EXECUTE FUNCTION barang.customer_reference_integrity('bank_id','bank','bank_code','account_type_id','bank.account.type','account_type_code');
        
        DO $grants$ BEGIN
         IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN
          GRANT DELETE ON barang.lookup_values,barang.lookup_value_translations,barang.customer_addresses,barang.customer_contacts,barang.customer_bank_accounts TO barang_app;
          GRANT UPDATE ON barang.customer_bank_accounts TO barang_app;
          GRANT DELETE ON barang.artwork_tags,barang.defect_types TO barang_app;
         END IF;
        END; $grants$;
        
        """;
}
