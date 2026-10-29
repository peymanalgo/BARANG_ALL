using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Companies;
using Microsoft.AspNetCore.Mvc;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Files;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Files;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Api.Files;

public static class PublicCatalogEndpoints
{
    // Anonymous B2C storefront API. Serves only designs of the configured
    // public company whose lifecycle is Active (publishing = activating a
    // design; drafts and archived rows never appear) and only derivative
    // objects from the public bucket - originals have no public path.
    public static void MapPublicCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder catalog=endpoints.MapGroup("/api/v1/public/catalog")
            .RequireRateLimiting("client-read")
            .CacheOutput("public-catalog");

        catalog.MapGet("/",async(int? page,int? pageSize,string? search,string? categoryCode,IConfiguration configuration,CompanyContext companies,ITransactionExecutor tx,CoreDbContext db,IObjectStorage storage,CancellationToken ct)=>
        {
            Guid? publicCompany=ResolvePublicCompany(configuration);
            if(publicCompany is null)return Results.StatusCode(503);
            using IDisposable scope=companies.BeginScope(publicCompany.Value);
            Result<PublicCatalogPage> result=await tx.ExecuteAsync(async t=>{
                IQueryable<Artwork> query=db.Artworks.AsNoTracking().Where(x=>!x.IsArchived&&x.LifecycleStatus==ArtworkLifecycle.Active&&x.CurrentVersionId!=null);
                string term=search?.Trim()??"";
                if(term.Length>0)query=query.Where(x=>EF.Functions.ILike(x.Code,$"%{term}%")||EF.Functions.ILike(x.Title,$"%{term}%"));
                string category=categoryCode?.Trim().ToUpperInvariant()??"";
                if(category.Length>0)query=query.Where(x=>x.CategoryCode==category);
                int size=Math.Clamp(pageSize??24,1,60);
                int pageNumber=Math.Max(1,page??1);
                long total=await query.LongCountAsync(t);
                List<Artwork> items=await query.OrderBy(x=>x.Code).Skip((pageNumber-1)*size).Take(size).ToListAsync(t);
                Guid[] artworkVersionIds=items.Select(x=>x.CurrentVersionId!.Value).ToArray();
                Dictionary<Guid,Guid> fileVersionByArtworkVersion=await db.ArtworkVersions.AsNoTracking().Where(x=>artworkVersionIds.Contains(x.Id)).Select(x=>new KeyValuePair<Guid,Guid>(x.Id,x.FileVersionId)).ToDictionaryAsync(p=>p.Key,p=>p.Value,t);
                Guid[] fileVersionIds=fileVersionByArtworkVersion.Values.ToArray();
                List<(Guid FileVersion,FileDerivativeType Type,string Key)> derivatives=await db.FileDerivatives.AsNoTracking().Where(d=>fileVersionIds.Contains(d.SourceFileVersionId)).Select(d=>new ValueTuple<Guid,FileDerivativeType,string>(d.SourceFileVersionId,d.DerivativeType,d.StorageKey)).ToListAsync(t);
                PublicCatalogItemView[] rows=items.Select(item=>{
                    Guid artworkVersion=item.CurrentVersionId!.Value;
                    if(!fileVersionByArtworkVersion.TryGetValue(artworkVersion,out Guid fileVersion))return new PublicCatalogItemView(item.Id,item.Code,item.Title,item.CategoryCode,null,null);
                    string? thumbnail=derivatives.Where(d=>d.FileVersion==fileVersion&&d.Type==FileDerivativeType.Thumbnail).Select(d=>(string?)d.Key).FirstOrDefault();
                    string? preview=derivatives.Where(d=>d.FileVersion==fileVersion&&(d.Type==FileDerivativeType.PublicWeb||d.Type==FileDerivativeType.Web)).Select(d=>(string?)d.Key).FirstOrDefault();
                    return new PublicCatalogItemView(item.Id,item.Code,item.Title,item.CategoryCode,string.IsNullOrWhiteSpace(thumbnail)?null:storage.GetPublicObjectUrl(thumbnail),string.IsNullOrWhiteSpace(preview)?null:storage.GetPublicObjectUrl(preview));
                }).ToArray();
                return Result.Success(new PublicCatalogPage(total,pageNumber,size,rows));
            },ct);
            return Map(result);
        });

