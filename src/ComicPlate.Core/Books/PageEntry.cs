namespace ComicPlate.Core.Books;

public sealed record PageEntry(
    string DisplayName,
    string LogicalPath,
    PageSourceKind SourceKind,
    Func<CancellationToken, Task<Stream>> OpenStreamAsync);
