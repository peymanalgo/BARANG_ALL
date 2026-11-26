using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

internal static class BrowserCompanyContext
{
    public static async Task SelectIfRequiredAsync(IPage page)
    {
        Uri current = new(page.Url);
        if (!current.AbsolutePath.Equals("/company", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{current.Scheme}://{current.Authority}/company", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (!new Uri(page.Url).AbsolutePath.Equals("/company", StringComparison.OrdinalIgnoreCase))
                return;
        }

        string? companyId = Environment.GetEnvironmentVariable("BARANG_E2E_COMPANY_ID");
        ILocator choice = string.IsNullOrWhiteSpace(companyId)
            ? page.Locator(".company-list form button[type=submit]").First
            : page.Locator($".company-list form:has(input[name='companyId'][value='{companyId}']) button[type=submit]");

        if (await choice.CountAsync() == 0)
            choice = page.Locator(".company-list form button[type=submit]").First;

        await choice.ClickAsync();
        await page.WaitForURLAsync(url => !new Uri(url).AbsolutePath.Equals("/company", StringComparison.OrdinalIgnoreCase));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
