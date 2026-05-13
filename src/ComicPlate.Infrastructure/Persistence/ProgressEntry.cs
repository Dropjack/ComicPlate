using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Infrastructure.Persistence;

public sealed record ProgressEntry(
    string Path,
    string DisplayName,
    BookSourceKind SourceKind,
    int LastPageIndex,
    int LastKnownPageCount,
    ReadingDirection ReadingDirection,
    ViewMode ViewMode,
    DateTimeOffset LastOpenedAt);
