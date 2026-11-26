using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class SidebarIdentityE2ETests : IAsyncLifetime
{
    private readonly string backofficeUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly string coreUrl = Environment.GetEnvironmentVariable("BARANG_E2E_CORE_URL") ?? "http://localhost:8080";
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;

    public async ValueTask InitializeAsync()
    {
        string username = Required("BARANG_E2E_USERNAME");
        string password = Required("BARANG_E2E_PASSWORD");
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")
        });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 900 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{backofficeUrl}/culture?culture=en&returnUrl=%2Flogin", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Fact]
    public async Task LinkedPersonAndSelectedCompanyAreAuthoritativeAndCompanySwitchUpdatesBothShellLocations()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        using HttpClient api = new() { BaseAddress = new Uri(coreUrl) };
        using HttpResponseMessage loginResponse = await api.PostAsJsonAsync("api/v1/auth/login", new
        {
            username = Required("BARANG_E2E_USERNAME"),
            password = Required("BARANG_E2E_PASSWORD")
        }, ct);
        loginResponse.EnsureSuccessStatusCode();
        using JsonDocument login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync(ct));
        string token = login.RootElement.GetProperty("accessToken").GetString()!;
        Guid userId = login.RootElement.GetProperty("user").GetProperty("id").GetGuid();

        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        JsonElement[] originalAccess = await api.GetFromJsonAsync<JsonElement[]>("api/v1/auth/me/companies", ct) ?? [];
        Assert.NotEmpty(originalAccess);
        Guid originalCompanyId = originalAccess[0].GetProperty("companyId").GetGuid();
        using HttpResponseMessage selectOriginal = await api.PostAsJsonAsync("api/v1/auth/me/company", new { companyId = originalCompanyId }, ct);
        selectOriginal.EnsureSuccessStatusCode();

        api.DefaultRequestHeaders.Add("X-Company-Id", originalCompanyId.ToString());
        JsonElement[] allCompanies = await api.GetFromJsonAsync<JsonElement[]>("api/v1/organization/companies", ct) ?? [];
        HashSet<Guid> existingAccess = originalAccess.Select(x => x.GetProperty("companyId").GetGuid()).ToHashSet();
        JsonElement target = allCompanies.First(x => x.GetProperty("isActive").GetBoolean() && !existingAccess.Contains(x.GetProperty("id").GetGuid()));
        Guid targetId = target.GetProperty("id").GetGuid();
        string targetName = target.GetProperty("displayName").GetString()!;

        bool granted = false;
        try
        {
            using HttpResponseMessage grant = await api.PostAsJsonAsync($"api/v1/security/users/{userId}/companies", new { companyId = targetId, validToUtc = (DateTimeOffset?)null }, ct);
            grant.EnsureSuccessStatusCode();
            granted = true;

            await page.GotoAsync($"{backofficeUrl}/company", new() { WaitUntil = WaitUntilState.NetworkIdle });
            ILocator option = page.Locator("button.company-option[type='submit']").Filter(new() { HasText = targetName });
            await Assertions.Expect(option).ToBeVisibleAsync();
            await option.ClickAsync();
            await page.WaitForURLAsync(url => !new Uri(url).AbsolutePath.Equals("/company", StringComparison.OrdinalIgnoreCase));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Assertions.Expect(page.Locator(".account-summary strong")).ToHaveTextAsync("پیمان خوشه چین");
            await Assertions.Expect(page.Locator(".account-summary small")).ToHaveTextAsync(targetName);
            await Assertions.Expect(page.Locator(".company-chip strong")).ToHaveTextAsync(targetName);
            Assert.DoesNotContain("Development Administrator", await page.Locator(".account-summary").InnerTextAsync(), StringComparison.Ordinal);
        }
        finally
        {
            if (granted)
            {
                using HttpResponseMessage revoke = await api.DeleteAsync($"api/v1/security/users/{userId}/companies/{targetId}", ct);
                Assert.True(revoke.IsSuccessStatusCode || revoke.StatusCode == HttpStatusCode.NotFound,
                    $"Temporary company access cleanup failed: {(int)revoke.StatusCode} {await revoke.Content.ReadAsStringAsync(ct)}");
            }
        }
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"{name} is required.");
}
