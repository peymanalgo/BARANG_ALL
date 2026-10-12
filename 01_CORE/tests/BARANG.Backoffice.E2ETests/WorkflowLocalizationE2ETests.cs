using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace BARANG.Backoffice.E2ETests;

public sealed class WorkflowLocalizationE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
        context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
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

    [Theory]
    [InlineData("fa", "طراح گردش کار", "rtl")]
    [InlineData("ar", "مصمم سير العمل", "rtl")]
    [InlineData("en", "Workflow designer", "ltr")]
    public async Task WorkflowWorkspaceUsesLocalizedShellAndDirection(string culture, string title, string direction)
    {
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2Fworkspace%2Fworkflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("h1");
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(title);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir", direction);
        await Assertions.Expect(page.Locator("h2").First).ToContainTextAsync(culture == "en" ? "Components" : culture == "ar" ? "مكونات سير العمل" : "اجزای گردش کار");
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("DateTimeOffset");
    }

    [Fact]
    public async Task WorkflowDesignerActionsUseSharedButtonStyling()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Fworkflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(".workflow-toolbar-actions button");

        await Assertions.Expect(page.Locator(".workflow-toolbar-actions button.btn")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator(".workflow-toolbar-actions button:not(.btn):not(.barang-icon-button)")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".workflow-palette-item")).ToHaveCountAsync(5);
        await Assertions.Expect(page.Locator(".workflow-palette-item").First).ToHaveClassAsync(new Regex("workflow-palette-item"));
        await Assertions.Expect(page.Locator(".workflow-canvas-tools .barang-icon-button")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator(".workflow-canvas-tools .btn")).ToHaveCountAsync(1);

        string actionBackground = await page.Locator(".workflow-toolbar-actions .btn").First.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
        Assert.NotEqual("rgba(0, 0, 0, 0)", actionBackground);
    }

    [Fact]
    public async Task WorkflowDesignerDragDropAndDeleteUserTaskWorksInBrowser()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Fworkflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        if (await page.Locator(".workflow-readonly").CountAsync() > 0)
            await page.Locator(".workflow-toolbar-actions .btn-secondary").First.ClickAsync();

        int before = await page.Locator(".workflow-node.node-usertask").CountAsync();
        ILocator palette = page.Locator(".workflow-palette-item.palette-task");
        ILocator canvas = page.Locator(".workflow-canvas-surface");
        await palette.ScrollIntoViewIfNeededAsync();
        await canvas.ScrollIntoViewIfNeededAsync();
        var from = await palette.BoundingBoxAsync();
        var to = await canvas.BoundingBoxAsync();
        Assert.NotNull(from);
        Assert.NotNull(to);
        await page.Mouse.MoveAsync(from!.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to!.X + Math.Min(260, to.Width / 2), to.Y + Math.Min(220, to.Height / 2), new() { Steps = 8 });
        await page.Mouse.UpAsync();

        await Assertions.Expect(page.Locator(".workflow-node.node-usertask")).ToHaveCountAsync(before + 1);
        await page.Locator(".workflow-node.node-usertask").Last.ClickAsync();
        await page.Locator(".workflow-selection-actions .btn-danger").ClickAsync();
        await Assertions.Expect(page.Locator(".workflow-delete-confirm")).ToBeVisibleAsync();
        await page.Locator(".workflow-delete-confirm .btn-danger").ClickAsync();
        await Assertions.Expect(page.Locator(".workflow-node.node-usertask")).ToHaveCountAsync(before);
    }

    [Fact]
    public async Task WorkflowDesignerMovesNodesOnGridAndKeepsCanvasScrollable()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Fworkflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        if (await page.Locator(".workflow-readonly").CountAsync() > 0)
            await page.Locator(".workflow-toolbar-actions .btn-secondary").First.ClickAsync();

        ILocator task = page.Locator(".workflow-node.node-usertask").First;
        await task.ScrollIntoViewIfNeededAsync();
        var before = await task.BoundingBoxAsync();
        Assert.NotNull(before);
        await page.Mouse.MoveAsync(before!.X + before.Width / 2, before.Y + before.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(before.X + 180, before.Y + 120, new() { Steps = 8 });
        await page.Mouse.UpAsync();
        var after = await task.BoundingBoxAsync();
        Assert.NotNull(after);
        Assert.True(Math.Abs(after!.X - before.X) > 20 || Math.Abs(after.Y - before.Y) > 20, $"before=({before.X},{before.Y}) after=({after.X},{after.Y})");

        ILocator surface = page.Locator(".workflow-canvas-surface");
        string background = await surface.EvaluateAsync<string>("el => getComputedStyle(el).backgroundImage");
        Assert.Contains("linear-gradient", background, StringComparison.OrdinalIgnoreCase);
        Assert.True(await surface.EvaluateAsync<bool>("el => el.scrollWidth >= el.clientWidth && el.scrollHeight >= el.clientHeight"));
    }

    [Fact]
    public async Task WorkflowDesignerValidateUsesCurrentGraphAndShowsResult()
    {
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fworkspace%2Fworkflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        ILocator validate = page.Locator(".workflow-toolbar-actions .btn-secondary").Filter(new() { HasText = "Validate" });
        await Assertions.Expect(validate).ToBeVisibleAsync();
        await Assertions.Expect(validate).ToBeEnabledAsync();
        await validate.ClickAsync();
        await Assertions.Expect(page.Locator(".workflow-validation")).ToBeVisibleAsync();
        await page.Locator(".workflow-validation .inline-success, .workflow-validation .workflow-issue").First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        string validationText = await page.Locator(".workflow-validation").InnerTextAsync();
        Assert.True(await page.Locator(".workflow-validation .inline-success, .workflow-validation .workflow-issue").CountAsync() > 0, validationText);
        await Assertions.Expect(page.Locator("h1")).Not.ToContainTextAsync("unhandled exception", new() { IgnoreCase = true });
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
