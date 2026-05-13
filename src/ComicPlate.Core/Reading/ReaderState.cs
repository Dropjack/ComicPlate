using ComicPlate.Core.Books;

namespace ComicPlate.Core.Reading;

public sealed class ReaderState
{
    private IReadOnlyList<PageEntry> _pages = Array.Empty<PageEntry>();

    public IReadOnlyList<PageEntry> Pages => _pages;

    public int CurrentPageIndex { get; private set; }

    public int PageCount => _pages.Count;

    public bool HasPages => PageCount > 0;

    public bool CanGoNext => HasPages && CurrentPageIndex < PageCount - 1;

    public bool CanGoPrevious => HasPages && CurrentPageIndex > 0;

    public int CurrentReadingGroupSize => ViewMode == ViewMode.DoublePage && CurrentPageIndex > 0
        ? Math.Min(2, PageCount - CurrentPageIndex)
        : Math.Min(1, PageCount);

    public ViewMode ViewMode { get; private set; } = ViewMode.SinglePage;

    public ReadingDirection ReadingDirection { get; private set; } = ReadingDirection.RightToLeft;

    public IReadOnlyList<int> CurrentReadingGroupPageIndexes
    {
        get
        {
            if (!HasPages)
            {
                return Array.Empty<int>();
            }

            if (ViewMode == ViewMode.SinglePage || CurrentPageIndex == 0)
            {
                return new[] { CurrentPageIndex };
            }

            var group = Enumerable.Range(
                    CurrentPageIndex,
                    Math.Min(2, PageCount - CurrentPageIndex))
                .ToArray();

            if (ReadingDirection == ReadingDirection.RightToLeft)
            {
                Array.Reverse(group);
            }

            return group;
        }
    }

    public void LoadPages(IReadOnlyList<PageEntry> pages, int initialPageIndex = 0)
    {
        _pages = pages;
        CurrentPageIndex = NormalizePageIndexForViewMode(ClampPageIndex(initialPageIndex));
    }

    public void GoToPage(int pageIndex)
    {
        CurrentPageIndex = NormalizePageIndexForViewMode(ClampPageIndex(pageIndex));
    }

    public void GoToFrameStartPage(int pageIndex)
    {
        CurrentPageIndex = ClampPageIndex(pageIndex);
    }

    public void GoToFirstPage()
    {
        CurrentPageIndex = 0;
    }

    public void GoToLastPage()
    {
        CurrentPageIndex = NormalizePageIndexForViewMode(HasPages ? PageCount - 1 : 0);
    }

    public void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPageIndex = ClampPageIndex(CurrentPageIndex + GetReadingGroupStep());
        }
    }

    public void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPageIndex = ClampPageIndex(CurrentPageIndex - GetReadingGroupStep());
        }
    }

    public void SetViewMode(ViewMode viewMode)
    {
        ViewMode = viewMode;
        CurrentPageIndex = NormalizePageIndexForViewMode(CurrentPageIndex);
    }

    public void SetReadingDirection(ReadingDirection readingDirection)
    {
        ReadingDirection = readingDirection;
    }

    private int ClampPageIndex(int pageIndex)
    {
        if (!HasPages)
        {
            return 0;
        }

        return Math.Clamp(pageIndex, 0, PageCount - 1);
    }

    private int NormalizePageIndexForViewMode(int pageIndex)
    {
        if (ViewMode != ViewMode.DoublePage || pageIndex <= 0)
        {
            return pageIndex;
        }

        return pageIndex % 2 == 0 ? pageIndex - 1 : pageIndex;
    }

    private int GetReadingGroupStep()
    {
        return ViewMode == ViewMode.DoublePage && CurrentPageIndex > 0
            ? 2
            : 1;
    }
}
