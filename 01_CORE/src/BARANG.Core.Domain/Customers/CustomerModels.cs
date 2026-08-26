using System.Net.Mail;
using System.Text.Json.Serialization;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Customers;

[JsonConverter(typeof(JsonStringEnumConverter<CustomerType>))]
public enum CustomerType { Person, Organization }

public static class CustomerErrors
{
    public static Error InvalidCode => Error.Validation("customer.invalid_code");
    public static Error InvalidData => Error.Validation("customer.invalid_data");
    public static Error InvalidEmail => Error.Validation("customer.invalid_email");
    public static Error InvalidPhone => Error.Validation("customer.invalid_phone");
    public static Error DuplicateCode => Error.Conflict("customer.duplicate_code");
    public static Error DuplicateIdentifier => Error.Conflict("customer.duplicate_identifier");
    public static Error PrimaryConflict => Error.Conflict("customer.primary_conflict");
    public static Error NotFound => Error.NotFound("customer.not_found");
    public static Error ContactNotFound => Error.NotFound("customer.contact_not_found");
    public static Error AddressNotFound => Error.NotFound("customer.address_not_found");
    public static Error Concurrency => Error.Concurrency("customer.concurrency_conflict");
}

public static class CustomerText
{
    public static string? Code(string? value) { string v=value?.Trim().ToUpperInvariant()??""; return v.Length is >=2 and <=50 && v.All(static c=>char.IsAsciiLetterOrDigit(c)||c is '-' or '_' or '.')?v:null; }
    public static string? Name(string? value,int max=200,bool optional=false) { string v=value?.Trim()??""; return v.Length==0&&optional?null:v.Length is >=1&&v.Length<=max?v:null; }
    public static string? Identifier(string? value) { string v=value?.Trim().ToUpperInvariant()??""; return v.Length==0?null:v.Length<=64?v:null; }
    public static string? Email(string? value)
    {
        string v=value?.Trim().ToLowerInvariant()??""; if(v.Length==0)return null; if(v.Length>320)return "";
        try { MailAddress parsed=new(v); return parsed.Address.Equals(v,StringComparison.OrdinalIgnoreCase)?v:""; } catch(FormatException){return "";}
    }
    public static string? Phone(string? value)
    {
        string raw=value?.Trim()??""; if(raw.Length==0)return null; bool plus=raw.StartsWith('+'); string digits=new(raw.Where(char.IsAsciiDigit).ToArray());
        if(digits.Length is <7 or >15 || raw.Skip(plus?1:0).Any(static c=>!char.IsAsciiDigit(c)&&c is not ' ' and not '-' and not '(' and not ')')) return "";
        return plus?"+"+digits:digits;
    }
    public static string? Category(string? value) { string v=value?.Trim().ToLowerInvariant()??""; return v.Length is >=2 and <=50&&v[0] is >= 'a' and <= 'z'&&v.All(static c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or '-')?v:null; }
}

