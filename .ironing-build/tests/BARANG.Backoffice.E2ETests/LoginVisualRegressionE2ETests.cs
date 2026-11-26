using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class LoginVisualRegressionE2ETests
{
    [Fact]
    public async Task LoginVisualGeometryIsBounded()
    {
        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        await using IBrowserContext context = await browser.NewContextAsync(new() { ViewportSize = new ViewportSize { Width = 1440, Height = 900 } });
        IPage page = await context.NewPageAsync();
        await page.GotoAsync((Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100") + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.ScreenshotAsync(new() { Path = Path.Combine(Environment.CurrentDirectory, "TestResults", "login-deployed.png"), FullPage = true });
        string geometry = await page.EvaluateAsync<string>("""() => JSON.stringify(Object.fromEntries(['.login-page','.login-shell','.login-brand-side','.login-illustration','.login-illustration svg','.login-form-side','.login-box','.login-field input'].map(selector => { const el=document.querySelector(selector); const r=el?.getBoundingClientRect(); const s=el ? getComputedStyle(el) : null; return [selector,{width:r?.width,height:r?.height,display:s?.display,position:s?.position,maxWidth:s?.maxWidth,minWidth:s?.minWidth,overflow:s?.overflow}]; })))""");
        Console.WriteLine(geometry);
        double svgWidth = await page.Locator(".login-illustration svg").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        double svgHeight = await page.Locator(".login-illustration svg").EvaluateAsync<double>("el => el.getBoundingClientRect().height");
        double formWidth = await page.Locator(".login-box").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        double inputWidth = await page.Locator(".login-field input").First.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        bool desktopOverflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth || document.body.scrollWidth > document.body.clientWidth");
        Assert.InRange(svgWidth, 1, 400);
        Assert.InRange(svgHeight, 1, 320);
        Assert.InRange(formWidth, 1, 420);
        Assert.InRange(inputWidth, 1, 400);
        Assert.False(desktopOverflow);

        await page.SetViewportSizeAsync(390, 844);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        bool mobileOverflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth || document.body.scrollWidth > document.body.clientWidth");
        double mobileInputWidth = await page.Locator(".login-field input").First.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.False(mobileOverflow);
        Assert.InRange(mobileInputWidth, 1, 350);
    }
}
