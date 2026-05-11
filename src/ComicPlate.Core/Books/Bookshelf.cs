namespace ComicPlate.Core.Books;

public sealed record Bookshelf(
    string RootPath,
    IReadOnlyList<BookEntry> Books);
