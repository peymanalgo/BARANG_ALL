using System.Text.Json;
using BARANG.Backoffice.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BARANG.Backoffice.Api;

/// <summary>وضعیت فیچرهای سیستم برای UI پنل (مثلاً مخفی‌کردن دستیار هوشمند ERP).
/// GET /api/v1/features?scope=erp را با سشن کاربر می‌خواند؛ خطا/بی‌مجوزی = پیش‌فرض روشن (fail-open).</summary>
public sealed class FeatureFlagsService(ICoreApiClient core, AuthenticationStateProvider authState)
{
    // پیش‌بارگذاری یک‌باره برای منو/صفحه‌ها: همه کدهای روشنِ یک اسکوپ با کش ۳۰ثانیه‌ای (به تفکیک شرکت+اسکوپ).
    private static readonly Dictionary<string, (DateTimeOffset LoadedAt, HashSet<string> Codes)> Preloaded = new(StringComparer.Ordinal);
    private static readonly TimeSpan PreloadTtl = TimeSpan.FromSeconds(30);

    public async Task<bool> IsEnabledAsync(string featureCode, string scope = "erp")
    {
        try
        {
            var user = (await authState.GetAuthenticationStateAsync()).User;
            if (user.Identity?.IsAuthenticated != true
                || !Guid.TryParse(user.FindFirst(AuthClaimTypes.CompanyId)?.Value, out Guid company)
                || user.FindFirst(AuthClaimTypes.AccessToken)?.Value is not string token)
                return true;
            var result = await core.GetAsync($"api/v1/features?scope={Uri.EscapeDataString(scope)}", token, company, default);
            if (!result.IsSuccess || result.Value.ValueKind != JsonValueKind.Array) return true;
            foreach (JsonElement item in result.Value.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("featureCode", out JsonElement code)
                    && code.ValueKind == JsonValueKind.String
                    && string.Equals(code.GetString(), featureCode, StringComparison.OrdinalIgnoreCase)
                    && item.TryGetProperty("isEnabled", out JsonElement enabled)
                    && enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    return enabled.GetBoolean();
            return true;
        }
        catch { return true; }
    }

    /// <summary>همه کدهای روشن یک اسکوپ را یک‌بار می‌خواند (کش ۳۰ثانیه‌ای؛ خطا = null ⇒ fail-open همه روشن).
    /// برای چک‌های منوی MainLayout که نباید به‌ازای هر آیتم یک درخواست بزنند.</summary>
    public async Task<HashSet<string>?> EnabledCodesAsync(string scope = "erp")
    {
        try
        {
            var user = (await authState.GetAuthenticationStateAsync()).User;
            if (user.Identity?.IsAuthenticated != true
                || !Guid.TryParse(user.FindFirst(AuthClaimTypes.CompanyId)?.Value, out Guid company)
                || user.FindFirst(AuthClaimTypes.AccessToken)?.Value is not string token)
                return null;
            string key = scope + ":" + company;
            if (Preloaded.TryGetValue(key, out var hit) && hit.LoadedAt > DateTimeOffset.UtcNow.Subtract(PreloadTtl))
                return hit.Codes;
            HashSet<string>? codes = null;
            var result = await core.GetAsync($"api/v1/features?scope={Uri.EscapeDataString(scope)}", token, company, default);
            if (result.IsSuccess && result.Value.ValueKind == JsonValueKind.Array)
            {
                codes = new(StringComparer.OrdinalIgnoreCase);
                foreach (JsonElement item in result.Value.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("featureCode", out JsonElement code) && code.ValueKind == JsonValueKind.String
                        && item.TryGetProperty("isEnabled", out JsonElement enabled) && enabled.GetBoolean())
                        codes.Add(code.GetString()!);
            }
            Preloaded[key] = (DateTimeOffset.UtcNow, codes ?? []);
            return codes; // null = fail-open
        }
        catch { return null; }
    }

    /// <summary>محافظ ارزان صفحه: فیچر خاموش باشد → ریدایرکت به «/» و false برمی‌گرداند (سطح منو/ریدایرکت).</summary>
    public async Task<bool> RedirectIfDisabledAsync(NavigationManager navigation, string featureCode, string scope = "erp")
    {
        if (await IsEnabledAsync(featureCode, scope)) return true;
        navigation.NavigateTo("/", forceLoad: true);
        return false;
    }
}