public sealed class Customer
{
    private Customer() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public string CustomerCode { get; private set; }=""; public CustomerType CustomerType { get; private set; }
    public string DisplayName { get; private set; }=""; public string? FirstName { get; private set; } public string? LastName { get; private set; } public string? LegalName { get; private set; }
    public string? NationalId { get; private set; } public string? RegistrationNo { get; private set; } public string? TaxId { get; private set; }
    public string? PrimaryMobile { get; private set; } public string? PrimaryPhone { get; private set; } public string? PrimaryEmail { get; private set; } public string? Notes { get; private set; }
    public bool IsActive { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<Customer> Create(Guid id,Guid companyId,string code,CustomerType type,string? displayName,string? firstName,string? lastName,string? legalName,string? nationalId,string? registrationNo,string? taxId,string? mobile,string? phone,string? email,string? notes,DateTimeOffset now)
    {
        string? normalizedCode=CustomerText.Code(code); if(id==Guid.Empty||companyId==Guid.Empty||normalizedCode is null)return Result.Failure<Customer>(CustomerErrors.InvalidCode);
        Result<CustomerData> data=Validate(type,displayName,firstName,lastName,legalName,nationalId,registrationNo,taxId,mobile,phone,email,notes); if(data.IsFailure)return Result.Failure<Customer>(data.Error); CustomerData d=data.Value;
        return Result.Success(new Customer{Id=id,CompanyId=companyId,CustomerCode=normalizedCode,CustomerType=type,DisplayName=d.DisplayName,FirstName=d.FirstName,LastName=d.LastName,LegalName=d.LegalName,NationalId=d.NationalId,RegistrationNo=d.RegistrationNo,TaxId=d.TaxId,PrimaryMobile=d.Mobile,PrimaryPhone=d.Phone,PrimaryEmail=d.Email,Notes=d.Notes,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1});
    }
    public Result Update(string? displayName,string? firstName,string? lastName,string? legalName,string? nationalId,string? registrationNo,string? taxId,string? mobile,string? phone,string? email,string? notes,DateTimeOffset now)
    { Result<CustomerData> data=Validate(CustomerType,displayName,firstName,lastName,legalName,nationalId,registrationNo,taxId,mobile,phone,email,notes); if(data.IsFailure)return Result.Failure(data.Error); CustomerData d=data.Value; DisplayName=d.DisplayName;FirstName=d.FirstName;LastName=d.LastName;LegalName=d.LegalName;NationalId=d.NationalId;RegistrationNo=d.RegistrationNo;TaxId=d.TaxId;PrimaryMobile=d.Mobile;PrimaryPhone=d.Phone;PrimaryEmail=d.Email;Notes=d.Notes;Touch(now);return Result.Success(); }
    public void SetActive(bool active,DateTimeOffset now){if(IsActive!=active){IsActive=active;Touch(now);}}
    private static Result<CustomerData> Validate(CustomerType type,string? display,string? first,string? last,string? legal,string? national,string? registration,string? tax,string? mobile,string? phone,string? email,string? notes)
    {
        if(!Enum.IsDefined(type))return Result.Failure<CustomerData>(CustomerErrors.InvalidData); string? f=CustomerText.Name(first,100,true),l=CustomerText.Name(last,100,true),legalName=CustomerText.Name(legal,200,true);
        if((!string.IsNullOrWhiteSpace(first)&&f is null)||(!string.IsNullOrWhiteSpace(last)&&l is null)||(!string.IsNullOrWhiteSpace(legal)&&legalName is null))return Result.Failure<CustomerData>(CustomerErrors.InvalidData);
        if(type==CustomerType.Person&&f is null&&l is null||type==CustomerType.Organization&&legalName is null)return Result.Failure<CustomerData>(CustomerErrors.InvalidData);
        string derived=type==CustomerType.Person?string.Join(' ',new[]{f,l}.Where(x=>x is not null)):legalName!; string? name=CustomerText.Name(string.IsNullOrWhiteSpace(display)?derived:display); if(name is null)return Result.Failure<CustomerData>(CustomerErrors.InvalidData);
        string? e=CustomerText.Email(email); if(e=="")return Result.Failure<CustomerData>(CustomerErrors.InvalidEmail); string? m=CustomerText.Phone(mobile),p=CustomerText.Phone(phone); if(m==""||p=="")return Result.Failure<CustomerData>(CustomerErrors.InvalidPhone);
        string? n=CustomerText.Identifier(national),r=CustomerText.Identifier(registration),t=CustomerText.Identifier(tax); if((national?.Trim().Length>0&&n is null)||(registration?.Trim().Length>0&&r is null)||(tax?.Trim().Length>0&&t is null)||notes?.Trim().Length>2000)return Result.Failure<CustomerData>(CustomerErrors.InvalidData);
        return Result.Success(new CustomerData(name,f,l,legalName,n,r,t,m,p,e,CustomerText.Name(notes,2000,true)));
    }
    private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
    private sealed record CustomerData(string DisplayName,string? FirstName,string? LastName,string? LegalName,string? NationalId,string? RegistrationNo,string? TaxId,string? Mobile,string? Phone,string? Email,string? Notes);
}

public sealed class CustomerContact
{
    private CustomerContact() { }
    public Guid Id{get;private set;} public Guid CustomerId{get;private set;} public Guid CompanyId{get;private set;} public string ContactTypeCode{get;private set;}=""; public string? Name{get;private set;} public string? Mobile{get;private set;} public string? Phone{get;private set;} public string? Email{get;private set;} public string? JobTitle{get;private set;} public bool IsPrimary{get;private set;} public bool IsActive{get;private set;} public int DisplayOrder{get;private set;} public DateTimeOffset CreatedAtUtc{get;private set;} public DateTimeOffset UpdatedAtUtc{get;private set;} public long Version{get;private set;}
    public static Result<CustomerContact>Create(Guid id,Guid customerId,Guid companyId,string type,string? name,string? mobile,string? phone,string? email,string? jobTitle,bool primary,int order,DateTimeOffset now){if(id==Guid.Empty||customerId==Guid.Empty||companyId==Guid.Empty)return Result.Failure<CustomerContact>(CustomerErrors.InvalidData); CustomerContact x=new(){Id=id,CustomerId=customerId,CompanyId=companyId,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1,IsActive=true};Result r=x.Apply(type,name,mobile,phone,email,jobTitle,primary,order,now,false);return r.IsFailure?Result.Failure<CustomerContact>(r.Error):Result.Success(x);}
    public Result Update(string type,string? name,string? mobile,string? phone,string? email,string? jobTitle,bool primary,int order,DateTimeOffset now)=>Apply(type,name,mobile,phone,email,jobTitle,primary,order,now,true);
    public void SetActive(bool active,DateTimeOffset now){if(IsActive!=active){IsActive=active;if(!active)IsPrimary=false;Touch(now);}}
    private Result Apply(string type,string? name,string? mobile,string? phone,string? email,string? jobTitle,bool primary,int order,DateTimeOffset now,bool touch){string? category=CustomerText.Category(type),n=CustomerText.Name(name,200,true),j=CustomerText.Name(jobTitle,150,true),m=CustomerText.Phone(mobile),p=CustomerText.Phone(phone),e=CustomerText.Email(email);if(category is null||e==""||m==""||p==""||(primary&&!IsActive)||(!string.IsNullOrWhiteSpace(name)&&n is null)||(!string.IsNullOrWhiteSpace(jobTitle)&&j is null)||(n is null&&m is null&&p is null&&e is null))return Result.Failure(CustomerErrors.InvalidData);ContactTypeCode=category;Name=n;Mobile=m;Phone=p;Email=e;JobTitle=j;IsPrimary=primary;DisplayOrder=order;if(touch)Touch(now);return Result.Success();}
    private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
}

public sealed class CustomerAddress
{
    private CustomerAddress() { }
    public Guid Id{get;private set;} public Guid CustomerId{get;private set;} public Guid CompanyId{get;private set;} public string AddressTypeCode{get;private set;}=""; public string Title{get;private set;}=""; public string CountryCode{get;private set;}=""; public string? Province{get;private set;} public string? City{get;private set;} public string? District{get;private set;} public string AddressLine1{get;private set;}=""; public string? AddressLine2{get;private set;} public string? PostalCode{get;private set;} public string? RecipientName{get;private set;} public string? RecipientPhone{get;private set;} public bool IsPrimary{get;private set;} public bool IsActive{get;private set;} public int DisplayOrder{get;private set;} public DateTimeOffset CreatedAtUtc{get;private set;} public DateTimeOffset UpdatedAtUtc{get;private set;} public long Version{get;private set;}
    public static Result<CustomerAddress>Create(Guid id,Guid customerId,Guid companyId,string type,string title,string country,string? province,string? city,string? district,string line1,string? line2,string? postal,string? recipient,string? recipientPhone,bool primary,int order,DateTimeOffset now){if(id==Guid.Empty||customerId==Guid.Empty||companyId==Guid.Empty)return Result.Failure<CustomerAddress>(CustomerErrors.InvalidData);CustomerAddress x=new(){Id=id,CustomerId=customerId,CompanyId=companyId,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1,IsActive=true};Result r=x.Apply(type,title,country,province,city,district,line1,line2,postal,recipient,recipientPhone,primary,order,now,false);return r.IsFailure?Result.Failure<CustomerAddress>(r.Error):Result.Success(x);}
    public Result Update(string type,string title,string country,string? province,string? city,string? district,string line1,string? line2,string? postal,string? recipient,string? recipientPhone,bool primary,int order,DateTimeOffset now)=>Apply(type,title,country,province,city,district,line1,line2,postal,recipient,recipientPhone,primary,order,now,true);
    public void SetActive(bool active,DateTimeOffset now){if(IsActive!=active){IsActive=active;if(!active)IsPrimary=false;Touch(now);}}
    private Result Apply(string type,string title,string country,string? province,string? city,string? district,string line1,string? line2,string? postal,string? recipient,string? recipientPhone,bool primary,int order,DateTimeOffset now,bool touch){string? category=CustomerText.Category(type),t=CustomerText.Name(title,100),l1=CustomerText.Name(line1,500),p=CustomerText.Name(province,150,true),c=CustomerText.Name(city,150,true),d=CustomerText.Name(district,150,true),l2=CustomerText.Name(line2,500,true),pc=CustomerText.Name(postal,32,true),rn=CustomerText.Name(recipient,200,true),rp=CustomerText.Phone(recipientPhone);string cc=country?.Trim().ToUpperInvariant()??"";if(category is null||t is null||l1 is null||cc.Length!=2||!cc.All(char.IsAsciiLetter)||rp==""||(primary&&!IsActive)||(!string.IsNullOrWhiteSpace(postal)&&pc is null))return Result.Failure(CustomerErrors.InvalidData);AddressTypeCode=category;Title=t;CountryCode=cc;Province=p;City=c;District=d;AddressLine1=l1;AddressLine2=l2;PostalCode=pc;RecipientName=rn;RecipientPhone=rp;IsPrimary=primary;DisplayOrder=order;if(touch)Touch(now);return Result.Success();}
    private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
}
