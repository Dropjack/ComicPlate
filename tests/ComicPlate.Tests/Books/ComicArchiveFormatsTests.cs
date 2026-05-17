using ComicPlate.Core.Books;

namespace ComicPlate.Tests.Books;

public sealed class ComicArchiveFormatsTests
{
    [Theory]
    [InlineData("comic.zip", ComicArchiveKind.Zip, BookSourceKind.Zip)]
    [InlineData("comic.cbz", ComicArchiveKind.Zip, BookSourceKind.Zip)]
    [InlineData("comic.rar", ComicArchiveKind.Rar, BookSourceKind.Rar)]
    [InlineData("comic.cbr", ComicArchiveKind.Rar, BookSourceKind.Rar)]
    [InlineData("COMIC.CBR", ComicArchiveKind.Rar, BookSourceKind.Rar)]
    public void MapsSupportedArchiveExtensions(string path, ComicArchiveKind archiveKind, BookSourceKind sourceKind)
    {
        var found = ComicArchiveFormats.TryGetByPath(path, out var format);

        Assert.True(found);
        Assert.Equal(archiveKind, format.ArchiveKind);
        Assert.Equal(sourceKind, format.SourceKind);
    }

    [Theory]
    [InlineData("comic.7z")]
    [InlineData("comic.cb7")]
    [InlineData("comic.pdf")]
    public void RejectsUnsupportedArchiveExtensions(string path)
    {
        Assert.False(ComicArchiveFormats.TryGetByPath(path, out _));
    }
}

