using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class ReaderFramePageTextFormatterTests
{
    [Fact]
    public void FormatsSingleFrameAsSinglePage()
    {
        var frame = CreateFrame(ReaderFrameKind.Single, 13);

        Assert.Equal("14 / 58", ReaderFramePageTextFormatter.Format(frame, fallbackPageIndex: 0, pageCount: 58));
    }

    [Fact]
    public void FormatsWideSingleFrameAsSinglePage()
    {
        var frame = CreateFrame(ReaderFrameKind.WideSingle, 13);

        Assert.Equal("14 / 58", ReaderFramePageTextFormatter.Format(frame, fallbackPageIndex: 0, pageCount: 58));
    }

    [Fact]
    public void FormatsSpreadFrameAsLogicalPageRange()
    {
        var frame = CreateFrame(ReaderFrameKind.Spread, 14, 13);

        Assert.Equal("14-15 / 58", ReaderFramePageTextFormatter.Format(frame, fallbackPageIndex: 0, pageCount: 58));
    }

    [Fact]
    public void FallsBackToCurrentPageWhenFrameIsMissing()
    {
        Assert.Equal("5 / 58", ReaderFramePageTextFormatter.Format(null, fallbackPageIndex: 4, pageCount: 58));
    }

    [Fact]
    public void FormatsEmptyBook()
    {
        Assert.Equal("0 / 0", ReaderFramePageTextFormatter.Format(null, fallbackPageIndex: 0, pageCount: 0));
    }

    private static ReaderFrame CreateFrame(ReaderFrameKind kind, params int[] pageIndexes)
    {
        var pages = pageIndexes
            .Select(index => new ReaderFramePage(
                index,
                index + 1,
                new PageEntry(
                    $"{index + 1}.jpg",
                    $"{index + 1}.jpg",
                    PageSourceKind.FileSystem,
                    _ => Task.FromResult<Stream>(new MemoryStream())),
                PageImageInfo.Unknown))
            .ToArray();
        return new ReaderFrame(0, kind, pages, true);
    }
}
