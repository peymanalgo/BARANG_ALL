namespace BARANG.Backoffice.Components;

public sealed record BackofficeModule(string Code,string Icon,string Permission,string ReadPath);
public static class ModuleCatalog
{
    public static readonly IReadOnlyList<BackofficeModule> All=
    [
        new("foundation","⌘","organization.company.manage","api/v1/organization/branches"),new("master-data","◇","product.read","api/v1/products?pageSize=20"),
        new("customers","♙","customer.read","api/v1/customers?pageSize=20"),new("artwork-files","▧","artwork.read","api/v1/artworks?pageSize=20"),
        new("orders","▤","order.read","api/v1/orders?pageSize=20"),new("finance","¤","finance.read","api/v1/orders?pageSize=10"),
        new("inventory","▦","inventory.read","api/v1/inventory/balances"),new("purchasing","▣","purchasing.read","api/v1/purchasing/orders"),
        new("production","⚙","production.read","api/v1/production/jobs?pageSize=20"),new("quality","✓","qc.read","api/v1/quality/analytics?from=2020-01-01T00:00:00Z&to=2099-01-01T00:00:00Z&top=10"),
        new("logistics","⌁","logistics.read","api/v1/orders?pageSize=10"),new("workflow","◷","workflow.read","api/v1/workflows/tasks/mine?pageSize=20"),
        new("reports","▥","audit.read","api/v1/audit/events?pageSize=20")
    ];
    public static bool IsVisible(BackofficeModule module,System.Security.Claims.ClaimsPrincipal user)=>user.IsInRole("system.administrator")||user.HasClaim("barang:permission",module.Permission);
}
