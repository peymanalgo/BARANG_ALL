using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Ai;
using BARANG.Core.Application.Files;
using BARANG.Core.Domain.Ai;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>تولید تصویر با هوش مصنوعی برای ویزارد طراحی فروشگاه B2C:
/// ۱) مشتری فقط سرور-side از نام کاربری توکن (== کد مشتری) حل می‌شود؛
/// ۲) سهمیه ۵ پرامپت در پنجره غلتان ۲۴ ساعته روی ردیف‌های ai_design_jobs پیش از فراخوانی سرویس تصویر کنترل می‌شود؛
/// ۳) تصویر از API تصویر Z.AI (cogview-3-flash، سازگار با OpenAI images/generations) گرفته، دانلود و
/// در باکت عمومی MinIO ذخیره می‌شود تا لینکش مستقیم برای مرورگر مشتری قابل استفاده باشد؛
/// ۴) پیام‌ها به زبان کاربر (fa/en/ar/es).</summary>
public sealed class AiDesignService(
    CoreDbContext db,
    ICompanyContext company,
    ICurrentActor actor,
    ITransactionExecutor transactions,
    IObjectStorage storage,
    IOptions<FileStorageOptions> fileOptions,
    IHttpClientFactory httpClientFactory,
    TimeProvider time,
    IConfiguration configuration,
    ILogger<AiDesignService> logger) : IAiDesignService
{
    public const int QuotaLimit = AiDesignServiceDefaults.QuotaLimit;
    // مدل پیش‌فرض سرویس تصویر سازگار OpenAI؛ با تنظیم Ai:ImageModel قابل تغییر است (بدون تغییر اسکیما)
    public const string DefaultImageModel = "cogview-3-flash";
    public const string ImageSize = "1024x1024";
    private const string HttpClientName = "ai-llm";
    private const int MaxPromptLength = 1000;
    private static readonly Action<ILogger, Guid, Guid, long, Exception?> DesignGenerated = LoggerMessage.Define<Guid, Guid, long>(LogLevel.Information, new(3113, "AiDesignGenerated"), "AI design generated for customer {CustomerId} in company {CompanyId} ({Bytes} bytes)");
    private static readonly Action<ILogger, string, Exception?> ProviderCallFailed = LoggerMessage.Define<string>(LogLevel.Warning, new(3114, "AiDesignProviderCallFailed"), "AI design provider call failed: {Error}");

    private string EncryptionKey => configuration["Ai:SettingsEncryptionKey"] ?? string.Empty;
    private string ImageModel => configuration["Ai:ImageModel"] is { Length: > 0 } model ? model.Trim() : DefaultImageModel;

    public async Task<Result<AiDesignReply>> DesignAsync(AiDesignRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Result.Failure<AiDesignReply>(AiErrors.AccessDenied);
        Guid companyId = company.RequireCompanyId();
        string culture = string.IsNullOrWhiteSpace(request.Culture) ? "fa" : request.Culture;
        DateTimeOffset now = time.GetUtcNow();

        // حل مشتری فقط سرور-side: نام کاربری توکن == کد مشتری (قرارداد پورتال B2C)؛ داخل ترنزاکشن با RLS سشن.
        Domain.Customers.Customer? customer = await ResolveCustomerAsync(ct);
        if (customer is null) return Result.Failure<AiDesignReply>(AiErrors.AccessDenied);
        Guid customerId = customer.Id;

        string prompt = (request.Prompt ?? "").Trim();
        if (prompt.Length is < 1 or > MaxPromptLength) return Result.Failure<AiDesignReply>(AiErrors.SettingsInvalid);

        int quotaUsed = await db.AiDesignJobs.CountAsync(j => j.CustomerId == customerId && j.CreatedAtUtc >= now.AddHours(-24), ct);
        if (quotaUsed >= QuotaLimit)
            return Result.Success(AiDesignReply.Exceeded(quotaUsed, QuotaLimit, QuotaExceededText(culture, quotaUsed)));

        AiModelSettings? settings = await db.AiModelSettings.FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsEnabled)
            return Result.Success(AiDesignReply.Failed("ai.settings_invalid", NotConfiguredText(culture)));

        string? apiKey = AiSecretCipher.TryDecrypt(settings.ApiKeyCipher, EncryptionKey);
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result.Success(AiDesignReply.Failed("ai.settings_invalid", NotConfiguredText(culture)));

        // ۱) تولید تصویر با سرویس تصویر سازگار OpenAI (Z.AI /images/generations)
        string? providerUrl = await GenerateImageAsync(settings.BaseUrl, apiKey, prompt, TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 30, 300)), ct);
        if (providerUrl is null)
            return Result.Success(AiDesignReply.Failed("ai.provider_error", ProviderErrorText(culture)));

        // ۲) دانلود بایت‌های تصویر و ذخیره در باکت عمومی (الگوی SiteContentAdministration.UploadImageAsync)
        byte[] imageBytes = await DownloadImageAsync(providerUrl, ct);
        if (imageBytes.Length == 0)
            return Result.Success(AiDesignReply.Failed("ai.provider_error", ProviderErrorText(culture)));

        string key = $"companies/{companyId}/ai-designs/{Guid.NewGuid():N}.png";
        string? publicBucket = fileOptions.Value.PublicBucket;
        await using MemoryStream imageStream = new(imageBytes);
        await storage.PutAsync(new ObjectStorageWrite(key, imageStream, imageBytes.LongLength, "image/png", "public,max-age=31536000,immutable",
            string.IsNullOrWhiteSpace(publicBucket) ? null : publicBucket), ct);
        string publicUrl = storage.GetPublicObjectUrl(key);

        // ۳) ثبت کار برای سهمیه و ردیابی
        db.AiDesignJobs.Add(AiDesignJob.Create(Guid.NewGuid(), companyId, customerId, prompt, publicUrl, now));
        await db.SaveChangesAsync(ct);
        quotaUsed++;

        DesignGenerated(logger, customerId, companyId, imageBytes.LongLength, null);
        return Result.Success(AiDesignReply.Made(publicUrl, quotaUsed, QuotaLimit, ReadyText(culture)));
    }

    /// <summary>شناسه مشتری فقط از توکن (نام کاربری == کد مشتری)؛ ورودی کلاینت هرگز استفاده نمی‌شود.
    /// داخل ITransactionExecutor اجرا می‌شود تا متغیر سشن RLS (barang.company_id) تنظیم باشد.</summary>
    private async Task<Domain.Customers.Customer?> ResolveCustomerAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor.Actor.ActorUsername)) return null;
        Result<Domain.Customers.Customer?> lookup = await transactions.ExecuteAsync(async t =>
            Result.Success(await db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerCode == actor.Actor.ActorUsername, t)), ct);
        return lookup.IsSuccess ? lookup.Value : null;
    }

    /// <summary>سهمیه باقی‌مانده برای نمایش در پاپ‌آپ — هیچ پرامپتی مصرف نمی‌کند.</summary>
    public async Task<Result<AiDesignReply>> QuotaAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Result.Failure<AiDesignReply>(AiErrors.AccessDenied);
        Domain.Customers.Customer? customer = await ResolveCustomerAsync(ct);
        if (customer is null) return Result.Failure<AiDesignReply>(AiErrors.AccessDenied);
        int quotaUsed = await db.AiDesignJobs.CountAsync(j => j.CustomerId == customer.Id && j.CreatedAtUtc >= time.GetUtcNow().AddHours(-24), ct);
        return Result.Success(new AiDesignReply(false, quotaUsed >= QuotaLimit, null, quotaUsed, QuotaLimit, Math.Max(0, QuotaLimit - quotaUsed), null, null));
    }

    /// <summary>فراخوانی POST {base_url}/images/generations — پاسخ موفق شامل data[0].url است.
    /// مدل تصویر ثابت cogview-3-flash است (بدون تغییر اسکیما)؛ base_url و کلید از ai_model_settings می‌آید.</summary>
    private async Task<string?> GenerateImageAsync(string baseUrl, string apiKey, string prompt, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            using HttpClient http = httpClientFactory.CreateClient(HttpClientName);
            using HttpRequestMessage request = new(HttpMethod.Post, BuildUrl(baseUrl, "images/generations"));
            request.Content = JsonContent.Create(new JsonObject
            {
                ["model"] = ImageModel,
                ["prompt"] = prompt,
                ["size"] = ImageSize,
            });            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            using HttpResponseMessage response = await http.SendAsync(request, cts.Token);
            string body = await response.Content.ReadAsStringAsync(cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                LogProviderError($"HTTP {(int)response.StatusCode}: {Trim(body)}");
                return null;
            }
            JsonNode? root = JsonNode.Parse(body);
            return root?["data"]?[0]?["url"]?.GetValue<string>();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            LogProviderError("timeout");
            return null;
        }
        catch (HttpRequestException exception)
        {
            LogProviderError(exception.Message);
            return null;
        }
        catch (JsonException exception)
        {
            LogProviderError("invalid provider response: " + exception.Message);
            return null;
        }
        catch (TaskCanceledException)
        {
            LogProviderError("timeout");
            return null;
        }
    }

    private async Task<byte[]> DownloadImageAsync(string url, CancellationToken ct)
    {
        try
        {
            using HttpClient http = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogProviderError($"image download HTTP {(int)response.StatusCode}");
                return [];
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or TaskCanceledException)
        {
            LogProviderError("image download failed: " + exception.Message);
            return [];
        }
    }

    private void LogProviderError(string error) => ProviderCallFailed.Invoke(logger, Trim(error), null);

    private static string BuildUrl(string baseUrl, string path)
    {
        string trimmed = baseUrl.Trim().TrimEnd('/');
        return trimmed.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase) ? trimmed : trimmed + "/" + path.TrimStart('/');
    }

    private static string Trim(string value) => value.Length <= 400 ? value : value[..400];

    // ── پیام‌های چهارزبانه ──
    public static string QuotaExceededText(string culture, int used) => IsFa(culture)
        ? $"سهمیه طراحی با هوش مصنوعی امروز ({FaDigits(used)} از ۵ پرامپت در ۲۴ ساعت) تمام شد؛ فردا دوباره امتحان کن."
        : IsAr(culture)
            ? $"انتهت حصة التصميم بالذكاء الاصطناعي اليوم ({used} من 5 مطالبات كل 24 ساعة)؛ حاول غدًا."
            : IsEs(culture)
                ? $"Se agotó tu cupo de diseño con IA de hoy ({used} de 5 prompts en 24 horas); inténtalo mañana."
                : $"Today's AI design quota is used up ({used} of 5 prompts per 24 hours); please try again tomorrow.";

    public static string NotConfiguredText(string culture) => IsFa(culture)
        ? "امکان طراحی با هوش مصنوعی فعلاً فعال نیست؛ بعداً تلاش کن."
        : IsAr(culture)
            ? "ميزة التصميم بالذكاء الاصطناعي غير مفعّلة حالياً؛ حاول لاحقاً."
            : IsEs(culture)
                ? "El diseño con IA no está disponible en este momento; inténtalo más tarde."
                : "AI design is not available right now; please try again later.";

    public static string ProviderErrorText(string culture) => IsFa(culture)
        ? "تولید تصویر با خطا مواجه شد؛ چند لحظه بعد دوباره تلاش کن."
        : IsAr(culture)
            ? "حدث خطأ أثناء توليد الصورة؛ حاول بعد قليل."
            : IsEs(culture)
                ? "La generación de la imagen falló; inténtalo de nuevo en unos momentos."
                : "Image generation failed; please try again in a few moments.";

    public static string ReadyText(string culture) => IsFa(culture)
        ? "طرح شما آماده شد!"
        : IsAr(culture)
            ? "تصميمك جاهز!"
            : IsEs(culture)
                ? "¡Tu diseño está listo!"
                : "Your design is ready!";

    private static bool IsFa(string culture) => culture.StartsWith("fa", StringComparison.Ordinal);
    private static bool IsAr(string culture) => culture.StartsWith("ar", StringComparison.Ordinal);
    private static bool IsEs(string culture) => culture.StartsWith("es", StringComparison.Ordinal);

    private static string FaDigits(int value) => string.Create(value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length, value,
        static (span, n) => { string s = n.ToString(System.Globalization.CultureInfo.InvariantCulture); for (int i = 0; i < s.Length; i++) span[i] = "۰۱۲۳۴۵۶۷۸۹"[s[i] - '0']; });
}
