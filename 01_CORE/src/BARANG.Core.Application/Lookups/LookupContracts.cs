using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Lookups;

namespace BARANG.Core.Application.Lookups;

public sealed record LookupTranslationView(string Culture, string DisplayName, string? Description, long Version);
public sealed record LookupTypeView(Guid Id, Guid? CompanyId, string Code, string DisplayName, LookupScope Scope, bool IsSystem, bool IsActive, bool AllowCustomValues, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record LookupValueView(Guid Id, Guid LookupTypeId, Guid? CompanyId, string Code, int DisplayOrder, bool IsSystem, bool IsActive, IReadOnlyList<LookupTranslationView> Translations, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record LookupOptionView(Guid Id, string Code, string DisplayName, int DisplayOrder, bool IsActive);
public sealed record CreateLookupTypeCommand(string Code, string DisplayName, bool AllowCustomValues);
public sealed record CreateLookupValueCommand(string Code, int DisplayOrder, IReadOnlyDictionary<string, string> Translations);

public interface ILookupAdministration
{
    Task<Result<IReadOnlyList<LookupTypeView>>> ListTypesAsync(bool activeOnly, CancellationToken ct);
    Task<Result<LookupTypeView>> GetTypeAsync(Guid id, CancellationToken ct);
    Task<Result<LookupTypeView>> GetTypeByCodeAsync(string code, CancellationToken ct);
    Task<Result<LookupTypeView>> CreateTypeAsync(CreateLookupTypeCommand command, CancellationToken ct);
    Task<Result<LookupTypeView>> UpdateTypeAsync(Guid id, string displayName, bool allowCustomValues, long version, CancellationToken ct);
    Task<Result<LookupTypeView>> SetTypeActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<IReadOnlyList<LookupValueView>>> ListValuesAsync(string typeCode, bool activeOnly, CancellationToken ct);
    Task<Result<LookupValueView>> GetValueAsync(Guid id, CancellationToken ct);
    Task<Result<LookupValueView>> GetValueByCodeAsync(string typeCode, string valueCode, CancellationToken ct);
    Task<Result<LookupValueView>> CreateValueAsync(string typeCode, CreateLookupValueCommand command, CancellationToken ct);
    Task<Result<LookupValueView>> UpdateValueAsync(Guid id, int displayOrder, long version, CancellationToken ct);
    Task<Result<LookupValueView>> SetValueActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<LookupValueView>> SetTranslationAsync(Guid id, string culture, string displayName, string? description, long version, CancellationToken ct);
    Task<Result<IReadOnlyList<LookupOptionView>>> GetOptionsAsync(string typeCode, string culture, bool activeOnly, CancellationToken ct);
}

public interface ILookupStore
{
    void Add(LookupType type);
    void Add(LookupValue value);
    Task<LookupType?> FindTypeAsync(Guid id, CancellationToken ct);
    Task<LookupType?> FindTypeByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<LookupType>> ListTypesAsync(bool activeOnly, CancellationToken ct);
    Task<bool> TypeCodeExistsAsync(string code, CancellationToken ct);
    Task<LookupValue?> FindValueAsync(Guid id, CancellationToken ct);
    Task<LookupValue?> FindValueByCodeAsync(Guid typeId, string code, CancellationToken ct);
    Task<IReadOnlyList<LookupValue>> ListValuesAsync(Guid typeId, bool activeOnly, CancellationToken ct);
    Task<bool> ValueCodeExistsAsync(Guid typeId, string code, CancellationToken ct);
}

// Cache decorators can later wrap this stable, company/culture-aware read contract.
public interface ILookupOptionReader
{
    Task<Result<IReadOnlyList<LookupOptionView>>> GetOptionsAsync(string typeCode, string culture, bool activeOnly, CancellationToken ct);
}
