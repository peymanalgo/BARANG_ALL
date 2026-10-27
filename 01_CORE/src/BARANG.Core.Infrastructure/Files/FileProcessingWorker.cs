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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
#pragma warning disable CA1711

namespace BARANG.Core.Infrastructure.Files;

public interface IFileProcessingQueue { void Enqueue(Guid companyId,Guid fileVersionId); ValueTask<(Guid CompanyId,Guid FileVersionId)> DequeueAsync(CancellationToken ct); }
public sealed class FileProcessingQueue:IFileProcessingQueue
{
    private readonly System.Threading.Channels.Channel<(Guid,Guid)>_values=System.Threading.Channels.Channel.CreateUnbounded<(Guid,Guid)>();public void Enqueue(Guid companyId,Guid fileVersionId)=>_values.Writer.TryWrite((companyId,fileVersionId));public ValueTask<(Guid CompanyId,Guid FileVersionId)>DequeueAsync(CancellationToken ct)=>_values.Reader.ReadAsync(ct);
}
internal sealed class FileProcessingWorker(IServiceScopeFactory scopes,IFileProcessingQueue queue,CompanyContext companies,ILogger<FileProcessingWorker> logger,IOptions<FileStorageOptions> optionsAccessor):BackgroundService
{
    private static readonly Action<ILogger,Exception?> RecoverySkipped=Microsoft.Extensions.Logging.LoggerMessage.Define(Microsoft.Extensions.Logging.LogLevel.Warning,new Microsoft.Extensions.Logging.EventId(1402,"FileProcessingRecoverySkipped"),"File processing recovery skipped; pending jobs retry on next restart.");
    private static readonly Action<ILogger,int,Exception?> RecoveredPending=Microsoft.Extensions.Logging.LoggerMessage.Define<int>(Microsoft.Extensions.Logging.LogLevel.Information,new Microsoft.Extensions.Logging.EventId(1401,"PendingJobsRecovered"),"File processing recovery re-enqueued {Count} pending thumbnail job(s).");
    private static readonly Action<ILogger,Guid,Exception?> ThumbnailFailed=Microsoft.Extensions.Logging.LoggerMessage.Define<Guid>(Microsoft.Extensions.Logging.LogLevel.Warning,new Microsoft.Extensions.Logging.EventId(1403,"ThumbnailGenerationFailed"),"Thumbnail generation failed for file version {FileVersionId}.");
    private static readonly Action<ILogger,string,Exception?> TraceOutcome=Microsoft.Extensions.Logging.LoggerMessage.Define<string>(Microsoft.Extensions.Logging.LogLevel.Warning,new Microsoft.Extensions.Logging.EventId(1411,"FinalizeOutcome"),"Derivative finalize returned {Code}; retrying once with a fresh scope.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingJobsAsync(stoppingToken);
        int parallelism=Math.Clamp(optionsAccessor.Value.ProcessingParallelism,1,32);
        Task[] consumers=Enumerable.Range(0,parallelism).Select(_=>Task.Run(()=>ConsumeAsync(stoppingToken),stoppingToken)).ToArray();
        await Task.WhenAll(consumers);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            (Guid company,Guid version)=await queue.DequeueAsync(stoppingToken);
            await ProcessAsync(company,version,stoppingToken);
        }
    }

    private async Task ProcessAsync(Guid company,Guid version,CancellationToken stoppingToken)
    {
        using IDisposable context=companies.BeginScope(company);
        await using AsyncServiceScope scope=scopes.CreateAsyncScope();CoreDbContext db=scope.ServiceProvider.GetRequiredService<CoreDbContext>();ITransactionExecutor tx=scope.ServiceProvider.GetRequiredService<ITransactionExecutor>();IObjectStorage storage=scope.ServiceProvider.GetRequiredService<IObjectStorage>();IAuditWriter audit=scope.ServiceProvider.GetRequiredService<IAuditWriter>();
        try{Result<(FileVersion Version,FileProcessingJob Job)> loaded=await tx.ExecuteAsync(async ct=>{FileVersion?v=await db.FileVersions.SingleOrDefaultAsync(x=>x.Id==version,ct);FileProcessingJob?j=await db.FileProcessingJobs.SingleOrDefaultAsync(x=>x.FileVersionId==version,ct);return v is null||j is null?Result.Failure<(FileVersion,FileProcessingJob)>(FileErrors.NotFound):Result.Success((v,j));},stoppingToken);if(loaded.IsFailure)return;FileVersion file=loaded.Value.Version;FileProcessingJob job=loaded.Value.Job;await tx.ExecuteAsync(ct=>{job.Start(DateTimeOffset.UtcNow);return Task.FromResult(Result.Success());},stoppingToken);await using Stream?source=await storage.OpenReadAsync(file.StorageKey,stoppingToken);if(source is null)throw new InvalidOperationException("source_missing");bool declaredImage=file.MediaType is("image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp" or "image/tiff");MagickImage image;Task<MagickImage> decode=Task.Run(()=>new MagickImage(source),stoppingToken);int decodeTimeout=optionsAccessor.Value.DecodeTimeoutSeconds<1?30:optionsAccessor.Value.DecodeTimeoutSeconds;Task finished=await Task.WhenAny(decode,Task.Delay(TimeSpan.FromSeconds(decodeTimeout),stoppingToken));if(finished!=decode){await tx.ExecuteAsync(ct=>{job.Fail("thumbnail.decode_timeout",DateTimeOffset.UtcNow);audit.Write(new("file.processing.failed","file_version",version.ToString(),FailureCode:"thumbnail.decode_timeout",ChangeKind:AuditChangeKind.Modified));return Task.FromResult(Result.Success());},stoppingToken);return;}try{image=decode.Result;}catch(AggregateException ex)when(ex.InnerExceptions.Any(static e=>e is MagickException)&&!declaredImage){await tx.ExecuteAsync(ct=>{job.Skip(DateTimeOffset.UtcNow);return Task.FromResult(Result.Success());},stoppingToken);return;}using(image){Result<List<FileDerivativeType>> existingResult=await tx.ExecuteAsync(async ct=>Result.Success(await db.FileDerivatives.AsNoTracking().Where(d=>d.SourceFileVersionId==version).Select(d=>d.DerivativeType).ToListAsync(ct)),stoppingToken);List<FileDerivativeType> existing=existingResult.Value;List<(FileDerivativeType Type,string Key,string Media,byte[] Bytes,string Hash)> prepared=new();if(!existing.Contains(FileDerivativeType.Thumbnail))prepared.Add(await RenderAsync(company,file,storage,image,FileDerivativeType.Thumbnail,320u,MagickFormat.Png,"image/png","png",0,stoppingToken));if(!existing.Contains(FileDerivativeType.Web))prepared.Add(await RenderAsync(company,file,storage,image,FileDerivativeType.Web,1024u,MagickFormat.WebP,"image/webp","webp",80,stoppingToken));await UploadPreparedAsync(scopes,company,version,prepared,stoppingToken);}}
        catch(Exception ex){ThumbnailFailed(logger,version,ex);await tx.ExecuteAsync(async ct=>{FileProcessingJob?job=await db.FileProcessingJobs.SingleOrDefaultAsync(x=>x.FileVersionId==version,ct);job?.Fail("thumbnail.processing_failed",DateTimeOffset.UtcNow);audit.Write(new("file.processing.failed","file_version",version.ToString(),FailureCode:"thumbnail.processing_failed",ChangeKind:AuditChangeKind.Modified));return Result.Success();},CancellationToken.None);}
    }

    private static async Task<(FileDerivativeType Type,string Key,string Media,byte[] Bytes,string Hash)> RenderAsync(Guid company,FileVersion file,IObjectStorage storage,MagickImage source,FileDerivativeType type,uint box,MagickFormat format,string media,string extension,int quality,CancellationToken ct)
    {
        using MagickImage variant=(MagickImage)source.Clone();
        variant.AutoOrient();
        if(variant.BaseWidth>box||variant.BaseHeight>box)variant.Thumbnail(box,box);
        variant.Strip();
        variant.Format=format;
        if(quality>0)variant.Quality=(uint)quality;
        using MemoryStream output=new();
        await variant.WriteAsync(output,ct);
        byte[]bytes=output.ToArray();
        string hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string key=storage.CreateKey(company,file.FileId,Guid.NewGuid(),extension,true);
        return (type,key,media,bytes,hash);
    }

    private async Task UploadPreparedAsync(IServiceScopeFactory scopeFactory,Guid company,Guid version,List<(FileDerivativeType Type,string Key,string Media,byte[] Bytes,string Hash)> prepared,CancellationToken stoppingToken)
    {
        await using AsyncServiceScope uploadScope=scopeFactory.CreateAsyncScope();IObjectStorage storage=uploadScope.ServiceProvider.GetRequiredService<IObjectStorage>();
        foreach((FileDerivativeType _,string key,string media,byte[] bytes,string _) in prepared)
        {
            await using MemoryStream upload=new(bytes,false);
            await storage.PutAsync(new(key,upload,bytes.Length,media),stoppingToken);
        }
        for(int attempt=1;;attempt++)
        {
            await using AsyncServiceScope scope=scopeFactory.CreateAsyncScope();CoreDbContext db=scope.ServiceProvider.GetRequiredService<CoreDbContext>();ITransactionExecutor tx=scope.ServiceProvider.GetRequiredService<ITransactionExecutor>();IAuditWriter audit=scope.ServiceProvider.GetRequiredService<IAuditWriter>();
            Result outcome=await tx.ExecuteAsync(async ct=>{
                FileProcessingJob? job=await db.FileProcessingJobs.SingleOrDefaultAsync(j=>j.FileVersionId==version,ct);
                if(job is null)return Result.Failure(FileErrors.NotFound);
                if(job.Status==FileProcessingStatus.Succeeded)return Result.Success();
                List<FileDerivativeType> existing=await db.FileDerivatives.AsNoTracking().Where(d=>d.SourceFileVersionId==version).Select(d=>d.DerivativeType).ToListAsync(ct);
                foreach((FileDerivativeType type,string key,string media,byte[] bytes,string hash) in prepared)
                {
                    if(existing.Contains(type))continue;
                    db.FileDerivatives.Add(FileDerivative.Create(company,version,type,key,media,bytes.Length,hash,DateTimeOffset.UtcNow));
                    audit.Write(new(type==FileDerivativeType.Web?"file.webvariant.created":"file.thumbnail.created","file_version",version.ToString(),ChangeKind:AuditChangeKind.Added));
                }
                job.Succeed(DateTimeOffset.UtcNow);
                audit.Write(new("file.processing.succeeded","file_version",version.ToString(),ChangeKind:AuditChangeKind.Modified));
                return Result.Success();
            },stoppingToken);
            if(outcome.IsSuccess)return;
            TraceOutcome(logger,outcome.Error.Code,null);
            if(attempt==1&&outcome.Error.Code is "core.database.concurrency_conflict" or "core.database.unique_conflict")continue;
            await using AsyncServiceScope failScope=scopeFactory.CreateAsyncScope();CoreDbContext failDb=failScope.ServiceProvider.GetRequiredService<CoreDbContext>();ITransactionExecutor failTx=failScope.ServiceProvider.GetRequiredService<ITransactionExecutor>();IAuditWriter failAudit=failScope.ServiceProvider.GetRequiredService<IAuditWriter>();
            await failTx.ExecuteAsync(async ct=>{FileProcessingJob? job=await failDb.FileProcessingJobs.SingleOrDefaultAsync(j=>j.FileVersionId==version,ct);job?.Fail("thumbnail.finalize_failed",DateTimeOffset.UtcNow);failAudit.Write(new("file.processing.failed","file_version",version.ToString(),FailureCode:"thumbnail.finalize_failed",ChangeKind:AuditChangeKind.Modified));return Result.Success();},CancellationToken.None);
            return;
        }
    }

    private async Task RecoverPendingJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope bootstrap=scopes.CreateAsyncScope();CoreDbContext bootstrapDb=bootstrap.ServiceProvider.GetRequiredService<CoreDbContext>();
            List<Guid> companyIds=await bootstrapDb.Companies.AsNoTracking().Select(c=>c.Id).ToListAsync(stoppingToken);
            int recovered=0;
            foreach(Guid companyId in companyIds)
            {
                using IDisposable guard=companies.BeginScope(companyId);
                await using AsyncServiceScope scope=scopes.CreateAsyncScope();CoreDbContext db=scope.ServiceProvider.GetRequiredService<CoreDbContext>();ITransactionExecutor tx=scope.ServiceProvider.GetRequiredService<ITransactionExecutor>();
                Result<List<(Guid,Guid)>> retryable=await tx.ExecuteAsync(async ct=>Result.Success(await db.FileProcessingJobs.AsNoTracking().Where(j=>j.Status==FileProcessingStatus.Pending||j.Status==FileProcessingStatus.Failed&&j.AttemptCount<3&&j.LastErrorCode!="thumbnail.decode_timeout").Select(j=>new ValueTuple<Guid,Guid>(j.CompanyId,j.FileVersionId)).ToListAsync(ct)),stoppingToken);
                if(retryable.IsFailure)continue;
                foreach((Guid c,Guid v) in retryable.Value){queue.Enqueue(c,v);recovered++;}
            }
            if(recovered>0)RecoveredPending(logger,recovered,null);
        }
        catch(OperationCanceledException){throw;}
        catch(Exception ex){RecoverySkipped(logger,ex);}
    }
}
