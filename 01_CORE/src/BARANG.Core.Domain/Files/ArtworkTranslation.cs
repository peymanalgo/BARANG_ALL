using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Files;

// Per-culture artwork texts. The base Artwork.Title/Description stay the
// default (fa) values so legacy consumers keep working; en/ar (and an explicit
// fa copy) live here. Cultures: fa | en | ar.
public sealed class ArtworkTranslation
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public static readonly IReadOnlySet<string> SupportedCultures = new HashSet<string>(["fa", "en", "ar"], StringComparer.Ordinal);

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ArtworkId { get; private set; }
    public string Culture { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private ArtworkTranslation() { }

    public static Result<ArtworkTranslation> Create(Guid id, Guid companyId, Guid artworkId, string culture, string title, string? description, DateTimeOffset now)
    {
        string c = (culture ?? "").Trim().ToLowerInvariant();
        if (!SupportedCultures.Contains(c)) return Result.Failure<ArtworkTranslation>(FileErrors.InvalidData);
        string? t = Text(title, TitleMaxLength);
        if (t is null) return Result.Failure<ArtworkTranslation>(FileErrors.InvalidData);
        string? d = Text(description, DescriptionMaxLength);
        return Result.Success(new ArtworkTranslation { Id = id, CompanyId = companyId, ArtworkId = artworkId, Culture = c, Title = t, Description = d, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }

    public Result Update(string title, string? description, DateTimeOffset now)
    {
        string? t = Text(title, TitleMaxLength);
        if (t is null) return Result.Failure(FileErrors.InvalidData);
        string? d = Text(description, DescriptionMaxLength);
        if (t == Title && d == Description) return Result.Success();
        Title = t;
        Description = d;
        UpdatedAtUtc = now;
        Version++;
        return Result.Success();
    }

    internal static string? Text(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string x = value.Trim();
        return x.Length == 0 || x.Length > max ? null : x;
    }
}
