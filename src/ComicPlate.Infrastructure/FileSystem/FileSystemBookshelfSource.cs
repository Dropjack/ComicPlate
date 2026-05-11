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
            .ThenBy(book => book.Path, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult(new Bookshelf(_rootPath, books));
    }

    private IEnumerable<BookEntry> EnumerateBookEntries(CancellationToken cancellationToken)
    {
        foreach (var book in EnumerateBookEntriesInDirectory(_rootPath, allowCurrentDirectoryAsBook: false, cancellationToken))
        {
            yield return book;
        }
    }

    private static IEnumerable<BookEntry> EnumerateBookEntriesInDirectory(
        string directory,
        bool allowCurrentDirectoryAsBook,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullDirectoryPath = Path.GetFullPath(directory);

        if (allowCurrentDirectoryAsBook && ContainsDirectPageFiles(fullDirectoryPath, cancellationToken))
        {
            yield return new BookEntry(
                fullDirectoryPath,
                Path.GetFileName(fullDirectoryPath),
                BookSourceKind.Folder,
                fullDirectoryPath);
            yield break;
        }

        foreach (var archive in EnumerateArchiveFiles(fullDirectoryPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(archive);
            yield return new BookEntry(
                fullPath,
                Path.GetFileName(fullPath),
                BookSourceKind.Zip,
                fullPath);
        }

        foreach (var childDirectory in EnumerateDirectories(fullDirectoryPath, cancellationToken))
        {
            foreach (var book in EnumerateBookEntriesInDirectory(childDirectory, allowCurrentDirectoryAsBook: true, cancellationToken))
            {
                yield return book;
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string directory, CancellationToken cancellationToken)
    {
        return EnumerateSafe(directory, Directory.EnumerateDirectories, cancellationToken);
    }

    private static IEnumerable<string> EnumerateArchiveFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumerateFiles(directory, cancellationToken)
            .Where(IsSupportedArchivePath);
    }

    private static bool ContainsDirectPageFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumerateFiles(directory, cancellationToken)
            .Any(file => SupportedPageFormats.IsSupportedExtension(Path.GetExtension(file)));
    }

    private static IEnumerable<string> EnumerateFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumerateSafe(directory, Directory.EnumerateFiles, cancellationToken);
    }

    private static IEnumerable<string> EnumerateSafe(
        string directory,
        Func<string, IEnumerable<string>> enumerate,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> items;

        try
        {
            items = enumerate(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static bool IsSupportedArchivePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase);
    }
}
