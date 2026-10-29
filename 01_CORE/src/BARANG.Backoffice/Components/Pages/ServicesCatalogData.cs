using BARANG.Backoffice.Components.Help;

namespace BARANG.Backoffice.Components.Pages;

public sealed record ServiceGroup(string Id, string Fa, string En, string Ar)
{
    public string Name(string culture) => culture switch { "en" => En, "ar" => Ar, _ => Fa };
}

public sealed record ServiceInput(string Name, string Type, string In, bool Required);

public sealed record ServiceInfo(
    string GroupId, int Sort, string Method, string Route, string Permission, string Limiter,
    string NameFa, string NameEn, string NameAr,
    string DescFa, string DescEn, string DescAr,
    ServiceInput[] Inputs, string Output, string Headers, bool PublicAccess)
{
    public const string BaseUrl = "/api/v1";
    public string Name(string culture) => culture switch { "en" => NameEn, "ar" => NameAr, _ => NameFa };
    public string FullRoute => BaseUrl + Route;
    public string Auth(string culture) => PublicAccess
        ? culture switch { "en" => "None (public endpoint)", "ar" => "بدون مصادقة (عام)", _ => "بدون احراز هویت (عمومی)" }
        : culture switch { "en" => "Bearer Token + X-Company-ID header", "ar" => "Bearer Token + ترويسة X-Company-ID", _ => "Bearer Token + هدر X-Company-ID" };
}

public static class ServicesCatalogData
{
    private const string JsonOut = "JSON: رکورد درخواستی با تمام فیلدها";
    private const string ListOut = "JSON: آرایه رکوردها";
    private const string NoContent = "204 No Content (بدون بدنه)";

    private static ServiceInput In(string name, string type, string location, bool required, bool header = false)
        => header ? new(name, type, "Header", required) : new(name, type, location, required);

    public static readonly IReadOnlyList<ServiceGroup> Groups =
    [
        new("orders", "سفارشات", "Orders", "الطلبات"),
        new("customers", "مشتریان", "Customers", "العملاء"),
        new("catalog", "کاتالوگ محصولات", "Product Catalog", "كتالوج المنتجات"),
        new("files", "فایل‌ها", "Files", "الملفات"),
        new("public", "کاتالوگ عمومی (B2C)", "Public Catalog (B2C)", "الكتالوج العام"),
    ];

