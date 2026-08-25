namespace BARANG.Core.Application;

public sealed record CoreIdentity(string Name, string Status, string Version)
{
    public static CoreIdentity Current { get; } = new("BARANG Core", "operational", "1.0");
}
