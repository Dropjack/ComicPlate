using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class SingleImageBookSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ComicPlateSingleImageTests-{Guid.NewGuid():N}");

    public SingleImageBookSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsSingleImageAsSinglePage()
    {
        var imagePath = Path.Combine(_tempDirectory, "001.jpg");
        await File.WriteAllTextAsync(imagePath, "fake image");
        var source = new SingleImageBookSource(imagePath);

        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Single(pages);
        Assert.Equal("001.jpg", pages[0].DisplayName);
        Assert.Equal("001.jpg", pages[0].LogicalPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
