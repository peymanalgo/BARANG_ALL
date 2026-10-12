using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Organization;
using BARANG.TestSupport;
using Npgsql;
using Xunit;
using static BARANG.TestSupport.SupplierReferenceScenario;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class SupplierSourcingApiTests(PostgreSqlDatabaseFixture database)
{
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow);
    private static JsonElement Row(JsonElement value,string name)=>value.GetProperty(name);
    private static string Root(Guid supplier)=>$"/api/v1/suppliers/{supplier}/sourcing";
    private static SupplierPriceTerms Terms(Guid unit,decimal amount=12.3456m,decimal quantity=2)=>new(unit,amount,quantity,10,null,10,2);
    private static SupplierPriceData Price(Guid link,Guid unit,decimal quantity=2)=>new(link,"USD",Today.AddDays(-1),Today.AddDays(30),Terms(unit,quantity:quantity));
    private static SourcingCommand<T> Command<T>(T data,JsonElement row=default,long parent=0)=>new(data,row.ValueKind==JsonValueKind.Object?Version(row):0,parent);

    [Theory][InlineData(false)][InlineData(true)]
    public async Task RelationshipCrudPersistsExactProductSkuReferencesAndPlanningHints(bool sku){
        await using var f=await Setup();Guid? variant=sku?f.Variant:null;
        var data=new SupplierSourcingData(f.Product,variant,f.Unit,f.Packaging,"SUP-CODE","Supplier item","Description","Brand",2.3456m,50.1256m,7,Notes:"Notes");
        string path=Root(f.Supplier)+"/products";
        var created=await f.Api.Send(HttpMethod.Post,path,Command(data),201);var row=Row(created,"relationship");Guid id=Id(row);
        Assert.Equal(f.Company,row.GetProperty("companyId").GetGuid());Assert.Equal(f.Product,row.GetProperty("productId").GetGuid());
        Assert.Equal(variant,row.GetProperty("productVariantId").ValueKind==JsonValueKind.Null?null:row.GetProperty("productVariantId").GetGuid());
        Assert.Equal(2.3456m,row.GetProperty("minimumOrderQuantity").GetDecimal());Assert.Equal(7,row.GetProperty("leadTimeDays").GetInt32());
        var updated=await f.Api.Send(HttpMethod.Put,path+"/"+id,Command(data with{Notes="Updated",LeadTimeDays=11},row));
        await f.Api.Send(HttpMethod.Put,path+"/"+id,Command(data,row),409);
        var loaded=await f.Api.Send(HttpMethod.Get,path+"/"+id);Assert.Equal("Updated",Row(loaded,"relationship").GetProperty("notes").GetString());
        foreach(bool active in new[]{false,true})updated=await f.Api.Send(HttpMethod.Post,path+"/"+id+"/activation",new{isActive=active,version=Version(Row(updated,"relationship"))});
        var search=await f.Api.Send(HttpMethod.Get,path+"?search=SUP-CODE&pageSize=1");Assert.Equal(id,Id(Row(Assert.Single(search.GetProperty("items").EnumerateArray()),"relationship")));
        await f.Api.Send(HttpMethod.Put,path+"/"+id,Command(data with{ProductId=Guid.NewGuid()},Row(updated,"relationship")),400);
        await f.Api.Send(HttpMethod.Delete,path+"/"+id+"?version="+Version(Row(updated,"relationship")));
        await f.Api.Send(HttpMethod.Get,path+"/"+id,expected:404);
    }
    [Fact]public async Task DraftPublishedPriceHistoryQuantityBreaksOverlapAndReadinessAreAuthoritative(){
        await using var f=await Setup();var link=await Link(f,f.Supplier);Guid id=Id(Row(link,"relationship"));
        string path=Root(f.Supplier)+"/prices";var data=Price(id,f.Unit);
        var draft=await f.Api.Send(HttpMethod.Post,path,Command(data),201);var price=Row(draft,"price");
        Assert.Equal("Draft",price.GetProperty("status").GetString());Assert.Equal(12.3456m,price.GetProperty("unitPrice").GetDecimal());
        draft=await f.Api.Send(HttpMethod.Put,path+"/"+Id(price),Command(data with{Terms=data.Terms with{UnitPrice=20.1234m}},price));
        var published=await f.Api.Send(HttpMethod.Post,path+"/"+Id(price)+"/activation",new{isActive=true,version=Version(Row(draft,"price"))});
        Assert.Equal("Active",Row(published,"price").GetProperty("status").GetString());
        await f.Api.Send(HttpMethod.Put,path+"/"+Id(price),Command(data,Row(published,"price")),422);
        await f.Api.Send(HttpMethod.Delete,path+"/"+Id(price)+"?version="+Version(Row(published,"price")),expected:422);
        var overlapping=await f.Api.Send(HttpMethod.Post,path,Command(data),201);
        Assert.Equal("supplier.sourcing.price_overlap",(await f.Api.Send(HttpMethod.Post,path+"/"+Id(Row(overlapping,"price"))+"/activation",new{isActive=true,version=Version(Row(overlapping,"price"))},409)).GetProperty("code").GetString());
        var tier=await f.Api.Send(HttpMethod.Post,path,Command(data with{Terms=Terms(f.Unit,9.8765m,10)}),201);
        tier=await f.Api.Send(HttpMethod.Post,path+"/"+Id(Row(tier,"price"))+"/activation",new{isActive=true,version=Version(Row(tier,"price"))});
        var source=Assert.Single((await f.Api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={f.Product}&productVariantId={f.Variant}&quantity=10")).EnumerateArray());
        Assert.Equal(f.Supplier,source.GetProperty("supplierId").GetGuid());Assert.Equal(2,source.GetProperty("minimumOrderQuantity").GetDecimal());
        Assert.Equal(7,source.GetProperty("leadTimeDays").GetInt32());Assert.Equal(f.Unit,source.GetProperty("purchaseUnitId").GetGuid());
        var quote=Assert.Single(source.GetProperty("prices").EnumerateArray());
        Assert.Equal(9.8765m,Row(quote,"price").GetProperty("unitPrice").GetDecimal());Assert.Equal("USD",Row(quote,"price").GetProperty("currencyCode").GetString());
        var historical=await f.Api.Send(HttpMethod.Post,path,Command(data with{EffectiveFrom=new(1985,6,8),EffectiveTo=new(1985,6,30)}),201);
        historical=await f.Api.Send(HttpMethod.Post,path+"/"+Id(Row(historical,"price"))+"/activation",new{isActive=true,version=Version(Row(historical,"price"))});
        Assert.False(historical.GetProperty("isCurrent").GetBoolean());
        Assert.Contains((await f.Api.Send(HttpMethod.Get,path)).GetProperty("items").EnumerateArray(),x=>Id(Row(x,"price"))==Id(Row(historical,"price")));
        var ended=await f.Api.Send(HttpMethod.Post,path+"/"+Id(Row(published,"price"))+"/end-date",new{effectiveTo=Today.AddDays(1),version=Version(Row(published,"price"))});
        Assert.Equal(20.1234m,Row(ended,"price").GetProperty("unitPrice").GetDecimal());
        await f.Api.Send(HttpMethod.Post,path,Command(data with{Terms=data.Terms with{UnitPrice=-1}}),400);
        await f.Api.Send(HttpMethod.Post,path,Command(data with{Terms=data.Terms with{UnitPrice=0.00001m}}),400);
        await f.Api.Send(HttpMethod.Post,Root(f.Supplier)+"/products/"+id+"/activation",new{isActive=false,version=Version(Row(link,"relationship"))});
        Assert.Empty((await f.Api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={f.Product}&productVariantId={f.Variant}&quantity=10")).EnumerateArray());
    }
    [Fact]public async Task PriceListDraftLinesPublishAtomicallyAndRetainImmutableHistory(){
        await using var f=await Setup();Guid link=Id(Row(await Link(f,f.Supplier),"relationship"));string root=Root(f.Supplier);
        var headerData=new SupplierPriceListData("LIST-1","Supplier list","USD",Today,Today.AddDays(15));
        var header=await f.Api.Send(HttpMethod.Post,root+"/price-lists",Command(headerData),201);Guid list=Id(Row(header,"priceList"));string path=root+"/price-lists/"+list;
        await f.Api.Send(HttpMethod.Post,path+"/activation",new{isActive=true,version=Version(Row(header,"priceList"))},422);
        var lineData=new SupplierPriceListLineData(link,Terms(f.Unit));
        var line=await f.Api.Send(HttpMethod.Post,path+"/lines",Command(lineData,parent:Version(Row(header,"priceList"))),201);
        header=await f.Api.Send(HttpMethod.Get,path);
        line=await f.Api.Send(HttpMethod.Put,path+"/lines/"+Id(Row(line,"line")),Command(lineData with{Terms=Terms(f.Unit,15)},Row(line,"line"),Version(Row(header,"priceList"))));
        var reload=await f.Api.Send(HttpMethod.Get,path+"/lines");Assert.Equal(15,Row(Assert.Single(reload.GetProperty("items").EnumerateArray()),"line").GetProperty("unitPrice").GetDecimal());
        header=await f.Api.Send(HttpMethod.Get,path);
        var conflict=await f.Api.Send(HttpMethod.Post,root+"/prices",Command(Price(link,f.Unit)),201);
        conflict=await f.Api.Send(HttpMethod.Post,root+"/prices/"+Id(Row(conflict,"price"))+"/activation",new{isActive=true,version=Version(Row(conflict,"price"))});
        await f.Api.Send(HttpMethod.Post,path+"/activation",new{isActive=true,version=Version(Row(header,"priceList"))},409);
        Assert.Equal("Draft",Row(await f.Api.Send(HttpMethod.Get,path),"priceList").GetProperty("status").GetString());
        Assert.DoesNotContain((await f.Api.Send(HttpMethod.Get,root+"/prices")).GetProperty("items").EnumerateArray(),x=>ValueOrNull(Row(x,"price"),"priceListId")==list);
        await f.Api.Send(HttpMethod.Post,root+"/prices/"+Id(Row(conflict,"price"))+"/activation",new{isActive=false,version=Version(Row(conflict,"price"))});
        header=await f.Api.Send(HttpMethod.Post,path+"/activation",new{isActive=true,version=Version(Row(header,"priceList"))});
        line=await f.Api.Send(HttpMethod.Get,path+"/lines/"+Id(Row(line,"line")));
        Assert.NotNull(ValueOrNull(Row(line,"line"),"publishedPriceId"));
        await f.Api.Send(HttpMethod.Put,path,Command(headerData with{Title="Denied"},Row(header,"priceList")),422);
        await f.Api.Send(HttpMethod.Put,path+"/lines/"+Id(Row(line,"line")),Command(lineData,Row(line,"line"),Version(Row(header,"priceList"))),422);
        await f.Api.Send(HttpMethod.Delete,path+"?version="+Version(Row(header,"priceList")),expected:422);
        foreach(bool active in new[]{false,true})header=await f.Api.Send(HttpMethod.Post,path+"/activation",new{isActive=active,version=Version(Row(header,"priceList"))});
        Assert.Equal("Active",Row(await f.Api.Send(HttpMethod.Get,path),"priceList").GetProperty("status").GetString());
        var source=Assert.Single((await f.Api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={f.Product}&productVariantId={f.Variant}&quantity=2")).EnumerateArray());
        Assert.Equal(15,Row(Assert.Single(source.GetProperty("prices").EnumerateArray()),"price").GetProperty("unitPrice").GetDecimal());
        var disposable=await f.Api.Send(HttpMethod.Post,root+"/price-lists",Command(headerData with{Code="DISPOSABLE"}),201);
        await f.Api.Send(HttpMethod.Delete,root+"/price-lists/"+Id(Row(disposable,"priceList"))+"?version="+Version(Row(disposable,"priceList")));
    }
    [Fact]public async Task PreferredReplacementIsAtomicAndLegacyApiCannotBypassIt(){
        await using var f=await Setup();Guid other=await Supplier(f.Api,"SECOND");
        var first=await Link(f,f.Supplier,true);var second=await Link(f,other);
        second=await f.Api.Send(HttpMethod.Post,Root(other)+"/products/"+Id(Row(second,"relationship"))+"/preferred",new{isPreferred=true,version=Version(Row(second,"relationship"))});
        first=await f.Api.Send(HttpMethod.Get,Root(f.Supplier)+"/products/"+Id(Row(first,"relationship")));
        Assert.False(Row(first,"relationship").GetProperty("isPreferred").GetBoolean());Assert.True(Row(second,"relationship").GetProperty("isPreferred").GetBoolean());
        await f.Api.Send(HttpMethod.Put,$"/api/v1/suppliers/{f.Supplier}/products/{Id(Row(first,"relationship"))}",new{productId=f.Product,productVariantId=f.Variant,isPreferred=true,minimumOrderQuantity=2,leadTimeDays=7,version=Version(Row(first,"relationship"))});
        var quotes=(await f.Api.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={f.Product}&productVariantId={f.Variant}&quantity=2")).EnumerateArray().ToArray();
        Assert.Equal(2,quotes.Length);Assert.Equal(f.Supplier,Assert.Single(quotes,x=>x.GetProperty("isPreferred").GetBoolean()).GetProperty("supplierId").GetGuid());
        Assert.False(Row(await f.Api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{f.Supplier}"),"supplier").GetProperty("isPreferred").GetBoolean());
    }
    [Fact]public async Task InactiveReferenceRemainsHistoricalButCannotBeNewlySelectedOrPublished(){
        await using var f=await Setup();var link=await Link(f,f.Supplier);Guid id=Id(Row(link,"relationship"));
        var unit=await f.Api.Value(f.Unit);await f.Api.Activate(unit,false);
        Assert.DoesNotContain((await f.Api.Options("unit.of.measure","en",true)).EnumerateArray(),x=>Id(x)==f.Unit);
        Assert.Contains((await f.Api.Options("unit.of.measure","fa",false)).EnumerateArray(),x=>Id(x)==f.Unit);
        var data=new SupplierSourcingData(f.Product,f.Variant,f.Unit,Notes:"Retained inactive reference");
        await f.Api.Send(HttpMethod.Put,Root(f.Supplier)+"/products/"+id,Command(data,Row(link,"relationship")));
        Guid other=await Supplier(f.Api,"NEW");
        await f.Api.Send(HttpMethod.Post,Root(other)+"/products",Command(data),422);
        await f.Api.Send(HttpMethod.Post,Root(f.Supplier)+"/prices",Command(Price(id,f.Unit)),422);
        await f.Api.Delete(await f.Api.Value(f.Unit),422);
    }
    [Fact]public async Task PostgreSqlEnforcesHistoryOverlapPreferenceReferenceScopeAndRls(){
        await using var f=await Setup();var first=await Link(f,f.Supplier,true);Guid other=await Supplier(f.Api,"OTHER");var second=await Link(f,other);
        await Reject(f.Company,$"UPDATE barang.supplier_products SET is_preferred=true WHERE id='{Id(Row(second,"relationship"))}'","23505");
        Guid link=Id(Row(first,"relationship"));string path=Root(f.Supplier)+"/prices";
        var draft=await f.Api.Send(HttpMethod.Post,path,Command(Price(link,f.Unit)),201);
        var published=await f.Api.Send(HttpMethod.Post,path+"/"+Id(Row(draft,"price"))+"/activation",new{isActive=true,version=Version(Row(draft,"price"))});
        await Reject(f.Company,$"UPDATE barang.supplier_product_prices SET unit_price=99 WHERE id='{Id(Row(published,"price"))}'","23514");
        await Reject(f.Company,$"DELETE FROM barang.supplier_product_prices WHERE id='{Id(Row(published,"price"))}'","23503");
        var overlap=await f.Api.Send(HttpMethod.Post,path,Command(Price(link,f.Unit)),201);
        await Reject(f.Company,$"UPDATE barang.supplier_product_prices SET is_active=true,published_at_utc=CURRENT_TIMESTAMP WHERE id='{Id(Row(overlap,"price"))}'","23P01");
        var foreignUnit=await f.Api.InCompany(f.Other).CreateValue("unit.of.measure","foreign-unit");
        await Reject(f.Company,$"UPDATE barang.supplier_products SET purchase_unit_id='{Id(foreignUnit)}' WHERE id='{link}'","23503");
        await using var connection=new NpgsqlConnection(database.ConnectionString);await connection.OpenAsync(Ct);
        await using var transaction=await connection.BeginTransactionAsync(Ct);
        await using(var scope=new NpgsqlCommand("SELECT set_config('barang.company_id',@company,true)",connection,transaction)){scope.Parameters.AddWithValue("company",f.Other.ToString());await scope.ExecuteNonQueryAsync(Ct);}
        foreach(string table in new[]{"supplier_products","supplier_product_prices","supplier_price_lists","supplier_price_list_lines"}){
            await using var command=new NpgsqlCommand($"SELECT count(*) FROM barang.{table} WHERE company_id=@owner",connection,transaction);command.Parameters.AddWithValue("owner",f.Company);
            Assert.Equal(0L,(long)(await command.ExecuteScalarAsync(Ct))!);
        }
    }
    [Fact]public async Task SecurityDeniesWritesAndCrossCompanyReadsWithoutInventingGrants(){
        await using var f=await Setup();var link=await Link(f,f.Supplier);Guid id=Id(Row(link,"relationship"));
        await f.Api.InCompany(f.Other).Send(HttpMethod.Get,Root(f.Supplier)+"/products/"+id,expected:404);
        var product=await f.Api.InCompany(f.Other).Send(HttpMethod.Post,"/api/v1/products",new{productCode="FOREIGN",name="Foreign",productTypeCode="RAW",unitOfMeasureCode="PCS"},201);
        await f.Api.Send(HttpMethod.Post,Root(f.Supplier)+"/products",Command(new SupplierSourcingData(Id(product),PurchaseUnitId:f.Unit)),422);
        Guid foreignSupplier=await Supplier(f.Api.InCompany(f.Other),"FOREIGN");
        await f.Api.Send(HttpMethod.Get,Root(foreignSupplier)+"/products",expected:404);
        await f.Api.InCompany(f.Other).Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={f.Product}",expected:404);
        var person=await f.Api.Send(HttpMethod.Post,"/api/v1/organization/persons",new{firstName="Sourcing",lastName="Reader",phone="+989120000079"},201);
        var user=await f.Api.Send(HttpMethod.Post,"/api/v1/security/users/linked",new{personId=Id(person),username="sourcing-reader",password="SourcingReader123!",enabled=true});
        var role=await f.Api.Send(HttpMethod.Post,"/api/v1/security/roles",new{code="sourcing.reader",displayTitle="Sourcing reader"});
        var permission=Assert.Single((await f.Api.Send(HttpMethod.Get,"/api/v1/security/permissions")).EnumerateArray(),x=>x.GetProperty("code").GetString()=="supplier.read");
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/roles/{Id(role)}/permissions/{Id(permission)}",expected:204);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/users/{Id(user)}/roles",new{roleId=Id(role),companyId=f.Company});
        var login=await SupplierReferenceScenario.Send(f.Client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username="sourcing-reader",password="SourcingReader123!"},200,Ct);
        var reader=new SupplierReferenceScenario(f.Client,login.GetProperty("accessToken").GetString()!,f.Company,Ct);
        await reader.Send(HttpMethod.Get,Root(f.Supplier)+"/products/"+id);
        await reader.Send(HttpMethod.Post,Root(f.Supplier)+"/products",Command(new SupplierSourcingData(f.Product,PurchaseUnitId:f.Unit)),403);
        await reader.Send(HttpMethod.Post,Root(f.Supplier)+"/products/"+id+"/preferred",new{isPreferred=true,version=Version(Row(link,"relationship"))},403);
        await reader.Send(HttpMethod.Delete,Root(f.Supplier)+"/products/"+id+"?version="+Version(Row(link,"relationship")),expected:403);
        await reader.Send(HttpMethod.Get,$"/api/v1/purchasing/sourcing-options?productId={f.Product}",expected:403);
        await reader.InCompany(f.Other).Send(HttpMethod.Get,Root(f.Supplier)+"/products",expected:403);
    }
    private static Guid?ValueOrNull(JsonElement row,string key)=>row.GetProperty(key).ValueKind==JsonValueKind.Null?null:row.GetProperty(key).GetGuid();
    private static Task<JsonElement> Link(Fixture f,Guid supplier,bool preferred=false)=>f.Api.Send(HttpMethod.Post,Root(supplier)+"/products",
        Command(new SupplierSourcingData(f.Product,f.Variant,f.Unit,f.Packaging,"ITEM","Sourcing item",MinimumOrderQuantity:2,LeadTimeDays:7,IsPreferred:preferred)),201);
    private static async Task<Guid> Supplier(SupplierReferenceScenario api,string code){
        Guid category=Id((await api.Options("supplier.category","en",true)).EnumerateArray().First());
        return Id(Row(await api.Send(HttpMethod.Post,"/api/v1/suppliers/core",new{identity=new{supplierCode=code,displayName="Sourcing "+code,entityType="Individual",status="Active"},categoryIds=new[]{category}},201),"supplier"));
    }
    private async Task Reject(Guid company,string sql,string state){
        await using var connection=new NpgsqlConnection(database.ConnectionString);await connection.OpenAsync(Ct);await using var tx=await connection.BeginTransactionAsync(Ct);
        await using(var scope=new NpgsqlCommand("SELECT set_config('barang.company_id',@company,true)",connection,tx)){scope.Parameters.AddWithValue("company",company.ToString());await scope.ExecuteNonQueryAsync(Ct);}
        await using var command=new NpgsqlCommand(sql,connection,tx);var error=await Assert.ThrowsAsync<PostgresException>(async()=>await command.ExecuteNonQueryAsync(Ct));
        Assert.Equal(state,error.SqlState);await tx.RollbackAsync(Ct);
    }
    private async Task<Fixture> Setup(){
        await database.ResetAsync(Ct);Guid company=Guid.NewGuid(),other=Guid.NewGuid();
        await using(var db=database.CreateAdministrationCoreDbContext()){db.Companies.AddRange(Company.Create(company,"SOURCE_A","Sourcing A",DateTimeOffset.UtcNow).Value,Company.Create(other,"SOURCE_B","Sourcing B",DateTimeOffset.UtcNow).Value);await db.SaveChangesAsync(Ct);}
        var factory=new PostgreSqlCoreApiFactory(database.ConnectionString);var client=factory.CreateClient();
        using var bootstrap=new HttpRequestMessage(HttpMethod.Post,"/api/v1/security/bootstrap"){Content=JsonContent.Create(new{companyId=company,username="sourcing-admin",displayName="Sourcing Admin",password="SourcingAdmin123!"})};
        bootstrap.Headers.Add("X-Bootstrap-Secret","test-bootstrap-secret-123456789");using(var response=await client.SendAsync(bootstrap,Ct))response.EnsureSuccessStatusCode();
        var login=await SupplierReferenceScenario.Send(client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username="sourcing-admin",password="SourcingAdmin123!"},200,Ct);
        var api=new SupplierReferenceScenario(client,login.GetProperty("accessToken").GetString()!,company,Ct);
        Guid unit=Id(await api.CreateValue("unit.of.measure","sourcing.unit")),packaging=Id(await api.CreateValue("packaging.type","sourcing.pack"));
        Guid product=Id(await api.Send(HttpMethod.Post,"/api/v1/products",new{productCode="SOURCE-ITEM",name="Sourcing item",productTypeCode="RAW",unitOfMeasureCode="SOURCING.UNIT"},201));
        Guid variant=Id(await api.Send(HttpMethod.Post,$"/api/v1/products/{product}/variants",new{sku="SOURCE-SKU",displayName="Sourcing SKU"},201));
        return new(factory,client,api,company,other,await Supplier(api,"SOURCE-SUPPLIER"),product,variant,unit,packaging);
    }
    private sealed record Fixture(PostgreSqlCoreApiFactory Factory,HttpClient Client,SupplierReferenceScenario Api,Guid Company,Guid Other,Guid Supplier,Guid Product,Guid Variant,Guid Unit,Guid Packaging):IAsyncDisposable{
        public async ValueTask DisposeAsync(){Client.Dispose();await Factory.DisposeAsync();}
    }
}
