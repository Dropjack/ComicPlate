using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed record OpenPathResult(
    OpenPathKind Kind,
    string Path,
    BookEntry? Book = null);
