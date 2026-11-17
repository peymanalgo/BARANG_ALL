namespace BARANG.Core.Infrastructure.SiteContent;

// ─── محتوای اولیه CMS — معادل چیزی که الان به‌صورت ایستا در B2C هست ───
internal static class SiteSeedData
{
    // slug، عنوان fa/en/ar، بدنه fa/en/ar، placement، ستون فوتر، ترتیب
    internal static readonly (string Slug, string Fa, string En, string Ar, string BodyFa, string BodyEn, string BodyAr, string Placement, int? FooterColumn, int Order)[] Pages =
    [
        ("about", "درباره پی‌رنگ", "About Peyrang", "عن بي‌رنگ",
            "<h2>درباره پی‌رنگ خان</h2><p>پی‌رنگ خان خانه‌ی هزاران طرح آماده چاپ روی پوشاک و هدایای تبلیغاتی است. ما هنر را به صنعت چاپ نزدیک کرده‌ایم: از بانک طرح‌های متنوع گرفته تا طراحی اختصاصی خودتان، همه در چند کلیک روی تیشرت، پولوشرت و هودی شما حک می‌شود.</p><p>از انتخاب طرح تا تحویل سفارش، همه‌چیز آنلاین و شفاف پیگیری می‌شود.</p>",
            "<h2>About Peyrang Khan</h2><p>Peyrang Khan is home to thousands of ready-to-print designs for apparel and promotional gifts. From our rich design bank to your own custom artwork, everything is printed onto your t-shirt, polo or hoodie in a few clicks.</p><p>From picking a design to delivery, everything is online and fully trackable.</p>",
            "<h2>عن بي‌رنگ</h2><p>بي‌رنگ خان هوية آلاف التصاميم الجاهزة للطباعة على الملابس والهدايا الدعائية. من بنك التصاميم إلى تصميمك الخاص، كل شيء يُطبع على قميصك في بضع نقرات.</p>",
            "none", null, 5),
        ("contact", "تماس با ما", "Contact us", "اتصل بنا",
            "<h2>تماس با پی‌رنگ خان</h2><p>سوال یا پیشنهاد دارید؟ خوشحال می‌شویم بشنویم:</p><ul><li>ایمیل: salam@pirang.ir</li><li>تلفن: 021-00000000</li></ul><p>پاسخگوی شما در روزهای کاری ۹ تا ۱۸ هستیم.</p>",
            "<h2>Contact Peyrang Khan</h2><p>Questions or feedback? We would love to hear from you:</p><ul><li>Email: salam@pirang.ir</li><li>Phone: 021-00000000</li></ul><p>We answer on business days, 9:00–18:00.</p>",
            "<h2>اتصل ببي‌رنگ</h2><p>لديك سؤال أو اقتراح؟ يسعدنا سماعك: salam@pirang.ir — 021-00000000</p>",
            "none", null, 6),
        ("printing-packaging", "چاپ و بسته‌بندی", "Printing & packaging", "الطباعة والتغليف",
            "<h2>چاپ و بسته‌بندی پی‌رنگ خان</h2><p>چاپ با بالاترین کیفیت روی پوشاک در سه اندازه استاندارد A3، A4 و A5 و امکان چاپ جلو و پشت.</p><h3>روش‌های بسته‌بندی</h3><ul><li>بسته‌بندی استاندارد — مناسب استفاده روزمره</li><li>بسته هدیه — با کارت پیام شخصی برای هدیه دادن</li><li>جعبه لوکس — برای هدایای سازمانی و مناسبتی</li></ul><p>زمان ارسال: ۳ تا ۵ روز کاری.</p>",
            "<h2>Peyrang printing & packaging</h2><p>High-quality apparel printing in three standard sizes — A3, A4 and A5 — with front and back options.</p><h3>Packaging</h3><ul><li>Standard packaging — for everyday use</li><li>Gift wrap — with a personal message card</li><li>Luxury box — for corporate and special gifts</li></ul><p>Delivery time: 3–5 business days.</p>",
            "<h2>الطباعة والتغليف</h2><p>طباعة عالية الجودة بالمقاسات القياسية A3 وA4 وA5 مع خيار الأمام والظهر، وتغليف قياسي أو هدايا أو صندوق فاخر. مدة التوصيل: ٣ إلى ٥ أيام عمل.</p>",
            "none", null, 4),
    ];

