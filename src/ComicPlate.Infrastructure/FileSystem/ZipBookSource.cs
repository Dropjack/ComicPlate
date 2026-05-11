using System.IO.Compression;
using ComicPlate.Core.Books;
using ComicPlate.Core.Sorting;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class ZipBookSource : IBookSource
{
    private readonly string _archivePath;

    public ZipBookSource(string archivePath)
    {
        _archivePath = Path.GetFullPath(archivePath);
    }

    public string Id => _archivePath;

    public string DisplayName => Path.GetFileName(_archivePath);

    public BookSourceKind SourceKind => BookSourceKind.Zip;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ZipFile.OpenRead(_archivePath);
        var pages = archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .Where(entry => SupportedPageFormats.IsSupportedExtension(Path.GetExtension(entry.FullName)))
            .Select(CreatePageEntry)
            .OrderBy(page => page.LogicalPath, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PageEntry>>(pages);
    }

    private PageEntry CreatePageEntry(ZipArchiveEntry entry)
    {
        var logicalPath = entry.FullName.Replace('\\', '/');

        return new PageEntry(
            entry.Name,
            logicalPath,
            PageSourceKind.ZipEntry,
            async cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var archive = ZipFile.OpenRead(_archivePath);
                var liveEntry = archive.GetEntry(entry.FullName);

                if (liveEntry is null)
                {
                    throw new FileNotFoundException("The ZIP entry no longer exists.", entry.FullName);
                }

                await using var entryStream = liveEntry.Open();
                var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                return memoryStream;
            });
    }
}
