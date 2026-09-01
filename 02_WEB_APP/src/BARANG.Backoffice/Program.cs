using System.Globalization;
using BARANG.Backoffice.Api;
using BARANG.Backoffice.Auth;
using BARANG.Backoffice.Components;
using BARANG.Backoffice.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.DataProtection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
string keyPath = Path.GetFullPath(builder.Configuration["DataProtection:KeysPath"] ?? ".keys", builder.Environment.ContentRootPath);
Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection().SetApplicationName("BARANG.Backoffice").PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocaleDateService,LocaleDateService>();
builder.Services.Configure<CoreApiOptions>(builder.Configuration.GetSection(CoreApiOptions.Section));
builder.Services.AddHttpClient<ICoreApiClient, CoreApiClient>((services, client) =>
{
    CoreApiOptions options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<CoreApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "BARANG.Backoffice.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login?message=unauthorized";
    options.SlidingExpiration = false;
});
builder.Services.AddAuthorizationBuilder().AddPolicy(AuthPolicies.CompanySelected, policy =>
    policy.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.CompanyId));
builder.Services.AddCascadingAuthenticationState();

WebApplication app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("fa"),
    SupportedCultures = LocalePolicy.Supported.Select(code => new CultureInfo(code)).ToArray(),
    SupportedUICultures = LocalePolicy.Supported.Select(code => new CultureInfo(code)).ToArray(),
    RequestCultureProviders = [new CookieRequestCultureProvider()]
});
app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapBackofficeAuthEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public partial class Program;
