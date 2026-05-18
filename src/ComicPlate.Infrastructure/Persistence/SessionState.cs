using ComicPlate.Core.Navigation;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record SessionState(
    int Version,
    ReadableUnitState? Current,
    NavigationEntry? ShelfCurrent,
    IReadOnlyList<NavigationEntry> BackStack,
    DateTimeOffset SavedAt)
{
    public NavigationEntry? ReadingShelfCurrent { get; init; }

    public IReadOnlyList<NavigationEntry> ReadingShelfBackStack { get; init; } = Array.Empty<NavigationEntry>();

    public static SessionState Empty { get; } = new(
        Version: 1,
        Current: null,
        ShelfCurrent: null,
        BackStack: Array.Empty<NavigationEntry>(),
        SavedAt: DateTimeOffset.MinValue);
}
