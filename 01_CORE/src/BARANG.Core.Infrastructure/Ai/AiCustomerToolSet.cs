using System.Globalization;
using System.Text.Json.Nodes;
using BARANG.Core.Application.Ai;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>پایه ابزارهای دستیار مشتری B2C. همه ابزارها فقط داده‌های خود مشتری را می‌بینند:
/// customerId از زمینه سرور می‌آید (هرگز از ورودی مدل) و هر کوئری با CustomerId فیلد سخت فیلتر می‌شود.
/// هیچ مجوز ERP کارکنان در این ابزارها نقشی ندارد؛ مالکیت داده با خودِ فیلتر customerId تضمین می‌شود.</summary>
public abstract class AiCustomerTool(CoreDbContext db) : IAiCustomerTool
{
    protected CoreDbContext Db { get; } = db;

    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual JsonObject ParameterSchema => new() { ["type"] = "object", ["properties"] = new JsonObject() };
    public virtual IReadOnlyList<string> RequiredParameters => [];
    public IReadOnlyList<string> AnyOfPermissions => []; // احراز هویت کافی است؛ مالکیت با فیلتر customerId تضمین است

    public async Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        if (context.CustomerId is not Guid customerId || customerId == Guid.Empty)
            return new JsonObject { ["error"] = "no_customer_profile", ["message"] = "The signed-in user has no customer profile." };
        try
        {
            return await ExecuteForCustomerAsync(customerId, context, arguments, ct);
        }
        catch (Exception exception)
        {
            return new JsonObject { ["error"] = "tool_failed", ["message"] = Truncate(exception.Message, 200) };
        }
    }

    protected abstract Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct);

    protected static string Str(JsonObject? args, string name, string fallback = "") => args?[name]?.GetValue<string>() is { Length: > 0 } value ? value : fallback;

    protected static int ArgInt(JsonObject? args, string name, int fallback) => args?[name] is { } node && int.TryParse(node.GetValue<string?>() ?? node.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;

    protected static JsonObject DateFields(DateOnly date, string culture) => new()
    {
        ["iso"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["text"] = AiCultureFormat.Date(date, culture)
    };

    protected static string Iso(DateTimeOffset value) => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}

// ─── my_orders_overview: تعداد سفارش‌های خود مشتری به تفکیک وضعیت ───
public sealed class MyOrdersOverviewTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_orders_overview";
    public override string Description => "All-time overview of THIS customer's own orders: total count, counts per status (Draft/Submitted/Cancelled/Delivered), this-month count, first/last order dates. Use for \"how many orders do I have\" questions.";
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        Guid company = context.CompanyId;
        int total = await Db.Orders.CountAsync(o => o.CompanyId == company && o.CustomerId == customerId, ct);
        var byStatus = await Db.Orders.Where(o => o.CompanyId == company && o.CustomerId == customerId)
            .GroupBy(o => o.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct);
        int thisMonth = await Db.Orders.CountAsync(o => o.CompanyId == company && o.CustomerId == customerId && o.OrderDate >= new DateOnly(today.Year, today.Month, 1), ct);
        DateOnly? first = await Db.Orders.Where(o => o.CompanyId == company && o.CustomerId == customerId).MinAsync(o => (DateOnly?)o.OrderDate, ct);
        DateOnly? last = await Db.Orders.Where(o => o.CompanyId == company && o.CustomerId == customerId).MaxAsync(o => (DateOnly?)o.OrderDate, ct);
        JsonObject status = [];
        foreach (string st in new[] { "Draft", "Submitted", "Cancelled", "Delivered" }) status[st] = byStatus.FirstOrDefault(r => r.Status == st)?.Count ?? 0;
        return new JsonObject
        {
            ["total_orders"] = total,
            ["this_month"] = thisMonth,
            ["by_status"] = status,
            ["first_order"] = first is { } f ? DateFields(f, context.Culture) : null,
            ["last_order"] = last is { } l ? DateFields(l, context.Culture) : null
        };
    }
}

