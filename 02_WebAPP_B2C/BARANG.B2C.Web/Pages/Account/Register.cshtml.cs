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
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? InviteCode { get; set; }

    public bool CodeSent { get; private set; }
    public string? MaskedTarget { get; private set; }
    public string? Error { get; private set; }
    public bool AlreadyRegistered { get; private set; }
    public string? ReturnUrl { get; private set; }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = SafeUrl(returnUrl);
        SetCultureFromQuery();
        RememberInvite();
    }
    private void RememberInvite()
    {
        string code = (InviteCode ?? Request.Query["ref"].ToString() ?? "").Trim().ToUpperInvariant();
        if (code.Length is >= 4 and <= 20)
            Response.Cookies.Append("barang.ref", code, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), IsEssential = true });
    }

    public async Task<IActionResult> OnPostRequestCodeAsync(string? returnUrl)
    {
        SetCultureFromQuery();
        ReturnUrl = SafeUrl(returnUrl);
        RememberInvite();
        string culture = CultureName();
        Target = NormalizeDigits(Target).Trim(); // ارقام فارسی/عربی → لاتین
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            Error = culture switch { "en" => "Enter your first and last name.", "ar" => "أدخل الاسم والعائلة.", "es" => "Introduce tu nombre y apellido.", _ => "نام و نام خانوادگی را وارد کنید." };
            return Page();
        }
        if (!IsValidTarget(Target.Trim()))
        {
            Error = culture switch { "en" => "Enter a valid email address or 11-digit mobile number (e.g. 09121234567).", "ar" => "أدخل بريدًا إلكترونيًا صحيحًا أو رقم هاتف مكونًا من 11 رقمًا.", "es" => "Introduce un correo válido o un número de móvil de 11 dígitos (p. ej. 09121234567).", _ => "یک ایمیل معتبر یا شماره موبایل ۱۱ رقمی (مثلاً ۰۹۱۲۱۲۳۴۵۶۷) وارد کنید." };
            return Page();
        }
        CoreApiClient.LoginOutcome sent = await core.OtpRequestAsync(Target.Trim());
        if (!sent.Ok)
        {
            Error = culture switch { "en" => "Could not send the verification code. Try again.", "ar" => "لم يتم إرسال رمز التحقق. حاول مرة أخرى.", "es" => "No se pudo enviar el código de verificación. Inténtalo de nuevo.", _ => "ارسال کد تأیید ناموفق بود. دوباره تلاش کنید." };
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
        Target = NormalizeDigits(Target).Trim();
        CodeSent = true;
        MaskedTarget = Mask(Target.Trim());
        if (string.IsNullOrWhiteSpace(Code))
        {
            Error = culture switch { "en" => "Enter the verification code.", "ar" => "أدخل رمز التحقق.", "es" => "Introduce el código de verificación.", _ => "کد تأیید را وارد کنید." };
            return Page();
        }
        // رمز عبور اجباری در ثبت‌نام — همین رمز بعداً برای ورود بدون OTP استفاده می‌شود
        if (Password.Length < 9 || !Password.Any(char.IsUpper) || !Password.Any(char.IsLower) || !Password.Any(char.IsDigit))
        {
            Error = culture switch
            {
                "en" => "Password must be at least 9 characters and include an uppercase letter, a lowercase letter and a digit.",
                "ar" => "يجب أن تكون كلمة المرور 9 أحرف على الأقل وتتضمن حرفًا كبيرًا وحرفًا صغيرًا ورقمًا.",
                "es" => "La contraseña debe tener al menos 9 caracteres e incluir una mayúscula, una minúscula y un dígito.",
                _ => "رمز عبور باید حداقل ۹ کاراکتر و شامل حرف بزرگ، حرف کوچک و رقم باشد."
            };
            return Page();
        }
        if (Password != ConfirmPassword)
        {
            Error = culture switch
            {
                "en" => "Password confirmation does not match.",
                "ar" => "تأكيد كلمة المرور غير مطابق.",
                "es" => "La confirmación de la contraseña no coincide.",
                _ => "تکرار رمز عبور با رمز عبور یکسان نیست."
            };
            return Page();
        }
        CoreApiClient.LoginOutcome outcome = await core.OtpRegisterAsync(Target.Trim(), Code.Trim(), FirstName.Trim(), LastName.Trim(), Password);
        if (!outcome.Ok)
        {
            if (outcome.ProblemCode == "auth.already_registered")
            {
                AlreadyRegistered = true;
                Error = culture switch { "en" => "This email/mobile is already registered. Sign in instead.", "ar" => "هذا البريد/الرقم مسجل بالفعل. سجّل الدخول بدلاً من ذلك.", "es" => "Este correo/número ya está registrado. Inicia sesión.", _ => "این ایمیل/شماره قبلاً ثبت شده است. وارد شوید." };
            }
            else if (outcome.ProblemCode == "auth.password_policy")
            {
                Error = culture switch
                {
                    "en" => "Password must be at least 9 characters and include an uppercase letter, a lowercase letter and a digit.",
                    "ar" => "يجب أن تكون كلمة المرور 9 أحرف على الأقل وتتضمن حرفًا كبيرًا وحرفًا صغيرًا ورقمًا.",
                    "es" => "La contraseña debe tener al menos 9 caracteres e incluir una mayúscula, una minúscula y un dígito.",
                    _ => "رمز عبور باید حداقل ۹ کاراکتر و شامل حرف بزرگ، حرف کوچک و رقم باشد."
                };
            }
            else
            {
                Error = culture switch { "en" => "The verification code is wrong.", "ar" => "رمز التحقق غير صحيح.", "es" => "El código de verificación es incorrecto.", _ => "کد تأیید نادرست است." };
            }
            return Page();
        }
        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/account/complete-profile" : "/account/complete-profile?returnUrl=" + Uri.EscapeDataString(ReturnUrl));
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
            if (name is "fa" or "en" or "ar" or "es") Response.Cookies.Append("barang.store.culture", CookieRequestCultureProvider.MakeCookieValue(new(name)));
        }
    }

    private string CultureName() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
    private string? SafeUrl(string? url) => Url.IsLocalUrl(url) ? url : null;
}
