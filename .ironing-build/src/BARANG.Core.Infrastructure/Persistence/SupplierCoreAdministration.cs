using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Auditing;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class SupplierCoreAdministration(CoreDbContext db,ITransactionExecutor transactions,ICompanyContext company,IAuditWriter audit,TimeProvider time):ISupplierCoreAdministration
{
    private DateTimeOffset Now=>time.GetUtcNow();
    private Task<Result<T>> Run<T>(Func<CancellationToken,Task<Result<T>>> action,CancellationToken ct)
    {company.RequireCompanyId();return transactions.ExecuteAsync(action,ct);}
    private Task<Supplier?> Owner(Guid id,CancellationToken ct)=>db.Suppliers.FromSqlInterpolated($"SELECT * FROM barang.suppliers WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct);
    private void Log(string action,string entity,Guid id,AuditChangeKind kind)=>audit.Write(new(action,entity,id.ToString(),CompanyId:company.RequireCompanyId(),ChangeKind:kind));
    private async Task<SupplierCoreView> View(Supplier supplier,CancellationToken ct)=>new(supplier,
        await db.Set<SupplierCategoryAssignment>().Where(x=>x.SupplierId==supplier.Id).Select(x=>x.CategoryId).ToArrayAsync(ct),
        await db.Set<SupplierFinancialProfile>().SingleOrDefaultAsync(x=>x.SupplierId==supplier.Id,ct));
    private async Task<bool> Reference(string typeCode,Guid? id,Guid? previous,CancellationToken ct)
    {
        if(id is null)return true;if(id==Guid.Empty)return false;
        var value=await (from v in db.LookupValues.AsNoTracking() join t in db.LookupTypes.AsNoTracking() on v.LookupTypeId equals t.Id
            where v.Id==id&&t.Code==typeCode&&(typeCode=="bank"||typeCode=="payment.method"?v.CompanyId==null:v.CompanyId==company.CompanyId) select new{Active=v.IsActive&&t.IsActive}).SingleOrDefaultAsync(ct);
        return value is not null&&(value.Active||id==previous);
    }
    private async Task<bool> Geography(string type,string? code,string? previous,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return true;
        return await (from v in db.LookupValues join t in db.LookupTypes on v.LookupTypeId equals t.Id
            where t.Code==type&&v.Code==code&&(v.IsActive&&t.IsActive||code==previous) select v.Id).AnyAsync(ct);
    }
    public Task<Result<SupplierCoreView>> GetAsync(Guid id,CancellationToken ct)=>Run(async token=>{
        var value=await db.Suppliers.SingleOrDefaultAsync(x=>x.Id==id,token);
        return value is null?Result.Failure<SupplierCoreView>(SupplierCoreErrors.NotFound):Result.Success(await View(value,token));
    },ct);
    public Task<Result<PagedResult<SupplierCoreView>>> SearchAsync(SupplierCoreQuery request,CancellationToken ct)=>Run(async token=>{
        if(request.PageNumber<1||request.PageSize is <1 or >100||request.EntityType is not (null or "" or "Individual" or "LegalEntity")||request.Status is not (null or "" or "Active" or "Inactive" or "Blocked")||(request.Search?.Length??0)>200)
            return Result.Failure<PagedResult<SupplierCoreView>>(SupplierCoreErrors.InvalidData);
        var query=db.Suppliers.AsNoTracking();
        if(!string.IsNullOrEmpty(request.EntityType))query=query.Where(x=>x.EntityType==request.EntityType);
        if(request.Status=="Blocked")query=query.Where(x=>x.IsBlocked);
        else if(request.Status=="Active")query=query.Where(x=>x.IsActive&&!x.IsBlocked);
        else if(request.Status=="Inactive")query=query.Where(x=>!x.IsActive&&!x.IsBlocked);
        if(request.CategoryId is Guid category)query=query.Where(x=>db.Set<SupplierCategoryAssignment>().Any(c=>c.SupplierId==x.Id&&c.CategoryId==category));
        if(request.GradeId is Guid grade)query=query.Where(x=>x.GradeId==grade);
        if(request.Preferred is bool preferred)query=query.Where(x=>x.IsPreferred==preferred);
        string search=request.Search?.Trim()??"";
        if(search.Length>0){
            string pattern="%"+search.Replace("\\","\\\\",StringComparison.Ordinal).Replace("%","\\%",StringComparison.Ordinal).Replace("_","\\_",StringComparison.Ordinal)+"%";
            var numberMatches=db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM barang.suppliers WHERE supplier_number::text ILIKE {pattern}");
            query=query.Where(x=>numberMatches.Contains(x.Id)||EF.Functions.ILike(x.SupplierCode,pattern,"\\")||EF.Functions.ILike(x.DisplayName,pattern,"\\")||
                x.LegalName!=null&&EF.Functions.ILike(x.LegalName,pattern,"\\")||x.NationalId!=null&&EF.Functions.ILike(x.NationalId,pattern,"\\")||
                x.PrimaryMobile!=null&&EF.Functions.ILike(x.PrimaryMobile,pattern,"\\")||x.PrimaryPhone!=null&&EF.Functions.ILike(x.PrimaryPhone,pattern,"\\")||
                x.PrimaryEmail!=null&&EF.Functions.ILike(x.PrimaryEmail,pattern,"\\"));
        }
        long count=await query.LongCountAsync(token);
        IOrderedQueryable<Supplier> ordered=(request.SortBy,request.SortDescending) switch{
            ("name",false)=>query.OrderBy(x=>x.DisplayName),("name",true)=>query.OrderByDescending(x=>x.DisplayName),
            ("type",false)=>query.OrderBy(x=>x.EntityType),("type",true)=>query.OrderByDescending(x=>x.EntityType),
            ("status",false)=>query.OrderBy(x=>x.IsBlocked).ThenBy(x=>x.IsActive),("status",true)=>query.OrderByDescending(x=>x.IsBlocked).ThenByDescending(x=>x.IsActive),
            ("mobile",false)=>query.OrderBy(x=>x.PrimaryMobile),("mobile",true)=>query.OrderByDescending(x=>x.PrimaryMobile),
            (_,true)=>query.OrderByDescending(x=>x.SupplierNumber),_=>query.OrderBy(x=>x.SupplierNumber)};
        var rows=await ordered.ThenBy(x=>x.Id).Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).ToListAsync(token);
        var ids=rows.Select(x=>x.Id).ToArray();
        var categories=await db.Set<SupplierCategoryAssignment>().AsNoTracking().Where(x=>ids.Contains(x.SupplierId)).ToListAsync(token);
        return Result.Success(new PagedResult<SupplierCoreView>(rows.Select(x=>new SupplierCoreView(x,categories.Where(c=>c.SupplierId==x.Id).Select(c=>c.CategoryId).ToArray(),null)),request.PageNumber,request.PageSize,count));
    },ct);
    public Task<Result<SupplierCoreView>> SaveAsync(Guid? id,SupplierCoreCommand command,CancellationToken cancellationToken)=>Run(async ct=>{
        if(command.Identity is null||command.CategoryIds is null||command.CategoryIds.Length is <1 or >100||command.CategoryIds.Distinct().Count()!=command.CategoryIds.Length)
            return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidData);
        Supplier? previous=id is Guid ownerId?await Owner(ownerId,ct):null;
        if(id is not null&&previous is null)return Result.Failure<SupplierCoreView>(SupplierCoreErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Result.Failure<SupplierCoreView>(SupplierCoreErrors.Concurrency);
        var valid=SupplierCoreRules.Identity(command.Identity);if(valid.IsFailure)return Result.Failure<SupplierCoreView>(valid.Error);
        var data=valid.Value;
        if(!await Reference("legal.entity.type",data.LegalEntityTypeId,previous?.LegalEntityTypeId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("supplier.grade",data.GradeId,previous?.GradeId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("supplier.hold.reason",data.HoldReasonId,previous?.HoldReasonId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        var oldCategories=previous is null?new List<SupplierCategoryAssignment>():await db.Set<SupplierCategoryAssignment>().Where(x=>x.SupplierId==previous.Id).ToListAsync(ct);
        foreach(Guid category in command.CategoryIds)
            if(!await Reference("supplier.category",category,oldCategories.Any(x=>x.CategoryId==category)?category:null,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        SupplierFinancialProfile? financial=previous is null?null:await db.Set<SupplierFinancialProfile>().SingleOrDefaultAsync(x=>x.SupplierId==previous.Id,ct);
        if(command.Financial is not null){
            if((financial?.Version??0)!=command.FinancialVersion)return Result.Failure<SupplierCoreView>(SupplierCoreErrors.Concurrency);
            var financialValid=SupplierCoreRules.Financial(command.Financial);if(financialValid.IsFailure)return Result.Failure<SupplierCoreView>(financialValid.Error);
        if(!await Reference("payment.term",command.Financial.PaymentTermId,financial?.PaymentTermId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("payment.condition",command.Financial.PaymentConditionId,financial?.PaymentConditionId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("payment.method",command.Financial.PaymentMethodId,financial?.PaymentMethodId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("shipping.method",command.Financial.ShippingMethodId,financial?.ShippingMethodId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("freight.delivery.term",command.Financial.FreightDeliveryTermId,financial?.FreightDeliveryTermId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        }
        if(previous is null&&await db.Suppliers.AnyAsync(x=>x.SupplierCode==data.SupplierCode,ct))return Result.Failure<SupplierCoreView>(CatalogErrors.DuplicateCode);
        Supplier supplier;
        if(previous is null){var created=Supplier.CreateCore(Guid.NewGuid(),company.RequireCompanyId(),data,Now);if(created.IsFailure)return Result.Failure<SupplierCoreView>(created.Error);supplier=created.Value;db.Suppliers.Add(supplier);}
        else {var changed=previous.UpdateCore(data,Now);if(changed.IsFailure)return Result.Failure<SupplierCoreView>(changed.Error);supplier=previous;}
        foreach(var old in oldCategories.Where(x=>!command.CategoryIds.Contains(x.CategoryId)))db.Remove(old);
        foreach(Guid category in command.CategoryIds.Where(x=>oldCategories.All(old=>old.CategoryId!=x)))db.Add(SupplierCategoryAssignment.Create(supplier.Id,supplier.CompanyId,category,Now));
        if(command.Financial is not null){
            if(financial is null){var created=SupplierFinancialProfile.Create(supplier.Id,supplier.CompanyId,command.Financial,Now);if(created.IsFailure)return Result.Failure<SupplierCoreView>(created.Error);db.Add(created.Value);}
            else {var changed=financial.Update(command.Financial,Now);if(changed.IsFailure)return Result.Failure<SupplierCoreView>(changed.Error);}
        }
        Log(id is null?"supplier.created":"supplier.updated","supplier",supplier.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);
        await db.SaveChangesAsync(ct);
        return Result.Success(await View(supplier,ct));
    },cancellationToken);
    public Task<Result<SupplierCoreView>> StatusAsync(Guid id,string status,Guid? reason,long version,CancellationToken cancellationToken)=>Run(async ct=>{
        var supplier=await Owner(id,ct);if(supplier is null)return Result.Failure<SupplierCoreView>(SupplierCoreErrors.NotFound);
        if(supplier.Version!=version)return Result.Failure<SupplierCoreView>(SupplierCoreErrors.Concurrency);
        if(!await Reference("supplier.hold.reason",reason,supplier.HoldReasonId,ct))return Result.Failure<SupplierCoreView>(SupplierCoreErrors.InvalidReference);
        var result=supplier.SetCoreStatus(status,reason,Now);if(result.IsFailure)return Result.Failure<SupplierCoreView>(result.Error);
        Log("supplier.status.changed","supplier",id,AuditChangeKind.Modified);await db.SaveChangesAsync(ct);return Result.Success(await View(supplier,ct));
    },cancellationToken);
    public Task<Result<bool>> DeleteAsync(Guid id,long version,CancellationToken cancellationToken)=>Run(async ct=>{
        var supplier=await Owner(id,ct);if(supplier is null)return Result.Failure<bool>(SupplierCoreErrors.NotFound);
        if(supplier.Version!=version)return Result.Failure<bool>(SupplierCoreErrors.Concurrency);
        if(await Protected("suppliers",id,ct))return Result.Failure<bool>(SupplierCoreErrors.Referenced);
        db.RemoveRange(await db.Set<SupplierCategoryAssignment>().Where(x=>x.SupplierId==id).ToListAsync(ct));
        var financial=await db.Set<SupplierFinancialProfile>().SingleOrDefaultAsync(x=>x.SupplierId==id,ct);
        if(financial is not null){if(await Protected("supplier_financial_profiles",financial.Id,ct))return Result.Failure<bool>(SupplierCoreErrors.Referenced);db.Remove(financial);}
        db.Suppliers.Remove(supplier);Log("supplier.deleted","supplier",id,AuditChangeKind.Archived);return Result.Success(true);
    },cancellationToken);
    private Task<bool> Protected(string table,Guid id,CancellationToken ct)=>db.Database.SqlQuery<bool>($"SELECT barang.supplier_has_protected_references({table},{id}) AS \"Value\"").SingleAsync(ct);
    public Task<Result<IReadOnlyList<T>>> ChildrenAsync<T>(Guid owner,CancellationToken cancellationToken) where T:SupplierOwnedEntity=>Run(async ct=>{
        if(!await db.Suppliers.AnyAsync(x=>x.Id==owner,ct))return Result.Failure<IReadOnlyList<T>>(SupplierCoreErrors.NotFound);
        return Result.Success<IReadOnlyList<T>>(await db.Set<T>().AsNoTracking().Where(x=>x.SupplierId==owner).OrderBy(x=>x.CreatedAtUtc).ThenBy(x=>x.Id).ToListAsync(ct));
    },cancellationToken);
    private async Task ClearDefaults<T>(Guid owner,Guid? except,CancellationToken ct) where T:SupplierOwnedEntity
    {
        var current=await db.Set<T>().Where(x=>x.SupplierId==owner&&x.IsDefault&&x.Id!=except).ToListAsync(ct);
        foreach(var row in current){row.SelectDefault(false,Now);Log("supplier.child.default.cleared",Table<T>(),row.Id,AuditChangeKind.Modified);}
        if(current.Count>0)await db.SaveChangesAsync(ct);
    }
    public Task<Result<SupplierAddress>> SaveAddressAsync(Guid owner,Guid? id,SupplierChildCommand<SupplierAddressData> command,CancellationToken cancellationToken)=>Run(async ct=>{
        var supplier=await Owner(owner,ct);if(supplier is null)return Result.Failure<SupplierAddress>(SupplierCoreErrors.NotFound);
        var previous=id is null?null:await db.Set<SupplierAddress>().SingleOrDefaultAsync(x=>x.SupplierId==owner&&x.Id==id,ct);
        if(id is not null&&previous is null)return Result.Failure<SupplierAddress>(SupplierCoreErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Result.Failure<SupplierAddress>(SupplierCoreErrors.Concurrency);
        if(command.Data is null)return Result.Failure<SupplierAddress>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Address(command.Data);if(valid.IsFailure)return Result.Failure<SupplierAddress>(valid.Error);var data=valid.Value;
        if(!await Reference("address.type",data.AddressTypeId,previous?.AddressTypeId,ct))return Result.Failure<SupplierAddress>(SupplierCoreErrors.InvalidReference);
        if(!await Geography("geography.province",data.ProvinceCode,previous?.ProvinceCode,ct)||
            data.CityCode is not null&&(data.ProvinceCode is null||!await Geography("geography.city."+data.ProvinceCode,data.CityCode,previous?.ProvinceCode==data.ProvinceCode?previous.CityCode:null,ct)))
            return Result.Failure<SupplierAddress>(SupplierCoreErrors.InvalidReference);
        if(data.IsDefault)await ClearDefaults<SupplierAddress>(owner,id,ct);
        SupplierAddress value;
        if(previous is null){value=SupplierAddress.Create(owner,supplier.CompanyId,data,Now).Value;db.Add(value);}
        else {var changed=previous.Update(data,Now);if(changed.IsFailure)return Result.Failure<SupplierAddress>(changed.Error);value=previous;}
        Log("supplier.address.saved","supplier_address",value.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);return Result.Success(value);
    },cancellationToken);
    public Task<Result<SupplierContact>> SaveContactAsync(Guid owner,Guid? id,SupplierChildCommand<SupplierContactData> command,CancellationToken cancellationToken)=>Run(async ct=>{
        var supplier=await Owner(owner,ct);if(supplier is null)return Result.Failure<SupplierContact>(SupplierCoreErrors.NotFound);
        var previous=id is null?null:await db.Set<SupplierContact>().SingleOrDefaultAsync(x=>x.SupplierId==owner&&x.Id==id,ct);
        if(id is not null&&previous is null)return Result.Failure<SupplierContact>(SupplierCoreErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Result.Failure<SupplierContact>(SupplierCoreErrors.Concurrency);
        if(command.Data is null)return Result.Failure<SupplierContact>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Contact(command.Data);if(valid.IsFailure)return Result.Failure<SupplierContact>(valid.Error);var data=valid.Value;
        if(!await Reference("contact.role",data.ContactRoleId,previous?.ContactRoleId,ct))return Result.Failure<SupplierContact>(SupplierCoreErrors.InvalidReference);
        if(data.IsPrimary)await ClearDefaults<SupplierContact>(owner,id,ct);
        SupplierContact value;
        if(previous is null){value=SupplierContact.Create(owner,supplier.CompanyId,data,Now).Value;db.Add(value);}
        else {var changed=previous.Update(data,Now);if(changed.IsFailure)return Result.Failure<SupplierContact>(changed.Error);value=previous;}
        Log("supplier.contact.saved","supplier_contact",value.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);return Result.Success(value);
    },cancellationToken);
    public Task<Result<SupplierBankAccount>> SaveBankAsync(Guid owner,Guid? id,SupplierChildCommand<SupplierBankData> command,CancellationToken cancellationToken)=>Run(async ct=>{
        var supplier=await Owner(owner,ct);if(supplier is null)return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.NotFound);
        var previous=id is null?null:await db.Set<SupplierBankAccount>().SingleOrDefaultAsync(x=>x.SupplierId==owner&&x.Id==id,ct);
        if(id is not null&&previous is null)return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.Concurrency);
        if(command.Data is null)return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.InvalidData);
        var valid=SupplierCoreRules.Bank(command.Data);if(valid.IsFailure)return Result.Failure<SupplierBankAccount>(valid.Error);var data=valid.Value;
        if(!await Reference("bank",data.BankId,previous?.BankId,ct))return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.InvalidReference);
        if(!await Reference("bank.account.type",data.AccountTypeId,previous?.AccountTypeId,ct))return Result.Failure<SupplierBankAccount>(SupplierCoreErrors.InvalidReference);
        if(data.IsDefault)await ClearDefaults<SupplierBankAccount>(owner,id,ct);
        SupplierBankAccount value;
        if(previous is null){value=SupplierBankAccount.Create(owner,supplier.CompanyId,data,Now).Value;db.Add(value);}
        else {var changed=previous.Update(data,Now);if(changed.IsFailure)return Result.Failure<SupplierBankAccount>(changed.Error);value=previous;}
        Log("supplier.bank.saved","supplier_bank_account",value.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);return Result.Success(value);
    },cancellationToken);
    public Task<Result<T>> ChangeChildAsync<T>(Guid owner,Guid id,long version,bool? active,CancellationToken cancellationToken) where T:SupplierOwnedEntity=>Run(async ct=>{
        if(await Owner(owner,ct) is null)return Result.Failure<T>(SupplierCoreErrors.NotFound);
        var value=await db.Set<T>().SingleOrDefaultAsync(x=>x.SupplierId==owner&&x.Id==id,ct);
        if(value is null)return Result.Failure<T>(SupplierCoreErrors.NotFound);if(value.Version!=version)return Result.Failure<T>(SupplierCoreErrors.Concurrency);
        if(active is bool enabled)value.ChangeActivation(enabled,Now);
        else{
            if(!value.IsActive)return Result.Failure<T>(SupplierCoreErrors.InvalidDefault);
            await ClearDefaults<T>(owner,id,ct);value.SelectDefault(true,Now);
        }
        Log(active is null?"supplier.child.default.changed":"supplier.child.activation.changed",Table<T>(),id,AuditChangeKind.Modified);
        return Result.Success(value);
    },cancellationToken);
    public Task<Result<bool>> DeleteChildAsync<T>(Guid owner,Guid id,long version,CancellationToken cancellationToken) where T:SupplierOwnedEntity=>Run(async ct=>{
        if(await Owner(owner,ct) is null)return Result.Failure<bool>(SupplierCoreErrors.NotFound);
        var value=await db.Set<T>().SingleOrDefaultAsync(x=>x.SupplierId==owner&&x.Id==id,ct);
        if(value is null)return Result.Failure<bool>(SupplierCoreErrors.NotFound);if(value.Version!=version)return Result.Failure<bool>(SupplierCoreErrors.Concurrency);
        if(await Protected(Table<T>(),id,ct))return Result.Failure<bool>(SupplierCoreErrors.Referenced);
        db.Remove(value);Log("supplier.child.deleted",Table<T>(),id,AuditChangeKind.Archived);return Result.Success(true);
    },cancellationToken);
    private static string Table<T>()=>typeof(T)==typeof(SupplierAddress)?"supplier_addresses":typeof(T)==typeof(SupplierContact)?"supplier_contacts":"supplier_bank_accounts";
    public Task<Result<SupplierDefaultAddress>> ResolveDefaultAddressAsync(Guid owner,CancellationToken cancellationToken)=>Run(async ct=>{
        if(!await db.Suppliers.AnyAsync(x=>x.Id==owner,ct))return Result.Failure<SupplierDefaultAddress>(SupplierCoreErrors.NotFound);
        return Result.Success(new SupplierDefaultAddress(await db.Set<SupplierAddress>().AsNoTracking().SingleOrDefaultAsync(x=>x.SupplierId==owner&&x.IsActive&&x.IsDefault,ct)));
    },cancellationToken);
}
