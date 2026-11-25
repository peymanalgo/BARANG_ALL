using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable CA1861 // EF migration column arrays are generated migration metadata.

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// فیچرهای ماژول‌ها («فیچرهای سیستم» — System Management): ردیف‌های اولیه barang.system_features
    /// برای همه شرکت‌های موجود — اسکوپ erp: ۱۷ ماژول پنل (design.bank، workflow.bpms، cartable، orders،
    /// pricing، inventory.purchasing، production، quality، logistics، customer.wallet، customer.feedback،
    /// management.dashboard، suppliers، equipment، shifts، products، b2c.management) و اسکوپ store:
    /// ۱۱ فیچر سایت پی‌رنگ (ai.design، referral، blog، concept.search، wizard.upload، wizard.text،
    /// wizard.gift، favorites، custom.designs، notifications، feedback). همه روشن (enabled=true).
    /// الگوی درج همان seed شناسه‌دار ai.assistant در AddSystemFeatures است (md5 روی شرکت+کد+اسکوپ)
    /// تا idempotent باشد و با مهاجرت‌های هم‌زمان تداخل نکند.
    /// </summary>
    public partial class AddModuleFeatureFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Seed فیچرهای ماژول‌های ERP (اسکوپ erp) برای هر شرکت موجود — همه روشن، idempotent
                INSERT INTO barang.system_features
                    (id, company_id, feature_code, scope, name_fa, name_en, name_ar, name_es,
                     description_fa, description_en, description_ar, description_es,
                     is_enabled, created_at_utc, updated_at_utc, version)
                SELECT md5(c.id::text || '-erp-' || f.code)::uuid,
                       c.id, f.code, 'erp',
                       f.fa, f.en, f.ar, f.es,
                       f.dfa, f.den, f.dar, f.des,
                       true, now(), now(), 1
                FROM barang.companies c
                CROSS JOIN (VALUES
                    ('design.bank',          N'بانک طرح',             N'Design bank',             N'بنك التصاميم',            N'Banco de diseños',
                                             N'بانک طرح‌های آماده چاپ', N'Ready-to-print design bank', N'بنك التصاميم الجاهزة للطباعة', N'Banco de diseños listos para imprimir'),
                    ('workflow.bpms',        N'گردش کار',             N'Workflow (BPMS)',         N'سير العملية (BPMS)',      N'Flujo de trabajo (BPMS)',
                                             N'موتور گردش کار و فرایندهای سازمانی', N'Workflow engine and business processes', N'محرک سير العملية والعمليات المؤسسية', N'Motor de flujos de trabajo y procesos de negocio'),
                    ('cartable',             N'کارتابل',              N'Cartable',                N'سلة المهام',              N'Bandeja de tareas',
                                             N'کارتابل مکاتبات و تصمیم‌ها', N'Correspondence and decision cartable', N'سلة المراسلات والقرارات', N'Bandeja de correspondencia y decisiones'),
                    ('orders',               N'سفارش‌ها',             N'Orders',                  N'الطلبات',                 N'Pedidos',
                                             N'مدیریت سفارش‌های چاپ',   N'Print order management',  N'إدارة طلبات الطباعة',     N'Gestión de pedidos de impresión'),
                    ('pricing',              N'قیمت‌گذاری',           N'Pricing',                 N'التسعير',                 N'Fijación de precios',
                                             N'قیمت‌گذاری و پیش‌فاکتورها', N'Pricing and quotations', N'التسعير وعروض الأسعار',  N'Tarifas y cotizaciones'),
                    ('inventory.purchasing', N'انبار و خرید',         N'Inventory & purchasing',  N'المستودع والمشتريات',     N'Inventario y compras',
                                             N'موجودی انبار و سفارش‌های خرید', N'Warehouse stock and purchase orders', N'مخزون المستودع وأوامر الشراء', N'Existencias de almacén y órdenes de compra'),
                    ('production',           N'تولید',                N'Production',              N'الإنتاج',                 N'Producción',
                                             N'دستور کارها و خط تولید', N'Production jobs and shop floor', N'أوامر العمل وخط الإنتاج', N'Órdenes de producción y planta'),
                    ('quality',              N'کیفیت',                N'Quality',                 N'الجودة',                  N'Calidad',
                                             N'کنترل کیفیت و نقص‌ها',   N'Quality control and defects', N'مراقبة الجودة والعيوب', N'Control de calidad y defectos'),
                    ('logistics',            N'لجستیک',               N'Logistics',               N'الخدمات اللوجستية',       N'Logística',
                                             N'بسته‌بندی و ارسال سفارش‌ها', N'Order packaging and delivery', N'تغليف الطلبات وتسليمها', N'Embalaje y entrega de pedidos'),
                    ('customer.wallet',      N'کیف پول مشتری',        N'Customer wallet',         N'محفظة العميل',            N'Billetera del cliente',
                                             N'اعتبار و تراکنش‌های مشتریان', N'Customer credit and transactions', N'رصيد العملاء والمعاملات', N'Crédito y transacciones de clientes'),
                    ('customer.feedback',    N'بازخورد مشتریان',      N'Customer feedback',       N'ملاحظات العملاء',         N'Opiniones de clientes',
                                             N'دیدگاه‌ها و پیشنهادهای مشتریان', N'Customer opinions and suggestions', N'آراء العملاء ومقترحاتهم', N'Opiniones y sugerencias de clientes'),
                    ('management.dashboard', N'داشبورد مدیریتی',      N'Management dashboard',    N'لوحة القيادة الإدارية',   N'Panel de gestión',
                                             N'شاخص‌های مدیریتی و تحلیلی', N'Management and analytics KPIs', N'مؤشرات الإدارة والتحليلات', N'Indicadores de gestión y analíticas'),
                    ('suppliers',            N'عرضه‌کنندگان',         N'Suppliers',               N'الموردون',                N'Proveedores',
                                             N'مدیریت عرضه‌کنندگان',    N'Supplier management',     N'إدارة الموردين',          N'Gestión de proveedores'),
                    ('equipment',            N'تجهیزات',              N'Equipment',               N'المعدات',                 N'Equipos',
                                             N'تجهیزات و ماشین‌آلات تولید', N'Production equipment and machines', N'معدات وآلات الإنتاج', N'Equipos y máquinas de producción'),
                    ('shifts',               N'شیفت‌ها',               N'Shifts',                  N'الورديات',                N'Turnos',
                                             N'شیفت‌های کاری و برنامه زمانی', N'Work shifts and schedules', N'ورديات العمل والجداول', N'Turnos de trabajo y horarios'),
                    ('products',             N'محصولات',              N'Products',                N'المنتجات',                N'Productos',
                                             N'محصولات و واریانت‌های چاپ', N'Products and print variants', N'المنتجات والخيارات', N'Productos y variantes de impresión'),
                    ('b2c.management',       N'مدیریت سایت B2C',      N'B2C site management',     N'إدارة موقع B2C',          N'Gestión del sitio B2C',
                                             N'مدیریت محتوای سایت فروشگاه', N'Storefront content management', N'إدارة محتوى المتجر الإلكتروني', N'Gestión de contenidos de la tienda')
                ) AS f(code, fa, en, ar, es, dfa, den, dar, des)
                WHERE NOT EXISTS (
                    SELECT 1 FROM barang.system_features sf
                    WHERE sf.company_id = c.id AND sf.feature_code = f.code AND sf.scope = 'erp');
                """);

            migrationBuilder.Sql("""
                -- Seed فیچرهای سایت پی‌رنگ (اسکوپ store) برای هر شرکت موجود — همه روشن، idempotent
                INSERT INTO barang.system_features
                    (id, company_id, feature_code, scope, name_fa, name_en, name_ar, name_es,
                     description_fa, description_en, description_ar, description_es,
                     is_enabled, created_at_utc, updated_at_utc, version)
                SELECT md5(c.id::text || '-store-' || f.code)::uuid,
                       c.id, f.code, 'store',
                       f.fa, f.en, f.ar, f.es,
                       f.dfa, f.den, f.dar, f.des,
                       true, now(), now(), 1
                FROM barang.companies c
                CROSS JOIN (VALUES
                    ('ai.design',      N'طراحی با هوش مصنوعی',   N'AI design generation',   N'التصميم بالذكاء الاصطناعي', N'Diseño con IA',
                                       N'ساخت طرح با پرامپت هوش مصنوعی در ویزارد', N'Generate designs from AI prompts in the wizard', N'إنشاء التصاميم من أوصاف الذكاء الاصطناعي', N'Crear diseños a partir de prompts de IA'),
                    ('referral',       N'دعوت از دوستان',        N'Referral program',       N'دعوة الأصدقاء',             N'Programa de referidos',
                                       N'کد دعوت و اعتبار هدیه برای معرفی دوستان', N'Invite code and gift credit for referrals', N'رمز الدعوة ورصيد الهدايا للإحالة', N'Código de invitación y crédito de regalo por referidos'),
                    ('blog',           N'وبلاگ',                 N'Blog',                   N'المدونة',                   N'Blog',
                                       N'مقالات وبلاگ سایت فروشگاه', N'Storefront blog articles', N'مقالات مدونة المتجر',  N'Artículos del blog de la tienda'),
                    ('concept.search', N'جستجوی مفهومی',         N'Concept search',         N'البحث المفاهيمي',           N'Búsqueda conceptual',
                                       N'جستجوی معنایی طرح‌ها در کاتالوگ', N'Semantic design search in the catalog', N'البحث الدلالي في كتالوج التصاميم', N'Búsqueda semántica en el catálogo'),
                    ('wizard.upload',  N'طرح خودم (آپلود)',      N'Upload own design',      N'رفع تصميمك الخاص',          N'Subir diseño propio',
                                       N'مسیر آپلود فایل طرح در ویزارد سفارش', N'Upload own design file in the order wizard', N'رفع ملف تصميمك في ويزارد الطلب', N'Subir archivo de diseño propio en el asistente'),
                    ('wizard.text',    N'متن و تایپوگرافی',      N'Text & typography',      N'نص وخط',                    N'Texto y tipografía',
                                       N'مسیر نوشتن متن و تایپوگرافی در ویزارد', N'Text and typography path in the order wizard', N'مسار النص والخط في ويزارد الطلب', N'Ruta de texto y tipografía en el asistente'),
                    ('wizard.gift',    N'بسته هدیه و ارسال برای دیگران', N'Gift options',   N'خيارات الهدايا',            N'Opciones de regalo',
                                       N'ارسال سفارش به گیرنده هدیه با کارت تبریک', N'Send orders as gifts with a message card', N'إرسال الطلبات كهدية مع بطاقة معايدة', N'Enviar pedidos como regalo con tarjeta'),
                    ('favorites',      N'علاقه‌مندی‌ها',          N'Favorites',              N'المفضلة',                   N'Favoritos',
                                       N'ذخیره طرح‌های مورد علاقه مشتری', N'Customer favorite designs', N'تصاميم العملاء المفضلة', N'Diseños favoritos del cliente'),
                    ('custom.designs', N'طرح‌های سفارشی',         N'Custom designs',         N'التصاميم المخصصة',          N'Diseños personalizados',
                                       N'آپلود و مدیریت طرح‌های سفارشی مشتری', N'Customer custom design uploads and management', N'رفع وإدارة تصاميم العملاء المخصصة', N'Carga y gestión de diseños personalizados'),
                    ('notifications',  N'اعلان‌ها',               N'Notifications',          N'الإشعارات',                 N'Notificaciones',
                                       N'اعلان‌های وضعیت سفارش در پنل مشتری', N'Order status notifications in the customer panel', N'إشعارات حالة الطلب في لوحة العميل', N'Notificaciones de estado en el panel del cliente'),
                    ('feedback',       N'دیدگاه‌ها و پیشنهادها',  N'Feedback',               N'الملاحظات والاقتراحات',     N'Comentarios',
                                       N'فرم دیدگاه و پیشنهاد در پنل مشتری', N'Feedback form in the customer panel', N'نموذج الملاحظات في لوحة العميل', N'Formulario de comentarios del cliente')
                ) AS f(code, fa, en, ar, es, dfa, den, dar, des)
                WHERE NOT EXISTS (
                    SELECT 1 FROM barang.system_features sf
                    WHERE sf.company_id = c.id AND sf.feature_code = f.code AND sf.scope = 'store');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM barang.system_features
                WHERE scope = 'erp' AND feature_code IN (
                    'design.bank', 'workflow.bpms', 'cartable', 'orders', 'pricing', 'inventory.purchasing',
                    'production', 'quality', 'logistics', 'customer.wallet', 'customer.feedback',
                    'management.dashboard', 'suppliers', 'equipment', 'shifts', 'products', 'b2c.management');

                DELETE FROM barang.system_features
                WHERE scope = 'store' AND feature_code IN (
                    'ai.design', 'referral', 'blog', 'concept.search', 'wizard.upload', 'wizard.text',
                    'wizard.gift', 'favorites', 'custom.designs', 'notifications', 'feedback');
                """);
        }
    }
}
