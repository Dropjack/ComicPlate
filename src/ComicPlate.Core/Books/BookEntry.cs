namespace ComicPlate.Core.Books;

public sealed record BookEntry(
    string Id,
    string DisplayName,
    BookSourceKind SourceKind,
    string Path);
