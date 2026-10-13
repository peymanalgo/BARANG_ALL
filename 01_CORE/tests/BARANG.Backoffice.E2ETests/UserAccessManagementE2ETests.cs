using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class UserAccessManagementE2ETests : IAsyncLifetime
{
    private static readonly string[] TabLabels = ["Users", "Roles", "Access"];
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        string? executable = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH");
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = executable });
        context = await browser.NewContextAsync(new() { Locale = "en-US" });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
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
    public async Task IntegratedWorkspaceExecutesUserRoleAndPermissionRoundtrips()
    {
        string suffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        string username;
        string roleCode = $"uam.browser.role.{suffix}";
        string roleTitle = $"Browser role {suffix}";

        await page.GotoAsync($"{baseUrl}/admin/user-access", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("main h1")).ToHaveTextAsync("User & Access Management");
        await Assertions.Expect(page.Locator("nav.sidebar-navigation > a[href='/admin/user-access']")).ToHaveTextAsync(new Regex("User & Access Management"));
        Assert.Equal(0, await page.Locator("nav a[href='/admin/security'], nav a[href='/admin/users'], nav a[href='/admin/roles'], nav a[href='/admin/role-permissions']").CountAsync());
        Assert.Equal(TabLabels, await page.Locator(".uam-tabs [role=tab]").AllInnerTextsAsync());

        await SearchCurrentGrid("uam.browser.");
        ILocator acceptanceUsers = page.Locator(".uam-grid tbody tr");
        if (await acceptanceUsers.CountAsync() > 0)
        {
            ILocator reusable = acceptanceUsers.First;
            username = (await reusable.Locator(".code-pill").InnerTextAsync()).Trim();
            await reusable.Locator("button.barang-icon-button[title='View']").ClickAsync();
            ILocator existingDetail = page.Locator(".uam-dialog[aria-label='Users']");
            await Assertions.Expect(existingDetail).ToBeVisibleAsync();
            if ((await reusable.Locator(".status-badge").InnerTextAsync()).Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            {
                await existingDetail.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
                await existingDetail.GetByRole(AriaRole.Checkbox, new() { Name = "Account enabled" }).CheckAsync();
                await existingDetail.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
            }
            await existingDetail.Locator("header button.barang-icon-button[title='Close']").ClickAsync();
        }
        else
        {
            username = $"uam.browser.{suffix}";
            await SearchCurrentGrid("");
            await page.Locator(".uam-panel .btn-primary").ClickAsync();
            ILocator userDialog = page.Locator(".uam-dialog[aria-label='Add User']");
            await Assertions.Expect(userDialog).ToBeVisibleAsync();
            ILocator personSearch = userDialog.Locator(".searchable-select input").First;
            await SelectFirstCandidate(personSearch, "01", "02");
            await Assertions.Expect(userDialog.Locator(".uam-person-card")).ToBeVisibleAsync();
            await Assertions.Expect(userDialog.Locator(".uam-account-panel h3")).ToHaveTextAsync("User Account Information");
            await userDialog.Locator("input[autocomplete='username']").FillAsync(username);
            await userDialog.Locator("input[autocomplete='new-password']").Nth(0).FillAsync("UamBrowserStrongPassword123");
            await userDialog.Locator("input[autocomplete='new-password']").Nth(1).FillAsync("UamBrowserStrongPassword123");
            await userDialog.Locator("footer .btn-primary").ClickAsync();
            await Assertions.Expect(userDialog).ToBeHiddenAsync(new() { Timeout = 15_000 });
            await SearchCurrentGrid(username);
            await Assertions.Expect(page.Locator(".uam-grid tbody tr")).ToContainTextAsync(username.ToUpperInvariant());
        }

        await page.Locator(".uam-tabs [role=tab]").Nth(1).ClickAsync();
        await page.Locator(".uam-panel .btn-primary").ClickAsync();
        ILocator roleDialog = page.Locator(".uam-dialog[aria-label='Roles']");
        await Assertions.Expect(roleDialog).ToBeVisibleAsync();
        await roleDialog.Locator("input").Nth(0).FillAsync(roleCode);
        await roleDialog.Locator("input").Nth(1).FillAsync(roleTitle);
        await roleDialog.Locator("textarea").FillAsync("Rendered browser acceptance role");
        await roleDialog.Locator("footer .btn-primary").ClickAsync();
        await Assertions.Expect(roleDialog).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await SearchCurrentGrid(roleCode);
        ILocator roleRow = page.Locator(".uam-grid tbody tr").Filter(new() { HasText = roleCode });
        await Assertions.Expect(roleRow).ToBeVisibleAsync();
        await roleRow.Locator("button.barang-icon-button[title='Edit']").ClickAsync();
        await roleDialog.Locator("input").Nth(1).FillAsync(roleTitle + " edited");
        await roleDialog.Locator("footer .btn-primary").ClickAsync();
        await SearchCurrentGrid(roleCode);
        await Assertions.Expect(page.Locator(".uam-grid tbody tr").Filter(new() { HasText = roleCode })).ToContainTextAsync("edited");

        await page.Locator(".uam-tabs [role=tab]").Nth(0).ClickAsync();
        await SearchCurrentGrid(username);
        ILocator userRow = page.Locator(".uam-grid tbody tr").Filter(new() { HasText = username.ToUpperInvariant() });
        await userRow.Locator("button.barang-icon-button[title='View']").ClickAsync();
        ILocator detailDialog = page.Locator(".uam-dialog[aria-label='Users']");
        ILocator roleSearch = detailDialog.Locator(".uam-role-assignment .searchable-select input");
        await roleSearch.FillAsync(roleCode);
        await page.WaitForTimeoutAsync(500);
        await roleSearch.PressAsync("Enter");
        await detailDialog.GetByRole(AriaRole.Button, new() { Name = "Assign Role" }).ClickAsync();
        await Assertions.Expect(detailDialog.Locator(".uam-chip-list")).ToContainTextAsync(roleTitle);
        await detailDialog.Locator("header button.barang-icon-button[title='Close']").ClickAsync();

        await page.Locator(".uam-tabs [role=tab]").Nth(2).ClickAsync();
        ILocator selectors = page.Locator(".uam-target-selectors");
        ILocator roleTargetSearch = selectors.Locator(".searchable-select input").Nth(0);
        await roleTargetSearch.FillAsync(roleCode);
        await page.WaitForTimeoutAsync(500);
        await roleTargetSearch.PressAsync("Enter");
        await Assertions.Expect(page.Locator(".permission-matrix")).ToBeVisibleAsync();
        ILocator rolePermission = page.Locator(".permission-row .permission-cell input:not([disabled])").First;
        await rolePermission.CheckAsync();
        await Assertions.Expect(page.Locator(".uam-savebar")).ToHaveClassAsync(new Regex("dirty"));
        await page.Locator(".uam-savebar .btn-primary").ClickAsync();
        await Assertions.Expect(page.Locator(".uam-savebar")).Not.ToHaveClassAsync(new Regex("dirty"));

        await selectors.GetByRole(AriaRole.Button, new() { Name = "Clear selection" }).ClickAsync();
        ILocator userTargetSearch = selectors.Locator(".searchable-select input").Nth(1);
        await userTargetSearch.FillAsync(username);
        await page.WaitForTimeoutAsync(500);
        await userTargetSearch.PressAsync("Enter");
        await Assertions.Expect(page.Locator(".permission-via.on").First).ToBeVisibleAsync();
        int inheritedBefore = await page.Locator(".permission-via.on").CountAsync();
        ILocator directPermission = page.Locator(".permission-row .permission-cell input:not([disabled])").Nth(1);
        await directPermission.CheckAsync();
        await page.Locator(".uam-savebar .btn-primary").ClickAsync();
        await Assertions.Expect(page.Locator(".uam-savebar")).Not.ToHaveClassAsync(new Regex("dirty"));
        await directPermission.UncheckAsync();
        await page.Locator(".uam-savebar .btn-primary").ClickAsync();
        await Assertions.Expect(page.Locator(".uam-savebar")).Not.ToHaveClassAsync(new Regex("dirty"));
        Assert.Equal(inheritedBefore, await page.Locator(".permission-via.on").CountAsync());
        Assert.True(await page.Locator(".permission-effective.on").CountAsync() >= inheritedBefore);

        await page.Locator(".uam-tabs [role=tab]").Nth(0).ClickAsync();
        await SearchCurrentGrid(username);
        userRow = page.Locator(".uam-grid tbody tr").Filter(new() { HasText = username.ToUpperInvariant() });
        await userRow.Locator("button.barang-icon-button[title='Edit']").ClickAsync();
        detailDialog = page.Locator(".uam-dialog[aria-label='Users']");
        string renamedUsername = username + ".edited";
        await detailDialog.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(renamedUsername);
        await detailDialog.GetByRole(AriaRole.Checkbox, new() { Name = "Account enabled" }).UncheckAsync();
        await detailDialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await detailDialog.Locator("header button.barang-icon-button[title='Close']").ClickAsync();

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchCurrentGrid(renamedUsername);
        userRow = page.Locator(".uam-grid tbody tr").Filter(new() { HasText = renamedUsername.ToUpperInvariant() });
        await Assertions.Expect(userRow).ToBeVisibleAsync();
        await Assertions.Expect(userRow.Locator(".status-badge")).ToContainTextAsync("Disabled");

        await userRow.Locator("button.barang-icon-button[title='Edit']").ClickAsync();
        detailDialog = page.Locator(".uam-dialog[aria-label='Users']");
        await detailDialog.GetByRole(AriaRole.Checkbox, new() { Name = "Account enabled" }).CheckAsync();
        await detailDialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await detailDialog.Locator("header button.barang-icon-button[title='Close']").ClickAsync();
    }

    [Theory]
    [InlineData("fa", "rtl", "مدیریت کاربران و دسترسیها", "کاربران", "نقشها", "دسترسیها", "مشاهده")]
    [InlineData("en", "ltr", "User & Access Management", "Users", "Roles", "Access", "View")]
    [InlineData("ar", "rtl", "إدارة المستخدمين والصلاحيات", "المستخدمون", "الأدوار", "الصلاحيات", "عرض")]
    public async Task WorkspaceIsFullyLocalizedAndDirectional(
        string culture,
        string direction,
        string title,
        string users,
        string roles,
        string access,
        string viewCaption)
    {
        await page.GotoAsync(
            $"{baseUrl}/culture?culture={culture}&returnUrl={Uri.EscapeDataString("/admin/user-access")}",
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", culture);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
        await Assertions.Expect(page.Locator("main h1")).ToHaveTextAsync(title);
        Assert.Equal(new[] { users, roles, access }, await page.Locator(".uam-tabs [role=tab]").AllInnerTextsAsync());
        ILocator viewAction = page.Locator(".uam-users-grid .barang-icon-button").First;
        await Assertions.Expect(viewAction).ToHaveAttributeAsync("title", viewCaption);
        await Assertions.Expect(viewAction).ToHaveAttributeAsync("aria-label", viewCaption);
        Assert.Equal(0, await page.Locator("body").GetByText(new Regex("UserAccessStrings|Nav_UserAccessManagement")).CountAsync());
    }

    private async Task SearchCurrentGrid(string value)
    {
        ILocator search = page.Locator(".uam-panel .uam-search input");
        await search.FillAsync(value);
        await search.PressAsync("Tab");
        await page.WaitForTimeoutAsync(500);
    }

    private async Task SelectFirstCandidate(ILocator input, params string[] searches)
    {
        foreach (string search in searches)
        {
            await input.FillAsync(search);
            await page.WaitForTimeoutAsync(500);
            if (await input.Locator("xpath=following-sibling::*[@role='listbox']//*[@role='option']").CountAsync() == 0) continue;
            await input.PressAsync("ArrowDown");
            await input.PressAsync("ArrowUp");
            await input.PressAsync("Enter");
            return;
        }
        Assert.Fail("No eligible Person candidate was available for the rendered Add User acceptance flow.");
    }
}
