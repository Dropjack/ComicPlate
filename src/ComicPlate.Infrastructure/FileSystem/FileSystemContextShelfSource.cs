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

        var entries = EnumerateShelfEntries(cancellationToken)
            .OrderBy(entry => entry.DisplayName, NaturalPathComparer.Instance)
            .ThenBy(entry => entry.Path, NaturalPathComparer.Instance)
            .ToArray();

        return Task.FromResult(new ContextShelf(_rootPath, entries));
    }

    private IEnumerable<ShelfEntry> EnumerateShelfEntries(CancellationToken cancellationToken)
    {
        foreach (var archive in EnumerateArchiveFiles(_rootPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(archive);
            if (!ComicArchiveFormats.TryGetByPath(fullPath, out var archiveFormat))
            {
                continue;
            }

            yield return new ShelfEntry(
                fullPath,
                Path.GetFileName(fullPath),
                ShelfEntryKind.Book,
                fullPath,
                archiveFormat.SourceKind);
        }

        foreach (var pdf in EnumeratePdfFiles(_rootPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(pdf);
            yield return new ShelfEntry(
                fullPath,
                Path.GetFileName(fullPath),
                ShelfEntryKind.Book,
                fullPath,
                BookSourceKind.Pdf);
        }

        foreach (var image in EnumeratePageFiles(_rootPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(image);
            yield return new ShelfEntry(
                fullPath,
                Path.GetFileName(fullPath),
                ShelfEntryKind.Book,
                fullPath,
                BookSourceKind.Image);
        }

        foreach (var childDirectory in EnumerateDirectories(_rootPath, cancellationToken))
        {
            var fullPath = Path.GetFullPath(childDirectory);
            var entryKind = GetDirectoryEntryKind(fullPath, cancellationToken);
            if (entryKind is null)
            {
                continue;
            }

            yield return new ShelfEntry(
                fullPath,
                Path.GetFileName(fullPath),
                entryKind.Value,
                fullPath,
                entryKind == ShelfEntryKind.Book ? BookSourceKind.Folder : null);
        }
    }

    private static ShelfEntryKind? GetDirectoryEntryKind(string directory, CancellationToken cancellationToken)
    {
        if (ContainsDirectPageFiles(directory, cancellationToken))
        {
            return ShelfEntryKind.Book;
        }

        if (ContainsChildContentCandidates(directory, cancellationToken))
        {
            return ShelfEntryKind.Collection;
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
            .Where(ComicArchiveFormats.IsSupportedArchivePath);
    }

    private static IEnumerable<string> EnumeratePdfFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumerateFiles(directory, cancellationToken)
            .Where(PdfBookFormat.IsSupportedPath);
    }

    private static bool ContainsDirectPageFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumeratePageFiles(directory, cancellationToken).Any();
    }

    private static bool ContainsChildContentCandidates(string directory, CancellationToken cancellationToken)
    {
        return EnumerateArchiveFiles(directory, cancellationToken).Any()
            || EnumeratePdfFiles(directory, cancellationToken).Any()
            || EnumerateDirectories(directory, cancellationToken).Any();
    }

    private static IEnumerable<string> EnumerateFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumerateSafe(directory, Directory.EnumerateFiles, cancellationToken);
    }

    private static IEnumerable<string> EnumeratePageFiles(string directory, CancellationToken cancellationToken)
    {
        return EnumerateFiles(directory, cancellationToken)
            .Where(file => SupportedPageFormats.IsSupportedExtension(Path.GetExtension(file)));
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

}
