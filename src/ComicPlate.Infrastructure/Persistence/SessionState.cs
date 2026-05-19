using ComicPlate.Core.Navigation;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record SessionState(
    int Version,
    ReadableUnitState? Current,
    NavigationEntry? ShelfCurrent,
    IReadOnlyList<NavigationEntry> BackStack,
    DateTimeOffset SavedAt)
{
    // Compatibility fields written by earlier builds. New code should prefer
    // ReadingParentCollection*.
    public NavigationEntry? ReadingShelfCurrent { get; init; }

    public IReadOnlyList<NavigationEntry> ReadingShelfBackStack { get; init; } = Array.Empty<NavigationEntry>();

    public NavigationEntry? ReadingContainerCurrent { get; init; }

    public IReadOnlyList<NavigationEntry> ReadingContainerBackStack { get; init; } = Array.Empty<NavigationEntry>();

    // Compatibility fields written during the Shelf -> Collection rename.
    public NavigationEntry? ReadingParentShelfCurrent { get; init; }

    public IReadOnlyList<NavigationEntry> ReadingParentShelfBackStack { get; init; } = Array.Empty<NavigationEntry>();

    public NavigationEntry? ReadingParentCollectionCurrent { get; init; }

    public IReadOnlyList<NavigationEntry> ReadingParentCollectionBackStack { get; init; } = Array.Empty<NavigationEntry>();

    public static SessionState Empty { get; } = new(
        Version: 1,
        Current: null,
        ShelfCurrent: null,
        BackStack: Array.Empty<NavigationEntry>(),
        SavedAt: DateTimeOffset.MinValue);
}
