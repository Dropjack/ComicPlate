namespace ComicPlate.Core.Reading;

public static class ReaderFramePageTextFormatter
{
    public static string Format(ReaderFrame? frame, int fallbackPageIndex, int pageCount)
    {
        if (pageCount <= 0)
        {
            return "0 / 0";
        }

        if (frame is null || frame.PageIndexes.Count == 0)
        {
            return $"{Math.Clamp(fallbackPageIndex, 0, pageCount - 1) + 1} / {pageCount}";
        }

        if (frame.Kind != ReaderFrameKind.Spread || frame.PageIndexes.Count == 1)
        {
            return $"{frame.PageIndexes.Min() + 1} / {pageCount}";
        }

        return $"{frame.PageIndexes.Min() + 1}-{frame.PageIndexes.Max() + 1} / {pageCount}";
    }
}