        catalog.MapGet("/categories",async(IConfiguration configuration,CompanyContext companies,ITransactionExecutor tx,CoreDbContext db,CancellationToken ct)=>
        {
            Guid? publicCompany=ResolvePublicCompany(configuration);
            if(publicCompany is null)return Results.StatusCode(503);
            using IDisposable scope=companies.BeginScope(publicCompany.Value);
            Result<IReadOnlyList<PublicCatalogCategoryView>> result=await tx.ExecuteAsync(async t=>{
                List<PublicCatalogCategoryView> rows=await db.Artworks.AsNoTracking()
                    .Where(x=>!x.IsArchived&&x.LifecycleStatus==ArtworkLifecycle.Active&&x.CurrentVersionId!=null&&x.CategoryCode!=null&&x.CategoryCode!="")
                    .GroupBy(x=>x.CategoryCode!)
                    .OrderBy(x=>x.Key)
                    .Select(x=>new PublicCatalogCategoryView(x.Key,x.Count()))
                    .ToListAsync(t);
                return Result.Success<IReadOnlyList<PublicCatalogCategoryView>>(rows);
            },ct);
            return result.IsSuccess?Results.Ok(result.Value):Results.StatusCode(503);
        });

        catalog.MapGet("/concept-search",async(string? q,int? limit,IConfiguration configuration,CompanyContext companies,ITransactionExecutor tx,CoreDbContext db,IObjectStorage storage,IArtworkFileAdministration artworks,CancellationToken ct)=>
        {
            Guid? publicCompany=ResolvePublicCompany(configuration);
            if(publicCompany is null)return Results.StatusCode(503);
            using IDisposable scope=companies.BeginScope(publicCompany.Value);
            Result<IReadOnlyList<ArtworkConceptMatch>> matches=await artworks.ConceptSearchArtworksAsync(q,Math.Clamp(limit??24,1,60),ct);
            if(matches.IsFailure)return Results.StatusCode(503);
            // B2C storefront serves published (active) designs only
            ArtworkConceptMatch[] active=matches.Value.Where(x=>!x.IsArchived&&string.Equals(x.LifecycleStatus,"Active",StringComparison.OrdinalIgnoreCase)).ToArray();
            IReadOnlyList<PublicConceptMatch> rows=(await tx.ExecuteAsync<IReadOnlyList<PublicConceptMatch>>(async t=>{
                Guid[] ids=active.Select(x=>x.Id).ToArray();
                Dictionary<Guid,(Guid? FileVersion,string Title)> byId=await db.Artworks.AsNoTracking().Where(x=>ids.Contains(x.Id)).Select(x=>new KeyValuePair<Guid,ValueTuple<Guid?,string>>(x.Id,new ValueTuple<Guid?,string>(x.CurrentVersionId,x.Title))).ToDictionaryAsync(p=>p.Key,p=>p.Value,t);
                Guid[] artworkVersionIds=byId.Values.Where(x=>x.FileVersion is not null).Select(x=>x.FileVersion!.Value).ToArray();
                Dictionary<Guid,Guid> fileVersionByArtworkVersion=await db.ArtworkVersions.AsNoTracking().Where(x=>artworkVersionIds.Contains(x.Id)).Select(x=>new KeyValuePair<Guid,Guid>(x.Id,x.FileVersionId)).ToDictionaryAsync(p=>p.Key,p=>p.Value,t);
                Guid[] fileVersionIds=fileVersionByArtworkVersion.Values.ToArray();
                List<(Guid FileVersion,FileDerivativeType Type,string Key)> derivatives=await db.FileDerivatives.AsNoTracking().Where(d=>fileVersionIds.Contains(d.SourceFileVersionId)).Select(d=>new ValueTuple<Guid,FileDerivativeType,string>(d.SourceFileVersionId,d.DerivativeType,d.StorageKey)).ToListAsync(t);
                IReadOnlyList<PublicConceptMatch> rows=active.Select(m=>{
                    string? thumbnail=null,preview=null;
                    if(byId.TryGetValue(m.Id,out var meta)&&meta.FileVersion is Guid av&&fileVersionByArtworkVersion.TryGetValue(av,out Guid fv))
                    {
                        thumbnail=derivatives.Where(d=>d.FileVersion==fv&&d.Type==FileDerivativeType.Thumbnail).Select(d=>(string?)d.Key).FirstOrDefault();
                        preview=derivatives.Where(d=>d.FileVersion==fv&&(d.Type==FileDerivativeType.PublicWeb||d.Type==FileDerivativeType.Web)).Select(d=>(string?)d.Key).FirstOrDefault();
                    }
                    return new PublicConceptMatch(m.Id,m.Code,m.Title,m.CategoryName,string.IsNullOrWhiteSpace(thumbnail)?null:storage.GetPublicObjectUrl(thumbnail),string.IsNullOrWhiteSpace(preview)?null:storage.GetPublicObjectUrl(preview),m.Score);
                }).ToArray();
                return Result.Success(rows);
            },ct)).Value;
            return Results.Ok(rows);
        });

