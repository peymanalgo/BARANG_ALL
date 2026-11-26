using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing; using BARANG.Core.Application.Files;
using BARANG.Core.Domain.Files;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BARANG.Core.Api.DesignAudit;

// موتور ممیزی طراحی — قوانین در ERP (منوی «قوانین ممیزی») تعریف می‌شوند و این سرویس
// وضعیت طراحی سایت پی‌رنگ را به‌ازای هر قانون می‌سنجد: پاسخ rule-by-rule با پیام
// بومی‌شده + پاسخ کلی (سبز/قرمز). مدیریت قوانین با مجوز designaudit.manage انجام می‌شود.
public static class DesignAuditEndpoints
{
    public static void MapDesignAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/v1/public/design-audit")
            .RequireRateLimiting("client-read");

        // ارزیابی وضعیت طراحی — فراخوانی دستی از پنل ممیزی یا خودکار هنگام «ادامه»
        group.MapPost("/evaluate", async (DesignAuditRequest r, IConfiguration config, CompanyContext companies,
            ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, CancellationToken ct) =>
        {
            Guid? publicCompany = ResolveCompany(config, "Catalog:PublicCompanyId");
            if (publicCompany is null) return Results.StatusCode(503);
            using IDisposable scope = companies.BeginScope(publicCompany.Value);
            DesignAuditOutcome outcome = await EvaluateAsync(db, storage, publicCompany.Value, r, ct);
            return Results.Json(outcome);
        });

        RouteGroupBuilder admin = endpoints.MapGroup("/api/v1/system/design-audit-rules")
            .RequireAuthorization(SecurityPermissions.DesignAuditManage);

        admin.MapGet("/", async (CompanyContext companies, CoreDbContext db, CancellationToken ct) =>
        {
            Guid companyId = companies.RequireCompanyId();
            List<DesignAuditRule> rules = await db.DesignAuditRules.AsNoTracking()
                .Where(x => x.CompanyId == companyId).OrderBy(x => x.Code).ToListAsync(ct);
            return Results.Json(rules.Select(View).ToArray());
        });

