using System.Text.Json;

namespace BARANG.B2C.Web.Services;

// Notification summaries are stored in Core as template codes (e.g. "order.submitted");
// the customer-facing pages render them in the active UI culture here, using the
// notification payload for arguments like the order number.
public static class NotificationText
{
    public static string Localize(string culture, string? templateCode, string? payloadJson)
    {
        string code = templateCode?.Trim() ?? "";
        string arg = ExtractOrderNumber(payloadJson) ?? "";
        string T(string fa, string en, string ar) => culture switch { "en" => en, "ar" => ar, _ => fa };
        return code switch
        {
            "order.submitted" => string.Format(T("سفارش {0} ثبت شد و وارد مراحل تولید شد.", "Order {0} was submitted and entered production.", "تم إرسال الطلب {0} ودخل مرحلة الإنتاج."), arg),
            "order.cancelled" => string.Format(T("سفارش {0} لغو شد.", "Order {0} was cancelled.", "تم إلغاء الطلب {0}."), arg),
            "order.status.changed" => string.Format(T("وضعیت سفارش {0} به‌روزرسانی شد.", "The status of order {0} was updated.", "تم تحديث حالة الطلب {0}."), arg),
            "shipment.dispatched" => string.Format(T("مرسوله سفارش {0} تحویل شرکت پست شد.", "The shipment for order {0} was handed to the carrier.", "سُلمت شحنة الطلب {0} إلى الناقل."), arg),
            "shipment.delivered" => string.Format(T("سفارش {0} تحویل داده شد.", "Order {0} was delivered.", "تم تسليم الطلب {0}."), arg),
            "order.price.approval_requested" => T("قیمت سفارش شما در انتظار تأیید است.", "Your order price is awaiting approval.", "سعر طلبك في انتظار الموافقة."),
            "workflow.task.assigned" => T("یک کار جدید برای سفارش شما ثبت شد.", "A new task was registered for your order.", "تم تسجيل مهمة جديدة لطلبك."),
            _ => code.Length > 0 ? code : T("اعلان", "Notification", "إشعار")
        };
    }

    private static string? ExtractOrderNumber(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payloadJson);
            // args live either at the root or nested under "payload"
            foreach (JsonElement root in new[] { doc.RootElement,
                doc.RootElement.TryGetProperty("payload", out JsonElement p) && p.ValueKind == JsonValueKind.Object ? p : default })
            {
                if (root.ValueKind != JsonValueKind.Object) continue;
                foreach (string key in new[] { "orderNumber", "OrderNumber", "shipmentNumber", "ShipmentNumber" })
                {
                    if (root.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.String)
                    {
                        string? s = v.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                    }
                }
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
