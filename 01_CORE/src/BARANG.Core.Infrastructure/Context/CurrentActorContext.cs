using System.Threading;
using BARANG.Core.Application.Abstractions.Actors;

namespace BARANG.Core.Infrastructure.Context;

public sealed class CurrentActorContext : ICurrentActor
{
    private readonly AsyncLocal<ContextState?> _current = new();

    public ActorContext Actor => _current.Value?.Actor ?? ActorContext.Anonymous;

    public IDisposable BeginScope(ActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ContextState? previous = _current.Value;
        _current.Value = new ContextState(actor);
        return new Scope(() => _current.Value = previous);
    }

    private sealed record ContextState(ActorContext Actor);

    private sealed class Scope(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
