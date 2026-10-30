using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Customers;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root=endpoints.MapGroup("/api/v1/customers");
        RouteGroupBuilder read=root.MapGroup("").RequireAuthorization(SecurityPermissions.CustomerRead);
        read.MapGet("/",async([AsParameters]CustomerSearchRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SearchAsync(new(r.Search,r.CustomerType,r.Status,r.PageNumber,r.PageSize,r.SortBy??"customerCode",r.SortDescending),ct)));
        read.MapGet("/options",async(string? search,bool? activeOnly,int? limit,ICustomerAdministration s,CancellationToken ct)=>Map(await s.OptionsAsync(search,activeOnly??true,limit??20,ct)));
        read.MapGet("/sales-representatives",async(string? search,int? limit,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SalesRepresentativesAsync(search,limit??20,ct)));
        read.MapGet("/{id:guid}",async(Guid id,ICustomerAdministration s,CancellationToken ct)=>Map(await s.GetAsync(id,ct)));
        read.MapGet("/{id:guid}/details",async(Guid id,ICustomerAdministration s,CancellationToken ct)=>Map(await s.GetDetailsAsync(id,false,ct)));
        read.MapGet("/by-code/{code}",async(string code,ICustomerAdministration s,CancellationToken ct)=>Map(await s.GetByCodeAsync(code,ct)));
        read.MapGet("/{id:guid}/contacts",async(Guid id,bool? activeOnly,ICustomerAdministration s,CancellationToken ct)=>Map(await s.ListContactsAsync(id,activeOnly??false,ct)));
        read.MapGet("/{id:guid}/addresses",async(Guid id,bool? activeOnly,ICustomerAdministration s,CancellationToken ct)=>Map(await s.ListAddressesAsync(id,activeOnly??false,ct)));

        root.MapPost("/",async(CustomerCreateRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.CreateAsync(r.Command(),ct),true)).RequireAuthorization(SecurityPermissions.CustomerManage);
        RouteGroupBuilder edit=root.MapGroup("").RequireAuthorization(SecurityPermissions.CustomerManage);
        edit.MapPut("/{id:guid}",async(Guid id,CustomerUpdateRequest r,ICustomerAdministration s,IPermissionAuthorizationService permissions,CancellationToken ct)=>
        {
            if(r.Status is not null){Result allowed=await permissions.AuthorizeAsync(SecurityPermissions.CustomerManage,ct);if(allowed.IsFailure)return Map(Result.Failure<CustomerView>(allowed.Error));}
            return Map(await s.UpdateAsync(id,r.Command(),ct));
        });
        edit.MapDelete("/{id:guid}",async(Guid id,long version,ICustomerAdministration s,CancellationToken ct)=>Map(await s.DeleteAsync(id,version,ct)));
        edit.MapPost("/{id:guid}/contacts",async(Guid id,ContactRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.AddContactAsync(id,r.Command(),ct),true));edit.MapPut("/{id:guid}/contacts/{contactId:guid}",async(Guid id,Guid contactId,ContactRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpdateContactAsync(id,contactId,r.Command(),ct)));edit.MapPost("/{id:guid}/contacts/{contactId:guid}/activation",async(Guid id,Guid contactId,ActivationRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetContactActiveAsync(id,contactId,r.IsActive,r.Version,ct)));edit.MapPost("/{id:guid}/contacts/{contactId:guid}/primary",async(Guid id,Guid contactId,VersionRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetPrimaryContactAsync(id,contactId,r.Version,ct)));
        edit.MapPost("/{id:guid}/addresses",async(Guid id,AddressRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.AddAddressAsync(id,r.Command(),ct),true));edit.MapPut("/{id:guid}/addresses/{addressId:guid}",async(Guid id,Guid addressId,AddressRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpdateAddressAsync(id,addressId,r.Command(),ct)));edit.MapPost("/{id:guid}/addresses/{addressId:guid}/activation",async(Guid id,Guid addressId,ActivationRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetAddressActiveAsync(id,addressId,r.IsActive,r.Version,ct)));edit.MapPost("/{id:guid}/addresses/{addressId:guid}/default",async(Guid id,Guid addressId,VersionRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetDefaultAddressAsync(id,addressId,r.Version,ct)));
        root.MapPost("/{id:guid}/status",async(Guid id,CustomerStatusRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetStatusAsync(id,r.Status,r.Version,ct))).RequireAuthorization(SecurityPermissions.CustomerManage);

        RouteGroupBuilder financialRead=root.MapGroup("").RequireAuthorization(SecurityPermissions.CustomerRead);
        financialRead.MapGet("/{id:guid}/financial",async(Guid id,ICustomerAdministration s,CancellationToken ct)=>Map(await s.GetFinancialAsync(id,ct)));
        financialRead.MapGet("/{id:guid}/bank-accounts",async(Guid id,bool? activeOnly,ICustomerAdministration s,CancellationToken ct)=>Map(await s.ListBankAccountsAsync(id,activeOnly??false,ct)));
        RouteGroupBuilder financial=root.MapGroup("").RequireAuthorization(SecurityPermissions.CustomerManage);
        financial.MapPut("/{id:guid}/financial",async(Guid id,FinancialRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpsertFinancialAsync(id,r.Command(),ct)));
        financial.MapPost("/{id:guid}/bank-accounts",async(Guid id,BankAccountRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.AddBankAccountAsync(id,r.Command(),ct),true));financial.MapPut("/{id:guid}/bank-accounts/{bankId:guid}",async(Guid id,Guid bankId,BankAccountRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.UpdateBankAccountAsync(id,bankId,r.Command(),ct)));financial.MapPost("/{id:guid}/bank-accounts/{bankId:guid}/activation",async(Guid id,Guid bankId,ActivationRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetBankAccountActiveAsync(id,bankId,r.IsActive,r.Version,ct)));financial.MapPost("/{id:guid}/bank-accounts/{bankId:guid}/default",async(Guid id,Guid bankId,VersionRequest r,ICustomerAdministration s,CancellationToken ct)=>Map(await s.SetDefaultBankAccountAsync(id,bankId,r.Version,ct)));
        read.MapGet("/{id:guid}/addresses/default",async(Guid id,ICustomerDefaultAddressResolver s,CancellationToken ct)=>Map(await s.ResolveDefaultAddressAsync(id,ct)));
        edit.MapDelete("/{id:guid}/addresses/{addressId:guid}",async(Guid id,Guid addressId,long version,ICustomerAdministration s,CancellationToken ct)=>Map(await s.DeleteAddressAsync(id,addressId,version,ct)));
        edit.MapDelete("/{id:guid}/contacts/{contactId:guid}",async(Guid id,Guid contactId,long version,ICustomerAdministration s,CancellationToken ct)=>Map(await s.DeleteContactAsync(id,contactId,version,ct)));
        edit.MapDelete("/{id:guid}/bank-accounts/{bankId:guid}",async(Guid id,Guid bankId,long version,ICustomerAdministration s,CancellationToken ct)=>Map(await s.DeleteBankAccountAsync(id,bankId,version,ct)));

        // Customer feedbacks (دیدگاه‌ها و پیشنهادها) — manager view over the store feedback inbox.
        read.MapGet("/feedbacks",async(string? status,string? type,int? page,int? pageSize,ITransactionExecutor tx,CoreDbContext db,CancellationToken ct)=>
        {
            Result<IReadOnlyList<CustomerFeedbackAdminView>> result=await tx.ExecuteAsync(async t=>
            {
                string? statusFilter=string.IsNullOrWhiteSpace(status)?null:status.Trim();
                string? typeFilter=string.IsNullOrWhiteSpace(type)?null:type.Trim();
                int size=Math.Clamp(pageSize??20,1,100),pageNumber=Math.Max(1,page??1);
                var rows=await (from f in db.CustomerFeedbacks.AsNoTracking()
                        join c in db.Customers.AsNoTracking() on f.CustomerId equals c.Id
                        where (statusFilter==null||f.StatusCode==statusFilter)&&(typeFilter==null||f.FeedbackTypeCode==typeFilter)
                        orderby f.CreatedAtUtc descending
                        select new {f.Id,f.CompanyId,f.CustomerId,f.FeedbackTypeCode,f.Subject,f.Body,f.StatusCode,f.CreatedAtUtc,c.CustomerCode,c.DisplayName})
                    .Skip((pageNumber-1)*size).Take(size).ToListAsync(t);
                IReadOnlyList<CustomerFeedbackAdminView> items=rows.Select(x=>new CustomerFeedbackAdminView(x.Id,x.CustomerId,x.CustomerCode,x.DisplayName,x.FeedbackTypeCode,x.Subject,x.Body,x.StatusCode,x.CreatedAtUtc)).ToList();
                return Result.Success<IReadOnlyList<CustomerFeedbackAdminView>>(items);
            },ct);
            return Map(result);
        });
        edit.MapPost("/feedbacks/{feedbackId:guid}/status",async(Guid feedbackId,FeedbackStatusRequest r,ITransactionExecutor tx,CoreDbContext db,CancellationToken ct)=>
        {
            Result result=await tx.ExecuteAsync(async t=>
            {
                var feedback=await db.CustomerFeedbacks.SingleOrDefaultAsync(x=>x.Id==feedbackId,t);
                if(feedback is null)return Result.Failure(BARANG.Core.Domain.Files.FileErrors.NotFound);
                Result changed=feedback.SetStatus(r.StatusCode);
                return changed;
            },ct);
            return Map(result);
        });

        // Customer wallet administration: the ledger inbox for the backoffice.
        read.MapGet("/referrals",async(string? status,int? page,int? pageSize,IReferralAdministration referrals,CancellationToken ct)=>Map(await referrals.AdminListAsync(status,page??1,pageSize??20,ct)));
        read.MapGet("/wallet/topups",async(string? status,Guid? customerId,int? page,int? pageSize,IWalletAdministration wallet,CancellationToken ct)=>Map(await wallet.AdminListTopUpsAsync(status,customerId,page??1,pageSize??20,ct)));
        read.MapGet("/wallet/transactions",async(Guid? customerId,string? reason,int? page,int? pageSize,IWalletAdministration wallet,CancellationToken ct)=>Map(await wallet.AdminListTransactionsAsync(customerId,reason,page??1,pageSize??20,ct)));
        edit.MapPost("/{customerId:guid}/wallet/adjustments",async(Guid customerId,WalletAdjustRequest r,IWalletAdministration wallet,CancellationToken ct)=>Map(await wallet.AdminAdjustAsync(customerId,new WalletAdjustCommand(r.Direction,r.Amount,r.ReferenceNote),ct)));
        return endpoints;
    }
    private static CustomerResult<T>Map<T>(Result<T> result,bool created=false)=>new(result,created);
    private static CustomerResult<bool>Map(Result result)=>new(result.IsSuccess?Result.Success(true):Result.Failure<bool>(result.Error),false);
    private sealed class CustomerResult<T>(Result<T> result,bool created):IResult{public Task ExecuteAsync(HttpContext c){if(result.IsSuccess){c.Response.StatusCode=created?StatusCodes.Status201Created:StatusCodes.Status200OK;return c.Response.WriteAsJsonAsync(result.Value,c.RequestAborted);}ICorrelationContext correlation=c.RequestServices.GetRequiredService<ICorrelationContext>();ProblemDetails p=ApiProblemDetailsMapper.Create(result.Error,correlation.CorrelationId??c.TraceIdentifier,c.Request.Path);c.Response.StatusCode=p.Status!.Value;return c.Response.WriteAsJsonAsync(p,c.RequestAborted);}}
}

