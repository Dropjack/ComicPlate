using ComicPlate.Core.Books;
using ComicPlate.Core.Sorting;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class FileSystemContextShelfSource : IContextShelfSource
{
    private readonly string _rootPath;

    public FileSystemContextShelfSource(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    public string RootPath => _rootPath;

    public Task<ContextShelf> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var books = EnumerateBookEntries(cancellationToken)
            .OrderBy(book => book.DisplayName, NaturalPathComparer.Instance)
            .ThenBy(book => book.Path, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult(new ContextShelf(_rootPath, books));
    }

    private IEnumerable<BookEntry> EnumerateBookEntries(CancellationToken cancellationToken)
    {
        foreach (var archive in EnumerateArchiveFiles(_rootPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(archive);
            yield return new BookEntry(
                fullPath,
                Path.GetFileName(fullPath),
                BookSourceKind.Zip,
                fullPath);
        }

        foreach (var childDirectory in EnumerateDirectories(_rootPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(childDirectory);
            var sourceKind = GetDirectorySourceKind(fullPath, cancellationToken);
            if (sourceKind is null)
            {
                continue;
            }

            yield return new BookEntry(
                fullPath,
                Path.GetFileName(fullPath),
                sourceKind.Value,
                fullPath);
        }
    }

    private static BookSourceKind? GetDirectorySourceKind(string directory, CancellationToken cancellationToken)
    {
        if (ContainsDirectPageFiles(directory, cancellationToken))
        {
            return BookSourceKind.Folder;
        }

        if (ContainsChildContentCandidates(directory, cancellationToken))
        {
            return BookSourceKind.Collection;
        }

        return null;
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

    private static bool ContainsChildContentCandidates(string directory, CancellationToken cancellationToken)
    {
        return EnumerateArchiveFiles(directory, cancellationToken).Any()
            || EnumerateDirectories(directory, cancellationToken).Any();
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
