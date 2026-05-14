using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class ReaderProgressMapperTests
{
    [Fact]
    public void MapsLeftToRightRatioToPageIndex()
    {
        Assert.Equal(0, ReaderProgressMapper.RatioToPageIndex(0, 10, ReadingDirection.LeftToRight));
        Assert.Equal(9, ReaderProgressMapper.RatioToPageIndex(1, 10, ReadingDirection.LeftToRight));
        Assert.Equal(5, ReaderProgressMapper.RatioToPageIndex(0.5, 10, ReadingDirection.LeftToRight));
    }

    [Fact]
    public void MapsRightToLeftRatioToReversedPageIndex()
    {
        Assert.Equal(9, ReaderProgressMapper.RatioToPageIndex(0, 10, ReadingDirection.RightToLeft));
        Assert.Equal(0, ReaderProgressMapper.RatioToPageIndex(1, 10, ReadingDirection.RightToLeft));
        Assert.Equal(4, ReaderProgressMapper.RatioToPageIndex(0.5, 10, ReadingDirection.RightToLeft));
    }

    [Fact]
    public void ClampsRatioToReadableRange()
    {
        Assert.Equal(0, ReaderProgressMapper.RatioToPageIndex(-1, 10, ReadingDirection.LeftToRight));
        Assert.Equal(9, ReaderProgressMapper.RatioToPageIndex(2, 10, ReadingDirection.LeftToRight));
    }

    [Fact]
    public void EmptyBookMapsToZero()
    {
        Assert.Equal(0, ReaderProgressMapper.RatioToPageIndex(0.5, 0, ReadingDirection.LeftToRight));
    }
}
