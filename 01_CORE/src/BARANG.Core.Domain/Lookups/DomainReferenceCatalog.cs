namespace BARANG.Core.Domain.Lookups;

public sealed record DomainReferenceDefinition(string Code, string[] Domains, string English, string Persian, string Arabic, string Spanish, LookupScope Scope)
{
    public string Caption(string culture) => culture.Split('-')[0].ToLowerInvariant() switch { "en" => English, "ar" => Arabic, "es" => Spanish, _ => Persian };
}
public sealed record ReferenceDomainDefinition(string Code, string English, string Persian, string Arabic, string Spanish)
{
    public string Caption(string culture) => culture.Split('-')[0].ToLowerInvariant() switch { "en" => English, "ar" => Arabic, "es" => Spanish, _ => Persian };
}

/// <summary>Audited configurable catalogs only. Entity relationships and system enums are deliberately excluded.</summary>
public static class DomainReferenceCatalog
{
    public static IReadOnlyList<ReferenceDomainDefinition> Domains { get; } =
    [
        new("shared", "Shared Basic Data", "داده‌های پایه مشترک", "البيانات الأساسية المشتركة", "Datos básicos compartidos"),
        new("customers", "Customer Reference Data", "داده‌های پایه مشتریان", "البيانات المرجعية للعملاء", "Datos maestros de clientes"),
        new("suppliers", "Supplier Reference Data", "داده‌های پایه تأمین‌کنندگان", "البيانات المرجعية للموردين", "Datos maestros de proveedores"),
        new("products", "Product Reference Data", "داده‌های پایه محصولات", "البيانات المرجعية للمنتجات", "Datos maestros de productos"),
        new("artwork", "Design Bank Reference Data", "داده‌های پایه بانک طرح", "البيانات المرجعية لبنك التصاميم", "Datos maestros del banco de diseños"),
        new("orders", "Orders Reference Data", "داده‌های پایه سفارشات", "البيانات المرجعية للطلبات", "Datos maestros de pedidos"),
        new("pricing", "Pricing Reference Data", "داده‌های پایه قیمت‌گذاری", "البيانات المرجعية للتسعير", "Datos maestros de precios"),
        new("finance", "Pricing & Finance Reference Data", "داده‌های پایه قیمت‌گذاری و مالی", "البيانات المرجعية للتسعير والمالية", "Datos maestros de precios y finanzas"),
        new("inventory", "Inventory & Purchasing Reference Data", "داده‌های پایه موجودی و خرید", "البيانات المرجعية للمخزون والمشتريات", "Datos maestros de inventario y compras"),
        new("equipment", "Equipment Reference Data", "داده‌های پایه تجهیزات", "البيانات المرجعية للمعدات", "Datos maestros de equipos"),
        new("production", "Design, Print & Heat Press Reference Data", "داده‌های پایه طراحی و چاپ و پرس", "البيانات المرجعية للتصميم والطباعة والكبس الحراري", "Datos maestros de diseño, impresión y prensa térmica"),
        new("quality", "Quality Control & Reproduction Reference Data", "داده‌های پایه کنترل کیفی و بازتولید", "البيانات المرجعية للجودة وإعادة الإنتاج", "Datos maestros de control de calidad y reproducción"),
        new("logistics", "Packaging, Shipping & Delivery Reference Data", "داده‌های پایه بسته‌بندی و ارسال و تحویل", "البيانات المرجعية للتغليف والشحن والتسليم", "Datos maestros de embalaje, envío y entrega"),
        new("workflow", "Inbox & Workflow Reference Data", "داده‌های پایه کارتابل و گردش کار", "البيانات المرجعية للمهام وسير العمل", "Datos maestros de bandeja de tareas y flujo de trabajo")
    ];
    public static IReadOnlyList<DomainReferenceDefinition> Types { get; } =
    [
        new("supplier.category", ["suppliers"], "Supplier Categories", "دسته‌های تأمین‌کننده", "فئات الموردين", "Categorías de proveedores", LookupScope.Company),
        new("supplier.grade", ["suppliers"], "Supplier Grades", "رتبه‌های تأمین‌کننده", "درجات الموردين", "Niveles de proveedores", LookupScope.Company),
        new("freight.delivery.term", ["suppliers"], "Freight / Delivery Terms", "شرایط حمل و تحویل", "شروط النقل والتسليم", "Condiciones de transporte y entrega", LookupScope.Company),
        new("supplier.hold.reason", ["suppliers"], "Supplier Hold Reasons", "دلایل مسدودی تأمین‌کننده", "أسباب حظر الموردين", "Motivos de bloqueo de proveedores", LookupScope.Company),
        new("customer.group", ["customers"], "Customer Groups", "گروه‌های مشتری", "مجموعات العملاء", "Grupos de clientes", LookupScope.Company),
        new("price.group", ["shared"], "Price Groups", "گروه‌های قیمت", "مجموعات الأسعار", "Grupos de precios", LookupScope.Company),
        new("legal.entity.type", ["shared"], "Legal Entity Types", "انواع شخصیت حقوقی", "أنواع الكيانات القانونية", "Tipos de entidad jurídica", LookupScope.Company),
        new("payment.term", ["shared"], "Payment Terms", "مهلت‌های پرداخت", "آجال الدفع", "Plazos de pago", LookupScope.Company),
        new("payment.condition", ["shared"], "Payment Conditions", "شرایط پرداخت", "شروط الدفع", "Condiciones de pago", LookupScope.Company),
        new("payment.method", ["shared"], "Payment Methods", "روش‌های پرداخت", "طرق الدفع", "Métodos de pago", LookupScope.Global),
        new("tax.status", ["shared"], "Tax Statuses", "وضعیت‌های مالیاتی", "الحالات الضريبية", "Estados fiscales", LookupScope.Company),
        new("address.type", ["shared"], "Address Types", "انواع نشانی", "أنواع العناوين", "Tipos de dirección", LookupScope.Company),
        new("contact.role", ["shared"], "Contact Roles", "نقش‌های مخاطب", "أدوار جهات الاتصال", "Roles de contacto", LookupScope.Company),
        new("bank", ["shared"], "Banks", "بانک‌ها", "البنوك", "Bancos", LookupScope.Global),
        new("bank.account.type", ["shared"], "Bank Account Types", "انواع حساب بانکی", "أنواع الحسابات البنكية", "Tipos de cuenta bancaria", LookupScope.Company),
        new("product.type", ["products"], "Product Types", "انواع محصول", "أنواع المنتجات", "Tipos de producto", LookupScope.Company),
        new("product.size", ["products"], "Sizes", "اندازه‌ها", "المقاسات", "Tallas", LookupScope.Company),
        new("product.color", ["products"], "Colors", "رنگ‌ها", "الألوان", "Colores", LookupScope.Company),
        new("unit.of.measure", ["shared"], "Units of Measure", "واحدهای اندازه‌گیری", "وحدات القياس", "Unidades de medida", LookupScope.Company),
        new("product.brand", ["products"], "Brands", "برندها", "العلامات التجارية", "Marcas", LookupScope.Company),
        new("product.material", ["products"], "Materials / Fabrics", "مواد و پارچه‌ها", "المواد والأقمشة", "Materiales y telas", LookupScope.Company),
        new("product.gender", ["products"], "Apparel Gender Classifications", "گروه‌های جنسیتی پوشاک", "تصنيفات جنس الملابس", "Clasificaciones de género de ropa", LookupScope.Company),
        new("product.fit", ["products"], "Fits", "قالب‌های پوشاک", "قصات الملابس", "Cortes de ropa", LookupScope.Company),
        new("product.style", ["products"], "Styles", "سبک‌های محصول", "أنماط المنتجات", "Estilos de producto", LookupScope.Company),
        new("artwork.category", ["artwork"], "Artwork Categories", "دسته‌بندی طرح‌ها", "فئات التصاميم", "Categorías de diseños", LookupScope.Company),
        new("artwork.subcategory", ["artwork"], "Design Subcategories", "زیرگروه‌های طرح", "الفئات الفرعية للتصاميم", "Subcategorías de diseños", LookupScope.Company),
        new("artwork.type", ["artwork"], "Design Types", "انواع طرح", "أنواع التصاميم", "Tipos de diseño", LookupScope.Company),
        new("artwork.style", ["artwork"], "Design Styles", "سبک‌های طرح", "أنماط التصاميم", "Estilos de diseño", LookupScope.Company),
        new("artwork.theme", ["artwork"], "Design Themes", "موضوعات طرح", "موضوعات التصاميم", "Temas de diseño", LookupScope.Company),
        new("artwork.audience", ["artwork"], "Design Audiences", "مخاطبان طرح", "جمهور التصاميم", "Públicos de diseño", LookupScope.Company),
        new("artwork.license", ["artwork"], "Design License Types", "انواع مجوز طرح", "أنواع تراخيص التصميم", "Tipos de licencia de diseño", LookupScope.Company),
        new("artwork.usage", ["artwork"], "Design Usage Types", "انواع کاربرد طرح", "أنواع استخدام التصميم", "Tipos de uso de diseño", LookupScope.Company),
        new("order.type", ["orders"], "Order Types", "انواع سفارش", "أنواع الطلبات", "Tipos de pedido", LookupScope.Company),
        new("inventory.adjustment.reason", ["inventory"], "Inventory Adjustment Reasons", "دلایل تعدیل موجودی", "أسباب تسوية المخزون", "Motivos de ajuste de inventario", LookupScope.Company),
        // `inventory` is retained as a compatibility alias for the existing route;
        // equipment is the owning master-data domain and the canonical consumer.
        new("equipment.type", ["equipment", "inventory"], "Equipment Types", "انواع تجهیزات", "أنواع المعدات", "Tipos de equipo", LookupScope.Company),
        new("equipment.status", ["equipment", "inventory"], "Equipment Operational Statuses", "وضعیت‌های عملیاتی تجهیزات", "الحالات التشغيلية للمعدات", "Estados operativos de equipos", LookupScope.Company),
        new("print.method", ["production"], "Print Methods", "روش‌های چاپ", "طرق الطباعة", "Métodos de impresión", LookupScope.Company),
        new("print.direction", ["production"], "Print Directions", "جهت‌های چاپ", "اتجاهات الطباعة", "Direcciones de impresión", LookupScope.Company),
        new("print.size", ["production"], "Print Sizes", "اندازه‌های چاپ", "مقاسات الطباعة", "Tamaños de impresión", LookupScope.Company),
        new("defect.category", ["quality"], "Defect Categories", "دسته‌بندی عیوب", "فئات العيوب", "Categorías de defectos", LookupScope.Company),
        new("packaging.type", ["shared"], "Packaging Types", "انواع بسته‌بندی", "أنواع التغليف", "Tipos de embalaje", LookupScope.Company),
        new("shipping.method", ["shared"], "Shipping Methods", "روش‌های ارسال", "طرق الشحن", "Métodos de envío", LookupScope.Company),
        new("shipping.carrier", ["logistics"], "Carriers", "حمل‌کنندگان", "شركات النقل", "Transportistas", LookupScope.Company)
    ];
    // Existing structured entities use adapters, never duplicate lookup tables.
    public static IReadOnlyList<DomainReferenceDefinition> Adapters { get; } =
    [
        new("artwork.tag", ["artwork"], "Artwork Tags", "برچسب‌های طرح", "وسوم التصاميم", "Etiquetas de diseño", LookupScope.Company),
        new("defect.type", ["quality"], "Defect Types", "انواع عیب", "أنواع العيوب", "Tipos de defecto", LookupScope.Company)
    ];
    public static DomainReferenceDefinition? Find(string code) => Types.FirstOrDefault(x => x.Code == code);
}
