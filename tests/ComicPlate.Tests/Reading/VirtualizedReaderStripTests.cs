using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class VirtualizedReaderStripTests
{
    [Fact]
    public void CreatesRightToLeftWindowInVisualOrder()
    {
        var strip = new VirtualizedReaderStrip(neighborPageLimit: 2);

        var window = strip.CreateWindow(pageCount: 7, currentPageIndex: 3, ReadingDirection.RightToLeft);

        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, window);
    }

    [Fact]
    public void CentersCurrentSlot()
    {
        var strip = new VirtualizedReaderStrip(neighborPageLimit: 2);
        var window = new[] { 4, 3, 2 };
        var extents = window.ToDictionary(index => index, _ => 100.0);
        var layout = strip.CreateLayout(window, currentPageIndex: 3, extents);

        var offset = strip.GetCenteredOffset(layout, currentPageIndex: 3, viewportWidth: 500);

        Assert.Equal(100, offset);
    }

    [Fact]
    public void FindsNearestPageFromViewportCenter()
    {
        var strip = new VirtualizedReaderStrip(neighborPageLimit: 2);
        var window = new[] { 4, 3, 2 };
        var extents = window.ToDictionary(index => index, _ => 100.0);
        var layout = strip.CreateLayout(window, currentPageIndex: 3, extents);

        var nearest = strip.FindNearestPageIndex(
            layout,
            viewportWidth: 500,
            stripOffsetX: 200,
            fallbackPageIndex: 3);

        Assert.Equal(4, nearest);
    }
}
