using System.Xml.Linq;
using BARANG.Backoffice.Components;
using Xunit;

namespace BARANG.Backoffice.Tests;

public sealed class SupplierSourcingUiTests
{
    private static string Root{get{DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;return root!.FullName;}}
    private static string Read(string path)=>File.ReadAllText(Path.Combine(Root,"01_CORE","src","BARANG.Backoffice",path));
    [Fact]public void SourcingExtendsSupplierEditorWithoutSeparateRoutesOrEmptyFutureTabs(){
        string editor=Read("Components/SupplierEditor.razor");
        Assert.Contains("SupplierSourcingWorkspace",editor);Assert.Contains("\"products\"",editor);Assert.Contains("\"prices\"",editor);
        foreach(string tab in new[]{"evaluation","quality","quality-incidents","contracts","documents"})Assert.Contains(tab,editor);
        Assert.DoesNotContain("@page",Read("Components/SupplierSourcingWorkspace.razor"));
    }
    [Fact]public void GridsAndDialogsReuseSharedDenseLocalizedComponents(){
        string source=Read("Components/SupplierSourcingWorkspace.razor");
        foreach(string component in new[]{"BarangToolbar","BarangPagination","BarangSortHeader","BarangIconButton","BarangModal","SupplierFields","LocaleDateInput","BarangConfirmDialog"})Assert.Contains(component,source);
        Assert.Contains("pageNumber={Page}",source);Assert.Contains("LoadRevision",source);Assert.Contains("data-sourcing-grid",source);
        Assert.DoesNotContain("font-family",source);Assert.DoesNotContain("code-pill",source);Assert.DoesNotContain("type=\"date\"",source);
    }
    [Fact]public void SharedFieldsPreserveCoreDefaultsAndPermitPreciseSourcingAmounts(){
        Assert.All(SupplierSourcingUiSchema.Terms.Where(x=>x.Kind=="decimal"),item=>Assert.Equal(4,item.Scale));
        Assert.All(SupplierUiSchema.Financial.Where(x=>x.Kind=="decimal"),item=>Assert.Equal(2,item.Scale));
        Assert.Contains("Labels",Read("Components/SupplierFields.razor"));
        Assert.Contains("0.0001",Read("Components/SupplierFields.razor"));
    }
    [Fact]public void SourcingSaveCancelReadonlyAndHistoricalErrorsAreReal(){
        string source=Read("Components/SupplierSourcingWorkspace.razor");
        foreach(string operation in new[]{"HttpMethod.Post","HttpMethod.Put","HttpMethod.Delete","activation","preferred","end-date","parentVersion=ParentVersion","await Changed.InvokeAsync()","Snapshot=State()","ReadOnly","CanEdit"})Assert.Contains(operation,source);
        Assert.Contains("PublishedImmutable",Read("Components/SupplierSourcingUiSchema.cs"));
        Assert.Contains("PriceOverlap",Read("Components/SupplierSourcingUiSchema.cs"));
    }
    [Fact]public void ProductSkuAndCurrencySelectorsAreSearchableScopedAndHistorical(){
        string source=Read("Components/SupplierSourcingSelect.razor");
        Assert.Contains("SearchableSelect",source);Assert.Contains("SearchChanged=\"Search\"",source);
        Assert.Contains("AuthClaimTypes.CompanyId",source);Assert.Contains("activeOnly=false",source);
        Assert.Contains("variants/options",source);Assert.Contains("ISOCurrencySymbol",source);
    }
    [Fact]public void SourcingResourcesHaveIdenticalNonemptyFaArEnKeys(){
        var docs=new[]{"",".ar",".en"}.Select(suffix=>XDocument.Parse(Read("Localization/SupplierSourcingStrings"+suffix+".resx"))).ToArray();
        string[] keys=docs[0].Root!.Elements("data").Select(x=>x.Attribute("name")!.Value).Order().ToArray();
        Assert.NotEmpty(keys);Assert.Equal(keys.Length,keys.Distinct().Count());
        foreach(var doc in docs){Assert.Equal(keys,doc.Root!.Elements("data").Select(x=>x.Attribute("name")!.Value).Order());Assert.All(doc.Root.Elements("data"),item=>Assert.False(string.IsNullOrWhiteSpace(item.Element("value")!.Value)));}
    }
}
