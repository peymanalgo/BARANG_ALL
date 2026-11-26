using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace BARANG.Backoffice.E2ETests;

public sealed class WorkflowDesignerE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync() { playwright = await Playwright.CreateAsync(); browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") ?? @"C:\Program Files\Google\Chrome\Application\chrome.exe" }); context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } }); page = await context.NewPageAsync(); await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.NetworkIdle }); await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin"); await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026"); await page.Locator("button[type=submit]").ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); if (new Uri(page.Url).AbsolutePath == "/company") { await page.Locator(".company-list form button[type=submit]").First.ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); } }
    [Fact]
    public async Task DesignerLoadsValidatesAndKeepsTheActiveVersion()
    {
        await page.GotoAsync($"{baseUrl}/workspace/workflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1000);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(new Regex("^(Workflow designer|طراح گردش کار|مصمم سير العمل)$"));
        await page.Locator(".workflow-toolbar-actions button").Nth(1).ClickAsync();
        await Assertions.Expect(page.Locator(".workflow-validation")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".workflow-toolbar")).ToContainTextAsync(new Regex("Active|فعال|نشط"));
    }
    [Fact]
    public async Task DesignerCreatesAndPersistsAVisualTaskConnection()
    {
        await page.GotoAsync($"{baseUrl}/workspace/workflow", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1000);
        if (await page.Locator(".workflow-readonly").CountAsync() > 0)
        {
            await page.Locator(".workflow-toolbar-actions button").First.ClickAsync();
            await page.WaitForTimeoutAsync(300);
        }
        Assert.True(await page.Locator(".workflow-toolbar-actions button").CountAsync() >= 2, "Designer must expose save and validation actions after entering edit mode.");
        await page.Locator(".palette-group button").Nth(2).ClickAsync();
        await page.WaitForTimeoutAsync(500);
        int nodeCountAfterAdd = await page.Locator(".workflow-node").CountAsync();
        Assert.True(nodeCountAfterAdd >= 3, $"nodes={nodeCountAfterAdd}, palette={await page.Locator(".palette-group button").CountAsync()}, readonly={await page.Locator(".workflow-readonly").CountAsync()}, first={await page.Locator(".workflow-toolbar-actions button").First.TextContentAsync()}");
        ILocator position = page.Locator(".workflow-properties select").First;
        if (await position.Locator("option").CountAsync() > 1) await position.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        ILocator start = page.Locator(".workflow-node.node-start");
        ILocator end = page.Locator(".workflow-node.node-end");
        ILocator task = page.Locator(".workflow-node.node-usertask").Last;
        Assert.True(await page.Locator(".workflow-node").CountAsync() >= 3, await page.Locator(".workflow-canvas-surface").InnerTextAsync());
        await page.Locator(".workflow-palette > button").ClickAsync();
        await start.ClickAsync(new() { Force = true }); await task.ClickAsync(new() { Force = true });
        await page.Locator(".workflow-palette > button").ClickAsync();
        await task.ClickAsync(new() { Force = true }); await end.ClickAsync(new() { Force = true });
        Assert.True(await page.Locator(".workflow-connections line").CountAsync() >= 2);
        await page.Locator(".workflow-toolbar-actions button").First.ClickAsync();
        await page.WaitForTimeoutAsync(300);
        string? saveMessage = await page.Locator(".inline-error").CountAsync() > 0 ? await page.Locator(".inline-error").First.TextContentAsync() : "none";
        Assert.Equal("none", saveMessage);
        Assert.True(await page.Locator(".workflow-node").CountAsync() >= 3, $"save changed nodes unexpectedly; message={saveMessage}");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.True(await page.Locator(".workflow-node").CountAsync() >= 3);
        Assert.True(await page.Locator(".workflow-connections line").CountAsync() >= 2);
    }
    public async ValueTask DisposeAsync() { await context.CloseAsync(); await browser.CloseAsync(); playwright.Dispose(); }
}
