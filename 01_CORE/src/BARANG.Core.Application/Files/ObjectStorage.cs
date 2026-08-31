namespace BARANG.Core.Application.Files;

public sealed record ObjectStorageWrite(string Key, Stream Content, long Length, string MediaType);
public sealed record ObjectStorageHead(string Key, long Length, string MediaType);
public interface IObjectStorage
{
    string Provider { get; }
    string Bucket { get; }
    string CreateKey(Guid companyId, Guid fileId, Guid versionId, string extension, bool derivative = false);
    Task PutAsync(ObjectStorageWrite value, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct);
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
    public long MaximumUploadBytes { get; set; } = 25 * 1024 * 1024;
    public string[] AllowedMediaTypes { get; set; } = ["image/png", "image/jpeg", "application/pdf"];
}
