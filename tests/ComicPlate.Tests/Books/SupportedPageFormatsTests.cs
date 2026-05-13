using ComicPlate.Core.Books;

namespace ComicPlate.Tests.Books;

public sealed class SupportedPageFormatsTests
{
    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".bmp")]
    [InlineData(".gif")]
    [InlineData(".JPG")]
    public void AcceptsSupportedPageExtensions(string extension)
    {
        Assert.True(SupportedPageFormats.IsSupportedExtension(extension));
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".zip")]
    [InlineData("")]
    public void RejectsUnsupportedPageExtensions(string extension)
    {
        Assert.False(SupportedPageFormats.IsSupportedExtension(extension));
    }
}