        admin.MapPost("/", async (DesignAuditRuleRequest r, CompanyContext companies, ITransactionExecutor tx,
            CoreDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            Guid companyId = companies.RequireCompanyId();
            Result<DesignAuditRule> result = await tx.ExecuteAsync(async t =>
            {
                if (await db.DesignAuditRules.AnyAsync(x => x.CompanyId == companyId && x.Code == r.Code.Trim(), t))
                    return Result.Failure<DesignAuditRule>(Error.Conflict("designaudit.code_conflict"));
                DesignAuditRule rule = DesignAuditRule.Create(Guid.NewGuid(), companyId, r.Code, r.RuleType,
                    r.TitleFa, r.TitleEn ?? "", r.TitleAr ?? "", r.TitleEs ?? "",
                    JsonSerializer.Serialize(r.Params is JsonElement pe && pe.ValueKind == JsonValueKind.Object ? pe.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()) : new Dictionary<string, JsonElement>()),
                    r.PrintSizeCode, r.IsActive ?? true, DateTimeOffset.UtcNow).Value;
                db.DesignAuditRules.Add(rule);
                audit.Write(new AuditWriteRequest("designaudit.rule_created", "design_audit_rule", rule.Id.ToString(), CompanyId: companyId, ChangeKind: AuditChangeKind.Added));
                return Result.Success(rule);
            }, ct);
            if (result.IsFailure) return Map(result.Error);
            return Results.Json(View(result.Value), statusCode: 201);
        });

        admin.MapPut("/{id:guid}", async (Guid id, DesignAuditRuleRequest r, CompanyContext companies,
            ITransactionExecutor tx, CoreDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            Guid companyId = companies.RequireCompanyId();
            Result<DesignAuditRule> result = await tx.ExecuteAsync(async t =>
            {
                DesignAuditRule? rule = await db.DesignAuditRules.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, t);
                if (rule is null) return Result.Failure<DesignAuditRule>(Error.NotFound("designaudit.rule_not_found"));
                Result update = rule.Update(r.RuleType, r.TitleFa, r.TitleEn ?? "", r.TitleAr ?? "", r.TitleEs ?? "",
                    JsonSerializer.Serialize(r.Params is JsonElement pe && pe.ValueKind == JsonValueKind.Object ? pe.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()) : new Dictionary<string, JsonElement>()),
                    r.PrintSizeCode, r.IsActive ?? true, DateTimeOffset.UtcNow, r.Version);
                return update.IsFailure ? Result.Failure<DesignAuditRule>(update.Error) : Result.Success(rule);
            }, ct);
            if (result.IsFailure) return Map(result.Error);
            audit.Write(new AuditWriteRequest("designaudit.rule_updated", "design_audit_rule", id.ToString(), CompanyId: companyId, ChangeKind: AuditChangeKind.Modified));
            return Results.Json(View(result.Value));
        });

        admin.MapDelete("/{id:guid}", async (Guid id, CompanyContext companies, ITransactionExecutor tx,
            CoreDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            Guid companyId = companies.RequireCompanyId();
            Result result = await tx.ExecuteAsync(async t =>
            {
                DesignAuditRule? rule = await db.DesignAuditRules.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, t);
                if (rule is null) return Result.Failure(Error.NotFound("designaudit.rule_not_found"));
                db.DesignAuditRules.Remove(rule);
                audit.Write(new AuditWriteRequest("designaudit.rule_deleted", "design_audit_rule", id.ToString(), CompanyId: companyId, ChangeKind: AuditChangeKind.Deleted));
                return Result.Success();
            }, ct);
            if (result.IsFailure) return Map(result.Error);
            return Results.NoContent();
        });
    }

    // ── ارزیابی ──
    private static async Task<DesignAuditOutcome> EvaluateAsync(CoreDbContext db, IObjectStorage storage,
        Guid companyId, DesignAuditRequest r, CancellationToken ct)
    {
        List<DesignAuditRule> rules = await db.DesignAuditRules.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.Code).ToListAsync(ct);
        string culture = string.IsNullOrWhiteSpace(r.Culture) ? "fa" : r.Culture.Trim().ToLowerInvariant();
        if (culture is not "fa" and not "en" and not "ar" and not "es") culture = "fa";

        // ابعاد طرح: از اندپوینت (آپلود/AI) یا سنجش فایل اصلی طرح بانک از استوریج
        (int widthPx, int heightPx) = (r.DesignWidthPx ?? 0, r.DesignHeightPx ?? 0);
        string designSourceLabel = "";
        if ((widthPx <= 0 || heightPx <= 0) && r.ArtworkId is { } artworkId)
        {
            (widthPx, heightPx, designSourceLabel) = await MeasureArtworkAsync(db, storage, companyId, artworkId, ct);
        }
        else if (r.ArtworkId is { } id && string.IsNullOrWhiteSpace(designSourceLabel))
        {
            designSourceLabel = r.Source == "bank" ? "bank" : r.Source ?? "custom";
        }

        List<DesignAuditResultRow> rows = [];
        bool passed = true;
        foreach (DesignAuditRule rule in rules)
        {
            Dictionary<string, JsonElement> p = ParseParams(rule.ParamsJson);
            bool ruleApplies = string.IsNullOrWhiteSpace(rule.PrintSizeCode)
                || string.Equals(rule.PrintSizeCode, r.PrintSizeCode?.Trim(), StringComparison.OrdinalIgnoreCase);
            if (!ruleApplies) continue;
            (bool ok, string message, string? fix) = rule.RuleType switch
            {
                "min_resolution" => EvaluateResolution(p, rule, widthPx, heightPx, culture, r.Source ?? ""),
                "text_in_bounds" => EvaluateTextBounds(p, r, culture),
                "contrast" => EvaluateContrast(p, r, culture),
                _ => (true, Msg(culture, "قانون ناشناخته نادیده گرفته شد.", "Unknown rule skipped."), null)
            };
            rows.Add(new(rule.Code, Title(rule, culture), ok, message, fix));
            if (!ok) passed = false;
        }
        if (rows.Count == 0)
            rows.Add(new("none", Msg(culture, "قانون فعالی ثبت نشده است", "No active rules"), true,
                Msg(culture, "هیچ قانون ممیزی فعالی برای بررسی وجود ندارد.", "No active audit rules to evaluate."), null));
        return new DesignAuditOutcome(passed, rows);
    }

    private static async Task<(int, int, string)> MeasureArtworkAsync(CoreDbContext db, IObjectStorage storage,
        Guid companyId, Guid artworkId, CancellationToken ct)
    {
        try
        {
            Guid? versionId = await db.Artworks.AsNoTracking()
                .Where(x => x.Id == artworkId && x.CompanyId == companyId && x.CurrentVersionId != null)
                .Select(x => (Guid?)x.CurrentVersionId!.Value).SingleOrDefaultAsync(ct);
            if (versionId is null) return (0, 0, "");
            Guid fileId = await db.ArtworkVersions.AsNoTracking().Where(x => x.Id == versionId.Value)
                .Select(x => x.FileVersionId).SingleOrDefaultAsync(ct);
            global::BARANG.Core.Domain.Files.FileVersion? file = await db.FileVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, ct);
            if (file is null) return (0, 0, "");
            await using global::System.IO.Stream? stream = await storage.OpenReadAsync(file.StorageKey, ct, file.StorageBucket);
            if (stream is null) return (0, 0, "");
            using var ms = new global::System.IO.MemoryStream();
            await stream.CopyToAsync(ms, ct);
            using var image = new ImageMagick.MagickImage(ms.ToArray());
            return ((int)image.Width, (int)image.Height, "bank");
        }
        catch { return (0, 0, ""); }
    }

    private static (bool, string, string?) EvaluateResolution(Dictionary<string, JsonElement> p, DesignAuditRule rule,
        int widthPx, int heightPx, string culture, string source)
    {
        int minPx = TryGetInt(p, "minPx", 800);
        if (widthPx <= 0 || heightPx <= 0)
            return (true,
                Msg(culture, "ابعاد طرح در دسترس نیست؛ بررسی رزولوشن نادیده گرفته شد.", "Design dimensions unavailable; resolution check skipped."),
                null);
        bool ok = Math.Min(widthPx, heightPx) >= minPx;
        string nums = $"{widthPx}×{heightPx}";
        string msg = ok
            ? Msg(culture,
                $"رزولوشن طرح {nums} پیکسل است — برای چاپ {(rule.PrintSizeCode ?? "")} مناسب است.",
                $"Design resolution is {nums} px — suitable for {(rule.PrintSizeCode ?? "")} printing.")
            : Msg(culture,
                $"رزولوشن طرح {nums} پیکسل است؛ حداقل {minPx} پیکسل برای چاپ {(rule.PrintSizeCode ?? "")} لازم است.",
                $"Design resolution is {nums} px; at least {minPx} px is required for {(rule.PrintSizeCode ?? "")} printing.");
        string? fix = ok ? null : Msg(culture,
            "طرح با رزولوشن بالاتر انتخاب یا آپلود کنید.",
            "Pick or upload a higher-resolution design.");
        return (ok, msg, fix);
    }

    private static (bool, string, string?) EvaluateTextBounds(Dictionary<string, JsonElement> p, DesignAuditRequest r, string culture)
    {
        if (r.Text is null)
            return (true, Msg(culture, "متنی روی طرح قرار ندارد.", "No text layer on the design."), null);
        double marginMm = TryGetDouble(p, "safeMarginMm", 5);
        (double w, double h) = PrintSizeMm(r.PrintSizeCode);
        double safe = marginMm / Math.Min(w, h);
        double l = r.Text.CxPct - r.Text.WPct / 2, rt = r.Text.CxPct + r.Text.WPct / 2;
        double t = r.Text.CyPct - r.Text.HPct / 2, b = r.Text.CyPct + r.Text.HPct / 2;
        bool ok = l >= safe - 0.01 && rt <= 1 - safe + 0.01 && t >= safe - 0.01 && b <= 1 - safe + 0.01;
        string msg = ok
            ? Msg(culture, "متن داخل ناحیه چاپ و حاشیه امن است.", "Text is inside the print area and safe margin.")
            : Msg(culture, $"متن از ناحیه چاپ یا حاشیه امن {marginMm} میلی‌متری بیرون زده است.", $"Text exceeds the print area or the {marginMm} mm safe margin.");
        string? fix = ok ? null : Msg(culture, "متن را کوچک‌تر کنید یا داخل ناحیه چاپ جابجا کنید.", "Shrink the text or move it inside the print area.");
        return (ok, msg, fix);
    }

    private static (bool, string, string?) EvaluateContrast(Dictionary<string, JsonElement> p, DesignAuditRequest r, string culture)
    {
        if (r.Text is null || string.IsNullOrWhiteSpace(r.Text.Hex) || string.IsNullOrWhiteSpace(r.FabricHex))
            return (true, Msg(culture, "کنتراست متن قابل محاسبه نیست (رنگ نامشخص).", "Text contrast could not be computed (unknown colors)."), null);
        double minDelta = TryGetDouble(p, "minDelta", 0.3);
        double delta = Math.Abs(Luminance(r.Text.Hex) - Luminance(r.FabricHex));
        bool ok = delta >= minDelta;
        string msg = ok
            ? Msg(culture, "کنتراست متن با رنگ پارچه مناسب و خواناست.", "Text contrast with the fabric color is readable.")
            : Msg(culture, "کنتراست متن با رنگ پارچه کم است؛ متن به‌سختی خوانده می‌شود.", "Text contrast with the fabric is too low; the text is hard to read.");
        string? fix = ok ? null : Msg(culture, "رنگ متن یا رنگ تیشرت را تغییر دهید.", "Change the text color or the shirt color.");
        return (ok, msg, fix);
    }

    private static (double W, double H) PrintSizeMm(string? code) => (code ?? "").Trim().ToUpperInvariant() switch
    {
        "A3" => (300, 420), "A5" => (140, 210), _ => (210, 297)
    };

    private static double Luminance(string hex)
    {
        try
        {
            string h = hex.TrimStart('#');
            if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}"));
            double r = Convert.ToInt32(h[..2], 16) / 255.0, g = Convert.ToInt32(h.Substring(2, 2), 16) / 255.0, b = Convert.ToInt32(h[4..], 16) / 255.0;
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
        catch { return 0.5; }
    }

    private static string Title(DesignAuditRule rule, string culture) => culture switch
    {
        "en" => string.IsNullOrWhiteSpace(rule.TitleEn) ? rule.TitleFa : rule.TitleEn,
        "ar" => string.IsNullOrWhiteSpace(rule.TitleAr) ? rule.TitleFa : rule.TitleAr,
        "es" => string.IsNullOrWhiteSpace(rule.TitleEs) ? rule.TitleEn is { Length: > 0 } e ? e : rule.TitleFa : rule.TitleEs,
        _ => rule.TitleFa
    };

    private static string Msg(string culture, string fa, string en) => culture switch { "en" => en, "ar" => fa, "es" => en, _ => fa };

    private static int TryGetInt(Dictionary<string, JsonElement> p, string key, int fallback) =>
        p.TryGetValue(key, out JsonElement v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n) ? n : fallback;

    private static double TryGetDouble(Dictionary<string, JsonElement> p, string key, double fallback) =>
        p.TryGetValue(key, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;

    private static Dictionary<string, JsonElement> ParseParams(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            Dictionary<string, JsonElement> map = [];
            foreach (System.Text.Json.JsonProperty p in doc.RootElement.EnumerateObject()) map[p.Name] = p.Value.Clone();
            return map;
        }
        catch { return []; }
    }

    private static Guid? ResolveCompany(IConfiguration config, string key)
    {
        string? configured = config.GetValue<string?>(key);
        return string.IsNullOrWhiteSpace(configured) || !Guid.TryParse(configured, out Guid id) || id == Guid.Empty ? null : id;
    }

    private static IResult Map(Error error)
    {
        var problem = ApiProblemDetailsMapper.Create(error, "designaudit", null);
        return Results.Problem(problem.Detail, problem.Instance, problem.Status, problem.Title, extensions: problem.Extensions);
    }

    private static object View(DesignAuditRule x) => new
    {
        id = x.Id, code = x.Code, ruleType = x.RuleType, titleFa = x.TitleFa, titleEn = x.TitleEn,
        titleAr = x.TitleAr, titleEs = x.TitleEs, printSizeCode = x.PrintSizeCode, isActive = x.IsActive,
        version = x.Version, updatedAtUtc = x.UpdatedAtUtc,
        @params = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(x.ParamsJson) ? "{}" : x.ParamsJson)
    };
}

public sealed record DesignAuditRequest(
    string? PrintSizeCode, string? FabricHex, string? Source, Guid? ArtworkId,
    int? DesignWidthPx, int? DesignHeightPx, string? Culture,
    DesignAuditTextState? Text);

public sealed record DesignAuditTextState(double CxPct, double CyPct, double WPct, double HPct, string? Hex);
public sealed record DesignAuditResultRow(string Code, string Title, bool Passed, string Message, string? FixHint);
public sealed record DesignAuditOutcome(bool Passed, IReadOnlyList<DesignAuditResultRow> Results);

public sealed record DesignAuditRuleRequest(
    string Code, string RuleType, string TitleFa, string? TitleEn, string? TitleAr, string? TitleEs,
    JsonElement? Params, string? PrintSizeCode, bool? IsActive, long Version = 0);
