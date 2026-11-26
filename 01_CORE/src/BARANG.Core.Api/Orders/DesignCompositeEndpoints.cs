using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Files;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Files;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BARANG.Core.Api.Orders;

// ترکیب «تی‌شرت هم‌رنگ محصول + طرح روی سینه» — قرارداد-placement ویزارد پی‌رنگ:
// طرح وسط ناحیه چاپ (۴ انگشت زیر یقه) و وسط‌چین افقی. وقتی کامپوزیت ذخیره‌شدهٔ ویزارد
// (mockup-fv در یادداشت قلم) وجود ندارد یا نامعتبر است (رندر ناموفق مرورگر = فایل تختِ کوچک)،
// همین‌جا سمت سرور با همان هندسه ساخته می‌شود تا مشتری و کارتابل دقیقاً تی‌شرتِ طراحی‌شده را ببینند.
public static class DesignCompositeEndpoints
{
    private const double CanvasW = 600, CanvasH = 680, StageW = 300, StageH = 340;
    private const double CollarTopRatio = 108 / StageH; // لبه بالایی ناحیه چاپ (۴ انگشت زیر یقه)
    private const double PxPerCm = 82 / 21.0;           // A4 = ۲۱cm → ۸۲px استیج (قرارداد ویزارد)
    private const long RealCompositeMinBytes = 50_000;  // کامپوزیت واقعی (عکس تی‌شرت+طرح) همیشه بزرگ‌تر از فایل تختِ رندرِ ناموفق است

