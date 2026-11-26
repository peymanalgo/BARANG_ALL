using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class Step6PricingAcceptanceE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe"
        });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin");
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (new Uri(page.Url).AbsolutePath == "/company")
        {
            await page.Locator(".company-list form button[type=submit]").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    [Fact]
    public async Task IsolatedOrderWizardPersistsCustomerGroupBreakdownAndFinalSnapshot()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Forders%2Fnew", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync($"{baseUrl}/orders/new", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await ChooseFirst("order-branch");
        await ChooseFirst("order-customer");
        Console.WriteLine($"STEP6_HEADER branch={await page.Locator("input[name='order-branch']").InputValueAsync()} customer={await page.Locator("input[name='order-customer']").InputValueAsync()}");
        Assert.Equal("A Step6 Group Buyer", (await page.Locator("input[name='order-customer']").InputValueAsync()).Trim());
        ILocator headerNext = page.GetByText("Next", new() { Exact = true }).First;
        Console.WriteLine($"STEP6_HEADER_NEXT count={await headerNext.CountAsync()}");
        await headerNext.ClickAsync();

        await ChooseFirst("order-item-product");
        await ChooseFirst("order-item-sku");
        await page.Locator("input[name='order-item-quantity']").FillAsync("2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add item", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(1, new() { Timeout = 15000 });
        await page.Locator(".order-items-step .form-actions").Last.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        await page.WaitForTimeoutAsync(500);

        ILocator print = page.Locator(".order-print-step-redesign");
        Assert.True(await print.CountAsync() > 0, $"Expected print step at {page.Url}; page={await page.Locator("main").InnerTextAsync()}");
        await print.Locator("select[name='print-method-step2']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await print.GetByLabel("Width * cm").FillAsync("10");
        await print.GetByLabel("Height * cm").FillAsync("12");
        await print.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "step6-pricing.png",
            MimeType = "image/png",
            Buffer = File.ReadAllBytes(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", "src/BARANG.Backoffice/wwwroot/images/login-background.png")))
                .Concat(Guid.NewGuid().ToByteArray()).ToArray()
        });
        await Assertions.Expect(print.Locator(".order-print-upload-preview")).ToBeVisibleAsync(new() { Timeout = 15000 });
        await print.GetByRole(AriaRole.Button, new() { Name = "+ Add", Exact = true }).ClickAsync();
        Console.WriteLine("STEP6_PRINT_BODY=" + (await print.InnerTextAsync()).ReplaceLineEndings(" | "));
        await Assertions.Expect(print.Locator("tbody tr")).ToHaveCountAsync(1);
        await print.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();

        await Assertions.Expect(page.Locator(".order-services-step")).ToBeVisibleAsync();
        await page.Locator(".order-services-step").GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        ILocator shipping = page.Locator(".order-shipping-step");
        await shipping.Locator("button[name='order-step5-specific-address']").ClickAsync();
        await shipping.Locator("input[name='order-step5-recipient']").FillAsync("STEP6 Receiver");
        await shipping.Locator("input[name='order-step5-country']").FillAsync("IR");
        await shipping.Locator("textarea[name='order-step5-address']").FillAsync("STEP6 isolated address");
        await shipping.Locator("input[name='order-step5-shipping-method']").ClickAsync();
        await shipping.Locator("[role=option]").Last.ClickAsync();
        await shipping.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();

        ILocator pricing = page.Locator(".order-pricing-step");
        await Assertions.Expect(pricing).ToBeVisibleAsync();
        await Assertions.Expect(pricing.Locator(".pricing-summary-grid")).ToBeVisibleAsync();
        await pricing.GetByRole(AriaRole.Button, new() { Name = "Recalculate", Exact = true }).ClickAsync();
        await Assertions.Expect(pricing.Locator(".loading-state")).ToHaveCountAsync(0);
        string breakdown = await pricing.InnerTextAsync();
        Assert.Contains("Discount", breakdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tax", breakdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IRR", breakdown, StringComparison.OrdinalIgnoreCase);

        await pricing.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
        ILocator review = page.Locator(".order-review-step");
        await review.GetByRole(AriaRole.Button, new() { Name = "Finalize order", Exact = true }).ClickAsync();
        await review.GetByRole(AriaRole.Button, new() { Name = "Confirm finalization", Exact = true }).ClickAsync();
        await page.WaitForURLAsync("**/360", new() { WaitUntil = WaitUntilState.NetworkIdle });
        string orderId = new Uri(page.Url).Segments[^2].Trim('/');
        Assert.True(Guid.TryParse(orderId, out _), $"Finalized order URL did not contain an id: {page.Url}");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator(".order360-shell")).ToBeVisibleAsync();
        Console.WriteLine($"STEP6_BROWSER_ORDER_ID={orderId}");
    }

    private async Task ChooseFirst(string name)
    {
        ILocator input = page.Locator($"input[name='{name}']");
        await input.ClickAsync();
        ILocator options = input.Locator("xpath=..").Locator("[role=option]");
        await Assertions.Expect(options).Not.ToHaveCountAsync(0);
        await options.First.ClickAsync();
        await Assertions.Expect(input).Not.ToHaveValueAsync("");
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await browser.DisposeAsync();
        playwright.Dispose();
    }
}
