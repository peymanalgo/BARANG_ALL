using System.Text.Json.Nodes;
using BARANG.Core.Application.Ai;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Ai;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>سرویس گفتگوی دستیار: حلقه فراخوانی ابزار (function calling)، اعمال مجوزهای واقعی کاربر،
/// قالب‌بندی تاریخ سرور-side بر اساس فرهنگ و ذخیره تاریخچه و لاگ ممیزی.</summary>
public sealed class AiAssistantService(
    CoreDbContext db,
    ICompanyContext company,
    ICurrentActor actor,
    IPermissionAuthorizationService authorization,
    IAiToolRegistry tools,
    AiLlmClient llm,
    TimeProvider time,
    IConfiguration configuration,
    ILogger<AiAssistantService> logger) : IAiAssistantService
{
    private const int MaxToolIterations = 5;
    private const int MaxHistoryTurns = 20;
    private static readonly Action<ILogger, string, Exception?> ProviderCallFailed = LoggerMessage.Define<string>(LogLevel.Warning, new(3102, "AiProviderCallFailed"), "AI provider call failed: {Error}");

    private string EncryptionKey => configuration["Ai:SettingsEncryptionKey"] ?? string.Empty;

    public async Task<Result<AiAssistantReply>> ChatAsync(AiAssistantRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Result.Failure<AiAssistantReply>(AiErrors.AccessDenied);
        Guid companyId = company.RequireCompanyId();
        string culture = string.IsNullOrWhiteSpace(request.Culture) ? "fa" : request.Culture;
        DateTimeOffset now = time.GetUtcNow();

        AiModelSettings? settings = await db.AiModelSettings.FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsEnabled)
            return Result.Success(AiAssistantReply.NotConfiguredReply(await StoreUserTurnAsync(companyId, userId, request, culture, now, null, ct)));

        AiConversation conversation = await EnsureConversationAsync(companyId, userId, now, request.ResetConversation, ct);
        if (request.ResetConversation)
            await db.AiChatMessages.Where(m => m.ConversationId == conversation.Id).ExecuteDeleteAsync(ct);


        string userMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
        if (userMessage.Length == 0) return Result.Failure<AiAssistantReply>(AiErrors.SettingsInvalid);

        List<AiLlmChatMessage> llmMessages = await BuildLlmMessagesAsync(settings, culture, companyId, userId, conversation.Id, request, prefetchContext: false, ct: ct);

        string reply;
        List<string> toolsUsed = [];
        int promptTokens = 0, completionTokens = 0;
        bool useTools = settings.ToolCallingEnabled;
        for (int iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            List<(string Name, string Description, JsonObject Parameters)>? toolSpecs = useTools && iteration < MaxToolIterations - 1
                ? [.. tools.Tools.Select(t => (t.Name, t.Description, t.ParameterSchema))]
                : null;
            AiLlmChatResult result = await llm.ChatAsync(settings.BaseUrl, AiSecretCipher.TryDecrypt(settings.ApiKeyCipher, EncryptionKey), settings.ModelName, settings.Temperature, settings.MaxOutputTokens, llmMessages, toolSpecs, TimeSpan.FromSeconds(settings.TimeoutSeconds), ct);
            promptTokens += result.PromptTokens;
            completionTokens += result.CompletionTokens;
            if (!result.Success)
            {
                if (useTools && result.Error is not null && LooksLikeToolUnsupported(result.Error))
                {
                    useTools = false;
                    llmMessages = await BuildLlmMessagesAsync(settings, culture, companyId, userId, conversation.Id, request, prefetchContext: true, ct: ct);
                    LogProviderCallFailed(logger, result.Error);
                    continue;
                }
                await PersistTurnAsync(companyId, userId, conversation, culture, userMessage, ProviderErrorText(culture, result.Error), null, null, null, now, ct);
                return Result.Success(new AiAssistantReply(ProviderErrorText(culture, result.Error), false, result.Error, [], promptTokens, completionTokens, await ReadHistoryAsync(conversation.Id, ct)));
            }
            if (result.HasToolCalls)
            {
                llmMessages.Add(AiLlmChatMessage.Assistant(result.Content, result.ToolCalls));
                foreach (AiLlmToolCall call in result.ToolCalls)
                {
                    JsonObject arguments = JsonNode.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson) as JsonObject ?? new JsonObject();
                    JsonObject payload = await ExecuteToolWithSessionAsync(companyId, userId, culture, call.Name, arguments, ct);
                    if (!payload.ContainsKey("error") || payload["error"]?.GetValue<string>() != "access_denied") toolsUsed.Add(call.Name);
                    llmMessages.Add(AiLlmChatMessage.Tool(call.Id, payload.ToJsonString(AiJson.Options), call.Name));
                }
                continue;
            }
            reply = result.Content?.Trim() ?? "";
            if (reply.Length == 0) reply = EmptyReplyText(culture);
            await PersistTurnAsync(companyId, userId, conversation, culture, userMessage, reply, toolsUsed.Count == 0 ? null : string.Join(",", toolsUsed.Distinct()), promptTokens, completionTokens, now, ct);
            return Result.Success(new AiAssistantReply(reply, false, null, [.. toolsUsed.Distinct()], promptTokens, completionTokens, await ReadHistoryAsync(conversation.Id, ct)));
        }
        reply = LoopExhaustedText(culture);
        await PersistTurnAsync(companyId, userId, conversation, culture, userMessage, reply, string.Join(",", toolsUsed.Distinct()), promptTokens, completionTokens, now, ct);
        return Result.Success(new AiAssistantReply(reply, false, "tool_loop_exhausted", [.. toolsUsed.Distinct()], promptTokens, completionTokens, await ReadHistoryAsync(conversation.Id, ct)));
    }

    public async Task<Result<IReadOnlyList<AiAssistantTurnMessage>>> GetHistoryAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Result.Failure<IReadOnlyList<AiAssistantTurnMessage>>(AiErrors.AccessDenied);
        AiConversation? conversation = await db.AiConversations.FirstOrDefaultAsync(c => c.UserId == userId && c.Channel == AiChannels.Staff, ct);
        return Result.Success(conversation is null ? [] : await ReadHistoryAsync(conversation.Id, ct));
    }

    public async Task<Result> ClearConversationAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Result.Failure(AiErrors.AccessDenied);
        AiConversation? conversation = await db.AiConversations.FirstOrDefaultAsync(c => c.UserId == userId && c.Channel == AiChannels.Staff, ct);
        if (conversation is null) return Result.Success();
        await db.AiChatMessages.Where(m => m.ConversationId == conversation.Id).ExecuteDeleteAsync(ct);
        conversation.Touch(time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<AiConversation> EnsureConversationAsync(Guid companyId, Guid userId, DateTimeOffset now, bool reset, CancellationToken ct)
    {
        AiConversation? conversation = await db.AiConversations.FirstOrDefaultAsync(c => c.UserId == userId && c.Channel == AiChannels.Staff, ct);
        if (conversation is not null) return conversation;
        conversation = AiConversation.Create(Guid.NewGuid(), companyId, userId, AiChannels.Staff, now);
        db.AiConversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }

    private async Task<IReadOnlyList<AiAssistantTurnMessage>> StoreUserTurnAsync(Guid companyId, Guid userId, AiAssistantRequest request, string culture, DateTimeOffset now, Guid? conversationId, CancellationToken ct)
    {
        string userMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
        if (userMessage.Length > 0)
        {
            AiConversation conversation = conversationId is { } id
                ? await db.AiConversations.FirstAsync(c => c.Id == id, ct)
                : await EnsureConversationAsync(companyId, userId, now, reset: false, ct);
            db.AiChatMessages.Add(AiChatMessage.Create(Guid.NewGuid(), conversation.Id, companyId, userId, AiChannels.Staff, "user", userMessage, null, null, null, culture, now));
            db.AiAuditEntries.Add(AiAuditEntry.Create(Guid.NewGuid(), companyId, userId, actor.Actor.ActorUsername ?? "", "chat.blocked", Truncate(userMessage, 200), now));
            await db.SaveChangesAsync(ct);
            return await ReadHistoryAsync(conversation.Id, ct);
        }
        return [];
    }

    private async Task<List<AiLlmChatMessage>> BuildLlmMessagesAsync(AiModelSettings settings, string culture, Guid companyId, Guid userId, Guid conversationId, AiAssistantRequest request, bool prefetchContext, CancellationToken ct)
    {
        List<AiLlmChatMessage> messages = [AiLlmChatMessage.System(BuildSystemPrompt(settings, culture, prefetchContext ? await BuildPrefetchedSnapshot(culture, companyId, userId, ct) : null))];
        foreach (AiChatMessage history in await RecentHistoryAsync(conversationId)) messages.Add(new AiLlmChatMessage(history.Role == "assistant" ? "assistant" : "user", history.Content));
        foreach (AiAssistantTurnMessage message in request.Messages.TakeLast(6)) messages.Add(new(message.Role == "assistant" ? "assistant" : "user", message.Content));
        return messages;
    }

    private string BuildSystemPrompt(AiModelSettings settings, string culture, string? prefetchedSnapshot)
    {
        DateOnly today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        string direction = AiCultureFormat.CultureDirection(culture);
        string prompt = $$"""
You are the BARANG ERP backoffice AI assistant embedded in the admin panel. You answer questions about THIS company's live ERP data using the provided tools.

Rules:
- ALWAYS reply in the SAME language and script as the user's latest message (Persian question → Persian answer, English → English, Arabic → Arabic, Spanish → Spanish). Never mix languages except technical codes.
- Dates: today is {{AiCultureFormat.Date(today, culture)}} ({{today:yyyy-MM-dd}} ISO). Tool results contain pre-formatted "text" date fields — prefer those verbatim in your answer.
- When a tool returns {"error":"access_denied"}, do NOT reveal the data: politely tell the user they do not have access to that information, in the user's language.
- Money amounts are in IRT (Iranian Toman) unless a tool says otherwise; format numbers with thousand separators.
- Keep answers concise and operational. Use short bullet lists or compact markdown tables (pipe syntax) when comparing values.
- When a numeric breakdown (counts per category, totals over time, shares) is the core of the answer, ALSO emit one fenced chart block exactly in this format (no other text inside the fence):
  ```chart
  {"type":"bar","title":"<title>","labels":["A","B"],"series":[{"name":"<series>","data":[1,2]}]}
  ```
  Allowed types: bar, line, pie. One chart block maximum per answer.
- Scope discipline: "today" tools only cover TODAY. For questions about totals/all-time (e.g. "تعداد کل سفارش‌ها", "how many orders in total"), call orders_overview (or another all-time tool) — NEVER infer totals from today-only results or claim zero orders just because today is empty.
- If data is empty or zero, say so plainly instead of inventing numbers.
""";
        if (!string.IsNullOrWhiteSpace(settings.SystemPromptOverride)) prompt += "\nAdditional operator instructions:\n" + settings.SystemPromptOverride + "\n";
        if (prefetchedSnapshot is not null) prompt += "\nLive ERP snapshot for this user (the ONLY data source; no tools are available):\n" + prefetchedSnapshot + "\n";
        return prompt;
    }

    /// <summary>حالت جایگزین وقتی provider از function-calling پشتیبانی نمی‌کند: پیش‌واکشی مجاز‌به‌نمایش داده‌ها به‌صورت یک سند.</summary>
    private async Task<string> BuildPrefetchedSnapshot(string culture, Guid companyId, Guid userId, CancellationToken ct)
    {
        AiToolContext context = await BuildContextAsync(culture, ct);
        JsonObject snapshot = new();
        foreach (IAiTool tool in tools.Tools)
        {
            bool allowed = tool.AnyOfPermissions.Count == 0;
            foreach (string permission in tool.AnyOfPermissions) allowed = allowed || await context.CanUseAsync(permission, ct);
            if (!allowed) continue;
            try { snapshot[tool.Name] = await tool.ExecuteAsync(context, null, ct); }
            catch (Exception exception) { snapshot[tool.Name] = new JsonObject { ["error"] = Truncate(exception.Message, 120) }; }
        }
        return snapshot.ToJsonString(AiJson.Options);
    }

    // ابزارها مستقیم از DbContext کوئری می‌گیرند؛ RLS جداول ERP به متغیر سشن barang.company_id وابسته است.
    // اتصال را باز نگه می‌داریم تا متغیر سشن در تمام کوئری‌های ابزار در طول اجرا باقی بماند.
    private async Task<JsonObject> ExecuteToolWithSessionAsync(Guid companyId, Guid userId, string culture, string toolName, JsonObject arguments, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT set_config('barang.company_id', {0}, false), set_config('barang.user_id', {1}, false)", companyId.ToString(), userId.ToString());
            return await ExecuteToolGuardedAsync(companyId, userId, culture, toolName, arguments, ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<JsonObject> ExecuteToolGuardedAsync(Guid companyId, Guid userId, string culture, string toolName, JsonObject arguments, CancellationToken ct)
    {
        IAiTool? tool = tools.Find(toolName);
        if (tool is null) return new JsonObject { ["error"] = "unknown_tool", ["message"] = $"Tool '{toolName}' is not registered." };
        AiToolContext context = await BuildContextAsync(culture, ct);
        if (tool.AnyOfPermissions.Count > 0)
        {
            bool allowed = false;
            foreach (string permission in tool.AnyOfPermissions) allowed = allowed || await context.CanUseAsync(permission, ct);
            if (!allowed)
            {
                db.AiAuditEntries.Add(AiAuditEntry.Create(Guid.NewGuid(), companyId, userId, actor.Actor.ActorUsername ?? "", "tool.denied", toolName, time.GetUtcNow()));
                return new JsonObject { ["error"] = "access_denied" };
            }
        }
        try
        {
            return await tool.ExecuteAsync(context, arguments, ct);
        }
        catch (Exception exception)
        {
            return new JsonObject { ["error"] = "tool_failed", ["message"] = Truncate(exception.Message, 200) };
        }
    }

    private async Task<AiToolContext> BuildContextAsync(string culture, CancellationToken ct) => new(
        company.RequireCompanyId(),
        Guid.Parse(actor.Actor.UserId ?? Guid.Empty.ToString()),
        actor.Actor.ActorUsername ?? "",
        culture,
        async (code, token) => (await authorization.AuthorizeAsync(code, token)).IsSuccess);

    private async Task<List<AiChatMessage>> RecentHistoryAsync(Guid conversationId) =>
        await db.AiChatMessages.Where(m => m.ConversationId == conversationId).OrderByDescending(m => m.CreatedAtUtc).Take(MaxHistoryTurns).OrderBy(m => m.CreatedAtUtc).ToListAsync();

    private async Task PersistTurnAsync(Guid companyId, Guid userId, AiConversation conversation, string culture, string userMessage, string reply, string? toolsUsed, int? promptTokens, int? completionTokens, DateTimeOffset now, CancellationToken ct)
    {
        db.AiChatMessages.Add(AiChatMessage.Create(Guid.NewGuid(), conversation.Id, companyId, userId, AiChannels.Staff, "user", userMessage, null, null, null, culture, now));
        db.AiChatMessages.Add(AiChatMessage.Create(Guid.NewGuid(), conversation.Id, companyId, userId, AiChannels.Staff, "assistant", reply, toolsUsed, promptTokens, completionTokens, culture, now.AddMilliseconds(1)));
        db.AiAuditEntries.Add(AiAuditEntry.Create(Guid.NewGuid(), companyId, userId, actor.Actor.ActorUsername ?? "", "chat.answered", $"q={Truncate(userMessage, 300)} · tools={toolsUsed ?? "-"} · tokens={promptTokens ?? 0}+{completionTokens ?? 0}", now));
        conversation.Touch(now);
        await db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<AiAssistantTurnMessage>> ReadHistoryAsync(Guid conversationId)
    {
        List<AiChatMessage> rows = await db.AiChatMessages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.CreatedAtUtc).ToListAsync();
        return [.. rows.Select(m => new AiAssistantTurnMessage(m.Role, m.Content, AiCultureFormat.CultureDirection(m.Culture), m.CreatedAtUtc, m.ToolsUsed))];
    }

    private Task<IReadOnlyList<AiAssistantTurnMessage>> ReadHistoryAsync(Guid conversationId, CancellationToken ct) => ReadHistoryAsync(conversationId);

    private static bool LooksLikeToolUnsupported(string error) =>
        error.Contains("tool", StringComparison.OrdinalIgnoreCase)
        || error.Contains("function", StringComparison.OrdinalIgnoreCase)
        || error.Contains("400", StringComparison.Ordinal);

    private static string ProviderErrorText(string culture, string? error) => IsFa(culture)
        ? $"در ارتباط با مدل زبانی خطا رخ داد: {Truncate(error ?? "unknown", 200)}"
        : IsAr(culture)
            ? $"حدث خطأ أثناء الاتصال بنموذج اللغة: {Truncate(error ?? "unknown", 200)}"
            : IsEs(culture)
                ? $"Ocurrió un error al comunicarse con el modelo de lenguaje: {Truncate(error ?? "unknown", 200)}"
                : $"Error communicating with the language model: {Truncate(error ?? "unknown", 200)}";

    private static string EmptyReplyText(string culture) => IsFa(culture) ? "پاسخی دریافت نشد؛ دوباره بپرسید." : IsAr(culture) ? "لم يتم تلقي رد؛ حاول مرة أخرى." : IsEs(culture) ? "No se recibió respuesta; inténtalo de nuevo." : "No reply received; please ask again.";

    private static string LoopExhaustedText(string culture) => IsFa(culture) ? "پرسش بیش از حد پیچیده بود و ابزارها نتیجه نهایی ندادند؛ آن را ساده‌تر بپرسید." : IsAr(culture) ? "كان السؤال معقدًا للغاية؛ أعد صياغته بشكل أبسط." : IsEs(culture) ? "La pregunta fue demasiado compleja; inténtala con una más simple." : "The question was too complex for the tool loop; try a simpler one.";

    private static bool IsFa(string culture) => culture.StartsWith("fa", StringComparison.Ordinal);
    private static bool IsAr(string culture) => culture.StartsWith("ar", StringComparison.Ordinal);
    private static bool IsEs(string culture) => culture.StartsWith("es", StringComparison.Ordinal);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static void LogProviderCallFailed(ILogger<AiAssistantService> localLogger, string error) => ProviderCallFailed.Invoke(localLogger, error, null);
}

internal static class AiJson
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new(System.Text.Json.JsonSerializerDefaults.Web);
}
