using ComicPlate.Core.Books;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record ReadableUnitState(
    string Path,
    string DisplayName,
    BookSourceKind SourceKind,
    int LastPageIndex);
