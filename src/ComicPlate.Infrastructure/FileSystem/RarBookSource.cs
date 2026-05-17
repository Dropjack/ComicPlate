using ComicPlate.Core.Books;
using ComicPlate.Core.Sorting;
using SharpCompress.Archives;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class RarBookSource : IBookSource
{
    private readonly string _archivePath;

    public RarBookSource(string archivePath)
    {
        _archivePath = Path.GetFullPath(archivePath);
    }

    public string Id => _archivePath;

    public string DisplayName => Path.GetFileName(_archivePath);

    public BookSourceKind SourceKind => BookSourceKind.Rar;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ArchiveFactory.OpenArchive(_archivePath);
        var pages = archive.Entries
            .Where(entry => !entry.IsDirectory)
            .Select(entry => NormalizeEntryPath(entry.Key))
            .Where(logicalPath => !string.IsNullOrWhiteSpace(logicalPath))
            .Where(logicalPath => SupportedPageFormats.IsSupportedExtension(Path.GetExtension(logicalPath)))
            .Select(CreatePageEntry)
            .OrderBy(page => page.LogicalPath, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PageEntry>>(pages);
    }

    private PageEntry CreatePageEntry(string logicalPath)
    {
        return new PageEntry(
            GetDisplayName(logicalPath),
            logicalPath,
            PageSourceKind.RarEntry,
            async cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var archive = ArchiveFactory.OpenArchive(_archivePath);
                var liveEntry = archive.Entries
                    .FirstOrDefault(entry => !entry.IsDirectory && NormalizeEntryPath(entry.Key) == logicalPath);

                if (liveEntry is null)
                {
                    throw new FileNotFoundException("The RAR entry no longer exists.", logicalPath);
                }

                await using var entryStream = liveEntry.OpenEntryStream();
                var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                return memoryStream;
            });
    }

    private static string NormalizeEntryPath(string? path)
    {
        return path?.Replace('\\', '/') ?? string.Empty;
    }

    private static string GetDisplayName(string logicalPath)
    {
        var lastSlashIndex = logicalPath.LastIndexOf('/');
        return lastSlashIndex < 0
            ? logicalPath
            : logicalPath[(lastSlashIndex + 1)..];
    }
}
