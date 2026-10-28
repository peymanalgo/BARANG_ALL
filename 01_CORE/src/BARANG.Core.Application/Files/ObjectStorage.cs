namespace BARANG.Core.Application.Files;

public sealed record ObjectStorageWrite(string Key, Stream Content, long Length, string MediaType, string? CacheControl = null, string? Bucket = null);
public sealed record ObjectStorageHead(string Key, long Length, string MediaType);
public interface IObjectStorage
{
    string Provider { get; }
    string Bucket { get; }
    string CreateKey(Guid companyId, Guid fileId, Guid versionId, string extension, bool derivative = false);
    Task PutAsync(ObjectStorageWrite value, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct, string? bucket = null);
    Task<string?> GetPresignedReadUrlAsync(string key, int expirySeconds, CancellationToken ct, string? bucket = null);
    Task<ObjectStorageHead?> HeadAsync(string key, CancellationToken ct);
    Task<bool> RemoveUncommittedAsync(string key, CancellationToken ct);
}

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string Endpoint { get; set; } = "localhost:9000";
    public bool Enabled { get; set; }
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "barang-files";
    public bool Secure { get; set; }
    public long MaximumUploadBytes { get; set; } = long.MaxValue;
    public int ProcessingParallelism { get; set; } = 8;
    public int DecodeTimeoutSeconds { get; set; } = 30;
    public string PublicEndpoint { get; set; } = "";
    public bool PublicSecure { get; set; }
    public int DerivativeUrlExpirySeconds { get; set; } = 604800;
    public int OriginalUrlExpirySeconds { get; set; } = 3600;
    public string PublicBucket { get; set; } = "";
    public string[] AllowedMediaTypes { get; set; } = ["image/png", "image/jpeg", "image/gif", "image/webp", "image/bmp", "image/tiff", "application/pdf"];
}
