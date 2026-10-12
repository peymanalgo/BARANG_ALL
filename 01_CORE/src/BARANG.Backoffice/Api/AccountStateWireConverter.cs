using System.Text.Json;
using System.Text.Json.Serialization;

namespace BARANG.Backoffice.Api;

/// <summary>
/// Reads canonical account-state names and the legacy numeric enum wire form
/// emitted by older Core deployments. Unknown values are rejected deliberately.
/// </summary>
public sealed class AccountStateWireConverter : JsonConverter<string>
{
    private static readonly IReadOnlyDictionary<int, string> NumericStates = new Dictionary<int, string>
    {
        [0] = "Active",
        [1] = "Inactive",
        [2] = "Locked"
    };

    private static readonly IReadOnlyDictionary<string, string> NamedStates =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Active"] = "Active",
            ["Inactive"] = "Inactive",
            ["Locked"] = "Locked"
        };

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && NamedStates.TryGetValue(reader.GetString() ?? string.Empty, out string? named))
            return named;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int numeric) && NumericStates.TryGetValue(numeric, out string? number))
            return number;

        throw new JsonException("The accountState value is not a supported BARANG account state.");
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null || !NamedStates.TryGetValue(value, out string? canonical))
            throw new JsonException("The accountState value is not a supported BARANG account state.");
        writer.WriteStringValue(canonical);
    }
}
