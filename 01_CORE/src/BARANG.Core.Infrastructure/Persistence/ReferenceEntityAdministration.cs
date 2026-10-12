using System.Text.Json;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Lookups;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Files;
using BARANG.Core.Domain.Lookups;
using BARANG.Core.Domain.Quality;
using Microsoft.EntityFrameworkCore;
namespace BARANG.Core.Infrastructure.Persistence;

public sealed class ReferenceEntityAdministration(CoreDbContext db, ITransactionExecutor tx, ICompanyContext company,
    ICurrentActor actor, IAuditWriter audit, TimeProvider time) : IReferenceEntityAdministration
{
    private static readonly string[] Cultures = ["fa", "ar", "en"];
    public Task<Result<IReadOnlyList<ReferenceEntityValueView>>> ListAsync(string type, CancellationToken ct) =>
        tx.ExecuteAsync(async token =>
        {
            company.RequireCompanyId();
            if (type == "artwork.tag")
                return Result.Success<IReadOnlyList<ReferenceEntityValueView>>((await db.ArtworkTags.OrderBy(x => x.Code).ToListAsync(token)).Select(View).ToArray());
            if (type == "defect.type")
                return Result.Success<IReadOnlyList<ReferenceEntityValueView>>((await db.DefectTypes.OrderBy(x => x.Code).ToListAsync(token)).Select(View).ToArray());
            return Result.Failure<IReadOnlyList<ReferenceEntityValueView>>(LookupErrors.TypeNotFound);
        }, ct);

    public Task<Result<ReferenceEntityValueView>> SaveAsync(string type, Guid? id, ReferenceEntityCommand command, CancellationToken ct) =>
        tx.ExecuteAsync(async token =>
        {
            Guid scope = company.RequireCompanyId();
            int max = type == "artwork.tag" ? 100 : 200;
            if (command.Translations is null || command.DisplayOrder < 0 ||
                Cultures.Any(c => !command.Translations.TryGetValue(c, out string? name) || string.IsNullOrWhiteSpace(name) || name.Trim().Length > max || name.Any(char.IsControl)) ||
                command.Translations.Keys.Any(c => !Cultures.Contains(c)) ||
                command.Descriptions?.Any(x => !Cultures.Contains(x.Key) || x.Value.Length > 1000) == true)
                return Result.Failure<ReferenceEntityValueView>(LookupErrors.InvalidName);
            if (type == "artwork.tag")
            {
                ArtworkTag? row = id is null ? null : await db.ArtworkTags.SingleOrDefaultAsync(x => x.Id == id, token);
                if (id is not null && row is null) return Result.Failure<ReferenceEntityValueView>(LookupErrors.ValueNotFound);
                if (row is not null && row.Version != command.Version) return Result.Failure<ReferenceEntityValueView>(LookupErrors.Concurrency);
                if (row is null)
                {
                    var made = ArtworkTag.Create(Guid.NewGuid(), scope, command.Code, command.Translations["fa"], time.GetUtcNow());
                    if (made.IsFailure) return Result.Failure<ReferenceEntityValueView>(made.Error);
                    row = made.Value; db.ArtworkTags.Add(row);
                }
                else
                {
                    Result updated = row.Update(command.Translations["fa"], command.IsActive, time.GetUtcNow());
                    if (updated.IsFailure) return Result.Failure<ReferenceEntityValueView>(updated.Error);
                }
                Metadata(row, command); Log(type, row.Id, id is null ? "created" : "updated");
                return Result.Success(View(row));
            }
            if (type == "defect.type")
            {
                DefectType? row = id is null ? null : await db.DefectTypes.SingleOrDefaultAsync(x => x.Id == id, token);
                if (id is not null && row is null) return Result.Failure<ReferenceEntityValueView>(LookupErrors.ValueNotFound);
                if (row is not null && row.Version != command.Version) return Result.Failure<ReferenceEntityValueView>(LookupErrors.Concurrency);
                if (!Enum.TryParse(command.Severity, out DefectSeverity severity) || !Enum.IsDefined(severity))
                    return Result.Failure<ReferenceEntityValueView>(QualityErrors.Invalid);
                string category = command.CategoryCode?.Trim().ToLowerInvariant() ?? "";
                bool historical = row is not null && string.Equals(row.CategoryCode, category, StringComparison.OrdinalIgnoreCase);
                if (!await (from value in db.LookupValues join catalog in db.LookupTypes on value.LookupTypeId equals catalog.Id
                    where catalog.Code == "defect.category" && value.Code == category && (historical || value.IsActive && catalog.IsActive)
                    select value.Id).AnyAsync(token))
                    return Result.Failure<ReferenceEntityValueView>(QualityErrors.Invalid);
                if (row is null)
                {
                    var made = DefectType.Create(scope, command.Code, command.Translations["fa"], category, severity, command.IsBlocking, time.GetUtcNow());
                    if (made.IsFailure) return Result.Failure<ReferenceEntityValueView>(made.Error);
                    row = made.Value; db.DefectTypes.Add(row);
                }
                else
                {
                    Result updated = row.EditReference(command.Translations["fa"], category, severity, command.IsBlocking, command.IsActive, command.Version, time.GetUtcNow());
                    if (updated.IsFailure) return Result.Failure<ReferenceEntityValueView>(updated.Error);
                }
                Metadata(row, command); Log(type, row.Id, id is null ? "created" : "updated");
                return Result.Success(View(row));
            }
            return Result.Failure<ReferenceEntityValueView>(LookupErrors.TypeNotFound);
        }, ct);

    public Task<Result<ReferenceEntityValueView>> ActivateAsync(string type, Guid id, bool active, long version, CancellationToken ct) =>
        tx.ExecuteAsync(async token =>
        {
            company.RequireCompanyId();
            if (type == "artwork.tag")
            {
                ArtworkTag? row = await db.ArtworkTags.SingleOrDefaultAsync(x => x.Id == id, token);
                if (row is null) return Result.Failure<ReferenceEntityValueView>(LookupErrors.ValueNotFound);
                if (row.Version != version) return Result.Failure<ReferenceEntityValueView>(LookupErrors.Concurrency);
                Result changed = row.Update(row.DisplayName, active, time.GetUtcNow());
                if (changed.IsFailure) return Result.Failure<ReferenceEntityValueView>(changed.Error);
                Log(type, id, active ? "activated" : "deactivated"); return Result.Success(View(row));
            }
            if (type == "defect.type")
            {
                DefectType? row = await db.DefectTypes.SingleOrDefaultAsync(x => x.Id == id, token);
                if (row is null) return Result.Failure<ReferenceEntityValueView>(LookupErrors.ValueNotFound);
                Result changed = row.SetActive(active, version, time.GetUtcNow());
                if (changed.IsFailure) return Result.Failure<ReferenceEntityValueView>(changed.Error);
                Log(type, id, active ? "activated" : "deactivated"); return Result.Success(View(row));
            }
            return Result.Failure<ReferenceEntityValueView>(LookupErrors.TypeNotFound);
        }, ct);

    public Task<Result<bool>> DeleteAsync(string type, Guid id, long version, CancellationToken ct) =>
        tx.ExecuteAsync(async token =>
        {
            company.RequireCompanyId();
            if (type == "artwork.tag")
            {
                ArtworkTag? row = await db.ArtworkTags.SingleOrDefaultAsync(x => x.Id == id, token);
                if (row is null) return Result.Failure<bool>(LookupErrors.ValueNotFound);
                if (row.Version != version) return Result.Failure<bool>(LookupErrors.Concurrency);
                if (await db.ArtworkTagAssignments.AnyAsync(x => x.TagId == id, token)) return Result.Failure<bool>(Error.BusinessRule("lookup.value_referenced"));
                db.ArtworkTags.Remove(row);
            }
            else if (type == "defect.type")
            {
                DefectType? row = await db.DefectTypes.SingleOrDefaultAsync(x => x.Id == id, token);
                if (row is null) return Result.Failure<bool>(LookupErrors.ValueNotFound);
                if (row.Version != version) return Result.Failure<bool>(LookupErrors.Concurrency);
                if (await db.QCDefects.AnyAsync(x => x.DefectTypeId == id, token)) return Result.Failure<bool>(Error.BusinessRule("lookup.value_referenced"));
                db.DefectTypes.Remove(row);
            }
            else return Result.Failure<bool>(LookupErrors.TypeNotFound);
            Log(type, id, "deleted"); return Result.Success(true);
        }, ct);

    private void Metadata(object row, ReferenceEntityCommand command)
    {
        db.Entry(row).Property("ReferenceNames").CurrentValue = JsonSerializer.Serialize(command.Translations);
        db.Entry(row).Property("ReferenceDescriptions").CurrentValue = JsonSerializer.Serialize(command.Descriptions ?? new Dictionary<string, string>());
        db.Entry(row).Property("ReferenceDisplayOrder").CurrentValue = command.DisplayOrder;
    }
    private LookupTranslationView[] Captions(object row, string defaultName, long version)
    {
        var names = JsonSerializer.Deserialize<Dictionary<string, string>>((string?)db.Entry(row).Property("ReferenceNames").CurrentValue ?? "{}")!;
        var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>((string?)db.Entry(row).Property("ReferenceDescriptions").CurrentValue ?? "{}")!;
        return Cultures.Select(c => new LookupTranslationView(c, c == "fa" ? defaultName : names.GetValueOrDefault(c, defaultName), descriptions.GetValueOrDefault(c), version)).ToArray();
    }
    private int Order(object row) => (int)(db.Entry(row).Property("ReferenceDisplayOrder").CurrentValue ?? 0);
    private ReferenceEntityValueView View(ArtworkTag row) => new(row.Id, row.Code, Order(row), row.IsActive, false, row.Version, Captions(row, row.DisplayName, row.Version));
    private ReferenceEntityValueView View(DefectType row) => new(row.Id, row.Code, Order(row), row.IsActive, false, row.Version, Captions(row, row.Name, row.Version), row.CategoryCode, row.Severity.ToString(), row.IsBlocking);
    private void Log(string type, Guid id, string action)
    {
        audit.Write(new(type + "." + action, type, id.ToString(), ChangeKind: action == "created" ? AuditChangeKind.Added : AuditChangeKind.Modified));
        if (type == "defect.type" && Guid.TryParse(actor.Actor.UserId, out Guid user))
            db.QCHistory.Add(QCHistory.Create(company.RequireCompanyId(), "DefectType", id, action, user, time.GetUtcNow()));
    }
}
