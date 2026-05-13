using ComicPlate.Core.Navigation;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record SessionState(
    int Version,
    ReadableUnitState? Current,
    IReadOnlyList<NavigationEntry> BackStack,
    DateTimeOffset SavedAt)
{
    public static SessionState Empty { get; } = new(
        Version: 1,
        Current: null,
        BackStack: Array.Empty<NavigationEntry>(),
        SavedAt: DateTimeOffset.MinValue);
}
