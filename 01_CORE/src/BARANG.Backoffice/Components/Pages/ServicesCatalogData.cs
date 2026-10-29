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
    ServiceInput[] Inputs, string Output, string Headers, bool PublicAccess, bool BearerOnly = false)
{
    public const string BaseUrl = "/api/v1";
    public string Name(string culture) => culture switch { "en" => NameEn, "ar" => NameAr, _ => NameFa };
    public string FullRoute => BaseUrl + Route;
    public string Auth(string culture) => PublicAccess
        ? culture switch { "en" => "None (public endpoint)", "ar" => "بدون مصادقة (عام)", _ => "بدون احراز هویت (عمومی)" }
        : BearerOnly ? "Bearer Token"
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
        new("catalog", "کاتالوگ و مرجع", "Catalog & Reference", "الكتالوج والمرجع"),
        new("files", "فایل‌ها", "Files", "الملفات"),
        new("public", "کاتالوگ عمومی (B2C)", "Public Catalog (B2C)", "الكتالوج العام"),
        new("notifications", "اعلان‌ها", "Notifications", "الإشعارات"),
        new("auth", "احراز هویت و حساب کاربری", "Auth & Account", "المصادقة والحساب"),
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

        N("orders", "2", "GET", "/client/orders", "ClientOrderRead", "client-read",
            "لیست سفارش‌های مشتری", "List customer orders", "قائمة طلبات العميل",
            "سفارش‌های مشتری را با فیلتر مشتری، شماره سفارش و وضعیت و با صفحه‌بندی برمی‌گرداند؛ مشتری فهرست سفارش‌های خود و مرحله فعلی هرکدام را می‌بیند.",
            "Lists customer orders with customer, order-number and status filters plus pagination; the customer sees their orders and each order's current stage.",
            "يسرد طلبات العميل مع فلاتر العميل ورقم الطلب والحالة وترقيم الصفحات؛ يرى العميل طلباته ومرحلة كل طلب.",
            Inputs(
                In("customerId", "Guid", "Query", false),
                In("status", "string", "Query", false),
                In("search", "string", "Query", false),
                In("page", "int", "Query", false),
                In("pageSize", "int", "Query", false)),
            "JSON: صفحه‌ای از خلاصه سفارش‌ها (items + total)", "X-Company-ID", false),

        N("orders", "3", "GET", "/client/orders/{id}", "ClientOrderRead", "client-read",
            "مشاهده سفارش", "Get order", "عرض الطلب",
            "جزئیات کامل سفارش شامل آیتم‌ها، عملیات چاپ، وضعیت و قیمت را برمی‌گرداند.",
            "Returns full order details including items, print operations, status and pricing.",
            "يعيد تفاصيل الطلب كاملة بما فيها البنود وعمليات الطباعة والحالة والسعر.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("orders", "4", "PUT", "/client/orders/{id}", "ClientOrderWrite", "client-mutation",
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

        N("orders", "5", "POST", "/client/orders/{id}/items", "ClientOrderWrite", "client-mutation",
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

        N("orders", "6", "PUT", "/client/orders/{id}/items/{itemId}", "ClientOrderWrite", "client-mutation",
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

        N("orders", "7", "POST", "/client/orders/{id}/items/{itemId}/remove", "ClientOrderWrite", "client-mutation",
            "حذف آیتم", "Remove order item", "إزالة بند",
            "آیتم را از پیش‌نویس حذف می‌کند؛ حذف نیازمند شماره نسخه جاری است.",
            "Removes an item from the draft; removal requires the current version number.",
            "يزيل بنداً من المسودة؛ تتطلب الإزالة رقم الإصدار الحالي.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("itemId", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("orders", "8", "POST", "/client/orders/{id}/items/{itemId}/print-operations", "ClientOrderWrite", "client-mutation",
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

        N("orders", "9", "PUT", "/client/orders/{id}/items/{itemId}/print-operations/{operationId}", "ClientOrderWrite", "client-mutation",
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

        N("orders", "10", "POST", "/client/orders/{id}/pricing/preview", "ClientPricingPreview", "client-read",
            "پیش‌نمایش قیمت", "Pricing preview", "معاينة السعر",
            "بدون تغییر سفارش، قیمت نهایی را با تعرفه‌ها و تخفیف‌های جاری محاسبه و جزئیات هر مؤلفه را برمی‌گرداند.",
            "Computes the final price with current tariffs and discounts without modifying the order, returning each price component.",
            "يحسب السعر النهائي بالتعریفات والخصومات الحالية دون تعديل الطلب ويعيد تفاصيل كل مكوّن.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "11", "POST", "/client/orders/{id}/submit", "ClientOrderWrite", "submit + هدر Idempotency-Key",
            "ثبت نهایی سفارش", "Submit order", "إرسال الطلب",
            "سفارش کامل‌شده را برای ورود به گردش کار تأیید ثبت می‌کند؛ پس از ثبت، سفارش قفل می‌شود.",
            "Submits the completed order into the approval workflow; after submission the order is locked.",
            "يرسل الطلب المكتمل إلى سير عمل الموافقة؛ بعد الإرسال يُقفل الطلب.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("version", "long", "Body", true),
                In("Idempotency-Key", "string", "Header", true, true)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("orders", "12", "POST", "/client/orders/{id}/cancel", "ClientOrderWrite", "client-mutation",
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

        N("orders", "13", "GET", "/client/orders/{id}/tracking", "ClientTrackingRead", "client-read",
            "رهگیری سفارش", "Order tracking", "تتبع الطلب",
            "وضعیت سفارش، مرسولات (شماره، حمل‌کننده، کد رهگیری، تاریخ) و رویدادهای خط زمانی را برای نمایش به مشتری نهایی برمی‌گرداند.",
            "Returns order status, shipments (number, carrier, tracking code, dates) and timeline events for display to the end customer.",
            "يعيد حالة الطلب والشحنات وأحداث الخط الزمني للعرض على العميل النهائي.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("orders", "14", "GET", "/client/orders/{id}/validate", "ClientOrderRead", "client-read",
            "اعتبارسنجی سفارش", "Validate order", "التحقق من الطلب",
            "قبل از ثبت نهایی، نقص‌های سفارش (آیتم خالی، نشانی، عملیات چاپ و…) را فهرست می‌کند تا رابط کاربری B2C بتواند قبل از Submit به مشتری نشان دهد.",
            "Before submission, lists order deficiencies (missing items, address, print operations, …) so the B2C UI can show them to the customer prior to Submit.",
            "قبل من الإرسال، يسرد نقص الطلب (البنود، العنوان، عمليات الطباعة…) ليعرضها مطقم B2C على العميل.",
            Inputs(In("id", "Guid", "Path", true)),
            ListOut, "X-Company-ID", false),

        N("orders", "15", "GET", "/client/orders/form", "ClientOrderRead", "client-read",
            "فراداده فرم سفارش", "Order form metadata", "بيانات نموذج الطلب",
            "مقادیر مجاز فرم سفارش را برمی‌گرداند: محل‌های چاپ مجاز (FRONT/BACK/SLEEVE/OTHER)، انواع مشتری و انواع نشانی؛ برای ساخت فرم سفارش در وب و موبایل.",
            "Returns allowed order-form values: permitted print placements (FRONT/BACK/SLEEVE/OTHER), customer types and address types; used to build the web/mobile order form.",
            "يعيد القيم المسموحة لنموذج الطلب: مواضع الطباعة، أنواع العملاء وأنواع العناوين؛ لبناء نموذج الطلب في الويب والهاتف.",
            Inputs(),
            JsonOut, "X-Company-ID", false),

        N("customers", "16", "POST", "/client/customers/register", "ClientCustomerWrite", "client-mutation + هدر Idempotency-Key",
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

        N("customers", "17", "GET", "/client/customers/by-code/{code}", "ClientCustomerRead", "client-read",
            "مشاهده مشتری با کد", "Get customer by code", "عرض العميل بالرمز",
            "مشتری را با کد یکتا پیدا می‌کند؛ برای ورود مشتری به پورتال B2C و یافتن پروفایل او استفاده می‌شود.",
            "Finds a customer by its unique code; used by the B2C portal to resolve the signed-in customer's profile.",
            "يجد العميل برمزه الفريد؛ تستخدمه بوابة B2C لتحديد ملف العميل بعد تسجيل الدخول.",
            Inputs(In("code", "string", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("customers", "18", "GET", "/client/customers/{id}", "ClientCustomerRead", "client-read",
            "مشاهده مشتری", "Get customer", "عرض العميل",
            "پروفایل کامل مشتری را برمی‌گرداند.",
            "Returns the full customer profile.",
            "يعيد ملف العميل الكامل.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("customers", "19", "PUT", "/client/customers/{id}", "ClientCustomerWrite", "client-mutation",
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

        N("customers", "20", "GET", "/client/customers/{id}/addresses", "ClientCustomerRead", "client-read",
            "فهرست نشانی‌های مشتری", "List customer addresses", "قائمة عناوين العميل",
            "نشانی‌های فعال مشتری (با نوع: دفتر، انبار، ارسال) را برمی‌گرداند؛ برای انتخاب نشانی ارسال سفارش استفاده می‌شود.",
            "Returns the customer's active addresses (typed: HQ, warehouse, shipping); used when picking an order shipping address.",
            "يعيد عناوين العميل النشطة؛ تستخدم لاختيار عنوان الشحن للطلب.",
            Inputs(In("id", "Guid", "Path", true)),
            ListOut, "X-Company-ID", false),

        N("customers", "21", "POST", "/client/customers/{id}/addresses", "ClientCustomerWrite", "client-mutation",
            "افزودن نشانی", "Add address", "إضافة عنوان",
            "یک نشانی جدید (کشور، شهر، خط نشانی، گیرنده و…) برای مشتری ثبت می‌کند؛ مشتری B2C از همین سرویس نشانی ارسال خود را در پنل کاربری اضافه می‌کند.",
            "Registers a new address (country, city, address line, recipient, …) for the customer; B2C customers use it to add shipping addresses in their account panel.",
            "يسجل عنواناً جديداً (البلد، المدينة، العنوان، المستلم…) للعميل؛ يستخدمه عميل B2C لإضافة عناوين الشحن في لوحته.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("addressTypeCode", "string", "Body", true),
                In("title", "string", "Body", true),
                In("countryCode", "string", "Body", true),
                In("provinceCode", "string", "Body", false),
                In("cityCode", "string", "Body", false),
                In("district", "string", "Body", false),
                In("addressLine1", "string", "Body", true),
                In("addressLine2", "string", "Body", false),
                In("postalCode", "string", "Body", false),
                In("recipientName", "string", "Body", false),
                In("recipientPhone", "string", "Body", false),
                In("recipientMobile", "string", "Body", false),
                In("isDefault", "bool", "Body", true),
                In("displayOrder", "int", "Body", true),
                In("email", "string", "Body", false),
                In("notes", "string", "Body", false)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("customers", "22", "PUT", "/client/customers/{id}/addresses/{addressId}", "ClientCustomerWrite", "client-mutation",
            "ویرایش نشانی", "Update address", "تحرير العنوان",
            "فیلدهای یک نشانی موجود را با کنترل نسخه به‌روزرسانی می‌کند.",
            "Updates the fields of an existing address with version control.",
            "يحدّث حقول عنوان موجود مع التحكم بالإصدار.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("addressId", "Guid", "Path", true),
                In("addressTypeCode", "string", "Body", true),
                In("title", "string", "Body", true),
                In("countryCode", "string", "Body", true),
                In("addressLine1", "string", "Body", true),
                In("recipientName", "string", "Body", false),
                In("recipientMobile", "string", "Body", false),
                In("isDefault", "bool", "Body", true),
                In("displayOrder", "int", "Body", true),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("customers", "23", "POST", "/client/customers/{id}/addresses/{addressId}/remove", "ClientCustomerWrite", "client-mutation",
            "حذف نشانی", "Remove address", "إزالة العنوان",
            "نشانی را از پروفایل مشتری حذف می‌کند؛ نیازمند شماره نسخه جاری است.",
            "Deletes an address from the customer profile; requires the current version number.",
            "يحذف عنواناً من ملف العميل؛ يتطلب رقم الإصدار الحالي.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("addressId", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("customers", "24", "POST", "/client/customers/{id}/addresses/{addressId}/default", "ClientCustomerWrite", "client-mutation",
            "نشانی پیش‌فرض", "Set default address", "العنوان الافتراضي",
            "نشانی انتخاب‌شده را به‌عنوان نشانی پیش‌فرض مشتری (برای تسویه‌حساب و ارسال) تنظیم می‌کند.",
            "Marks the selected address as the customer's default (for checkout and shipping).",
            "يعيّن العنوان المحدد كعنوان افتراضي للعميل (للدفع والشحن).",
            Inputs(
                In("id", "Guid", "Path", true),
                In("addressId", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            NoContent, "Idempotency-Key + X-Company-ID", false),

        N("catalog", "25", "GET", "/client/catalog/products", "ClientCatalogRead", "client-read",
            "جستجوی محصولات", "Search products", "بحث في المنتجات",
            "فهرست محصولات قابل سفارش با جستجو و صفحه‌بندی؛ برای انتخاب آیتم در سفارش موبایل/وب استفاده می‌شود.",
            "Lists orderable products with search and pagination; used for item selection in the mobile/web order flow.",
            "يسرد المنتجات القابلة للطلب مع بحث وترقيم؛ تستخدم لاختيار البنود في الطلب.",
            Inputs(
                In("search", "string", "Query", false),
                In("page", "int", "Query", false),
                In("pageSize", "int", "Query", false)),
            ListOut, "X-Company-ID", false),

        N("catalog", "26", "GET", "/client/catalog/products/{id}", "ClientCatalogRead", "client-read",
            "جزئیات محصول", "Product detail", "تفاصيل المنتج",
            "کامل‌ترین اطلاعات یک محصول (کد، نام، نوع، واحد، وضعیت) را برای صفحه محصول در وب/موبایل برمی‌گرداند.",
            "Returns the full product info (code, name, type, unit, status) for the product page in web/mobile.",
            "يعيد بيانات المنتج الكاملة (الرمز، الاسم، النوع، الوحدة، الحالة) لصفحة المنتج.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "X-Company-ID", false),

        N("catalog", "27", "GET", "/client/catalog/products/{id}/variants", "ClientCatalogRead", "client-read",
            "واریانت‌های محصول", "Product variants", "متغيرات المنتج",
            "واریانت‌های قابل سفارش یک محصول (سایز/رنگ) را برمی‌گرداند.",
            "Returns the orderable variants (size/color) of a product.",
            "يعيد متغيرات المنتج القابلة للطلب (مقاس/لون).",
            Inputs(In("id", "Guid", "Path", true)),
            ListOut, "X-Company-ID", false),

        N("catalog", "28", "GET", "/client/branches", "ClientOrderRead", "client-read",
            "شعبه‌های فعال", "Active branches", "الفروع النشطة",
            "شعبه‌های فعال شرکت را برای انتخاب شعبه در فرم سفارش برمی‌گرداند؛ فیلد branchId در ایجاد سفارش از همین لیست انتخاب می‌شود.",
            "Lists the company's active branches for branch selection on the order form; the order-create branchId is picked from this list.",
            "يسرد فروع الشركة النشطة لاختيار الفرع في نموذج الطلب؛ يُختار branchId من هذه القائمة.",
            Inputs(),
            ListOut, "X-Company-ID", false),

        N("catalog", "29", "GET", "/client/lookups/{typeCode}/options", "ClientCatalogRead", "client-read",
            "گزینه‌های داده مرجع", "Reference lookup options", "خيارات البيانات المرجعية",
            "گزینه‌های مرجعِ مجاز برای فرم‌های B2C را با whitelist برمی‌گرداند: روش ارسال (shipping.method)، حمل‌کننده (shipping.carrier)، نوع بسته (packaging.type)، نوع نشانی (customer.address.type) و دلیل لغو (order.cancellation.reason). سایر نوع‌ها 404 می‌دهند.",
            "Returns whitelisted reference options for B2C forms: shipping method, carrier, packaging type, address type and cancellation reason. Other type codes return 404.",
            "يعيد الخيارات المرجعية المسموحة لنماذج B2C: طريقة الشحن، الناقل، نوع التغليف، نوع العنوان وسبب الإلغاء. الأنواع الأخرى تعيد 404.",
            Inputs(
                In("typeCode", "string", "Path", true),
                In("culture", "string", "Query", false)),
            ListOut, "X-Company-ID", false),

        N("files", "30", "POST", "/client/files/upload", "ClientFileUpload", "upload + هدر Idempotency-Key",
            "آپلود فایل طرح", "Upload design file", "رفع ملف التصميم",
            "فایل طرح را (multipart/form-data) آپلود می‌کند؛ با Idempotency-Key آپلود مجدد همان فایل، همان فایل قبلی را برمی‌گرداند. فایل وارد بانک طرح می‌شود و بندانگشتی می‌گیرد.",
            "Uploads a design file (multipart/form-data); with Idempotency-Key, re-uploading the same file returns the existing one. The file enters the design bank and gets a thumbnail.",
            "يرفع ملف التصميم (multipart)؛ مع الترويسة يعيد الرفع المكرر نفس الملف. يدخل الملف بنك التصاميم ويُنشأ له مصغرة.",
            Inputs(
                In("Idempotency-Key", "string", "Header", true, true),
                In("file", "file", "Form", true),
                In("sha256", "string", "Form", false)),
            JsonOut, "Idempotency-Key + X-Company-ID", false),

        N("public", "31", "GET", "/public/catalog", "—", "عمومی — OutputCache 60 ثانیه",
            "کاتالوگ عمومی طرح‌ها", "Public catalog", "الكتالوج العام",
            "طرح‌های منتشرشده (Active) را با جستجو و صفحه‌بندی برای وب‌سایت B2C برمی‌گرداند؛ هر آیتم URL مستقیم بندانگشتی و پیش‌نمایش واترمارک‌دار دارد.",
            "Returns published (active) designs with search and pagination for the B2C website; each item has direct thumbnail and watermarked-preview URLs.",
            "يعيد التصاميم المنشورة مع البحث وترقيم الصفحات لموقع B2C؛ كل عنصر له مصغرة ومعاينة بعلامة مائية.",
            Inputs(
                In("search", "string", "Query", false),
                In("page", "int", "Query", false),
                In("pageSize", "int", "Query", false)),
            JsonOut, "بدون هدر (عمومی)", true),

        N("public", "32", "GET", "/public/catalog/{id}", "—", "عمومی — OutputCache 60 ثانیه",
            "جزئیات طرح عمومی", "Public design detail", "تفاصيل التصميم",
            "جزئیات یک طرح منتشرشده شامل توضیح و URL های تصاویر برای صفحه محصول B2C.",
            "Details of one published design including description and image URLs for the B2C product page.",
            "تفاصيل تصميم منشور واحد بما فيها الوصف وروابط الصور لصفحة المنتج.",
            Inputs(In("id", "Guid", "Path", true)),
            JsonOut, "بدون هدر (عمومی)", true),

        N("public", "33", "GET", "/public/catalog/concept-search", "—", "عمومی — client-read",
            "جستجوی مفهومی طرح‌ها", "Concept (semantic) search", "البحث المفهومي للتصاميم",
            "جستجوی هوشمند طرح‌ها بر اساس مفهوم عبارت: کد، عنوان، نام دسته و برچسب‌ها امتیازدهی می‌شوند و مشابه‌ترین طرح‌های منتشرشده با درصد تطابق و تصاویر برمی‌گردند؛ موتور جستجوی سایت B2C و موبایل.",
            "Smart design search by concept: code, title, category name and tags are scored and the most similar published designs are returned with a match score and images; powers B2C web/mobile search.",
            "بحث ذكي للتصاميم حسب المفهوم: يتم تقييم الرمز والعنوان واسم الفئة والوسوم وإعادة أكثر التصاميم المنشورة تشابهاً مع نسبة تطابق وصور؛ محرك بحث موقع B2C والتطبيق.",
            Inputs(
                In("q", "string", "Query", true),
                In("limit", "int", "Query", false)),
            "JSON: آرایه طرح‌های مشابه با score", "بدون هدر (عمومی)", true),

        N("notifications", "33", "GET", "/notifications/mine", "NotificationRead", "—",
            "اعلان‌های من", "My notifications", "إشعاراتي",
            "اعلان‌های کاربر جاری را با فیلتر خوانده‌نشده، دسته، اولویت و بازه زمانی و با صفحه‌بندی برمی‌گرداند؛ اپ موبایل/وب تغییر وضعیت سفارش را از همین‌جا نشان می‌دهد.",
            "Lists the current user's notifications with unread/category/priority/date filters and pagination; the mobile/web app surfaces order status changes from here.",
            "يسرد إشعارات المستخدم الحالي مع فلاتر غير المقروء والفئة والأولوية والتاريخ وترقيم الصفحات.",
            Inputs(
                In("unread", "bool", "Query", false),
                In("search", "string", "Query", false),
                In("category", "string", "Query", false),
                In("priority", "string", "Query", false),
                In("actionable", "bool", "Query", false),
                In("fromUtc", "date", "Query", false),
                In("toUtc", "date", "Query", false),
                In("page", "int", "Query", false),
                In("pageSize", "int", "Query", false)),
            "JSON: صفحه‌ای از اعلان‌ها (items + total)", "Authorization: Bearer Token", false, true),

        N("notifications", "34", "POST", "/notifications/{id}/read", "NotificationRead", "—",
            "خوانده‌شدن اعلان", "Mark notification read", "تعليم الإشعار مقروءاً",
            "یک اعلان را خوانده‌شده علامت می‌زند؛ نیازمند شماره نسخه است.",
            "Marks a single notification as read; requires the version number.",
            "يعلّم إشعاراً واحداً مقروءاً؛ يتطلب رقم الإصدار.",
            Inputs(
                In("id", "Guid", "Path", true),
                In("version", "long", "Body", true)),
            NoContent, "Authorization: Bearer Token", false, true),

        N("notifications", "35", "POST", "/notifications/read-all", "NotificationRead", "—",
            "خواندن همه اعلان‌ها", "Mark all read", "تعليم الكل مقروءاً",
            "تمام اعلان‌های خوانده‌نشده کاربر جاری را خوانده‌شده علامت می‌زند.",
            "Marks all of the current user's unread notifications as read.",
            "يعلّم جميع إشعارات المستخدم غير المقروءة كمقروءة.",
            Inputs(),
            NoContent, "Authorization: Bearer Token", false, true),

        N("auth", "36", "POST", "/auth/login", "—", "عمومی — RateLimiter login",
            "ورود و دریافت توکن", "Login and issue token", "تسجيل الدخول وإصدار الرمز",
            "با نام کاربری و کلمه عبور ورود می‌کند و توکن Bearer برای فراخوانی سایر سرویس‌ها صادر می‌کند؛ همان سرویسی که وب‌سایت B2C و اپلیکیشن موبایل برای ورود کاربران استفاده می‌کنند. مدیریت حساب‌ها و کلمه عبور در بک‌آفیس (مدیریت کاربران و دسترسی‌ها) انجام می‌شود.",
            "Authenticates with username and password and issues the Bearer token used to call all other services; the same login the B2C website and mobile app use. Accounts and passwords are managed in the backoffice (User & access management).",
            "يوثّق باسم المستخدم وكلمة المرور ويصدر رمز Bearer لاستدعاء الخدمات الأخرى؛ وهي نفس الخدمة التي يستخدمها موقع B2C وتطبيق الهاتف. تُدار الحسابات وكلمات المرور من لوحة التحكم.",
            Inputs(
                In("username", "string", "Body", true),
                In("password", "string", "Body", true)),
            "JSON: کاربر + توکن Bearer", "بدون هدر", true),

        N("auth", "37", "POST", "/auth/change-password", "—", "RateLimiter login",
            "تغییر کلمه عبور", "Change password", "تغيير كلمة المرور",
            "کاربر واردشده با تأیید کلمه عبور فعلی، رمز جدید تنظیم می‌کند؛ پس از تغییر، همه نشست‌ها (از جمله نشست جاری) باطل می‌شود و ورود مجدد لازم است. سیاست رمز (طول، حروف بزرگ/کوچک، رقم) اعمال می‌شود.",
            "Lets the signed-in user set a new password after verifying the current one; all sessions (including the current one) are revoked afterwards and re-login is required. Password policy (length, cases, digit) is enforced.",
            "يتيح للمستخدم المسجّل تعيين كلمة مرور جديدة بعد التحقق من الحالية؛ تُبطل جميع الجلسات (بما فيها الحالية) ويلزم تسجيل الدخول مجدداً. تُطبق سياسة كلمة المرور.",
            Inputs(
                In("currentPassword", "string", "Body", true),
                In("newPassword", "string", "Body", true)),
            NoContent, "Authorization: Bearer Token", false, true),

        N("auth", "38", "POST", "/auth/logout", "—", "—",
            "خروج و ابطال توکن", "Logout and revoke token", "تسجيل الخروج وإبطال الرمز",
            "توکن جاری را باطل می‌کند؛ برای خروج امن کاربر از وب‌سایت یا اپلیکیشن.",
            "Revokes the current token for a secure sign-out from the website or app.",
            "يبطل الرمز الحالي لتسجيل خروج آمن من الموقع أو التطبيق.",
            Inputs(),
            NoContent, "Authorization: Bearer Token", false, true),

        N("auth", "39", "GET", "/auth/me", "—", "—",
            "پروفایل کاربر جاری", "Current user profile", "ملف المستخدم الحالي",
            "اطلاعات کاربر واردشده (نام، نام کاربری، وضعیت فعال بودن) را برمی‌گرداند.",
            "Returns the signed-in user's info (name, username, active status).",
            "يعيد بيانات المستخدم المسجّل (الاسم واسم المستخدم وحالة التنشيط).",
            Inputs(),
            JsonOut, "Authorization: Bearer Token", false, true),

        N("auth", "40", "GET", "/auth/me/companies", "—", "—",
            "شرکت‌های کاربر جاری", "Current user companies", "شركات المستخدم الحالي",
            "فهرست شرکت‌های مجاز کاربر برای انتخاب شرکت جاری؛ در سامانه چندشرکتی برای انتخاب زمینه (Company-ID) استفاده می‌شود.",
            "Lists the user's authorized companies for picking the active company context (Company-ID) in the multi-company setup.",
            "يسرد شركات المستخدم المصرح بها لاختيار سياق الشركة النشط (Company-ID) في النظام متعدد الشركات.",
            Inputs(),
            "JSON: آرایه شرکت‌های مجاز", "Authorization: Bearer Token", false, true)
    ];

    private static ServiceInfo N(string groupId, string sort, string method, string route, string permission, string limiter,
        string nameFa, string nameEn, string nameAr, string descFa, string descEn, string descAr,
        ServiceInput[] inputs, string output, string headers, bool publicAccess, bool bearerOnly = false)
        => new(groupId, int.Parse(sort, System.Globalization.CultureInfo.InvariantCulture), method, route, permission, limiter,
            nameFa, nameEn, nameAr, descFa, descEn, descAr, inputs, output, headers, publicAccess, bearerOnly);

    private static ServiceInput[] Inputs(params ServiceInput[] inputs) => inputs;
}