// ─── my_recent_orders: آخرین سفارش‌های خود مشتری ───
public sealed class MyRecentOrdersTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_recent_orders";
    public override string Description => "The customer's own most recent orders (newest first) with order number, status, date and total. Use for \"my recent orders\", \"سفارش‌های من\".";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "How many orders (default 5, max 20)" } }
    };
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        int take = Math.Clamp(ArgInt(arguments, "take", 5), 1, 20);
        Guid company = context.CompanyId;
        var orders = await Db.Orders.Where(o => o.CompanyId == company && o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc).Take(take)
            .Select(o => new { o.Id, o.OrderNumber, Status = o.Status.ToString(), o.OrderDate, o.CreatedAtUtc })
            .ToListAsync(ct);
        List<Guid> ids = [.. orders.Select(o => o.Id)];
        Dictionary<Guid, decimal> totals = [];
        if (ids.Count > 0)
        {
            List<(Guid OrderId, decimal FinalTotal)> rows = await Db.OrderPriceSnapshots
                .Where(p => p.IsCurrent && ids.Contains(p.OrderId))
                .Select(p => new ValueTuple<Guid, decimal>(p.OrderId, p.FinalTotal))
                .ToListAsync(ct);
            totals = rows.GroupBy(r => r.OrderId).ToDictionary(g => g.Key, g => g.First().FinalTotal);
        }
        JsonArray items = [];
        foreach (var order in orders)
        {
            totals.TryGetValue(order.Id, out decimal total);
            items.Add(new JsonObject
            {
                ["orderNumber"] = order.OrderNumber,
                ["status"] = order.Status,
                ["orderDate"] = DateFields(order.OrderDate, context.Culture).DeepClone(),
                ["total"] = total,
                ["currency"] = "IRT"
            });
        }
        return new JsonObject { ["count"] = items.Count, ["orders"] = items };
    }
}

// ─── my_order_status_detail: جزئیات یک سفارش — فقط اگر متعلق به خود مشتری باشد ───
public sealed class MyOrderStatusDetailTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_order_status_detail";
    public override string Description => "Full status of ONE of the customer's own orders by order number: status, workflow stage, payment state, item count and total. Orders belonging to other customers are invisible: they simply return not_found.";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["orderNumber"] = new JsonObject { ["type"] = "string", ["description"] = "Order number (e.g. ORD-...)" } },
        ["required"] = new JsonArray("orderNumber")
    };
    public override IReadOnlyList<string> RequiredParameters => ["orderNumber"];
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        string raw = Str(arguments, "orderNumber").Trim();
        if (raw.Length == 0) return new JsonObject { ["error"] = "orderNumber parameter is required" };
        Guid company = context.CompanyId;
        // فیلتر سخت روی CustomerId: سفارش دیگران عملاً «یافت نشد» است — نه ممنوع‌الدیدنی که وجودش لو برود
        Domain.Orders.Order? order = await Db.Orders
            .FirstOrDefaultAsync(o => o.CompanyId == company && o.CustomerId == customerId && o.OrderNumber != null && EF.Functions.ILike(o.OrderNumber, raw), ct);
        if (order is null) return new JsonObject { ["error"] = "not_found", ["message"] = "No order with this number belongs to you." };
        Domain.Workflow.WorkflowInstance? instance = await Db.WorkflowInstances.FirstOrDefaultAsync(w => w.OrderId == order.Id, ct);
        Domain.Finance.OrderFinancialState? finance = await Db.OrderFinancialStates.FirstOrDefaultAsync(f => f.OrderId == order.Id, ct);
        int items = await Db.OrderItems.CountAsync(i => i.OrderId == order.Id, ct);
        Domain.Pricing.OrderPriceSnapshot? price = await Db.OrderPriceSnapshots
            .Where(p => p.OrderId == order.Id && p.IsCurrent).OrderByDescending(p => p.CalculatedAtUtc).FirstOrDefaultAsync(ct);
        return new JsonObject
        {
            ["orderNumber"] = order.OrderNumber,
            ["status"] = order.Status.ToString(),
            ["orderDate"] = DateFields(order.OrderDate, context.Culture).DeepClone(),
            ["submittedAtUtc"] = order.SubmittedAtUtc is { } submitted ? Iso(submitted) : null,
            ["items"] = items,
            ["workflowStage"] = instance?.CurrentStage.ToString(),
            ["workflowStatus"] = instance?.Status.ToString(),
            ["paymentStatus"] = finance?.Status.ToString(),
            ["total"] = price?.FinalTotal,
            ["currency"] = price?.CurrencyCode ?? "IRT"
        };
    }
}

