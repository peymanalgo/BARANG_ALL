using System.Threading;
using BARANG.Core.Application.Abstractions.Correlation;

namespace BARANG.Core.Infrastructure.Context;

public sealed class CorrelationContext : ICorrelationContext
{
    private readonly AsyncLocal<ContextState?> _current = new();

    public string? CorrelationId => _current.Value?.CorrelationId;

    public IDisposable BeginScope(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ContextState? previous = _current.Value;
        _current.Value = new ContextState(correlationId);
        return new Scope(() => _current.Value = previous);
    }

    private sealed record ContextState(string CorrelationId);

    private sealed class Scope(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
