using System.Globalization;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace BARANG.B2C.Web.Pages.Account;

// Sign-in via OTP code (mocked in Core: fixed dev code 1111) OR via email/mobile
// + password (customers set the password at sign-up; admin can reset it in the
// ERP without the old password). If the account exists but no B2C customer
// profile does yet, the visitor is sent to /account/complete-profile first.
public sealed class LoginModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^0\d{10}$", RegexOptions.Compiled);

    [BindProperty] public string Target { get; set; } = "";
    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? LoginMethod { get; set; }

    public bool CodeSent { get; private set; }
    public string? MaskedTarget { get; private set; }
    public string? Error { get; private set; }
    public string? ReturnUrl { get; private set; }

    // سوییچ روش ورود با حفظ فرهنگ و returnUrl
    public string OtpSwitchUrl => "/account/login?LoginMethod=otp" + QuerySuffix();
    public string PasswordSwitchUrl => "/account/login?LoginMethod=password" + QuerySuffix();
    private string QuerySuffix() =>
        (string.IsNullOrWhiteSpace(ReturnUrl) ? "" : "&returnUrl=" + Uri.EscapeDataString(ReturnUrl))
        + (string.IsNullOrWhiteSpace(Target) ? "" : "&target=" + Uri.EscapeDataString(Target));

    public void OnGet(string? returnUrl, string? loginMethod)
    {
        ReturnUrl = SafeUrl(returnUrl);
        LoginMethod = loginMethod;
        SetCultureFromQuery();
    }

    public async Task<IActionResult> OnPostRequestCodeAsync(string? returnUrl)
    {
        SetCultureFromQuery();
        ReturnUrl = SafeUrl(returnUrl);
        Target = NormalizeDigits(Target).Trim(); // ارقام فارسی/عربی → لاتین (همان چیزی که در دیتابیس ذخیره است)
        if (!IsValidTarget(Target))
        {
            Error = CultureName() switch
            {
                "en" => "Enter a valid email address or 11-digit mobile number (e.g. 09121234567).",
                "ar" => "أدخل بريدًا إلكترونيًا صحيحًا أو رقم هاتف مكونًا من 11 رقمًا.", "es" => "Introduce un correo válido o un número de móvil de 11 dígitos (p. ej. 09121234567).",
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
                "ar" => "لم يتم إرسال رمز التحقق. حاول مرة أخرى.", "es" => "No se pudo enviar el código de verificación. Inténtalo de nuevo.",
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
        Target = NormalizeDigits(Target).Trim();
        CodeSent = true;
        MaskedTarget = Mask(Target.Trim());
        string culture = CultureName();
        if (string.IsNullOrWhiteSpace(Code))
        {
            Error = culture switch { "en" => "Enter the verification code.", "ar" => "أدخل رمز التحقق.", "es" => "Introduce el código de verificación.", _ => "کد تأیید را وارد کنید." };
            return Page();
        }
        CoreApiClient.LoginOutcome outcome = await core.OtpLoginAsync(Target.Trim(), Code.Trim());
        if (!outcome.Ok)
        {
            Error = outcome.ProblemCode == "auth.user_not_found"
                ? culture switch { "en" => "No account for this email/mobile — sign up first.", "ar" => "لا يوجد حساب لهذا البريد/الرقم — سجّل أولاً.", "es" => "No hay cuenta para este correo/número: regístrate primero.", _ => "برای این ایمیل/شماره حسابی نیست — اول ثبت‌نام کنید." }
                : culture switch { "en" => "The verification code is wrong.", "ar" => "رمز التحقق غير صحيح.", "es" => "El código de verificación es incorrecto.", _ => "کد تأیید نادرست است." };
            return Page();
        }
        // First login with an incomplete account: force profile completion.
        // پروفایل مشتری هنوز ساخته نشده: تکمیل اجباری اطلاعات، و بعد ادامه همان مسیر
        string profileTarget = "/account/complete-profile"
            + (string.IsNullOrWhiteSpace(ReturnUrl) ? "" : "?returnUrl=" + Uri.EscapeDataString(ReturnUrl));
        var profile = await customers.CurrentAsync();
        if (profile is null) return LocalRedirect(profileTarget);
        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/account" : ReturnUrl);
    }

    // ورود با رمز عبور (مشتریانی که هنگام ثبت‌نام رمز تعیین کرده‌اند یا ادمین برایشان ریست کرده است)
    public async Task<IActionResult> OnPostPasswordLoginAsync(string? returnUrl)
    {
        SetCultureFromQuery();
        ReturnUrl = SafeUrl(returnUrl);
        Target = NormalizeDigits(Target).Trim();
        string culture = CultureName();
        if (!IsValidTarget(Target) || string.IsNullOrWhiteSpace(Password))
        {
            Error = culture switch
            {
                "en" => "Enter your email/mobile and password.",
                "ar" => "أدخل البريد/الهاتف وكلمة المرور.", "es" => "Introduce tu correo/móvil y contraseña.",
                _ => "ایمیل/موبایل و رمز عبور را وارد کنید."
            };
            return Page();
        }
        CoreApiClient.LoginOutcome outcome = await core.PortalPasswordLoginAsync(Target, Password);
        if (!outcome.Ok)
        {
            Error = outcome.ProblemCode == "auth.user_not_found"
                ? culture switch { "en" => "No account for this email/mobile — sign up first.", "ar" => "لا يوجد حساب لهذا البريد/الرقم — سجّل أولاً.", "es" => "No hay cuenta para este correo/número: regístrate primero.", _ => "برای این ایمیل/شماره حسابی نیست — اول ثبت‌نام کنید." }
                : culture switch { "en" => "Email/mobile or password is incorrect.", "ar" => "البريد/الهاتف أو كلمة المرور غير صحيحة.", "es" => "El correo/número o la contraseña es incorrecta.", _ => "ایمیل/موبایل یا رمز عبور نادرست است." };
            return Page();
        }
        var profile = await customers.CurrentAsync();
        if (profile is null)
        {
            string profileTarget = "/account/complete-profile"
                + (string.IsNullOrWhiteSpace(ReturnUrl) ? "" : "?returnUrl=" + Uri.EscapeDataString(ReturnUrl));
            return LocalRedirect(profileTarget);
        }
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
            if (name is "fa" or "en" or "ar" or "es") Response.Cookies.Append("barang.store.culture", CookieRequestCultureProvider.MakeCookieValue(new(name)));
        }
    }

    private string CultureName() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
    private string? SafeUrl(string? url) => Url.IsLocalUrl(url) ? url : null;
}
