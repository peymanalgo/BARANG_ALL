using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class GlobalTypographyE2ETests:IAsyncLifetime
{
    private const string Iran="42f0e459-6095-45bb-aa13-f38126d91d9c";
    private readonly string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
    private IPlaywright playwright=null!;
    private IBrowser browser=null!;
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private static readonly string[] Cultures=["ar","en","fa"];

    public async ValueTask InitializeAsync()
    {
        playwright=await Playwright.CreateAsync();
        browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")});
    }
    public async ValueTask DisposeAsync(){await browser.DisposeAsync();playwright.Dispose();}

    [Fact]
    public async Task LoginSwitchesLoadedFontsAndLocalizedTextWithoutReloadOrLostInput()
    {
        await using IBrowserContext context=await NewContext();IPage page=await context.NewPageAsync();
        await page.GotoAsync(baseUrl+"/culture?culture=fa&returnUrl=%2Flogin%3Fmessage%3Dinvalid");
        await WaitForHandler(page,".language-switch a[hreflang='ar']","click");
        await page.Locator("#username").FillAsync("unsaved-typography-input");
        string document=await MarkDocument(page);
        foreach(string culture in Cultures)
        {
            await Switch(page,culture,document);
            await Assertions.Expect(page.Locator("#username")).ToHaveValueAsync("unsaved-typography-input");
            await Assertions.Expect(page.Locator("label[for=username]")).ToHaveTextAsync(Caption("AppStrings","Username",culture));
            await Assertions.Expect(page.Locator(".login-brand-message h2")).ToHaveTextAsync(Caption("AppStrings","LoginBrandTagline",culture));
            await Assertions.Expect(page.Locator(".login-alert")).ToBeVisibleAsync();
            await AssertFonts(page,culture);
            Assert.Equal(56,await page.Locator("#username").EvaluateAsync<double>("el=>el.getBoundingClientRect().height"));
            Assert.Equal(58,await page.Locator(".login-submit").EvaluateAsync<double>("el=>el.getBoundingClientRect().height"));
        }
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang","fa");
        await AssertFonts(page,"fa");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/manage/customers")]
    [InlineData("/master/persons")]
    [InlineData("/admin/user-access")]
    [InlineData("/master/organization-chart")]
    [InlineData("/master/reference-data/customers")]
    [InlineData("/orders/new")]
    [InlineData("/workspace/finance")]
    [InlineData("/workspace/workflow")]
    public async Task RepresentativePagesUseLiveLanguageFontsWithoutNavigation(string path)
    {
        await using IBrowserContext context=await NewContext();IPage page=await context.NewPageAsync();
        await Login(page);await page.GotoAsync(baseUrl+path);
        await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
        string document=await MarkDocument(page);
        string hierarchy=await page.Locator("h1").First.EvaluateAsync<string>("el=>{const s=getComputedStyle(el);return JSON.stringify([s.fontSize,s.fontWeight,s.color,s.lineHeight]);}");
        if(path=="/manage/customers")
        {
            await WaitForHandler(page,".customer-search input","input");
            await page.Locator(".customer-search input").FillAsync("typography-kept-search");
        }
        foreach(string culture in Cultures)
        {
            await Switch(page,culture,document);
            await Assertions.Expect(page.Locator(".company-chip small")).ToHaveTextAsync(Caption("AppStrings","CurrentCompany",culture));
            Assert.Equal(hierarchy,await page.Locator("h1").First.EvaluateAsync<string>("el=>{const s=getComputedStyle(el);return JSON.stringify([s.fontSize,s.fontWeight,s.color,s.lineHeight]);}"));
            await AssertFonts(page,culture);
            if(path=="/manage/customers")
            {
                await Assertions.Expect(page.Locator(".customer-search input")).ToHaveValueAsync("typography-kept-search");
                await InspectCustomerModal(page,culture);
            }
            if(path=="/master/organization-chart")await InspectChartSelector(page,culture);
            if(path=="/orders/new")
            {
                ILocator date=page.Locator(".locale-date-input").First;
                await Assertions.Expect(date).ToHaveAttributeAsync("data-calendar-culture",culture);
                await Assertions.Expect(date).ToHaveAttributeAsync("data-calendar-ready","true");
                await date.Locator(".date-picker-button").ClickAsync();
                await Assertions.Expect(date.Locator(".locale-calendar")).ToBeVisibleAsync();
                await AssertFonts(page,culture);
                await date.Locator(".date-picker-button").ClickAsync();
            }
            Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        }
    }

    [Fact]
    public async Task MobileLoginAndCustomerModalRetainResponsiveGeometryInAllLanguages()
    {
        await using IBrowserContext context=await NewContext(390,844);IPage page=await context.NewPageAsync();
        await page.GotoAsync(baseUrl+"/culture?culture=fa&returnUrl=%2Flogin");
        string document=await MarkDocument(page);
        foreach(string culture in Cultures){await Switch(page,culture,document);await AssertFonts(page,culture);Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));}
        await Login(page);await page.GotoAsync(baseUrl+"/manage/customers");document=await MarkDocument(page);
        foreach(string culture in Cultures)
        {
            await page.SetViewportSizeAsync(768,844);
            await Switch(page,culture,document);
            await page.SetViewportSizeAsync(390,844);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang",culture);
            await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");
            await AssertFonts(page,culture);
            Assert.Equal(390,await page.EvaluateAsync<int>("()=>innerWidth"));
            Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
            await InspectCustomerModal(page,culture);
        }
    }

    private async Task InspectCustomerModal(IPage page,string culture)
    {
        await WaitForHandler(page,".customer-page-header .btn-primary","click");
        await page.Locator(".customer-page-header .btn-primary").ClickAsync();
        ILocator modal=page.Locator(".dialog-backdrop [role=dialog]").First;
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await AssertFonts(page,culture);
        await AssertViewport(modal,page);
        ILocator date=modal.Locator(".locale-date-input").First;
        await Assertions.Expect(date).ToHaveAttributeAsync("data-calendar-ready","true");
        await date.Locator(".date-picker-button").ClickAsync();
        await Assertions.Expect(date.Locator(".locale-calendar")).ToBeVisibleAsync();
        await AssertFonts(page,culture);
        await date.Locator(".date-picker-button").ClickAsync();
        await modal.GetByRole(AriaRole.Button,new(){Name=Caption("CustomerStrings","Cancel",culture),Exact=true}).ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
    }

    private async Task InspectChartSelector(IPage page,string culture)
    {
        await WaitForHandler(page,"[data-chart-action=print] button","click");
        await page.Locator("[data-chart-action=print] button").ClickAsync();
        ILocator modal=page.Locator("[data-chart-workflow=print-company]");
        await Assertions.Expect(modal).ToBeVisibleAsync();await AssertViewport(modal,page);
        await modal.Locator(".searchable-select input").ClickAsync();
        await Assertions.Expect(modal.Locator(".searchable-select-list")).ToBeVisibleAsync();
        await AssertFonts(page,culture);
        await modal.GetByRole(AriaRole.Button,new(){Name=Caption("CrudStrings","Cancel",culture),Exact=true}).ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
    }

    private static async Task AssertViewport(ILocator modal,IPage page)
    {
        var box=await modal.BoundingBoxAsync();Assert.NotNull(box);
        Assert.InRange(box.X,0,page.ViewportSize!.Width);
        Assert.InRange(box.Y,0,page.ViewportSize.Height);
        Assert.True(box.X+box.Width<=page.ViewportSize.Width+1);
        Assert.True(box.Y+box.Height<=page.ViewportSize.Height+1);
    }

    private static async Task AssertFonts(IPage page,string culture)
    {
        string family=Family(culture),sample=culture=="en"?"BARANG":culture=="ar"?"الإدارة":"برنگ";
        bool loaded=await page.EvaluateAsync<bool>("async ({family,sample})=>{const faces=await document.fonts.load('400 14px \"'+family+'\"',sample);return faces.length>0&&faces.every(face=>face.status==='loaded');}",new{family,sample});
        Assert.True(loaded,$"The actual {family} webfont must load; a named fallback is insufficient.");
        string[] failures=await page.EvaluateAsync<string[]>(@"family=>{
            const primary=value=>value.split(',')[0].trim().replaceAll('""','').replaceAll(""'"",'');
            const failures=[];
            for(const el of document.body.querySelectorAll('*')){
                if(!el.getClientRects().length||getComputedStyle(el).visibility==='hidden')continue;
                if(!['INPUT','BUTTON','SELECT','TEXTAREA','OPTION'].includes(el.tagName)&&![...el.childNodes].some(n=>n.nodeType===Node.TEXT_NODE&&n.textContent.trim()))continue;
                const font=getComputedStyle(el).fontFamily;
                const technical=el.closest('code,pre,kbd,samp,.entity-code,.code-pill');
                if(technical){if(!font.includes('monospace'))failures.push(el.tagName+': missing technical monospace');}
                else if(primary(font)!==family)failures.push(el.tagName+'.'+el.className+': '+font);
                if(el.matches('input[placeholder],textarea[placeholder]')&&!technical&&primary(getComputedStyle(el,'::placeholder').fontFamily)!==family)failures.push(el.tagName+': placeholder');
            }
            return failures;
        }",family);
        Assert.Empty(failures);
    }

    private static string Family(string culture)=>culture switch{"fa"=>"Vazirmatn","ar"=>"Noto Sans Arabic",_=>"Inter"};
    private async Task<IBrowserContext> NewContext(int width=1440,int height=1000)=>await browser.NewContextAsync(new(){ViewportSize=new(){Width=width,Height=height}});
    private static async Task<string> MarkDocument(IPage page)
    {
        string marker=Guid.NewGuid().ToString("N");await page.EvaluateAsync("marker=>window.__barangTypographyDocument=marker",marker);return marker;
    }
    private static async Task Switch(IPage page,string culture,string document)
    {
        string url=page.Url;
        await WaitForHandler(page,$".language-switch a[hreflang='{culture}']","click");
        await page.Locator($".language-switch a[hreflang='{culture}']").ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang",culture);
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("dir",culture=="en"?"ltr":"rtl");
        await Assertions.Expect(page.Locator($".language-switch a[hreflang='{culture}']")).ToHaveClassAsync(new Regex("\\bselected\\b"));
        Assert.Equal(document,await page.EvaluateAsync<string>("()=>window.__barangTypographyDocument"));
        Assert.Equal(url,page.Url);
    }
    private static async Task WaitForHandler(IPage page,string selector,string eventName)=>await page.WaitForFunctionAsync("({selector,eventName})=>{const el=document.querySelector(selector);return !!el&&Object.keys(el).some(key=>key.startsWith('_blazorEvents_')&&!!el[key].getHandler?.(eventName));}",new{selector,eventName});
    private async Task Login(IPage page)
    {
        await page.GotoAsync(baseUrl+"/culture?culture=fa&returnUrl=%2Flogin");
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??throw new InvalidOperationException("E2E username required"));
        await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??throw new InvalidOperationException("E2E password required"));
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/login");
        if(new Uri(page.Url).AbsolutePath!="/company")await page.GotoAsync(baseUrl+"/company");
        await page.Locator($".company-list form:has(input[name=companyId][value='{Iran}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company");
        await Assertions.Expect(page.Locator(".shell")).ToBeVisibleAsync();
    }
    private static string Caption(string family,string key,string culture)
    {
        DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        string suffix=culture=="fa"?"":"."+culture;
        XDocument document=XDocument.Load(Path.Combine(root?.FullName??throw new DirectoryNotFoundException(),"01_CORE","src","BARANG.Backoffice","Localization",family+suffix+".resx"));
        return document.Root!.Elements("data").Single(x=>x.Attribute("name")!.Value==key).Element("value")!.Value;
    }
}
