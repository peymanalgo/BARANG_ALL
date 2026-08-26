using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;

namespace BARANG.Core.Application.Customers;

public sealed record CustomerView(Guid Id,Guid CompanyId,string CustomerCode,CustomerType CustomerType,string DisplayName,string? FirstName,string? LastName,string? LegalName,string? NationalId,string? RegistrationNo,string? TaxId,string? PrimaryMobile,string? PrimaryPhone,string? PrimaryEmail,bool IsActive,string? Notes,DateTimeOffset CreatedAtUtc,DateTimeOffset UpdatedAtUtc,long Version);
public sealed record CustomerContactView(Guid Id,Guid CustomerId,string ContactTypeCode,string? Name,string? Mobile,string? Phone,string? Email,string? JobTitle,bool IsPrimary,bool IsActive,int DisplayOrder,long Version);
public sealed record CustomerAddressView(Guid Id,Guid CustomerId,string AddressTypeCode,string Title,string CountryCode,string? Province,string? City,string? District,string AddressLine1,string? AddressLine2,string? PostalCode,string? RecipientName,string? RecipientPhone,bool IsPrimary,bool IsActive,int DisplayOrder,long Version);
public sealed record CustomerOptionView(Guid Id,string CustomerCode,string DisplayName,CustomerType CustomerType,string? PrimaryMobile);
public sealed record CustomerQuery(string? Search=null,bool ActiveOnly=false,int PageNumber=1,int PageSize=PagedRequest.DefaultPageSize);
public sealed record CustomerDataCommand(string CustomerCode,CustomerType CustomerType,string? DisplayName,string? FirstName,string? LastName,string? LegalName,string? NationalId,string? RegistrationNo,string? TaxId,string? PrimaryMobile,string? PrimaryPhone,string? PrimaryEmail,string? Notes);
public sealed record CustomerUpdateCommand(string? DisplayName,string? FirstName,string? LastName,string? LegalName,string? NationalId,string? RegistrationNo,string? TaxId,string? PrimaryMobile,string? PrimaryPhone,string? PrimaryEmail,string? Notes,long Version);
public sealed record ContactCommand(string ContactTypeCode,string? Name,string? Mobile,string? Phone,string? Email,string? JobTitle,bool IsPrimary,int DisplayOrder,long Version=0);
public sealed record AddressCommand(string AddressTypeCode,string Title,string CountryCode,string? Province,string? City,string? District,string AddressLine1,string? AddressLine2,string? PostalCode,string? RecipientName,string? RecipientPhone,bool IsPrimary,int DisplayOrder,long Version=0);

public interface ICustomerAdministration
{
    Task<Result<CustomerView>> CreateAsync(CustomerDataCommand c,CancellationToken ct); Task<Result<CustomerView>> GetAsync(Guid id,CancellationToken ct); Task<Result<CustomerView>> GetByCodeAsync(string code,CancellationToken ct); Task<Result<PagedResult<CustomerView>>> SearchAsync(CustomerQuery query,CancellationToken ct); Task<Result<IReadOnlyList<CustomerOptionView>>> OptionsAsync(string? search,bool activeOnly,int limit,CancellationToken ct); Task<Result<CustomerView>> UpdateAsync(Guid id,CustomerUpdateCommand c,CancellationToken ct); Task<Result<CustomerView>> SetActiveAsync(Guid id,bool active,long version,CancellationToken ct);
    Task<Result<CustomerContactView>> AddContactAsync(Guid customerId,ContactCommand c,CancellationToken ct); Task<Result<IReadOnlyList<CustomerContactView>>> ListContactsAsync(Guid customerId,bool activeOnly,CancellationToken ct); Task<Result<CustomerContactView>> UpdateContactAsync(Guid customerId,Guid id,ContactCommand c,CancellationToken ct); Task<Result<CustomerContactView>> SetContactActiveAsync(Guid customerId,Guid id,bool active,long version,CancellationToken ct);
    Task<Result<CustomerAddressView>> AddAddressAsync(Guid customerId,AddressCommand c,CancellationToken ct); Task<Result<IReadOnlyList<CustomerAddressView>>> ListAddressesAsync(Guid customerId,bool activeOnly,CancellationToken ct); Task<Result<CustomerAddressView>> UpdateAddressAsync(Guid customerId,Guid id,AddressCommand c,CancellationToken ct); Task<Result<CustomerAddressView>> SetAddressActiveAsync(Guid customerId,Guid id,bool active,long version,CancellationToken ct);
}

public interface ICustomerStore
{
    void Add(Customer value);void Add(CustomerContact value);void Add(CustomerAddress value); Task<Customer?> FindAsync(Guid id,CancellationToken ct);Task<Customer?> FindByCodeAsync(string code,CancellationToken ct);Task<bool> CodeExistsAsync(string code,CancellationToken ct);Task<bool> IdentifierExistsAsync(string? national,string? registration,Guid? excludingId,CancellationToken ct);Task<PagedResult<Customer>> SearchAsync(CustomerQuery query,CancellationToken ct);Task<IReadOnlyList<Customer>> OptionsAsync(string? search,bool activeOnly,int limit,CancellationToken ct);
    Task<CustomerContact?> FindContactAsync(Guid customerId,Guid id,CancellationToken ct);Task<IReadOnlyList<CustomerContact>> ListContactsAsync(Guid customerId,bool activeOnly,CancellationToken ct);Task<bool> OtherPrimaryContactExistsAsync(Guid customerId,Guid? excludingId,CancellationToken ct);
    Task<CustomerAddress?> FindAddressAsync(Guid customerId,Guid id,CancellationToken ct);Task<IReadOnlyList<CustomerAddress>> ListAddressesAsync(Guid customerId,bool activeOnly,CancellationToken ct);Task<bool> OtherPrimaryAddressExistsAsync(Guid customerId,Guid? excludingId,CancellationToken ct);
}
