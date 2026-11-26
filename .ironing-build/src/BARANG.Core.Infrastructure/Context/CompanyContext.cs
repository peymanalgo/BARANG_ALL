using System.Threading;
using BARANG.Core.Application.Abstractions.Companies;

namespace BARANG.Core.Infrastructure.Context;

public sealed class CompanyContext : ICompanyContext
{
    private readonly AsyncLocal<ContextState?> _current = new();

    public Guid? CompanyId => _current.Value?.CompanyId;

    public bool IsAvailable => CompanyId is not null;

    public IDisposable BeginScope(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company identifier cannot be empty.", nameof(companyId));
        }

        ContextState? previous = _current.Value;
        _current.Value = new ContextState(companyId);
        return new Scope(() => _current.Value = previous);
    }

    private sealed record ContextState(Guid CompanyId);

    private sealed class Scope(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
