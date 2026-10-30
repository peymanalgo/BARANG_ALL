using System.Globalization;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;

CultureInfo[] cultures = [new("fa"), new("en"), new("ar")];

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();
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
builder.Services.AddOutputCache(o => o.AddPolicy("catalog60", b => b.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("page", "pageSize", "search", "culture", "mode", "category")));
builder.Services.AddHttpClient("core", c => c.BaseAddress = new Uri(builder.Configuration["CoreApi:BaseUrl"] ?? "http://localhost:8080/").PlusTrailingSlash());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CoreApiClient>();
builder.Services.AddScoped<CustomerContext>();
builder.Services.AddRequestLocalization(o =>
{
    o.SetDefaultCulture("fa").AddSupportedCultures("fa", "en", "ar").AddSupportedUICultures("fa", "en", "ar");
    o.RequestCultureProviders = [new CookieRequestCultureProvider { CookieName = "barang.store.culture" }];
});

builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
    app.UseHttpsRedirection();
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new("fa"),
    SupportedCultures = cultures,
    SupportedUICultures = cultures,
    // ?culture=xx on any URL switches instantly; the cookie keeps the choice afterwards
    RequestCultureProviders = [new QueryStringRequestCultureProvider(), new CookieRequestCultureProvider { CookieName = "barang.store.culture" }]
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
// Same-origin proxy for artwork thumbnails: lets the "see your t-shirt" canvas
// engine read pixels without MinIO CORS/cache pitfalls. Only the configured
// public file endpoint host may pass through.
app.MapGet("/design-image", async (string u, HttpContext http, IConfiguration cfg, IHttpClientFactory factory, CancellationToken ct) =>
{
    var allowedHosts = (cfg["FileStorage:PublicEndpoint"] ?? "127.0.0.1:9000")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (!Uri.TryCreate(u, UriKind.Absolute, out var uri)
        || uri.Scheme is not ("http" or "https")
        || !allowedHosts.Any(a => uri.Authority.Equals(a, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.BadRequest();
    }
    var client = factory.CreateClient("core");
    using var response = await client.GetAsync(uri.ToString(), HttpCompletionOption.ResponseHeadersRead, ct);
    if (!response.IsSuccessStatusCode) return Results.NotFound();
    var bytes = await response.Content.ReadAsByteArrayAsync(ct);
    var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
    http.Response.Headers.CacheControl = "public, max-age=86400";
    return Results.File(bytes, contentType);
});
app.Run();

file static class UriExtensions
{
    public static Uri PlusTrailingSlash(this Uri uri) => uri.AbsolutePath.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
}
