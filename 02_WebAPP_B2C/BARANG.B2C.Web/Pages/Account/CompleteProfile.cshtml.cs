using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace BARANG.B2C.Web.Pages.Account;

// Mandatory second step of self registration: after OTP sign-up the visitor
// must complete the full individual profile (national id, father name,
// birth date, gender, certificate number, place of issue, occupation,
// landline, email) before using the store. Everything is written to the Core
// database in one call via POST /client/customers/register.
[Authorize(Policy = "LoggedIn")]
public sealed class CompleteProfileModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    private static readonly Regex NationalIdPattern = new(@"^\d{10}$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string LastName { get; set; } = "";
    [BindProperty] public string Mobile { get; set; } = "";
    [BindProperty] public string NationalId { get; set; } = "";
    [BindProperty] public string FatherName { get; set; } = "";
    [BindProperty] public string BirthDate { get; set; } = "";
    [BindProperty] public string Gender { get; set; } = "";
    [BindProperty] public string BirthCertificateNumber { get; set; } = "";
    [BindProperty] public string PlaceOfIssue { get; set; } = "";
    [BindProperty] public string Occupation { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";

    public string? Error { get; private set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public bool Saved { get; private set; }
    public bool IsJalali { get; private set; }
    public string? BirthPlaceholder { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        IsJalali = AppDate.IsJalali(AppDate.Culture());
        BirthPlaceholder = IsJalali ? "1370/05/12" : "1991/08/03";
        if (await customers.CurrentAsync() is not null) return RedirectToPage("/Account/Index");
        string displayName = User.Identity?.Name ?? "";
        string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && string.IsNullOrWhiteSpace(FirstName)) FirstName = parts[0];
        if (parts.Length > 1 && string.IsNullOrWhiteSpace(LastName)) LastName = string.Join(' ', parts[1..]);
        string? username = User.FindFirst("barang:username")?.Value;
        if (!string.IsNullOrWhiteSpace(username) && Regex.IsMatch(username, @"^0\d{10}$")) Mobile = username;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsJalali = AppDate.IsJalali(AppDate.Culture());
        BirthPlaceholder = IsJalali ? "1370/05/12" : "1991/08/03";
        string culture = AppDate.Culture();
        if (new[] { FirstName, LastName, Mobile, NationalId, FatherName, BirthDate, Gender, BirthCertificateNumber, PlaceOfIssue, Occupation }.Any(string.IsNullOrWhiteSpace)
            || !EmailPattern.IsMatch(Email.Trim()))
        {
            Error = culture switch
            {
                "en" => "Please fill all the fields below (with a valid email).",
                "ar" => "يرجى إدخال جميع الحقول (مع بريد إلكتروني صحيح).",
                _ => "لطفاً همه فیلدها را (با ایمیل معتبر) کامل کنید."
            };
            return Page();
        }
        string mobile = NormalizeDigits(Mobile).Trim();
        if (!Regex.IsMatch(mobile, @"^0\d{10}$"))
        {
            Error = culture switch { "en" => "Enter an 11-digit mobile number (e.g. 09121234567).", "ar" => "أدخل رقم هاتف مكونًا من 11 رقمًا.", _ => "شماره موبایل ۱۱ رقمی وارد کنید." };
            return Page();
        }
        if (!NationalIdPattern.IsMatch(NormalizeDigits(NationalId).Trim()))
        {
            Error = culture switch { "en" => "National id must be exactly 10 digits.", "ar" => "الرقم الوطني يجب أن يكون 10 أرقام.", _ => "کد ملی باید ۱۰ رقم باشد." };
            return Page();
        }
        DateOnly? birth = ParseDate(BirthDate.Trim(), IsJalali);
        if (birth is null || birth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            Error = culture switch { "en" => "Enter a valid birth date.", "ar" => "أدخل تاريخ ميلاد صحيحًا.", _ => "تاریخ تولد معتبر وارد کنید." };
            return Page();
        }
        string first = FirstName.Trim(), last = LastName.Trim();
        string username = User.FindFirst("barang:username")?.Value ?? mobile;
        (bool ok, JsonElement? value, string? problem) = await core.PostRawWithBodyAsync("api/v1/client/customers/register", new
        {
            customerCode = username,
            customerType = "Individual",
            firstName = first,
            lastName = last,
            displayName = $"{first} {last}".Trim(),
            primaryMobile = mobile,
            primaryPhone = string.IsNullOrWhiteSpace(Phone) ? null : NormalizeDigits(Phone).Trim(),
            primaryEmail = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            nationalId = NormalizeDigits(NationalId).Trim(),
            fatherName = FatherName.Trim(),
            birthDate = birth.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            genderCode = Gender,
            birthCertificateNumber = NormalizeDigits(BirthCertificateNumber).Trim(),
            placeOfIssue = PlaceOfIssue.Trim(),
            occupation = Occupation.Trim()
        });
        if (!ok)
        {
            Error = problem == "customers.code_exists"
                ? culture switch { "en" => "A profile for this account already exists.", "ar" => "يوجد ملف لهذا الحساب بالفعل.", _ => "برای این حساب قبلاً پروفایل ساخته شده است." }
                : culture switch { "en" => "Saving the profile failed. Check the entered values.", "ar" => "فشل حفظ الملف. تحقق من القيم.", _ => "ذخیره پروفایل ناموفق بود. مقادیر را بررسی کنید." };
            return Page();
        }
        if (value is JsonElement created && created.TryGetProperty("id", out JsonElement id))
        {
            string? refCode = Request.Cookies["barang.ref"];
            if (!string.IsNullOrWhiteSpace(refCode))
            {
                await core.PostRawAsync($"api/v1/client/customers/{id.GetString()}/referrals/attach", new { code = refCode.Trim() });
                Response.Cookies.Delete("barang.ref");
            }
        }
        // پس از تکمیل اطلاعات، ادامه همان مسیری که کاربر در آن بود
        return LocalRedirect(SafeReturnUrl() ?? "/account");
    }

    private string? SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    // Accepts Jalali (fa) or Gregorian (en/ar) input in yyyy/MM/dd shape and
    // converts the Jalali one via the Persian calendar.
    private static DateOnly? ParseDate(string input, bool jalali)
    {
        string normalized = NormalizeDigits(input).Replace('-', '/');
        string[] parts = normalized.Split('/');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out int y) || !int.TryParse(parts[1], out int m) || !int.TryParse(parts[2], out int d)) return null;
        try
        {
            if (jalali)
            {
                PersianCalendar pc = new();
                DateTime gregorian = pc.ToDateTime(y, m, d, 0, 0, 0, 0);
                return new DateOnly(gregorian.Year, gregorian.Month, gregorian.Day);
            }
            return new DateOnly(y, m, d);
        }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    private static string NormalizeDigits(string value)
    {
        System.Text.StringBuilder sb = new(value.Length);
        foreach (char ch in value)
        {
            sb.Append(ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                _ => ch
            });
        }
        return sb.ToString();
    }
}
