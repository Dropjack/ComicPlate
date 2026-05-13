using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class ReaderStripTests
{
    [Fact]
    public void PlacesNextPagesOnTheLeftForRightToLeftReading()
    {
        var strip = new ReaderStrip(neighborPageLimit: 2);
        var pages = CreatePages(5);

        var slots = strip.CreateSlots(pages, currentPageIndex: 2, ReadingDirection.RightToLeft);

        Assert.Equal(new[] { 4, 3, 2, 1, 0 }, slots.Select(slot => slot.PageIndex));
        Assert.True(slots[2].IsCurrent);
    }

    [Fact]
    public void PlacesFirstPageAtRightEdgeForRightToLeftReading()
    {
        var strip = new ReaderStrip(neighborPageLimit: 3);
        var pages = CreatePages(5);

        var slots = strip.CreateSlots(pages, currentPageIndex: 0, ReadingDirection.RightToLeft);

        Assert.Equal(new[] { 3, 2, 1, 0 }, slots.Select(slot => slot.PageIndex));
        Assert.True(slots[^1].IsCurrent);
    }

    [Fact]
    public void PlacesNextPagesOnTheRightForLeftToRightReading()
    {
        var strip = new ReaderStrip(neighborPageLimit: 2);
        var pages = CreatePages(5);

        var slots = strip.CreateSlots(pages, currentPageIndex: 2, ReadingDirection.LeftToRight);

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, slots.Select(slot => slot.PageIndex));
        Assert.True(slots[2].IsCurrent);
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
}
