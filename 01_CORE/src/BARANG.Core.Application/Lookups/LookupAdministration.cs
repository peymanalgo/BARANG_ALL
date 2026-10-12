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
        if (DomainReferenceCatalog.Adapters.Any(x => string.Equals(x.Code, command.Code.Trim(), StringComparison.OrdinalIgnoreCase))) return Result.Failure<LookupTypeView>(LookupErrors.SystemRecordProtected);
        Guid? companyId = company.RequireCompanyId();
        if (command.Scope == LookupScope.Global) { if (!await store.CanManageGlobalAsync(token)) return Result.Failure<LookupTypeView>(LookupErrors.SystemRecordProtected); companyId = null; }
        if (DomainReferenceCatalog.Find(command.Code.Trim().ToLowerInvariant()) is { } definition && definition.Scope != command.Scope) return Result.Failure<LookupTypeView>(LookupErrors.InvalidScope);
        Result<LookupType> created = LookupType.Create(Guid.NewGuid(), companyId, command.Code, command.DisplayName, command.Scope, false, command.AllowCustomValues, time.GetUtcNow());
        if (created.IsFailure) return Result.Failure<LookupTypeView>(created.Error);
        if (await store.TypeCodeExistsAsync(created.Value.Code, token)) return Result.Failure<LookupTypeView>(LookupErrors.DuplicateCode);
        store.Add(created.Value); Write(AuditActions.LookupTypeCreated, "lookup_type", created.Value.Id, AuditChangeKind.Added, new Dictionary<string,object?>{{"code",created.Value.Code}}); return Result.Success(View(created.Value));
    }, ct);
    public Task<Result<LookupTypeView>> UpdateTypeAsync(Guid id, string displayName, bool allowCustomValues, long version, CancellationToken ct) => ChangeType(id, version, type => type.Update(displayName, allowCustomValues, time.GetUtcNow()), AuditActions.LookupTypeUpdated, ct);
    public Task<Result<LookupTypeView>> SetTypeActiveAsync(Guid id, bool active, long version, CancellationToken ct) => ChangeType(id, version, type => type.SetActive(active, time.GetUtcNow()), active ? AuditActions.LookupTypeActivated : AuditActions.LookupTypeDeactivated, ct);
    public Task<Result<IReadOnlyList<LookupValueView>>> ListValuesAsync(string typeCode, bool activeOnly, CancellationToken ct) => Read(async token => { LookupType? type = await TypeByCode(typeCode, token); return type is null ? Result.Failure<IReadOnlyList<LookupValueView>>(LookupErrors.TypeNotFound) : Result.Success<IReadOnlyList<LookupValueView>>((await store.ListValuesAsync(type.Id, activeOnly, token)).Select(View).ToArray()); }, ct);
    public Task<Result<LookupValueView>> GetValueAsync(Guid id, CancellationToken ct) => Read(async token => Map(await store.FindValueAsync(id, token), View, LookupErrors.ValueNotFound), ct);
    public Task<Result<LookupValueView>> GetValueByCodeAsync(string typeCode, string valueCode, CancellationToken ct) => Read(async token => { LookupType? type = await TypeByCode(typeCode, token); string? code = LookupText.NormalizeValueCode(valueCode); if (type is null) return Result.Failure<LookupValueView>(LookupErrors.TypeNotFound); return code is null ? Result.Failure<LookupValueView>(LookupErrors.InvalidCode) : Map(await store.FindValueByCodeAsync(type.Id, code, token), View, LookupErrors.ValueNotFound); }, ct);
    public Task<Result<LookupValueView>> CreateValueAsync(string typeCode, CreateLookupValueCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        company.RequireCompanyId(); LookupType? type = await TypeByCode(typeCode, token);
        if (type is null && DomainReferenceCatalog.Find(typeCode) is { } definition)
        {
            if (definition.Scope == LookupScope.Global && !await store.CanManageGlobalAsync(token)) return Result.Failure<LookupValueView>(LookupErrors.SystemRecordProtected);
            Result<LookupType> createdType = LookupType.Create(Guid.NewGuid(), definition.Scope == LookupScope.Global ? null : company.RequireCompanyId(), definition.Code, definition.English, definition.Scope, false, true, time.GetUtcNow());
            if (createdType.IsFailure) return Result.Failure<LookupValueView>(createdType.Error);
            type = createdType.Value; store.Add(type);
        }
        if (type is null) return Result.Failure<LookupValueView>(LookupErrors.TypeNotFound);
        if (type.IsSystem || (type.Scope == LookupScope.Global && !await store.CanManageGlobalAsync(token))) return Result.Failure<LookupValueView>(LookupErrors.SystemRecordProtected);
        Result<LookupValue> created = LookupValue.Create(Guid.NewGuid(), type, command.Code, command.DisplayOrder, false, time.GetUtcNow());
        if (created.IsFailure) return Result.Failure<LookupValueView>(created.Error);
        if (await store.ValueCodeExistsAsync(type.Id, created.Value.Code, token)) return Result.Failure<LookupValueView>(LookupErrors.DuplicateCode);
        if (type.Code == "artwork.category") { Result parent = await ValidateCategoryParent(created.Value.Id, type.Id, command.ParentId, token); if (parent.IsFailure) return Result.Failure<LookupValueView>(parent.Error); created.Value.SetParent(command.ParentId, time.GetUtcNow()); }
        HashSet<string> cultures = new(StringComparer.Ordinal);
        foreach ((string culture, string name) in command.Translations) { string? normalized = LookupText.NormalizeCulture(culture); if (normalized is not null && !cultures.Add(normalized)) return Result.Failure<LookupValueView>(LookupErrors.DuplicateTranslation); Result<LookupValueTranslation> translation = created.Value.SetTranslation(culture, name, string.IsNullOrWhiteSpace(command.Descriptions?.GetValueOrDefault(culture)) ? null : command.Descriptions.GetValueOrDefault(culture), time.GetUtcNow()); if (translation.IsFailure) return Result.Failure<LookupValueView>(translation.Error); }
        store.Add(created.Value); Write(AuditActions.LookupValueCreated, "lookup_value", created.Value.Id, AuditChangeKind.Added, new Dictionary<string,object?>{{"code",created.Value.Code},{"lookupTypeId",type.Id}}); return Result.Success(View(created.Value));
    }, ct);
    public Task<Result<LookupValueView>> UpdateValueAsync(Guid id, int displayOrder, long version, CancellationToken ct) => ChangeValue(id, version, value => value.UpdateOrder(displayOrder, time.GetUtcNow()), AuditActions.LookupValueUpdated, ct);
    public Task<Result<LookupValueView>> SetValueActiveAsync(Guid id, bool active, long version, CancellationToken ct) => ChangeValue(id, version, value => value.SetActive(active, time.GetUtcNow()), active ? AuditActions.LookupValueActivated : AuditActions.LookupValueDeactivated, ct);
    public Task<Result<LookupValueView>> SetTranslationAsync(Guid id, string culture, string displayName, string? description, long version, CancellationToken ct) => ChangeValue(id, version, value => { Result<LookupValueTranslation> changed = value.SetTranslation(culture, displayName, description, time.GetUtcNow()); return changed.IsFailure ? Result.Failure(changed.Error) : Result.Success(); }, AuditActions.LookupTranslationUpdated, ct);
    public Task<Result<IReadOnlyList<LookupOptionView>>> GetOptionsAsync(string typeCode, string culture, bool activeOnly, CancellationToken ct) => Read(async token =>
    {
        string? normalizedCulture = LookupText.NormalizeCulture(culture); if (normalizedCulture is null) return Result.Failure<IReadOnlyList<LookupOptionView>>(LookupErrors.UnsupportedCulture);
        LookupType? type = await TypeByCode(typeCode, token); if (type is null) return DomainReferenceCatalog.Find(typeCode) is not null ? Result.Success<IReadOnlyList<LookupOptionView>>([]) : Result.Failure<IReadOnlyList<LookupOptionView>>(LookupErrors.TypeNotFound);
        IReadOnlyList<LookupValue> values = activeOnly && !type.IsActive ? [] : await store.ListValuesAsync(type.Id, activeOnly, token);
        return Result.Success<IReadOnlyList<LookupOptionView>>(values.Select(v => new LookupOptionView(v.Id, v.Code, v.Translations.FirstOrDefault(t => t.Culture == normalizedCulture)?.DisplayName ?? v.Translations.FirstOrDefault(t => t.Culture == LookupText.DefaultCulture)?.DisplayName ?? v.Code, v.DisplayOrder, v.IsActive && type.IsActive)).ToArray());
    }, ct);


    public Task<Result<IReadOnlyList<ReferenceDomainView>>> GetDomainsAsync(string culture, CancellationToken ct) => Read(async token =>
    {
        if (LookupText.NormalizeCulture(culture) is not string normalized) return Result.Failure<IReadOnlyList<ReferenceDomainView>>(LookupErrors.UnsupportedCulture);
        bool global = await store.CanManageGlobalAsync(token);
        return Result.Success<IReadOnlyList<ReferenceDomainView>>(DomainReferenceCatalog.Domains.Select(domain => new ReferenceDomainView(domain.Code, domain.Caption(normalized),
            DomainReferenceCatalog.Types.Concat(DomainReferenceCatalog.Adapters).Where(type => type.Domains.Contains(domain.Code)).Select(type => new ReferenceCatalogView(type.Code, type.Caption(normalized), type.Scope.ToString(), type.Scope == LookupScope.Company || global)).ToArray())).ToArray());
    }, ct);

    public Task<Result<LookupValueView>> EditValueAsync(Guid id, EditLookupValueCommand command, CancellationToken ct) =>
        EditValueWithCategoryValidationAsync(id, command, ct);

    private Task<Result<LookupValueView>> EditValueWithCategoryValidationAsync(Guid id, EditLookupValueCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        company.RequireCompanyId();
        LookupValue? value = await store.FindValueAsync(id, token);
        if (value is null) return Result.Failure<LookupValueView>(LookupErrors.ValueNotFound);
        LookupType? type = await store.FindTypeAsync(value.LookupTypeId, token);
        if (type?.IsSystem == true || value.IsSystem || (value.CompanyId is null && !await store.CanManageGlobalAsync(token))) return Result.Failure<LookupValueView>(LookupErrors.SystemRecordProtected);
        if (value.Version != command.Version) return Result.Failure<LookupValueView>(LookupErrors.Concurrency);
        if (type?.Code == "artwork.category")
        {
            Result parentValidation = await ValidateCategoryMove(value, command.ParentId, token);
            if (parentValidation.IsFailure) return Result.Failure<LookupValueView>(parentValidation.Error);
        }
        Result result = ApplyLookupEdit(value, command);
        if (result.IsFailure) return Result.Failure<LookupValueView>(result.Error);
        Write(AuditActions.LookupValueUpdated, "lookup_value", id, AuditChangeKind.Modified);
        return Result.Success(View(value));
    }, ct);

    private Result ApplyLookupEdit(LookupValue value, EditLookupValueCommand command)
    {
        Result order = value.UpdateOrder(command.DisplayOrder, time.GetUtcNow());
        return order.IsFailure ? order : ApplyLookupEditDetails(value, command);
    }

    private Result ApplyLookupEditDetails(LookupValue value, EditLookupValueCommand command)
    {
        foreach ((string culture, string caption) in command.Translations)
        {
            string? description = command.Descriptions?.GetValueOrDefault(culture);
            Result<LookupValueTranslation> translation = value.SetTranslation(culture, caption, string.IsNullOrWhiteSpace(description) ? null : description, time.GetUtcNow());
            if (translation.IsFailure) return Result.Failure(translation.Error);
        }
        Result parent = value.SetParent(command.ParentId, time.GetUtcNow()); if (parent.IsFailure) return parent;
        return value.SetActive(command.IsActive, time.GetUtcNow());
    }

    public Task<Result<bool>> DeleteValueAsync(Guid id, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        company.RequireCompanyId();
        LookupValue? value = await store.FindValueAsync(id, token);
        if (value is null) return Result.Failure<bool>(LookupErrors.ValueNotFound);
        if ((await store.FindTypeAsync(value.LookupTypeId, token))?.IsSystem == true || value.IsSystem || (value.CompanyId is null && !await store.CanManageGlobalAsync(token))) return Result.Failure<bool>(LookupErrors.SystemRecordProtected);
        if (value.Version != version) return Result.Failure<bool>(LookupErrors.Concurrency);
        if ((await store.FindTypeAsync(value.LookupTypeId, token))?.Code == "artwork.category" && (await store.ListValuesAsync(value.LookupTypeId, false, token)).Any(x => x.ParentId == id)) return Result.Failure<bool>(Error.BusinessRule("artwork.category.has_children"));
        if (await store.HasReferencesAsync(id, token)) return Result.Failure<bool>(Error.BusinessRule("lookup.value_referenced"));
        store.Remove(value);
        Write("lookup.value.deleted", "lookup_value", id, AuditChangeKind.Archived, new Dictionary<string, object?> { ["code"] = value.Code, ["lookupTypeId"] = value.LookupTypeId });
        return Result.Success(true);
    }, ct);

    private Task<Result<LookupTypeView>> ChangeType(Guid id, long version, Func<LookupType, Result> change, string action, CancellationToken ct) => transactions.ExecuteAsync(async token => { company.RequireCompanyId(); LookupType? type = await store.FindTypeAsync(id, token); if (type is null) return Result.Failure<LookupTypeView>(LookupErrors.TypeNotFound); if (type.Scope == LookupScope.Global && !await store.CanManageGlobalAsync(token)) return Result.Failure<LookupTypeView>(LookupErrors.SystemRecordProtected); if (type.Version != version) return Result.Failure<LookupTypeView>(LookupErrors.Concurrency); var before=(type.DisplayName,type.AllowCustomValues,type.IsActive); Result result = change(type); if (result.IsFailure) return Result.Failure<LookupTypeView>(result.Error); Dictionary<string,AuditValueChange> changes=[]; AddChange(changes,"displayName",before.DisplayName,type.DisplayName); AddChange(changes,"allowCustomValues",before.AllowCustomValues,type.AllowCustomValues); AddChange(changes,"isActive",before.IsActive,type.IsActive); Write(action, "lookup_type", id, AuditChangeKind.Modified, changes:changes); return Result.Success(View(type)); }, ct);
    private Task<Result<LookupValueView>> ChangeValue(Guid id, long version, Func<LookupValue, Result> change, string action, CancellationToken ct) => transactions.ExecuteAsync(async token => { company.RequireCompanyId(); LookupValue? value = await store.FindValueAsync(id, token); if (value is null) return Result.Failure<LookupValueView>(LookupErrors.ValueNotFound); if ((await store.FindTypeAsync(value.LookupTypeId, token))?.IsSystem == true || value.IsSystem || (value.CompanyId is null && !await store.CanManageGlobalAsync(token))) return Result.Failure<LookupValueView>(LookupErrors.SystemRecordProtected); if (value.Version != version) return Result.Failure<LookupValueView>(LookupErrors.Concurrency); int oldOrder=value.DisplayOrder; bool oldActive=value.IsActive; string oldTranslations=TranslationSnapshot(value); Result result = change(value); if (result.IsFailure) return Result.Failure<LookupValueView>(result.Error); Dictionary<string,AuditValueChange> changes=[]; AddChange(changes,"displayOrder",oldOrder,value.DisplayOrder); AddChange(changes,"isActive",oldActive,value.IsActive); AddChange(changes,"translations",oldTranslations,TranslationSnapshot(value)); Write(action, "lookup_value", id, AuditChangeKind.Modified, changes:changes); return Result.Success(View(value)); }, ct);
    private Task<Result<T>> Read<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct) { company.RequireCompanyId(); return transactions.ExecuteAsync(operation, ct); }
    private async Task<LookupType?> TypeByCode(string code, CancellationToken ct) { string? normalized = LookupText.NormalizeCode(code); return normalized is null ? null : await store.FindTypeByCodeAsync(normalized, ct); }
    private void Write(string action, string entity, Guid id, AuditChangeKind kind, IReadOnlyDictionary<string,object?>? metadata=null, IReadOnlyDictionary<string,AuditValueChange>? changes=null) => audit.Write(new(action, entity, id.ToString(), CompanyId: company.RequireCompanyId(), ChangeKind: kind, Metadata:metadata, Changes:changes));
    private static void AddChange<T>(Dictionary<string,AuditValueChange> changes,string name,T oldValue,T newValue) { if(!EqualityComparer<T>.Default.Equals(oldValue,newValue)) changes[name]=new(oldValue,newValue); }
    private static string TranslationSnapshot(LookupValue value)=>string.Join('|',value.Translations.OrderBy(x=>x.Culture).Select(x=>$"{x.Culture}:{x.DisplayName}"));
    private static Result<TView> Map<TEntity,TView>(TEntity? value, Func<TEntity,TView> map, Error error) where TEntity:class => value is null ? Result.Failure<TView>(error) : Result.Success(map(value));
    private static LookupTypeView View(LookupType x) => new(x.Id,x.CompanyId,x.Code,x.DisplayName,x.Scope,x.IsSystem,x.IsActive,x.AllowCustomValues,x.CreatedAtUtc,x.UpdatedAtUtc,x.Version);
    private async Task<Result> ValidateCategoryParent(Guid id,Guid typeId,Guid? parentId,CancellationToken token)
    {
        if (parentId is null) return Result.Success();
        LookupValue? parent=await store.FindValueAsync(parentId.Value,token);
        if(parent is null||parent.LookupTypeId!=typeId) return Result.Failure(Error.BusinessRule("artwork.category.invalid_parent"));
        HashSet<Guid>seen=[]; Guid? current=parent.Id;
        while(current is Guid cursor){if(!seen.Add(cursor)||cursor==id)return Result.Failure(Error.BusinessRule("artwork.category.cycle"));LookupValue? node=await store.FindValueAsync(cursor,token);if(node is null)break;current=node.ParentId;}
        return Result.Success();
    }
    private async Task<Result> ValidateCategoryMove(LookupValue value, Guid? parentId, CancellationToken token)
    {
        Result parent = await ValidateCategoryParent(value.Id, value.LookupTypeId, parentId, token);
        return parent;
    }
    private static LookupValueView View(LookupValue x) => new(x.Id,x.LookupTypeId,x.CompanyId,x.ParentId,x.Code,x.DisplayOrder,x.IsSystem,x.IsActive,x.Translations.OrderBy(t=>t.Culture).Select(t=>new LookupTranslationView(t.Culture,t.DisplayName,t.Description,t.Version)).ToArray(),x.CreatedAtUtc,x.UpdatedAtUtc,x.Version);
}
