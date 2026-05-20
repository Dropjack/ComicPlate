using ComicPlate.App.Controllers;
using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.ViewModels;

public sealed class ReaderStripItemBuilderTests
{
    [Fact]
    public void BuildWindowItemsCreatesCurrentSlotsAndActiveIndexes()
    {
        var builder = new ReaderStripItemBuilder();
        var stripController = new ReaderStripController(neighborPageLimit: 2);
        stripController.SetViewportSize(1000, 800);
        var frames = new ReaderFrameBuilder().Build(
            CreatePages(5),
            CreatePortraitInfos(5),
            currentPageIndex: 1,
            ViewMode.DoublePage,
            ReadingDirection.RightToLeft);
        var currentFrame = frames.Single(frame => frame.IsCurrent);

        var result = builder.BuildWindowItems(
            frames,
            currentFrame,
            stripController,
            ReadingDirection.RightToLeft);

        Assert.Equal(new[] { 4, 3, 2, 1, 0 }, result.Items.Select(item => item.PageIndex));
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, result.ActivePageIndexes.Order());
        Assert.True(result.Items.Single(item => item.PageIndex == 1).IsCurrent);
        Assert.True(result.Items.Single(item => item.PageIndex == 2).IsCurrent);
        Assert.Equal(800, result.Items[0].DisplayHeight);
        Assert.Equal(533.333, result.Items[0].DisplayWidth, 3);
    }

    [Fact]
    public void UpdateVisibleItemSizesRefreshesExistingItemsForNewViewport()
    {
        var builder = new ReaderStripItemBuilder();
        var stripController = new ReaderStripController(neighborPageLimit: 2);
        stripController.SetViewportSize(1000, 800);
        var frames = new ReaderFrameBuilder().Build(
            CreatePages(3),
            CreatePortraitInfos(3),
            currentPageIndex: 1,
            ViewMode.SinglePage,
            ReadingDirection.LeftToRight);
        var currentFrame = frames.Single(frame => frame.IsCurrent);
        var result = builder.BuildWindowItems(
            frames,
            currentFrame,
            stripController,
            ReadingDirection.LeftToRight);

        stripController.SetViewportSize(1000, 600);
        var updated = builder.UpdateVisibleItemSizes(
            frames,
            result.Items,
            stripController,
            ReadingDirection.LeftToRight);

        Assert.True(updated);
        Assert.All(result.Items, item => Assert.Equal(600, item.DisplayHeight));
        Assert.All(result.Items, item => Assert.Equal(400, item.DisplayWidth));
    }

    private static IReadOnlyList<PageEntry> CreatePages(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new PageEntry(
                $"{index + 1}.jpg",
                $"{index + 1}.jpg",
                PageSourceKind.FileSystem,
                _ => Task.FromResult<Stream>(new MemoryStream())))
            .ToArray();
    }

    private static IReadOnlyList<PageImageInfo> CreatePortraitInfos(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new PageImageInfo(800, 1200))
            .ToArray();
    }
}
