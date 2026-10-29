using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Files;

namespace BARANG.Core.Application.Files;

public sealed record ArtworkView(Guid Id,string Code,string Title,string?Description,string?CategoryCode,Guid?DesignerUserId,string LifecycleStatus,Guid?CurrentVersionId,bool IsActive,bool IsArchived,long Version,IReadOnlyList<ArtworkTranslationView>?Translations=null);
public sealed record ArtworkPageView(long Total,int PageNumber,int PageSize,IReadOnlyList<ArtworkView> Items);
public sealed record ArtworkConceptMatch(Guid Id,string Code,string Title,string CategoryName,string LifecycleStatus,bool IsArchived,IReadOnlyList<string> Formats,int Score);
public sealed record ArtworkOption(Guid Id,string Code,string Title,string?CategoryCode,Guid?CurrentVersionId);
public sealed record ArtworkVersionView(Guid Id,Guid ArtworkId,int VersionNumber,Guid FileVersionId,string?Notes,Guid CreatedByUserId,DateTimeOffset CreatedAtUtc);
public sealed record ArtworkHistoryView(string Action,DateTimeOffset OccurredAtUtc,string?ActorUsername);
public sealed record ArtworkTagView(Guid Id,string Code,string DisplayName,bool IsActive,long Version);
public sealed record FileVersionView(Guid Id,Guid FileId,int VersionNumber,string OriginalFileName,string MediaType,string Extension,long SizeBytes,string Sha256Hash,string ProcessingStatus,bool IsArchived,DateTimeOffset CreatedAtUtc);
public sealed record StoredFileView(Guid Id,Guid?CurrentVersionId,bool IsArchived,long Version,IReadOnlyList<FileVersionView> Versions);
public sealed record UploadView(Guid FileId,Guid FileVersionId,int VersionNumber,string OriginalFileName,string MediaType,long SizeBytes,string Sha256Hash,string ProcessingStatus,bool DuplicateDetected);
public sealed record FileLinkView(Guid Id,Guid FileId,Guid?FileVersionId,string EntityType,Guid EntityId,string Purpose,bool IsActive);
public sealed record ArtworkTagAssignmentView(Guid Id,Guid ArtworkId,Guid TagId,bool IsActive);
public sealed record DownloadFile(string FileName,string MediaType,long Length,Stream Content,string? ETag=null);
public sealed record DerivativeUrlView(string Url,string MediaType,int ExpiresInSeconds,string ETag);
public sealed record ArtworkTranslationInput(string Culture,string Title,string?Description=null);
public sealed record ArtworkTranslationView(string Culture,string Title,string?Description);
public sealed record ArtworkCommand(string Code,string Title,string?Description,string?CategoryCode,Guid?DesignerUserId,string LifecycleStatus="Draft",long Version=0,IReadOnlyList<ArtworkTranslationInput>?Translations=null);
public sealed record UploadCommand(Guid?FileId,string FileName,string DeclaredMediaType,long DeclaredLength,Stream Content,string?ClientHash=null);

public interface IArtworkFileAdministration
{
    Task<Result<ArtworkView>>CreateArtworkAsync(ArtworkCommand command,CancellationToken ct);Task<Result<ArtworkView>>GetArtworkAsync(Guid id,CancellationToken ct);Task<Result<ArtworkPageView>>SearchArtworksAsync(string?search,string?category,bool includeArchived,int pageNumber,int pageSize,CancellationToken ct);Task<Result<IReadOnlyList<ArtworkOption>>>ArtworkOptionsAsync(string?search,int limit,CancellationToken ct);Task<Result<ArtworkView>>UpdateArtworkAsync(Guid id,ArtworkCommand command,CancellationToken ct);Task<Result<ArtworkView>>ArchiveArtworkAsync(Guid id,bool archived,long version,CancellationToken ct);
    Task<Result<ArtworkVersionView>>CreateArtworkVersionAsync(Guid artworkId,Guid fileVersionId,string?notes,CancellationToken ct);Task<Result<IReadOnlyList<ArtworkVersionView>>>ListArtworkVersionsAsync(Guid artworkId,CancellationToken ct);Task<Result<IReadOnlyList<ArtworkHistoryView>>>ListArtworkHistoryAsync(Guid artworkId,int pageNumber,int pageSize,CancellationToken ct);Task<Result<DownloadFile>>GetThumbnailAsync(Guid artworkId,CancellationToken ct);Task<Result<DownloadFile>>GetWebVariantAsync(Guid artworkId,CancellationToken ct);Task<Result<DerivativeUrlView>>GetDerivativeUrlAsync(Guid artworkId,string variant,CancellationToken ct);Task<Result<DerivativeUrlView>>GetOriginalUrlAsync(Guid artworkId,CancellationToken ct);Task<Result<DownloadFile>>GetOriginalArtworkAsync(Guid artworkId,CancellationToken ct);
    Task<Result<ArtworkTagView>>CreateTagAsync(string code,string name,CancellationToken ct);Task<Result<ArtworkTagView>>UpdateTagAsync(Guid id,string name,bool active,long version,CancellationToken ct);Task<Result<FileLinkView>>LinkFileAsync(Guid fileId,Guid?versionId,FileLinkEntityType entityType,Guid entityId,FileLinkPurpose purpose,CancellationToken ct);Task<Result<ArtworkTagAssignmentView>>AssignTagAsync(Guid artworkId,Guid tagId,CancellationToken ct);
    Task<Result<UploadView>>UploadAsync(UploadCommand command,CancellationToken ct);Task<Result<IReadOnlyList<ArtworkConceptMatch>>>ConceptSearchArtworksAsync(string?query,int limit,string?culture,CancellationToken ct);Task<Result<StoredFileView>>GetFileAsync(Guid id,CancellationToken ct);Task<Result<DownloadFile>>DownloadAsync(Guid fileId,Guid versionId,CancellationToken ct);Task<Result<StoredFileView>>ArchiveFileAsync(Guid id,long version,CancellationToken ct);
}
