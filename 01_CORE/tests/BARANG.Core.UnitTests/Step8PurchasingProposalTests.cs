using BARANG.Core.Domain.Inventory;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class Step8PurchasingProposalTests
{
    private static readonly Guid Company = Guid.NewGuid();
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Variant = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ShortageStartsAsOpenProposalAndLinksOnce()
    {
        StockShortage shortage = StockShortage.Create(Company, Item, Warehouse, Variant, 10m, 3m, Now);
        Guid request = Guid.NewGuid();

        Assert.Equal(ShortageProposalStatus.Open, shortage.ProposalStatus);
        Assert.True(shortage.MarkConverted(request).IsSuccess);
        Assert.Equal(ShortageProposalStatus.Converted, shortage.ProposalStatus);
        Assert.Equal(request, shortage.PurchaseRequestId);
        Assert.Equal(InventoryErrors.InvalidTransition.Code, shortage.MarkConverted(Guid.NewGuid()).Error.Code);
    }

    [Fact]
    public void RecheckResolvesShortageWithoutReservingOrChangingDemand()
    {
        StockShortage shortage = StockShortage.Create(Company, Item, Warehouse, Variant, 10m, 3m, Now);
        shortage.Refresh(10m, Now.AddMinutes(1));

        Assert.False(shortage.IsActive);
        Assert.Equal(ShortageProposalStatus.Resolved, shortage.ProposalStatus);
        Assert.Equal(0m, shortage.ShortageQuantity);
        Assert.Equal(10m, shortage.RequiredQuantity);
        Assert.Equal(Now.AddMinutes(1), shortage.LastRecheckedAtUtc);
    }
}
