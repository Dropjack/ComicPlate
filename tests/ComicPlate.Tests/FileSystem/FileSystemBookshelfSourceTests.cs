using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class FileSystemBookshelfSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlateBookshelfTests-{Guid.NewGuid():N}");

    public FileSystemBookshelfSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsCurrentFolderOpenableItemsOnly()
    {
        var book10 = Directory.CreateDirectory(Path.Combine(_tempDirectory, "Shelf", "Book 10"));
        var book2 = Directory.CreateDirectory(Path.Combine(_tempDirectory, "Book 2"));
        var chapter = Directory.CreateDirectory(Path.Combine(book2.FullName, "Chapter 1"));

        File.WriteAllText(Path.Combine(book10.FullName, "001.jpg"), "");
        File.WriteAllText(Path.Combine(book2.FullName, "001.jpg"), "");
        File.WriteAllText(Path.Combine(chapter.FullName, "002.jpg"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Book 1.cbz"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Shelf", "Book 3.zip"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "notes.txt"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "cover.jpg"), "");

        var source = new FileSystemBookshelfSource(_tempDirectory);

        var bookshelf = await source.LoadAsync(CancellationToken.None);

        Assert.Equal(
            new[] { "Book 1.cbz", "Book 2", "Shelf" },
            bookshelf.Books.Select(book => book.DisplayName));
        Assert.Equal(
            new[] { BookSourceKind.Zip, BookSourceKind.Folder, BookSourceKind.Collection },
            bookshelf.Books.Select(book => book.SourceKind));
    }

    [Fact]
    public async Task SkipsEmptyChildFolders()
    {
        var book2 = Directory.CreateDirectory(Path.Combine(_tempDirectory, "Book 2"));
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Empty"));

        File.WriteAllText(Path.Combine(book2.FullName, "001.jpg"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Book 1.cbz"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "notes.txt"), "");

        var source = new FileSystemBookshelfSource(_tempDirectory);

        var bookshelf = await source.LoadAsync(CancellationToken.None);

        Assert.Equal(
            new[] { "Book 1.cbz", "Book 2" },
            bookshelf.Books.Select(book => book.DisplayName));
        Assert.Equal(
            new[] { BookSourceKind.Zip, BookSourceKind.Folder },
            bookshelf.Books.Select(book => book.SourceKind));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
