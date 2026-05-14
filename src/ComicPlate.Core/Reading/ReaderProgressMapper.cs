namespace ComicPlate.Core.Reading;

public static class ReaderProgressMapper
{
    public static int RatioToPageIndex(
        double visualRatio,
        int pageCount,
        ReadingDirection readingDirection)
    {
        if (pageCount <= 0)
        {
            return 0;
        }

        var lastPageIndex = pageCount - 1;
        var clampedRatio = Math.Clamp(visualRatio, 0, 1);
        var visualProgressIndex = (int)Math.Round(clampedRatio * lastPageIndex, MidpointRounding.AwayFromZero);
        return readingDirection == ReadingDirection.RightToLeft
            ? lastPageIndex - visualProgressIndex
            : visualProgressIndex;
    }
}
