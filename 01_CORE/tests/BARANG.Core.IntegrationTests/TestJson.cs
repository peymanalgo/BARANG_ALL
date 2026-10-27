using System.Text.Json;
using System.Text.Json.Serialization;

namespace BARANG.Core.IntegrationTests;

public static class TestJson
{
    public static JsonSerializerOptions ApiOptions { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
}
