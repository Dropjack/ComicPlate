namespace ComicPlate.Core.Books;

public sealed record Book(
    string Id,
    string DisplayName,
    BookSourceKind SourceKind,
    IReadOnlyList<PageEntry> Pages);
