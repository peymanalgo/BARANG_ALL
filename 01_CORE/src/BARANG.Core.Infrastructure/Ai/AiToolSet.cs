using System.Globalization;
using System.Text.Json.Nodes;
using BARANG.Core.Application.Ai;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>ابزارهای ERP برای مدل زبانی. هر ابزار پیش از اجرا مجوز فراخوان را از سرویس امنیت مرکزی کنترل می‌کند.</summary>
public abstract class AiErpTool(CoreDbContext db, IPermissionAuthorizationService authorization) : IAiTool
{
    protected CoreDbContext Db { get; } = db;
    protected IPermissionAuthorizationService Authorization { get; } = authorization;

    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual JsonObject ParameterSchema => new() { ["type"] = "object", ["properties"] = new JsonObject() };
    public virtual IReadOnlyList<string> RequiredParameters => [];
    public abstract IReadOnlyList<string> AnyOfPermissions { get; }
    public abstract Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct);

    protected async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct) => (await Authorization.AuthorizeAsync(permissionCode, ct)).IsSuccess;

    protected static string Str(JsonObject? args, string name, string fallback = "") => args?[name]?.GetValue<string>() is { Length: > 0 } value ? value : fallback;

    protected static int ArgInt(JsonObject? args, string name, int fallback) => args?[name] is { } node && int.TryParse(node.GetValue<string?>() ?? node.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;

    protected static JsonObject DateFields(DateOnly date, string culture) => new()
    {
        ["iso"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["text"] = AiCultureFormat.Date(date, culture)
    };

    protected static string Iso(DateTimeOffset value) => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}

/// <summary>قالب تاریخ بر اساس فرهنگ: جلالی برای fa و میلادی برای بقیه — سرور-side قبل از نمایش.</summary>
public static class AiCultureFormat
{
    private static readonly PersianCalendar Persian = new();

    public static string CultureDirection(string culture) => IsRtl(culture) ? "rtl" : "ltr";

    public static bool IsRtl(string culture) => culture.StartsWith("fa", StringComparison.OrdinalIgnoreCase) || culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    public static bool IsJalali(string culture) => culture.StartsWith("fa", StringComparison.OrdinalIgnoreCase);

    public static string Date(DateOnly value, string culture = "fa")
    {
        DateTime date = value.ToDateTime(TimeOnly.MinValue);
        return IsJalali(culture)
            ? $"{Persian.GetYear(date):0000}/{Persian.GetMonth(date):00}/{Persian.GetDayOfMonth(date):00}"
            : value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
    }

    public static string DateTimeUtc(DateTimeOffset value, string culture = "fa") => Date(DateOnly.FromDateTime(value.UtcDateTime), culture) + " " + value.UtcDateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
}

// ─── 1) today_order_summary ───
public sealed class TodayOrderSummaryTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "today_order_summary";
    public override string Description => "Order counts by status (draft/submitted/cancelled/delivered) and total value for today.";
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.OrderRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var rows = await Db.Orders.Where(o => o.OrderDate == today)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);
        List<Guid> ids = await Db.Orders.Where(o => o.OrderDate == today).Select(o => o.Id).ToListAsync(ct);
        decimal totalValue = ids.Count == 0 ? 0 : await Db.OrderPriceSnapshots.Where(p => ids.Contains(p.OrderId) && p.IsCurrent).SumAsync(p => (decimal?)p.FinalTotal, ct) ?? 0;
        JsonObject statusCounts = [];
        foreach (string status in new[] { "Draft", "Submitted", "Cancelled", "Delivered" }) statusCounts[status] = rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;
        return new JsonObject
        {
            ["today"] = DateFields(today, context.Culture),
            ["statusCounts"] = statusCounts,
            ["totalOrders"] = rows.Sum(r => r.Count),
            ["totalValue"] = totalValue,
            ["currency"] = "IRT"
        };
    }
}

