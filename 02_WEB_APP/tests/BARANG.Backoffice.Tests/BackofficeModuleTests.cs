using System.Security.Claims;
using BARANG.Backoffice.Components;

namespace BARANG.Backoffice.Tests;

public sealed class BackofficeModuleTests
{
    [Fact]public void EveryCompletedModuleHasUniquePermissionAndCoreReadRoute(){Assert.Equal(15,ModuleCatalog.All.Count);Assert.Equal(15,ModuleCatalog.All.Select(x=>x.Code).Distinct().Count());Assert.All(ModuleCatalog.All,x=>{Assert.StartsWith("api/v1/",x.ReadPath);Assert.Contains('.',x.Permission);});}
    [Fact]public void NavigationUsesRealPermissionClaims(){BackofficeModule orders=ModuleCatalog.All.Single(x=>x.Code=="orders");ClaimsPrincipal allowed=new(new ClaimsIdentity([new Claim("barang:permission","order.read")],"test"));ClaimsPrincipal denied=new(new ClaimsIdentity([],"test"));Assert.True(ModuleCatalog.IsVisible(orders,allowed));Assert.False(ModuleCatalog.IsVisible(orders,denied));}
    [Fact]public void SystemAdministratorCanReachAllModules(){ClaimsPrincipal admin=new(new ClaimsIdentity([new Claim(ClaimTypes.Role,"system.administrator")],"test"));Assert.All(ModuleCatalog.All,x=>Assert.True(ModuleCatalog.IsVisible(x,admin)));}
}
