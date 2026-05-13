using ComicPlate.Core.Books;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class SingleImageBookSource : IBookSource
{
    private readonly string _imagePath;

    public SingleImageBookSource(string imagePath)
    {
        _imagePath = Path.GetFullPath(imagePath);
    }

    public string Id => _imagePath;

    public string DisplayName => Path.GetFileName(_imagePath);

    public BookSourceKind SourceKind => BookSourceKind.Image;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = new PageEntry(
            Path.GetFileName(_imagePath),
            Path.GetFileName(_imagePath),
            PageSourceKind.FileSystem,
            token =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult<Stream>(File.OpenRead(_imagePath));
            });

        return Task.FromResult<IReadOnlyList<PageEntry>>(new[] { page });
    }
}
