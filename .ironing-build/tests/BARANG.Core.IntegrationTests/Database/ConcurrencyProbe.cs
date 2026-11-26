namespace BARANG.Core.IntegrationTests.Database;

public sealed class ConcurrencyProbe
{
    public Guid Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public long Version { get; set; }
}
