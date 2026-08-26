using BARANG.Core.Application.Lookups;
using BARANG.Core.Domain.Lookups;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class LookupStore(CoreDbContext context) : ILookupStore
{
    public void Add(LookupType type)=>context.LookupTypes.Add(type); public void Add(LookupValue value)=>context.LookupValues.Add(value);
    public Task<LookupType?> FindTypeAsync(Guid id,CancellationToken ct)=>context.LookupTypes.SingleOrDefaultAsync(x=>x.Id==id,ct);
    public Task<LookupType?> FindTypeByCodeAsync(string code,CancellationToken ct)=>context.LookupTypes.SingleOrDefaultAsync(x=>x.Code==code,ct);
    public async Task<IReadOnlyList<LookupType>> ListTypesAsync(bool activeOnly,CancellationToken ct)=>await context.LookupTypes.AsNoTracking().Where(x=>!activeOnly||x.IsActive).OrderBy(x=>x.Code).ToListAsync(ct);
    public Task<bool> TypeCodeExistsAsync(string code,CancellationToken ct)=>context.LookupTypes.IgnoreQueryFilters().AnyAsync(x=>x.Code==code,ct);
    public Task<LookupValue?> FindValueAsync(Guid id,CancellationToken ct)=>context.LookupValues.Include(x=>x.Translations).SingleOrDefaultAsync(x=>x.Id==id,ct);
    public Task<LookupValue?> FindValueByCodeAsync(Guid typeId,string code,CancellationToken ct)=>context.LookupValues.Include(x=>x.Translations).SingleOrDefaultAsync(x=>x.LookupTypeId==typeId&&x.Code==code,ct);
    public async Task<IReadOnlyList<LookupValue>> ListValuesAsync(Guid typeId,bool activeOnly,CancellationToken ct)=>await context.LookupValues.AsNoTracking().Include(x=>x.Translations).Where(x=>x.LookupTypeId==typeId&&(!activeOnly||x.IsActive)).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Code).ToListAsync(ct);
    public Task<bool> ValueCodeExistsAsync(Guid typeId,string code,CancellationToken ct)=>context.LookupValues.AnyAsync(x=>x.LookupTypeId==typeId&&x.Code==code,ct);
}