    public static IEndpointRouteBuilder MapDesignCompositeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // پرتال مشتری (B2C)
        endpoints.MapGet("/api/v1/client/orders/{orderId:guid}/designs/{fileVersionId:guid}/composite", (Guid orderId, Guid fileVersionId, string? kind, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, IMemoryCache cache, IWebHostEnvironment env, CancellationToken ct) =>
            ServeAsync(orderId, fileVersionId, ParseKind(kind), tx, db, storage, cache, env, ct))
            .RequireAuthorization(SecurityPermissions.ClientOrderRead);
        // کارمندان ERP (کارتابل/سفارش‌ها — پنجره گالری نمای ۳۶۰)
        endpoints.MapGet("/api/v1/orders/{orderId:guid}/360/design-composite/{fileVersionId:guid}", (Guid orderId, Guid fileVersionId, string? kind, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, IMemoryCache cache, IWebHostEnvironment env, CancellationToken ct) =>
            ServeAsync(orderId, fileVersionId, ParseKind(kind), tx, db, storage, cache, env, ct))
            .RequireAuthorization("orders.360.view");
        return endpoints;
    }

    private static string ParseKind(string? kind) => (kind ?? "full").Trim().ToLowerInvariant() switch { "thumb" => "thumb", "tiff" => "tiff", _ => "full" };

    private static async Task<IResult> ServeAsync(Guid orderId, Guid fileVersionId, string kind, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, IMemoryCache cache, IWebHostEnvironment env, CancellationToken ct)
    {
        string cacheKey = $"design-composite:{orderId:N}:{fileVersionId:N}:{kind}";
        if (cache.Get(cacheKey) is CachedComposite hit) return FileResult(hit.Bytes, hit.MediaType, hit.FileName);

        Result<Resolution> resolvedResult = await tx.ExecuteAsync(async t =>
        {
            // قلم سفارشِ صاحبِ این طرح: از طریق عملیات چاپ (فایل آپلودی یا نسخه طرح بانکی)
            var ops = await (from p in db.OrderPrintOperations.AsNoTracking()
                             join av in db.ArtworkVersions.AsNoTracking() on p.ArtworkVersionId equals av.Id into arts
                             from av in arts.DefaultIfEmpty()
                             where p.OrderId == orderId && p.IsActive && (p.FileVersionId == fileVersionId || av.FileVersionId == fileVersionId)
                             select new { p.Id, p.OrderItemId, p.PrintPositionCode, p.Width, p.Height, StoredFv = p.FileVersionId, ArtFv = (Guid?)av.FileVersionId }).ToListAsync(t);
            var op = ops.OrderByDescending(x => x.StoredFv == fileVersionId ? 0 : 1).FirstOrDefault();
            if (op is null) return Result.Failure<Resolution>(FileErrors.NotFound);
            string colorCode = await db.OrderItems.AsNoTracking().Where(x => x.Id == op.OrderItemId)
                .Select(x => x.SnapshotColorCode).FirstOrDefaultAsync(t) ?? "BLACK";
            Guid designFv = op.StoredFv ?? op.ArtFv ?? Guid.Empty;
            if (designFv == Guid.Empty) return Result.Failure<Resolution>(FileErrors.NotFound);
            Guid? storedFv = await StoredMockupFvAsync(db, op.OrderItemId, t);
            // بایت‌ها باید داخل تراکنش خوانده شوند — GUCهای RLS فقط تا پایان تراکنش معتبرند
            byte[]? storedBytes = null; string storedMedia = "image/png";
            if (storedFv is { } sf)
            {
                var stored = await LoadVersionAsync(db, storage, sf, t);
                if (stored is null) return Result.Failure<Resolution>(FileErrors.NotFound);
                storedBytes = stored.Value.Bytes; storedMedia = stored.Value.Media;
            }
            var design = await LoadVersionAsync(db, storage, designFv, t);
            if (design is null) return Result.Failure<Resolution>(FileErrors.NotFound);
            return Result.Success(new Resolution(design.Value.Bytes, storedBytes, storedMedia, op.PrintPositionCode ?? "FRONT", op.Width, op.Height, colorCode));
        }, ct);
        if (!resolvedResult.IsSuccess || resolvedResult.Value is not { } resolved) return Results.NotFound();

        byte[] bytes; string media; string name;
        if (resolved.StoredBytes is { } sb)
        {
            bytes = kind == "thumb" ? Resize(sb, 360) : sb;
            media = resolved.StoredMedia; name = "shirt-mockup.png";
        }
        else
        {
            string teePath = TeePath(env, resolved.ColorCode, resolved.Position);
            if (!File.Exists(teePath)) teePath = TeePath(env, "BLACK", "FRONT");
            bytes = Generate(resolved.DesignBytes, teePath, resolved.PrintWcm, resolved.PrintHcm, resolved.Position, kind);
            media = kind == "tiff" ? "image/tiff" : "image/png";
            name = kind == "tiff" ? "shirt-mockup.tiff" : "shirt-mockup.png";
        }
        cache.Set(cacheKey, new CachedComposite(bytes, media, name), TimeSpan.FromMinutes(30));
        return FileResult(bytes, media, name);
    }

    // مارکر mockup-fv در یادداشت قلم — فقط اگر «واقعی» است (عکس تی‌شرت+طرح، نه فایل تختِ رندرِ ناموفق مرورگر)
    private static async Task<Guid?> StoredMockupFvAsync(CoreDbContext db, Guid orderItemId, CancellationToken t)
    {
        string? notes = await db.OrderItems.AsNoTracking().Where(x => x.Id == orderItemId)
            .Select(x => x.Notes).FirstOrDefaultAsync(t);
        if (string.IsNullOrWhiteSpace(notes)) return null;
        foreach (var part in notes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (part.StartsWith("mockup-fv:", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(part.AsSpan(10), out Guid mfv) && mfv != Guid.Empty)
            {
                long size = await db.FileVersions.AsNoTracking().Where(x => x.Id == mfv && !x.IsArchived).Select(x => (long?)x.SizeBytes).SingleOrDefaultAsync(t) ?? 0;
                return size >= RealCompositeMinBytes ? mfv : null;
            }
        return null;
    }

    private static async Task<(byte[] Bytes, string Media)?> LoadVersionAsync(CoreDbContext db, IObjectStorage storage, Guid fileVersionId, CancellationToken ct)
    {
        var fv = await db.FileVersions.AsNoTracking().Where(x => x.Id == fileVersionId && !x.IsArchived)
            .Select(x => new { x.StorageBucket, x.StorageKey, x.MediaType }).SingleOrDefaultAsync(ct);
        if (fv is null) return null;
        await using Stream? s = await storage.OpenReadAsync(fv.StorageKey, ct, fv.StorageBucket);
        if (s is null) return null;
        await using MemoryStream ms = new();
        await s.CopyToAsync(ms, ct);
        return (ms.ToArray(), fv.MediaType);
    }

    private static byte[] Generate(byte[] designBytes, string teePath, decimal? wcm, decimal? hcm, string position, string kind)
    {
        using MagickImage tee = new(teePath);
        tee.Resize((UInt32)CanvasW, (UInt32)CanvasH);
        // ناحیه چاپ مطابق ویزارد: وسط افقی، لبه بالایی ۴ انگشت زیر یقه، ابعاد از عملیات چاپ (پیش‌فرض A4)
        double rw = Math.Min((double?)(wcm * (decimal)PxPerCm) * (CanvasW / StageW) ?? 164, CanvasW * 0.72);
        double rh = Math.Min((double?)(hcm * (decimal)PxPerCm) * (CanvasH / StageH) ?? 232, CanvasH * 0.62);
        double rx = (CanvasW - rw) / 2, ry = CollarTopRatio * CanvasH;
        using MagickImage design = new(designBytes);
        double sc = Math.Min(rw / design.Width, rh / design.Height);
        design.Resize((UInt32)Math.Max(1, design.Width * sc), (UInt32)Math.Max(1, design.Height * sc));
        if (string.Equals(position, "BACK", StringComparison.OrdinalIgnoreCase)) tee.Flop();
        tee.Composite(design, (int)(rx + (rw - design.Width) / 2), (int)(ry + (rh - design.Height) / 2), CompositeOperator.Over);
        if (kind == "thumb") tee.Resize(360, 0);
        if (kind == "tiff")
        {
            tee.Format = MagickFormat.Tiff;
            tee.SetCompression(CompressionMethod.LZW);
            tee.Density = new Density(300, 300);
            tee.Quality = 100;
            return tee.ToByteArray();
        }
        tee.Format = MagickFormat.Png;
        return tee.ToByteArray();
    }

    private static byte[] Resize(byte[] source, int width)
    {
        using MagickImage img = new(source);
        img.Resize((UInt32)width, 0);
        img.Format = MagickFormat.Png;
        return img.ToByteArray();
    }

    private static string TeePath(IWebHostEnvironment env, string? colorCode, string position)
    {
        string color = (colorCode ?? "").Trim().ToUpperInvariant() switch { "WHITE" or "CREAM" => "white", "RED" or "PINK" => "red", "BLUE" or "PURPLE" => "blue", _ => "black" };
        string side = string.Equals(position, "BACK", StringComparison.OrdinalIgnoreCase) ? "back" : "front";
        string root = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) root = Path.Combine(env.ContentRootPath, "wwwroot");
        return Path.Combine(root, "images", "tee", $"{color}-{side}.jpg");
    }

    private static IResult FileResult(byte[] bytes, string media, string name) => Results.File(bytes, media, fileDownloadName: name);

    private sealed record Resolution(byte[] DesignBytes, byte[]? StoredBytes, string StoredMedia, string Position, decimal? PrintWcm, decimal? PrintHcm, string ColorCode);
    private sealed record CachedComposite(byte[] Bytes, string MediaType, string FileName);
}
