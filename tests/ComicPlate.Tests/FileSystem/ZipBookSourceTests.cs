using System.IO.Compression;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class ZipBookSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlateZipTests-{Guid.NewGuid():N}");

    public ZipBookSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsSupportedImagesInNaturalOrder()
    {
        var zipPath = Path.Combine(_tempDirectory, "comic.cbz");
        CreateZip(
            zipPath,
            "chapter/10.jpg",
            "chapter/2.png",
            "chapter/readme.txt",
            "chapter/1.JPG",
            "nested.zip");

        var source = new ZipBookSource(zipPath);

        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Equal(new[] { "chapter/1.JPG", "chapter/2.png", "chapter/10.jpg" }, pages.Select(page => page.LogicalPath));
    }

    [Fact]
    public async Task OpensZipEntriesAsSeekableStreams()
    {
        var zipPath = Path.Combine(_tempDirectory, "comic.zip");
        CreateZip(zipPath, "001.jpg");
        var source = new ZipBookSource(zipPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);

        await using var stream = await pages[0].OpenStreamAsync(CancellationToken.None);

        Assert.True(stream.CanSeek);
        Assert.Equal(0, stream.Position);
        using var reader = new StreamReader(stream);
        Assert.Equal("test", await reader.ReadToEndAsync());
    }

    private static void CreateZip(string path, params string[] entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        foreach (var entryName in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write("test");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
