using ComicPlate.App.Services;
using ComicPlate.Core.Books;

namespace ComicPlate.Tests.Services;

public sealed class ContentOpenServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"comicplate-open-{Guid.NewGuid():N}");
    private readonly ContentOpenService _service = new();

    public ContentOpenServiceTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void ClassifiesFolderAsContentFolder()
    {
        var result = _service.ClassifyPath(_rootPath);

        Assert.Equal(OpenPathKind.ContentFolder, result.Kind);
        Assert.Equal(Path.GetFullPath(_rootPath), result.Path);
        Assert.Null(result.Book);
    }

    [Fact]
    public void ClassifiesMissingPath()
    {
        var result = _service.ClassifyPath(Path.Combine(_rootPath, "missing.cbz"));

        Assert.Equal(OpenPathKind.Missing, result.Kind);
        Assert.Null(result.Book);
    }

    [Fact]
    public void ClassifiesUnsupportedFile()
    {
        var path = Path.Combine(_rootPath, "notes.txt");
        File.WriteAllText(path, "not a comic");

        var result = _service.ClassifyPath(path);

        Assert.Equal(OpenPathKind.Unsupported, result.Kind);
        Assert.Null(result.Book);
    }

    [Theory]
    [InlineData("comic.zip", BookSourceKind.Zip)]
    [InlineData("comic.cbz", BookSourceKind.Zip)]
    [InlineData("001.jpg", BookSourceKind.Image)]
    public void ClassifiesReadableFilesAsBooks(string fileName, BookSourceKind sourceKind)
    {
        var path = Path.Combine(_rootPath, fileName);
        File.WriteAllBytes(path, Array.Empty<byte>());

        var result = _service.ClassifyPath(path);

        Assert.Equal(OpenPathKind.Book, result.Kind);
        Assert.NotNull(result.Book);
        Assert.Equal(sourceKind, result.Book.SourceKind);
        Assert.Equal(Path.GetFullPath(path), result.Book.Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
