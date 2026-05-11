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

    public ViewMode ViewMode { get; private set; } = ViewMode.SinglePage;

    public ReadingDirection ReadingDirection { get; private set; } = ReadingDirection.LeftToRight;

    public void LoadPages(IReadOnlyList<PageEntry> pages, int initialPageIndex = 0)
    {
        _pages = pages;
        CurrentPageIndex = ClampPageIndex(initialPageIndex);
    }

    public void GoToPage(int pageIndex)
    {
        CurrentPageIndex = ClampPageIndex(pageIndex);
    }

    public void GoToFirstPage()
    {
        CurrentPageIndex = 0;
    }

    public void GoToLastPage()
    {
        CurrentPageIndex = HasPages ? PageCount - 1 : 0;
    }

    public void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPageIndex++;
        }
    }

    public void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPageIndex--;
        }
    }

    public void SetViewMode(ViewMode viewMode)
    {
        ViewMode = viewMode;
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
}
