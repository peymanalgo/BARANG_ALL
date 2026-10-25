using BARANG.Core.Application.Customers;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class CustomerStore(CoreDbContext db):ICustomerStore
{
    public Task<Customer?> LockOwnerAsync(Guid customerId,CancellationToken ct) => db.Customers.FromSqlInterpolated($"SELECT * FROM barang.customers WHERE id={customerId} FOR UPDATE").SingleOrDefaultAsync(ct);
    public Task FlushAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public void Remove(CustomerAddress value) => db.CustomerAddresses.Remove(value);
    public void Remove(CustomerContact value) => db.CustomerContacts.Remove(value);
    public void Remove(CustomerBankAccount value) => db.CustomerBankAccounts.Remove(value);
    public Task<CustomerReferenceValue?> FindReferenceAsync(string typeCode,Guid? valueId,string? code,CancellationToken ct) =>
        (from value in db.LookupValues.AsNoTracking()
         join type in db.LookupTypes.AsNoTracking() on value.LookupTypeId equals type.Id
         where type.Code==typeCode && (valueId!=null ? value.Id==valueId : value.Code==code)
         orderby type.CompanyId!=null descending
         select new CustomerReferenceValue(value.Id,value.Code,value.IsActive&&type.IsActive)).FirstOrDefaultAsync(ct);
    public void Add(Customer value)=>db.Customers.Add(value); public void Add(IndividualCustomerProfile value)=>db.IndividualCustomerProfiles.Add(value); public void Add(LegalCustomerProfile value)=>db.LegalCustomerProfiles.Add(value); public void Add(CustomerContact value)=>db.CustomerContacts.Add(value); public void Add(CustomerAddress value)=>db.CustomerAddresses.Add(value); public void Add(CustomerFinancialProfile value)=>db.CustomerFinancialProfiles.Add(value); public void Add(CustomerBankAccount value)=>db.CustomerBankAccounts.Add(value);
    public Task<Customer?>FindAsync(Guid id,CancellationToken ct)=>db.Customers.SingleOrDefaultAsync(x=>x.Id==id,ct);
    public Task<Customer?>FindByCodeAsync(string code,CancellationToken ct)=>db.Customers.SingleOrDefaultAsync(x=>x.CustomerCode==code,ct);
    public Task<IndividualCustomerProfile?>FindIndividualProfileAsync(Guid customerId,bool tracking,CancellationToken ct)=>(tracking?db.IndividualCustomerProfiles:db.IndividualCustomerProfiles.AsNoTracking()).SingleOrDefaultAsync(x=>x.CustomerId==customerId,ct);
    public Task<LegalCustomerProfile?>FindLegalProfileAsync(Guid customerId,bool tracking,CancellationToken ct)=>(tracking?db.LegalCustomerProfiles:db.LegalCustomerProfiles.AsNoTracking()).SingleOrDefaultAsync(x=>x.CustomerId==customerId,ct);
    public Task<bool>CodeExistsAsync(string code,CancellationToken ct)=>db.Customers.AnyAsync(x=>x.CustomerCode==code,ct);
    public Task<bool>IdentifierExistsAsync(CustomerType type,string? national,string? registration,Guid? excludingCustomerId,CancellationToken ct)
        =>type==CustomerType.Individual
            ?db.IndividualCustomerProfiles.AnyAsync(x=>(excludingCustomerId==null||x.CustomerId!=excludingCustomerId)&&national!=null&&x.NationalId==national,ct)
            :db.LegalCustomerProfiles.AnyAsync(x=>(excludingCustomerId==null||x.CustomerId!=excludingCustomerId)&&((national!=null&&x.NationalId==national)||(registration!=null&&x.RegistrationNumber==registration)),ct);

    public async Task<PagedResult<CustomerDirectoryRow>>SearchAsync(CustomerQuery query,CancellationToken ct)
    {
        PagedRequest page=new(query.PageNumber,query.PageSize);
        IQueryable<Customer> customers=db.Customers.AsNoTracking();
        if(query.CustomerType is { } type)customers=customers.Where(x=>x.CustomerType==type);
        if(query.Status is { } status)customers=customers.Where(x=>x.Status==status);
        string search=query.Search?.Trim()??"";
        if(search.Length>0)
        {
            string pattern=$"%{search.Replace("%","\\%").Replace("_","\\_")}%";
            customers=customers.Where(x=>EF.Functions.ILike(x.CustomerCode,pattern,"\\")||EF.Functions.ILike(x.DisplayName,pattern,"\\")||(x.PrimaryMobile!=null&&EF.Functions.ILike(x.PrimaryMobile,pattern,"\\"))||(x.PrimaryEmail!=null&&EF.Functions.ILike(x.PrimaryEmail,pattern,"\\"))||(x.EconomicCode!=null&&EF.Functions.ILike(x.EconomicCode,pattern,"\\"))||db.IndividualCustomerProfiles.Any(p=>p.CustomerId==x.Id&&(EF.Functions.ILike(p.FirstName,pattern,"\\")||EF.Functions.ILike(p.LastName,pattern,"\\")||(p.NationalId!=null&&EF.Functions.ILike(p.NationalId,pattern,"\\"))))||db.LegalCustomerProfiles.Any(p=>p.CustomerId==x.Id&&(EF.Functions.ILike(p.LegalName,pattern,"\\")||(p.TradeName!=null&&EF.Functions.ILike(p.TradeName,pattern,"\\"))||(p.NationalId!=null&&EF.Functions.ILike(p.NationalId,pattern,"\\"))||(p.RegistrationNumber!=null&&EF.Functions.ILike(p.RegistrationNumber,pattern,"\\")))));
        }
        long total=await customers.LongCountAsync(ct);
        IQueryable<Customer> ordered=(query.SortBy.ToLowerInvariant(),query.SortDescending) switch
        {
            ("name",false)=>customers.OrderBy(x=>x.DisplayName).ThenBy(x=>x.Id),
            ("name",true)=>customers.OrderByDescending(x=>x.DisplayName).ThenBy(x=>x.Id),
            ("type",false)=>customers.OrderBy(x=>x.CustomerType).ThenBy(x=>x.CustomerCode),
            ("type",true)=>customers.OrderByDescending(x=>x.CustomerType).ThenBy(x=>x.CustomerCode),
            ("status",false)=>customers.OrderBy(x=>x.Status).ThenBy(x=>x.CustomerCode),
            ("status",true)=>customers.OrderByDescending(x=>x.Status).ThenBy(x=>x.CustomerCode),
            ("mobile",false)=>customers.OrderBy(x=>x.PrimaryMobile).ThenBy(x=>x.CustomerCode),
            ("mobile",true)=>customers.OrderByDescending(x=>x.PrimaryMobile).ThenBy(x=>x.CustomerCode),
            (_,true)=>customers.OrderByDescending(x=>x.CustomerCode).ThenBy(x=>x.Id),
            _=>customers.OrderBy(x=>x.CustomerCode).ThenBy(x=>x.Id)
        };
        int skip=(page.PageNumber-1)*page.PageSize;
        var rows=await(from customer in ordered.Skip(skip).Take(page.PageSize)
                       join person in db.Persons.AsNoTracking() on customer.SalesRepresentativePersonId equals (Guid?)person.Id into people
                       from person in people.DefaultIfEmpty()
                       select new{Customer=customer,SalesRepresentativeName=person==null?null:person.DisplayName}).ToListAsync(ct);
        return new(rows.Select(x=>new CustomerDirectoryRow(x.Customer,x.SalesRepresentativeName)).ToArray(),page.PageNumber,page.PageSize,total);
    }

    public async Task<IReadOnlyList<Customer>>OptionsAsync(string? search,bool activeOnly,int limit,CancellationToken ct)
    {
        IQueryable<Customer>q=db.Customers.AsNoTracking();if(activeOnly)q=q.Where(x=>x.Status==CustomerStatus.Active);string value=search?.Trim()??"";if(value.Length>0){string pattern=$"%{value.Replace("%","\\%").Replace("_","\\_")}%";q=q.Where(x=>EF.Functions.ILike(x.CustomerCode,pattern,"\\")||EF.Functions.ILike(x.DisplayName,pattern,"\\")||(x.PrimaryMobile!=null&&EF.Functions.ILike(x.PrimaryMobile,pattern,"\\")));}return await q.OrderBy(x=>x.DisplayName).ThenBy(x=>x.CustomerCode).ThenBy(x=>x.Id).Take(limit).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<SalesRepresentativeOption>>SalesRepresentativesAsync(string? search,int limit,CancellationToken ct){var q=db.Persons.AsNoTracking().Where(x=>x.IsActive);string value=search?.Trim()??"";if(value.Length>0){string p=$"%{value.Replace("%","\\%").Replace("_","\\_")}%";q=q.Where(x=>EF.Functions.ILike(x.DisplayName,p,"\\")||EF.Functions.ILike(x.PersonnelCode,p,"\\")||(x.Phone!=null&&EF.Functions.ILike(x.Phone,p,"\\")));}return await q.OrderBy(x=>x.DisplayName).ThenBy(x=>x.PersonnelCode).Take(limit).Select(x=>new SalesRepresentativeOption(x.Id,x.DisplayName,x.PersonnelCode,x.Phone)).ToListAsync(ct);}
    public Task<string?>SalesRepresentativeNameAsync(Guid? personId,CancellationToken ct)=>personId is null?Task.FromResult<string?>(null):db.Persons.AsNoTracking().Where(x=>x.Id==personId).Select(x=>(string?)x.DisplayName).SingleOrDefaultAsync(ct);
    public Task<bool>SalesRepresentativeExistsAsync(Guid personId,CancellationToken ct)=>db.Persons.AnyAsync(x=>x.Id==personId&&x.IsActive,ct);

    public Task<CustomerContact?>FindContactAsync(Guid customerId,Guid id,CancellationToken ct)=>db.CustomerContacts.SingleOrDefaultAsync(x=>x.CustomerId==customerId&&x.Id==id,ct);
    public async Task<IReadOnlyList<CustomerContact>>ListContactsAsync(Guid customerId,bool activeOnly,CancellationToken ct)=>await db.CustomerContacts.AsNoTracking().Where(x=>x.CustomerId==customerId&&(!activeOnly||x.IsActive)).OrderByDescending(x=>x.IsPrimary).ThenBy(x=>x.DisplayOrder).ThenBy(x=>x.Id).ToListAsync(ct);
    public async Task<IReadOnlyList<CustomerContact>>ListPrimaryContactsAsync(Guid customerId,Guid? excludingId,CancellationToken ct)=>await db.CustomerContacts.Where(x=>x.CustomerId==customerId&&x.IsActive&&x.IsPrimary&&(excludingId==null||x.Id!=excludingId)).ToListAsync(ct);
    public Task<CustomerAddress?>FindAddressAsync(Guid customerId,Guid id,CancellationToken ct)=>db.CustomerAddresses.SingleOrDefaultAsync(x=>x.CustomerId==customerId&&x.Id==id,ct);
    public async Task<IReadOnlyList<CustomerAddress>>ListAddressesAsync(Guid customerId,bool activeOnly,CancellationToken ct)=>await db.CustomerAddresses.AsNoTracking().Where(x=>x.CustomerId==customerId&&(!activeOnly||x.IsActive)).OrderByDescending(x=>x.IsDefault).ThenBy(x=>x.DisplayOrder).ThenBy(x=>x.Id).ToListAsync(ct);
    public async Task<IReadOnlyList<CustomerAddress>>ListDefaultAddressesAsync(Guid customerId,Guid? excludingId,CancellationToken ct)=>await db.CustomerAddresses.Where(x=>x.CustomerId==customerId&&x.IsActive&&x.IsDefault&&(excludingId==null||x.Id!=excludingId)).ToListAsync(ct);
    public Task<CustomerFinancialProfile?>FindFinancialAsync(Guid customerId,bool tracking,CancellationToken ct)=>(tracking?db.CustomerFinancialProfiles:db.CustomerFinancialProfiles.AsNoTracking()).SingleOrDefaultAsync(x=>x.CustomerId==customerId,ct);
    public Task<CustomerBankAccount?>FindBankAccountAsync(Guid customerId,Guid id,CancellationToken ct)=>db.CustomerBankAccounts.SingleOrDefaultAsync(x=>x.CustomerId==customerId&&x.Id==id,ct);
    public async Task<IReadOnlyList<CustomerBankAccount>>ListBankAccountsAsync(Guid customerId,bool activeOnly,CancellationToken ct)=>await db.CustomerBankAccounts.AsNoTracking().Where(x=>x.CustomerId==customerId&&(!activeOnly||x.IsActive)).OrderByDescending(x=>x.IsDefault).ThenBy(x=>x.BankName).ThenBy(x=>x.Id).ToListAsync(ct);
    public async Task<IReadOnlyList<CustomerBankAccount>>ListDefaultBankAccountsAsync(Guid customerId,Guid? excludingId,CancellationToken ct)=>await db.CustomerBankAccounts.Where(x=>x.CustomerId==customerId&&x.IsActive&&x.IsDefault&&(excludingId==null||x.Id!=excludingId)).ToListAsync(ct);
    public Task<bool>BankAccountExistsAsync(Guid customerId,string? accountNumber,string? iban,Guid? excludingId,CancellationToken ct)=>db.CustomerBankAccounts.AnyAsync(x=>x.CustomerId==customerId&&(excludingId==null||x.Id!=excludingId)&&((accountNumber!=null&&x.AccountNumber==accountNumber)||(iban!=null&&x.Iban==iban)),ct);
    public Task<bool>LookupValueExistsAsync(string lookupTypeCode,string valueCode,CancellationToken ct)=>db.LookupValues.AnyAsync(value=>value.Code==valueCode&&value.IsActive&&db.LookupTypes.Any(type=>type.Id==value.LookupTypeId&&type.Code==lookupTypeCode&&type.IsActive),ct);
    public Task<bool>CityBelongsToProvinceAsync(string provinceCode,string cityCode,CancellationToken ct)=>LookupValueExistsAsync($"geography.city.{provinceCode}",cityCode,ct);
    public async Task<bool>HasProtectedDependenciesAsync(Guid customerId,CancellationToken ct)=>
        await db.Customers.AnyAsync(x=>x.Id==customerId&&x.Version>1,ct)||
        await db.Orders.AnyAsync(x=>x.CustomerId==customerId,ct)||
        await db.CustomerAddresses.AnyAsync(x=>x.CustomerId==customerId,ct)||
        await db.CustomerContacts.AnyAsync(x=>x.CustomerId==customerId,ct)||
        await db.CustomerFinancialProfiles.AnyAsync(x=>x.CustomerId==customerId,ct)||
        await db.CustomerBankAccounts.AnyAsync(x=>x.CustomerId==customerId,ct)||
        await db.DiscountRules.AnyAsync(x=>x.CustomerId==customerId,ct)||
        await db.AuditEvents.IgnoreQueryFilters().AnyAsync(x=>x.EntityType=="customer"&&x.EntityId==customerId.ToString()&&x.Action!="customer.created",ct);
    public async Task RemoveAggregateAsync(Customer customer,CancellationToken ct)
    {
        await db.IndividualCustomerProfiles.Where(x=>x.CustomerId==customer.Id).ExecuteDeleteAsync(ct);
        await db.LegalCustomerProfiles.Where(x=>x.CustomerId==customer.Id).ExecuteDeleteAsync(ct);
        db.Customers.Remove(customer);
    }
}
