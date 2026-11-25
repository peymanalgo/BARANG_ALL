using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BARANG.Core.Infrastructure.Ai;

public sealed record AiLlmToolCall(string Id, string Name, string ArgumentsJson);

public sealed record AiLlmChatMessage(
    string Role,
    string? Content = null,
    string? ToolCallId = null,
    string? Name = null,
    IReadOnlyList<AiLlmToolCall>? ToolCalls = null)
{
    public static AiLlmChatMessage System(string content) => new("system", content);
    public static AiLlmChatMessage User(string content) => new("user", content);
    public static AiLlmChatMessage Assistant(string? content, IReadOnlyList<AiLlmToolCall>? toolCalls = null) => new("assistant", content, ToolCalls: toolCalls);
    public static AiLlmChatMessage Tool(string toolCallId, string content, string? name = null) => new("tool", content, toolCallId, name);
}

public sealed record AiLlmChatResult(
    bool Success,
    string? Content,
    IReadOnlyList<AiLlmToolCall> ToolCalls,
    int PromptTokens,
    int CompletionTokens,
    string? Error)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}

/// <summary>کلاینت سازگار با OpenAI/Z.AI — HTTP دستی بدون وابستگی NuGet جدید. مسیر /chat/completions را صدا می‌زند.</summary>
public sealed class AiLlmClient(IHttpClientFactory httpClientFactory)
{
    private const string HttpClientName = "ai-llm";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiLlmChatResult> ChatAsync(
        string baseUrl,
        string? apiKey,
        string model,
        decimal temperature,
        int maxTokens,
        IReadOnlyList<AiLlmChatMessage> messages,
        IReadOnlyList<(string Name, string Description, JsonObject Parameters)>? tools,
        TimeSpan timeout,
        CancellationToken ct)
    {
        JsonObject payload = new()
        {
            ["model"] = model,
            ["temperature"] = (double)temperature,
            ["max_tokens"] = maxTokens,
            ["messages"] = new JsonArray(messages.Select(ToWire).ToArray())
        };
        if (tools is { Count: > 0 })
        {
            payload["tools"] = new JsonArray(tools.Select(static tool => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject { ["name"] = tool.Name, ["description"] = tool.Description, ["parameters"] = tool.Parameters.DeepClone() }
            }).ToArray());
            payload["tool_choice"] = "auto";
        }

        try
        {
            using HttpClient http = httpClientFactory.CreateClient(HttpClientName);
            using HttpRequestMessage request = new(HttpMethod.Post, BuildUrl(baseUrl, "chat/completions"));
            request.Content = JsonContent.Create(payload, options: JsonOptions);
            if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            using HttpResponseMessage response = await http.SendAsync(request, cts.Token);
            string body = await response.Content.ReadAsStringAsync(cts.Token);
            if (!response.IsSuccessStatusCode)
                return new AiLlmChatResult(false, null, [], 0, 0, $"HTTP {(int)response.StatusCode}: {Trim(body)}");
            JsonNode? root = JsonNode.Parse(body);
            JsonNode? choice = root?["choices"]?[0]?["message"];
            string? content = choice?["content"]?.GetValue<string>();
            List<AiLlmToolCall> calls = [];
            if (choice?["tool_calls"] is JsonArray callArray)
            {
                foreach (JsonNode? call in callArray)
                {
                    string? id = call?["id"]?.GetValue<string>();
                    string? fnName = call?["function"]?["name"]?.GetValue<string>();
                    string? args = call?["function"]?["arguments"]?.GetValue<string>() ?? "{}";
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(fnName)) calls.Add(new AiLlmToolCall(id!, fnName!, args));
                }
            }
            int promptTokens = root?["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;
            int completionTokens = root?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0;
            return new AiLlmChatResult(true, content, calls, promptTokens, completionTokens, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiLlmChatResult(false, null, [], 0, 0, "timeout");
        }
        catch (HttpRequestException exception)
        {
            return new AiLlmChatResult(false, null, [], 0, 0, exception.Message);
        }
        catch (JsonException exception)
        {
            return new AiLlmChatResult(false, null, [], 0, 0, "invalid provider response: " + exception.Message);
        }
        catch (TaskCanceledException)
        {
            return new AiLlmChatResult(false, null, [], 0, 0, "timeout");
        }
    }

    public async Task<(bool Ok, int LatencyMs, string? Reply, string? Error)> PingAsync(string baseUrl, string? apiKey, string model, TimeSpan timeout, CancellationToken ct)
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        AiLlmChatResult result = await ChatAsync(baseUrl, apiKey, model, 0m, 64,
        [
            AiLlmChatMessage.System("You are a connection test. Reply with the single word: pong"),
            AiLlmChatMessage.User("ping")
        ], null, timeout, ct);
        int latency = (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
        return result.Success ? (true, latency, result.Content, null) : (false, latency, null, result.Error);
    }

    private static string BuildUrl(string baseUrl, string path)
    {
        string trimmed = baseUrl.Trim().TrimEnd('/');
        return trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ? trimmed : trimmed + "/" + path.TrimStart('/');
    }

    private static JsonNode ToWire(AiLlmChatMessage message)
    {
        JsonObject node = new() { ["role"] = message.Role };
        if (message.Content is not null) node["content"] = message.Content;
        if (message.ToolCallId is not null) node["tool_call_id"] = message.ToolCallId;
        if (message.Name is not null) node["name"] = message.Name;
        if (message.ToolCalls is { Count: > 0 })
        {
            node["tool_calls"] = new JsonArray(message.ToolCalls.Select(static call => (JsonNode)new JsonObject
            {
                ["id"] = call.Id,
                ["type"] = "function",
                ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson }
            }).ToArray());
        }
        return node;
    }

    private static string Trim(string value) => value.Length > 400 ? value[..400] : value;
}
