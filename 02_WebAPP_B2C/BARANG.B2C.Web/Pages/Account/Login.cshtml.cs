using System.Globalization;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace BARANG.B2C.Web.Pages.Account;

// Sign-in is always via a one-time code sent to the customer's email or
// mobile (delivery mocked in Core: the fixed development code 1111 is
// accepted). If the account exists but no B2C customer profile does yet,
// the visitor is sent to /account/complete-profile first.
public sealed class LoginModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^0\d{10}$", RegexOptions.Compiled);

    [BindProperty] public string Target { get; set; } = "";
    [BindProperty] public string Code { get; set; } = "";

    public bool CodeSent { get; private set; }
    public string? MaskedTarget { get; private set; }
    public string? Error { get; private set; }
    public string? ReturnUrl { get; private set; }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = SafeUrl(returnUrl);
        SetCultureFromQuery();
    }

    public async Task<IActionResult> OnPostRequestCodeAsync(string? returnUrl)
    {
        SetCultureFromQuery();
        ReturnUrl = SafeUrl(returnUrl);
        if (!IsValidTarget(Target.Trim()))
        {
            Error = CultureName() switch
            {
                "en" => "Enter a valid email address or 11-digit mobile number (e.g. 09121234567).",
                "ar" => "أدخل بريدًا إلكترونيًا صحيحًا أو رقم هاتف مكونًا من 11 رقمًا.",
                _ => "یک ایمیل معتبر یا شماره موبایل ۱۱ رقمی (مثلاً ۰۹۱۲۱۲۳۴۵۶۷) وارد کنید."
            };
            return Page();
        }
        CoreApiClient.LoginOutcome sent = await core.OtpRequestAsync(Target.Trim());
        if (!sent.Ok)
        {
            Error = CultureName() switch
            {
                "en" => "Could not send the verification code. Try again.",
                "ar" => "لم يتم إرسال رمز التحقق. حاول مرة أخرى.",
                _ => "ارسال کد تأیید ناموفق بود. دوباره تلاش کنید."
            };
            return Page();
        }
        CodeSent = true;
        MaskedTarget = Mask(Target.Trim());
        return Page();
    }

    public async Task<IActionResult> OnPostVerifyAsync(string? returnUrl)
    {
        SetCultureFromQuery();
        ReturnUrl = SafeUrl(returnUrl);
        CodeSent = true;
        MaskedTarget = Mask(Target.Trim());
        string culture = CultureName();
        if (string.IsNullOrWhiteSpace(Code))
        {
            Error = culture switch { "en" => "Enter the verification code.", "ar" => "أدخل رمز التحقق.", _ => "کد تأیید را وارد کنید." };
            return Page();
        }
        CoreApiClient.LoginOutcome outcome = await core.OtpLoginAsync(Target.Trim(), Code.Trim());
        if (!outcome.Ok)
        {
            Error = outcome.ProblemCode == "auth.user_not_found"
                ? culture switch { "en" => "No account for this email/mobile — sign up first.", "ar" => "لا يوجد حساب لهذا البريد/الرقم — سجّل أولاً.", _ => "برای این ایمیل/شماره حسابی نیست — اول ثبت‌نام کنید." }
                : culture switch { "en" => "The verification code is wrong.", "ar" => "رمز التحقق غير صحيح.", _ => "کد تأیید نادرست است." };
            return Page();
        }
        // First login with an incomplete account: force profile completion.
        if (await customers.CurrentAsync() is null)
            return LocalRedirect("/account/complete-profile");
        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/account" : ReturnUrl);
    }

    private bool IsValidTarget(string value)
    {
        string normalized = NormalizeDigits(value);
        return EmailPattern.IsMatch(value) || PhonePattern.IsMatch(normalized);
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
        return sb.ToString().Replace(" ", "").Replace("-", "");
    }

    private static string Mask(string value)
    {
        if (value.Contains('@')) return value;
        return value.Length == 11 ? value[..4] + "***" + value[7..] : value;
    }

    private void SetCultureFromQuery()
    {
        if (Request.Query.TryGetValue("culture", out Microsoft.Extensions.Primitives.StringValues value))
        {
            string name = value.ToString();
            if (name is "fa" or "en" or "ar") Response.Cookies.Append("barang.store.culture", CookieRequestCultureProvider.MakeCookieValue(new(name)));
        }
    }

    private string CultureName() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
    private string? SafeUrl(string? url) => Url.IsLocalUrl(url) ? url : null;
}