// ─── 2) orders_by_stage ───
public sealed class OrdersByStageTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "orders_by_stage";
    public override string Description => "Count of orders per current workflow stage (administrative review, printing, packaging, shipping, ...).";
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.OrderRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        var rows = await Db.WorkflowInstances
            .GroupBy(w => new { w.CurrentStage, w.Status })
            .Select(g => new { Stage = g.Key.CurrentStage.ToString(), Status = g.Key.Status.ToString(), Count = g.Count() })
            .ToListAsync(ct);
        JsonArray stages = [];
        foreach (var row in rows) stages.Add(new JsonObject { ["stage"] = row.Stage, ["instanceStatus"] = row.Status, ["orders"] = row.Count });
        long totalOrders = await Db.Orders.CountAsync(ct);
        return new JsonObject { ["stages"] = stages, ["totalOrders"] = totalOrders };
    }
}

// ─── 3) ready_to_ship_count ───
public sealed class ReadyToShipTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "ready_to_ship_count";
    public override string Description => "Packages completed/at shipping warehouse and shipments ready to dispatch.";
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.LogisticsRead, SecurityPermissions.ShippingManage, SecurityPermissions.DeliveryManage];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        var packages = await Db.Packages
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);
        var shipments = await Db.Shipments
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);
        JsonObject packageCounts = [], shipmentCounts = [];
        foreach (var row in packages) packageCounts[row.Status] = row.Count;
        foreach (var row in shipments) shipmentCounts[row.Status] = row.Count;
        return new JsonObject
        {
            ["packagesCompletedOrAtShippingWarehouse"] = packages.Where(p => p.Status is "Completed" or "AtShippingWarehouse").Sum(p => p.Count),
            ["shipmentsReady"] = shipments.Where(s => s.Status == "Ready").Sum(s => s.Count),
            ["packageStatusCounts"] = packageCounts,
            ["shipmentStatusCounts"] = shipmentCounts
        };
    }
}

// ─── 4) my_cartable_tasks ───
public sealed class MyCartableTasksTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "my_cartable_tasks";
    public override string Description => "The current user's open/claimed cartable workflow tasks with stage, type and due time.";
    public override IReadOnlyList<string> AnyOfPermissions => [];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        var tasks = await Db.HumanTasks
            .Where(t => t.Status == Domain.Workflow.HumanTaskStatus.Open || (t.Status == Domain.Workflow.HumanTaskStatus.Claimed && t.ClaimedByUserId == context.UserId))
            .OrderByDescending(t => t.Priority).ThenBy(t => t.DueAtUtc)
            .Take(30)
            .Select(t => new { t.Id, Stage = t.Stage.ToString(), t.TaskTypeCode, Status = t.Status.ToString(), t.Priority, t.DueAtUtc, t.OrderId, t.ClaimedByUserId })
            .ToListAsync(ct);
        JsonArray items = [];
        foreach (var task in tasks) items.Add(new JsonObject
        {
            ["stage"] = task.Stage,
            ["type"] = task.TaskTypeCode,
            ["status"] = task.Status,
            ["priority"] = task.Priority,
            ["dueUtc"] = Iso(task.DueAtUtc),
            ["mine"] = task.ClaimedByUserId == context.UserId
        });
        return new JsonObject { ["openCount"] = tasks.Count, ["tasks"] = items };
    }
}

