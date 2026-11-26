using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class CompanySetting
{
    private CompanySetting() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public CompanySettingValueType ValueType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<CompanySetting> Create(Guid id, Guid companyId, string key, string value, CompanySettingValueType valueType, DateTimeOffset utcNow)
    {
        string? normalizedKey = OrganizationText.NormalizeCode(key, 100);
        if (id == Guid.Empty || companyId == Guid.Empty || normalizedKey is null || value is null || value.Length > 2000 || !IsValid(value, valueType))
            return Result.Failure<CompanySetting>(OrganizationErrors.InvalidSetting);
        return Result.Success(new CompanySetting { Id = id, CompanyId = companyId, Key = normalizedKey, Value = value, ValueType = valueType, IsActive = true, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow, Version = 1 });
    }

    public Result Update(string value, CompanySettingValueType valueType, DateTimeOffset utcNow)
    {
        if (value is null || value.Length > 2000 || !IsValid(value, valueType)) return Result.Failure(OrganizationErrors.InvalidSetting);
        Value = value; ValueType = valueType; UpdatedAtUtc = utcNow; Version++; return Result.Success();
    }
    public void Deactivate(DateTimeOffset utcNow) { if (IsActive) { IsActive = false; UpdatedAtUtc = utcNow; Version++; } }
    private static bool IsValid(string value, CompanySettingValueType type) => type switch
    {
        CompanySettingValueType.Text => true,
        CompanySettingValueType.Boolean => bool.TryParse(value, out _),
        CompanySettingValueType.WholeNumber => long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _),
        CompanySettingValueType.DecimalNumber => decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _),
        _ => false
    };
}

public enum CompanySettingValueType { Text, Boolean, WholeNumber, DecimalNumber }
