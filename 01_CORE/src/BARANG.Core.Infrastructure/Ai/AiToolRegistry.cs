using BARANG.Core.Application.Ai;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>رجیستری توسعه‌پذیر: هر IAiTool ثبت‌شده در DI به‌صورت خودکار در دسترس مدل قرار می‌گیرد.</summary>
public sealed class AiToolRegistry(IEnumerable<IAiTool> tools) : IAiToolRegistry
{
    private readonly IReadOnlyList<IAiTool> _tools = [.. tools.OrderBy(t => t.Name, StringComparer.Ordinal)];

    public IReadOnlyList<IAiTool> Tools => _tools;

    public IAiTool? Find(string name) => _tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal));
}
