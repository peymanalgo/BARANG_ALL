using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

// دستیار هوشمند مشتری — فقط نقش BFF دارد (الگوی ویزارد/سبد خرید):
// مشتری و شناسه‌اش فقط سمت سرور از کوکی احراز هویت حل می‌شود (هرگز از ورودی مرورگر)،
// پیام به Core (/api/v1/client/ai/assistant) پاس داده می‌شود و سهمیه/خطاها برگردانده می‌شوند.
public sealed class AiChatModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    private string Culture() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

    // ── پرسش: POST /ai-chat?handler=Ask {message, culture} ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostAskAsync([FromBody] JsonElement body)
    {
        if (User.Identity?.IsAuthenticated != true) return Auth();
        string message = Text(body, "message").Trim();
        if (message.Length == 0 || message.Length > 2000) return new JsonResult(new { ok = false, problem = "invalid" });
        string culture = Text(body, "culture") is { Length: > 0 } cul ? cul : Culture();

        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid))
            return new JsonResult(new { ok = false, problem = "auth" });
        if (core.SawUnauthorized) return Auth();

        (bool ok, JsonElement? value, string? problem) = await core.PostRawWithBodyAsync("api/v1/client/ai/assistant", new
        {
            message,
            culture,
            resetConversation = false
        });
        if (!ok)
        {
            // توکن منقضی یا مشتری بدون پروفایل → همان گیت ورود
            if (problem == "ai.access_denied" || core.SawUnauthorized) return Auth();
            return new JsonResult(new { ok = false, problem = problem ?? "unavailable" });
        }
        if (value is not JsonElement reply) return new JsonResult(new { ok = false, problem = "unavailable" });
        // فیچرهای سیستم: «دستیار هوشمند» در تب فیچرهای پی‌رنگ خاموش است — پرچم + پاسخ چهارزبانه از Core
        bool featureDisabled = Text(reply, "error") == "feature_disabled";
        if (featureDisabled) return new JsonResult(new
        {
            ok = false,
            featureDisabled = true,
            problem = "feature_disabled",
            reply = Text(reply, "reply") is { Length: > 0 } r ? r
                : Culture() is "fa" ? "دستیار هوشمند در حال حاضر غیرفعال است."
                : Culture() is "ar" ? "المساعد الذكي معطّل حاليًا."
                : Culture() is "es" ? "El asistente inteligente está desactivado en este momento."
                : "The AI assistant is currently disabled.",
        });
        return new JsonResult(new
        {
            ok = true,
            reply = Text(reply, "reply"),
            error = Text(reply, "error") is { Length: > 0 } e ? e : null,
            notConfigured = reply.TryGetProperty("notConfigured", out JsonElement nc) && nc.ValueKind == JsonValueKind.True,
            quotaUsed = reply.TryGetProperty("quotaUsed", out JsonElement qu) && qu.TryGetInt32(out int q) ? q : 0,
            quotaLimit = reply.TryGetProperty("quotaLimit", out JsonElement ql) && ql.TryGetInt32(out int l) ? l : 10,
        });
    }

    // ── تاریخچه + سهمیه: GET /ai-chat?handler=History ──
    public async Task<IActionResult> OnGetHistoryAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return Auth();
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement _)) return Auth();
        JsonElement? result = await core.GetAsync("api/v1/client/ai/conversation");
        if (core.SawUnauthorized) return Auth();
        if (result is not JsonElement reply) return new JsonResult(new { ok = false, problem = "unavailable" });
        List<object> history = [];
        if (reply.TryGetProperty("history", out JsonElement h) && h.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in h.EnumerateArray())
            {
                string role = Text(item, "role");
                if (role is not ("user" or "assistant")) continue;
                history.Add(new
                {
                    role,
                    content = Text(item, "content"),
                    direction = Text(item, "direction") is { Length: > 0 } d ? d : (Culture() is "fa" or "ar" ? "rtl" : "ltr"),
                });
            }
        }
        return new JsonResult(new
        {
            ok = true,
            history,
            quotaUsed = reply.TryGetProperty("quotaUsed", out JsonElement qu) && qu.TryGetInt32(out int q) ? q : 0,
            quotaLimit = reply.TryGetProperty("quotaLimit", out JsonElement ql) && ql.TryGetInt32(out int l) ? l : 10,
        });
    }

    // ── پاک‌کردن گفتگو: POST /ai-chat?handler=Clear ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostClearAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return Auth();
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement _)) return Auth();
        (bool ok, string? problem) = await core.PostRawAsync("api/v1/client/ai/conversation/clear", new { });
        if (!ok && (problem == "ai.access_denied" || core.SawUnauthorized)) return Auth();
        return new JsonResult(new { ok, problem = ok ? null : problem ?? "unavailable" });
    }

    // بدون هندلر (GET /ai-chat) → صفحه اصلی؛ این آدرس فقط برای فراخوانی‌های هندلر است
    public IActionResult OnGet() => Redirect("/");

    private static JsonResult Auth() => new(new { ok = false, problem = "auth" });

    private static string Text(JsonElement x, string name) => x.ValueKind == JsonValueKind.Object && x.TryGetProperty(name, out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
}