    // نشان‌های مجوز نمونه — همان SVGهای ایستای فروشگاه؛ هنگام سیدر/تعمیر داخل MinIO بارگذاری می‌شوند
    // تا تصویر بندانگشتی فوتر در ERP و B2C هر دو از یک URL عمومی واقعی بیاید.
    // کلید = مسیر ایستای قبلی (که در دیتابیس‌های قدیمی داخل imageStorageKey مانده)؛ مقدار = محتوای SVG.
    internal static readonly IReadOnlyDictionary<string, string> LicenseBadgeSvgs = new Dictionary<string, string>
    {
        ["/images/license-badge-1.svg"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120" width="120" height="120">
              <rect x="4" y="4" width="112" height="112" rx="14" fill="#0f2b46" stroke="#f6b23c" stroke-width="2.5"/>
              <circle cx="60" cy="48" r="22" fill="none" stroke="#f6b23c" stroke-width="3"/>
              <path d="M49 48l8 8 15-16" fill="none" stroke="#f6b23c" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
              <text x="60" y="92" text-anchor="middle" font-family="Tahoma" font-size="13" fill="#f6b23c">مجوز نمونه ۱</text>
            </svg>
            """,
        ["/images/license-badge-2.svg"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120" width="120" height="120">
              <rect x="4" y="4" width="112" height="112" rx="14" fill="#123524" stroke="#8ff0c4" stroke-width="2.5"/>
              <circle cx="60" cy="48" r="22" fill="none" stroke="#8ff0c4" stroke-width="3"/>
              <path d="M49 48l8 8 15-16" fill="none" stroke="#8ff0c4" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
              <text x="60" y="92" text-anchor="middle" font-family="Tahoma" font-size="13" fill="#8ff0c4">مجوز نمونه ۲</text>
            </svg>
            """,
        ["/images/license-badge-3.svg"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120" width="120" height="120">
              <rect x="4" y="4" width="112" height="112" rx="14" fill="#33130f" stroke="#f09a7a" stroke-width="2.5"/>
              <circle cx="60" cy="48" r="22" fill="none" stroke="#f09a7a" stroke-width="3"/>
              <path d="M49 48l8 8 15-16" fill="none" stroke="#f09a7a" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
              <text x="60" y="92" text-anchor="middle" font-family="Tahoma" font-size="13" fill="#f09a7a">مجوز نمونه ۳</text>
            </svg>
            """,
        ["/images/license-badge-4.svg"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120" width="120" height="120">
              <rect x="4" y="4" width="112" height="112" rx="14" fill="#221336" stroke="#c4a5ff" stroke-width="2.5"/>
              <circle cx="60" cy="48" r="22" fill="none" stroke="#c4a5ff" stroke-width="3"/>
              <path d="M49 48l8 8 15-16" fill="none" stroke="#c4a5ff" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
              <text x="60" y="92" text-anchor="middle" font-family="Tahoma" font-size="13" fill="#c4a5ff">مجوز نمونه ۴</text>
            </svg>
            """,
    };

    // نوع، صفحه ارجاعی، عنوان fa/en/ar، متن، url، کلید تصویر، ستون، ترتیب
    internal static readonly (string Type, string? PageSlug, string? TitleFa, string? TitleEn, string? TitleAr, string? Text, string? Url, string? ImageKey, int Column, int Order)[] FooterWidgets =
    [
        ("text", null, null, null, null, "طرح‌های آماده چاپ روی پوشاک و هدایای تبلیغاتی؛ سفارش آنلاین، پیگیری تا تحویل.\nپی‌رنگ خان، نقشه‌ها داره برات!", null, null, 1, 1),
        ("menu", null, "همه طرح‌ها", "All designs", "كل التصاميم", null, "/catalog", null, 2, 1),
        ("menu", null, "پیگیری سفارش", "Track order", "تتبع الطلب", null, "/account/orders", null, 2, 2),
        ("menu", null, "سوالات متداول", "FAQ", "الأسئلة الشائعة", null, "/faq", null, 2, 3),
        ("menu", null, "حساب کاربری", "My account", "حسابي", null, "/account", null, 2, 4),
        ("menu", null, "021-00000000", "021-00000000", "021-00000000", null, "tel:02100000000", null, 2, 5),
        ("menu", null, "salam@pirang.ir", "salam@pirang.ir", "salam@pirang.ir", null, "mailto:salam@pirang.ir", null, 3, 1),
        ("page", "about", null, null, null, null, null, null, 3, 2),
        ("image", null, "مجوز نمونه ۱", "Sample license 1", "ترخيص نموذجي ١", null, null, "/images/license-badge-1.svg", 4, 1),
        ("image", null, "مجوز نمونه ۲", "Sample license 2", "ترخيص نموذجي ٢", null, null, "/images/license-badge-2.svg", 4, 2),
        ("image", null, "مجوز نمونه ۳", "Sample license 3", "ترخيص نموذجي ٣", null, null, "/images/license-badge-3.svg", 4, 3),
        ("image", null, "مجوز نمونه ۴", "Sample license 4", "ترخيص نموذجي ٤", null, null, "/images/license-badge-4.svg", 4, 4),
    ];

    // نوع، صفحه ارجاعی، url، عنوان fa/en/ar (برای custom)، pinned
    internal static readonly (string Type, string? PageSlug, string? Url, string? TitleFa, string? TitleEn, string? TitleAr, bool Pinned)[] MenuItems =
    [
        ("custom", null, "/", "خانه", "Home", "الرئيسية", false),
        ("custom", null, "/order/wizard", "شروع طراحی تیشرت", "Start designing", "ابدأ التصميم", false),
        ("custom", null, "/catalog", "طرح‌ها", "Designs", "التصاميم", false),
        ("page", "printing-packaging", null, null, null, null, false),
        ("page", "about", null, null, null, null, false),
        ("page", "contact", null, null, null, null, false),
        ("blog", null, "/blog", "وبلاگ", "Blog", "المدونة", true),
    ];
}
