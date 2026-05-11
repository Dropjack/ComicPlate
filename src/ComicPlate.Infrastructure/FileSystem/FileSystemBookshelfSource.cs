using ComicPlate.Core.Books;
using ComicPlate.Core.Sorting;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class FileSystemBookshelfSource : IBookshelfSource
{
    private readonly string _rootPath;

    public FileSystemBookshelfSource(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    public string RootPath => _rootPath;

    public Task<Bookshelf> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var books = EnumerateBookEntries(cancellationToken)
            .OrderBy(book => book.DisplayName, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult(new Bookshelf(_rootPath, books));
    }

    private IEnumerable<BookEntry> EnumerateBookEntries(CancellationToken cancellationToken)
    {
        foreach (var directory in EnumerateDirectories(cancellationToken))
        {
            var fullPath = Path.GetFullPath(directory);
            yield return new BookEntry(
                fullPath,
                Path.GetFileName(fullPath),
                BookSourceKind.Folder,
                fullPath);
        }

        foreach (var archive in EnumerateArchiveFiles(cancellationToken))
        {
            var fullPath = Path.GetFullPath(archive);
            yield return new BookEntry(
                fullPath,
                Path.GetFileName(fullPath),
                BookSourceKind.Zip,
                fullPath);
        }
    }

    private IEnumerable<string> EnumerateDirectories(CancellationToken cancellationToken)
    {
        IEnumerable<string> directories;

        try
        {
            directories = Directory.EnumerateDirectories(_rootPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return directory;
        }
    }

    private IEnumerable<string> EnumerateArchiveFiles(CancellationToken cancellationToken)
    {
        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(_rootPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsSupportedArchivePath(file))
            {
                yield return file;
            }
        }
    }

    private static bool IsSupportedArchivePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase);
    }
}
