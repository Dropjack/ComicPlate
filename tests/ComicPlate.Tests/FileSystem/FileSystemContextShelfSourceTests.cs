using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class FileSystemContextShelfSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlateContextShelfTests-{Guid.NewGuid():N}");

    public FileSystemContextShelfSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsCurrentFolderOpenableItemsOnly()
    {
        var book10 = Directory.CreateDirectory(Path.Combine(_tempDirectory, "Collection", "Book 10"));
        var book2 = Directory.CreateDirectory(Path.Combine(_tempDirectory, "Book 2"));
        var chapter = Directory.CreateDirectory(Path.Combine(book2.FullName, "Chapter 1"));

        File.WriteAllText(Path.Combine(book10.FullName, "001.jpg"), "");
        File.WriteAllText(Path.Combine(book2.FullName, "001.jpg"), "");
        File.WriteAllText(Path.Combine(chapter.FullName, "002.jpg"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Book 1.cbz"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Book 4.cbr"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Book 5.rar"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Collection", "Book 3.zip"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "notes.txt"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Skipped.cb7"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "cover.jpg"), "");

        var source = new FileSystemContextShelfSource(_tempDirectory);

        var contextShelf = await source.LoadAsync(CancellationToken.None);

        Assert.Equal(
            new[] { "Book 1.cbz", "Book 2", "Book 4.cbr", "Book 5.rar", "Collection" },
            contextShelf.Entries.Select(book => book.DisplayName));
        Assert.Equal(
            new[] { BookSourceKind.Zip, BookSourceKind.Folder, BookSourceKind.Rar, BookSourceKind.Rar, BookSourceKind.Collection },
            contextShelf.Entries.Select(book => book.SourceKind));
    }

    [Fact]
    public async Task SkipsEmptyChildFolders()
    {
        var book2 = Directory.CreateDirectory(Path.Combine(_tempDirectory, "Book 2"));
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Empty"));

        File.WriteAllText(Path.Combine(book2.FullName, "001.jpg"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Book 1.cbz"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "notes.txt"), "");

        var source = new FileSystemContextShelfSource(_tempDirectory);

        var contextShelf = await source.LoadAsync(CancellationToken.None);

        Assert.Equal(
            new[] { "Book 1.cbz", "Book 2" },
            contextShelf.Entries.Select(book => book.DisplayName));
        Assert.Equal(
            new[] { BookSourceKind.Zip, BookSourceKind.Folder },
            contextShelf.Entries.Select(book => book.SourceKind));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
