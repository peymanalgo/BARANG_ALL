using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class LoginPageE2ETests : IAsyncLifetime
{
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Fact]
    public async Task LoginMatchesTheReferenceAcrossFaArEnAndResponsiveViewports()
    {
        await using IBrowserContext context = await NewContext(1440, 900);
        IPage page = await context.NewPageAsync();
        (string Code, string Direction, string Title, string Error)[] locales =
        [
            ("fa", "rtl", "ورود به حساب کاربری", "نام کاربری یا رمز عبور صحیح نیست."),
            ("ar", "rtl", "تسجيل الدخول إلى حسابك", "اسم المستخدم أو كلمة المرور غير صحيحة."),
            ("en", "ltr", "Sign in to your account", "The username or password is incorrect.")
        ];

        foreach ((string code, string direction, string title, string error) in locales)
        {
            await GotoCulture(page, code, "/login?message=invalid");
            await Assertions.Expect(page.Locator(".login-alert")).ToHaveTextAsync(error);
            await GotoCulture(page, code, "/login");
            Assert.Equal(code, await page.Locator("html").GetAttributeAsync("lang"));
            Assert.Equal(direction, await page.Locator("html").GetAttributeAsync("dir"));
            await Assertions.Expect(page.Locator(".login-shell")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".login-brand-side")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#login-title")).ToHaveTextAsync(title);
            await Assertions.Expect(page.Locator($".language-switch a[href*='culture={code}']")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("selected"));
            Assert.Equal(3, await page.Locator(".language-switch a").CountAsync());
            Assert.Contains("login-background.png", await page.Locator(".login-page").EvaluateAsync<string>("el => getComputedStyle(el).backgroundImage"));

            Box shell = await Bounds(page.Locator(".login-shell"));
            Box username = await Bounds(page.Locator("#username"));
            Box password = await Bounds(page.Locator("#password"));
            Box submit = await Bounds(page.Locator("button[type=submit]"));
            Assert.InRange(shell.Width, 1038, 1042);
            Assert.InRange(shell.Height, 618, 650);
            Assert.InRange(username.Height, 55, 57);
            Assert.InRange(password.Height, 55, 57);
            Assert.InRange(submit.Height, 57, 59);
        }

        await GotoCulture(page, "en", "/login");
        await page.GetByRole(AriaRole.Link, new() { Name = "فارسی" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "fa");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(page.Locator("#login-title")).ToHaveTextAsync("ورود به حساب کاربری");
        await page.GetByRole(AriaRole.Link, new() { Name = "English" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "en");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
        await Assertions.Expect(page.Locator("#login-title")).ToHaveTextAsync("Sign in to your account");

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(Path.GetTempPath(), "barang-login-en-desktop.png"), FullPage = true });

        await page.SetViewportSizeAsync(820, 900);
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".login-brand-side")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".login-mobile-brand")).ToBeVisibleAsync();
        Assert.InRange((await Bounds(page.Locator(".login-shell"))).Width, 510, 522);
        Assert.False(await HasHorizontalOverflow(page));

        await page.SetViewportSizeAsync(390, 844);
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Box language = await Bounds(page.Locator(".language-switch"));
        Box box = await Bounds(page.Locator(".login-box"));
        Assert.InRange(Math.Abs(language.Width - box.Width), 0, 2);
        Assert.False(await HasHorizontalOverflow(page));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(Path.GetTempPath(), "barang-login-en-mobile.png"), FullPage = true });
    }

    [Fact]
    public async Task LoginKeepsNativeValidationSecurePasswordToggleAndLocalizedFailure()
    {
        await using IBrowserContext context = await NewContext(1440, 900);
        IPage page = await context.NewPageAsync();
        await GotoCulture(page, "en", "/login");

        Assert.Equal("password", await page.Locator("#password").GetAttributeAsync("type"));
        await page.Locator("button[type=submit]").ClickAsync();
        Assert.True(await page.Locator("#username").EvaluateAsync<bool>("el => el.matches(':invalid')"));
        Assert.EndsWith("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.Locator("#username").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.Equal("password", await page.EvaluateAsync<string>("document.activeElement?.id"));
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.EvaluateAsync<bool>("document.activeElement?.classList.contains('login-password-toggle')"));
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.EvaluateAsync<bool>("document.activeElement?.matches('button[type=submit]')"));

        await page.Locator("#password").FillAsync("temporary-visible-check");
        ILocator toggle = page.Locator(".login-password-toggle");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-label", "Show password");
        await toggle.ClickAsync();
        Assert.Equal("text", await page.Locator("#password").GetAttributeAsync("type"));
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-label", "Hide password");
        await toggle.ClickAsync();
        Assert.Equal("password", await page.Locator("#password").GetAttributeAsync("type"));

        await page.Locator("#username").FillAsync($"not-a-user-{Guid.NewGuid():N}");
        await page.Locator("#password").FillAsync("definitely-not-a-valid-password");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator(".login-alert")).ToHaveTextAsync("The username or password is incorrect.");
        Assert.Contains("message=invalid", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task EveryVisibleLoginTextUsesTheSharedLoginFontFamily(string culture)
    {
        await using IBrowserContext context = await NewContext(1440, 900);
        IPage page = await context.NewPageAsync();
        await GotoCulture(page, culture, "/login?message=invalid");

        string[] selectors =
        [
            ".login-brand-message p",
            "#login-title",
            ".login-copy p",
            ".login-field label",
            "#username",
            "#password",
            ".login-submit",
            ".language-switch a",
            ".login-alert",
            ".login-footer"
        ];

        string expected = await page.Locator("label[for=username]").EvaluateAsync<string>("el => getComputedStyle(el).fontFamily");
        string family = culture switch { "fa" => "Vazirmatn", "ar" => "Noto Sans Arabic", _ => "Inter" };
        Assert.Equal(family, expected.Split(',')[0].Trim().Trim('"'));
        string title = culture switch { "fa" => "سامانه جامع مرکزی شرکت برنگ", "ar" => "النظام المركزي المتكامل لشركة برنگ", _ => "BARANG Company Integrated Central System" };
        await Assertions.Expect(page.Locator(".login-brand-message h2")).ToHaveTextAsync(title);
        string[] visibleFonts = await page.Locator(".login-page").EvaluateAsync<string[]>("root => [...root.querySelectorAll('*')].filter(el => el.getClientRects().length && (['INPUT','BUTTON','SELECT','TEXTAREA'].includes(el.tagName) || [...el.childNodes].some(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim()))).map(el => getComputedStyle(el).fontFamily)");
        Assert.NotEmpty(visibleFonts);
        Assert.All(visibleFonts, font => Assert.Equal(expected, font));
        Assert.Equal(expected, await page.Locator("#username").EvaluateAsync<string>("el => getComputedStyle(el, '::placeholder').fontFamily"));
        string content = await page.Locator(".login-page").InnerTextAsync();
        Assert.DoesNotContain("رنگ ایده", content, StringComparison.Ordinal);
        Assert.DoesNotContain("سامانه مدیریت سفارشات چاپ", content, StringComparison.Ordinal);
        Assert.DoesNotContain("دفتر پشتیبان", content, StringComparison.Ordinal);
        foreach (string selector in selectors)
        {
            ILocator element = page.Locator(selector).First;
            await Assertions.Expect(element).ToBeVisibleAsync();
            Assert.Equal(expected, await element.EvaluateAsync<string>("el => getComputedStyle(el).fontFamily"));
        }
    }

    [Fact]
    public async Task SuccessfulLoginStillUsesTheExistingAuthenticationFlow()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        await using IBrowserContext context = await NewContext(1440, 900);
        IPage page = await context.NewPageAsync();
        await GotoCulture(page, "en", "/login");

        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IBrowserContext> NewContext(int width, int height) => await browser.NewContextAsync(new BrowserNewContextOptions
    {
        Locale = "en-US",
        ViewportSize = new ViewportSize { Width = width, Height = height }
    });

    private async Task GotoCulture(IPage page, string culture, string returnUrl)
    {
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl={Uri.EscapeDataString(returnUrl)}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".login-shell")).ToBeVisibleAsync();
    }

    private static async Task<Box> Bounds(ILocator locator) => await locator.EvaluateAsync<Box>("el => { const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; }")
        ?? throw new InvalidOperationException($"Element '{locator}' has no bounding box.");

    private static async Task<bool> HasHorizontalOverflow(IPage page) =>
        await page.Locator("body").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1");

    private sealed class Box
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
