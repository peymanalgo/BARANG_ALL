using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using BARANG.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using static BARANG.TestSupport.SupplierReferenceScenario;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class SupplierCoreApiTests(PostgreSqlDatabaseFixture database)
{
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    private static JsonElement Root(JsonElement value)=>value.GetProperty("supplier");
    private static Guid RootId(JsonElement value)=>Id(Root(value));
    private static long RootVersion(JsonElement value)=>Version(Root(value));
    private static long FinancialVersion(JsonElement value)=>value.GetProperty("financial").ValueKind==JsonValueKind.Object?Version(value.GetProperty("financial")):0;
    private static SupplierCoreCommand Command(Guid category,string code="CORE-1",string type="Individual")=>new(
        new(SupplierCode:code,DisplayName:"Core supplier "+code,EntityType:type,LegalName:type=="LegalEntity"?"Legal supplier":null,
            FirstName:type=="Individual"?"Supplier":null,LastName:type=="Individual"?"Contact":null,PrimaryMobile:"+989120000123",PrimaryEmail:"supplier@example.com",
            NationalId:"NATIONAL-"+code,CooperationStartDate:new(1985,6,8),EstablishmentDate:type=="LegalEntity"?new(1985,6,8):null,Notes:"Initial notes"),
        [category],new(DefaultCurrencyCode:"IRR",MinimumOrderAmount:10,DefaultLeadTimeDays:3,BaseDiscountPercent:5,CreditPurchaseAllowed:true,PurchaseCreditLimit:1000));
    private static SupplierCoreCommand Update(SupplierCoreCommand command,JsonElement value)=>command with{Version=RootVersion(value),FinancialVersion=FinancialVersion(value)};

    [Theory][InlineData("Individual")][InlineData("LegalEntity")]
    public async Task RootRoundTripStatusConcurrencySearchSortAndSafeDelete(string type){
        await using var f=await Setup();var command=Command(f.Category,type:type);
        var created=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",command,201);Guid id=RootId(created);
        Assert.Equal(f.Company,Root(created).GetProperty("companyId").GetGuid());Assert.Equal(1,Root(created).GetProperty("supplierNumber").GetInt64());
        Assert.Equal("1985-06-08",Root(created).GetProperty("cooperationStartDate").GetString());
        Assert.Equal(type,Root(created).GetProperty("entityType").GetString());
        var saved=await f.Api.Send(HttpMethod.Put,$"/api/v1/suppliers/core/{id}",Update(command,created) with{Identity=command.Identity with{Notes="Persisted notes",IsPreferred=true}});
        var loaded=await f.Api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}");
        Assert.Equal("Persisted notes",Root(loaded).GetProperty("notes").GetString());Assert.True(Root(loaded).GetProperty("isPreferred").GetBoolean());
        Assert.Equal(5,loaded.GetProperty("financial").GetProperty("baseDiscountPercent").GetDecimal());
        await f.Api.Send(HttpMethod.Put,$"/api/v1/suppliers/core/{id}",Update(command,created),409);
        foreach(string search in new[]{"CORE-1","NATIONAL-CORE-1","supplier@example.com","+989120000123","1"}){
            var list=await f.Api.Send(HttpMethod.Get,"/api/v1/suppliers/core?search="+Uri.EscapeDataString(search));
            Assert.Equal(id,RootId(Assert.Single(list.GetProperty("items").EnumerateArray())));
        }
        var other=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category,"CORE-2"),201);
        var descending=await f.Api.Send(HttpMethod.Get,"/api/v1/suppliers/core?pageSize=1&sortBy=number&sortDescending=true");
        Assert.Equal(2,descending.GetProperty("totalCount").GetInt64());Assert.Equal(RootId(other),RootId(Assert.Single(descending.GetProperty("items").EnumerateArray())));
        var second=await f.Api.Send(HttpMethod.Get,"/api/v1/suppliers/core?pageSize=1&pageNumber=2&sortBy=number&sortDescending=true");
        Assert.Equal(id,RootId(Assert.Single(second.GetProperty("items").EnumerateArray())));
        var hold=await f.Api.CreateValue("supplier.hold.reason","core.hold");
        saved=await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{id}/status",new{status="Blocked",holdReasonId=Id(hold),version=RootVersion(saved)});
        Assert.False(Root(saved).GetProperty("isActive").GetBoolean());
        Assert.DoesNotContain((await f.Api.Send(HttpMethod.Get,"/api/v1/suppliers/options?limit=100")).EnumerateArray(),x=>Id(x)==id);
        Assert.Equal("supplier.blocked",(await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{id}/activation",new{isActive=true,version=RootVersion(saved)},422)).GetProperty("code").GetString());
        foreach(string status in new[]{"Active","Inactive","Active"}){
            saved=await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{id}/status",new{status,version=RootVersion(saved)});
            Assert.Equal(status,Root(saved).GetProperty("status").GetString());
        }
        Assert.True((await f.Api.Send(HttpMethod.Delete,$"/api/v1/suppliers/{id}?version={RootVersion(saved)}")).GetBoolean());
        await f.Api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{id}",expected:404);
    }

    [Theory][InlineData("addresses")][InlineData("contacts")][InlineData("bank-accounts")]
    public async Task ChildCrudSelectionPersistenceAndInactiveRules(string kind){
        await using var f=await Setup();var supplier=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category),201);Guid owner=RootId(supplier);
        var refs=new Dictionary<string,Guid>();
        foreach(string type in new[]{"address.type","contact.role","bank","bank.account.type"})refs[type]=Id(await f.Api.CreateValue(type,"core.child"));
        string path=$"/api/v1/suppliers/{owner}/{kind}",selected=kind=="contacts"?"isPrimary":"isDefault",action=kind=="contacts"?"primary":"default";
        object Data(string name,bool chosen=false)=>kind switch{
            "addresses"=>new SupplierAddressData(AddressName:name,AddressTypeId:refs["address.type"],FullAddress:"Address road",Email:"address@example.com",IsDefault:chosen),
            "contacts"=>new SupplierContactData(FirstName:name,LastName:"Contact",ContactRoleId:refs["contact.role"],Extension:"12",IsPrimary:chosen),
            _=>new SupplierBankData(BankId:refs["bank"],AccountTypeId:refs["bank.account.type"],AccountName:name,AccountHolderNames:"First; Second",OwnershipType:"Joint",AccountNumber:name,IsDefault:chosen)};
        var first=await f.Api.Send(HttpMethod.Post,path,new{data=Data("First",true)},201);
        var second=await f.Api.Send(HttpMethod.Post,path,new{data=Data("Second")},201);
        await f.Api.Send(HttpMethod.Post,path+"/"+Id(second)+"/"+action,new{version=Version(second)});
        var rows=await f.Api.Send(HttpMethod.Get,path);
        Assert.Equal(Id(second),Id(Assert.Single(rows.EnumerateArray(),x=>x.GetProperty(selected).GetBoolean())));
        first=rows.EnumerateArray().Single(x=>Id(x)==Id(first));second=rows.EnumerateArray().Single(x=>Id(x)==Id(second));
        var edited=await f.Api.Send(HttpMethod.Put,path+"/"+Id(first),new{data=Data("Updated"),version=Version(first)});
        await f.Api.Send(HttpMethod.Put,path+"/"+Id(first),new{data=Data("Stale"),version=Version(first)},409);
        Assert.Equal("supplier.referenced",(await f.Api.Send(HttpMethod.Delete,$"/api/v1/suppliers/{owner}?version={RootVersion(supplier)}",expected:422)).GetProperty("code").GetString());
        var disabled=await f.Api.Send(HttpMethod.Post,path+"/"+Id(second)+"/activation",new{isActive=false,version=Version(second)});
        Assert.False(disabled.GetProperty(selected).GetBoolean());
        Assert.Equal("supplier.inactive_default",(await f.Api.Send(HttpMethod.Post,path+"/"+Id(second)+"/"+action,new{version=Version(disabled)},422)).GetProperty("code").GetString());
        rows=await f.Api.Send(HttpMethod.Get,path);Assert.DoesNotContain(rows.EnumerateArray(),x=>x.GetProperty(selected).GetBoolean());
        if(kind=="addresses")Assert.Equal(JsonValueKind.Null,(await f.Api.Send(HttpMethod.Get,$"/api/v1/suppliers/{owner}/default-address")).GetProperty("address").ValueKind);
        var active=await f.Api.Send(HttpMethod.Post,path+"/"+Id(second)+"/activation",new{isActive=true,version=Version(disabled)});
        active=await f.Api.Send(HttpMethod.Post,path+"/"+Id(second)+"/"+action,new{version=Version(active)});
        Assert.True(active.GetProperty(selected).GetBoolean());
        foreach(var row in (await f.Api.Send(HttpMethod.Get,path)).EnumerateArray())Assert.True((await f.Api.Send(HttpMethod.Delete,path+"/"+Id(row)+"?version="+Version(row))).GetBoolean());
        Assert.Empty((await f.Api.Send(HttpMethod.Get,path)).EnumerateArray());
        Assert.True((await f.Api.Send(HttpMethod.Delete,$"/api/v1/suppliers/{owner}?version={RootVersion(supplier)}")).GetBoolean());
    }

    [Fact]public async Task ConcurrentCreationAllocatesUniqueSequentialNumbersAndLegacyCreationParticipates(){
        await using var f=await Setup();
        var results=await Task.WhenAll(Enumerable.Range(0,12).Select(i=>f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category,"PAR-"+i),201)));
        Assert.Equal(Enumerable.Range(1,12).Select(x=>(long)x),results.Select(x=>Root(x).GetProperty("supplierNumber").GetInt64()).Order());
        var legacy=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers",new{supplierCode="LEGACY",displayName="Legacy client"},201);
        var loaded=await f.Api.Send(HttpMethod.Get,"/api/v1/suppliers/core/"+Id(legacy));
        Assert.Equal(13,Root(loaded).GetProperty("supplierNumber").GetInt64());Assert.Equal("LEGACY",Root(loaded).GetProperty("supplierCode").GetString());
        var first=results[0];await f.Api.Send(HttpMethod.Delete,$"/api/v1/suppliers/{RootId(first)}?version={RootVersion(first)}");
        var next=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category,"AFTER-DELETE"),201);
        Assert.Equal(14,Root(next).GetProperty("supplierNumber").GetInt64());
    }

    [Fact]public async Task AllModeledReferenceIdsAreProtectedAndInactiveHistoryRemainsValid(){
        await using var f=await Setup();var references=new Dictionary<string,JsonElement>();
        foreach(string type in new[]{"supplier.category","supplier.grade","supplier.hold.reason","freight.delivery.term","legal.entity.type","address.type","contact.role","payment.term","payment.condition","payment.method","shipping.method","bank","bank.account.type"})references[type]=await f.Api.CreateValue(type,"core.reference");
        Guid Ref(string type)=>Id(references[type]);
        var command=Command(Ref("supplier.category"),type:"LegalEntity");
        command=command with{Identity=command.Identity with{LegalEntityTypeId=Ref("legal.entity.type"),GradeId=Ref("supplier.grade"),Status="Blocked",HoldReasonId=Ref("supplier.hold.reason")},
            Financial=command.Financial! with{PaymentTermId=Ref("payment.term"),PaymentConditionId=Ref("payment.condition"),PaymentMethodId=Ref("payment.method"),ShippingMethodId=Ref("shipping.method"),FreightDeliveryTermId=Ref("freight.delivery.term")}};
        var supplier=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",command,201);Guid owner=RootId(supplier);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{owner}/addresses",new{data=new SupplierAddressData(AddressName:"Main",AddressTypeId:Ref("address.type"),FullAddress:"Road")},201);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{owner}/contacts",new{data=new SupplierContactData(FirstName:"Contact",ContactRoleId:Ref("contact.role"))},201);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{owner}/bank-accounts",new{data=new SupplierBankData(BankId:Ref("bank"),AccountTypeId:Ref("bank.account.type"),AccountHolderNames:"Holder")},201);
        foreach(var(type,value)in references){
            await f.Api.Delete(value,422);
            var inactive=await f.Api.Activate(value,false);
            foreach(string language in new[]{"fa","ar","en"}){
                Assert.DoesNotContain((await f.Api.Options(type,language,true)).EnumerateArray(),x=>Id(x)==Id(value));
                Assert.Contains((await f.Api.Options(type,language,false)).EnumerateArray(),x=>Id(x)==Id(value));
            }
        }
        var changed=await f.Api.Send(HttpMethod.Put,$"/api/v1/suppliers/core/{owner}",Update(command,supplier) with{Identity=command.Identity with{Notes="Inactive history retained"}});
        Assert.Equal(Ref("supplier.grade"),Root(changed).GetProperty("gradeId").GetGuid());
        await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",command with{Identity=command.Identity with{SupplierCode="NEW-INACTIVE"}},422);
        var wrong=Command(f.Category,"WRONG-TYPE") with{Identity=Command(f.Category,"WRONG-TYPE").Identity with{GradeId=Ref("bank")}};
        Assert.Equal("supplier.invalid_reference",(await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",wrong,422)).GetProperty("code").GetString());
    }

    [Fact]public async Task ExistingSupplierProductDependencyBlocksDeletionButAllowsDeactivation(){
        await using var f=await Setup();var supplier=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category),201);
        var product=await f.Api.Send(HttpMethod.Post,"/api/v1/products",new{productCode="DEPENDENCY",name="Dependency",productTypeCode="RAW",unitOfMeasureCode="PCS"},201);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{RootId(supplier)}/products",new{productId=Id(product),isPreferred=false},201);
        Assert.Equal("supplier.referenced",(await f.Api.Send(HttpMethod.Delete,$"/api/v1/suppliers/{RootId(supplier)}?version={RootVersion(supplier)}",expected:422)).GetProperty("code").GetString());
        var disabled=await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{RootId(supplier)}/status",new{status="Inactive",version=RootVersion(supplier)});
        Assert.Equal("Inactive",Root(disabled).GetProperty("status").GetString());
    }

    [Fact]public async Task OrdinaryReadOnlyPermissionsAndBothCompanyIsolationDirectionsAreEnforced(){
        await using var f=await Setup();
        var a=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category),201);
        Guid otherCategory=Id((await f.Api.InCompany(f.Other).Options("supplier.category","en",true)).EnumerateArray().First());
        var b=await f.Api.InCompany(f.Other).Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(otherCategory),201);
        await f.Api.InCompany(f.Other).Send(HttpMethod.Get,$"/api/v1/suppliers/core/{RootId(a)}",expected:404);
        await f.Api.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{RootId(b)}",expected:404);
        await f.Api.InCompany(f.Other).Send(HttpMethod.Put,$"/api/v1/suppliers/core/{RootId(a)}",Update(Command(f.Category),a),404);
        foreach(string kind in new[]{"addresses","contacts","bank-accounts"})await f.Api.InCompany(f.Other).Send(HttpMethod.Get,$"/api/v1/suppliers/{RootId(a)}/{kind}",expected:404);
        Assert.DoesNotContain((await f.Api.Send(HttpMethod.Get,"/api/v1/suppliers/core")).GetProperty("items").EnumerateArray(),x=>RootId(x)==RootId(b));
        Assert.DoesNotContain((await f.Api.InCompany(f.Other).Send(HttpMethod.Get,"/api/v1/suppliers/core")).GetProperty("items").EnumerateArray(),x=>RootId(x)==RootId(a));
        var person=await f.Api.Send(HttpMethod.Post,"/api/v1/organization/persons",new{firstName="Supplier",lastName="Reader",phone="+989120000079"},201);
        var user=await f.Api.Send(HttpMethod.Post,"/api/v1/security/users/linked",new{personId=Id(person),username="supplier-core-reader",password="SupplierCoreReader123!",enabled=true});
        var role=await f.Api.Send(HttpMethod.Post,"/api/v1/security/roles",new{code="supplier.core.reader",displayTitle="Supplier core reader"});
        var permissions=await f.Api.Send(HttpMethod.Get,"/api/v1/security/permissions");Guid read=Id(permissions.EnumerateArray().Single(x=>x.GetProperty("code").GetString()=="supplier.read"));
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/roles/{Id(role)}/permissions/{read}",expected:204);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/users/{Id(user)}/roles",new{roleId=Id(role),companyId=f.Company});
        string token=(await SupplierReferenceScenario.Send(f.Client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username="supplier-core-reader",password="SupplierCoreReader123!"},200,Ct)).GetProperty("accessToken").GetString()!;
        var reader=new SupplierReferenceScenario(f.Client,token,f.Company,Ct);
        await reader.Send(HttpMethod.Get,$"/api/v1/suppliers/core/{RootId(a)}");
        await reader.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category,"DENIED"),403);
        await reader.Send(HttpMethod.Put,$"/api/v1/suppliers/core/{RootId(a)}",Update(Command(f.Category),a),403);
        await reader.Send(HttpMethod.Delete,$"/api/v1/suppliers/{RootId(a)}?version={RootVersion(a)}",expected:403);
        foreach(string kind in new[]{"addresses","contacts","bank-accounts"})await reader.Send(HttpMethod.Post,$"/api/v1/suppliers/{RootId(a)}/{kind}",new{data=new{}},403);
        await reader.InCompany(f.Other).Send(HttpMethod.Get,"/api/v1/suppliers/core",expected:403);
    }

    [Fact]public async Task PostgreSqlEnforcesNumberImmutabilityDefaultUniquenessAndInactiveDefault(){
        await using var f=await Setup();var supplier=await f.Api.Send(HttpMethod.Post,"/api/v1/suppliers/core",Command(f.Category),201);
        var type=await f.Api.CreateValue("address.type","db.test");Guid owner=RootId(supplier);
        var first=await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{owner}/addresses",new{data=new SupplierAddressData(AddressName:"First",AddressTypeId:Id(type),FullAddress:"Road",IsDefault:true)},201);
        var second=await f.Api.Send(HttpMethod.Post,$"/api/v1/suppliers/{owner}/addresses",new{data=new SupplierAddressData(AddressName:"Second",AddressTypeId:Id(type),FullAddress:"Road")},201);
        await RejectSql(f.Company,$"UPDATE barang.suppliers SET supplier_number=999 WHERE id='{owner}'","23514");
        await RejectSql(f.Company,$"UPDATE barang.supplier_addresses SET is_default=true WHERE id='{Id(second)}'","23505");
        await RejectSql(f.Company,$"UPDATE barang.supplier_addresses SET is_active=false WHERE id='{Id(first)}'","23514");
        await RejectSql(f.Company,$"DELETE FROM barang.lookup_values WHERE id='{Id(type)}'","23503");
    }
    private async Task RejectSql(Guid company,string sql,string state){
        await using var connection=new NpgsqlConnection(database.ConnectionString);await connection.OpenAsync(Ct);await using var tx=await connection.BeginTransactionAsync(Ct);
        await using(var security=new NpgsqlCommand("SELECT set_config('barang.company_id',@company,true)",connection,tx)){security.Parameters.AddWithValue("company",company.ToString());await security.ExecuteNonQueryAsync(Ct);}
        await using var command=new NpgsqlCommand(sql,connection,tx);
        var exception=await Assert.ThrowsAsync<PostgresException>(async()=>await command.ExecuteNonQueryAsync(Ct));Assert.Equal(state,exception.SqlState);await tx.RollbackAsync(Ct);
    }
    private async Task<Fixture> Setup(){
        await database.ResetAsync(Ct);Guid company=Guid.NewGuid(),other=Guid.NewGuid();
        await using(var db=database.CreateAdministrationCoreDbContext()){
            db.Companies.AddRange(Company.Create(company,"SUPCORE_A","Supplier core A",DateTimeOffset.UtcNow).Value,Company.Create(other,"SUPCORE_B","Supplier core B",DateTimeOffset.UtcNow).Value);await db.SaveChangesAsync(Ct);
        }
        var factory=new PostgreSqlCoreApiFactory(database.ConnectionString);var client=factory.CreateClient();
        using var bootstrap=new HttpRequestMessage(HttpMethod.Post,"/api/v1/security/bootstrap"){Content=JsonContent.Create(new{companyId=company,username="supplier-core-admin",displayName="Supplier Core Admin",password="SupplierCoreAdmin123!"})};
        bootstrap.Headers.Add("X-Bootstrap-Secret","test-bootstrap-secret-123456789");
        using(var response=await client.SendAsync(bootstrap,Ct))response.EnsureSuccessStatusCode();
        var login=await SupplierReferenceScenario.Send(client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username="supplier-core-admin",password="SupplierCoreAdmin123!"},200,Ct);
        var api=new SupplierReferenceScenario(client,login.GetProperty("accessToken").GetString()!,company,Ct);
        Guid category=Id((await api.Options("supplier.category","en",true)).EnumerateArray().First());
        return new(factory,client,api,company,other,category);
    }
    private sealed record Fixture(PostgreSqlCoreApiFactory Factory,HttpClient Client,SupplierReferenceScenario Api,Guid Company,Guid Other,Guid Category):IAsyncDisposable{
        public async ValueTask DisposeAsync(){Client.Dispose();await Factory.DisposeAsync();}
    }
}
