using ComicPlate.Core.Books;

namespace ComicPlate.Tests.Books;

public sealed class EpubBookFormatTests
{
    [Theory]
    [InlineData(".epub")]
    [InlineData(".EPUB")]
    public void AcceptsEpubExtensions(string extension)
    {
        Assert.True(EpubBookFormat.IsSupportedExtension(extension));
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".cbz")]
    [InlineData(".jpg")]
    [InlineData("")]
    public void RejectsNonEpubExtensions(string extension)
    {
        Assert.False(EpubBookFormat.IsSupportedExtension(extension));
    }
}
