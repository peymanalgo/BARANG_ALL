using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class SupplierSourcingAdministration(CoreDbContext db,ITransactionExecutor transactions,ICompanyContext company,
    ICatalogStore catalog,IAuditWriter audit,TimeProvider time):ISupplierSourcingAdministration
{
    private DateTimeOffset Now=>time.GetUtcNow();
    private DateOnly Today=>DateOnly.FromDateTime(Now.UtcDateTime);
    private async Task<Result<T>> Run<T>(bool write,Func<CancellationToken,Task<Result<T>>> action,CancellationToken ct)
    {
        Guid scope=company.RequireCompanyId();string lockKey="supplier-sourcing:"+scope.ToString();
        try{return await transactions.ExecuteAsync(async token=>{
            if(write)await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))",token);
            return await action(token);
        },ct);}
        catch(DbUpdateException ex) when(ex.InnerException is PostgresException{SqlState:PostgresErrorCodes.ExclusionViolation})
        {return Result.Failure<T>(SupplierSourcingErrors.Overlap);}
        catch(DbUpdateException ex) when(ex.InnerException is PostgresException{SqlState:PostgresErrorCodes.ForeignKeyViolation})
        {return Result.Failure<T>(SupplierSourcingErrors.InvalidReference);}
        catch(DbUpdateException ex) when(ex.InnerException is PostgresException{SqlState:PostgresErrorCodes.CheckViolation})
        {return Result.Failure<T>(SupplierSourcingErrors.InvalidData);}
    }
    private static Result<T> Fail<T>(Error error)=>Result.Failure<T>(error);
    private Task<Supplier?> Owner(Guid id,CancellationToken ct)=>db.Suppliers.SingleOrDefaultAsync(x=>x.Id==id,ct);
    private Task<SupplierProduct?> Link(Guid supplier,Guid id,CancellationToken ct)=>db.SupplierProducts.SingleOrDefaultAsync(x=>x.SupplierId==supplier&&x.Id==id,ct);
    private Task<SupplierPriceList?> Header(Guid supplier,Guid id,CancellationToken ct)=>db.Set<SupplierPriceList>().SingleOrDefaultAsync(x=>x.SupplierId==supplier&&x.Id==id,ct);
    private void Log(string action,string entity,Guid id,AuditChangeKind kind=AuditChangeKind.Modified)=>
        audit.Write(new(action,entity,id.ToString(),CompanyId:company.RequireCompanyId(),ChangeKind:kind));
    private Task<bool> Protected(string table,Guid id,CancellationToken ct)=>
        db.Database.SqlQuery<bool>($"SELECT barang.supplier_sourcing_has_references({table},{id}) AS \"Value\"").SingleAsync(ct);
    private async Task<bool> Reference(string type,Guid? id,Guid? previous,CancellationToken ct)
    {
        if(id is null)return true;
        var value=await(from v in db.LookupValues join t in db.LookupTypes on v.LookupTypeId equals t.Id
            where v.Id==id&&t.Code==type&&v.CompanyId==company.CompanyId select new{Active=v.IsActive&&t.IsActive}).SingleOrDefaultAsync(ct);
        return value is not null&&(value.Active||id==previous);
    }
    private async Task<bool> Usable(SupplierProduct link,CancellationToken ct)
    {
        if(!link.IsActive||link.Availability=="Unavailable"||link.PurchaseUnitId is null)return false;
        var supplier=await Owner(link.SupplierId,ct);
        return supplier is{IsActive:true,IsBlocked:false}&&await db.Products.AnyAsync(x=>x.Id==link.ProductId&&x.IsActive,ct)&&
            (link.ProductVariantId is null||await db.ProductVariants.AnyAsync(x=>x.Id==link.ProductVariantId&&x.ProductId==link.ProductId&&x.IsActive,ct))&&
            await Reference("unit.of.measure",link.PurchaseUnitId,null,ct);
    }
    private async Task<bool> TermsReferences(SupplierProduct link,SupplierPriceTerms terms,SupplierPriceTerms? previous,CancellationToken ct)=>
        terms.PurchaseUnitId==link.PurchaseUnitId&&await Reference("unit.of.measure",terms.PurchaseUnitId,previous?.PurchaseUnitId,ct)&&
        await Reference("tax.status",terms.TaxStatusId,previous?.TaxStatusId,ct);
    private async Task ReplacePreferred(SupplierProduct link,CancellationToken ct)
    {
        foreach(var cleared in await catalog.ReplacePreferredSupplierProductAsync(link,Now,ct))
            Log("supplier.sourcing.preferred.cleared","supplier_product",cleared.Id);
    }
    private sealed record Description(string Name,string Code,string? Sku,string? SupplierItemCode);
    private async Task<Description> Describe(SupplierProduct link,CancellationToken ct)
    {
        var product=await db.Products.AsNoTracking().SingleAsync(x=>x.Id==link.ProductId,ct);
        string? sku=link.ProductVariantId is Guid variant?await db.ProductVariants.Where(x=>x.Id==variant).Select(x=>x.Sku).SingleAsync(ct):null;
        return new(product.Name,product.ProductCode,sku,link.SupplierItemCode);
    }
    private async Task<SupplierPriceView> View(SupplierProductPrice price,CancellationToken ct)
    {
        var link=(await Link(price.SupplierId,price.SupplierProductId,ct))!;var info=await Describe(link,ct);
        bool current=price.IsActive&&price.PublishedAtUtc is not null&&SupplierSourcingRules.Applies(price.EffectiveFrom,price.EffectiveTo,Today)&&
            price.PurchaseUnitId==link.PurchaseUnitId&&await Usable(link,ct);
        return new(price,info.Name,info.Code,info.Sku,SupplierSourcingRules.NetUnitCost(price.Terms),current);
    }
    private async Task<IReadOnlyList<SupplierPriceView>> Current(SupplierProduct link,DateOnly date,decimal quantity,string? currency,Guid? unit,CancellationToken ct)
    {
        if(!await Usable(link,ct)||quantity<(link.MinimumOrderQuantity??0))return [];
        var prices=await db.Set<SupplierProductPrice>().Where(x=>x.SupplierProductId==link.Id&&x.IsActive&&x.PublishedAtUtc!=null&&
            x.EffectiveFrom<=date&&(x.EffectiveTo==null||x.EffectiveTo>=date)&&x.MinimumQuantity<=quantity&&x.PurchaseUnitId==link.PurchaseUnitId&&
            (currency==null||x.CurrencyCode==currency)&&(unit==null||x.PurchaseUnitId==unit)&&
            (x.PriceListId==null||db.Set<SupplierPriceList>().Any(p=>p.Id==x.PriceListId&&p.IsActive)))
            .OrderByDescending(x=>x.MinimumQuantity).ThenBy(x=>x.Id).ToListAsync(ct);
        var info=await Describe(link,ct);
        return prices.GroupBy(x=>new{x.CurrencyCode,x.PurchaseUnitId}).Select(g=>g.First())
            .OrderBy(x=>x.CurrencyCode,StringComparer.Ordinal).ThenBy(x=>x.PurchaseUnitId)
            .Select(x=>new SupplierPriceView(x,info.Name,info.Code,info.Sku,SupplierSourcingRules.NetUnitCost(x.Terms),true)).ToArray();
    }
    private async Task<SupplierSourcingView> View(SupplierProduct link,CancellationToken ct)
    {
        var info=await Describe(link,ct);return new(link,info.Name,info.Code,info.Sku,await Current(link,Today,Math.Max(1,link.MinimumOrderQuantity??0),null,null,ct));
    }
    private async Task<SupplierPriceListView> View(SupplierPriceList list,CancellationToken ct)=>
        new(list,await db.Set<SupplierPriceListLine>().CountAsync(x=>x.PriceListId==list.Id,ct));
    private async Task<SupplierPriceListLineView> View(SupplierPriceListLine line,CancellationToken ct)
    {
        var info=await Describe((await Link(line.SupplierId,line.SupplierProductId,ct))!,ct);
        return new(line,info.Name,info.Code,info.Sku,info.SupplierItemCode,SupplierSourcingRules.NetUnitCost(line.Terms));
    }
    private static bool Valid(SourcingQuery q)=>q.PageNumber is>=1 and<=1000000&&q.PageSize is>=1 and<=100&&(q.Search?.Length??0)<=200;
    private static string Pattern(string? search)=>"%"+(search?.Trim()??"").Replace("\\","\\\\",StringComparison.Ordinal).Replace("%","\\%",StringComparison.Ordinal).Replace("_","\\_",StringComparison.Ordinal)+"%";
    private IQueryable<SupplierProduct> Links(Guid supplier,string? search)
    {
        var query=db.SupplierProducts.Where(x=>x.SupplierId==supplier);
        if(string.IsNullOrWhiteSpace(search))return query;
        string pattern=Pattern(search);
        return query.Where(x=>x.SupplierItemCode!=null&&EF.Functions.ILike(x.SupplierItemCode,pattern,"\\")||
            x.SupplierItemName!=null&&EF.Functions.ILike(x.SupplierItemName,pattern,"\\")||
            db.Products.Any(p=>p.Id==x.ProductId&&(EF.Functions.ILike(p.Name,pattern,"\\")||EF.Functions.ILike(p.ProductCode,pattern,"\\")))||
            db.ProductVariants.Any(v=>v.Id==x.ProductVariantId&&EF.Functions.ILike(v.Sku,pattern,"\\")));
    }
    private static async Task<PagedResult<TView>> Page<T,TView>(IOrderedQueryable<T> rows,SourcingQuery q,Func<T,CancellationToken,Task<TView>> view,CancellationToken ct) where T:CatalogEntity
    {
        long count=await rows.LongCountAsync(ct);var data=await rows.ThenBy(x=>x.Id).Skip((q.PageNumber-1)*q.PageSize).Take(q.PageSize).ToListAsync(ct);
        List<TView> result=[];foreach(var row in data)result.Add(await view(row,ct));
        return new(result,q.PageNumber,q.PageSize,count);
    }
    public Task<Result<PagedResult<SupplierSourcingView>>> ProductsAsync(Guid supplier,SourcingQuery query,CancellationToken ct)=>Run(false,async t=>{
        if(await Owner(supplier,t) is null)return Fail<PagedResult<SupplierSourcingView>>(SupplierSourcingErrors.NotFound);
        if(!Valid(query))return Fail<PagedResult<SupplierSourcingView>>(SupplierSourcingErrors.InvalidData);
        var rows=Links(supplier,query.Search).Where(x=>!query.ActiveOnly||x.IsActive);
        if(query.SupplierProductId is Guid id)rows=rows.Where(x=>x.Id==id);
        IOrderedQueryable<SupplierProduct> ordered=(query.SortBy,query.SortDescending) switch{
            ("moq",false)=>rows.OrderBy(x=>x.MinimumOrderQuantity),("moq",true)=>rows.OrderByDescending(x=>x.MinimumOrderQuantity),
            ("lead",false)=>rows.OrderBy(x=>x.LeadTimeDays),("lead",true)=>rows.OrderByDescending(x=>x.LeadTimeDays),
            ("code",false)=>rows.OrderBy(x=>x.SupplierItemCode),("code",true)=>rows.OrderByDescending(x=>x.SupplierItemCode),
            (_,true)=>rows.OrderByDescending(x=>db.Products.Where(p=>p.Id==x.ProductId).Select(p=>p.Name).First()),
            _=>rows.OrderBy(x=>db.Products.Where(p=>p.Id==x.ProductId).Select(p=>p.Name).First())};
        return Result.Success(await Page(ordered,query,View,t));
    },ct);
    public Task<Result<SupplierSourcingView>> ProductAsync(Guid supplier,Guid id,CancellationToken ct)=>Run(false,async t=>{
        var row=await Link(supplier,id,t);return row is null?Fail<SupplierSourcingView>(SupplierSourcingErrors.NotFound):Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierSourcingView>> SaveProductAsync(Guid supplier,Guid? id,SourcingCommand<SupplierSourcingData> command,CancellationToken ct)=>Run(true,async t=>{
        var owner=await Owner(supplier,t);if(owner is null)return Fail<SupplierSourcingView>(SupplierSourcingErrors.NotFound);
        var previous=id is Guid key?await Link(supplier,key,t):null;
        if(id is not null&&previous is null)return Fail<SupplierSourcingView>(SupplierSourcingErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Fail<SupplierSourcingView>(SupplierSourcingErrors.Concurrency);
        var data=command.Data;if(!SupplierSourcingRules.Sourcing(data)||data.PurchaseUnitId is null)return Fail<SupplierSourcingView>(SupplierSourcingErrors.InvalidData);
        if(previous is not null&&(data.ProductId!=previous.ProductId||data.ProductVariantId!=previous.ProductVariantId))return Fail<SupplierSourcingView>(SupplierSourcingErrors.InvalidData);
        var product=await db.Products.SingleOrDefaultAsync(x=>x.Id==data.ProductId,t);
        var variant=data.ProductVariantId is Guid variantId?await db.ProductVariants.SingleOrDefaultAsync(x=>x.Id==variantId&&x.ProductId==data.ProductId,t):null;
        if(product is null||data.ProductVariantId is not null&&variant is null)return Fail<SupplierSourcingView>(SupplierSourcingErrors.InvalidReference);
        if(previous is null&&(!owner.IsActive||owner.IsBlocked||!product.IsActive||variant is{IsActive:false}))return Fail<SupplierSourcingView>(SupplierSourcingErrors.Ineligible);
        if(!await Reference("unit.of.measure",data.PurchaseUnitId,previous?.PurchaseUnitId,t)||
           !await Reference("packaging.type",data.PackagingTypeId,previous?.PackagingTypeId,t))return Fail<SupplierSourcingView>(SupplierSourcingErrors.InvalidReference);
        if(data.IsPreferred&&!data.IsActive&&previous is not{IsPreferred:true,IsActive:false})return Fail<SupplierSourcingView>(SupplierSourcingErrors.Ineligible);
        if(previous is null&&await db.SupplierProducts.AnyAsync(x=>x.SupplierId==supplier&&x.ProductId==data.ProductId&&x.ProductVariantId==data.ProductVariantId,t))
            return Fail<SupplierSourcingView>(CatalogErrors.DuplicateRelationship);
        SupplierProduct row;
        if(previous is null){var created=SupplierProduct.CreateSourcing(Guid.NewGuid(),company.RequireCompanyId(),supplier,data,Now);if(created.IsFailure)return Fail<SupplierSourcingView>(created.Error);row=created.Value;await ReplacePreferred(row,t);db.Add(row);}
        else{var changed=previous.UpdateSourcing(data,Now);if(changed.IsFailure)return Fail<SupplierSourcingView>(changed.Error);row=previous;await ReplacePreferred(row,t);}
        Log("supplier.sourcing.saved","supplier_product",row.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);
        await db.SaveChangesAsync(t);return Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierSourcingView>> ProductStateAsync(Guid supplier,Guid id,long version,bool? active,bool? preferred,CancellationToken ct)=>Run(true,async t=>{
        var row=await Link(supplier,id,t);if(row is null)return Fail<SupplierSourcingView>(SupplierSourcingErrors.NotFound);
        if(row.Version!=version)return Fail<SupplierSourcingView>(SupplierSourcingErrors.Concurrency);
        if(preferred is true&&!row.IsActive)return Fail<SupplierSourcingView>(SupplierSourcingErrors.Ineligible);
        if(active is true){var owner=await Owner(supplier,t);if(owner is not{IsActive:true,IsBlocked:false}||!await db.Products.AnyAsync(x=>x.Id==row.ProductId&&x.IsActive,t)||
            row.ProductVariantId is not null&&!await db.ProductVariants.AnyAsync(x=>x.Id==row.ProductVariantId&&x.IsActive,t))return Fail<SupplierSourcingView>(SupplierSourcingErrors.Ineligible);}
        if(active is bool enabled)row.SetActive(enabled,Now);if(preferred is bool chosen)row.SelectPreferred(chosen,Now);
        await ReplacePreferred(row,t);Log("supplier.sourcing.state.changed","supplier_product",id);await db.SaveChangesAsync(t);return Result.Success(await View(row,t));
    },ct);
    public Task<Result<bool>> DeleteProductAsync(Guid supplier,Guid id,long version,CancellationToken ct)=>Run(true,async t=>{
        var row=await Link(supplier,id,t);if(row is null)return Fail<bool>(SupplierSourcingErrors.NotFound);
        if(row.Version!=version)return Fail<bool>(SupplierSourcingErrors.Concurrency);
        if(await Protected("supplier_products",id,t))return Fail<bool>(SupplierSourcingErrors.Referenced);
        db.Remove(row);Log("supplier.sourcing.deleted","supplier_product",id,AuditChangeKind.Archived);return Result.Success(true);
    },ct);
    private Task<bool> Overlap(SupplierProductPrice price,CancellationToken ct)=>db.Set<SupplierProductPrice>().AnyAsync(x=>
        x.Id!=price.Id&&x.SupplierProductId==price.SupplierProductId&&x.PurchaseUnitId==price.PurchaseUnitId&&x.CurrencyCode==price.CurrencyCode&&
        x.MinimumQuantity==price.MinimumQuantity&&x.IsActive&&x.PublishedAtUtc!=null&&
        (price.EffectiveTo==null||x.EffectiveFrom<=price.EffectiveTo)&&(x.EffectiveTo==null||price.EffectiveFrom<=x.EffectiveTo),ct);
    public Task<Result<PagedResult<SupplierPriceView>>> PricesAsync(Guid supplier,SourcingQuery query,CancellationToken ct)=>Run(false,async t=>{
        if(await Owner(supplier,t) is null)return Fail<PagedResult<SupplierPriceView>>(SupplierSourcingErrors.NotFound);
        if(!Valid(query))return Fail<PagedResult<SupplierPriceView>>(SupplierSourcingErrors.InvalidData);
        var links=Links(supplier,query.Search).Select(x=>x.Id);
        var rows=db.Set<SupplierProductPrice>().Where(x=>x.SupplierId==supplier&&links.Contains(x.SupplierProductId)&&
            (query.SupplierProductId==null||x.SupplierProductId==query.SupplierProductId)&&(!query.ActiveOnly||x.IsActive));
        IOrderedQueryable<SupplierProductPrice> ordered=query.SortBy=="name"
            ?query.SortDescending?rows.OrderByDescending(x=>db.SupplierProducts.Where(l=>l.Id==x.SupplierProductId).Select(l=>db.Products.Where(p=>p.Id==l.ProductId).Select(p=>p.Name).First()).First())
                :rows.OrderBy(x=>db.SupplierProducts.Where(l=>l.Id==x.SupplierProductId).Select(l=>db.Products.Where(p=>p.Id==l.ProductId).Select(p=>p.Name).First()).First())
            :query.SortBy=="price"
            ?query.SortDescending?rows.OrderByDescending(x=>x.UnitPrice):rows.OrderBy(x=>x.UnitPrice)
            :query.SortDescending?rows.OrderByDescending(x=>x.EffectiveFrom):rows.OrderBy(x=>x.EffectiveFrom);
        return Result.Success(await Page(ordered,query,View,t));
    },ct);
    public Task<Result<SupplierPriceView>> PriceAsync(Guid supplier,Guid id,CancellationToken ct)=>Run(false,async t=>{
        var row=await db.Set<SupplierProductPrice>().SingleOrDefaultAsync(x=>x.SupplierId==supplier&&x.Id==id,t);
        return row is null?Fail<SupplierPriceView>(SupplierSourcingErrors.NotFound):Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierPriceView>> SavePriceAsync(Guid supplier,Guid? id,SourcingCommand<SupplierPriceData> command,CancellationToken ct)=>Run(true,async t=>{
        if(await Owner(supplier,t) is null)return Fail<SupplierPriceView>(SupplierSourcingErrors.NotFound);
        var previous=id is Guid key?await db.Set<SupplierProductPrice>().SingleOrDefaultAsync(x=>x.Id==key&&x.SupplierId==supplier,t):null;
        if(id is not null&&previous is null)return Fail<SupplierPriceView>(SupplierSourcingErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Fail<SupplierPriceView>(SupplierSourcingErrors.Concurrency);
        if(previous is{PublishedAtUtc:not null}||previous?.PriceListId is not null)return Fail<SupplierPriceView>(SupplierSourcingErrors.Published);
        var data=command.Data;
        if(data is null||!SupplierSourcingRules.Terms(data.Terms))return Fail<SupplierPriceView>(SupplierSourcingErrors.InvalidData);
        var link=await Link(supplier,data.SupplierProductId,t);if(link is null)return Fail<SupplierPriceView>(SupplierSourcingErrors.InvalidReference);
        if(previous is null&&!await Usable(link,t))return Fail<SupplierPriceView>(SupplierSourcingErrors.Ineligible);
        if(!await TermsReferences(link,data.Terms,previous?.Terms,t))return Fail<SupplierPriceView>(SupplierSourcingErrors.InvalidReference);
        SupplierProductPrice row;
        if(previous is null){var made=SupplierProductPrice.Create(company.RequireCompanyId(),supplier,data,Now);if(made.IsFailure)return Fail<SupplierPriceView>(made.Error);row=made.Value;db.Add(row);}
        else{var changed=previous.Update(data,Now);if(changed.IsFailure)return Fail<SupplierPriceView>(changed.Error);row=previous;}
        Log("supplier.price.saved","supplier_product_price",row.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);return Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierPriceView>> PriceStateAsync(Guid supplier,Guid id,long version,bool? active,DateOnly? endDate,CancellationToken ct)=>Run(true,async t=>{
        var row=await db.Set<SupplierProductPrice>().SingleOrDefaultAsync(x=>x.Id==id&&x.SupplierId==supplier,t);
        if(row is null)return Fail<SupplierPriceView>(SupplierSourcingErrors.NotFound);
        if(row.Version!=version)return Fail<SupplierPriceView>(SupplierSourcingErrors.Concurrency);
        if(row.PriceListId is not null)return Fail<SupplierPriceView>(SupplierSourcingErrors.Published);
        if(active is true){var link=(await Link(supplier,row.SupplierProductId,t))!;if(!await Usable(link,t))return Fail<SupplierPriceView>(SupplierSourcingErrors.Ineligible);
            if(!await TermsReferences(link,row.Terms,null,t))return Fail<SupplierPriceView>(SupplierSourcingErrors.InvalidReference);
            if(await Overlap(row,t))return Fail<SupplierPriceView>(SupplierSourcingErrors.Overlap);}
        if(endDate is DateOnly end){var result=row.End(end,Now);if(result.IsFailure)return Fail<SupplierPriceView>(result.Error);}
        else if(active is bool enabled)row.Publish(enabled,Now);else return Fail<SupplierPriceView>(SupplierSourcingErrors.InvalidData);
        Log("supplier.price.state.changed","supplier_product_price",id);return Result.Success(await View(row,t));
    },ct);
    public Task<Result<bool>> DeletePriceAsync(Guid supplier,Guid id,long version,CancellationToken ct)=>Run(true,async t=>{
        var row=await db.Set<SupplierProductPrice>().SingleOrDefaultAsync(x=>x.Id==id&&x.SupplierId==supplier,t);
        if(row is null)return Fail<bool>(SupplierSourcingErrors.NotFound);
        if(row.Version!=version)return Fail<bool>(SupplierSourcingErrors.Concurrency);
        if(row.PublishedAtUtc is not null||row.PriceListId is not null||await Protected("supplier_product_prices",id,t))return Fail<bool>(SupplierSourcingErrors.Referenced);
        db.Remove(row);Log("supplier.price.deleted","supplier_product_price",id,AuditChangeKind.Archived);return Result.Success(true);
    },ct);
    public Task<Result<PagedResult<SupplierPriceListView>>> ListsAsync(Guid supplier,SourcingQuery query,CancellationToken ct)=>Run(false,async t=>{
        if(await Owner(supplier,t) is null)return Fail<PagedResult<SupplierPriceListView>>(SupplierSourcingErrors.NotFound);
        if(!Valid(query))return Fail<PagedResult<SupplierPriceListView>>(SupplierSourcingErrors.InvalidData);
        var rows=db.Set<SupplierPriceList>().Where(x=>x.SupplierId==supplier&&(!query.ActiveOnly||x.IsActive));
        if(!string.IsNullOrWhiteSpace(query.Search)){string pattern=Pattern(query.Search);rows=rows.Where(x=>EF.Functions.ILike(x.Code,pattern,"\\")||EF.Functions.ILike(x.Title,pattern,"\\"));}
        IOrderedQueryable<SupplierPriceList> ordered=query.SortBy=="date"
            ?query.SortDescending?rows.OrderByDescending(x=>x.EffectiveFrom):rows.OrderBy(x=>x.EffectiveFrom)
            :query.SortDescending?rows.OrderByDescending(x=>x.Code):rows.OrderBy(x=>x.Code);
        return Result.Success(await Page(ordered,query,View,t));
    },ct);
    public Task<Result<SupplierPriceListView>> ListAsync(Guid supplier,Guid id,CancellationToken ct)=>Run(false,async t=>{
        var row=await Header(supplier,id,t);return row is null?Fail<SupplierPriceListView>(SupplierSourcingErrors.NotFound):Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierPriceListView>> SaveListAsync(Guid supplier,Guid? id,SourcingCommand<SupplierPriceListData> command,CancellationToken ct)=>Run(true,async t=>{
        var owner=await Owner(supplier,t);if(owner is null)return Fail<SupplierPriceListView>(SupplierSourcingErrors.NotFound);
        var previous=id is Guid key?await Header(supplier,key,t):null;
        if(id is not null&&previous is null)return Fail<SupplierPriceListView>(SupplierSourcingErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Fail<SupplierPriceListView>(SupplierSourcingErrors.Concurrency);
        if(previous is null&&owner is not{IsActive:true,IsBlocked:false})return Fail<SupplierPriceListView>(SupplierSourcingErrors.Ineligible);
        SupplierPriceList row;
        if(previous is null){var made=SupplierPriceList.Create(company.RequireCompanyId(),supplier,command.Data,Now);if(made.IsFailure)return Fail<SupplierPriceListView>(made.Error);row=made.Value;db.Add(row);}
        else{var changed=previous.Update(command.Data,Now);if(changed.IsFailure)return Fail<SupplierPriceListView>(changed.Error);row=previous;}
        Log("supplier.price_list.saved","supplier_price_list",row.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);return Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierPriceListView>> ListStateAsync(Guid supplier,Guid id,long version,bool active,CancellationToken ct)=>Run(true,async t=>{
        var header=await Header(supplier,id,t);if(header is null)return Fail<SupplierPriceListView>(SupplierSourcingErrors.NotFound);
        if(header.Version!=version)return Fail<SupplierPriceListView>(SupplierSourcingErrors.Concurrency);
        var lines=await db.Set<SupplierPriceListLine>().Where(x=>x.PriceListId==id).OrderBy(x=>x.Id).ToListAsync(t);
        if(active&&lines.Count==0)return Fail<SupplierPriceListView>(SupplierSourcingErrors.EmptyList);
        List<(SupplierPriceListLine Line,SupplierProductPrice Price)> pending=[];
        foreach(var line in lines){
            var link=(await Link(supplier,line.SupplierProductId,t))!;
            if(active&&(!await Usable(link,t)||!await TermsReferences(link,line.Terms,null,t)))return Fail<SupplierPriceListView>(SupplierSourcingErrors.Ineligible);
            SupplierProductPrice price;
            if(line.PublishedPriceId is Guid priceId)price=await db.Set<SupplierProductPrice>().SingleAsync(x=>x.Id==priceId,t);
            else{
                if(!active)continue;
                var made=SupplierProductPrice.Create(header.CompanyId,supplier,new(line.SupplierProductId,header.CurrencyCode,header.EffectiveFrom,header.EffectiveTo,line.Terms),Now,id);
                if(made.IsFailure)return Fail<SupplierPriceListView>(made.Error);price=made.Value;
            }
            if(active&&await Overlap(price,t))return Fail<SupplierPriceListView>(SupplierSourcingErrors.Overlap);
            if(active&&pending.Any(x=>x.Price.SupplierProductId==price.SupplierProductId&&x.Price.PurchaseUnitId==price.PurchaseUnitId&&
                x.Price.CurrencyCode==price.CurrencyCode&&x.Price.MinimumQuantity==price.MinimumQuantity&&
                SupplierSourcingRules.Overlaps(x.Price.EffectiveFrom,x.Price.EffectiveTo,price.EffectiveFrom,price.EffectiveTo)))
                return Fail<SupplierPriceListView>(SupplierSourcingErrors.Overlap);
            pending.Add((line,price));
        }
        foreach(var (line,price) in pending){
            if(line.PublishedPriceId is null){db.Add(price);line.BindPublishedPrice(price.Id,Now);}
            price.Publish(active,Now);Log("supplier.price.state.changed","supplier_product_price",price.Id);
        }
        header.Publish(active,Now);Log("supplier.price_list.state.changed","supplier_price_list",id);
        await db.SaveChangesAsync(t);return Result.Success(await View(header,t));
    },ct);
    public Task<Result<bool>> DeleteListAsync(Guid supplier,Guid id,long version,CancellationToken ct)=>Run(true,async t=>{
        var header=await Header(supplier,id,t);if(header is null)return Fail<bool>(SupplierSourcingErrors.NotFound);
        if(header.Version!=version)return Fail<bool>(SupplierSourcingErrors.Concurrency);
        if(header.PublishedAtUtc is not null||await Protected("supplier_price_lists",id,t))return Fail<bool>(SupplierSourcingErrors.Referenced);
        var lines=await db.Set<SupplierPriceListLine>().Where(x=>x.PriceListId==id).ToListAsync(t);
        foreach(var line in lines){if(await Protected("supplier_price_list_lines",line.Id,t))return Fail<bool>(SupplierSourcingErrors.Referenced);Log("supplier.price_list.line.deleted","supplier_price_list_line",line.Id,AuditChangeKind.Archived);}
        db.RemoveRange(lines);db.Remove(header);Log("supplier.price_list.deleted","supplier_price_list",id,AuditChangeKind.Archived);return Result.Success(true);
    },ct);
    public Task<Result<PagedResult<SupplierPriceListLineView>>> LinesAsync(Guid supplier,Guid list,SourcingQuery query,CancellationToken ct)=>Run(false,async t=>{
        if(await Header(supplier,list,t) is null)return Fail<PagedResult<SupplierPriceListLineView>>(SupplierSourcingErrors.NotFound);
        if(!Valid(query))return Fail<PagedResult<SupplierPriceListLineView>>(SupplierSourcingErrors.InvalidData);
        var links=Links(supplier,query.Search).Select(x=>x.Id);
        var rows=db.Set<SupplierPriceListLine>().Where(x=>x.PriceListId==list&&x.SupplierId==supplier&&links.Contains(x.SupplierProductId));
        IOrderedQueryable<SupplierPriceListLine> ordered=query.SortBy=="name"
            ?query.SortDescending?rows.OrderByDescending(x=>db.SupplierProducts.Where(l=>l.Id==x.SupplierProductId).Select(l=>db.Products.Where(p=>p.Id==l.ProductId).Select(p=>p.Name).First()).First())
                :rows.OrderBy(x=>db.SupplierProducts.Where(l=>l.Id==x.SupplierProductId).Select(l=>db.Products.Where(p=>p.Id==l.ProductId).Select(p=>p.Name).First()).First())
            :query.SortDescending?rows.OrderByDescending(x=>x.UnitPrice):rows.OrderBy(x=>x.UnitPrice);
        return Result.Success(await Page(ordered,query,View,t));
    },ct);
    public Task<Result<SupplierPriceListLineView>> LineAsync(Guid supplier,Guid list,Guid id,CancellationToken ct)=>Run(false,async t=>{
        var row=await db.Set<SupplierPriceListLine>().SingleOrDefaultAsync(x=>x.SupplierId==supplier&&x.PriceListId==list&&x.Id==id,t);
        return row is null?Fail<SupplierPriceListLineView>(SupplierSourcingErrors.NotFound):Result.Success(await View(row,t));
    },ct);
    public Task<Result<SupplierPriceListLineView>> SaveLineAsync(Guid supplier,Guid list,Guid? id,SourcingCommand<SupplierPriceListLineData> command,CancellationToken ct)=>Run(true,async t=>{
        var header=await Header(supplier,list,t);if(header is null)return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.NotFound);
        if(header.Version!=command.ParentVersion)return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.Concurrency);
        if(header.PublishedAtUtc is not null)return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.Published);
        var previous=id is Guid key?await db.Set<SupplierPriceListLine>().SingleOrDefaultAsync(x=>x.Id==key&&x.PriceListId==list&&x.SupplierId==supplier,t):null;
        if(id is not null&&previous is null)return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.NotFound);
        if(previous is not null&&previous.Version!=command.Version)return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.Concurrency);
        var data=command.Data;if(data is null||!SupplierSourcingRules.Terms(data.Terms))return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.InvalidData);
        var link=await Link(supplier,data.SupplierProductId,t);if(link is null)return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.InvalidReference);
        if(previous is null&&!await Usable(link,t))return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.Ineligible);
        if(!await TermsReferences(link,data.Terms,previous?.Terms,t))return Fail<SupplierPriceListLineView>(SupplierSourcingErrors.InvalidReference);
        SupplierPriceListLine row;
        if(previous is null){var made=SupplierPriceListLine.Create(header,data,Now);if(made.IsFailure)return Fail<SupplierPriceListLineView>(made.Error);row=made.Value;db.Add(row);}
        else{var changed=previous.Update(data,Now);if(changed.IsFailure)return Fail<SupplierPriceListLineView>(changed.Error);row=previous;}
        header.LinesChanged(Now);Log("supplier.price_list.line.saved","supplier_price_list_line",row.Id,id is null?AuditChangeKind.Added:AuditChangeKind.Modified);
        return Result.Success(await View(row,t));
    },ct);
    public Task<Result<bool>> DeleteLineAsync(Guid supplier,Guid list,Guid id,long version,long parentVersion,CancellationToken ct)=>Run(true,async t=>{
        var header=await Header(supplier,list,t);if(header is null)return Fail<bool>(SupplierSourcingErrors.NotFound);
        if(header.Version!=parentVersion)return Fail<bool>(SupplierSourcingErrors.Concurrency);
        if(header.PublishedAtUtc is not null)return Fail<bool>(SupplierSourcingErrors.Published);
        var row=await db.Set<SupplierPriceListLine>().SingleOrDefaultAsync(x=>x.Id==id&&x.PriceListId==list&&x.SupplierId==supplier,t);
        if(row is null)return Fail<bool>(SupplierSourcingErrors.NotFound);
        if(row.Version!=version)return Fail<bool>(SupplierSourcingErrors.Concurrency);
        if(await Protected("supplier_price_list_lines",id,t))return Fail<bool>(SupplierSourcingErrors.Referenced);
        db.Remove(row);header.LinesChanged(Now);Log("supplier.price_list.line.deleted","supplier_price_list_line",id,AuditChangeKind.Archived);return Result.Success(true);
    },ct);
    public Task<Result<IReadOnlyList<SupplierSourceQuote>>> SourcesAsync(PurchasingSourcingQuery query,CancellationToken ct)=>Run(false,async t=>{
        if(query.ProductId==Guid.Empty||!SupplierSourcingRules.Number(query.Quantity)||query.Quantity<=0||query.PurchaseUnitId==Guid.Empty||
           query.CurrencyCode is not null&&SupplierSourcingRules.Currency(query.CurrencyCode) is null)return Fail<IReadOnlyList<SupplierSourceQuote>>(SupplierSourcingErrors.InvalidData);
        if(!await db.Products.AnyAsync(x=>x.Id==query.ProductId,t)||query.ProductVariantId is Guid variant&&!await db.ProductVariants.AnyAsync(x=>x.Id==variant&&x.ProductId==query.ProductId,t))
            return Fail<IReadOnlyList<SupplierSourceQuote>>(SupplierSourcingErrors.NotFound);
        var links=await db.SupplierProducts.Where(x=>x.ProductId==query.ProductId&&x.ProductVariantId==query.ProductVariantId&&x.IsActive)
            .OrderByDescending(x=>x.IsPreferred).ThenBy(x=>x.SupplierId).ToListAsync(t);
        List<SupplierSourceQuote> rows=[];
        foreach(var link in links){
            if(!await Usable(link,t)||query.Quantity<(link.MinimumOrderQuantity??0)||query.PurchaseUnitId is Guid unit&&unit!=link.PurchaseUnitId)continue;
            var supplier=(await Owner(link.SupplierId,t))!;
            rows.Add(new(link.Id,supplier.Id,supplier.SupplierCode,supplier.DisplayName,supplier.SupplierNumber,link.SupplierItemCode,link.ProductId,link.ProductVariantId,
                link.IsPreferred,link.PurchaseUnitId,link.MinimumOrderQuantity,link.MinimumOrderAmount,link.LeadTimeDays,link.Availability,
                await Current(link,query.AsOf??Today,query.Quantity,query.CurrencyCode is null?null:SupplierSourcingRules.Currency(query.CurrencyCode),query.PurchaseUnitId,t)));
        }
        return Result.Success<IReadOnlyList<SupplierSourceQuote>>(rows.OrderByDescending(x=>x.IsPreferred).ThenBy(x=>x.SupplierNumber).ThenBy(x=>x.SupplierId).ToArray());
    },ct);
    public Task<Result<IReadOnlyList<SupplierSourceQuote>>> ProductSourcesAsync(Guid product,Guid? variant,CancellationToken ct)=>Run(false,async t=>{
        if(product==Guid.Empty||!await db.Products.AnyAsync(x=>x.Id==product,t)||variant is Guid v&&!await db.ProductVariants.AnyAsync(x=>x.Id==v&&x.ProductId==product,t))return Fail<IReadOnlyList<SupplierSourceQuote>>(SupplierSourcingErrors.NotFound);
        var links=await db.SupplierProducts.Where(x=>x.ProductId==product&&x.ProductVariantId==variant).OrderByDescending(x=>x.IsPreferred).ThenBy(x=>x.SupplierId).ToListAsync(t);
        List<SupplierSourceQuote> rows=[];
        foreach(var link in links){var supplier=await Owner(link.SupplierId,t);if(supplier is null)continue;rows.Add(new(link.Id,supplier.Id,supplier.SupplierCode,supplier.DisplayName,supplier.SupplierNumber,link.SupplierItemCode,link.ProductId,link.ProductVariantId,link.IsPreferred,link.PurchaseUnitId,link.MinimumOrderQuantity,link.MinimumOrderAmount,link.LeadTimeDays,link.Availability,await Current(link,Today,1,null,null,t)));}
        return Result.Success<IReadOnlyList<SupplierSourceQuote>>(rows.ToArray());
    },ct);
}