        catalog.MapGet("/{id:guid}",async(Guid id,IConfiguration configuration,CompanyContext companies,ITransactionExecutor tx,CoreDbContext db,IObjectStorage storage,CancellationToken ct)=>
        {
            Guid? publicCompany=ResolvePublicCompany(configuration);
            if(publicCompany is null)return Results.StatusCode(503);
            using IDisposable scope=companies.BeginScope(publicCompany.Value);
            Result<PublicCatalogItemDetail> result=await tx.ExecuteAsync(async t=>{
                Artwork? artwork=await db.Artworks.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&!x.IsArchived&&x.LifecycleStatus==ArtworkLifecycle.Active&&x.CurrentVersionId!=null,t);
                if(artwork?.CurrentVersionId is not Guid artworkVersion)return Result.Failure<PublicCatalogItemDetail>(FileErrors.NotFound);
                Guid? fileVersionId=await db.ArtworkVersions.AsNoTracking().Where(x=>x.Id==artworkVersion).Select(x=>(Guid?)x.FileVersionId).SingleOrDefaultAsync(t);
                if(fileVersionId is null)return Result.Failure<PublicCatalogItemDetail>(FileErrors.NotFound);
                List<(FileDerivativeType Type,string Key)> derivatives=await db.FileDerivatives.AsNoTracking().Where(d=>d.SourceFileVersionId==fileVersionId).Select(d=>new ValueTuple<FileDerivativeType,string>(d.DerivativeType,d.StorageKey)).ToListAsync(t);
                string? thumbnail=derivatives.Where(d=>d.Type==FileDerivativeType.Thumbnail).Select(d=>(string?)d.Key).FirstOrDefault();
                string? preview=derivatives.Where(d=>d.Type is FileDerivativeType.PublicWeb or FileDerivativeType.Web).Select(d=>(string?)d.Key).FirstOrDefault();
                return Result.Success(new PublicCatalogItemDetail(artwork.Id,artwork.Code,artwork.Title,artwork.CategoryCode,artwork.Description,string.IsNullOrWhiteSpace(thumbnail)?null:storage.GetPublicObjectUrl(thumbnail),string.IsNullOrWhiteSpace(preview)?null:storage.GetPublicObjectUrl(preview)));
            },ct);
            return Map(result);
        });
    }

    private static Guid? ResolvePublicCompany(IConfiguration configuration)
    {
        string? configured=configuration.GetValue<string?>("Catalog:PublicCompanyId");
        return string.IsNullOrWhiteSpace(configured)||!Guid.TryParse(configured,out Guid publicCompany)||publicCompany==Guid.Empty?null:publicCompany;
    }

    private static IResult Map<T>(Result<T> result)=>result.IsSuccess?Results.Json(result.Value):Problem(result.Error);

    private static IResult Problem(Error error)
    {
        ProblemDetails problem=ApiProblemDetailsMapper.Create(error,"unavailable",null);
        return Results.Problem(problem.Detail,problem.Instance,problem.Status,problem.Title,extensions:problem.Extensions);
    }
}

public sealed record PublicCatalogItemView(Guid Id,string Code,string Title,string?CategoryCode,string?ThumbnailUrl,string?PreviewUrl);
public sealed record PublicCatalogItemDetail(Guid Id,string Code,string Title,string?CategoryCode,string?Description,string?ThumbnailUrl,string?PreviewUrl);
public sealed record PublicCatalogPage(long Total,int Page,int PageSize,IReadOnlyList<PublicCatalogItemView> Items);
public sealed record PublicCatalogCategoryView(string Code,int Count);
public sealed record PublicConceptMatch(Guid Id,string Code,string Title,string?CategoryName,string?ThumbnailUrl,string?PreviewUrl,int Score);
