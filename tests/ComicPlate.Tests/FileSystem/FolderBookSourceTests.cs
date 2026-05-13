using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class FolderBookSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlateTests-{Guid.NewGuid():N}");

    public FolderBookSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsSupportedImagesInNaturalOrder()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "10.jpg"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "2.png"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "readme.txt"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "1.JPG"), "");

        var source = new FolderBookSource(_tempDirectory, recursive: false);

        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Equal(new[] { "1.JPG", "2.png", "10.jpg" }, pages.Select(page => page.LogicalPath));
    }

    [Fact]
    public async Task RespectsRecursiveOption()
    {
        var childDirectory = Path.Combine(_tempDirectory, "Chapter 1");
        Directory.CreateDirectory(childDirectory);
        File.WriteAllText(Path.Combine(childDirectory, "001.jpg"), "");

        var nonRecursiveSource = new FolderBookSource(_tempDirectory, recursive: false);
        var recursiveSource = new FolderBookSource(_tempDirectory, recursive: true);

        Assert.Empty(await nonRecursiveSource.LoadPagesAsync(CancellationToken.None));
        Assert.Equal("Chapter 1/001.jpg", (await recursiveSource.LoadPagesAsync(CancellationToken.None)).Single().LogicalPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
