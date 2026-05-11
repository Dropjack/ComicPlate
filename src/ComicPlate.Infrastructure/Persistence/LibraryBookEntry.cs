using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record LibraryBookEntry(
    string Id,
    string DisplayName,
    BookSourceKind SourceKind,
    int LastPageIndex,
    int LastKnownPageCount,
    ReadingDirection ReadingDirection,
    ViewMode ViewMode,
    DateTimeOffset LastOpenedAt);