// ─── 5) inventory_by_warehouse ───
public sealed class InventoryByWarehouseTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "inventory_by_warehouse";
    public override string Description => "Stock balance per warehouse (on-hand/reserved/available) with top SKUs; filterable by warehouse name or SKU search (e.g. RAWTEE).";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["warehouse"] = new JsonObject { ["type"] = "string", ["description"] = "Optional warehouse name/code filter" },
            ["sku"] = new JsonObject { ["type"] = "string", ["description"] = "Optional SKU filter (e.g. RAWTEE)" },
            ["topSkus"] = new JsonObject { ["type"] = "integer", ["description"] = "Top SKUs per warehouse (default 5)" }
        }
    };
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.InventoryRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        string warehouseFilter = Str(arguments, "warehouse").Trim();
        string skuFilter = Str(arguments, "sku").Trim();
        int topSkus = Math.Clamp(ArgInt(arguments, "topSkus", 5), 1, 20);
        string warehousePattern = $"%{warehouseFilter}%";
        string skuPattern = $"%{skuFilter}%";
        var balances = await Db.StockBalances
            .Join(Db.Warehouses, b => b.WarehouseId, w => w.Id, (b, w) => new { Balance = b, Warehouse = w })
            .Join(Db.ProductVariants, x => x.Balance.ProductVariantId, v => v.Id, (x, v) => new { x.Balance, x.Warehouse, Variant = v })
            .Where(x => x.Balance.OnHand != 0 || x.Balance.Reserved != 0)
            .Where(x => warehouseFilter.Length == 0 || EF.Functions.ILike(x.Warehouse.Name, warehousePattern) || EF.Functions.ILike(x.Warehouse.Code, warehousePattern))
            .Where(x => skuFilter.Length == 0 || EF.Functions.ILike(x.Variant.Sku, skuPattern))
            .Select(x => new { x.Warehouse.Name, x.Warehouse.Code, x.Variant.Sku, x.Balance.OnHand, x.Balance.Reserved })
            .ToListAsync(ct);
        JsonArray warehouses = [];
        foreach (var group in balances.GroupBy(b => new { b.Name, b.Code }))
        {
            JsonObject topSkusNode = [];
            foreach (var sku in group.OrderByDescending(x => x.OnHand).Take(topSkus)) topSkusNode[sku.Sku] = new JsonObject { ["onHand"] = sku.OnHand, ["reserved"] = sku.Reserved };
            warehouses.Add(new JsonObject
            {
                ["warehouse"] = group.Key.Name,
                ["code"] = group.Key.Code,
                ["onHand"] = group.Sum(x => x.OnHand),
                ["reserved"] = group.Sum(x => x.Reserved),
                ["available"] = group.Sum(x => x.OnHand - x.Reserved),
                ["topSkus"] = topSkusNode
            });
        }
        return new JsonObject
        {
            ["warehouses"] = warehouses,
            ["totalOnHand"] = balances.Sum(b => b.OnHand),
            ["totalAvailable"] = balances.Sum(b => b.OnHand - b.Reserved)
        };
    }
}

// ─── 6) customer_summary ───
public sealed class CustomerSummaryTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "customer_summary";
    public override string Description => "Top customers by order count with status; wallet balances only when financial permission is held.";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "Number of customers (default 10)" } }
    };
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.CustomerRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        int take = Math.Clamp(ArgInt(arguments, "take", 10), 1, 50);
        var top = await Db.Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Orders = g.Count() })
            .OrderByDescending(x => x.Orders)
            .Take(take)
            .ToListAsync(ct);
        List<Guid> ids = top.Select(t => t.CustomerId).ToList();
        Dictionary<Guid, string> names = await Db.Customers.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.DisplayName, ct);
        bool financial = await HasPermissionAsync(SecurityPermissions.CustomerFinancialRead, ct) || await HasPermissionAsync(SecurityPermissions.FinanceRead, ct);
        Dictionary<Guid, decimal>? balances = null;
        if (financial)
        {
            balances = await Db.CustomerWalletTransactions
                .GroupBy(t => t.CustomerId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.Direction == Domain.Customers.CustomerWalletDirection.Credit ? x.Amount : -x.Amount), ct);
        }
        JsonArray items = [];
        foreach (var row in top) items.Add(new JsonObject
        {
            ["name"] = names.GetValueOrDefault(row.CustomerId, row.CustomerId.ToString()),
            ["orders"] = row.Orders,
            ["walletBalance"] = balances is not null ? balances.GetValueOrDefault(row.CustomerId, 0) : null
        });
        long total = await Db.Customers.CountAsync(ct);
        return new JsonObject { ["totalCustomers"] = total, ["top"] = items, ["walletBalancesIncluded"] = financial };
    }
}

