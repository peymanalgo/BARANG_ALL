using System.Globalization;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace BARANG.B2C.Web.Pages.Account;

// Two-step self registration: (1) first/last name + email or mobile,
// (2) the OTP code. Delivery is mocked for now — the Core API accepts the
// fixed development code 1111. On success the visitor is signed in and sent
// to /account/complete-profile to fill the remaining mandatory profile data.
public sealed class RegisterModel(CoreApiClient core) : PageModel
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^0\d{10}$", RegexOptions.Compiled);

    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string LastName { get; set; } = "";
    [BindProperty] public string Target { get; set; } = "";
    [BindProperty] public string Code { get; set; } = "";

    public bool CodeSent { get; private set; }
    public string? MaskedTarget { get; private set; }
    public string? Error { get; private set; }
    public bool AlreadyRegistered { get; private set; }
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
        string culture = CultureName();
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            Error = culture switch { "en" => "Enter your first and last name.", "ar" => "أدخل الاسم والعائلة.", _ => "نام و نام خانوادگی را وارد کنید." };
            return Page();
        }
        if (!IsValidTarget(Target.Trim()))
        {
            Error = culture switch { "en" => "Enter a valid email address or 11-digit mobile number (e.g. 09121234567).", "ar" => "أدخل بريدًا إلكترونيًا صحيحًا أو رقم هاتف مكونًا من 11 رقمًا.", _ => "یک ایمیل معتبر یا شماره موبایل ۱۱ رقمی (مثلاً ۰۹۱۲۱۲۳۴۵۶۷) وارد کنید." };
            return Page();
        }
        CoreApiClient.LoginOutcome sent = await core.OtpRequestAsync(Target.Trim());
        if (!sent.Ok)
        {
            Error = culture switch { "en" => "Could not send the verification code. Try again.", "ar" => "لم يتم إرسال رمز التحقق. حاول مرة أخرى.", _ => "ارسال کد تأیید ناموفق بود. دوباره تلاش کنید." };
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
        string culture = CultureName();
        CodeSent = true;
        MaskedTarget = Mask(Target.Trim());
        if (string.IsNullOrWhiteSpace(Code))
        {
            Error = culture switch { "en" => "Enter the verification code.", "ar" => "أدخل رمز التحقق.", _ => "کد تأیید را وارد کنید." };
            return Page();
        }
        CoreApiClient.LoginOutcome outcome = await core.OtpRegisterAsync(Target.Trim(), Code.Trim(), FirstName.Trim(), LastName.Trim());
        if (!outcome.Ok)
        {
            if (outcome.ProblemCode == "auth.already_registered")
            {
                AlreadyRegistered = true;
                Error = culture switch { "en" => "This email/mobile is already registered. Sign in instead.", "ar" => "هذا البريد/الرقم مسجل بالفعل. سجّل الدخول بدلاً من ذلك.", _ => "این ایمیل/شماره قبلاً ثبت شده است. وارد شوید." };
            }
            else
            {
                Error = culture switch { "en" => "The verification code is wrong.", "ar" => "رمز التحقق غير صحيح.", _ => "کد تأیید نادرست است." };
            }
            return Page();
        }
        return LocalRedirect("/account/complete-profile");
    }

    private bool IsValidTarget(string value)
        => EmailPattern.IsMatch(value) || PhonePattern.IsMatch(NormalizeDigits(value));

    // Persian/Arabic digits are accepted and normalized to Latin before checks.
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
