using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Inventory;

public sealed partial class InventoryAdministration
{
    private async Task RecheckShortagesAsync(Guid warehouseId, Guid variantId, CancellationToken cancellationToken)
    {
        StockBalance? balance = await LockBalance(warehouseId, variantId, cancellationToken);
        if (balance is null) return;
        List<StockShortage> shortages = await db.StockShortages.Where(x => x.WarehouseId == warehouseId && x.ProductVariantId == variantId && x.IsActive).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        foreach (StockShortage shortage in shortages) shortage.Refresh(balance.Available, Now);
    }

    public Task<Result<PurchaseRequestView>> ConvertShortageToRequestAsync(Guid shortageId, CancellationToken cancellationToken) =>
        tx.ExecuteAsync(async ct =>
        {
            StockShortage? candidate = await db.StockShortages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == shortageId && x.IsActive, ct);
            if (candidate is null) return Result.Failure<PurchaseRequestView>(InventoryErrors.NotFound);

            // Receipt/recheck locks the balance first and then the matching shortages. Keep the
            // same order here so conversion cannot deadlock with a concurrent receipt.
            StockBalance? balance = await LockBalance(candidate.WarehouseId, candidate.ProductVariantId, ct);
            if (balance is null) return Result.Failure<PurchaseRequestView>(InventoryErrors.NotFound);
            if (db.Database.IsNpgsql())
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM barang.stock_shortages WHERE company_id={Company} AND id={shortageId} FOR UPDATE", ct);

            StockShortage? shortage = await db.StockShortages.SingleOrDefaultAsync(x => x.Id == shortageId && x.IsActive, ct);
            if (shortage is null) return Result.Failure<PurchaseRequestView>(InventoryErrors.NotFound);

            PurchaseRequest? existing = await db.PurchaseRequests.SingleOrDefaultAsync(x => x.SourceShortageId == shortageId, ct);
            if (existing is not null) return Result.Success(RequestView(existing));

            if (balance.Available >= shortage.RequiredQuantity)
            {
                shortage.Refresh(balance.Available, Now);
                return Result.Failure<PurchaseRequestView>(InventoryErrors.InvalidTransition);
            }

            string uom = await Uom(shortage.ProductVariantId, ct);
            Result<PurchaseRequest> created = PurchaseRequest.Create(Company, shortage.WarehouseId, Actor, $"Shortage proposal {shortage.Id:N}", shortage.Id, Now);
            if (created.IsFailure) return Result.Failure<PurchaseRequestView>(created.Error);
            db.Add(created.Value);
            db.Add(PurchaseRequestItem.Create(Company, created.Value.Id, shortage.ProductVariantId, uom, shortage.ShortageQuantity));
            Result linked = shortage.MarkConverted(created.Value.Id);
            if (linked.IsFailure) return Result.Failure<PurchaseRequestView>(linked.Error);
            History("PURCHASE_REQUEST", created.Value.Id, "ProposalConverted", shortage.Id.ToString("N"));
            Log("purchasing.shortage.proposal.converted", created.Value.Id);
            return Result.Success(RequestView(created.Value));
        }, cancellationToken);
}