// ─── 7) wallet_balance ───
public sealed class WalletBalanceTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "wallet_balance";
    public override string Description => "Wallet balance and recent transactions of one customer, searched by code or name.";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["customer"] = new JsonObject { ["type"] = "string", ["description"] = "Customer code or display name (required)" } },
        ["required"] = new JsonArray("customer")
    };
    public override IReadOnlyList<string> RequiredParameters => ["customer"];
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.CustomerFinancialRead, SecurityPermissions.FinanceRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        string search = Str(arguments, "customer").Trim();
        if (search.Length == 0) return new JsonObject { ["error"] = "customer parameter is required" };
        Domain.Customers.Customer? customer = await Db.Customers
            .Where(c => EF.Functions.ILike(c.CustomerCode, $"%{search}%") || EF.Functions.ILike(c.DisplayName, $"%{search}%"))
            .OrderBy(c => c.Status)
            .FirstOrDefaultAsync(ct);
        if (customer is null) return new JsonObject { ["error"] = "customer not found" };
        decimal balance = await Db.CustomerWalletTransactions.Where(t => t.CustomerId == customer.Id)
            .SumAsync(t => (decimal?)(t.Direction == Domain.Customers.CustomerWalletDirection.Credit ? t.Amount : -t.Amount), ct) ?? 0;
        var recent = await Db.CustomerWalletTransactions.Where(t => t.CustomerId == customer.Id)
            .OrderByDescending(t => t.CreatedAtUtc).Take(10)
            .Select(t => new { Direction = t.Direction.ToString(), t.Amount, t.ReasonCode, t.CreatedAtUtc }).ToListAsync(ct);
        JsonArray recentNode = [];
        foreach (var row in recent) recentNode.Add(new JsonObject { ["direction"] = row.Direction, ["amount"] = row.Amount, ["reason"] = row.ReasonCode, ["atUtc"] = Iso(row.CreatedAtUtc) });
        return new JsonObject
        {
            ["customer"] = customer.DisplayName,
            ["code"] = customer.CustomerCode,
            ["balance"] = balance,
            ["currency"] = "IRT",
            ["recent"] = recentNode
        };
    }
}

// ─── 8) sales_revenue ───
public sealed class SalesRevenueTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "sales_revenue";
    public override string Description => "Sales figures: payments today and revenue this month (net of reversals) plus order totals.";
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.FinanceRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
        DateTimeOffset monthStart = new(now.UtcDateTime.Year, now.UtcDateTime.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var paymentRows = await Db.Payments
            .Select(p => new { p.Amount, p.PaidAtUtc, IsPayment = p.RecordType == Domain.Finance.PaymentRecordType.Payment })
            .ToListAsync(ct);
        List<(decimal Amount, DateTimeOffset At, bool IsPayment)> payments = [.. paymentRows.Select(p => (p.Amount, p.PaidAtUtc, p.IsPayment))];
        decimal paymentsToday = Net(payments, p => DateOnly.FromDateTime(p.At.UtcDateTime) == today);
        decimal revenueMonth = Net(payments, p => p.At >= monthStart);
        int ordersToday = await Db.Orders.CountAsync(o => o.OrderDate == today, ct);
        int ordersMonth = await Db.Orders.CountAsync(o => o.OrderDate >= DateOnly.FromDateTime(monthStart.UtcDateTime), ct);
        var monthIds = await Db.Orders.Where(o => o.OrderDate >= DateOnly.FromDateTime(monthStart.UtcDateTime)).Select(o => o.Id).ToListAsync(ct);
        decimal orderValueMonth = monthIds.Count == 0 ? 0 : await Db.OrderPriceSnapshots.Where(p => monthIds.Contains(p.OrderId) && p.IsCurrent).SumAsync(p => (decimal?)p.FinalTotal, ct) ?? 0;
        return new JsonObject
        {
            ["today"] = new JsonObject
            {
                ["date"] = DateFields(today, context.Culture).DeepClone(),
                ["paymentsNet"] = paymentsToday,
                ["orders"] = ordersToday
            },
            ["thisMonth"] = new JsonObject
            {
                ["startDate"] = DateFields(DateOnly.FromDateTime(monthStart.UtcDateTime), context.Culture).DeepClone(),
                ["revenueNet"] = revenueMonth,
                ["orders"] = ordersMonth,
                ["orderValue"] = orderValueMonth
            },
            ["currency"] = "IRT"
        };
    }

    private static decimal Net(IEnumerable<(decimal Amount, DateTimeOffset At, bool IsPayment)> rows, Func<(decimal Amount, DateTimeOffset At, bool IsPayment), bool> where) =>
        rows.Where(where).Sum(r => r.IsPayment ? r.Amount : -r.Amount);
}

