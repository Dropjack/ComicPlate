using ComicPlate.Core.Books;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record OpenedBookEntry(
    string Path,
    string DisplayName,
    BookSourceKind SourceKind,
    DateTimeOffset LastOpenedAt);
