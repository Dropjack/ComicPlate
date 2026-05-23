using ComicPlate.App.Controllers;
using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;
using System.Runtime.Serialization;

namespace ComicPlate.Tests.Controllers;

public sealed class ReaderStripRefreshCoordinatorTests
{
    [Fact]
    public void BeginRefreshAdvancesCurrentVersion()
    {
        using var coordinator = new ReaderStripRefreshCoordinator(TimeSpan.FromMilliseconds(1));

        var first = coordinator.BeginRefresh();
        var second = coordinator.BeginRefresh();

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.False(coordinator.IsCurrent(first));
        Assert.True(coordinator.IsCurrent(second));
    }

    [Fact]
    public async Task QueueViewportRefreshKeepsOnlyLatestRequest()
    {
        using var coordinator = new ReaderStripRefreshCoordinator(TimeSpan.FromMilliseconds(10));
        var committed = new List<int>();

        coordinator.QueueViewportRefresh(
            new ReaderStripPlacement(1, 100),
            placement =>
            {
                committed.Add(placement!.AnchorPageIndex);
                return Task.CompletedTask;
            });
        coordinator.QueueViewportRefresh(
            new ReaderStripPlacement(2, 100),
            placement =>
            {
                committed.Add(placement!.AnchorPageIndex);
                return Task.CompletedTask;
            });

        await Task.Delay(80);

        Assert.Equal(new[] { 2 }, committed);
    }

    [Fact]
    public async Task StartImageLoadKeepsSamePagePreviewVisibleWhileSharperImageLoads()
    {
        var preview = CreateBitmap();
        var sharper = CreateBitmap();
        var delayedImage = new TaskCompletionSource<Bitmap>();
        var cache = new ReaderImageCache((_, request, _) =>
            request.PixelWidth <= 1
                ? Task.FromResult(preview)
                : delayedImage.Task);
        await cache.GetOrLoadAsync(
            pageIndex: 0,
            CreatePage("preview.png"),
            new ReaderImageDecodeRequest(1, 1),
            CancellationToken.None);
        using var coordinator = new ReaderStripRefreshCoordinator(TimeSpan.FromMilliseconds(1));
        var refreshVersion = coordinator.BeginRefresh();
        var item = new ReaderStripItemViewModel(
            new ReaderStripSlot(
                0,
                1,
                CreatePage("full.png"),
                IsCurrent: true),
            new PageImageInfo(1000, 1000));
        item.SetDisplaySize(1000, 1000);

        coordinator.StartImageLoad(
            [item],
            refreshVersion,
            currentPageIndex: 0,
            cache,
            isItemVisible: visibleItem => ReferenceEquals(visibleItem, item));

        await WaitUntilAsync(() => item.Image is not null);

        Assert.Same(preview, item.Image);

        delayedImage.SetResult(sharper);
        await WaitUntilAsync(() => !ReferenceEquals(preview, item.Image));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("Condition was not reached before the test timeout.");
            }

            await Task.Delay(10);
        }
    }

    private static PageEntry CreatePage(
        string name,
        Func<CancellationToken, Task<Stream>>? openStreamAsync = null)
    {
        return new PageEntry(
            name,
            name,
            PageSourceKind.FileSystem,
            openStreamAsync ?? (_ => Task.FromResult<Stream>(new MemoryStream())));
    }

    private static Bitmap CreateBitmap()
    {
#pragma warning disable SYSLIB0050
        return (Bitmap)FormatterServices.GetUninitializedObject(typeof(Bitmap));
#pragma warning restore SYSLIB0050
    }

}
