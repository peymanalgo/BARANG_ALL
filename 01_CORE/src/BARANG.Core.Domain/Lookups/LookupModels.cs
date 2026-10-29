using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Lookups;

public enum LookupScope { Global, Company }

public static class LookupErrors
{
    public static Error InvalidCode => Error.Validation("lookup.invalid_code");
    public static Error InvalidName => Error.Validation("lookup.invalid_name");
    public static Error InvalidScope => Error.Validation("lookup.invalid_scope");
    public static Error UnsupportedCulture => Error.Validation("lookup.unsupported_culture");
    public static Error DuplicateCode => Error.Conflict("lookup.duplicate_code");
    public static Error DuplicateTranslation => Error.Conflict("lookup.duplicate_translation");
    public static Error TypeNotFound => Error.NotFound("lookup.type_not_found");
    public static Error ValueNotFound => Error.NotFound("lookup.value_not_found");
    public static Error SystemRecordProtected => Error.BusinessRule("lookup.system_record_protected");
    public static Error Concurrency => Error.Concurrency("lookup.concurrency_conflict");
}
public static class LookupText
{
    public const string DefaultCulture = "fa";

    // Single-translation picker for display: requested culture first, then the
    // default (fa), then en and ar. Never mixes translations into one string.
    public static string PickTranslation(System.Collections.Generic.IEnumerable<(string Culture, string Name)> rows, string? culture)
    {
        List<(string Culture, string Name)> list = rows.ToList();
        string wanted = NormalizeCulture(culture) ?? DefaultCulture;
        return list.FirstOrDefault(x => x.Culture == wanted).Name
            ?? list.FirstOrDefault(x => x.Culture == DefaultCulture).Name
            ?? list.FirstOrDefault(x => x.Culture == "en").Name
            ?? list.FirstOrDefault(x => x.Culture == "ar").Name
            ?? list.FirstOrDefault().Name
            ?? "";
    }
    public static IReadOnlySet<string> SupportedCultures { get; } = new HashSet<string>(["fa", "en", "ar"], StringComparer.Ordinal);
    public static string? NormalizeCode(string? value)
    {
        string code = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return code.Length is >= 2 and <= 100 && code[0] is >= 'a' and <= 'z' &&
            code.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-') ? code : null;
    }
    public static string? NormalizeValueCode(string? value)
    {
        string code = value?.Trim().ToLowerInvariant() ?? "";
        return code.Length is >= 1 and <= 100 && code.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-') ? code : null;
    }
    public static string? NormalizeName(string? value, int max = 200)
    {
        string name = value?.Trim() ?? string.Empty;
        return name.Length is >= 1 && name.Length <= max ? name : null;
    }
    public static string? NormalizeCulture(string? value)
    {
        string culture = value?.Trim().ToLowerInvariant().Split('-', 2)[0] ?? string.Empty;
        return SupportedCultures.Contains(culture) ? culture : null;
    }
}

