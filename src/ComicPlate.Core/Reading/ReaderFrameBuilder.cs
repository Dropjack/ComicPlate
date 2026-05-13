using ComicPlate.Core.Books;

namespace ComicPlate.Core.Reading;

public sealed class ReaderFrameBuilder
{
    public const double WidePageAspectRatio = 1.25;

    public IReadOnlyList<ReaderFrame> Build(
        IReadOnlyList<PageEntry> pages,
        IReadOnlyList<PageImageInfo> imageInfos,
        int currentPageIndex,
        ViewMode viewMode,
        ReadingDirection readingDirection)
    {
        if (pages.Count == 0)
        {
            return Array.Empty<ReaderFrame>();
        }

        var frames = new List<ReaderFrame>();
        var pageIndex = 0;

        while (pageIndex < pages.Count)
        {
            var framePages = CreateFramePages(pages, imageInfos, pageIndex, viewMode, readingDirection);
            var kind = GetFrameKind(framePages);
            var isCurrent = framePages.Any(page => page.PageIndex == currentPageIndex);
            frames.Add(new ReaderFrame(frames.Count, kind, framePages, isCurrent));
            pageIndex += framePages.Count;
        }

        return frames;
    }

    private static IReadOnlyList<ReaderFramePage> CreateFramePages(
        IReadOnlyList<PageEntry> pages,
        IReadOnlyList<PageImageInfo> imageInfos,
        int pageIndex,
        ViewMode viewMode,
        ReadingDirection readingDirection)
    {
        if (viewMode == ViewMode.SinglePage || pageIndex == 0 || IsWidePage(imageInfos, pageIndex))
        {
            return new[] { CreateFramePage(pages, imageInfos, pageIndex) };
        }

        if (pageIndex + 1 >= pages.Count || IsWidePage(imageInfos, pageIndex + 1))
        {
            return new[] { CreateFramePage(pages, imageInfos, pageIndex) };
        }

        var pair = new[]
        {
            CreateFramePage(pages, imageInfos, pageIndex),
            CreateFramePage(pages, imageInfos, pageIndex + 1)
        };

        return readingDirection == ReadingDirection.RightToLeft
            ? pair.Reverse().ToArray()
            : pair;
    }

    private static ReaderFrameKind GetFrameKind(IReadOnlyList<ReaderFramePage> pages)
    {
        if (pages.Count > 1)
        {
            return ReaderFrameKind.Spread;
        }

        return pages[0].ImageInfo.AspectRatio >= WidePageAspectRatio
            ? ReaderFrameKind.WideSingle
            : ReaderFrameKind.Single;
    }

    private static ReaderFramePage CreateFramePage(
        IReadOnlyList<PageEntry> pages,
        IReadOnlyList<PageImageInfo> imageInfos,
        int pageIndex)
    {
        return new ReaderFramePage(
            pageIndex,
            pageIndex + 1,
            pages[pageIndex],
            GetImageInfo(imageInfos, pageIndex));
    }

    private static bool IsWidePage(IReadOnlyList<PageImageInfo> imageInfos, int pageIndex)
    {
        return GetImageInfo(imageInfos, pageIndex).AspectRatio >= WidePageAspectRatio;
    }

    private static PageImageInfo GetImageInfo(IReadOnlyList<PageImageInfo> imageInfos, int pageIndex)
    {
        return pageIndex >= 0 && pageIndex < imageInfos.Count
            ? imageInfos[pageIndex]
            : PageImageInfo.Unknown;
    }
}
