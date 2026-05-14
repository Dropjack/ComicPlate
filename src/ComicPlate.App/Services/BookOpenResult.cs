using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed record BookOpenResult(
    BookEntry Book,
    IReadOnlyList<PageEntry> Pages);
