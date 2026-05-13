using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.Tests.Reading;

public sealed class ReaderStateTests
{
    [Fact]
    public void ClampsInitialPageToAvailableRange()
    {
        var state = new ReaderState();

        state.LoadPages(CreatePages(3), 10);

        Assert.Equal(2, state.CurrentPageIndex);
    }

    [Fact]
    public void DoesNotMovePastFirstOrLastPage()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(2));

        state.PreviousPage();
        Assert.Equal(0, state.CurrentPageIndex);

        state.NextPage();
        state.NextPage();
        Assert.Equal(1, state.CurrentPageIndex);
    }

    [Fact]
    public void DoublePageModeMovesByReadingGroupAfterCover()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(6));
        state.SetViewMode(ViewMode.DoublePage);

        state.NextPage();
        Assert.Equal(1, state.CurrentPageIndex);
        Assert.Equal(2, state.CurrentReadingGroupSize);

        state.NextPage();
        Assert.Equal(3, state.CurrentPageIndex);

        state.PreviousPage();
        Assert.Equal(1, state.CurrentPageIndex);
    }

    [Fact]
    public void LastDoublePageGroupCanContainOnePage()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(4));
        state.SetViewMode(ViewMode.DoublePage);

        state.GoToPage(3);

        Assert.Equal(1, state.CurrentReadingGroupSize);
    }

    [Fact]
    public void SinglePageModeReadingGroupContainsOnlyCurrentPage()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(4));

        state.GoToPage(2);

        Assert.Equal(new[] { 2 }, state.CurrentReadingGroupPageIndexes);
    }

    [Fact]
    public void DoublePageModeKeepsCoverAsSinglePageGroup()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(4));
        state.SetViewMode(ViewMode.DoublePage);

        Assert.Equal(new[] { 0 }, state.CurrentReadingGroupPageIndexes);
    }

    [Fact]
    public void DoublePageModeReturnsPairInLeftToRightVisualOrder()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(5));
        state.SetViewMode(ViewMode.DoublePage);
        state.SetReadingDirection(ReadingDirection.LeftToRight);

        state.GoToPage(1);

        Assert.Equal(new[] { 1, 2 }, state.CurrentReadingGroupPageIndexes);
    }

    [Fact]
    public void DoublePageModeReturnsPairInRightToLeftVisualOrder()
    {
        var state = new ReaderState();
        state.LoadPages(CreatePages(5));
        state.SetViewMode(ViewMode.DoublePage);
        state.SetReadingDirection(ReadingDirection.RightToLeft);

        state.GoToPage(1);

        Assert.Equal(new[] { 2, 1 }, state.CurrentReadingGroupPageIndexes);
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
