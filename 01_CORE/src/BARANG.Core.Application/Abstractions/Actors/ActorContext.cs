using System.Collections.ObjectModel;

namespace BARANG.Core.Application.Abstractions.Actors;

public sealed class ActorContext
{
    public ActorContext(
        string? actorId,
        ActorType actorType,
        bool isAuthenticated,
        SourceChannel sourceChannel,
        string? userId = null,
        string? apiClientId = null,
        IEnumerable<string>? roleCodes = null)
    {
        if (isAuthenticated && string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("An authenticated actor must have an actor identifier.", nameof(actorId));
        }

        if (actorType is ActorType.Anonymous && isAuthenticated)
        {
            throw new ArgumentException("An anonymous actor cannot be authenticated.", nameof(isAuthenticated));
        }

        string[] roles = roleCodes?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
        if (roles.Any(static role => string.IsNullOrWhiteSpace(role) || !IsTechnicalCode(role)))
        {
            throw new ArgumentException("Role codes must be lowercase English technical identifiers.", nameof(roleCodes));
        }

        ActorId = actorId;
        ActorType = actorType;
        IsAuthenticated = isAuthenticated;
        SourceChannel = sourceChannel;
        UserId = userId;
        ApiClientId = apiClientId;
        RoleCodes = new ReadOnlyCollection<string>(roles);
    }

    public static ActorContext Anonymous { get; } =
        new(null, ActorType.Anonymous, false, SourceChannel.Unknown);

    public string? ActorId { get; }

    public ActorType ActorType { get; }

    public string? UserId { get; }

    public string? ApiClientId { get; }

    public SourceChannel SourceChannel { get; }

    public IReadOnlyCollection<string> RoleCodes { get; }

    public bool IsAuthenticated { get; }

    private static bool IsTechnicalCode(string value) => value.All(static character =>
        character is >= 'a' and <= 'z'
        or >= '0' and <= '9'
        or '.' or '_');
}