public sealed record CustomerSearchRequest(string? Search=null,CustomerType? CustomerType=null,CustomerStatus? Status=null,int PageNumber=1,int PageSize=20,string? SortBy=null,bool SortDescending=false);
public sealed record CustomerCreateRequest(string CustomerCode,CustomerType CustomerType,string? DisplayName=null,string? FirstName=null,string? LastName=null,string? LegalName=null,string? TradeName=null,string? NationalId=null,string? RegistrationNumber=null,string? RegistrationNo=null,string? EconomicCode=null,string? TaxId=null,string? PrimaryMobile=null,string? PrimaryPhone=null,string? PrimaryEmail=null,string? FatherName=null,DateOnly? BirthDate=null,string? GenderCode=null,string? BirthCertificateNumber=null,string? PlaceOfIssue=null,string? Occupation=null,DateOnly? EstablishmentDate=null,string? LegalEntityTypeCode=null,string? BusinessActivity=null,string? Website=null,string? ManagingDirector=null,string? CustomerGroupCode=null,string? PriceGroupCode=null,Guid? SalesRepresentativePersonId=null,string? Notes=null,Guid? CustomerGroupId=null,Guid? PriceGroupId=null,Guid? LegalEntityTypeId=null)
{public CustomerDataCommand Command()=>new(CustomerCode,CustomerType,DisplayName,FirstName,LastName,LegalName,TradeName,NationalId,RegistrationNumber??RegistrationNo,EconomicCode??TaxId,PrimaryMobile,PrimaryPhone,PrimaryEmail,FatherName,BirthDate,GenderCode,BirthCertificateNumber,PlaceOfIssue,Occupation,EstablishmentDate,LegalEntityTypeCode,BusinessActivity,Website,ManagingDirector,CustomerGroupCode,PriceGroupCode,SalesRepresentativePersonId,Notes,CustomerGroupId,PriceGroupId,LegalEntityTypeId);}
public sealed record CustomerUpdateRequest(string? DisplayName=null,string? FirstName=null,string? LastName=null,string? LegalName=null,string? TradeName=null,string? NationalId=null,string? RegistrationNumber=null,string? RegistrationNo=null,string? EconomicCode=null,string? TaxId=null,string? PrimaryMobile=null,string? PrimaryPhone=null,string? PrimaryEmail=null,string? FatherName=null,DateOnly? BirthDate=null,string? GenderCode=null,string? BirthCertificateNumber=null,string? PlaceOfIssue=null,string? Occupation=null,DateOnly? EstablishmentDate=null,string? LegalEntityTypeCode=null,string? BusinessActivity=null,string? Website=null,string? ManagingDirector=null,string? CustomerGroupCode=null,string? PriceGroupCode=null,Guid? SalesRepresentativePersonId=null,string? Notes=null,long Version=0,long ProfileVersion=0,Guid? CustomerGroupId=null,Guid? PriceGroupId=null,Guid? LegalEntityTypeId=null,CustomerStatus? Status=null)
{public CustomerUpdateCommand Command()=>new(DisplayName,FirstName,LastName,LegalName,TradeName,NationalId,RegistrationNumber??RegistrationNo,EconomicCode??TaxId,PrimaryMobile,PrimaryPhone,PrimaryEmail,FatherName,BirthDate,GenderCode,BirthCertificateNumber,PlaceOfIssue,Occupation,EstablishmentDate,LegalEntityTypeCode,BusinessActivity,Website,ManagingDirector,CustomerGroupCode,PriceGroupCode,SalesRepresentativePersonId,Notes,Version,ProfileVersion,CustomerGroupId,PriceGroupId,LegalEntityTypeId,Status);}
public sealed record ContactRequest(string ContactTypeCode,string? FirstName=null,string? LastName=null,string? Name=null,string? Mobile=null,string? Phone=null,string? Email=null,string? PositionTitle=null,string? JobTitle=null,string? Notes=null,bool IsPrimary=false,int DisplayOrder=0,long Version=0,Guid? ContactRoleId=null){public ContactCommand Command()=>new(ContactTypeCode,FirstName??Name??"",LastName,Mobile,Phone,Email,PositionTitle??JobTitle,Notes,IsPrimary,DisplayOrder,Version,ContactRoleId);}
public sealed record AddressRequest(string AddressTypeCode,string Title,string CountryCode,string? ProvinceCode=null,string? CityCode=null,string? Province=null,string? City=null,string? District=null,string AddressLine1="",string? AddressLine2=null,string? PostalCode=null,string? RecipientName=null,string? RecipientPhone=null,string? RecipientMobile=null,bool IsDefault=false,bool IsPrimary=false,int DisplayOrder=0,long Version=0,Guid? AddressTypeId=null,string? Email=null,string? Notes=null){public AddressCommand Command()=>new(AddressTypeCode,Title,CountryCode,ProvinceCode??Province,CityCode??City,District,AddressLine1,AddressLine2,PostalCode,RecipientName,RecipientPhone,RecipientMobile,IsDefault||IsPrimary,DisplayOrder,Version,AddressTypeId,Email,Notes);}
public sealed record FinancialRequest(decimal CreditLimit,string? PaymentTermCode=null,string? PaymentConditionCode=null,string? PreferredPaymentMethodCode=null,decimal DefaultDiscountPercent=0,CustomerFinancialStatus FinancialStatus=CustomerFinancialStatus.Normal,string? TaxStatusCode=null,string DefaultCurrencyCode="IRT",long Version=0,Guid? PaymentTermId=null,Guid? PaymentConditionId=null,Guid? PreferredPaymentMethodId=null,Guid? TaxStatusId=null){public FinancialProfileCommand Command()=>new(CreditLimit,PaymentTermCode,PaymentConditionCode,PreferredPaymentMethodCode,DefaultDiscountPercent,FinancialStatus,TaxStatusCode,DefaultCurrencyCode,Version,PaymentTermId,PaymentConditionId,PreferredPaymentMethodId,TaxStatusId);}
public sealed record BankAccountRequest(string AccountHolderName,string? BankCode=null,string? BankName=null,string? AccountNumber=null,string? Iban=null,string CurrencyCode="IRT",bool IsDefault=false,string? Notes=null,long Version=0,Guid? BankId=null,Guid? AccountTypeId=null,string? AccountTypeCode=null,string? AccountName=null,BankAccountOwnership OwnershipType=BankAccountOwnership.Individual,string? CardNumber=null,string? BranchCode=null){public BankAccountCommand Command()=>new(BankCode,BankName,AccountNumber,Iban,AccountHolderName,CurrencyCode,IsDefault,Notes,Version,BankId,AccountTypeId,AccountTypeCode,AccountName,OwnershipType,CardNumber,BranchCode);}
public sealed record CustomerStatusRequest(CustomerStatus Status,long Version);public sealed record ActivationRequest(bool IsActive,long Version);public sealed record VersionRequest(long Version);
public sealed record FeedbackStatusRequest(string StatusCode);
public sealed record WalletAdjustRequest(string Direction,decimal Amount,string? ReferenceNote);
public sealed record CustomerFeedbackAdminView(Guid Id,Guid CustomerId,string? CustomerCode,string? CustomerDisplayName,string FeedbackTypeCode,string Subject,string Body,string StatusCode,DateTimeOffset CreatedAtUtc);
