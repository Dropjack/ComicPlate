using ComicPlate.App.Controllers;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.Controllers;

public sealed class ReadingSessionControllerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"comicplate-session-{Guid.NewGuid():N}");

    public ReadingSessionControllerTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void StartsWithoutLastReadingPosition()
    {
        var controller = CreateController();

        Assert.False(controller.CanOpenLastReadingPosition);
        Assert.Equal("Continue Reading", controller.LastReadingPositionText);
    }

    [Fact]
    public void SaveReadingStateUpdatesLastReadingPosition()
    {
        var controller = CreateController();
        var book = CreateBook("A.cbz");

        controller.StartAtBook(book);
        controller.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 4,
            pageCount: 10,
            ReadingDirection.RightToLeft,
            ViewMode.DoublePage,
            deleteCompletedProgress: false);

        Assert.True(controller.CanOpenLastReadingPosition);
        Assert.Equal("Continue Reading \"A.cbz\"", controller.LastReadingPositionText);
        Assert.Equal(4, controller.FindProgress(book.Path)?.LastPageIndex);
    }

    [Fact]
    public void BackReturnsPreviousContentFolder()
    {
        var controller = CreateController();
        var folderPath = Path.Combine(_rootPath, "Series");
        var book = CreateBook("A.cbz");

        controller.StartAtContentFolder(folderPath);
        controller.NavigateToBook(book);

        var previous = controller.Back();

        Assert.NotNull(previous);
        Assert.Equal(BookSourceKind.Collection, previous.SourceKind);
        Assert.Equal(Path.GetFullPath(folderPath), previous.Path);
    }

    [Fact]
    public void PrepareOpenLastReadingPositionRestoresCurrentReadableUnit()
    {
        var controller = CreateController();
        var book = CreateBook("A.cbz");
        controller.StartAtBook(book);
        controller.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 3,
            pageCount: 10,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: false);

        var restoredController = CreateController();
        var current = restoredController.PrepareOpenLastReadingPosition();

        Assert.NotNull(current);
        Assert.Equal(book.Path, current.Path);
        Assert.Equal(3, current.LastPageIndex);
    }

    [Fact]
    public void MultipleControllersForSameBookUseSingleProgressRecordWithLastWriterWinning()
    {
        var firstWindow = CreateController();
        var secondWindow = CreateController();
        var book = CreateBook("Shared.cbz");

        firstWindow.StartAtBook(book);
        secondWindow.StartAtBook(book);

        firstWindow.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 10,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: true);
        secondWindow.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 50,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.DoublePage,
            deleteCompletedProgress: true);

        var progressStore = new JsonAppStateStore(_rootPath).LoadProgress();
        var normalizedPath = JsonAppStateStore.NormalizePath(book.Path);

        Assert.Single(progressStore.Books);
        Assert.True(progressStore.Books.ContainsKey(normalizedPath));
        Assert.Equal(50, progressStore.Books[normalizedPath].LastPageIndex);
        Assert.Equal(ViewMode.DoublePage, progressStore.Books[normalizedPath].ViewMode);
    }

    [Fact]
    public void MultipleControllersUpdateSingleLastSessionWithLastWriterWinning()
    {
        var firstWindow = CreateController();
        var secondWindow = CreateController();
        var firstBook = CreateBook("A.cbz");
        var secondBook = CreateBook("B.cbz");

        firstWindow.StartAtBook(firstBook);
        secondWindow.StartAtBook(secondBook);

        firstWindow.SaveReadingState(
            firstBook,
            hasPages: true,
            currentPageIndex: 12,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: true);
        secondWindow.SaveReadingState(
            secondBook,
            hasPages: true,
            currentPageIndex: 34,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: true);

        var session = new JsonAppStateStore(_rootPath).LoadSession();

        Assert.Equal(secondBook.Path, session.Current?.Path);
        Assert.Equal(34, session.Current?.LastPageIndex);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private ReadingSessionController CreateController()
    {
        return new ReadingSessionController(new JsonAppStateStore(_rootPath));
    }

    private BookEntry CreateBook(string fileName)
    {
        var path = Path.Combine(_rootPath, fileName);
        return new BookEntry(path, fileName, BookSourceKind.Zip, path);
    }
}
