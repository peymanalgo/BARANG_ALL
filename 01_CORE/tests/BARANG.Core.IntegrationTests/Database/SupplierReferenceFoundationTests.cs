using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Domain.Lookups;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using BARANG.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using static BARANG.TestSupport.SupplierReferenceScenario;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class SupplierReferenceFoundationTests(PostgreSqlDatabaseFixture database)
{
    private static CancellationToken Ct=>TestContext.Current.CancellationToken;
    public static TheoryData<string> Catalogs=>new(Types);

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task SharedSupplierCatalogCrudPreservesIdentityScopeHistoryAuditAndConcurrency(string type)
    {
        await using var fixture=await Setup();
        SupplierReferenceScenario api=fixture.Api;
        JsonElement originalType=await api.Send(HttpMethod.Get,$"/api/v1/lookups/types/by-code/{type}");
        foreach(string culture in new[]{"fa","ar","en"})
        {
            JsonElement domains=await api.Send(HttpMethod.Get,$"/api/v1/lookups/domains?culture={culture}");
            JsonElement shared=Assert.Single(domains.EnumerateArray(),x=>x.GetProperty("code").GetString()=="shared");
            Assert.Equal(Types.Order(),shared.GetProperty("types").EnumerateArray().Select(x=>x.GetProperty("code").GetString()!).Order());
            Assert.Equal(DomainReferenceCatalog.Domains.Single(x=>x.Code=="shared").Caption(culture),shared.GetProperty("caption").GetString());
            JsonElement catalog=Assert.Single(shared.GetProperty("types").EnumerateArray(),x=>x.GetProperty("code").GetString()==type);
            Assert.Equal(Global(type)?"Global":"Company",catalog.GetProperty("scope").GetString());
            Assert.True(catalog.GetProperty("canManage").GetBoolean());
        }
        JsonElement value=await api.CreateValue(type,"supplier.test",10);
        Guid id=Id(value);
        Assert.Equal(Id(originalType),value.GetProperty("lookupTypeId").GetGuid());
        if(Global(type))Assert.Equal(JsonValueKind.Null,value.GetProperty("companyId").ValueKind);
        else Assert.Equal(fixture.Company,value.GetProperty("companyId").GetGuid());
        Assert.Equal("supplier.test",value.GetProperty("code").GetString());
        Assert.True(value.GetProperty("createdAtUtc").GetDateTimeOffset()<=value.GetProperty("updatedAtUtc").GetDateTimeOffset());
        Assert.Equal(id,Id(await api.Send(HttpMethod.Get,$"/api/v1/lookups/types/{type}/values/by-code/supplier.test")));
        JsonElement duplicate=await api.Send(HttpMethod.Post,$"/api/v1/lookups/types/{type}/values",new{code="SUPPLIER.TEST",displayOrder=10,translations=Captions("Duplicate")},409);
        Assert.Equal("lookup.duplicate_code",duplicate.GetProperty("code").GetString());
        Dictionary<string,string> names=Captions("Updated"),descriptions=Captions("Description");
        JsonElement changed=await api.Send(HttpMethod.Put,$"/api/v1/lookups/values/{id}/editor",new{displayOrder=20,isActive=true,translations=names,descriptions,version=Version(value)});
        Assert.Equal(id,Id(changed));Assert.True(Version(changed)>Version(value));Assert.Equal(20,changed.GetProperty("displayOrder").GetInt32());
        JsonElement stale=await api.Send(HttpMethod.Put,$"/api/v1/lookups/values/{id}/editor",new{displayOrder=99,isActive=true,translations=names,version=Version(value)},409);
        Assert.Equal("lookup.concurrency_conflict",stale.GetProperty("code").GetString());
        changed=await api.Activate(changed,false);
        foreach(string culture in new[]{"fa","ar","en"})
        {
            Assert.DoesNotContain((await api.Options(type,culture,true)).EnumerateArray(),x=>Id(x)==id);
            JsonElement historical=Assert.Single((await api.Options(type,culture,false)).EnumerateArray(),x=>Id(x)==id);
            Assert.False(historical.GetProperty("isActive").GetBoolean());Assert.Equal(names[culture],historical.GetProperty("displayName").GetString());
            JsonElement translation=Assert.Single((await api.Value(id)).GetProperty("translations").EnumerateArray(),x=>x.GetProperty("culture").GetString()==culture);
            Assert.Equal(descriptions[culture],translation.GetProperty("description").GetString());
        }
        changed=await api.Activate(changed,true);
        Assert.Contains((await api.Options(type,"en",true)).EnumerateArray(),x=>Id(x)==id);
        Assert.True((await api.Delete(changed)).GetBoolean());
        await api.Send(HttpMethod.Get,$"/api/v1/lookups/values/{id}",expected:404);
        Assert.Equal(Id(originalType),Id(await api.Send(HttpMethod.Get,$"/api/v1/lookups/types/by-code/{type}")));
        await using CoreDbContext db=database.CreateAdministrationCoreDbContext();
        string[] actions=await db.AuditEvents.IgnoreQueryFilters().Where(x=>x.CompanyId==fixture.Company).Select(x=>x.Action).ToArrayAsync(Ct);
        foreach(string action in new[]{"lookup.value.created","lookup.value.updated","lookup.value.deactivated","lookup.value.activated","lookup.value.deleted"})
            Assert.Contains(action,actions);
    }

    [Fact]
    public async Task EverySharedCatalogRejectsDeletionThroughApiAndDatabaseWhenBusinessHistoryReferencesIt()
    {
        await using var fixture=await Setup();
        await fixture.Api.SeedDependencies(fixture.Branch,"integration");
        foreach(var (type,initial) in fixture.Api.References.Where(x=>ReferencedTypes.Contains(x.Key)))
        {
            Guid id=Id(initial);
            JsonElement rejected=await fixture.Api.Delete(await fixture.Api.Value(id),422);
            Assert.Equal("lookup.value_referenced",rejected.GetProperty("code").GetString());
            await using CoreDbContext db=database.CreateAdministrationCoreDbContext();
            await using var transaction=await db.Database.BeginTransactionAsync(Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('barang.company_id',{fixture.Company.ToString()},true)",Ct);
            PostgresException violation=await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM barang.lookup_values WHERE id={id}",Ct));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation,violation.SqlState);
            await transaction.RollbackAsync(Ct);
            JsonElement inactive=await fixture.Api.Activate(await fixture.Api.Value(id),false);
            Assert.False(inactive.GetProperty("isActive").GetBoolean());
            foreach(string culture in new[]{"fa","ar","en"})
            {
                Assert.DoesNotContain((await fixture.Api.Options(type,culture,true)).EnumerateArray(),x=>Id(x)==id);
                Assert.Contains((await fixture.Api.Options(type,culture,false)).EnumerateArray(),x=>Id(x)==id);
            }
        }
        Assert.NotNull(fixture.Api.CustomerId);
        JsonElement customer=await fixture.Api.Send(HttpMethod.Get,$"/api/v1/customers/{fixture.Api.CustomerId}");
        Assert.Equal(Id(fixture.Api.References["legal.entity.type"]),customer.GetProperty("legalProfile").GetProperty("legalEntityTypeId").GetGuid());
    }

    [Fact]
    public async Task OrdinaryPermissionsGlobalAdministrationAndCompanyIsolationRemainAuthoritative()
    {
        await using var f=await Setup();
        JsonElement person=await f.Api.Send(HttpMethod.Post,"/api/v1/organization/persons",new{firstName="Supplier",lastName="Reference Reader",phone="+989120000079"},201);
        JsonElement user=await f.Api.Send(HttpMethod.Post,"/api/v1/security/users/linked",new{personId=Id(person),username="supplier-reference-user",password="SupplierReferencePassword123!",enabled=true});
        JsonElement role=await f.Api.Send(HttpMethod.Post,"/api/v1/security/roles",new{code="supplier.reference.test",displayTitle="Supplier reference test"});
        JsonElement permissions=await f.Api.Send(HttpMethod.Get,"/api/v1/security/permissions");
        Guid Permission(string code)=>Id(permissions.EnumerateArray().Single(x=>x.GetProperty("code").GetString()==code));
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/roles/{Id(role)}/permissions/{Permission("lookup.read")}",expected:204);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/users/{Id(user)}/roles",new{roleId=Id(role),companyId=f.Company});
        string token=(await SupplierReferenceScenario.Send(f.Client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username="supplier-reference-user",password="SupplierReferencePassword123!"},200,Ct)).GetProperty("accessToken").GetString()!;
        SupplierReferenceScenario ordinary=new(f.Client,token,f.Company,Ct);
        var body=new{code="ordinary",displayOrder=0,translations=Captions("Ordinary")};
        await ordinary.Send(HttpMethod.Post,"/api/v1/lookups/types/contact.role/values",body,403);
        await f.Api.Send(HttpMethod.Post,$"/api/v1/security/roles/{Id(role)}/permissions/{Permission("lookup.manage")}",expected:204);
        JsonElement a=await ordinary.CreateValue("contact.role","same.code");
        JsonElement b=await f.Api.InCompany(f.Other).CreateValue("contact.role","same.code");
        Assert.NotEqual(Id(a),Id(b));
        await ordinary.InCompany(f.Other).Send(HttpMethod.Get,"/api/v1/lookups/domains",expected:403);
        await ordinary.InCompany(f.Other).Send(HttpMethod.Post,"/api/v1/lookups/types/contact.role/values",body,403);
        await f.Api.InCompany(f.Other).Send(HttpMethod.Get,$"/api/v1/lookups/values/{Id(a)}",expected:404);
        await f.Api.Send(HttpMethod.Get,$"/api/v1/lookups/values/{Id(b)}",expected:404);
        Assert.DoesNotContain((await f.Api.Options("contact.role","en",false)).EnumerateArray(),x=>Id(x)==Id(b));
        Assert.DoesNotContain((await f.Api.InCompany(f.Other).Options("contact.role","en",false)).EnumerateArray(),x=>Id(x)==Id(a));
        foreach(string type in Types.Where(Global))
        {
            JsonElement global=await f.Api.CreateValue(type,"global.test");
            Assert.Contains((await f.Api.InCompany(f.Other).Options(type,"en",true)).EnumerateArray(),x=>Id(x)==Id(global));
            Assert.Equal("lookup.system_record_protected",(await ordinary.Send(HttpMethod.Post,$"/api/v1/lookups/types/{type}/values",body,422)).GetProperty("code").GetString());
            Assert.Equal("lookup.system_record_protected",(await ordinary.Delete(global,422)).GetProperty("code").GetString());
        }
        JsonElement domains=await ordinary.Send(HttpMethod.Get,"/api/v1/lookups/domains?culture=fa");
        JsonElement supplier=domains.EnumerateArray().Single(x=>x.GetProperty("code").GetString()=="suppliers");
        Assert.All(supplier.GetProperty("types").EnumerateArray(),x=>Assert.Equal(!Global(x.GetProperty("code").GetString()!),x.GetProperty("canManage").GetBoolean()));
        await using(CoreDbContext db=database.CreateAdministrationCoreDbContext())
        {
            db.LookupTypes.Add(LookupType.Create(Guid.NewGuid(),null,"technical.supplier.state","Technical State",LookupScope.Global,true,false,DateTimeOffset.UtcNow).Value);
            await db.SaveChangesAsync(Ct);
        }
        Assert.Equal("lookup.system_record_protected",(await f.Api.Send(HttpMethod.Post,"/api/v1/lookups/types/technical.supplier.state/values",body,422)).GetProperty("code").GetString());
    }

    private async Task<Fixture> Setup()
    {
        await database.ResetAsync(Ct);Guid company=Guid.NewGuid(),other=Guid.NewGuid(),branch=Guid.NewGuid();
        await using(CoreDbContext db=database.CreateAdministrationCoreDbContext())
        {
            db.Companies.AddRange(Company.Create(company,"SUPREF_A","Supplier reference A",DateTimeOffset.UtcNow).Value,Company.Create(other,"SUPREF_B","Supplier reference B",DateTimeOffset.UtcNow).Value);
            db.Branches.Add(Branch.Create(branch,company,"SUPREF","Supplier reference branch",DateTimeOffset.UtcNow).Value);
            await db.SaveChangesAsync(Ct);
        }
        PostgreSqlCoreApiFactory factory=new(database.ConnectionString);HttpClient client=factory.CreateClient();
        using HttpRequestMessage bootstrap=new(HttpMethod.Post,"/api/v1/security/bootstrap"){Content=JsonContent.Create(new{companyId=company,username="supplier-ref-admin",displayName="Supplier Reference Admin",password="SupplierReferenceAdmin123!"})};
        bootstrap.Headers.Add("X-Bootstrap-Secret","test-bootstrap-secret-123456789");
        using(var response=await client.SendAsync(bootstrap,Ct))response.EnsureSuccessStatusCode();
        JsonElement login=await SupplierReferenceScenario.Send(client,HttpMethod.Post,"/api/v1/auth/login",null,null,new{username="supplier-ref-admin",password="SupplierReferenceAdmin123!"},200,Ct);
        return new(factory,client,new(client,login.GetProperty("accessToken").GetString()!,company,Ct),company,other,branch);
    }
    private sealed record Fixture(PostgreSqlCoreApiFactory Factory,HttpClient Client,SupplierReferenceScenario Api,Guid Company,Guid Other,Guid Branch):IAsyncDisposable
    {
        public async ValueTask DisposeAsync(){Client.Dispose();await Factory.DisposeAsync();}
    }
}
