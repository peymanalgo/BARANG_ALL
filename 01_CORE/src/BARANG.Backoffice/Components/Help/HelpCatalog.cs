namespace BARANG.Backoffice.Components.Help;

public sealed record HelpNode(
    string Id,
    string Fa, string En, string Ar,
    string? Route,
    string FaDesc, string EnDesc, string ArDesc,
    IReadOnlyList<HelpNode>? Children = null)
{
    public string Name(string culture) => culture switch
    {
        "en" => En,
        "ar" => Ar,
        _ => Fa
    };

    public string Description(string culture) => culture switch
    {
        "en" => EnDesc,
        "ar" => ArDesc,
        _ => FaDesc
    };
}

public static class HelpCatalog
{
    public static readonly IReadOnlyList<HelpNode> Roots =
    [
        new("home", "خانه", "Home", "الرئيسية", "/", "صفحه شخصی کاربر؛ خلاصه کارهای نیازمند اقدام شما، آخرین فعالیت‌های ثبت‌شده، اعلان‌های اخیر و دسترسی سریع به داده‌های پایه. نقطه شروع روزکاری است و هر ورود به سیستم ابتدا اینجا فرود می‌آید.",
            "The user's personal landing page: a summary of items awaiting your action, recent activity, latest notifications and quick links to reference data. It is the daily starting point where every sign-in lands.",
            "الصفحة الشخصية للمستخدم: ملخص العناصر التي تنتظر إجراءك والأنشطة الأخيرة وأحدث الإشعارات وروابط سريعة إلى البيانات المرجعية. إنها نقطة البداية اليومية التي تهبط فيها عند كل تسجيل دخول.", null),

        Group("master", "داده‌های پایه", "Reference Data", "البيانات المرجعية", null,
            "مخزن مشترک تعاریف اولیه سازمان: هر «نوع» و «کد»ی که در سفارش‌ها، قیمت‌گذاری، انبار و تولید انتخاب می‌شود از اینجا تعریف می‌گردد. داده پایه تمیز یعنی گزارش‌ها و محاسبات درست؛ این بخش را قبل از شروع عملیات روزمره تکمیل کنید.",
            "The organization's shared repository of core definitions: every type and code selected in orders, pricing, inventory and production is defined here. Clean master data means correct reports and calculations; complete this section before daily operations.",
            "مستودع مشترك لتعريفات المؤسسة الأساسية: كل نوع ورمز يتم اختياره في الطلبات والتسعير والمخازن والإنتاج يُعرَّف من هنا. البيانات المرجعية النظيفة تعني تقارير وحسابات صحيحة؛ أكمل هذا القسم قبل العمليات اليومية.", [
            Group("ref-shared", "مشترک", "Shared", "مشتركة", "/master/reference-data/shared",
                "تعاریفی که چند بخش مختلف از آن استفاده می‌کنند: پول، پرداخت، بانک، اندازه‌گیری و ارسال. یک‌بار اینجا تعریف می‌شود و در همه ماژول‌ها (سفارش، قیمت‌گذاری، لجستیک) به‌صورت لیست مشترک ظاهر می‌شود.",
                "Definitions used across several modules: currency, payments, banks, units of measure and shipping. Defined once here, they appear as shared pick-lists in orders, pricing and logistics.",
                "تعريفات تستخدمها عدة وحدات: العملة والدفع والبنوك ووحدات القياس والشحن. تُعرَّف مرة واحدة هنا وتظهر كقوائم مشتركة في الطلبات والتسعير واللوجستيات.", [
                Leaf("shared/price.group", "گروه‌های قیمت", "Price Groups", "مجموعات الأسعار", "سطوح قیمتی مشتریان (عمده، خرده، VIP). در موتور قیمت‌گذاری مبنای اعمال تعرفه و تخفیف پلکانی است.",
                    "Customer price tiers (wholesale, retail, VIP). The pricing engine uses them to apply tiered tariffs and discounts.",
                    "مستويات أسعار العملاء (جملة، تجزئة، مميز). يستخدمها محرك التسعير لتطبيق التعريفات والخصومات المتدرجة."),
                Leaf("shared/legal.entity.type", "انواع شخصیت حقوقی", "Legal Entity Types", "أنواع الكيان القانوني", "تعیین می‌کند مشتری/طرف حساب حقیقی است یا حقوقی؛ بر فیلدهای الزامی (شناسه ملی/کد اقتصادی) و اسناد مالی اثر دارد.",
                    "Distinguishes individual vs. corporate accounts; drives mandatory fields (national ID, economic code) and financial documents.",
                    "يميز بين الحسابات الفردية والشركات؛ يحدد الحقول الإلزامية (الرقم الوطني، الرمز الاقتصادي) والمستندات المالية."),
                Leaf("shared/payment.term", "مهلت‌های پرداخت", "Payment Terms", "شروط السداد", "شرایط نسیه (مثلاً ۳۰ روزه) که روی فاکتور اعمال می‌شود و مبناي محاسبه سررسید وصول در مالی است.",
                    "Credit terms (e.g. net 30) applied to invoices; the basis for due-date and collection tracking in finance.",
                    "شروط الائتمان (مثل 30 يوماً) تُطبق على الفواتير؛ وهي أساس حساب تاريخ الاستحقاق في المالية."),
                Leaf("shared/payment.condition", "شرایط پرداخت", "Payment Conditions", "شروط الدفع", "الگوی پرداخت معامله: پیش‌پرداخت، در هنگام تحویل، تقسیط. در سفارش و فاکتور انتخاب می‌شود.",
                    "The deal's payment pattern: advance, on delivery, installments. Selected on orders and invoices.",
                    "نمط الدفع للصفقة: دفعة مقدمة، عند التسليم، أقساط. يُختار في الطلبات والفواتير."),
                Leaf("shared/payment.method", "روش‌های پرداخت", "Payment Methods", "طرق الدفع", "کانال وصول پول: نقد، کارگاهی، کارت به کارت، انتقال بانکی. در ثبت پرداخت‌ها برای تفکیک گزارش وصول استفاده می‌شود.",
                    "How money is received: cash, card-to-card, bank transfer. Used when recording payments to break down collection reports.",
                    "كيفية استلام الأموال: نقداً، تحويل بطاقة، حوالة بنكية. تستخدم عند تسجيل المدفوعات."),
                Leaf("shared/tax.status", "وضعیت‌های مالیاتی", "Tax Statuses", "الحالات الضريبية", "مشمول/معاف بودن طرف حساب؛ در محاسبه مالیات بر ارزش افزوده فاکتورها اعمال می‌شود.",
                    "Whether the account is taxable or exempt; applied when computing VAT on invoices.",
                    "هل الحساب خاضع للضريبة أو معفى؛ تُطبق عند حساب ضريبة القيمة المضافة على الفواتير."),
                Leaf("shared/address.type", "انواع نشانی", "Address Types", "أنواع العناوين", "تفکیک نشانی‌ها (دفتر مرکزی، انبار، محل ارسال). در پروفایل مشتری و انتخاب نشانی ارسال سفارش استفاده می‌شود.",
                    "Classifies addresses (head office, warehouse, shipping). Used in customer profiles and when picking a shipping address.",
                    "يصنف العناوين (المركز، المستودع، الشحن). تستخدم في ملف العميل واختيار عنوان الشحن."),
                Leaf("shared/contact.role", "نقش‌های مخاطب", "Contact Roles", "أدوار جهات الاتصال", "نقش هر مخاطب در سازمان مشتری (مدیرعامل، واحد خرید، انبار) تا بدانید با چه کسی برای چه کاری تماس بگیرید.",
                    "Each contact's role in the customer organization (CEO, purchasing, warehouse) so you know whom to call for what.",
                    "دور كل جهة اتصال في منظمة العميل (المدير، المشتريات، المستودع) لتعرف بمن تتصل ولأي أمر."),
                Leaf("shared/bank", "بانک‌ها", "Banks", "البنوك", "فهرست بانک‌های کشور؛ در ثبت حساب‌های بانکی مشتریان و تأمین‌کنندگان و اطلاعات واریز انتخاب می‌شود.",
                    "The country's bank list; selected when recording customer/supplier bank accounts and deposits.",
                    "قائمة البنوك؛ تُختار عند تسجيل الحسابات البنكية للعملاء والموردين."),
                Leaf("shared/bank.account.type", "انواع حساب بانکی", "Bank Account Types", "أنواع الحسابات البنكية", "جاری/سپرده بودن حساب؛ در چک‌ها و تسویه با تأمین‌کننده و ثبت واریزی مشتری اثر دارد.",
                    "Current vs. savings account; affects cheques, supplier settlement and customer deposits.",
                    "حساب جاري أو توفير؛ يؤثر على الشيكات وتسوية الموردين وإيداعات العملاء."),
                Leaf("shared/unit.of.measure", "واحدهای اندازه‌گیری", "Units of Measure", "وحدات القياس", "واحد شمارش کالا و سفارش (عدد، بسته، متر). مبناي محاسبه قیمت و موجودی انبار است.",
                    "The counting unit for products and orders (piece, pack, meter). The basis of price and stock calculations.",
                    "وحدة عد السلع والطلبات (قطعة، حزمة، متر). أساس حساب السعر والمخزون."),
                Leaf("shared/packaging.type", "انواع بسته‌بندی", "Packaging Types", "أنواع التغليف", "شکل بسته‌بندی خروجی (پکت، کارتن، سلفون). در بسته‌بندی سفارش و مرحله لجستیک استفاده می‌شود.",
                    "The output packaging form (pack, carton, shrink-wrap). Used in order packing and logistics.",
                    "شكل التغليف النهائي (علبة، كرتون، سيلوفان). يستخدم في تغليف الطلب واللوجستيات."),
                Leaf("shared/shipping.method", "روش‌های ارسال", "Shipping Methods", "طرق الشحن", "شیوه رساندن سفارش به مشتری (پست، تیپاکس، پیک). در مرحله ارسال لجستیک انتخاب می‌شود.",
                    "How the order reaches the customer (post, courier, bike). Selected at the logistics shipping step.",
                    "كيف يصل الطلب للعميل (بريد، مندوب). يُختار في مرحلة الشحن باللوجستيات."),
            ]),
            Group("ref-customers", "مشتریان", "Customers", "العملاء", "/master/reference-data/customers",
                "داده پایه مشتریان: دسته‌بندی‌هایی که پروفایل هر مشتری با آن‌ها طبقه‌بندی می‌شود.",
                "Customer master classifications used to categorize every customer profile.",
                "تصنيفات ملف العملاء.", [
                Leaf("customers/customer.group", "گروه‌های مشتری", "Customer Groups", "مجموعات العملاء", "طبقه‌بندی مشتریان (عمده‌فروش، خرده‌فروش، سازمانی، VIP). در پروفایل مشتری انتخاب می‌شود و در قیمت‌گذاری (تعرفه/تخفیف گروهی)، سقف اعتبار و گزارش‌های تفکیکی فروش به کار می‌رود؛ یعنی تصمیم «به این مشتری چه قیمتی بدهیم» از همین‌جا می‌آید.",
                    "Customer segmentation (wholesale, retail, corporate, VIP). Chosen on the customer profile and used in pricing (group tariffs/discounts), credit limits and sales breakdowns — it is where the decision \"what price does this customer get\" originates.",
                    "تصنيف العملاء (جملة، تجزئة، شركات، مميز). يُختار في ملف العميل ويستخدم في التسعير (تعرفة المجموعة والخصومات) وحدود الائتمان وتقارير المبيعات؛ من هنا يأتي قرار «ما السعر الذي نمنحه لهذا العميل».")
            ]),
            Group("ref-suppliers", "تأمین‌کنندگان", "Suppliers", "الموردون", "/master/reference-data/suppliers",
                "داده پایه تأمین‌کنندگان برای خرید مواد و خدمات.",
                "Supplier master data for purchasing materials and services.",
                "البيانات المرجعية للموردين لشراء المواد والخدمات.", [
                Leaf("suppliers/supplier.category", "دسته‌های تأمین‌کننده", "Supplier Categories", "فئات الموردين", "نوع فعالیت تأمین‌کننده (پارچه‌فروش، چاپ‌کار، خدمات)؛ در انتخاب منبع خرید و فیلتر تأمین‌کنندگان استفاده می‌شود.",
                    "The supplier's line of business (fabric, printing, services); used when picking a purchase source and filtering suppliers.",
                    "نشاط المورد (أقمشة، طباعة، خدمات)؛ تستخدم عند اختيار مصدر الشراء وتصفية الموردين."),
                Leaf("suppliers/supplier.grade", "رتبه‌های تأمین‌کننده", "Supplier Grades", "درجات الموردين", "رتبه کیفی (A تا C) که از ارزیابی‌های تأمین‌کننده به‌دست می‌آید؛ در تصمیم خرید (کدام تأمین‌کننده اولویت دارد) اعمال می‌شود.",
                    "Quality grade (A to C) derived from supplier evaluations; applied in purchasing decisions (which supplier comes first).",
                    "درجة الجودة (A إلى C) المستمدة من تقييمات الموردين؛ تُطبق في قرارات الشراء."),
                Leaf("suppliers/freight.delivery.term", "شرایط حمل و تحویل", "Freight & Delivery Terms", "شروط الشحن والتسليم", "اینکه هزینه و ریسک حمل با کیست (تحویل درب کارخانه، با وسیله فروشنده)؛ در سفارش خرید درج می‌شود.",
                    "Who pays for and owns freight risk (ex-works, delivered by seller); recorded on purchase orders.",
                    "من يدفع تكاليف الشحن يتحمل مخاطرها (تسليم في المصنع، بالسيارة المشتري)؛ يُسجل في أمر الشراء."),
                Leaf("suppliers/supplier.hold.reason", "دلایل مسدودی تأمین‌کننده", "Supplier Hold Reasons", "أسباب إيقاف الموردين", "چرا یک تأمین‌کننده مسدود شده (بدهی، افت کیفیت)؛ سیستم اجازه ثبت سفارش خرید جدید به تأمین‌کننده مسدود را نمی‌دهد.",
                    "Why a supplier is blocked (debt, quality drop); the system prevents new purchase orders for blocked suppliers.",
                    "لماذا تم إيقاف المورد (دين، انخفاض الجودة)؛ يمنع النظام أوامر الشراء الجديدة للمورد الموقوف.")
            ]),
            Group("ref-products", "محصولات", "Products", "المنتجات", "/master/reference-data/products",
                "ویژگی‌های طبقه‌بندی محصول؛ هر محصول با ترکیبی از این ویژگی‌ها تعریف و در سفارش/انبار استفاده می‌شود.",
                "Product classification attributes; each product combines these and uses them in orders and inventory.",
                "خصائص تصنيف المنتجات؛ كل منتج يجمعها ويستخدمها في الطلبات والمخازن.", [
                Leaf("products/product.type", "انواع محصول", "Product Types", "أنواع المنتجات", "خانواده محصول (تیشرت، هودی، کلاه)؛ ریشه طبقه‌بندی کاتالوگ محصول.",
                    "The product family (t-shirt, hoodie, cap); the root of the product catalog.",
                    "عائلة المنتج (تيشيرت، هودي، قبعة)؛ جذر كتالوج المنتجات."),
                Leaf("products/product.size", "اندازه‌ها", "Sizes", "المقاسات", "سایزهای پوشاک (S/M/L/XL)؛ در تعریف واریانت‌های محصول و برآورد متراژ چاپ استفاده می‌شود.",
                    "Apparel sizes (S/M/L/XL); used in product variants and print-area estimation.",
                    "مقاسات الملابس؛ تستخدم في متغيرات المنتج وتقدير مساحة الطباعة."),
                Leaf("products/product.color", "رنگ‌ها", "Colors", "الألوان", "رنگ محصول/واریانت؛ در فیلتر کاتالوگ و سفارش مشتری انتخاب می‌شود.",
                    "Product/variant color; picked in catalog filters and customer orders.",
                    "لون المنتج؛ يُختار في فلاتر الكتالوج وطلبات العملاء."),
                Leaf("products/product.brand", "برندها", "Brands", "الماركات", "برند محصول؛ در کاتالوگ و گزارش فروش برندمحور استفاده می‌شود.",
                    "The product brand; used in the catalog and brand-based sales reports.",
                    "ماركة المنتج؛ تستخدم في الكتالوج وتقارير المبيعات."),
                Leaf("products/product.material", "مواد و پارچه‌ها", "Materials", "الخامات", "جنس پارچه/مواد اولیه (نخ پنبه، پلی‌استر)؛ در مشخصات فنی محصول و توصیه چاپ اثر دارد.",
                    "Fabric/raw material (cotton, polyester); affects product technical specs and print recommendations.",
                    "الخامة (قطن، بوليستر)؛ تؤثر على المواصفات الفنية وتوصيات الطباعة."),
                Leaf("products/product.gender", "گروه‌های جنسیتی پوشاک", "Gender Groups", "الفئات", "مردانه/زنانه/یونیسکس؛ در کاتالوگ و فیلتر محصول استفاده می‌شود.",
                    "Men/Women/Unisex; used in the product catalog and filters.",
                    "رجالي/نسائي/للجنسين؛ تستخدم في الكتالوج والفلاتر."),
                Leaf("products/product.fit", "قالب‌های پوشاک", "Fits", "القصات", "فرم دوخت (Regular، Oversize)؛ در مشخصات محصول و انتخاب طرح متناسب اثر دارد.",
                    "The cut form (Regular, Oversize); affects product specs and matching design choice.",
                    "شكل القص (ريجولار، أوفرسايز)؛ يؤثر على مواصفات المنتج واختيار التصميم."),
                Leaf("products/product.style", "سبک‌های محصول", "Product Styles", "أنماط المنتج", "سبک تولید (ساده، راگلان)؛ برای تفکیک کاتالوگ و گزارش‌ها.",
                    "The production style (plain, raglan); for catalog breakdowns and reports.",
                    "نمط الإنتاج؛ لتفصيل الكتالوج والتقارير.")
            ]),
            Group("ref-artwork", "بانک طرح", "Design Bank", "بنك التصاميم", "/master/reference-data/artwork",
                "طبقه‌بندی‌های بانک طرح: هر طرح با این محورها برچسب‌گذاری می‌شود تا جستجو، فیلتر و انتخاب طرح در سفارش دقیق و سریع باشد.",
                "Design bank taxonomy: every design is tagged with these axes so search, filtering and order-time design selection stay fast and precise.",
                "تصنيفات بنك التصاميم: كل تصميم يُوسم بها ليبقى البحث والاختيار في الطلبات سريعاً ودقيقاً.", [
                Leaf("artwork/artwork.category", "دسته‌بندی طرح‌ها", "Design Categories", "فئات التصاميم", "دسته اصلی طرح (حیوانات، کارتونی، اسپرت...)؛ سطح اول فیلتر بانک طرح و نقطه شروع انتخاب طرح برای سفارش مشتری است.",
                    "The design's top category (animals, cartoon, sport...); the first filter level of the design bank and the starting point for picking a design for an order.",
                    "الفئة الرئيسية للتصميم (حيوانات، كرتون، رياضة)؛ أول مستوى تصفية في بنك التصاميم ونقطة بدء اختيار التصميم للطلب."),
                Leaf("artwork/artwork.subcategory", "زیرگروه‌های طرح", "Design Subcategories", "الفئات الفرعية", "زیر هر دسته، تفکیک دقیق‌تر (آمبولانس، آتش‌نشانی زیر حیوانات)؛ جستجو را دقیق‌تر می‌کند.",
                    "Finer breakdown under each category (ambulance, fire-truck under animals); makes search more precise.",
                    "تفصيل أدق تحت كل فئة (إسعاف، إطفاء)؛ يجعل البحث أدق."),
                Leaf("artwork/artwork.type", "انواع طرح", "Design Types", "أنواع التصاميم", "جنس طرح (وکتور، عکس، تایپوگرافی)؛ در قابلیت چاپ و انتخاب روش چاپ اثر دارد.",
                    "The design's nature (vector, photo, typography); affects printability and print-method choice.",
                    "طبيعة التصميم (فيكتور، صورة، تايبوغرافي)؛ تؤثر على قابلية الطباعة واختيار طريقتها."),
                Leaf("artwork/artwork.style", "سبک‌های طرح", "Design Styles", "أنماط التصاميم", "سبک بصری (مینیمال، واقع‌گرا)؛ برای پیشنهاد طرح متناسب با سلیقه مشتری.",
                    "The visual style (minimal, realistic); to suggest designs matching customer taste.",
                    "النمط البصري (مينيمال، واقعي)؛ لاقتراح تصميم يناسب ذوق العميل."),
                Leaf("artwork/artwork.theme", "موضوعات طرح", "Design Themes", "مواضيع التصاميم", "موضوع محتوایی طرح (مادر، ورزش، مذهبی)؛ محور جستجوی مفهومی و سلیقه‌ای مشتری.",
                    "The design's content theme (mother, sport, religious); powers conceptual search.",
                    "الموضوع المحتوى للتصميم (الأم، الرياضة، ديني)؛ يدعم البحث المفاهيمي."),
                Leaf("artwork/artwork.audience", "مخاطبان طرح", "Design Audiences", "الجمهور المستهدف", "طرح برای چه گروهی مناسب است (کودکان، نوجوانان، بزرگسال)؛ در انتخاب طرح سفارش‌های خاص اثر دارد.",
                    "Which group the design suits (kids, teens, adults); matters when picking designs for specific orders.",
                    "لمن يناسب التصميم (أطفال، مراهقون، بالغون)؛ مهم عند اختيار التصاميم لطلبات معينة."),
                Leaf("artwork/artwork.license", "انواع مجوز طرح", "Design Licenses", "أنواع التراخيص", "مجوز استفاده طرح (اختصاصی، عمومی، خریداری‌شده)؛ برای کنترل حق استفاده و جلوگیری از سوءاستفاده طرح‌های اختصاصی.",
                    "The design's usage license (exclusive, public, purchased); controls usage rights and protects exclusive designs.",
                    "ترخيص استخدام التصميم (حصري، عام، مشترى)؛ يتحكم بحقوق الاستخدام ويحمي التصاميم الحصرية."),
                Leaf("artwork/artwork.usage", "انواع کاربرد طرح", "Design Usages", "أنواع الاستخدام", "کاربرد نهایی طرح (تیشرت، توت‌بگ، کلاه)؛ در پیشنهاد طرح متناسب با محصول سفارش استفاده می‌شود.",
                    "The design's final application (t-shirt, tote bag, cap); used to suggest designs matching the ordered product.",
                    "الاستخدام النهائي للتصميم (تيشيرت، حقيبة، قبعة)؛ لاقتراح تصميم يناسب المنتج المطلوب."),
                Leaf("artwork/artwork.tag", "برچسب‌های طرح", "Design Tags", "وسوم التصاميم", "برچسب‌های آزاد و دلخواه روی طرح؛ برای جستجوهای خاص که در دسته‌بندی‌ها جایی ندارند.",
                    "Free-form tags on designs; for special searches that don't fit the taxonomy.",
                    "وسوم حرة على التصاميم؛ لعمليات بحث خاصة لا تناسبها التصنيفات.")
            ]),
            Group("ref-orders", "سفارشات", "Orders", "الطلبات", "/master/reference-data/orders",
                "تعاریف پایه سفارش‌ها.",
                "Base order definitions.",
                "تعريفات أساسية للطلبات.", [
                Leaf("orders/order.type", "انواع سفارش", "Order Types", "أنواع الطلبات", "نوع سفارش (تولیدی معمولی، نمونه، مرجوعی)؛ در قیمت‌گذاری، مسیر گردش کار و گزارش‌ها تفکیک ایجاد می‌کند.",
                    "The order kind (standard production, sample, return); creates separation in pricing, workflow routing and reports.",
                    "نوع الطلب (إنتاج عادي، عينة، مرتجع)؛ يخلق تفرقة في التسعير ومسار سير العمل والتقارير.")
            ]),
            Group("ref-pricing", "قیمت‌گذاری", "Pricing", "التسعير", "/master/reference-data/pricing",
                "تعاریف پایه موتور قیمت‌گذاری (اگر در آینده جزء دیگری اضافه شود اینجا ظاهر می‌شود). قیمت نهایی سفارش از ترکیب تعرفه‌ها، قواعد تخفیف و مالیات در همین بخش محاسبه می‌شود.",
                "Base definitions of the pricing engine. The order's final price is computed here from tariffs, discount rules and taxes.",
                "تعريفات أساسية لمحرك التسعير؛ يُحسب السعر النهائي للطلب هنا من التعريفات وقواعد الخصم والضرائب.", null),
            Group("ref-finance", "مالی", "Finance", "المالية", "/master/reference-data/finance",
                "تعاریف مالی مشترک (شرایط/روش پرداخت، مالیات) که فاکتورها و پرداخت‌ها از آن استفاده می‌کنند.",
                "Shared finance definitions (payment terms/methods, tax) used by invoices and payments.",
                "تعريفات مالية مشتركة تستخدمها الفواتير والمدفوعات.", null),
            Group("ref-inventory", "موجودی و خرید", "Inventory & Purchasing", "المخازن والمشتريات", "/master/reference-data/inventory",
                "تعاریف انبار و خرید: دلایل تعدیل موجودی و اقلام مرتبط.",
                "Inventory and purchasing definitions: stock adjustment reasons and related items.",
                "تعريفات المخازن والمشتريات: أسباب تسوية المخزون وما يتعلق بها.", [
                Leaf("inventory/inventory.adjustment.reason", "دلایل تعدیل موجودی", "Stock Adjustment Reasons", "أسباب تسوية المخزون", "چرا موجودی اصلاح شده (مغایرت شمارش، شکست، اتلاف)؛ هر تعدیل انبار باید دلیل داشته باشد تا گزارش‌های اتلاف و کسری قابل تحلیل باشد.",
                    "Why stock was corrected (count mismatch, breakage, waste); every adjustment needs a reason so loss/shortage reports stay analyzable.",
                    "لماذا صُحح المخزون (فرق الجرد، الكسر، التلف)؛ كل تسوية تحتاج سبباً لتبقى تقارير التلف قابلة للتحليل.")
            ]),
            Group("ref-equipment", "تجهیزات", "Equipment", "المعدات", "/master/reference-data/equipment",
                "تعاریف تجهیزات تولید (چاپ/پرس).",
                "Production equipment definitions (print/press).",
                "تعريفات معدات الإنتاج (الطباعة والكبس).", [
                Leaf("equipment/equipment.type", "انواع تجهیزات", "Equipment Types", "أنواع المعدات", "نوع دستگاه (چاپ سیلک، پرس حرارتی)؛ در تعریف ماشین‌های خط تولید و محاسبه ظرفیت استفاده می‌شود.",
                    "The machine type (silk-screen printer, heat press); used to define production-line machines and compute capacity.",
                    "نوع الجهاز (طابعة سيلك، مكبس حراري)؛ يستخدم في تعريف ماكينات خط الإنتاج وحساب الطاقة."),
                Leaf("equipment/equipment.status", "وضعیت‌های عملیاتی تجهیزات", "Equipment Statuses", "حالات المعدات", "وضعیت دستگاه (فعال، تعمیر، از رده خارج)؛ زمان‌بندی صف تولید فقط دستگاه‌های آماده را پیشنهاد می‌دهد.",
                    "The machine state (active, under repair, retired); production scheduling only offers ready machines.",
                    "حالة الجهاز (يعمل، في الصيانة، خارج الخدمة)؛ جدولة الإنتاج يقترح الأجهزة الجاهزة فقط.")
            ]),
            Group("ref-production", "طراحی، چاپ و پرس", "Design, Print & Press", "التصميم والطباعة والكبس", "/master/reference-data/production",
                "پارامترهای فنی فرآیند چاپ و پرس که در عملیات تولید سفارش استفاده می‌شود.",
                "Technical parameters of the print & press process used in order production.",
                "المعايير الفنية لعملية الطباعة والكبس المستخدمة في إنتاج الطلبات.", [
                Leaf("production/print.method", "روش‌های چاپ", "Print Methods", "طرق الطباعة", "تکنیک چاپ (سیلک، دیجیتال DTF/DTG)؛ روی فرآیند تولید، زمان و قیمت هر عملیات چاپ اثر مستقیم دارد.",
                    "The print technique (silk-screen, DTF/DTG digital); directly affects the production process, time and per-print price.",
                    "تقنية الطباعة (سيلك، ديجيتال DTF/DTG)؛ تؤثر مباشرة على عملية الإنتاج ووقته وسعر كل طباعة."),
                Leaf("production/print.direction", "جهت‌های چاپ", "Print Directions", "اتجاهات الطباعة", "محل/جهت چاپ روی لباس (سینه، پشت، آستین)؛ در برگه فنی تولید درج می‌شود.",
                    "The print placement on the garment (chest, back, sleeve); recorded on the production technical sheet.",
                    "موضع الطباعة على الملابس (الصدر، الظهر، الكم)؛ يُسجل في ورقة الإنتاج الفنية."),
                Leaf("production/print.size", "اندازه‌های چاپ", "Print Sizes", "مقاسات الطباعة", "ابعاد سطح چاپ (A3، A4، ۳۰×۴۰)؛ مبنای مصرف مواد و قیمت‌گذاری هر چاپ.",
                    "The print area dimensions (A3, A4, 30×40); the basis of material consumption and per-print pricing.",
                    "أبعاد مساحة الطباعة (A3، A4)؛ أساس استهلاك المواد وتسعير كل طباعة.")
            ]),
            Group("ref-quality", "کنترل کیفی و بازتولید", "Quality Control", "مراقبة الجودة", "/master/reference-data/quality",
                "دسته‌بندی عیوب برای بازرسی کیفیت.",
                "Defect taxonomy for quality inspection.",
                "تصنيف العيوب لفحص الجودة.", [
                Leaf("quality/defect.category", "دسته‌بندی عیوب", "Defect Categories", "فئات العيوب", "خانواده عیوب (چاپ، دوخت، پارچه)؛ سطح اول طبقه‌بندی در بازرسی.",
                    "The defect family (print, stitching, fabric); the first classification level in inspection.",
                    "عائلة العيب (طباعة، خياطة، قماش)؛ أول مستوى تصنيف في الفحص."),
                Leaf("quality/defect.type", "انواع عیب", "Defect Types", "أنواع العيوب", "عیب مشخص و قابل اندازه‌گیری (رنگ‌پریزی، جابجایی چاپ)؛ ثبت عیوب با این نوع‌ها، نمودار پارتو و تصمیم بازتولید را ممکن می‌کند.",
                    "A specific measurable defect (color fading, print misalignment); logging defects by type enables Pareto analysis and reproduction decisions.",
                    "عيب محدد قابل للقياس (بهت اللون، انزياح الطباعة)؛ تسجيل العيوب بهذه الأنواع يمكّن تحليل باريتو وقرار إعادة الإنتاج.")
            ]),
            Group("ref-logistics", "بسته‌بندی و ارسال و تحویل", "Logistics", "اللوجستيات", "/master/reference-data/logistics",
                "تعاریف حمل و تحویل سفارش‌ها.",
                "Order delivery definitions.",
                "تعريفات تسليم الطلبات.", [
                Leaf("logistics/shipping.carrier", "حمل‌کنندگان", "Carriers", "الناقلون", "شرکت‌های حمل بار (پست، تیپاکس، پیک)؛ در مرحله ارسال مرسوله انتخاب و کد رهگیری به آن گره می‌خورد.",
                    "Carrier companies (post, courier); selected at shipment creation and linked with the tracking code.",
                    "شركات النقل (البريد، مندوب)؛ تُختار عند إنشاء الشحنة وتربط برمز التتبع.")
            ]),
            Group("ref-workflow", "کارتابل و گردش کار", "Cartable & Workflow", "سلة الأعمال وسير العمل", "/master/reference-data/workflow",
                "تعاریف گردش کار و کارتابل (اگر نوع اقدام یا الگوی جدیدی تعریف شود اینجا می‌آید). کارتابل صفحه اقدام روزانه کاربران است.",
                "Workflow and cartable definitions (new action types or patterns appear here). The cartable is the users' daily action page.",
                "تعريفات سير العمل وسلة الأعمال؛ سلة الأعمال هي صفحة الإجراءات اليومية للمستخدمين.", null)
        ]),

        Group("system", "مدیریت سیستم", "System Management", "إدارة النظام", null,
            "مدیریت ساختار سازمان، منابع انسانی، دارایی‌ها و امنیت دسترسی. معمولاً فقط نقش‌های مدیریتی به آن دسترسی دارند.",
            "Managing the organization structure, people, assets and access security. Usually limited to administrator roles.",
            "إدارة الهيكل التنظيمي والموظفين والأصول وأمن الوصول؛ عادة تقتصر على الأدوار الإدارية.", [
            Leaf("system/org-chart", "چارت سازمانی", "Organization Chart", "الهيكل التنظيمي", "/master/organization-chart", "تعریف واحدها (فروش، تولید، انبار) و سلسله‌مراتب آن‌ها؛ مبنای تخصیص کاربر به واحد، مسیر ارجاع کارتابل و گزارش‌های واحدی است.",
                "Defining units (sales, production, warehouse) and their hierarchy; the basis for assigning users to units, cartable routing and per-unit reports.",
                "تعريف الوحدات (المبيعات، الإنتاج، المستودع) وتسلسلها؛ أساس إسناد المستخدمين للوحدات وتوجيه سلة الأعمال والتقارير."),
            Leaf("system/persons", "افراد", "Persons", "الأشخاص", "/master/persons", "دفتر اشخاص مرتبط با سازمان (کارمندان، مسئولان واحدها)؛ هر کاربر سیستم می‌تواند به یک شخص متصل شود تا نام واقعی و سمت او در گردش کارها دیده شود.",
                "The registry of people related to the organization (employees, unit heads); each system user can link to a person so their real name and role appear in workflows.",
                "سجل الأشخاص المرتبطين بالمؤسسة (الموظفون، رؤساء الوحدات)؛ يمكن ربط كل مستخدم بشخص ليظهر اسمه الحقيقي وسمته في سير العمل."),
            Leaf("system/suppliers", "تأمین‌کنندگان", "Suppliers", "الموردون", "/master/suppliers", "دفتر کامل تأمین‌کنندگان: اطلاعات تماس، حساب بانکی، قرارداد، ارزیابی عملکرد و سابقه خرید؛ منبع انتخاب تأمین‌کننده در سفارش‌های خرید.",
                "The full supplier registry: contacts, bank accounts, contracts, performance evaluations and purchase history; the source for picking suppliers on purchase orders.",
                "سجل كامل للموردين: جهات الاتصال والحسابات البنكية والعقود وتقييم الأداء وسجل المشتريات؛ مصدر اختيار الموردين في أوامر الشراء."),
            Leaf("system/equipment", "تجهیزات", "Equipment", "المعدات", "/master/equipment", "دفتر تجهیزات و ماشین‌آلات: مشخصات، وضعیت عملیاتی، سابقه نگهداری و تعمیرات؛ در برنامه‌ریزی تولید برای انتخاب دستگاه و ظرفیت استفاده می‌شود.",
                "The equipment and machinery registry: specs, operational status, maintenance and repair history; used in production planning for machine and capacity selection.",
                "سجل المعدات والآلات: المواصفات والحالة التشغيلية وسجل الصيانة والإصلاحات؛ يستخدم في تخطيط الإنتاج لاختيار الجهاز والطاقة."),
            Leaf("system/shifts", "شیفت‌ها", "Work Shifts", "الورديات", "/master/shifts", "تعریف شیفت‌های کاری کارگاه (صبح، عصر، شب) با ساعات کاری؛ در برنامه‌ریزی تولید برای محاسبه ظرفیت روزانه هر خط به کار می‌رود.",
                "Defining workshop shifts (morning, evening, night) with working hours; used in production planning to compute daily line capacity.",
                "تعريف ورديات الورشة (صباحية، مسائية، ليلية) بساعات العمل؛ يستخدم في تخطيط الإنتاج لحساب الطاقة اليومية لكل خط."),
            Leaf("system/products", "محصولات", "Products", "المنتجات", "/manage/products", "دفتر محصولات قابل فروش: تعریف محصول، واریانت‌ها (سایز/رنگ)، مشخصات فنی و قیمت پایه؛ منبع انتخاب آیتم در سفارش‌ها.",
                "The sellable product catalog: product definitions, variants (size/color), technical specs and base price; the source for picking items on orders.",
                "كتالوج المنتجات القابلة للبيع: تعريف المنتج والمتغيرات (المقاس/اللون) والمواصفات الفنية والسعر الأساسي؛ مصدر اختيار البنود في الطلبات."),
            Leaf("system/user-access", "مدیریت کاربران و دسترسی", "User & Access Management", "إدارة المستخدمين والصلاحيات", "/admin/user-access", "ایجاد کاربران، تعیین نقش و مجوزها، اتصال کاربر به شرکت و واحد؛ تعیین می‌کند هر کس چه منویی ببیند و چه کاری بتواند انجام دهد. مهم‌ترین ابزار امنیتی سیستم.",
                "Creating users, assigning roles and permissions, linking users to companies and units; it decides which menus each person sees and what they can do. The system's primary security tool.",
                "إنشاء المستخدمين وتعيين الأدوار والصلاحيات وربط المستخدم بالشركة والوحدة؛ يحدد ما يراه كل شخص وما يستطيع فعله. وهي أداة الأمن الرئيسية للنظام."),
            Leaf("system/workflow", "گردش کار", "Workflow", "سير العمل", "/workspace/workflow", "پیکربندی مسیر تأیید و اقدام‌ها (چه کسی بعد از چه کاری را انجام دهد)، SLAها و موارد نیازمند رسیدگی؛ موتور حرکت سفارش بین مراحل است.",
                "Configuring approval routes and actions (who does what next), SLAs and items needing attention; the engine that moves orders between stages.",
                "تكوين مسارات الموافقة والإجراءات (من يفعل ماذا بعد)، اتفاقيات مستوى الخدمة والبنود التي تحتاج معالجة؛ محرك انتقال الطلبات بين المراحل.")
        ]),

        Leaf("customers", "مشتریان", "Customers", "العملاء", "/manage/customers",
            "دفتر کامل مشتریان حقیقی و حقوقی: ثبت پروفایل، مخاطبان، نشانی‌ها، حساب‌های بانکی و وضعیت مالی. هر سفارش به یک مشتری وصل است و داده‌های همین بخش در فاکتور و ارسال استفاده می‌شود.",
            "The full registry of individual and corporate customers: profiles, contacts, addresses, bank accounts and financial status. Every order links to a customer, and this data feeds invoices and shipping.",
            "سجل كامل للعملاء الأفراد والشركات: الملفات وجهات الاتصال والعناوين والحسابات البنكية والوضع المالي. كل طلب يرتبط بعميل وتُستخدم هذه البيانات في الفواتير والشحن."),

        Leaf("design-bank", "بانک طرح", "Design Bank", "بنك التصاميم", "/manage/design-bank",
            "مخزن مرکزی طرح‌های چاپ: آپلود فایل طرح، نسخه‌بندی (هر بار اصلاح، نسخه جدید)، دسته‌بندی سه‌زبانه، برچسب‌ها، جستجوی مفهومی و بندانگشتی خودکار. هنگام ثبت سفارش، کاربر از همین‌جا طرح را انتخاب می‌کند و سیستم فقط اجازه استفاده از طرح‌های دارای مجوز را می‌دهد.",
            "The central print-design repository: upload design files, versioning (each revision becomes a new version), trilingual categories, tags, conceptual search and automatic thumbnails. When placing an order, the user picks the design here and the system only permits licensed usage.",
            "المستودع المركزي لتصاميم الطباعة: رفع الملفات، إدارة الإصدارات (كل تعديل نسخة جديدة)، التصنيفات الثلاثية، الوسوم، البحث المفاهيمي والمصغرات التلقائية. عند إنشاء الطلب يختار المستخدم التصميم من هنا والنظام يسمح فقط بالاستخدام المرخص."),

        Leaf("orders", "سفارشات", "Orders", "الطلبات", "/workspace/orders",
            "چرخه کامل سفارش: ایجاد پیش‌نویس، افزودن آیتم با طرح و عملیات چاپ، محاسبه لحظه‌ای قیمت، ثبت و عبور از گردش کار تأیید، تا تولید و ارسال. تاریخچه و تغییرات هر سفارش کامل ثبت می‌شود.",
            "The full order lifecycle: create a draft, add items with designs and print operations, live price calculation, submit through the approval workflow, down to production and shipping. Every change is fully tracked.",
            "دورة حياة الطلب الكاملة: إنشاء مسودة، إضافة البنود بالتصاميم وعمليات الطباعة، حساب السعر الفوري، الإرسال عبر سير عمل الموافقة، حتى الإنتاج والشحن. تُسجل كل التغييرات بالكامل."),

        Leaf("cartable", "کارتابل", "Cartable", "سلة الأعمال", "/cartable",
            "صفحه اقدام روزانه: همه کارهایی که نوبت شماست (تأیید سفارش، بازرسی کیفیت، تأیید قیمت) اینجا جمع می‌شود؛ ارجاع‌ها را می‌پذیرید یا رد می‌کنید و پرونده هر کار را می‌بینید. تأخیرها با SLA کنترل و کار معوق ایمیل/اعلان می‌شود.",
            "The daily action page: everything waiting on you (order approval, quality inspection, price confirmation) gathers here; you accept or decline referrals and open each case. Delays are controlled by SLA with email/notification escalation.",
            "صفحة الإجراءات اليومية: كل ما ينتظرك (اعتماد طلب، فحص الجودة، اعتماد سعر) يجمع هنا؛ تقبل الإحالات أو ترفضها وتفتح ملف كل مهمة. تُراقب التأخيرات باتفاقيات مستوى الخدمة مع تنبيهات بالبريد والإشعارات."),

        Leaf("pricing", "قیمت‌گذاری", "Pricing", "التسعير", "/workspace/pricing",
            "موتور قیمت: تعریف تعرفه‌های پایه، قواعد تخفیف (گروه مشتری/مقدار)، مالیات و شبیه‌سازی قیمت سفارش قبل از ثبت. خروجی این بخش، قیمت لحظه‌ای درون سفارش‌هاست.",
            "The pricing engine: base tariffs, discount rules (customer group/quantity), taxes and pre-submit order price simulation. Its output is the live price inside orders.",
            "محرك التسعير: التعريفات الأساسية وقواعد الخصم (مجموعة العميل/الكمية) والضرائب ومحاكاة سعر الطلب قبل الإرسال. ناتجه هو السعر الفوري داخل الطلبات."),

        Leaf("inventory", "موجودی و خرید", "Inventory & Purchasing", "المخزون والمشتريات", "/workspace/inventory",
            "مدیریت انبار و تأمین: موجودی کالا، رزرو برای سفارش‌ها، کسری‌ها، درخواست خرید، سفارش خرید به تأمین‌کننده و رسید کالا. کسری مواد قبل از تولید اینجا شناسایی می‌شود.",
            "Warehouse and supply management: stock balances, order reservations, shortages, purchase requests, purchase orders to suppliers and goods receipt. Material shortages are identified here before production.",
            "إدارة المخازن والتوريد: أرصدة المخزون وحجوزات الطلبات والعجز وطلبات الشراء وأوامر الشراء للموردين واستلام البضائع. يُكتشف عجز المواد هنا قبل الإنتاج."),

        Leaf("production", "تولید", "Production", "الإنتاج", "/workspace/production",
            "صف تولید: کارهای چاپ و پرس ساخته‌شده از سفارش‌های تأییدشده. اپراتور هر کار را شروع/متوقف/تکمیل می‌کند، دستگاه و شیفت مشخص می‌شود، زمان واقعی ثبت می‌گردد و با تکمیل چاپ/پرس، بازرسی کیفیت خودکار آغاز می‌شود.",
            "The production queue: print and press jobs generated from approved orders. Operators start/suspend/complete each job, machine and shift are assigned, actual time is recorded, and completing print/press auto-starts quality inspection.",
            "قائمة الإنتاج: مهام الطباعة والكبس الناتجة عن الطلبات المعتمدة. يبدأ المشغل كل مهمة أو يوقفها أو يكملها، ويُحدد الجهاز والوردية ويسجل الوقت الفعلي، وعند إتمام الطباعة/الكبس يبدأ فحص الجودة تلقائياً."),

        Leaf("quality", "کنترل کیفیت و بازتولید", "Quality Control", "مراقبة الجودة", "/workspace/quality",
            "بازرسی کیفیت تولید: چک‌لیست معیارها، ثبت عیوب با نوع و شدت، تصمیم (قبول/رد/تعلیق/بازبینی) و در صورت نیاز صدور درخواست بازتولید که به‌صورت کار تولید جدید به صف تولید برمی‌گردد. گزارش پارتو ضایعات از همین داده‌ها ساخته می‌شود.",
            "Production quality inspection: criteria checklists, defect logging with type and severity, disposition (accept/reject/suspend/re-review) and reproduction requests that return to the production queue as new jobs. The scrap Pareto report is built from this data.",
            "فحص جودة الإنتاج: قوائم معايير، تسجيل العيوب بنوعها وشدتها، القرار (قبول/رفض/تعليق/إعادة نظر) وإصدار طلبات إعادة الإنتاج التي تعود إلى قائمة الإنتاج كمهام جديدة. يُبنى تقرير باريتو للهالك من هذه البيانات."),

        Leaf("logistics", "لجستیک", "Logistics", "اللوجستيات", "/workspace/logistics",
            "از بسته‌بندی تا تحویل: ساخت بسته برای اقلام آماده، تحویل به انبار ارسال، ساخت مرسوله با کد رهگیری و حمل‌کننده، پیگیری تا تحویل موفق یا مرجوعی. وضعیت سفارش در همین چرخه به‌روز می‌شود.",
            "From packing to delivery: build packages for ready items, hand off to the shipping warehouse, create shipments with tracking code and carrier, and track to successful delivery or return. The order status updates through this cycle.",
            "من التغليف إلى التسليم: بناء الحزم للبنود الجاهزة، التسليم لمستودع الشحن، إنشاء شحنة برمز تتبع وناقل، والتتبع حتى التسليم الناجح أو الإرجاع. تتحدث حالة الطلب خلال هذه الدورة."),

        Leaf("notifications", "اعلان‌ها", "Notifications", "الإشعارات", "/workspace/notifications",
            "جعبه ورودی اعلان‌های درون‌سیستمی: رویدادهای مهم (سفارش جدید، قیمت نیازمند تأیید، ارسال مرسوله) به‌صورت اعلان می‌آیند؛ خوانده/نخوانده شدن و پیوند به صفحه مرتبط را مدیریت می‌کند.",
            "The in-app notification inbox: key events (new order, price awaiting approval, shipment sent) arrive as notifications; it manages read/unread state and links to the related page.",
            "صندوق الإشعارات الداخلية: الأحداث المهمة (طلب جديد، سعر بانتظار الاعتماد، شحنة مُرسلة) تصل كإشعارات؛ يدير حالة المقروء/غير المقروء ويربط بالصفحة ذات الصلة."),

        Leaf("management", "داشبورد مدیریتی", "Management Dashboard", "لوحة القيادة الإدارية", "/workspace/management",
            "تصویر کلان برای مدیریت: شمارش سفارش‌ها بر اساس وضعیت، روند تولید، ضایعات، عملکرد اپراتورها و تحویل‌ها؛ داده‌ها را زنده از ماژول‌ها می‌خواند و برای جلسات مدیریت ساخته شده است.",
            "The executive view: order counts by status, production trends, scrap, operator performance and deliveries; it reads live data from all modules and is built for management meetings.",
            "العرض التنفيذي: عدد الطلبات حسب الحالة واتجاهات الإنتاج والهالك وأداء المشغلين والتسليمات؛ يقرأ البيانات الحية من كل الوحدات وصُمم لاجتماعات الإدارة.")
    ];

    private static HelpNode Group(string id, string fa, string en, string ar, string? route, string faDesc, string enDesc, string arDesc, HelpNode[]? children = null)
        => new(id, fa, en, ar, route, faDesc, enDesc, arDesc, children);
    private static HelpNode Leaf(string id, string fa, string en, string ar, string faDesc, string enDesc, string arDesc)
        => new(id, fa, en, ar, null, faDesc, enDesc, arDesc, null);
    private static HelpNode Leaf(string id, string fa, string en, string ar, string route, string faDesc, string enDesc, string arDesc)
        => new(id, fa, en, ar, route, faDesc, enDesc, arDesc, null);
}
