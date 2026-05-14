using ComicPlate.App.Services;
using ComicPlate.Core.Books;

namespace ComicPlate.Tests.Services;

public sealed class ReaderImageDecodeRequestTests
{
    [Fact]
    public void CreateScalesDisplaySize()
    {
        var request = ReaderImageDecodeRequest.Create(
            displayWidth: 800,
            displayHeight: 1200,
            PageImageInfo.Unknown);

        Assert.Equal(1200, request.PixelWidth);
        Assert.Equal(1800, request.PixelHeight);
    }

    [Fact]
    public void CreateDoesNotRequestMoreThanSourceSize()
    {
        var request = ReaderImageDecodeRequest.Create(
            displayWidth: 800,
            displayHeight: 1200,
            new PageImageInfo(900, 1400));

        Assert.Equal(900, request.PixelWidth);
        Assert.Equal(1400, request.PixelHeight);
    }

    [Fact]
    public void CreateCapsExtremelyLargeRequests()
    {
        var request = ReaderImageDecodeRequest.Create(
            displayWidth: 10000,
            displayHeight: 8000,
            PageImageInfo.Unknown);

        Assert.Equal(ReaderImageDecodeRequest.MaximumDecodeDimension, request.PixelWidth);
        Assert.Equal(ReaderImageDecodeRequest.MaximumDecodeDimension, request.PixelHeight);
    }

    [Fact]
    public void ReuseRequiresCachedRequestToCoverNewRequest()
    {
        var cached = new ReaderImageDecodeRequest(1200, 1800);
        var larger = new ReaderImageDecodeRequest(1600, 2400);

        Assert.False(cached.CanReuseFor(larger));
    }

    [Fact]
    public void ReuseRejectsMuchLargerCachedImageSoItCanBeDownsized()
    {
        var cached = new ReaderImageDecodeRequest(3000, 3000);
        var smaller = new ReaderImageDecodeRequest(1000, 1000);

        Assert.False(cached.CanReuseFor(smaller));
    }
}
