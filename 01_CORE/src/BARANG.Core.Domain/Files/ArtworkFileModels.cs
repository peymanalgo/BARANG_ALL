using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Files;

public static class FileErrors
{
    public static Error InvalidData => Error.Validation("file.invalid_data");
    public static Error UnsupportedContent => Error.Validation("file.unsupported_content");
    public static Error TooLarge => Error.Validation("file.too_large");
    public static Error Empty => Error.Validation("file.empty");
    public static Error Duplicate => Error.Conflict("file.duplicate_content");
    public static Error DuplicateCode => Error.Conflict("artwork.duplicate_code");
    public static Error DuplicateAssignment => Error.Conflict("artwork.duplicate_tag");
    public static Error NotFound => Error.NotFound("file.not_found");
    public static Error CrossCompany => Error.NotFound("file.not_found");
    public static Error Concurrency => Error.Concurrency("file.concurrency_conflict");
    public static Error Archived => Error.BusinessRule("file.archived");
}

public static class FileText
{
    public static string? Code(string? value, int max = 64) { string v = value?.Trim().ToUpperInvariant() ?? ""; return v.Length is >= 1 && v.Length <= max && v.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.') ? v : null; }
    public static string? Name(string? value, int max, bool optional = false) { string v = value?.Trim() ?? ""; return v.Length == 0 && optional ? null : v.Length is >= 1 && v.Length <= max && !v.Any(char.IsControl) ? v : null; }
}

public enum ArtworkLifecycle { Draft, Active, Archived }
public enum FileProcessingStatus { Pending, Processing, Succeeded, Failed, NotApplicable }
public enum FileProcessingJobType { ThumbnailGeneration }
public enum FileLinkEntityType { Artwork, Order, OrderItem, OrderPrintOperation, SupplierContract, SupplierDocument }
public enum FileLinkPurpose { Source, Attachment, Reference }
public enum FileDerivativeType { Thumbnail, Web, PublicWeb }

public sealed class Artwork
{
    private Artwork() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public string Code { get; private set; } = ""; public string Title { get; private set; } = ""; public string? Description { get; private set; } public string? CategoryCode { get; private set; } public Guid? DesignerUserId { get; private set; } public ArtworkLifecycle LifecycleStatus { get; private set; } public Guid? CurrentVersionId { get; private set; } public bool IsActive { get; private set; } public bool IsArchived { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<Artwork> Create(Guid id, Guid company, string code, string title, string? description, string? category, Guid? designer, DateTimeOffset now) { string? c=FileText.Code(code,50),t=FileText.Name(title,200),d=FileText.Name(description,2000,true),cat=string.IsNullOrWhiteSpace(category)?null:FileText.Code(category,50); if(id==Guid.Empty||company==Guid.Empty||c is null||t is null||(!string.IsNullOrWhiteSpace(description)&&d is null)||(!string.IsNullOrWhiteSpace(category)&&cat is null))return Result.Failure<Artwork>(FileErrors.InvalidData); return Result.Success(new Artwork{Id=id,CompanyId=company,Code=c,Title=t,Description=d,CategoryCode=cat,DesignerUserId=designer,LifecycleStatus=ArtworkLifecycle.Draft,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1}); }
    public Result Update(string title,string? description,string? category,Guid? designer,ArtworkLifecycle lifecycle,DateTimeOffset now){string? t=FileText.Name(title,200),d=FileText.Name(description,2000,true),cat=string.IsNullOrWhiteSpace(category)?null:FileText.Code(category,50);if(t is null||lifecycle==ArtworkLifecycle.Archived||(!string.IsNullOrWhiteSpace(description)&&d is null)||(!string.IsNullOrWhiteSpace(category)&&cat is null))return Result.Failure(FileErrors.InvalidData);Title=t;Description=d;CategoryCode=cat;DesignerUserId=designer;LifecycleStatus=lifecycle;Touch(now);return Result.Success();}
    public void SetCurrentVersion(Guid id,DateTimeOffset now){CurrentVersionId=id;if(LifecycleStatus==ArtworkLifecycle.Draft)LifecycleStatus=ArtworkLifecycle.Active;Touch(now);} public void Archive(DateTimeOffset now){IsArchived=true;IsActive=false;LifecycleStatus=ArtworkLifecycle.Archived;Touch(now);} public void Restore(DateTimeOffset now){IsArchived=false;IsActive=true;LifecycleStatus=CurrentVersionId is null?ArtworkLifecycle.Draft:ArtworkLifecycle.Active;Touch(now);} private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
}

public sealed class ArtworkVersion
{
    private ArtworkVersion() { } public Guid Id{get;private set;} public Guid CompanyId{get;private set;} public Guid ArtworkId{get;private set;} public int VersionNumber{get;private set;} public Guid FileVersionId{get;private set;} public string? Notes{get;private set;} public Guid CreatedByUserId{get;private set;} public DateTimeOffset CreatedAtUtc{get;private set;}
    public static Result<ArtworkVersion>Create(Guid id,Guid company,Guid artwork,int number,Guid fileVersion,string? notes,Guid actor,DateTimeOffset now){string?n=FileText.Name(notes,1000,true);if(id==Guid.Empty||company==Guid.Empty||artwork==Guid.Empty||fileVersion==Guid.Empty||actor==Guid.Empty||number<1||(!string.IsNullOrWhiteSpace(notes)&&n is null))return Result.Failure<ArtworkVersion>(FileErrors.InvalidData);return Result.Success(new ArtworkVersion{Id=id,CompanyId=company,ArtworkId=artwork,VersionNumber=number,FileVersionId=fileVersion,Notes=n,CreatedByUserId=actor,CreatedAtUtc=now});}
}

public sealed class ArtworkTag
{
    private ArtworkTag(){} public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public string Code{get;private set;}="";public string DisplayName{get;private set;}="";public bool IsActive{get;private set;}public DateTimeOffset CreatedAtUtc{get;private set;}public DateTimeOffset UpdatedAtUtc{get;private set;}public long Version{get;private set;}
    public static Result<ArtworkTag>Create(Guid id,Guid company,string code,string name,DateTimeOffset now){string?c=FileText.Code(code,50),n=FileText.Name(name,100);if(id==Guid.Empty||company==Guid.Empty||c is null||n is null)return Result.Failure<ArtworkTag>(FileErrors.InvalidData);return Result.Success(new ArtworkTag{Id=id,CompanyId=company,Code=c,DisplayName=n,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1});} public Result Update(string name,bool active,DateTimeOffset now){string?n=FileText.Name(name,100);if(n is null)return Result.Failure(FileErrors.InvalidData);DisplayName=n;IsActive=active;UpdatedAtUtc=now;Version++;return Result.Success();}
}
public sealed class ArtworkTagAssignment { private ArtworkTagAssignment(){} public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid ArtworkId{get;private set;}public Guid TagId{get;private set;}public Guid CreatedByUserId{get;private set;}public DateTimeOffset CreatedAtUtc{get;private set;}public bool IsActive{get;private set;} public static ArtworkTagAssignment Create(Guid company,Guid artwork,Guid tag,Guid actor,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,ArtworkId=artwork,TagId=tag,CreatedByUserId=actor,CreatedAtUtc=now,IsActive=true}; public void Remove()=>IsActive=false; }

public sealed class StoredFile
{
    private StoredFile(){} public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid UploadedByUserId{get;private set;}public Guid? CurrentVersionId{get;private set;}public bool IsArchived{get;private set;}public DateTimeOffset CreatedAtUtc{get;private set;}public DateTimeOffset UpdatedAtUtc{get;private set;}public long Version{get;private set;}
    public static StoredFile Create(Guid id,Guid company,Guid actor,DateTimeOffset now)=>new(){Id=id,CompanyId=company,UploadedByUserId=actor,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1};public void SetCurrent(Guid version,DateTimeOffset now){CurrentVersionId=version;UpdatedAtUtc=now;Version++;}public void Archive(DateTimeOffset now){if(!IsArchived){IsArchived=true;UpdatedAtUtc=now;Version++;}}
}
public sealed class FileVersion
{
    private FileVersion(){}public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid FileId{get;private set;}public int VersionNumber{get;private set;}public string StorageProvider{get;private set;}="";public string StorageBucket{get;private set;}="";public string StorageKey{get;private set;}="";public string OriginalFileName{get;private set;}="";public string MediaType{get;private set;}="";public string Extension{get;private set;}="";public long SizeBytes{get;private set;}public string Sha256Hash{get;private set;}="";public Guid CreatedByUserId{get;private set;}public DateTimeOffset CreatedAtUtc{get;private set;}public FileProcessingStatus ProcessingStatus{get;private set;}public bool IsArchived{get;private set;}
    public static Result<FileVersion>Create(Guid id,Guid company,Guid file,int number,string provider,string bucket,string key,string name,string media,string extension,long size,string hash,Guid actor,DateTimeOffset now){if(id==Guid.Empty||company==Guid.Empty||file==Guid.Empty||actor==Guid.Empty||number<1||size<1||hash.Length!=64||key.Length is <1 or >500||FileText.Name(name,255) is null||media.Length is <3 or >100||extension.Length>20)return Result.Failure<FileVersion>(FileErrors.InvalidData);return Result.Success(new FileVersion{Id=id,CompanyId=company,FileId=file,VersionNumber=number,StorageProvider=provider,StorageBucket=bucket,StorageKey=key,OriginalFileName=name,MediaType=media,Extension=extension,SizeBytes=size,Sha256Hash=hash,CreatedByUserId=actor,CreatedAtUtc=now,ProcessingStatus=FileProcessingStatus.Pending});} public void Archive()=>IsArchived=true;
}
public sealed class FileLink { private FileLink(){} public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid FileId{get;private set;}public Guid? FileVersionId{get;private set;}public FileLinkEntityType EntityType{get;private set;}public Guid EntityId{get;private set;}public FileLinkPurpose Purpose{get;private set;}public Guid CreatedByUserId{get;private set;}public DateTimeOffset CreatedAtUtc{get;private set;}public bool IsActive{get;private set;} public static FileLink Create(Guid company,Guid file,Guid?version,FileLinkEntityType type,Guid entity,FileLinkPurpose purpose,Guid actor,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,FileId=file,FileVersionId=version,EntityType=type,EntityId=entity,Purpose=purpose,CreatedByUserId=actor,CreatedAtUtc=now,IsActive=true};public void Archive()=>IsActive=false; }
public sealed class FileProcessingJob { private FileProcessingJob(){}public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid FileVersionId{get;private set;}public FileProcessingJobType JobType{get;private set;}public FileProcessingStatus Status{get;private set;}public int AttemptCount{get;private set;}public DateTimeOffset RequestedAtUtc{get;private set;}public DateTimeOffset?StartedAtUtc{get;private set;}public DateTimeOffset?CompletedAtUtc{get;private set;}public string?LastErrorCode{get;private set;}public long Version{get;private set;}public static FileProcessingJob Create(Guid company,Guid version,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,FileVersionId=version,JobType=FileProcessingJobType.ThumbnailGeneration,Status=FileProcessingStatus.Pending,RequestedAtUtc=now,Version=1};public void Start(DateTimeOffset now){Status=FileProcessingStatus.Processing;StartedAtUtc=now;AttemptCount++;Version++;}public void Succeed(DateTimeOffset now){Status=FileProcessingStatus.Succeeded;CompletedAtUtc=now;LastErrorCode=null;Version++;}public void Skip(DateTimeOffset now){Status=FileProcessingStatus.NotApplicable;CompletedAtUtc=now;Version++;}public void Fail(string code,DateTimeOffset now){Status=FileProcessingStatus.Failed;CompletedAtUtc=now;LastErrorCode=code[..Math.Min(code.Length,100)];Version++;}}
public sealed class FileDerivative { private FileDerivative(){}public Guid Id{get;private set;}public Guid CompanyId{get;private set;}public Guid SourceFileVersionId{get;private set;}public FileDerivativeType DerivativeType{get;private set;}public string StorageKey{get;private set;}="";public string MediaType{get;private set;}="";public long SizeBytes{get;private set;}public string Sha256Hash{get;private set;}="";public DateTimeOffset CreatedAtUtc{get;private set;} public static FileDerivative Create(Guid company,Guid source,FileDerivativeType derivativeType,string key,string media,long size,string hash,DateTimeOffset now)=>new(){Id=Guid.NewGuid(),CompanyId=company,SourceFileVersionId=source,DerivativeType=derivativeType,StorageKey=key,MediaType=media,SizeBytes=size,Sha256Hash=hash,CreatedAtUtc=now};}
