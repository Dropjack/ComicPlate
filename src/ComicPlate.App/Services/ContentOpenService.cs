using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.App.Services;

public sealed class ContentOpenService
{
    public OpenPathResult ClassifyPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            return new OpenPathResult(OpenPathKind.ContentFolder, fullPath);
        }

        if (!File.Exists(fullPath))
        {
            return new OpenPathResult(OpenPathKind.Missing, fullPath);
        }

        if (ComicArchiveFormats.TryGetByPath(fullPath, out var archiveFormat))
        {
            return new OpenPathResult(
                OpenPathKind.Book,
                fullPath,
                CreateBookEntry(fullPath, archiveFormat.SourceKind));
        }

        if (SupportedPageFormats.IsSupportedExtension(Path.GetExtension(fullPath)))
        {
            return new OpenPathResult(
                OpenPathKind.Book,
                fullPath,
                CreateBookEntry(fullPath, BookSourceKind.Image));
        }

        return new OpenPathResult(OpenPathKind.Unsupported, fullPath);
    }

    public async Task<ContentFolderOpenResult> OpenContentFolderAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(folderPath);
        var contextShelfSource = new FileSystemContextShelfSource(fullPath);
        var directPageSource = new FolderBookSource(fullPath, recursive: false);

        var contextShelfTask = Task.Run(() => contextShelfSource.LoadAsync(cancellationToken), cancellationToken);
        var pagesTask = Task.Run(() => directPageSource.LoadPagesAsync(cancellationToken), cancellationToken);
        await Task.WhenAll(contextShelfTask, pagesTask);

        var contextShelf = await contextShelfTask;
        var pages = await pagesTask;
        return new ContentFolderOpenResult(
            fullPath,
            contextShelf.Entries,
            CreateBookEntry(fullPath, BookSourceKind.Folder),
            pages);
    }

    public async Task<BookOpenResult> OpenBookAsync(
        BookEntry book,
        CancellationToken cancellationToken)
    {
        var normalizedBook = NormalizeBookEntry(book);
        IBookSource source = normalizedBook.SourceKind switch
        {
            BookSourceKind.Image => new SingleImageBookSource(normalizedBook.Path),
            BookSourceKind.Zip => new ZipBookSource(normalizedBook.Path),
            BookSourceKind.Rar => new RarBookSource(normalizedBook.Path),
            _ => new FolderBookSource(normalizedBook.Path, recursive: false)
        };

        var pages = await Task.Run(() => source.LoadPagesAsync(cancellationToken), cancellationToken);
        return new BookOpenResult(normalizedBook, pages);
    }

    public static BookEntry CreateBookEntry(string path, BookSourceKind sourceKind)
    {
        var fullPath = Path.GetFullPath(path);
        return new BookEntry(fullPath, Path.GetFileName(fullPath), sourceKind, fullPath);
    }

    public static BookEntry NormalizeBookEntry(BookEntry book)
    {
        var fullPath = Path.GetFullPath(book.Path);
        return book with { Id = fullPath, Path = fullPath };
    }

}
