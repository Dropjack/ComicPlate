using ComicPlate.Core.Books;
using ComicPlate.Core.Sorting;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class FolderBookSource : IBookSource
{
    private readonly bool _recursive;
    private readonly string _rootPath;

    public FolderBookSource(string rootPath, bool recursive)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _recursive = recursive;
    }

    public string Id => _rootPath;

    public string DisplayName => new DirectoryInfo(_rootPath).Name;

    public BookSourceKind SourceKind => BookSourceKind.Folder;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var searchOption = _recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(_rootPath, "*", searchOption)
            .Where(IsSupportedPagePath)
            .Select(CreatePageEntry)
            .OrderBy(page => page.LogicalPath, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PageEntry>>(files);
    }

    private static bool IsSupportedPagePath(string path)
    {
        return SupportedPageFormats.IsSupportedExtension(Path.GetExtension(path));
    }

    private PageEntry CreatePageEntry(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var logicalPath = Path.GetRelativePath(_rootPath, fullPath).Replace('\\', '/');

        return new PageEntry(
            Path.GetFileName(fullPath),
            logicalPath,
            PageSourceKind.FileSystem,
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<Stream>(File.OpenRead(fullPath));
            });
    }
}
