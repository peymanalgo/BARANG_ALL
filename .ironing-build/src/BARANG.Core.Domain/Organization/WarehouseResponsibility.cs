using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class WarehouseResponsibility
{
    private WarehouseResponsibility() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ApplicationUserId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<WarehouseResponsibility> Create(Guid id, Guid companyId, Guid warehouseId, Guid applicationUserId, DateTimeOffset now)
    {
        if (id == Guid.Empty || companyId == Guid.Empty || warehouseId == Guid.Empty || applicationUserId == Guid.Empty)
            return Result.Failure<WarehouseResponsibility>(Error.Validation("inventory.warehouse_responsibility.invalid"));
        return Result.Success(new WarehouseResponsibility { Id = id, CompanyId = companyId, WarehouseId = warehouseId, ApplicationUserId = applicationUserId, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public void SetActive(bool active, DateTimeOffset now) { if (IsActive == active) return; IsActive = active; UpdatedAtUtc = now; Version++; }
}
