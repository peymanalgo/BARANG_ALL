using BARANG.Core.Domain.Catalog;

namespace BARANG.Core.Application.Catalog;

public interface IProductSalesPriceStore
{
    void Add(ProductSalesPrice price);
    void Remove(ProductSalesPrice price);
    Task<ProductSalesPrice?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSalesPrice>> ListAsync(Guid productId, bool includeInactive, CancellationToken cancellationToken);
    Task<bool> OverlapsAsync(Guid productId, Guid? variantId, DateOnly start, DateOnly? end, Guid? excludeId, CancellationToken cancellationToken);
}
