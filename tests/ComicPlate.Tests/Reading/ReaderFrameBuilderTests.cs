using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class ReaderFrameBuilderTests
{
    [Fact]
    public void BuildsRightToLeftSpreadInVisualOrder()
    {
        var builder = new ReaderFrameBuilder();

        var frames = builder.Build(
            CreatePages(4),
            CreatePortraitInfos(4),
            currentPageIndex: 1,
            ViewMode.DoublePage,
            ReadingDirection.RightToLeft);

        Assert.Equal(3, frames.Count);
        Assert.Equal(new[] { 0 }, frames[0].PageIndexes);
        Assert.Equal(new[] { 2, 1 }, frames[1].PageIndexes);
        Assert.Equal(new[] { 3 }, frames[2].PageIndexes);
        Assert.True(frames[1].IsCurrent);
    }

    [Fact]
    public void KeepsWidePageAsSingleFrame()
    {
        var builder = new ReaderFrameBuilder();
        var infos = new[]
        {
            new PageImageInfo(800, 1200),
            new PageImageInfo(800, 1200),
            new PageImageInfo(1800, 900),
            new PageImageInfo(800, 1200)
        };

        var frames = builder.Build(
            CreatePages(4),
            infos,
            currentPageIndex: 2,
            ViewMode.DoublePage,
            ReadingDirection.LeftToRight);

        Assert.Equal(new[] { 0 }, frames[0].PageIndexes);
        Assert.Equal(new[] { 1 }, frames[1].PageIndexes);
        Assert.Equal(new[] { 2 }, frames[2].PageIndexes);
        Assert.Equal(new[] { 3 }, frames[3].PageIndexes);
        Assert.Equal(ReaderFrameKind.WideSingle, frames[2].Kind);
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
