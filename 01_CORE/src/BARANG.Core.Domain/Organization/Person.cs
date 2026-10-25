using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class Person
{
    private Person() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string PersonnelCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? NationalId { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<Person> Create(Guid id, Guid companyId, string personnelCode, string firstName, string lastName, string? nationalId, DateOnly? birthDate, string? email, string? phone, string? address, string? notes, DateTimeOffset now)
    {
        string code = personnelCode.Trim().ToUpperInvariant(); string first = firstName.Trim(); string last = lastName.Trim();
        if (id == Guid.Empty || companyId == Guid.Empty || code.Length is < 1 or > 64 || first.Length is < 1 or > 100 || last.Length is < 1 or > 100) return Result.Failure<Person>(OrganizationErrors.InvalidPerson);
        return Result.Success(new Person { Id=id, CompanyId=companyId, PersonnelCode=code, FirstName=first, LastName=last, DisplayName=$"{first} {last}", NationalId=Clean(nationalId), BirthDate=birthDate, Email=Clean(email)?.ToLowerInvariant(), Phone=Clean(phone), Address=Clean(address), Notes=Clean(notes), IsActive=true, CreatedAtUtc=now, UpdatedAtUtc=now, Version=1 });
    }
    public Result Update(string firstName,string lastName,string? nationalId,DateOnly? birthDate,string? email,string? phone,string? address,string? notes,DateTimeOffset now)
    { string first=firstName.Trim(); string last=lastName.Trim(); if(first.Length is <1 or >100||last.Length is <1 or >100)return Result.Failure(OrganizationErrors.InvalidPerson); FirstName=first; LastName=last; DisplayName=$"{first} {last}"; NationalId=Clean(nationalId); BirthDate=birthDate; Email=Clean(email)?.ToLowerInvariant(); Phone=Clean(phone); Address=Clean(address); Notes=Clean(notes); Touch(now); return Result.Success(); }
    public void SetActive(bool active,DateTimeOffset now){if(IsActive!=active){IsActive=active;Touch(now);}}
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
}
