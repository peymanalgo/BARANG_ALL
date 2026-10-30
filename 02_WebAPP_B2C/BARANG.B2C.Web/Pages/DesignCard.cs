using System.Text.Json;

namespace BARANG.B2C.Web.Pages;

public sealed record DesignCard(Guid Id, string Code, string Title, string? CategoryCode, string? Thumbnail, string? Preview, decimal? FromPrice = null)
{
    public static (IReadOnlyList<DesignCard>, long) FromPage(JsonElement? page)
    {
        if (page is not JsonElement el || el.ValueKind != JsonValueKind.Object) return ([], 0);
        long total = el.TryGetProperty("total", out JsonElement t) && t.TryGetInt64(out long n) ? n : 0;
        JsonElement items = el.ValueKind == JsonValueKind.Array
            ? el
            : el.TryGetProperty("items", out JsonElement pi) && pi.ValueKind == JsonValueKind.Array ? pi : default;
        if (items.ValueKind != JsonValueKind.Array) return ([], total);
        List<DesignCard> cards = items.EnumerateArray().Select(x => new DesignCard(
            x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
            x.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "",
            x.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "" : "",
            x.TryGetProperty("categoryCode", out JsonElement cc) ? cc.GetString() : null,
            x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null,
            x.TryGetProperty("previewUrl", out JsonElement pv) ? pv.GetString() : null,
            x.TryGetProperty("fromPrice", out JsonElement fp) && fp.TryGetDecimal(out decimal fpd) ? fpd : (decimal?)null)).ToList();
        return (cards, total);
    }
}
