namespace ComicPlate.Core.Books;

public sealed record ContextShelf(
    string RootPath,
    IReadOnlyList<ShelfEntry> Entries);