public sealed class LookupType
{
    private LookupType() { }
    public Guid Id { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public LookupScope Scope { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public bool AllowCustomValues { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<LookupType> Create(Guid id, Guid? companyId, string code, string displayName, LookupScope scope, bool isSystem, bool allowCustomValues, DateTimeOffset now)
    {
        string? normalizedCode = LookupText.NormalizeCode(code);
        if (id == Guid.Empty || normalizedCode is null) return Result.Failure<LookupType>(LookupErrors.InvalidCode);
        string? name = LookupText.NormalizeName(displayName);
        if (name is null) return Result.Failure<LookupType>(LookupErrors.InvalidName);
        if ((scope == LookupScope.Global) != (companyId is null)) return Result.Failure<LookupType>(LookupErrors.InvalidScope);
        return Result.Success(new LookupType { Id = id, CompanyId = companyId, Code = normalizedCode, DisplayName = name, Scope = scope, IsSystem = isSystem, IsActive = true, AllowCustomValues = allowCustomValues, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result Update(string displayName, bool allowCustomValues, DateTimeOffset now)
    {
        if (IsSystem) return Result.Failure(LookupErrors.SystemRecordProtected);
        string? name = LookupText.NormalizeName(displayName);
        if (name is null) return Result.Failure(LookupErrors.InvalidName);
        DisplayName = name; AllowCustomValues = allowCustomValues; Touch(now); return Result.Success();
    }
    public Result SetActive(bool active, DateTimeOffset now)
    {
        if (IsSystem) return Result.Failure(LookupErrors.SystemRecordProtected);
        if (IsActive != active) { IsActive = active; Touch(now); }
        return Result.Success();
    }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class LookupValue
{
    private LookupValue() { }
    public Guid Id { get; private set; }
    public Guid LookupTypeId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<LookupValueTranslation> Translations => _translations;
    private readonly List<LookupValueTranslation> _translations = [];

    public static Result<LookupValue> Create(Guid id, LookupType type, string code, int displayOrder, bool isSystem, DateTimeOffset now)
    {
        string? normalized = LookupText.NormalizeValueCode(code);
        if (id == Guid.Empty || normalized is null) return Result.Failure<LookupValue>(LookupErrors.InvalidCode);
        if (!type.IsActive || (type.Scope == LookupScope.Company && type.CompanyId is null)) return Result.Failure<LookupValue>(LookupErrors.InvalidScope);
        return Result.Success(new LookupValue { Id = id, LookupTypeId = type.Id, CompanyId = type.CompanyId, Code = normalized, DisplayOrder = displayOrder, IsSystem = isSystem, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result UpdateOrder(int order, DateTimeOffset now) { if (IsSystem) return Result.Failure(LookupErrors.SystemRecordProtected); DisplayOrder = order; Touch(now); return Result.Success(); }
    public Result SetActive(bool active, DateTimeOffset now) { if (IsSystem) return Result.Failure(LookupErrors.SystemRecordProtected); if (IsActive != active) { IsActive = active; Touch(now); } return Result.Success(); }
    public Result SetParent(Guid? parentId, DateTimeOffset now) { if (parentId == Id) return Result.Failure(Error.BusinessRule("artwork.category.cycle")); if (ParentId != parentId) { ParentId = parentId; Touch(now); } return Result.Success(); }
    public Result<LookupValueTranslation> SetTranslation(string culture, string displayName, string? description, DateTimeOffset now)
    {
        string? normalizedCulture = LookupText.NormalizeCulture(culture);
        if (normalizedCulture is null) return Result.Failure<LookupValueTranslation>(LookupErrors.UnsupportedCulture);
        string? name = LookupText.NormalizeName(displayName);
        if (name is null || (description is not null && LookupText.NormalizeName(description, 1000) is null)) return Result.Failure<LookupValueTranslation>(LookupErrors.InvalidName);
        LookupValueTranslation? existing = _translations.SingleOrDefault(x => x.Culture == normalizedCulture);
        if (existing is null) { existing = LookupValueTranslation.Create(Guid.NewGuid(), Id, CompanyId, normalizedCulture, name, description?.Trim(), now); _translations.Add(existing); }
        else existing.Update(name, description?.Trim(), now);
        Touch(now); return Result.Success(existing);
    }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class LookupValueTranslation
{
    private LookupValueTranslation() { }
    public Guid Id { get; private set; }
    public Guid LookupValueId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Culture { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    internal static LookupValueTranslation Create(Guid id, Guid valueId, Guid? companyId, string culture, string name, string? description, DateTimeOffset now) => new() { Id = id, LookupValueId = valueId, CompanyId = companyId, Culture = culture, DisplayName = name, Description = description, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
    internal void Update(string name, string? description, DateTimeOffset now) { DisplayName = name; Description = description; UpdatedAtUtc = now; Version++; }
}

