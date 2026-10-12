using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using BARANG.Backoffice.Localization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BARANG.Backoffice.Tests;

public sealed class GlobalTypographyTests
{
    [Theory]
    [InlineData("fa","Vazirmatn")]
    [InlineData("ar","Noto Sans Arabic")]
    [InlineData("en","Inter")]
    public void EveryLanguageHasOneCentralFontStack(string culture,string family)
    {
        string css=Read("wwwroot/barang-design-system.css");
        Assert.Contains($"--barang-font-{culture}: \"{family}\", sans-serif;",css);
        Assert.Contains($":root[lang|=\"{culture}\"] {{ --barang-font-family: var(--barang-font-{culture}); }}",css);
        Assert.Contains(":where(body *) { font-family: inherit; }",css);
        Assert.Contains("::placeholder { font-family: inherit; }",css);
        Assert.Contains("font-family: var(--barang-font-family);",css);
        Assert.DoesNotContain("font-family",Read("wwwroot/login.css"));
    }

    [Fact]
    public void AllRoutedComponentsAutomaticallyReceiveTheLiveCulture()
    {
        Type[] routed=typeof(LocalePolicy).Assembly.GetTypes()
            .Where(type=>type.GetCustomAttributes<RouteAttribute>().Any()).ToArray();
        Assert.NotEmpty(routed);
        Assert.All(routed,type=>Assert.True(typeof(LocaleComponentBase).IsAssignableFrom(type),type.FullName));
        Assert.Contains("@inherits LocaleComponentBase",Read("Components/_Imports.razor"));
        Assert.Contains("@inherits LocaleLayoutBase",Read("Components/Layout/MainLayout.razor"));
        Assert.Contains("@inherits LocaleLayoutBase",Read("Components/Layout/EmptyLayout.razor"));
        Assert.DoesNotContain("@key",Read("Components/Routes.razor"));
    }

