namespace BARANG.Core.Domain.Lookups;

public sealed record DomainReferenceDefinition(string Code, string[] Domains, string English, string Persian, string Arabic, LookupScope Scope)
{
    public string Caption(string culture) => culture.Split('-')[0].ToLowerInvariant() switch { "en" => English, "ar" => Arabic, _ => Persian };
}
public sealed record ReferenceDomainDefinition(string Code, string English, string Persian, string Arabic)
{
    public string Caption(string culture) => culture.Split('-')[0].ToLowerInvariant() switch { "en" => English, "ar" => Arabic, _ => Persian };
}

/// <summary>Audited configurable catalogs only. Entity relationships and system enums are deliberately excluded.</summary>
public static class DomainReferenceCatalog
{
    public static IReadOnlyList<ReferenceDomainDefinition> Domains { get; } =
    [
        new("shared", "Shared Basic Data", "داده‌های پایه مشترک", "البيانات الأساسية المشتركة"),
        new("customers", "Customer Reference Data", "داده‌های پایه مشتریان", "البيانات المرجعية للعملاء"),
        new("suppliers", "Supplier Reference Data", "داده‌های پایه تأمین‌کنندگان", "البيانات المرجعية للموردين"),
        new("products", "Product Reference Data", "داده‌های پایه محصولات", "البيانات المرجعية للمنتجات"),
        new("artwork", "Design Bank Reference Data", "داده‌های پایه بانک طرح", "البيانات المرجعية لبنك التصاميم"),
        new("orders", "Orders Reference Data", "داده‌های پایه سفارشات", "البيانات المرجعية للطلبات"),
        new("pricing", "Pricing Reference Data", "داده‌های پایه قیمت‌گذاری", "البيانات المرجعية للتسعير"),
        new("finance", "Pricing & Finance Reference Data", "داده‌های پایه قیمت‌گذاری و مالی", "البيانات المرجعية للتسعير والمالية"),
        new("inventory", "Inventory & Purchasing Reference Data", "داده‌های پایه موجودی و خرید", "البيانات المرجعية للمخزون والمشتريات"),
        new("equipment", "Equipment Reference Data", "داده‌های پایه تجهیزات", "البيانات المرجعية للمعدات"),
        new("production", "Design, Print & Heat Press Reference Data", "داده‌های پایه طراحی و چاپ و پرس", "البيانات المرجعية للتصميم والطباعة والكبس الحراري"),
        new("quality", "Quality Control & Reproduction Reference Data", "داده‌های پایه کنترل کیفی و بازتولید", "البيانات المرجعية للجودة وإعادة الإنتاج"),
        new("logistics", "Packaging, Shipping & Delivery Reference Data", "داده‌های پایه بسته‌بندی و ارسال و تحویل", "البيانات المرجعية للتغليف والشحن والتسليم"),
        new("workflow", "Inbox & Workflow Reference Data", "داده‌های پایه کارتابل و گردش کار", "البيانات المرجعية للمهام وسير العمل")
    ];
    public static IReadOnlyList<DomainReferenceDefinition> Types { get; } =
    [
        new("supplier.category", ["suppliers"], "Supplier Categories", "دسته‌های تأمین‌کننده", "فئات الموردين", LookupScope.Company),
        new("supplier.grade", ["suppliers"], "Supplier Grades", "رتبه‌های تأمین‌کننده", "درجات الموردين", LookupScope.Company),
        new("freight.delivery.term", ["suppliers"], "Freight / Delivery Terms", "شرایط حمل و تحویل", "شروط النقل والتسليم", LookupScope.Company),
        new("supplier.hold.reason", ["suppliers"], "Supplier Hold Reasons", "دلایل مسدودی تأمین‌کننده", "أسباب حظر الموردين", LookupScope.Company),
        new("customer.group", ["customers"], "Customer Groups", "گروه‌های مشتری", "مجموعات العملاء", LookupScope.Company),
        new("price.group", ["shared"], "Price Groups", "گروه‌های قیمت", "مجموعات الأسعار", LookupScope.Company),
        new("legal.entity.type", ["shared"], "Legal Entity Types", "انواع شخصیت حقوقی", "أنواع الكيانات القانونية", LookupScope.Company),
        new("payment.term", ["shared"], "Payment Terms", "مهلت‌های پرداخت", "آجال الدفع", LookupScope.Company),
        new("payment.condition", ["shared"], "Payment Conditions", "شرایط پرداخت", "شروط الدفع", LookupScope.Company),
        new("payment.method", ["shared"], "Payment Methods", "روش‌های پرداخت", "طرق الدفع", LookupScope.Global),
        new("tax.status", ["shared"], "Tax Statuses", "وضعیت‌های مالیاتی", "الحالات الضريبية", LookupScope.Company),
        new("address.type", ["shared"], "Address Types", "انواع نشانی", "أنواع العناوين", LookupScope.Company),
        new("contact.role", ["shared"], "Contact Roles", "نقش‌های مخاطب", "أدوار جهات الاتصال", LookupScope.Company),
        new("bank", ["shared"], "Banks", "بانک‌ها", "البنوك", LookupScope.Global),
        new("bank.account.type", ["shared"], "Bank Account Types", "انواع حساب بانکی", "أنواع الحسابات البنكية", LookupScope.Company),
        new("product.type", ["products"], "Product Types", "انواع محصول", "أنواع المنتجات", LookupScope.Company),
        new("product.size", ["products"], "Sizes", "اندازه‌ها", "المقاسات", LookupScope.Company),
        new("product.color", ["products"], "Colors", "رنگ‌ها", "الألوان", LookupScope.Company),
        new("unit.of.measure", ["shared"], "Units of Measure", "واحدهای اندازه‌گیری", "وحدات القياس", LookupScope.Company),
        new("product.brand", ["products"], "Brands", "برندها", "العلامات التجارية", LookupScope.Company),
        new("product.material", ["products"], "Materials / Fabrics", "مواد و پارچه‌ها", "المواد والأقمشة", LookupScope.Company),
        new("product.gender", ["products"], "Apparel Gender Classifications", "گروه‌های جنسیتی پوشاک", "تصنيفات جنس الملابس", LookupScope.Company),
        new("product.fit", ["products"], "Fits", "قالب‌های پوشاک", "قصات الملابس", LookupScope.Company),
        new("product.style", ["products"], "Styles", "سبک‌های محصول", "أنماط المنتجات", LookupScope.Company),
        new("artwork.category", ["artwork"], "Artwork Categories", "دسته‌بندی طرح‌ها", "فئات التصاميم", LookupScope.Company),
        new("artwork.subcategory", ["artwork"], "Design Subcategories", "زیرگروه‌های طرح", "الفئات الفرعية للتصاميم", LookupScope.Company),
        new("artwork.type", ["artwork"], "Design Types", "انواع طرح", "أنواع التصاميم", LookupScope.Company),
        new("artwork.style", ["artwork"], "Design Styles", "سبک‌های طرح", "أنماط التصاميم", LookupScope.Company),
        new("artwork.theme", ["artwork"], "Design Themes", "موضوعات طرح", "موضوعات التصاميم", LookupScope.Company),
        new("artwork.audience", ["artwork"], "Design Audiences", "مخاطبان طرح", "جمهور التصاميم", LookupScope.Company),
        new("artwork.license", ["artwork"], "Design License Types", "انواع مجوز طرح", "أنواع تراخيص التصميم", LookupScope.Company),
        new("artwork.usage", ["artwork"], "Design Usage Types", "انواع کاربرد طرح", "أنواع استخدام التصميم", LookupScope.Company),
        new("order.type", ["orders"], "Order Types", "انواع سفارش", "أنواع الطلبات", LookupScope.Company),
        new("inventory.adjustment.reason", ["inventory"], "Inventory Adjustment Reasons", "دلایل تعدیل موجودی", "أسباب تسوية المخزون", LookupScope.Company),
        // `inventory` is retained as a compatibility alias for the existing route;
        // equipment is the owning master-data domain and the canonical consumer.
        new("equipment.type", ["equipment", "inventory"], "Equipment Types", "انواع تجهیزات", "أنواع المعدات", LookupScope.Company),
        new("equipment.status", ["equipment", "inventory"], "Equipment Operational Statuses", "وضعیت‌های عملیاتی تجهیزات", "الحالات التشغيلية للمعدات", LookupScope.Company),
        new("print.method", ["production"], "Print Methods", "روش‌های چاپ", "طرق الطباعة", LookupScope.Company),
        new("print.direction", ["production"], "Print Directions", "جهت‌های چاپ", "اتجاهات الطباعة", LookupScope.Company),
        new("print.size", ["production"], "Print Sizes", "اندازه‌های چاپ", "مقاسات الطباعة", LookupScope.Company),
        new("defect.category", ["quality"], "Defect Categories", "دسته‌بندی عیوب", "فئات العيوب", LookupScope.Company),
        new("packaging.type", ["shared"], "Packaging Types", "انواع بسته‌بندی", "أنواع التغليف", LookupScope.Company),
        new("shipping.method", ["shared"], "Shipping Methods", "روش‌های ارسال", "طرق الشحن", LookupScope.Company),
        new("shipping.carrier", ["logistics"], "Carriers", "حمل‌کنندگان", "شركات النقل", LookupScope.Company)
    ];
    // Existing structured entities use adapters, never duplicate lookup tables.
    public static IReadOnlyList<DomainReferenceDefinition> Adapters { get; } =
    [
        new("artwork.tag", ["artwork"], "Artwork Tags", "برچسب‌های طرح", "وسوم التصاميم", LookupScope.Company),
        new("defect.type", ["quality"], "Defect Types", "انواع عیب", "أنواع العيوب", LookupScope.Company)
    ];
    public static DomainReferenceDefinition? Find(string code) => Types.FirstOrDefault(x => x.Code == code);
}
