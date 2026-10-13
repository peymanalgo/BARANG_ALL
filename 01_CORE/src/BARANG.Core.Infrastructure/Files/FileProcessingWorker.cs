using System.Collections.Concurrent;
using System.Security.Cryptography;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Files;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Files;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#pragma warning disable CA1711

namespace BARANG.Core.Infrastructure.Files;

public interface IFileProcessingQueue { void Enqueue(Guid companyId,Guid fileVersionId); ValueTask<(Guid CompanyId,Guid FileVersionId)> DequeueAsync(CancellationToken ct); }
public sealed class FileProcessingQueue:IFileProcessingQueue
{
    private readonly System.Threading.Channels.Channel<(Guid,Guid)>_values=System.Threading.Channels.Channel.CreateUnbounded<(Guid,Guid)>();public void Enqueue(Guid companyId,Guid fileVersionId)=>_values.Writer.TryWrite((companyId,fileVersionId));public ValueTask<(Guid CompanyId,Guid FileVersionId)>DequeueAsync(CancellationToken ct)=>_values.Reader.ReadAsync(ct);
}
internal sealed class FileProcessingWorker(IServiceScopeFactory scopes,IFileProcessingQueue queue,CompanyContext companies):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){while(!stoppingToken.IsCancellationRequested){(Guid company,Guid version)=await queue.DequeueAsync(stoppingToken);using IDisposable context=companies.BeginScope(company);await using AsyncServiceScope scope=scopes.CreateAsyncScope();CoreDbContext db=scope.ServiceProvider.GetRequiredService<CoreDbContext>();ITransactionExecutor tx=scope.ServiceProvider.GetRequiredService<ITransactionExecutor>();IObjectStorage storage=scope.ServiceProvider.GetRequiredService<IObjectStorage>();IAuditWriter audit=scope.ServiceProvider.GetRequiredService<IAuditWriter>();try{Result<(FileVersion Version,FileProcessingJob Job)> loaded=await tx.ExecuteAsync(async ct=>{FileVersion?v=await db.FileVersions.SingleOrDefaultAsync(x=>x.Id==version,ct);FileProcessingJob?j=await db.FileProcessingJobs.SingleOrDefaultAsync(x=>x.FileVersionId==version,ct);return v is null||j is null?Result.Failure<(FileVersion,FileProcessingJob)>(FileErrors.NotFound):Result.Success((v,j));},stoppingToken);if(loaded.IsFailure)continue;FileVersion file=loaded.Value.Version;FileProcessingJob job=loaded.Value.Job;if(file.MediaType is not("image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp" or "image/tiff")){await tx.ExecuteAsync(ct=>{job.Skip(DateTimeOffset.UtcNow);return Task.FromResult(Result.Success());},stoppingToken);continue;}await tx.ExecuteAsync(ct=>{job.Start(DateTimeOffset.UtcNow);return Task.FromResult(Result.Success());},stoppingToken);await using Stream?source=await storage.OpenReadAsync(file.StorageKey,stoppingToken);if(source is null)throw new InvalidOperationException("source_missing");using MagickImage image=new(source);image.AutoOrient();image.Thumbnail(320,320);image.Strip();image.Format=MagickFormat.Png;using MemoryStream output=new();await image.WriteAsync(output,stoppingToken);byte[]bytes=output.ToArray();string hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();string key=storage.CreateKey(company,file.FileId,Guid.NewGuid(),"png",true);await using MemoryStream upload=new(bytes,false);await storage.PutAsync(new(key,upload,bytes.Length,"image/png"),stoppingToken);await tx.ExecuteAsync(ct=>{db.FileDerivatives.Add(FileDerivative.Create(company,version,key,"image/png",bytes.Length,hash,DateTimeOffset.UtcNow));job.Succeed(DateTimeOffset.UtcNow);audit.Write(new("file.thumbnail.created","file_version",version.ToString(),ChangeKind:AuditChangeKind.Added));audit.Write(new("file.processing.succeeded","file_version",version.ToString(),ChangeKind:AuditChangeKind.Modified));return Task.FromResult(Result.Success());},stoppingToken);}catch(Exception){await tx.ExecuteAsync(async ct=>{FileProcessingJob?job=await db.FileProcessingJobs.SingleOrDefaultAsync(x=>x.FileVersionId==version,ct);job?.Fail("thumbnail.processing_failed",DateTimeOffset.UtcNow);audit.Write(new("file.processing.failed","file_version",version.ToString(),FailureCode:"thumbnail.processing_failed",ChangeKind:AuditChangeKind.Modified));return Result.Success();},CancellationToken.None);}}}
}
