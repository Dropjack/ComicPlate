namespace ComicPlate.Core.Books;

public sealed record ContextShelf(
    string RootPath,
    IReadOnlyList<BookEntry> Entries);
