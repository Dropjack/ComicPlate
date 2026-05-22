using ComicPlate.Core.Books;

namespace ComicPlate.Tests.Books;

public sealed class PdfBookFormatTests
{
    [Theory]
    [InlineData(".pdf")]
    [InlineData(".PDF")]
    public void AcceptsPdfExtensions(string extension)
    {
        Assert.True(PdfBookFormat.IsSupportedExtension(extension));
    }

    [Theory]
    [InlineData(".epub")]
    [InlineData(".zip")]
    [InlineData("")]
    public void RejectsNonPdfExtensions(string extension)
    {
        Assert.False(PdfBookFormat.IsSupportedExtension(extension));
    }
}
