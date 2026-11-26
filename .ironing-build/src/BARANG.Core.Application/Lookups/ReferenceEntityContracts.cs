using BARANG.Core.Domain.Common;
namespace BARANG.Core.Application.Lookups;

public sealed record ReferenceEntityCommand(string Code, int DisplayOrder, IReadOnlyDictionary<string, string> Translations,
    IReadOnlyDictionary<string, string>? Descriptions, bool IsActive = true, long Version = 0,
    string? CategoryCode = null, string Severity = "Minor", bool IsBlocking = false);
public sealed record ReferenceEntityValueView(Guid Id, string Code, int DisplayOrder, bool IsActive, bool IsSystem,
    long Version, IReadOnlyList<LookupTranslationView> Translations, string? CategoryCode = null,
    string Severity = "Minor", bool IsBlocking = false);
public interface IReferenceEntityAdministration
{
    Task<Result<IReadOnlyList<ReferenceEntityValueView>>> ListAsync(string type, CancellationToken ct);
    Task<Result<ReferenceEntityValueView>> SaveAsync(string type, Guid? id, ReferenceEntityCommand command, CancellationToken ct);
    Task<Result<ReferenceEntityValueView>> ActivateAsync(string type, Guid id, bool active, long version, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(string type, Guid id, long version, CancellationToken ct);
}