// ─── my_wallet_balance ───
public sealed class MyWalletBalanceTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_wallet_balance";
    public override string Description => "The customer's own store wallet balance in IRT (credits minus debits).";
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        Guid company = context.CompanyId;
        decimal credit = await Db.CustomerWalletTransactions
            .Where(t => t.CompanyId == company && t.CustomerId == customerId && t.Direction == Domain.Customers.CustomerWalletDirection.Credit)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;
        decimal debit = await Db.CustomerWalletTransactions
            .Where(t => t.CompanyId == company && t.CustomerId == customerId && t.Direction == Domain.Customers.CustomerWalletDirection.Debit)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;
        return new JsonObject { ["balance"] = credit - debit, ["currency"] = "IRT", ["totalCredit"] = credit, ["totalDebit"] = debit };
    }
}

// ─── my_wallet_transactions ───
public sealed class MyWalletTransactionsTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_wallet_transactions";
    public override string Description => "The customer's own recent wallet transactions (credit/debit, amount, reason, date). Use for wallet history questions.";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "How many transactions (default 10, max 30)" } }
    };
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        int take = Math.Clamp(ArgInt(arguments, "take", 10), 1, 30);
        Guid company = context.CompanyId;
        var rows = await Db.CustomerWalletTransactions
            .Where(t => t.CompanyId == company && t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAtUtc).Take(take)
            .Select(t => new { Direction = t.Direction.ToString(), t.Amount, t.ReasonCode, t.ReferenceNote, t.CreatedAtUtc })
            .ToListAsync(ct);
        JsonArray items = [];
        foreach (var row in rows)
        {
            items.Add(new JsonObject
            {
                ["direction"] = row.Direction,
                ["amount"] = row.Amount,
                ["reason"] = row.ReasonCode,
                ["note"] = row.ReferenceNote,
                ["date"] = AiCultureFormat.DateTimeUtc(row.CreatedAtUtc, context.Culture),
                ["atUtc"] = Iso(row.CreatedAtUtc)
            });
        }
        return new JsonObject { ["count"] = items.Count, ["transactions"] = items, ["currency"] = "IRT" };
    }
}

// ─── my_profile_summary ───
public sealed class MyProfileSummaryTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_profile_summary";
    public override string Description => "The customer's own profile summary: name, customer code, mobile, email, customer type, status and default shipping address.";
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        Guid company = context.CompanyId;
        Domain.Customers.Customer? customer = await Db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == company && c.Id == customerId, ct);
        if (customer is null) return new JsonObject { ["error"] = "not_found" };
        Domain.Customers.CustomerAddress? address = await Db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CompanyId == company && a.CustomerId == customerId && a.IsActive)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.DisplayOrder).FirstOrDefaultAsync(ct);
        return new JsonObject
        {
            ["displayName"] = customer.DisplayName,
            ["customerCode"] = customer.CustomerCode,
            ["customerType"] = customer.CustomerType.ToString(),
            ["status"] = customer.Status.ToString(),
            ["mobile"] = customer.PrimaryMobile,
            ["email"] = customer.PrimaryEmail,
            ["memberSince"] = DateFields(DateOnly.FromDateTime(customer.CreatedAtUtc.UtcDateTime), context.Culture).DeepClone(),
            ["defaultAddress"] = address is null ? null : new JsonObject
            {
                ["title"] = address.Title,
                ["line"] = string.Join("، ", new[] { address.District, address.AddressLine1, address.AddressLine2 }.Where(x => !string.IsNullOrWhiteSpace(x))),
                ["cityCode"] = address.CityCode,
                ["postalCode"] = address.PostalCode
            }
        };
    }
}

