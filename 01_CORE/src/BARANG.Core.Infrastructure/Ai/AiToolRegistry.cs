using BARANG.Core.Application.Ai;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>رجیستری توسعه‌پذیر: هر IAiTool ثبت‌شده در DI به‌صورت خودکار در دسترس مدل قرار می‌گیرد.
/// ابزارهای مشتری (IAiCustomerTool) عمداً اینجا حذف می‌شوند تا فقط رجیستری مشتری آن‌ها را ببیند.</summary>
public sealed class AiToolRegistry(IEnumerable<IAiTool> tools) : IAiToolRegistry
{
    private readonly IReadOnlyList<IAiTool> _tools = [.. tools.Where(t => t is not IAiCustomerTool).OrderBy(t => t.Name, StringComparer.Ordinal)];

    public IReadOnlyList<IAiTool> Tools => _tools;

    public IAiTool? Find(string name) => _tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal));
}

/// <summary>رجیستری دستیار مشتری B2C: فقط IAiCustomerToolها — هیچ ابزار کارکنان ERP در این فهرست نیست.</summary>
public sealed class AiCustomerToolRegistry(IEnumerable<IAiCustomerTool> tools) : IAiToolRegistry
{
    private readonly IReadOnlyList<IAiTool> _tools = [.. tools.OrderBy(t => t.Name, StringComparer.Ordinal)];

    public IReadOnlyList<IAiTool> Tools => _tools;

    public IAiTool? Find(string name) => _tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal));
}