// ─── 9) production_stage_counts ───
public sealed class ProductionStageCountsTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "production_stage_counts";
    public override string Description => "Production jobs counted by job type (design/print/heatpress/ironing) and status.";
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.ProductionRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        var rows = await Db.ProductionJobs
            .GroupBy(j => new { j.JobType, j.Status })
            .Select(g => new { JobType = g.Key.JobType.ToString(), Status = g.Key.Status.ToString(), Count = g.Count() })
            .ToListAsync(ct);
        JsonArray items = [];
        foreach (var row in rows) items.Add(new JsonObject { ["jobType"] = row.JobType, ["status"] = row.Status, ["count"] = row.Count });
        return new JsonObject { ["jobs"] = items, ["total"] = rows.Sum(r => r.Count) };
    }
}

// ─── 10) quality_defect_summary ───
public sealed class QualityDefectSummaryTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "quality_defect_summary";
    public override string Description => "Quality defect quantities by defect type and severity within the recent period (default last 30 days).";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["days"] = new JsonObject { ["type"] = "integer", ["description"] = "Look-back window in days (default 30)" } }
    };
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.QCRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        int days = Math.Clamp(ArgInt(arguments, "days", 30), 1, 365);
        DateTimeOffset from = DateTimeOffset.UtcNow.AddDays(-days);
        var rows = await Db.QCDefects.Where(d => d.RecordedAtUtc >= from)
            .Join(Db.DefectTypes, d => d.DefectTypeId, t => t.Id, (d, t) => new { t.Name, d.Severity, d.Quantity })
            .GroupBy(x => new { x.Name, x.Severity })
            .Select(g => new { g.Key.Name, Severity = g.Key.Severity.ToString(), Count = g.Count(), Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        int inspections = await Db.QCInspections.CountAsync(i => i.CreatedAtUtc >= from, ct);
        JsonArray defects = [];
        foreach (var row in rows) defects.Add(new JsonObject { ["type"] = row.Name, ["severity"] = row.Severity, ["occurrences"] = row.Count, ["quantity"] = row.Quantity });
        return new JsonObject
        {
            ["windowDays"] = days,
            ["fromUtc"] = Iso(from),
            ["inspections"] = inspections,
            ["defects"] = defects
        };
    }
}

// ─── 11) search_reference_values ───
public sealed class SearchReferenceValuesTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "search_reference_values";
    public override string Description => "Search shared lookup/reference values by type code or value code.";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["search"] = new JsonObject { ["type"] = "string", ["description"] = "Text to search in type code, value code or type name (required)" },
            ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "Max results (default 20)" }
        },
        ["required"] = new JsonArray("search")
    };
    public override IReadOnlyList<string> RequiredParameters => ["search"];
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.LookupRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        string search = Str(arguments, "search").Trim();
        int take = Math.Clamp(ArgInt(arguments, "take", 20), 1, 100);
        if (search.Length == 0) return new JsonObject { ["error"] = "search parameter is required" };
        var rows = await Db.LookupValues
            .Join(Db.LookupTypes, v => v.LookupTypeId, t => t.Id, (v, t) => new { TypeCode = t.Code, TypeName = t.DisplayName, ValueCode = v.Code, v.IsActive })
            .Where(x => EF.Functions.ILike(x.TypeCode, $"%{search}%") || EF.Functions.ILike(x.ValueCode, $"%{search}%") || EF.Functions.ILike(x.TypeName, $"%{search}%"))
            .Take(take).ToListAsync(ct);
        JsonArray results = [];
        foreach (var row in rows) results.Add(new JsonObject { ["typeCode"] = row.TypeCode, ["typeName"] = row.TypeName, ["valueCode"] = row.ValueCode, ["active"] = row.IsActive });
        return new JsonObject { ["results"] = results };
    }
}

