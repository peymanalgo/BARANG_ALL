namespace BARANG.Backoffice.Components;

public sealed record BackofficeModule(string Code,string Icon,string Permission,string ReadPath);
public sealed record BackofficeNavigationItem(string LabelKey,string Route,string Permission,IReadOnlyList<BackofficeNavigationItem>? Children=null);
public sealed record BackofficeNavigationGroup(string LabelKey,string Icon,IReadOnlyList<BackofficeNavigationItem> Items,string? Route=null,string? Permission=null);
public static class ModuleCatalog
{
    public static readonly IReadOnlyList<BackofficeModule> All=
    [
        new("master-data","database","product.read","api/v1/products?pageSize=20"),
        new("customers","users","customer.read","api/v1/customers?pageSize=20"),new("artwork-files","image","artwork.read","api/v1/artworks?pageSize=20"),
        new("orders","orders","order.read","api/v1/orders?pageSize=20"),new("finance","finance","finance.read","api/v1/orders?pageSize=10"),
        new("inventory","warehouse","inventory.read","api/v1/inventory/balances"),new("purchasing","warehouse","purchasing.read","api/v1/purchasing/orders"),
        new("production","settings","production.read","api/v1/production/jobs?pageSize=20"),new("quality","quality","qc.read","api/v1/quality/analytics?from=2020-01-01T00:00:00Z&to=2099-01-01T00:00:00Z&top=10"),
        new("logistics","truck","logistics.read","api/v1/orders?pageSize=10"),new("workflow","workflow","workflow.read","api/v1/workflows/tasks/mine?pageSize=20"),
        new("notifications","bell","notification.read","api/v1/notifications/mine?pageSize=20"),
        new("audit","search","audit.read","api/v1/audit/events?pageSize=20"),
        new("reports","chart","audit.read","api/v1/audit/events?pageSize=20")
    ];
    public static bool IsVisible(BackofficeModule module,System.Security.Claims.ClaimsPrincipal user)=>user.IsInRole("system.administrator")||user.HasClaim("barang:permission",module.Permission);
    public static bool IsVisible(BackofficeNavigationItem item,System.Security.Claims.ClaimsPrincipal user)=>item.Children is not null ? item.Children.Any(child=>IsVisible(child,user)) : user.IsInRole("system.administrator")||user.HasClaim("barang:permission",item.Permission);
    public static bool IsVisible(BackofficeNavigationGroup group,System.Security.Claims.ClaimsPrincipal user)=>group.Permission is null||user.IsInRole("system.administrator")||user.HasClaim("barang:permission",group.Permission);
    public static readonly IReadOnlyList<BackofficeNavigationGroup> Navigation=
    [
        Group("Nav_MasterData","database",
            Item("Nav_Reference_shared","/master/reference-data/shared","lookup.read"),
            Item("Nav_Reference_customers","/master/reference-data/customers","lookup.read"),
            Item("Nav_Reference_suppliers","/master/reference-data/suppliers","lookup.read"),
            Item("Nav_Reference_products","/master/reference-data/products","lookup.read"),
            Item("Nav_Reference_artwork","/master/reference-data/artwork","lookup.read"),
            Item("Nav_Reference_orders","/master/reference-data/orders","lookup.read"),
            Item("Nav_Reference_inventory","/master/reference-data/inventory","lookup.read"),
            Item("Nav_Reference_equipment","/master/reference-data/equipment","lookup.read"),
            Item("Nav_Reference_production","/master/reference-data/production","lookup.read"),
            Item("Nav_Reference_quality","/master/reference-data/quality","lookup.read"),
            Item("Nav_Reference_logistics","/master/reference-data/logistics","lookup.read"),
            Item("Nav_Reference_workflow","/master/reference-data/workflow","lookup.read")),
        Group("Nav_SystemManagement","shield",
                Item("Nav_OrganizationalChart","/master/organization-chart","organization.unit.manage"),
                Item("Nav_Persons","/master/persons","organization.unit.manage"),
                Item("Nav_Suppliers","/master/suppliers","supplier.read"),
                Item("Nav_Equipment","/master/equipment","equipment.read"),
                Item("Nav_Shifts","/master/shifts","organization.settings.manage"),
                Item("Nav_Products","/manage/products","product.read"),
                Item("Nav_UserAccessManagement","/admin/user-access","security.user.read"),
                Item("Nav_Workflow","/workspace/workflow","workflow.read")),
        Direct("Nav_Customers","users","/manage/customers","customer.read"),
        Direct("Nav_ArtworkBank","image","/manage/design-bank","artwork.read"),
        Direct("Nav_Orders","orders","/workspace/orders","order.read"),
        Direct("Nav_Cartable","inbox","/cartable","workflow.read"),
        Direct("Nav_Pricing","finance","/workspace/pricing","pricing.read"),
        Direct("Nav_InventoryPurchasing","warehouse","/workspace/inventory","inventory.read"),
        Direct("Nav_Production","settings","/workspace/production","production.read"),
        Direct("Nav_Quality","quality","/workspace/quality","qc.read"),
        Direct("Nav_Logistics","truck","/workspace/logistics","logistics.read"),
        Direct("Nav_Notifications","bell","/workspace/notifications","notification.read"),
        Direct("Nav_ManagementDashboard","chart","/workspace/management","audit.read"),
    ];
    private static BackofficeNavigationGroup Group(string key,string icon,params BackofficeNavigationItem[] items)=>new(key,icon,items);
    private static BackofficeNavigationGroup Direct(string key,string icon,string route,string permission)=>new(key,icon,[],route,permission);
    private static BackofficeNavigationItem Item(string key,string route,string permission)=>new(key,route,permission);
}
