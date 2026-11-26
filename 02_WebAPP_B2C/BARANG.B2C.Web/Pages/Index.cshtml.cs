using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class IndexModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<DesignCard> Featured { get; private set; } = [];
    public IReadOnlyList<CategoryRow> Categories { get; private set; } = [];

    // نام نمایشی طرح: «سینما — طرح 18742651» → «سینما · #۲۶۵۱» (نام توصیفی + شماره کوتاه)
    public string CardTitle(string title) => Services.Display.Title(title);
    public string PriceFrom(decimal? from) => Services.Display.Money(from);



    public async Task OnGet([FromQuery(Name = "ref")] string? refCode)
    {
        if (!string.IsNullOrWhiteSpace(refCode) && refCode.Length is >= 4 and <= 20)
        {
            Response.Cookies.Append("barang.ref", refCode.Trim().ToUpperInvariant(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), IsEssential = true });
        }
        string homeCulture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        // دسته‌ها اول — برای تنوع «طرح‌های منتخب» از چند دسته پرفروش
        JsonElement? cats = await core.GetAsync($"api/v1/public/catalog/categories?culture={homeCulture}");
        if (cats is JsonElement c && c.ValueKind == JsonValueKind.Array)
        {
            Categories = c.EnumerateArray().Select(x => new CategoryRow(
                x.TryGetProperty("code", out JsonElement code) ? code.GetString() ?? "" : "",
                x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? "" : "",
                x.TryGetProperty("count", out JsonElement cnt) && cnt.TryGetInt32(out int n) ? n : 0))
                .Where(x => x.Code.Length > 0).ToList();
        }
        // تنوع منتخب‌ها: از ۶ دسته اول، ۲ طرح از هر دسته — صفحه اول API همه یک دسته است (مرتب بر اساس کد)
        List<DesignCard> mixed = [];
        foreach (CategoryRow cat in Categories.Where(x => x.Count > 0).Take(6))
        {
            JsonElement? catPage = await core.GetAsync($"api/v1/public/catalog?page=1&pageSize=2&categoryCode={Uri.EscapeDataString(cat.Code)}&culture={homeCulture}");
            (IReadOnlyList<DesignCard> rows, _) = DesignCard.FromPage(catPage);
            mixed.AddRange(rows);
            if (mixed.Count >= 12) break;
        }
        if (mixed.Count == 0)
        {
            JsonElement? page = await core.GetAsync($"api/v1/public/catalog?page=1&pageSize=12&culture={homeCulture}");
            (mixed, _) = DesignCard.FromPage(page) switch
            {
                (IReadOnlyList<DesignCard> l, long) l2 => (l2.Item1.ToList(), l2.Item2),
                _ => (new List<DesignCard>(), 0L)
            };
        }
        Featured = mixed.Take(12).ToList();
    }

    public sealed record CategoryRow(string Code, string DisplayName, int Count);

    public string CatName(string? code)
        => string.IsNullOrWhiteSpace(code) ? "" : Categories.FirstOrDefault(c => c.Code == code).DisplayName ?? code;
}