    [Fact]
    public void NormalFirstPartyCssCannotOverrideTheLanguageFamily()
    {
        string root=SourceRoot();
        string[] files=Directory.GetFiles(Path.Combine(root,"wwwroot"),"*.css")
            .Concat(Directory.GetFiles(Path.Combine(root,"Components"),"*.css",SearchOption.AllDirectories)).ToArray();
        Assert.NotEmpty(files);
        foreach(string path in files)
        {
            string css=File.ReadAllText(path);
            foreach(Match match in Regex.Matches(css,@"(?<![-\w])font-family\s*:\s*([^;}]+)"))
            {
                string value=match.Groups[1].Value.Trim();
                Assert.True(value=="inherit"||value=="var(--barang-font-family)"||value=="var(--barang-font-mono)"||value.Contains("monospace",StringComparison.Ordinal),
                    $"{path}: unapproved font-family {value}");
            }
            foreach(Match match in Regex.Matches(css,@"(?<![-\w])font\s*:\s*([^;}]+)"))
                Assert.True(match.Value.Contains("inherit",StringComparison.Ordinal)||match.Value.Contains("--barang-font-family",StringComparison.Ordinal)||match.Value.Contains("monospace",StringComparison.Ordinal),$"{path}: {match.Value}");
        }
        Assert.Equal(1,Regex.Matches(Read("Components/App.razor"),"LocalePolicy.FontStylesheetUrl").Count);
        Assert.Equal("fonts/barang-fonts.css",LocalePolicy.FontStylesheetUrl);
        Assert.Contains("@Assets[LocalePolicy.FontStylesheetUrl]",Read("Components/App.razor"));
        Assert.DoesNotMatch(@"(?i)fonts\.(?:googleapis|gstatic)\.com",Read("Components/App.razor"));
        string fontCss=Read("wwwroot/"+LocalePolicy.FontStylesheetUrl);
        Assert.DoesNotMatch(@"(?i)(?:https?:|//|data:|@import\b)",fontCss);
        foreach(string family in new[]{"Inter","Noto Sans Arabic","Vazirmatn"})
            Assert.Contains($"font-family: '{family}';",fontCss);
        MatchCollection faces=Regex.Matches(fontCss,@"@font-face\s*\{([^}]+)\}");
        Assert.NotEmpty(faces);
        foreach(Match face in faces)
        {
            Assert.Matches(@"font-style:\s*normal\s*;",face.Value);
            Assert.Matches(@"font-weight:\s*400\s+900\s*;",face.Value);
            Match source=Regex.Match(face.Value,@"src:\s*url\('\./(?<file>[a-z0-9-]+\.woff2)'\)\s+format\('woff2'\)\s*;");
            Assert.True(source.Success,$"Font face must have exactly one local WOFF2 source: {face.Value}");
            string asset=Path.Combine(root,"wwwroot","fonts",source.Groups["file"].Value);
            Assert.True(File.Exists(asset),$"Missing self-hosted font: {asset}");
            Assert.Equal("wOF2",System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(asset),0,4));
        }
        Assert.DoesNotContain("document.cookie",Read("wwwroot/app.js"));
    }

    [Fact]
    public async Task LiveCultureRerendersExistingInstancesWithoutDiscardingState()
    {
        CultureInfo original=CultureInfo.CurrentCulture,originalUi=CultureInfo.CurrentUICulture;
        try
        {
            using BunitContext context=new();
            IRenderedComponent<LiveHost> rendered=context.Render<LiveHost>();
            rendered.Find("button").Click();
            Assert.Equal("fa:1",rendered.Find("button").TextContent);
            foreach(string culture in new[]{"ar","en","fa"})
            {
                await rendered.InvokeAsync(()=>rendered.Instance.Switch(culture));
                rendered.WaitForAssertion(()=>Assert.Equal(culture+":1",rendered.Find("button").TextContent));
            }
        }
        finally{CultureInfo.CurrentCulture=original;CultureInfo.CurrentUICulture=originalUi;}
    }

    [Fact]
    public async Task CircuitCultureDoesNotChangeOtherUsersOrProcessDefaults()
    {
        CultureInfo original=CultureInfo.CurrentCulture,originalUi=CultureInfo.CurrentUICulture;
        CultureInfo? defaultCulture=CultureInfo.DefaultThreadCurrentCulture,defaultUi=CultureInfo.DefaultThreadCurrentUICulture;
        UiCultureState first=new(),second=new();string initial=second.Code;
        await first.ChangeAsync("ar");
        Assert.Equal(initial,second.Code);
        string? observed=null;
        UiCultureCircuitHandler handler=new(first);
        await handler.CreateInboundActivityHandler(_=>{observed=CultureInfo.CurrentUICulture.Name;return Task.CompletedTask;})(null!);
        Assert.Equal("ar",observed);
        Assert.Equal(original,CultureInfo.CurrentCulture);
        Assert.Equal(originalUi,CultureInfo.CurrentUICulture);
        Assert.Equal(defaultCulture,CultureInfo.DefaultThreadCurrentCulture);
        Assert.Equal(defaultUi,CultureInfo.DefaultThreadCurrentUICulture);
    }

    public sealed class LiveHost:ComponentBase
    {
        private string culture="fa";
        public void Switch(string code){culture=code;StateHasChanged();}
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<string>>(0);
            builder.AddAttribute(1,"Name","BarangCulture");
            builder.AddAttribute(2,"Value",culture);
            builder.AddAttribute(3,"ChildContent",(RenderFragment)(child=>{child.OpenComponent<Probe>(0);child.CloseComponent();}));
            builder.CloseComponent();
        }
    }

    public sealed class Probe:LocaleComponentBase
    {
        private int clicks;
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0,"button");builder.AddAttribute(1,"type","button");
            builder.AddAttribute(2,"onclick",EventCallback.Factory.Create(this,()=>clicks++));
            builder.AddContent(3,$"{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}:{clicks}");
            builder.CloseElement();
        }
    }

    private static string Read(string relative)=>File.ReadAllText(Path.Combine(SourceRoot(),relative));
    private static string SourceRoot()
    {
        DirectoryInfo? root=new(AppContext.BaseDirectory);
        while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;
        return Path.Combine(root?.FullName??throw new DirectoryNotFoundException(),"01_CORE","src","BARANG.Backoffice");
    }
}
