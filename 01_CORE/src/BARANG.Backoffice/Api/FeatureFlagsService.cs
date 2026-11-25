using System.Text.Json;
using BARANG.Backoffice.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace BARANG.Backoffice.Api;

/// <summary>وضعیت فیچرهای سیستم برای UI پنل (مثلاً مخفی‌کردن دستیار هوشمند ERP).
/// GET /api/v1/features?scope=erp را با سشن کاربر می‌خواند؛ خطا/بی‌مجوزی = پیش‌فرض روشن (fail-open).</summary>
public sealed class FeatureFlagsService(ICoreApiClient core, AuthenticationStateProvider authState)
{
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
}