// ─── 12) order_status_detail ───
public sealed class OrderStatusDetailTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "order_status_detail";
    public override string Description => "Full status of one order: number, dates, workflow stage, financial state, item count and current total. Accepts order number or GUID.";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["order"] = new JsonObject { ["type"] = "string", ["description"] = "Order number (e.g. ORD-...) or order GUID (required)" } },
        ["required"] = new JsonArray("order")
    };
    public override IReadOnlyList<string> RequiredParameters => ["order"];
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.OrderRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        string raw = Str(arguments, "order").Trim();
        if (raw.Length == 0) return new JsonObject { ["error"] = "order parameter is required" };
        Domain.Orders.Order? order = Guid.TryParse(raw, out Guid id)
            ? await Db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct)
            : await Db.Orders.FirstOrDefaultAsync(o => o.OrderNumber != null && EF.Functions.ILike(o.OrderNumber, raw), ct);
        if (order is null) return new JsonObject { ["error"] = "order not found" };
        Domain.Workflow.WorkflowInstance? instance = await Db.WorkflowInstances.FirstOrDefaultAsync(w => w.OrderId == order.Id, ct);
        Domain.Finance.OrderFinancialState? finance = await Db.OrderFinancialStates.FirstOrDefaultAsync(f => f.OrderId == order.Id, ct);
        int items = await Db.OrderItems.CountAsync(i => i.OrderId == order.Id, ct);
        Domain.Pricing.OrderPriceSnapshot? price = await Db.OrderPriceSnapshots.Where(p => p.OrderId == order.Id && p.IsCurrent).OrderByDescending(p => p.CalculatedAtUtc).FirstOrDefaultAsync(ct);
        return new JsonObject
        {
            ["orderNumber"] = order.OrderNumber,
            ["status"] = order.Status.ToString(),
            ["orderDate"] = DateFields(order.OrderDate, context.Culture).DeepClone(),
            ["createdAtUtc"] = Iso(order.CreatedAtUtc),
            ["submittedAtUtc"] = order.SubmittedAtUtc is { } submitted ? Iso(submitted) : null,
            ["items"] = items,
            ["workflowStage"] = instance?.CurrentStage.ToString(),
            ["workflowStatus"] = instance?.Status.ToString(),
            ["financialStatus"] = finance?.Status.ToString(),
            ["totalValue"] = price?.FinalTotal,
            ["currency"] = price?.CurrencyCode ?? "IRT"
        };
    }
}

// ─── 13) artwork_bank_stats ───
public sealed class ArtworkBankStatsTool(CoreDbContext db, IPermissionAuthorizationService authorization) : AiErpTool(db, authorization)
{
    public override string Name => "artwork_bank_stats";
    public override string Description => "Artwork/design bank statistics: totals by lifecycle status, category and version count.";
    public override IReadOnlyList<string> AnyOfPermissions => [SecurityPermissions.ArtworkRead];
    public override async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        var lifecycle = await Db.Artworks.GroupBy(a => a.LifecycleStatus).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct);
        var categories = await Db.Artworks.Where(a => a.CategoryCode != null).GroupBy(a => a.CategoryCode!).Select(g => new { Category = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(10).ToListAsync(ct);
        int versions = await Db.ArtworkVersions.CountAsync(ct);
        int active = await Db.Artworks.CountAsync(a => a.IsActive, ct);
        JsonObject lifecycleNode = [];
        foreach (var row in lifecycle) lifecycleNode[row.Status] = row.Count;
        JsonArray categoryNode = [];
        foreach (var row in categories) categoryNode.Add(new JsonObject { ["category"] = row.Category, ["count"] = row.Count });
        return new JsonObject
        {
            ["total"] = lifecycle.Sum(l => l.Count),
            ["active"] = active,
            ["versions"] = versions,
            ["byLifecycle"] = lifecycleNode,
            ["byCategory"] = categoryNode
        };
    }
}
