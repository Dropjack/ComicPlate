namespace ComicPlate.Core.Books;

public interface IBookSource
{
    string Id { get; }

    string DisplayName { get; }

    BookSourceKind SourceKind { get; }

    Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken);
}
