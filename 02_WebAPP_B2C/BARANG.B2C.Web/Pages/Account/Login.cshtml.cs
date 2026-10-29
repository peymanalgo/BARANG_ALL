using System.Globalization;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed class LoginModel(CoreApiClient core) : PageModel
{
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; private set; }
    public string? ReturnUrl { get; private set; }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = SafeUrl(returnUrl);
        SetCultureFromQuery();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        SetCultureFromQuery();
        ReturnUrl = SafeUrl(returnUrl);
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = CultureName() switch { "en" => "Enter username and password.", "ar" => "أدخل اسم المستخدم وكلمة المرور.", _ => "نام کاربری و کلمه عبور را وارد کنید." };
            return Page();
        }
        CoreApiClient.LoginOutcome outcome = await core.LoginAsync(Username.Trim(), Password);
        if (!outcome.Ok)
        {
            Error = CultureName() switch { "en" => "Wrong username or password.", "ar" => "اسم المستخدم أو كلمة المرور غير صحيحة.", _ => "نام کاربری یا کلمه عبور اشتباه است." };
            return Page();
        }
        return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/account" : ReturnUrl);
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