    public static readonly IReadOnlyList<ServiceInfo> All =
    [
        N("orders", "1", "POST", "/client/orders/drafts", "ClientOrderWrite", "client-mutation + هدر Idempotency-Key",
            "ایجاد پیش‌نویس سفارش", "Create order draft", "إنشاء مسودة طلب",
            "سفارش جدید را به‌صورت پیش‌نویس می‌سازد؛ با هدر Idempotency-Key ارسال تکراری دوباره سفارش نمی‌سازد و همان سفارش را برمی‌گرداند.",
            "Creates a new order as a draft; with the Idempotency-Key header, repeated calls return the same order instead of duplicating it.",
            "ينشئ الطلب كمسودة؛ مع ترويسة Idempotency-Key تعيد الندمات المتكررة نفس الطلب.",
            Inputs(
                In("branchId", "Guid", "Body", true),
                In("customerId", "Guid", "Body", true),
                In("source", "string", "Body", true),
                In("orderDate", "date", "Body", true),
                In("requestedDeliveryDate", "date", "Body", false),
                In("externalReference", "string", "Body", false),
                In("shippingAddress", "object", "Body", false),
                In("Idempotency-Key", "string", "Header", true, true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "2", "GET", "/client/orders/{id}", "ClientOrderRead", "client-read",
            "مشاهده سفارش", "Get order", "عرض الطلب",
            "جزئیات کامل سفارش شامل آیتم‌ها، عملیات چاپ، وضعیت و قیمت را برمی‌گرداند.",
            "Returns full order details including items, print operations, status and pricing.",
            "يعيد تفاصيل الطلب كاملة بما فيها البنود وعمليات الطباعة والحالة والسعر.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("orders", "3", "PUT", "/client/orders/{id}", "ClientOrderWrite", "client-mutation",
            "ویرایش پیش‌نویس", "Edit draft", "تحرير المسودة",
            "فیلدهای سرصفحه پیش‌نویس (شعبه، مشتری، تاریخ‌ها) را با کنترل نسخه ویرایش می‌کند.",
            "Edits draft header fields (branch, customer, dates) with optimistic version control.",
            "يحرر حقول المسودة مع التحكم بالإصدار.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("branchId", "Guid", "Body", true),
                In("customerId", "Guid", "Body", true),
                In("orderDate", "date", "Body", true),
                In("requestedDeliveryDate", "date", "Body", false),
                In("externalReference", "string", "Body", false),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("orders", "4", "POST", "/client/orders/{id}/items", "ClientOrderWrite", "client-mutation",
            "افزودن آیتم", "Add order item", "إضافة بند",
            "یک آیتم (محصول + واریانت + تعداد) به پیش‌نویس سفارش اضافه می‌کند.",
            "Adds an item (product + variant + quantity) to the order draft.",
            "يضيف بنداً (منتج + متغير + كمية) إلى مسودة الطلب.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("productId", "Guid", "Body", true),
                In("productVariantId", "Guid", "Body", true),
                In("quantity", "decimal", "Body", true),
                In("notes", "string", "Body", false),
                In("version", "long", "Body", true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "5", "PUT", "/client/orders/{id}/items/{itemId}", "ClientOrderWrite", "client-mutation",
            "ویرایش آیتم", "Update order item", "تحرير بند",
            "مقدار و یادداشت‌های یک آیتم موجود را با کنترل نسخه به‌روزرسانی می‌کند.",
            "Updates quantity and notes of an existing item with version control.",
            "يحدّث الكمية والملاحظات لبند موجود مع التحكم بالإصدار.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("itemId", "Guid", "Path", true),
                In("productId", "Guid", "Body", true),
                In("productVariantId", "Guid", "Body", true),
                In("quantity", "decimal", "Body", true),
                In("notes", "string", "Body", false),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("orders", "6", "POST", "/client/orders/{id}/items/{itemId}/remove", "ClientOrderWrite", "client-mutation",
            "حذف آیتم", "Remove order item", "إزالة بند",
            "آیتم را از پیش‌نویس حذف می‌کند؛ حذف نیازمند شماره نسخه جاری است.",
            "Removes an item from the draft; removal requires the current version number.",
            "يزيل بنداً من المسودة؛ تتطلب الإزالة رقم الإصدار الحالي.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("itemId", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("orders", "7", "POST", "/client/orders/{id}/items/{itemId}/print-operations", "ClientOrderWrite", "client-mutation",
            "افزودن عملیات چاپ", "Add print operation", "إضافة عملية طباعة",
            "یک عملیات چاپ (ترتیب، محل، روش، ابعاد، طرح یا فایل) برای آیتم ثبت می‌کند.",
            "Registers a print operation (sequence, placement, method, dimensions, artwork/file) for the item.",
            "يسجل عملية طباعة (الترتيب، الموضع، الطريقة، الأبعاد، التصميم أو الملف) للبند.",
        Inputs(
                In("id", "Guid", "Path", true),
                In("itemId", "Guid", "Path", true),
                In("sequence", "int", "Body", true),
                In("printPositionCode", "string", "Body", true),
                In("printMethodCode", "string", "Body", false),
                In("width", "decimal", "Body", false),
                In("height", "decimal", "Body", false),
                In("artworkVersionId", "Guid", "Body", false),
                In("fileVersionId", "Guid", "Body", false),
                In("notes", "string", "Body", false),
                In("version", "long", "Body", true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "8", "PUT", "/client/orders/{id}/items/{itemId}/print-operations/{operationId}", "ClientOrderWrite", "client-mutation",
            "ویرایش عملیات چاپ", "Update print operation", "تحرير عملية الطباعة",
            "عملیات چاپ ثبت‌شده را ویرایش می‌کند.",
            "Edits a registered print operation.",
            "يحرر عملية الطباعة المسجلة.",
        Inputs(
                In("id", "Guid", "Path", true),
                In("itemId", "Guid", "Path", true),
                In("operationId", "Guid", "Path", true),
                In("sequence", "int", "Body", true),
                In("printPositionCode", "string", "Body", true),
                In("printMethodCode", "string", "Body", false),
                In("width", "decimal", "Body", false),
                In("height", "decimal", "Body", false),
                In("artworkVersionId", "Guid", "Body", false),
                In("fileVersionId", "Guid", "Body", false),
                In("notes", "string", "Body", false),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("orders", "9", "POST", "/client/orders/{id}/pricing/preview", "ClientPricingPreview", "client-read",
            "پیش‌نمایش قیمت", "Pricing preview", "معاينة السعر",
            "بدون تغییر سفارش، قیمت نهایی را با تعرفه‌ها و تخفیف‌های جاری محاسبه و جزئیات هر مؤلفه را برمی‌گرداند.",
            "Computes the final price with current tariffs and discounts without modifying the order, returning each price component.",
            "يحسب السعر النهائي بالتعریفات والخصومات الحالية دون تعديل الطلب ويعيد تفاصيل كل مكوّن.",
        Inputs(
                In("id", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "10", "POST", "/client/orders/{id}/submit", "ClientOrderWrite", "submit + هدر Idempotency-Key",
            "ثبت نهایی سفارش", "Submit order", "إرسال الطلب",
            "سفارش کامل‌شده را برای ورود به گردش کار تأیید ثبت می‌کند؛ پس از ثبت، سفارش قفل می‌شود.",
            "Submits the completed order into the approval workflow; after submission the order is locked.",
            "يرسل الطلب المكتمل إلى سير عمل الموافقة؛ بعد الإرسال يُقفل الطلب.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("version", "long", "Body", true),
                In("Idempotency-Key", "string", "Header", true, true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "11", "POST", "/client/orders/{id}/cancel", "ClientOrderWrite", "client-mutation",
            "لغو سفارش", "Cancel order", "إلغاء الطلب",
            "سفارش را با کد دلیل لغو می‌کند؛ دلیل در تاریخچه سفارش ثبت می‌شود.",
            "Cancels the order with a reason code; the reason is stored in the order history.",
            "يلغي الطلب برمز سبب؛ يُسجل السبب في تاريخ الطلب.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("reasonCode", "string", "Body", true),
                In("reasonText", "string", "Body", false),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("orders", "12", "GET", "/client/orders/{id}/tracking", "ClientTrackingRead", "client-read",
            "رهگیری سفارش", "Order tracking", "تتبع الطلب",
            "وضعیت سفارش، مرسولات (شماره، حمل‌کننده، کد رهگیری، تاریخ) و رویدادهای خط زمانی را برای نمایش به مشتری نهایی برمی‌گرداند.",
            "Returns order status, shipments (number, carrier, tracking code, dates) and timeline events for display to the end customer.",
            "يعيد حالة الطلب والشحنات وأحداث الخط الزمني للعرض على العميل النهائي.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("customers", "13", "POST", "/client/customers/register", "ClientCustomerWrite", "client-mutation + هدر Idempotency-Key",
            "ثبت مشتری جدید", "Register customer", "تسجيل عميل",
            "مشتری حقیقی یا حقوقی را با کد یکتا ثبت می‌کند؛ با Idempotency-Key ثبت تکراری جلوگیری می‌شود.",
            "Registers an individual or corporate customer with a unique code; idempotency prevents duplicate registration.",
            "يسجل عميلاً فرداً أو شركة برمز فريد؛ ويمنع التسجيل المكرر.",
            Inputs(
                In("customerCode", "string", "Body", true),
                In("customerType", "string", "Body", true),
                In("displayName", "string", "Body", false),
                In("firstName", "string", "Body", false),
                In("lastName", "string", "Body", false),
                In("legalName", "string", "Body", false),
                In("nationalId", "string", "Body", false),
                In("primaryMobile", "string", "Body", false),
                In("primaryEmail", "string", "Body", false),
                In("customerGroupCode", "string", "Body", false),
                In("priceGroupCode", "string", "Body", false),
                In("legalEntityTypeId", "Guid", "Body", false),
                In("notes", "string", "Body", false),
                In("Idempotency-Key", "string", "Header", true, true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("customers", "14", "GET", "/client/customers/{id}", "ClientCustomerRead", "client-read",
            "مشاهده مشتری", "Get customer", "عرض العميل",
            "پروفایل کامل مشتری را برمی‌گرداند.",
            "Returns the full customer profile.",
            "يعيد ملف العميل الكامل.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("customers", "15", "PUT", "/client/customers/{id}", "ClientCustomerWrite", "client-mutation",
            "ویرایش مشتری", "Update customer", "تحرير العميل",
            "فیلدهای پروفایل مشتری را با کنترل نسخه (Version + ProfileVersion) به‌روزرسانی می‌کند.",
            "Updates customer profile fields with version control (Version + ProfileVersion).",
            "يحدّث حقول ملف العميل مع التحكم بالإصدار.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("displayName", "string", "Body", false),
                In("firstName", "string", "Body", false),
                In("lastName", "string", "Body", false),
                In("nationalId", "string", "Body", false),
                In("primaryMobile", "string", "Body", false),
                In("customerGroupCode", "string", "Body", false),
                In("priceGroupCode", "string", "Body", false),
                In("version", "long", "Body", true),
                In("profileVersion", "long", "Body", true),
                In("status", "string", "Body", false)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("customers", "16", "GET", "/client/customers/{id}/addresses", "ClientCustomerRead", "client-read",
            "فهرست نشانی‌های مشتری", "List customer addresses", "قائمة عناوين العميل",
            "نشانی‌های فعال مشتری (با نوع: دفتر، انبار، ارسال) را برمی‌گرداند؛ برای انتخاب نشانی ارسال سفارش استفاده می‌شود.",
            "Returns the customer's active addresses (typed: HQ, warehouse, shipping); used when picking an order shipping address.",
            "يعيد عناوين العميل النشطة؛ تستخدم لاختيار عنوان الشحن للطلب.",
            Inputs(In("id", "Guid", "Path", true)),
            ListOut, "X-Company-ID", false),

        N("catalog", "17", "GET", "/client/catalog/products", "ClientCatalogRead", "client-read",
            "جستجوی محصولات", "Search products", "بحث في المنتجات",
            "فهرست محصولات قابل سفارش با جستجو و صفحه‌بندی؛ برای انتخاب آیتم در سفارش موبایل/وب استفاده می‌شود.",
            "Lists orderable products with search and pagination; used for item selection in the mobile/web order flow.",
            "يسرد المنتجات القابلة للطلب مع بحث وترقيم؛ تستخدم لاختيار البنود في الطلب.",
            Inputs(
                In("search", "string", "Query", false),
                In("page", "int", "Query", false),
                In("pageSize", "int", "Query", false)),
            ListOut, "X-Company-ID", false),

        N("catalog", "18", "GET", "/client/catalog/products/{id}/variants", "ClientCatalogRead", "client-read",
            "واریانت‌های محصول", "Product variants", "متغيرات المنتج",
            "واریانت‌های قابل سفارش یک محصول (سایز/رنگ) را برمی‌گرداند.",
            "Returns the orderable variants (size/color) of a product.",
            "يعيد متغيرات المنتج القابلة للطلب (مقاس/لون).",
            Inputs(In("id", "Guid", "Path", true)),
            ListOut, "X-Company-ID", false),

        N("files", "19", "POST", "/client/files/upload", "ClientFileUpload", "upload + هدر Idempotency-Key",
            "آپلود فایل طرح", "Upload design file", "رفع ملف التصميم",
            "فایل طرح را (multipart/form-data) آپلود می‌کند؛ با Idempotency-Key آپلود مجدد همان فایل، همان فایل قبلی را برمی‌گرداند. فایل وارد بانک طرح می‌شود و بندانگشتی می‌گیرد.",
            "Uploads a design file (multipart/form-data); with Idempotency-Key, re-uploading the same file returns the existing one. The file enters the design bank and gets a thumbnail.",
            "يرفع ملف التصميم (multipart)؛ مع الترويسة يعيد الرفع المكرر نفس الملف. يدخل الملف بنك التصاميم ويُنشأ له مصغرة.",
            Inputs(
                In("Idempotency-Key", "string", "Header", true, true),
                In("file", "file", "Form", true),
                In("sha256", "string", "Form", false)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("public", "20", "GET", "/public/catalog", "—", "عمومی — OutputCache 60 ثانیه",
            "کاتالوگ عمومی طرح‌ها", "Public catalog", "الكتالوج العام",
            "طرح‌های منتشرشده (Active) را با جستجو و صفحه‌بندی برای وب‌سایت B2C برمی‌گرداند؛ هر آیتم URL مستقیم بندانگشتی و پیش‌نمایش واترمارک‌دار دارد.",
            "Returns published (active) designs with search and pagination for the B2C website; each item has direct thumbnail and watermarked-preview URLs.",
            "يعيد التصاميم المنشورة مع البحث وترقيم الصفحات لموقع B2C؛ كل عنصر له مصغرة ومعاينة بعلامة مائية.",
            Inputs(
                In("search", "string", "Query", false),
                In("page", "int", "Query", false),
                In("pageSize", "int", "Query", false)),
            JsonOut, "بدون هدر (عمومی)", true),

        N("public", "21", "GET", "/public/catalog/{id}", "—", "عمومی — OutputCache 60 ثانیه",
            "جزئیات طرح عمومی", "Public design detail", "تفاصيل التصميم",
            "جزئیات یک طرح منتشرشده شامل توضیح و URL های تصاویر برای صفحه محصول B2C.",
            "Details of one published design including description and image URLs for the B2C product page.",
            "تفاصيل تصميم منشور واحد بما فيها الوصف وروابط الصور لصفحة المنتج.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "بدون هدر (عمومی)", true)
    ];

    private static ServiceInfo N(string groupId, string sort, string method, string route, string permission, string limiter,
        string nameFa, string nameEn, string nameAr, string descFa, string descEn, string descAr,
        ServiceInput[] inputs, string output, string headers, bool publicAccess)
        => new(groupId, int.Parse(sort, System.Globalization.CultureInfo.InvariantCulture), method, route, permission, limiter,
            nameFa, nameEn, nameAr, descFa, descEn, descAr, inputs, output, headers, publicAccess);

    private static ServiceInput[] Inputs(params ServiceInput[] inputs) => inputs;
}
