using System.Xml.Linq;
using Xunit;

namespace BARANG.Backoffice.Tests;

public sealed class SupplierCoreUiTests
{
    private static string Root{get{DirectoryInfo? root=new(AppContext.BaseDirectory);while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"01_CORE")))root=root.Parent;return root!.FullName;}}
    private static string Read(string name)=>File.ReadAllText(Path.Combine(Root,"01_CORE","src","BARANG.Backoffice",name));
    [Fact]public void SuppliersUseExplicitRouteServerPagingAndSharedGridControls(){
        string page=Read("Components/Pages/SupplierManager.razor");
        Assert.Contains("@page \"/master/suppliers\"",page);Assert.Contains("pageNumber={Page}",page);Assert.Contains("sortBy={Sort}",page);
        Assert.Contains("BarangSortHeader",page);Assert.Contains("BarangPagination",page);Assert.Contains("entity-link",page);
        Assert.DoesNotContain("code-pill",page);Assert.DoesNotContain("SupplierProductLifecycle",page);
    }
    [Fact]public void RootEditorHasOnlyImplementedTabsAndRealSaveWithDirtyCancel(){
        string editor=Read("Components/SupplierEditor.razor");
        foreach(string tab in new[]{"general","addresses","contacts","financial","bank-accounts"})Assert.Contains(tab,editor);
        Assert.Contains("ReferenceMultiSelect",editor);Assert.Contains("HttpMethod.Put",editor);Assert.Contains("Saved.InvokeAsync()",editor);
        Assert.Contains("DiscardOpen",editor);Assert.Contains("ShowCloseButton=\"false\"",editor);
        foreach(string tab in new[]{"evaluation","quality","quality-incidents","contracts","documents"})Assert.Contains(tab,editor);
    }
    [Fact]public void AllThreeChildKindsShareOneEditorAndPersistentActions(){
        string children=Read("Components/SupplierChildWorkspace.razor");
        foreach(string operation in new[]{"HttpMethod.Post","HttpMethod.Put","HttpMethod.Delete","activation","primary","default"})Assert.Contains(operation,children);
        Assert.Contains("SupplierFields",children);Assert.Contains("SupplierUiSchema.Children",children);Assert.Contains("ReadOnly",children);
    }
    [Fact]public void DatesAndReferenceSelectorsUseSharedLanguageAwareComponents(){
        string fields=Read("Components/SupplierFields.razor");
        Assert.Contains("LocaleDateInput",fields);Assert.Contains("ReferenceSelect",fields);Assert.Contains("SearchableSelect",fields);
        Assert.Contains("CultureInfo.InvariantCulture",fields);Assert.DoesNotContain("font-family",fields);
    }
    [Fact]public void SupplierWritesRequireExistingPermissionAndCompanyContext(){
        string page=Read("Components/Pages/SupplierManager.razor"),editor=Read("Components/SupplierEditor.razor");
        Assert.Contains("supplier.manage",page);Assert.Contains("supplier.manage",editor);
        Assert.Contains("AuthClaimTypes.CompanyId",editor);Assert.Contains("LoadRevision",page);
        Assert.DoesNotContain("SystemAdministrator",editor);
    }
    [Fact]public void AllSupplierResourcesHaveIdenticalNonemptyThreeLanguageKeys(){
        var docs=new[]{"SupplierStrings.resx","SupplierStrings.ar.resx","SupplierStrings.en.resx"}.Select(name=>XDocument.Parse(Read("Localization/"+name))).ToArray();
        string[] keys=docs[0].Root!.Elements("data").Select(x=>x.Attribute("name")!.Value).Order().ToArray();
        Assert.Equal(174,keys.Length);Assert.Equal(keys.Length,keys.Distinct().Count());
        foreach(var doc in docs){Assert.Equal(keys,doc.Root!.Elements("data").Select(x=>x.Attribute("name")!.Value).Order());Assert.All(doc.Root.Elements("data"),x=>Assert.False(string.IsNullOrWhiteSpace(x.Element("value")!.Value)));}
    }
}
