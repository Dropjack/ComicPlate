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

        var files = EnumerateFiles(cancellationToken)
            .Where(IsSupportedPagePath)
            .Select(CreatePageEntry)
            .OrderBy(page => page.LogicalPath, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PageEntry>>(files);
    }

    private IEnumerable<string> EnumerateFiles(CancellationToken cancellationToken)
    {
        return _recursive
            ? EnumerateFilesRecursively(_rootPath, cancellationToken)
            : EnumerateFilesInDirectory(_rootPath, cancellationToken);
    }

    private static IEnumerable<string> EnumerateFilesRecursively(string directory, CancellationToken cancellationToken)
    {
        foreach (var file in EnumerateFilesInDirectory(directory, cancellationToken))
        {
            yield return file;
        }

        foreach (var childDirectory in EnumerateDirectories(directory, cancellationToken))
        {
            foreach (var file in EnumerateFilesRecursively(childDirectory, cancellationToken))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateFilesInDirectory(string directory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string directory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<string> directories;

        try
        {
            directories = Directory.EnumerateDirectories(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var childDirectory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return childDirectory;
        }
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
