using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using Avalonia.Media.Imaging;
using System.Runtime.Serialization;

namespace ComicPlate.Tests.Services;

public sealed class ReaderImageCacheTests
{
    [Fact]
    public async Task PreviewCanReturnSamePageImageEvenWhenItIsNotReusable()
    {
        var image = CreateBitmap();
        var cache = new ReaderImageCache((_, _, _) => Task.FromResult(image));
        var cachedRequest = new ReaderImageDecodeRequest(1, 1);
        var requested = new ReaderImageDecodeRequest(100, 100);

        await cache.GetOrLoadAsync(
            pageIndex: 3,
            CreatePage("3.png"),
            cachedRequest,
            CancellationToken.None);

        Assert.Same(image, cache.TryGetPreview(3));
        Assert.Null(cache.TryGetReusable(3, requested));
    }

    [Fact]
    public async Task ReusableRequiresCachedImageToCoverRequest()
    {
        var image = CreateBitmap();
        var cache = new ReaderImageCache((_, _, _) => Task.FromResult(image));
        var request = new ReaderImageDecodeRequest(1, 1);

        await cache.GetOrLoadAsync(
            pageIndex: 4,
            CreatePage("4.png"),
            request,
            CancellationToken.None);

        Assert.Same(image, cache.TryGetReusable(4, request));
    }

    private static PageEntry CreatePage(string name)
    {
        return new PageEntry(
            name,
            name,
            PageSourceKind.FileSystem,
            _ => Task.FromResult<Stream>(new MemoryStream()));
    }

    private static Bitmap CreateBitmap()
    {
#pragma warning disable SYSLIB0050
        return (Bitmap)FormatterServices.GetUninitializedObject(typeof(Bitmap));
#pragma warning restore SYSLIB0050
    }

}
