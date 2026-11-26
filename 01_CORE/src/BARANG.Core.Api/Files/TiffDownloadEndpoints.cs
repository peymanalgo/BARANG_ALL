using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Files;
using BARANG.Core.Domain.Security; using BARANG.Core.Domain.Common;
using Microsoft.AspNetCore.Builder;
using ImageMagick;

namespace BARANG.Core.Api.Files;

// دانلود فایل چاپ با فرمت TIFF — برای کارتابل چاپ و سایر مراحل تولید.
// فایل (طرح، یا تصویر ترکیبی طرح+متن) در لحظه دانلود به TIFF با کیفیت بالا
// (LZW بدون افت + ۳۰۰ DPI) تبدیل می‌شود تا مستقیم قابل چاپ باشد.
public static class TiffDownloadEndpoints
{
    public static void MapTiffDownloadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder fileRead = endpoints.MapGroup("/api/v1/files")
            .RequireAuthorization(SecurityPermissions.FileRead);
        fileRead.MapGet("/{id:guid}/versions/{versionId:guid}/download-tiff", async (Guid id, Guid versionId,
            HttpContext http, IArtworkFileAdministration s, CancellationToken ct) =>
        {
            Result<DownloadFile> result = await s.DownloadAsync(id, versionId, ct);
            if (result.IsFailure) return Problem(result.Error);
            DownloadFile file = result.Value;
            await using MemoryStream source = new();
            await file.Content.CopyToAsync(source, ct);
            source.Position = 0;
            using var image = new MagickImage(source);
            // کیفیت چاپ بالا: LZW بدون افت + ۳۰۰ DPI
            image.Format = MagickFormat.Tiff;
            image.SetCompression(CompressionMethod.LZW);
            image.Density = new Density(300, 300);
            image.Quality = 100;
            byte[] tiffBytes = image.ToByteArray();
            string fileName = Path.GetFileNameWithoutExtension(file.FileName) + ".tiff";
            http.Response.Headers.CacheControl = "no-store";
            return Results.File(tiffBytes, "image/tiff", fileDownloadName: fileName, enableRangeProcessing: false);
        });
    }

    private static IResult Problem(Error error)
    {
        var problem = ApiProblemDetailsMapper.Create(error, "files", null);
        return Results.Problem(problem.Detail, problem.Instance, problem.Status, problem.Title, extensions: problem.Extensions);
    }
}
