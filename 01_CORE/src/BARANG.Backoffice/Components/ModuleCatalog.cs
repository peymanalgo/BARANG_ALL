namespace BARANG.Backoffice.Components;

public sealed record BackofficeModule(string Code,string Icon,string Permission,string ReadPath);
// FeatureCode: کد فیچر سیستم (barang.system_features، اسکوپ erp) — وقتی فیچر خاموش باشد آیتم منو مخفی می‌شود (پیش‌فرض: بدون فیچر = همیشه).
public sealed record BackofficeNavigationItem(string LabelKey,string Route,string Permission,string? FeatureCode=null,IReadOnlyList<BackofficeNavigationItem>? Children=null);
public sealed record BackofficeNavigationGroup(string LabelKey,string Icon,IReadOnlyList<BackofficeNavigationItem> Items,string? Route=null,string? Permission=null,string? FeatureCode=null);
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
    // featureEnabled: پیش‌بارگذاری‌شدهٔ وضعیت فیچرها (null = fail-open همه روشن) — آیتمِ دارای FeatureCode وقتی فیچرش خاموش است دیده نمی‌شود.
    public static bool IsVisible(BackofficeNavigationItem item,System.Security.Claims.ClaimsPrincipal user,Func<string,bool>? featureEnabled=null)
        =>(item.FeatureCode is null||featureEnabled?.Invoke(item.FeatureCode) is not false)
        &&(item.Children is not null ? item.Children.Any(child=>IsVisible(child,user,featureEnabled)) : user.IsInRole("system.administrator")||user.HasClaim("barang:permission",item.Permission));
    public static bool IsVisible(BackofficeNavigationGroup group,System.Security.Claims.ClaimsPrincipal user,Func<string,bool>? featureEnabled=null)
        =>(group.FeatureCode is null||featureEnabled?.Invoke(group.FeatureCode) is not false)
        &&(group.Permission is null||user.IsInRole("system.administrator")||user.HasClaim("barang:permission",group.Permission));
    public static readonly IReadOnlyList<BackofficeNavigationGroup> Navigation=
    [
        Group("Nav_B2CSiteManagement","globe",
            Item("Nav_B2C_SiteSettings","/manage/b2c/settings","b2c.site.read"),
            Item("Nav_B2C_Pages","/manage/b2c/pages","b2c.site.read"),
            Item("Nav_B2C_Footer","/manage/b2c/footer","b2c.site.read"),
            Item("Nav_B2C_MainMenu","/manage/b2c/menu","b2c.site.read"),
            Item("Nav_B2C_Blog","/manage/b2c/blog","b2c.site.read")),
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
                Item("Nav_Suppliers","/master/suppliers","supplier.read","suppliers"),
                Item("Nav_Equipment","/master/equipment","equipment.read","equipment"),
                Item("Nav_Shifts","/master/shifts","organization.settings.manage","shifts"),
                Item("Nav_Products","/manage/products","product.read","products"),
                Item("Nav_UserAccessManagement","/admin/user-access","security.user.read"),
                Item("Nav_AI_ModelManagement","/master/ai-models","ai.settings.read"),
                Item("Nav_SystemFeatures","/system/features","features.manage"),
                Item("Nav_Workflow","/workspace/workflow","workflow.read","workflow.bpms"),
                Item("Nav_Services","/master/services","lookup.read")),
        Direct("Nav_Customers","users","/manage/customers","customer.read"),
        Direct("Nav_CustomerFeedbacks","message","/workspace/feedbacks","customer.read","customer.feedback"),
        Direct("Nav_CustomerWallet","wallet","/workspace/wallet","customer.read","customer.wallet"),
        Direct("Nav_ArtworkBank","image","/manage/design-bank","artwork.read","design.bank"),
        Direct("Nav_Orders","orders","/workspace/orders","order.read","orders"),
        Direct("Nav_Cartable","inbox","/cartable","workflow.read","cartable"),
        Direct("Nav_Pricing","finance","/workspace/pricing","pricing.read","pricing"),
        Direct("Nav_InventoryPurchasing","warehouse","/workspace/inventory","inventory.read","inventory.purchasing"),
        Direct("Nav_Production","settings","/workspace/production","production.read","production"),
        Direct("Nav_Quality","quality","/workspace/quality","qc.read","quality"),
        Direct("Nav_Logistics","truck","/workspace/logistics","logistics.read","logistics"),
        Direct("Nav_Notifications","bell","/workspace/notifications","notification.read"),
        Direct("Nav_ManagementDashboard","chart","/workspace/management","audit.read","management.dashboard"),
    ];
    private static BackofficeNavigationGroup Group(string key,string icon,params BackofficeNavigationItem[] items)=>new(key,icon,items);
    private static BackofficeNavigationGroup Direct(string key,string icon,string route,string permission,string? feature=null)=>new(key,icon,[],route,permission,feature);
    private static BackofficeNavigationItem Item(string key,string route,string permission,string? feature=null)=>new(key,route,permission,feature);
}
