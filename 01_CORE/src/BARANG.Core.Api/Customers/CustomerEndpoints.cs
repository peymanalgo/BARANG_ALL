using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Customers;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root=endpoints.MapGroup("/api/v1/customers");RouteGroupBuilder read=root.MapGroup("").RequireAuthorization(SecurityPermissions.CustomerRead);
        read.MapGet("/",async([AsParameters]CustomerSearchRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SearchAsync(new(r.Search,r.ActiveOnly,r.PageNumber,r.PageSize),ct)));
        read.MapGet("/options",async(string? search,bool? activeOnly,int? limit,ICustomerAdministration s,CancellationToken ct)=>Map(await s.OptionsAsync(search,activeOnly??true,limit??20,ct)));
        read.MapGet("/{id:guid}",async(Guid id,ICustomerAdministration s,CancellationToken ct)=>Map(await s.GetAsync(id,ct)));read.MapGet("/by-code/{code}",async(string code,ICustomerAdministration s,CancellationToken ct)=>Map(await s.GetByCodeAsync(code,ct)));
        read.MapGet("/{id:guid}/contacts",async(Guid id,bool? activeOnly,ICustomerAdministration s,CancellationToken ct)=>Map(await s.ListContactsAsync(id,activeOnly??false,ct)));read.MapGet("/{id:guid}/addresses",async(Guid id,bool? activeOnly,ICustomerAdministration s,CancellationToken ct)=>Map(await s.ListAddressesAsync(id,activeOnly??false,ct)));
        RouteGroupBuilder manage=root.MapGroup("").RequireAuthorization(SecurityPermissions.CustomerManage);
        manage.MapPost("/",async(CustomerCreateRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.CreateAsync(r.Command(),ct),true));manage.MapPut("/{id:guid}",async(Guid id,CustomerUpdateRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpdateAsync(id,r.Command(),ct)));manage.MapPost("/{id:guid}/activation",async(Guid id,CustomerActivationRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetActiveAsync(id,r.IsActive,r.Version,ct)));
        manage.MapPost("/{id:guid}/contacts",async(Guid id,ContactRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.AddContactAsync(id,r.Command(),ct),true));manage.MapPut("/{id:guid}/contacts/{contactId:guid}",async(Guid id,Guid contactId,ContactRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpdateContactAsync(id,contactId,r.Command(),ct)));manage.MapPost("/{id:guid}/contacts/{contactId:guid}/activation",async(Guid id,Guid contactId,CustomerActivationRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetContactActiveAsync(id,contactId,r.IsActive,r.Version,ct)));
        manage.MapPost("/{id:guid}/addresses",async(Guid id,AddressRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.AddAddressAsync(id,r.Command(),ct),true));manage.MapPut("/{id:guid}/addresses/{addressId:guid}",async(Guid id,Guid addressId,AddressRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpdateAddressAsync(id,addressId,r.Command(),ct)));manage.MapPost("/{id:guid}/addresses/{addressId:guid}/activation",async(Guid id,Guid addressId,CustomerActivationRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetAddressActiveAsync(id,addressId,r.IsActive,r.Version,ct)));
        return endpoints;
    }
    private static CustomerResult<T> Map<T>(Result<T> result,bool created=false)=>new(result,created);private sealed class CustomerResult<T>(Result<T> result,bool created):IResult{public Task ExecuteAsync(HttpContext c){if(result.IsSuccess){c.Response.StatusCode=created?201:200;return c.Response.WriteAsJsonAsync(result.Value,c.RequestAborted);}ICorrelationContext correlation=c.RequestServices.GetRequiredService<ICorrelationContext>();ProblemDetails p=ApiProblemDetailsMapper.Create(result.Error,correlation.CorrelationId??c.TraceIdentifier,c.Request.Path);c.Response.StatusCode=p.Status!.Value;return c.Response.WriteAsJsonAsync(p,c.RequestAborted);}}
}
public sealed record CustomerSearchRequest(string? Search=null,bool ActiveOnly=false,int PageNumber=1,int PageSize=20);
public sealed record CustomerCreateRequest(string CustomerCode,CustomerType CustomerType,string? DisplayName,string? FirstName,string? LastName,string? LegalName,string? NationalId,string? RegistrationNo,string? TaxId,string? PrimaryMobile,string? PrimaryPhone,string? PrimaryEmail,string? Notes){public CustomerDataCommand Command()=>new(CustomerCode,CustomerType,DisplayName,FirstName,LastName,LegalName,NationalId,RegistrationNo,TaxId,PrimaryMobile,PrimaryPhone,PrimaryEmail,Notes);}
public sealed record CustomerUpdateRequest(string? DisplayName,string? FirstName,string? LastName,string? LegalName,string? NationalId,string? RegistrationNo,string? TaxId,string? PrimaryMobile,string? PrimaryPhone,string? PrimaryEmail,string? Notes,long Version){public CustomerUpdateCommand Command()=>new(DisplayName,FirstName,LastName,LegalName,NationalId,RegistrationNo,TaxId,PrimaryMobile,PrimaryPhone,PrimaryEmail,Notes,Version);}
public sealed record ContactRequest(string ContactTypeCode,string? Name,string? Mobile,string? Phone,string? Email,string? JobTitle,bool IsPrimary,int DisplayOrder,long Version=0){public ContactCommand Command()=>new(ContactTypeCode,Name,Mobile,Phone,Email,JobTitle,IsPrimary,DisplayOrder,Version);}
public sealed record AddressRequest(string AddressTypeCode,string Title,string CountryCode,string? Province,string? City,string? District,string AddressLine1,string? AddressLine2,string? PostalCode,string? RecipientName,string? RecipientPhone,bool IsPrimary,int DisplayOrder,long Version=0){public AddressCommand Command()=>new(AddressTypeCode,Title,CountryCode,Province,City,District,AddressLine1,AddressLine2,PostalCode,RecipientName,RecipientPhone,IsPrimary,DisplayOrder,Version);}
public sealed record CustomerActivationRequest(bool IsActive,long Version);
