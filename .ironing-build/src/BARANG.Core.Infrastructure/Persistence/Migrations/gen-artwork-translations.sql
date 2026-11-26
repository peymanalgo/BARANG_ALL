-- One-off: generate fa/en/ar title+description for every artwork from its
-- category's localized names + the stock number in its title/code.
-- Idempotent: only fills missing cultures.
DO $gen$
DECLARE
    cid uuid;
    aid uuid;
    digits text;
    cat_fa text; cat_en text; cat_ar text;
    base_title text;
    r record;
BEGIN
    FOR cid IN SELECT id FROM barang.companies LOOP
        PERFORM set_config('barang.company_id', cid::text, false);

        FOR r IN
            SELECT a.id, a.title, a.code,
                   MAX(tr.display_name) FILTER (WHERE tr.culture='fa') AS c_fa,
                   MAX(tr.display_name) FILTER (WHERE tr.culture='en') AS c_en,
                   MAX(tr.display_name) FILTER (WHERE tr.culture='ar') AS c_ar
            FROM barang.artworks a
            LEFT JOIN barang.lookup_values lv ON lv.code = a.category_code
            LEFT JOIN barang.lookup_types lt ON lt.id = lv.lookup_type_id AND lt.code = 'artwork.category' AND lt.company_id IS NOT DISTINCT FROM a.company_id
            LEFT JOIN barang.lookup_value_translations tr ON tr.lookup_value_id = lv.id
            WHERE a.company_id = cid
            GROUP BY a.id, a.title, a.code
        LOOP
            aid := r.id;
            base_title := r.title;
            -- stock digits: longest digit run in title, else from code
            digits := COALESCE(
                (SELECT (regexp_matches(r.title, '(\d{5,})', 'g'))[1] LIMIT 1),
                (SELECT (regexp_matches(r.code, '(\d{5,})', 'g'))[1] LIMIT 1),
                substring(r.code from 1 for 8));
            cat_fa := COALESCE(NULLIF(r.c_fa, ''), base_title);
            cat_en := COALESCE(NULLIF(r.c_en, ''), base_title);
            cat_ar := COALESCE(NULLIF(r.c_ar, ''), base_title);

            -- fa
            INSERT INTO barang.artwork_translations(id,company_id,artwork_id,culture,title,description,created_at_utc,updated_at_utc,version)
            VALUES (gen_random_uuid(), cid, aid, 'fa',
                    cat_fa || ' — طرح ' || digits,
                    'طرح ' || cat_fa || ' آمادهٔ چاپ روی تیشرت و پوشاک؛ مناسب برای سفارش‌های تک و تیراژ.',
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
            ON CONFLICT (artwork_id, culture) DO NOTHING;

            -- en
            INSERT INTO barang.artwork_translations(id,company_id,artwork_id,culture,title,description,created_at_utc,updated_at_utc,version)
            VALUES (gen_random_uuid(), cid, aid, 'en',
                    cat_en || ' design — ' || digits,
                    'Print-ready ' || cat_en || ' design for t-shirts and apparel.',
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
            ON CONFLICT (artwork_id, culture) DO NOTHING;

            -- ar
            INSERT INTO barang.artwork_translations(id,company_id,artwork_id,culture,title,description,created_at_utc,updated_at_utc,version)
            VALUES (gen_random_uuid(), cid, aid, 'ar',
                    cat_ar || ' — تصميم ' || digits,
                    'تصميم ' || cat_ar || ' جاهز للطباعة على التيشرت والملابس.',
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
            ON CONFLICT (artwork_id, culture) DO NOTHING;
        END LOOP;
    END LOOP;
    PERFORM set_config('barang.company_id', '', false);
END
$gen$;
