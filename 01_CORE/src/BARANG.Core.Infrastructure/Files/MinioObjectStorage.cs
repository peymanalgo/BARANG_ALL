using BARANG.Core.Application.Files;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace BARANG.Core.Infrastructure.Files;

public sealed class MinioObjectStorage(IMinioClient client, IOptions<FileStorageOptions> settings) : IObjectStorage, IDisposable
{
    private readonly FileStorageOptions _settings = settings.Value;
    private readonly SemaphoreSlim _clientGate = new(1, 1);
    public string Provider => "minio";
    public string Bucket => _settings.Bucket;
    public string CreateKey(Guid companyId, Guid fileId, Guid versionId, string extension, bool derivative=false)
    {
        string ext = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (ext.Length is < 1 or > 20 || !ext.All(char.IsAsciiLetterOrDigit)) throw new ArgumentException("Invalid extension.", nameof(extension));
        return $"companies/{companyId:N}/files/{fileId:N}/{(derivative ? "derivatives" : "versions")}/{versionId:N}-{Guid.NewGuid():N}.{ext}";
    }
    public async Task PutAsync(ObjectStorageWrite value,CancellationToken ct){await _clientGate.WaitAsync(ct);try{PutObjectArgs args=new PutObjectArgs().WithBucket(Bucket).WithObject(value.Key).WithStreamData(value.Content).WithObjectSize(value.Length).WithContentType(value.MediaType);if(!string.IsNullOrWhiteSpace(value.CacheControl))args.WithHeaders(new Dictionary<string,string>{{"Cache-Control",value.CacheControl}});await client.PutObjectAsync(args,ct);}finally{_clientGate.Release();}}
    public async Task<Stream?> OpenReadAsync(string key,CancellationToken ct){await _clientGate.WaitAsync(ct);MemoryStream result=new();try{await client.GetObjectAsync(new GetObjectArgs().WithBucket(Bucket).WithObject(key).WithCallbackStream(async(stream,cancellation)=>await stream.CopyToAsync(result,cancellation)),ct);result.Position=0;return result;}catch(ObjectNotFoundException){result.Dispose();return null;}catch(MinioException ex)when(ex.Message.Contains("not exist",StringComparison.OrdinalIgnoreCase)){result.Dispose();return null;}finally{_clientGate.Release();}}
    public async Task<ObjectStorageHead?> HeadAsync(string key,CancellationToken ct){await _clientGate.WaitAsync(ct);try{var stat=await client.StatObjectAsync(new StatObjectArgs().WithBucket(Bucket).WithObject(key),ct);return new(key,stat.Size,stat.ContentType);}catch(ObjectNotFoundException){return null;}catch(MinioException ex)when(ex.Message.Contains("not exist",StringComparison.OrdinalIgnoreCase)){return null;}finally{_clientGate.Release();}}
    public async Task<bool> RemoveUncommittedAsync(string key,CancellationToken ct){await _clientGate.WaitAsync(ct);try{await client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(Bucket).WithObject(key),ct);return true;}catch(MinioException){return false;}finally{_clientGate.Release();}}
    public async Task<string?> GetPresignedReadUrlAsync(string key,int expirySeconds,CancellationToken ct)
    {
        IMinioClient signingClient=ResolveSigningClient();
        await _clientGate.WaitAsync(ct);
        try{return await signingClient.PresignedGetObjectAsync(new PresignedGetObjectArgs().WithBucket(Bucket).WithObject(key).WithExpiry(Math.Clamp(expirySeconds,1,604800)));}
        catch(MinioException){return null;}
        finally{_clientGate.Release();}
    }
    private IMinioClient ResolveSigningClient()
    {
        if(string.IsNullOrWhiteSpace(_settings.PublicEndpoint))return client;
        if(_signingClient is null)
        {
            IMinioClient built=new MinioClient().WithEndpoint(_settings.PublicEndpoint).WithCredentials(_settings.AccessKey,_settings.SecretKey).Build();
            _signingClient=_settings.PublicSecure?built.WithSSL():built;
        }
        return _signingClient;
    }
    private IMinioClient? _signingClient;
    public void Dispose() => _clientGate.Dispose();
}

internal sealed class MinioBucketInitializer(IMinioClient client,IOptions<FileStorageOptions> settings):IHostedService
{
    public async Task StartAsync(CancellationToken ct){string bucket=settings.Value.Bucket;if(!await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket),ct))await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket),ct);}
    public Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
internal sealed class MinioHealthCheck(IMinioClient client,IOptions<FileStorageOptions> settings):IHealthCheck
{
    public async Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken ct=default){try{return await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(settings.Value.Bucket),ct)?HealthCheckResult.Healthy("Private object-storage bucket is available."):HealthCheckResult.Unhealthy("Object-storage bucket is missing.");}catch(Exception ex){return HealthCheckResult.Unhealthy("Object storage is unavailable.",ex);}}
}
