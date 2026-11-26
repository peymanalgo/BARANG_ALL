using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class Company
{
    private Company() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<Company> Create(Guid id, string code, string displayName, DateTimeOffset utcNow)
    {
        string? normalizedCode = OrganizationText.NormalizeCode(code);
        if (id == Guid.Empty || normalizedCode is null)
        {
            return Result.Failure<Company>(OrganizationErrors.InvalidCode);
        }

        string? normalizedName = OrganizationText.NormalizeName(displayName);
        if (normalizedName is null)
        {
            return Result.Failure<Company>(OrganizationErrors.InvalidName);
        }

        return Result.Success(new Company
        {
            Id = id, Code = normalizedCode, DisplayName = normalizedName, IsActive = true,
            CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow, Version = 1
        });
    }

    public Result UpdateDisplayName(string displayName, DateTimeOffset utcNow)
    {
        string? normalizedName = OrganizationText.NormalizeName(displayName);
        if (normalizedName is null) return Result.Failure(OrganizationErrors.InvalidName);
        DisplayName = normalizedName;
        Touch(utcNow);
        return Result.Success();
    }

    public void Activate(DateTimeOffset utcNow) { if (!IsActive) { IsActive = true; Touch(utcNow); } }
    public void Deactivate(DateTimeOffset utcNow) { if (IsActive) { IsActive = false; Touch(utcNow); } }
    private void Touch(DateTimeOffset utcNow) { UpdatedAtUtc = utcNow; Version++; }
}
