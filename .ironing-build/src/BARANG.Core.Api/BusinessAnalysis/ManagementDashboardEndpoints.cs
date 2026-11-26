using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Domain.Finance;
using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Workflow;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BARANG.Core.Api.BusinessAnalysis;

public static class ManagementDashboardEndpoints
{
    public static IEndpointRouteBuilder MapManagementDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/management/dashboard", GetAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CoreDbContext db,
        ICompanyContext company, IAuthorizationService authorization, HttpContext http, CancellationToken ct)
    {
        Guid companyId = company.RequireCompanyId();
        DateTimeOffset to = (toUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        DateTimeOffset from = (fromUtc ?? to.AddDays(-30)).ToUniversalTime();
        if (from >= to || to - from > TimeSpan.FromDays(366)) return Results.BadRequest(new { error = "dashboard.invalid_date_range" });

        bool orderRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.OrderRead)).Succeeded;
        bool workflowRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.WorkflowRead)).Succeeded;
        bool inventoryRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.InventoryRead)).Succeeded;
        bool purchasingRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.PurchasingRead)).Succeeded;
        bool financeRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.FinanceRead)).Succeeded;
        bool productionRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.ProductionRead)).Succeeded;
        bool qualityRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.QCRead)).Succeeded;
        bool logisticsRead = (await authorization.AuthorizeAsync(http.User, SecurityPermissions.LogisticsRead)).Succeeded;

        // Dashboard reads are a bounded aggregate, but they still need the same
        // transaction-local RLS context as every other company-scoped service.
        // Without this, direct read queries see an empty tenant even though the
        // normal transactional APIs can read the same company's records.
        await using IDbContextTransaction securityTransaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('barang.company_id', {companyId.ToString()}, true);", ct);

        int? openOrders = orderRead ? await db.Orders.CountAsync(x => x.CompanyId == companyId && x.Status != Domain.Orders.OrderStatus.Cancelled && x.Status != Domain.Orders.OrderStatus.Delivered, ct) : null;
        int? overdueTasks = workflowRead ? await db.HumanTasks.CountAsync(x => x.CompanyId == companyId && (x.Status == HumanTaskStatus.Open || x.Status == HumanTaskStatus.Claimed) && x.DueAtUtc < to, ct) : null;
        int? openShortageItems = inventoryRead ? await db.StockShortages.CountAsync(x => x.CompanyId == companyId && x.IsActive && x.ShortageQuantity > 0, ct) : null;
        var receivables = financeRead ? await ReceivablesAsync(db, companyId, ct) : [];

        var stageRows = workflowRead ? await db.HumanTasks.AsNoTracking().Where(x => x.CompanyId == companyId && (x.Status == HumanTaskStatus.Open || x.Status == HumanTaskStatus.Claimed))
            .GroupBy(x => x.Stage).Select(g => new { Stage = g.Key, Count = g.Count() }).OrderBy(x => x.Stage).ToListAsync(ct) : [];
        var stages = stageRows.Select(x => new DashboardStageView(x.Stage.ToString(), x.Count)).ToList();
        var attention = new List<DashboardAttentionView>();
        if (workflowRead) attention.AddRange(await db.HumanTasks.AsNoTracking()
            .Where(x => x.CompanyId == companyId && (x.Status == HumanTaskStatus.Open || x.Status == HumanTaskStatus.Claimed) && x.DueAtUtc < to)
            .OrderBy(x => x.DueAtUtc).ThenByDescending(x => x.Priority).Take(8)
            .Select(x => new DashboardAttentionView("task", x.Id, x.OrderId, x.TaskTypeCode, x.DueAtUtc, x.Priority >= 3 ? "urgent" : "attention", "/workspace/cartable"))
            .ToListAsync(ct));
        if (inventoryRead) attention.AddRange(await (from s in db.StockShortages.AsNoTracking()
            join i in db.OrderItems.AsNoTracking() on new { s.CompanyId, Id = s.OrderItemId } equals new { i.CompanyId, Id = i.Id }
            join o in db.Orders.AsNoTracking() on new { s.CompanyId, Id = i.OrderId } equals new { o.CompanyId, Id = o.Id }
            where s.CompanyId == companyId && i.CompanyId == companyId && o.CompanyId == companyId && s.IsActive && s.ShortageQuantity > 0 orderby s.CreatedAtUtc
            select new DashboardAttentionView("shortage", s.Id, o.Id, o.OrderNumber ?? "shortage", s.CreatedAtUtc, "urgent", "/workspace/inventory?section=shortages")).Take(8).ToListAsync(ct));
        attention = attention.OrderByDescending(x => x.Priority == "urgent").ThenBy(x => x.OccurredAtUtc).Take(12).ToList();

        var submittedRows = orderRead ? await db.Orders.AsNoTracking().Where(x => x.CompanyId == companyId && x.CreatedAtUtc >= from && x.CreatedAtUtc < to)
            .GroupBy(x => new { Year = x.CreatedAtUtc.Year, Day = x.CreatedAtUtc.DayOfYear })
            .Select(g => new { g.Key.Year, g.Key.Day, Count = g.Count() }).OrderBy(x => x.Year).ThenBy(x => x.Day).ToListAsync(ct) : [];
        var deliveredRows = logisticsRead ? await db.ShipmentStatusHistory.AsNoTracking().Where(x => x.CompanyId == companyId && x.Status == Domain.Logistics.ShipmentStatus.Delivered && x.OccurredAtUtc >= from && x.OccurredAtUtc < to)
            .GroupBy(x => new { Year = x.OccurredAtUtc.Year, Day = x.OccurredAtUtc.DayOfYear })
            .Select(g => new { g.Key.Year, g.Key.Day, Count = g.Count() }).OrderBy(x => x.Year).ThenBy(x => x.Day).ToListAsync(ct) : [];
        var submittedTrend = submittedRows.Select(x => new DashboardTrendView(new DateTime(x.Year, 1, 1).AddDays(x.Day - 1), x.Count)).ToList();
        var deliveredTrend = deliveredRows.Select(x => new DashboardTrendView(new DateTime(x.Year, 1, 1).AddDays(x.Day - 1), x.Count)).ToList();

        IResult response = Results.Ok(new
        {
            generatedAtUtc = DateTimeOffset.UtcNow,
            range = new { fromUtc = from, toUtc = to, boundary = "inclusive-exclusive" },
            scope = new { companyId, permissions = new { orderRead, workflowRead, inventoryRead, purchasingRead, financeRead, productionRead, qualityRead, logisticsRead } },
            primary = new { openOrders, overdueTasks, openShortageItems, receivables },
            workflowStages = stages,
            attention,
            trends = new { submitted = submittedTrend, delivered = deliveredTrend },
            modules = new
            {
                production = productionRead ? await db.ProductionJobs.CountAsync(x => x.CompanyId == companyId && x.Status != Domain.Production.ProductionJobStatus.Completed && x.Status != Domain.Production.ProductionJobStatus.Failed, ct) : (int?)null,
                quality = qualityRead ? await db.QCInspections.CountAsync(x => x.CompanyId == companyId && x.Status != Domain.Quality.QCInspectionStatus.Approved && x.Status != Domain.Quality.QCInspectionStatus.Rejected, ct) : (int?)null,
                purchasing = purchasingRead ? await db.PurchaseRequests.CountAsync(x => x.CompanyId == companyId && x.Status != Domain.Inventory.PurchaseRequestStatus.Closed && x.Status != Domain.Inventory.PurchaseRequestStatus.Cancelled, ct) + await db.PurchaseOrders.CountAsync(x => x.CompanyId == companyId && x.Status != Domain.Inventory.PurchaseOrderStatus.Closed && x.Status != Domain.Inventory.PurchaseOrderStatus.Cancelled, ct) : (int?)null,
                logistics = logisticsRead ? await db.Shipments.CountAsync(x => x.CompanyId == companyId && x.Status != Domain.Logistics.ShipmentStatus.Delivered && x.Status != Domain.Logistics.ShipmentStatus.Returned, ct) : (int?)null
            }
        });
        await securityTransaction.CommitAsync(ct);
        return response;
    }

    private static async Task<IReadOnlyList<DashboardReceivableView>> ReceivablesAsync(CoreDbContext db, Guid companyId, CancellationToken ct)
    {
        var issued = await (from invoice in db.Invoices.AsNoTracking()
            join snapshot in db.InvoiceSnapshots.AsNoTracking() on new { invoice.CompanyId, InvoiceId = invoice.Id } equals new { snapshot.CompanyId, snapshot.InvoiceId }
            where invoice.CompanyId == companyId && snapshot.CompanyId == companyId
            group snapshot by snapshot.CurrencyCode into g select new { Currency = g.Key, Total = g.Sum(x => x.FinalTotal) }).ToListAsync(ct);
        var paid = await db.Payments.AsNoTracking().Where(x => x.CompanyId == companyId).GroupBy(x => new { x.CurrencyCode, x.RecordType })
            .Select(g => new { g.Key.CurrencyCode, g.Key.RecordType, Total = g.Sum(x => x.Amount) }).ToListAsync(ct);
        return issued.Select(x => new DashboardReceivableView(x.Currency, x.Total - paid.Where(p => p.CurrencyCode == x.Currency).Sum(p => p.RecordType == PaymentRecordType.Payment ? p.Total : -p.Total))).ToArray();
    }

    private sealed record DashboardStageView(string Stage, int Count);
    private sealed record DashboardAttentionView(string Kind, Guid RecordId, Guid? OrderId, string Reference, DateTimeOffset OccurredAtUtc, string Priority, string Route);
    private sealed record DashboardTrendView(DateTime Day, int Count);
    private sealed record DashboardReceivableView(string Currency, decimal Amount);
}
