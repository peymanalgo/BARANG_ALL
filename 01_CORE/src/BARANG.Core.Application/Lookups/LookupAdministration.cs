using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Lookups;

namespace BARANG.Core.Application.Lookups;

public sealed class LookupAdministration(ILookupStore store, ITransactionExecutor transactions, ICompanyContext company, IAuditWriter audit, TimeProvider time) : ILookupAdministration, ILookupOptionReader
{
    public Task<Result<IReadOnlyList<LookupTypeView>>> ListTypesAsync(bool activeOnly, CancellationToken ct) => Read(async token => Result.Success<IReadOnlyList<LookupTypeView>>((await store.ListTypesAsync(activeOnly, token)).Select(View).ToArray()), ct);
    public Task<Result<LookupTypeView>> GetTypeAsync(Guid id, CancellationToken ct) => Read(async token => Map(await store.FindTypeAsync(id, token), View, LookupErrors.TypeNotFound), ct);
    public Task<Result<LookupTypeView>> GetTypeByCodeAsync(string code, CancellationToken ct) => Read(async token => { string? normalized = LookupText.NormalizeCode(code); return normalized is null ? Result.Failure<LookupTypeView>(LookupErrors.InvalidCode) : Map(await store.FindTypeByCodeAsync(normalized, token), View, LookupErrors.TypeNotFound); }, ct);
    public Task<Result<LookupTypeView>> CreateTypeAsync(CreateLookupTypeCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid companyId = company.RequireCompanyId();
        Result<LookupType> created = LookupType.Create(Guid.NewGuid(), companyId, command.Code, command.DisplayName, LookupScope.Company, false, command.AllowCustomValues, time.GetUtcNow());
        if (created.IsFailure) return Result.Failure<LookupTypeView>(created.Error);
        if (await store.TypeCodeExistsAsync(created.Value.Code, token)) return Result.Failure<LookupTypeView>(LookupErrors.DuplicateCode);
        store.Add(created.Value); Write(AuditActions.LookupTypeCreated, "lookup_type", created.Value.Id, AuditChangeKind.Added, new Dictionary<string,object?>{{"code",created.Value.Code}}); return Result.Success(View(created.Value));
    }, ct);
    public Task<Result<LookupTypeView>> UpdateTypeAsync(Guid id, string displayName, bool allowCustomValues, long version, CancellationToken ct) => ChangeType(id, version, type => type.Update(displayName, allowCustomValues, time.GetUtcNow()), AuditActions.LookupTypeUpdated, ct);
    public Task<Result<LookupTypeView>> SetTypeActiveAsync(Guid id, bool active, long version, CancellationToken ct) => ChangeType(id, version, type => type.SetActive(active, time.GetUtcNow()), active ? AuditActions.LookupTypeActivated : AuditActions.LookupTypeDeactivated, ct);
    public Task<Result<IReadOnlyList<LookupValueView>>> ListValuesAsync(string typeCode, bool activeOnly, CancellationToken ct) => Read(async token => { LookupType? type = await TypeByCode(typeCode, token); return type is null ? Result.Failure<IReadOnlyList<LookupValueView>>(LookupErrors.TypeNotFound) : Result.Success<IReadOnlyList<LookupValueView>>((await store.ListValuesAsync(type.Id, activeOnly, token)).Select(View).ToArray()); }, ct);
    public Task<Result<LookupValueView>> GetValueAsync(Guid id, CancellationToken ct) => Read(async token => Map(await store.FindValueAsync(id, token), View, LookupErrors.ValueNotFound), ct);
    public Task<Result<LookupValueView>> GetValueByCodeAsync(string typeCode, string valueCode, CancellationToken ct) => Read(async token => { LookupType? type = await TypeByCode(typeCode, token); string? code = LookupText.NormalizeCode(valueCode); if (type is null) return Result.Failure<LookupValueView>(LookupErrors.TypeNotFound); return code is null ? Result.Failure<LookupValueView>(LookupErrors.InvalidCode) : Map(await store.FindValueByCodeAsync(type.Id, code, token), View, LookupErrors.ValueNotFound); }, ct);
    public Task<Result<LookupValueView>> CreateValueAsync(string typeCode, CreateLookupValueCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        company.RequireCompanyId(); LookupType? type = await TypeByCode(typeCode, token);
        if (type is null) return Result.Failure<LookupValueView>(LookupErrors.TypeNotFound);
        if (type.Scope != LookupScope.Company || type.IsSystem) return Result.Failure<LookupValueView>(LookupErrors.SystemRecordProtected);
        Result<LookupValue> created = LookupValue.Create(Guid.NewGuid(), type, command.Code, command.DisplayOrder, false, time.GetUtcNow());
        if (created.IsFailure) return Result.Failure<LookupValueView>(created.Error);
        if (await store.ValueCodeExistsAsync(type.Id, created.Value.Code, token)) return Result.Failure<LookupValueView>(LookupErrors.DuplicateCode);
        HashSet<string> cultures = new(StringComparer.Ordinal);
        foreach ((string culture, string name) in command.Translations) { string? normalized = LookupText.NormalizeCulture(culture); if (normalized is not null && !cultures.Add(normalized)) return Result.Failure<LookupValueView>(LookupErrors.DuplicateTranslation); Result<LookupValueTranslation> translation = created.Value.SetTranslation(culture, name, null, time.GetUtcNow()); if (translation.IsFailure) return Result.Failure<LookupValueView>(translation.Error); }
        store.Add(created.Value); Write(AuditActions.LookupValueCreated, "lookup_value", created.Value.Id, AuditChangeKind.Added, new Dictionary<string,object?>{{"code",created.Value.Code},{"lookupTypeId",type.Id}}); return Result.Success(View(created.Value));
    }, ct);
    public Task<Result<LookupValueView>> UpdateValueAsync(Guid id, int displayOrder, long version, CancellationToken ct) => ChangeValue(id, version, value => value.UpdateOrder(displayOrder, time.GetUtcNow()), AuditActions.LookupValueUpdated, ct);
    public Task<Result<LookupValueView>> SetValueActiveAsync(Guid id, bool active, long version, CancellationToken ct) => ChangeValue(id, version, value => value.SetActive(active, time.GetUtcNow()), active ? AuditActions.LookupValueActivated : AuditActions.LookupValueDeactivated, ct);
    public Task<Result<LookupValueView>> SetTranslationAsync(Guid id, string culture, string displayName, string? description, long version, CancellationToken ct) => ChangeValue(id, version, value => { Result<LookupValueTranslation> changed = value.SetTranslation(culture, displayName, description, time.GetUtcNow()); return changed.IsFailure ? Result.Failure(changed.Error) : Result.Success(); }, AuditActions.LookupTranslationUpdated, ct);
    public Task<Result<IReadOnlyList<LookupOptionView>>> GetOptionsAsync(string typeCode, string culture, bool activeOnly, CancellationToken ct) => Read(async token =>
    {
        string? normalizedCulture = LookupText.NormalizeCulture(culture); if (normalizedCulture is null) return Result.Failure<IReadOnlyList<LookupOptionView>>(LookupErrors.UnsupportedCulture);
        LookupType? type = await TypeByCode(typeCode, token); if (type is null) return Result.Failure<IReadOnlyList<LookupOptionView>>(LookupErrors.TypeNotFound);
        IReadOnlyList<LookupValue> values = await store.ListValuesAsync(type.Id, activeOnly, token);
        return Result.Success<IReadOnlyList<LookupOptionView>>(values.Select(v => new LookupOptionView(v.Id, v.Code, v.Translations.FirstOrDefault(t => t.Culture == normalizedCulture)?.DisplayName ?? v.Translations.FirstOrDefault(t => t.Culture == LookupText.DefaultCulture)?.DisplayName ?? v.Code, v.DisplayOrder, v.IsActive)).ToArray());
    }, ct);

    private Task<Result<LookupTypeView>> ChangeType(Guid id, long version, Func<LookupType, Result> change, string action, CancellationToken ct) => transactions.ExecuteAsync(async token => { company.RequireCompanyId(); LookupType? type = await store.FindTypeAsync(id, token); if (type is null) return Result.Failure<LookupTypeView>(LookupErrors.TypeNotFound); if (type.Version != version) return Result.Failure<LookupTypeView>(LookupErrors.Concurrency); var before=(type.DisplayName,type.AllowCustomValues,type.IsActive); Result result = change(type); if (result.IsFailure) return Result.Failure<LookupTypeView>(result.Error); Dictionary<string,AuditValueChange> changes=[]; AddChange(changes,"displayName",before.DisplayName,type.DisplayName); AddChange(changes,"allowCustomValues",before.AllowCustomValues,type.AllowCustomValues); AddChange(changes,"isActive",before.IsActive,type.IsActive); Write(action, "lookup_type", id, AuditChangeKind.Modified, changes:changes); return Result.Success(View(type)); }, ct);
    private Task<Result<LookupValueView>> ChangeValue(Guid id, long version, Func<LookupValue, Result> change, string action, CancellationToken ct) => transactions.ExecuteAsync(async token => { company.RequireCompanyId(); LookupValue? value = await store.FindValueAsync(id, token); if (value is null) return Result.Failure<LookupValueView>(LookupErrors.ValueNotFound); if (value.Version != version) return Result.Failure<LookupValueView>(LookupErrors.Concurrency); int oldOrder=value.DisplayOrder; bool oldActive=value.IsActive; string oldTranslations=TranslationSnapshot(value); Result result = change(value); if (result.IsFailure) return Result.Failure<LookupValueView>(result.Error); Dictionary<string,AuditValueChange> changes=[]; AddChange(changes,"displayOrder",oldOrder,value.DisplayOrder); AddChange(changes,"isActive",oldActive,value.IsActive); AddChange(changes,"translations",oldTranslations,TranslationSnapshot(value)); Write(action, "lookup_value", id, AuditChangeKind.Modified, changes:changes); return Result.Success(View(value)); }, ct);
    private Task<Result<T>> Read<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct) { company.RequireCompanyId(); return transactions.ExecuteAsync(operation, ct); }
    private async Task<LookupType?> TypeByCode(string code, CancellationToken ct) { string? normalized = LookupText.NormalizeCode(code); return normalized is null ? null : await store.FindTypeByCodeAsync(normalized, ct); }
    private void Write(string action, string entity, Guid id, AuditChangeKind kind, IReadOnlyDictionary<string,object?>? metadata=null, IReadOnlyDictionary<string,AuditValueChange>? changes=null) => audit.Write(new(action, entity, id.ToString(), CompanyId: company.RequireCompanyId(), ChangeKind: kind, Metadata:metadata, Changes:changes));
    private static void AddChange<T>(Dictionary<string,AuditValueChange> changes,string name,T oldValue,T newValue) { if(!EqualityComparer<T>.Default.Equals(oldValue,newValue)) changes[name]=new(oldValue,newValue); }
    private static string TranslationSnapshot(LookupValue value)=>string.Join('|',value.Translations.OrderBy(x=>x.Culture).Select(x=>$"{x.Culture}:{x.DisplayName}"));
    private static Result<TView> Map<TEntity,TView>(TEntity? value, Func<TEntity,TView> map, Error error) where TEntity:class => value is null ? Result.Failure<TView>(error) : Result.Success(map(value));
    private static LookupTypeView View(LookupType x) => new(x.Id,x.CompanyId,x.Code,x.DisplayName,x.Scope,x.IsSystem,x.IsActive,x.AllowCustomValues,x.CreatedAtUtc,x.UpdatedAtUtc,x.Version);
    private static LookupValueView View(LookupValue x) => new(x.Id,x.LookupTypeId,x.CompanyId,x.Code,x.DisplayOrder,x.IsSystem,x.IsActive,x.Translations.OrderBy(t=>t.Culture).Select(t=>new LookupTranslationView(t.Culture,t.DisplayName,t.Description,t.Version)).ToArray(),x.CreatedAtUtc,x.UpdatedAtUtc,x.Version);
}
