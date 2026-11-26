using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;
using BARANG.Core.Application.Pagination;

namespace BARANG.Core.Application.Catalog;

public sealed record SupplierCoreCommand(SupplierIdentityData Identity,Guid[] CategoryIds,SupplierFinancialData? Financial=null,long Version=0,long FinancialVersion=0);
public sealed record SupplierCoreView(Supplier Supplier,IReadOnlyList<Guid> CategoryIds,SupplierFinancialProfile? Financial);
public sealed record SupplierCoreQuery(string? Search=null,string? EntityType=null,string? Status=null,Guid? CategoryId=null,Guid? GradeId=null,bool? Preferred=null,int PageNumber=1,int PageSize=20,string SortBy="number",bool SortDescending=false);
public sealed record SupplierChildCommand<T>(T Data,long Version=0);
public sealed record SupplierDefaultAddress(SupplierAddress? Address);
public interface ISupplierCoreAdministration
{
    Task<Result<PagedResult<SupplierCoreView>>> SearchAsync(SupplierCoreQuery request,CancellationToken ct);
    Task<Result<SupplierCoreView>> GetAsync(Guid id,CancellationToken ct);
    Task<Result<SupplierCoreView>> SaveAsync(Guid? id,SupplierCoreCommand command,CancellationToken cancellationToken);
    Task<Result<SupplierCoreView>> StatusAsync(Guid id,string status,Guid? reason,long version,CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid id,long version,CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<T>>> ChildrenAsync<T>(Guid owner,CancellationToken cancellationToken) where T:SupplierOwnedEntity;
    Task<Result<SupplierAddress>> SaveAddressAsync(Guid owner,Guid? id,SupplierChildCommand<SupplierAddressData> command,CancellationToken cancellationToken);
    Task<Result<SupplierContact>> SaveContactAsync(Guid owner,Guid? id,SupplierChildCommand<SupplierContactData> command,CancellationToken cancellationToken);
    Task<Result<SupplierBankAccount>> SaveBankAsync(Guid owner,Guid? id,SupplierChildCommand<SupplierBankData> command,CancellationToken cancellationToken);
    Task<Result<T>> ChangeChildAsync<T>(Guid owner,Guid id,long version,bool? active,CancellationToken cancellationToken) where T:SupplierOwnedEntity;
    Task<Result<bool>> DeleteChildAsync<T>(Guid owner,Guid id,long version,CancellationToken cancellationToken) where T:SupplierOwnedEntity;
    Task<Result<SupplierDefaultAddress>> ResolveDefaultAddressAsync(Guid owner,CancellationToken cancellationToken);
}
