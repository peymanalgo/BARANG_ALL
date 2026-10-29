using System.Globalization;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

CultureInfo[] cultures = [new("fa"), new("en"), new("ar")];

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/account/login";
        o.AccessDeniedPath = "/account/login";
        o.ExpireTimeSpan = TimeSpan.FromHours(12);
        o.SlidingExpiration = true;
        o.Cookie.Name = "barang.store";
        o.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization(o => o.AddPolicy("LoggedIn", p => p.RequireAuthenticatedUser()));
builder.Services.AddOutputCache(o => o.AddPolicy("catalog60", b => b.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("page", "pageSize", "search", "culture")));
builder.Services.AddHttpClient("core", c => c.BaseAddress = new Uri(builder.Configuration["CoreApi:BaseUrl"] ?? "http://localhost:8080/").PlusTrailingSlash());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CoreApiClient>();
builder.Services.AddScoped<CustomerContext>();
builder.Services.AddRequestLocalization(o =>
{
    o.SetDefaultCulture("fa").AddSupportedCultures("fa", "en", "ar").AddSupportedUICultures("fa", "en", "ar");
    o.RequestCultureProviders = [new CookieRequestCultureProvider { CookieName = "barang.store.culture" }];
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new("fa"),
    SupportedCultures = cultures,
    SupportedUICultures = cultures,
    RequestCultureProviders = [new CookieRequestCultureProvider { CookieName = "barang.store.culture" }]
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

file static class UriExtensions
{
    public static Uri PlusTrailingSlash(this Uri uri) => uri.AbsolutePath.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
}
