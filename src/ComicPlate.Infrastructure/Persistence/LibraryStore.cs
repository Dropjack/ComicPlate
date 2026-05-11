namespace ComicPlate.Infrastructure.Persistence;

public sealed record LibraryStore(
    int Version,
    IReadOnlyList<LibraryBookEntry> Books)
{
    public static LibraryStore Empty { get; } = new(1, Array.Empty<LibraryBookEntry>());
}