// ─── my_favorites ───
public sealed class MyFavoritesTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_favorites";
    public override string Description => "The customer's own favorite designs (artwork title, category, date added).";
    public override JsonObject ParameterSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "How many favorites (default 10, max 50)" } }
    };
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        int take = Math.Clamp(ArgInt(arguments, "take", 10), 1, 50);
        Guid company = context.CompanyId;
        var rows = await Db.CustomerFavoriteDesigns
            .Where(f => f.CompanyId == company && f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAtUtc).Take(take)
            .Join(Db.Artworks, f => f.ArtworkId, a => a.Id, (f, a) => new { a.Title, Category = a.CategoryCode, f.CreatedAtUtc })
            .ToListAsync(ct);
        int total = await Db.CustomerFavoriteDesigns.CountAsync(f => f.CompanyId == company && f.CustomerId == customerId, ct);
        JsonArray items = [];
        foreach (var row in rows)
        {
            items.Add(new JsonObject
            {
                ["title"] = row.Title,
                ["category"] = row.Category,
                ["addedAt"] = AiCultureFormat.DateTimeUtc(row.CreatedAtUtc, context.Culture)
            });
        }
        return new JsonObject { ["total"] = total, ["favorites"] = items };
    }
}

// ─── my_referral_code_status ───
public sealed class MyReferralCodeStatusTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_referral_code_status";
    public override string Description => "The customer's own invite/referral code plus referred friends count and reward status (pending/rewarded with amounts).";
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        Guid company = context.CompanyId;
        string? code = await Db.CustomerReferralCodes.AsNoTracking()
            .Where(c => c.CompanyId == company && c.CustomerId == customerId)
            .OrderBy(c => c.CreatedAtUtc).Select(c => (string?)c.Code).FirstOrDefaultAsync(ct);
        var referrals = await Db.CustomerReferrals.AsNoTracking()
            .Where(r => r.CompanyId == company && r.ReferrerCustomerId == customerId)
            .Select(r => new { Status = r.Status.ToString(), r.RewardAmount, r.CreatedAtUtc })
            .ToListAsync(ct);
        return new JsonObject
        {
            ["inviteCode"] = code,
            ["codeGenerated"] = code is not null,
            ["referredCount"] = referrals.Count,
            ["rewardedCount"] = referrals.Count(r => r.Status == "Rewarded"),
            ["pendingCount"] = referrals.Count(r => r.Status == "Pending"),
            ["rewardTotal"] = referrals.Where(r => r.Status == "Rewarded").Sum(r => r.RewardAmount),
            ["currency"] = "IRT"
        };
    }
}

// ─── my_cart_items: سفارش‌های Draft = سبد خرید ───
public sealed class MyCartItemsTool(CoreDbContext db) : AiCustomerTool(db)
{
    public override string Name => "my_cart_items";
    public override string Description => "The customer's own shopping cart: draft orders with item count and total (same data as the /cart page).";
    protected override async Task<JsonObject> ExecuteForCustomerAsync(Guid customerId, AiToolContext context, JsonObject? arguments, CancellationToken ct)
    {
        Guid company = context.CompanyId;
        var drafts = await Db.Orders
            .Where(o => o.CompanyId == company && o.CustomerId == customerId && o.Status == Domain.Orders.OrderStatus.Draft)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new { o.Id, o.OrderNumber, o.CreatedAtUtc })
            .ToListAsync(ct);
        JsonArray items = [];
        decimal grandTotal = 0;
        foreach (var draft in drafts)
        {
            Domain.Pricing.OrderPriceSnapshot? price = await Db.OrderPriceSnapshots.AsNoTracking()
                .Where(p => p.OrderId == draft.Id && p.IsCurrent).OrderByDescending(p => p.CalculatedAtUtc).FirstOrDefaultAsync(ct);
            int itemCount = await Db.OrderItems.CountAsync(i => i.OrderId == draft.Id, ct);
            decimal total = price?.FinalTotal ?? 0;
            grandTotal += total;
            items.Add(new JsonObject
            {
                ["orderNumber"] = draft.OrderNumber,
                ["items"] = itemCount,
                ["total"] = total,
                ["currency"] = price?.CurrencyCode ?? "IRT",
                ["createdAtUtc"] = Iso(draft.CreatedAtUtc)
            });
        }
        return new JsonObject { ["cartCount"] = items.Count, ["grandTotal"] = grandTotal, ["currency"] = "IRT", ["items"] = items };
    }
}
