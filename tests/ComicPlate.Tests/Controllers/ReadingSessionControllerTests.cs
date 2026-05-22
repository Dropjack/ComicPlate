using ComicPlate.App.Controllers;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.Controllers;

public sealed class ReadingSessionControllerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"comicplate-session-{Guid.NewGuid():N}");

    public ReadingSessionControllerTests()
    {
        LocalizationService.Initialize(AppLanguage.English);
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
    public void OpeningBookDoesNotEnterBookInShelfNavigation()
    {
        var controller = CreateController();
        var folderPath = Path.Combine(_rootPath, "Series");
        var book = CreateBook("A.cbz");

        controller.StartAtContentFolder(folderPath);
        controller.NavigateToBook(book);

        Assert.True(controller.CanNavigateUp);
        Assert.Equal(BookSourceKind.Collection, controller.CurrentCollection?.SourceKind);
        Assert.Equal(Path.GetFullPath(folderPath), controller.CurrentCollection?.Path);
    }

    [Fact]
    public void NavigateUpReturnsPreviousCollectionWithoutReaderBook()
    {
        var controller = CreateController();
        var rootPath = Path.Combine(_rootPath, "Root");
        var seriesPath = Path.Combine(rootPath, "Series");
        var book = CreateBook(Path.Combine(seriesPath, "A.cbz"));

        controller.StartAtContentFolder(rootPath);
        controller.NavigateToContentFolder(seriesPath);
        controller.NavigateToBook(book);

        var previous = controller.NavigateUp();

        Assert.NotNull(previous);
        Assert.Equal(BookSourceKind.Collection, previous.SourceKind);
        Assert.Equal(Path.GetFullPath(rootPath), previous.Path);
        Assert.Equal(Path.GetFullPath(rootPath), controller.CurrentCollection?.Path);
    }

    [Fact]
    public void NavigateUpFallsBackToParentDirectoryWhenNavigationStackIsEmpty()
    {
        var controller = CreateController();
        var rootPath = Path.Combine(_rootPath, "Root");
        var seriesPath = Path.Combine(rootPath, "Series");

        controller.StartAtContentFolder(seriesPath);

        Assert.True(controller.CanNavigateUp);
        Assert.Equal(Path.GetFullPath(rootPath), controller.NavigateUp()?.Path);
        Assert.Equal(Path.GetFullPath(rootPath), controller.CurrentCollection?.Path);
    }

    [Fact]
    public void PrepareOpenLastReadingPositionRestoresStandaloneReadableUnitWithoutShelfContext()
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
        Assert.Null(restoredController.CurrentCollection);
        Assert.False(restoredController.CanNavigateUp);
    }

    [Fact]
    public void PrepareOpenLastReadingPositionUsesReadingParentCollectionInsteadOfLastBrowsedCollection()
    {
        var seriesPath = Path.Combine(_rootPath, "Series");
        var otherPath = Path.Combine(_rootPath, "Other");
        var bookPath = Path.Combine(seriesPath, "A.cbz");
        var store = new JsonAppStateStore(_rootPath);
        store.SaveSession(new SessionState(
            1,
            new ReadableUnitState(bookPath, "A.cbz", BookSourceKind.Zip, 7),
            new NavigationEntry(otherPath, "Other", BookSourceKind.Collection),
            new[] { new NavigationEntry(_rootPath, Path.GetFileName(_rootPath), BookSourceKind.Collection) },
            DateTimeOffset.UtcNow)
        {
            ReadingParentCollectionCurrent = new NavigationEntry(seriesPath, "Series", BookSourceKind.Collection),
            ReadingParentCollectionBackStack = new[] { new NavigationEntry(_rootPath, Path.GetFileName(_rootPath), BookSourceKind.Collection) }
        });

        var controller = CreateController();
        var current = controller.PrepareOpenLastReadingPosition();

        Assert.NotNull(current);
        Assert.Equal(bookPath, current.Path);
        Assert.Equal(Path.GetFullPath(seriesPath), controller.CurrentCollection?.Path);
        Assert.True(controller.CanNavigateUp);

        var previous = controller.NavigateUp();

        Assert.NotNull(previous);
        Assert.Equal(Path.GetFullPath(_rootPath), previous.Path);
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
    public void PdfProgressUsesPdfFilePathAsProgressKey()
    {
        var controller = CreateController();
        var bookPath = Path.Combine(_rootPath, "Manual.pdf");
        var book = new BookEntry(bookPath, "Manual.pdf", BookSourceKind.Pdf, bookPath);

        controller.StartAtBook(book);
        controller.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 17,
            pageCount: 291,
            ReadingDirection.LeftToRight,
            ViewMode.SinglePage,
            deleteCompletedProgress: false);

        var progressStore = new JsonAppStateStore(_rootPath).LoadProgress();
        var normalizedPath = JsonAppStateStore.NormalizePath(book.Path);

        Assert.Single(progressStore.Books);
        Assert.True(progressStore.Books.ContainsKey(normalizedPath));
        Assert.Equal(BookSourceKind.Pdf, progressStore.Books[normalizedPath].SourceKind);
        Assert.Equal(17, progressStore.Books[normalizedPath].LastPageIndex);
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

    [Fact]
    public void SaveReadingStateStoresReadingParentCollectionSeparatelyFromLastBrowsedCollection()
    {
        var controller = CreateController();
        var rootPath = Path.Combine(_rootPath, "Root");
        var seriesPath = Path.Combine(rootPath, "Series");
        var otherPath = Path.Combine(rootPath, "Other");
        var book = CreateBook(Path.Combine(seriesPath, "A.cbz"));

        controller.StartAtContentFolder(rootPath);
        controller.NavigateToContentFolder(seriesPath);
        controller.NavigateToBook(book);
        controller.NavigateToContentFolder(otherPath);
        controller.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 2,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: false);

        var session = new JsonAppStateStore(_rootPath).LoadSession();

        Assert.Equal(book.Path, session.Current?.Path);
        Assert.Equal(Path.GetFullPath(seriesPath), session.ReadingParentCollectionCurrent?.Path);
        Assert.Single(session.ReadingParentCollectionBackStack);
        Assert.Equal(Path.GetFullPath(rootPath), session.ReadingParentCollectionBackStack[0].Path);
        Assert.Equal(Path.GetFullPath(seriesPath), session.ShelfCurrent?.Path);
        Assert.Single(session.BackStack);
        Assert.Equal(Path.GetFullPath(rootPath), session.BackStack[0].Path);
    }

    [Fact]
    public void StartingAtHistoryBookOpensStandaloneWithoutShelfRoot()
    {
        var controller = CreateController();
        var rootPath = Path.Combine(_rootPath, "Root");
        var seriesPath = Path.Combine(rootPath, "Series");
        var historyPath = Path.Combine(_rootPath, "History");
        var historyBook = CreateBook(Path.Combine(historyPath, "B.cbz"));

        controller.StartAtContentFolder(rootPath);
        controller.NavigateToContentFolder(seriesPath);
        Assert.True(controller.CanNavigateUp);

        controller.StartAtBook(historyBook);

        Assert.False(controller.CanNavigateUp);
        Assert.Null(controller.CurrentCollection);
        Assert.Null(controller.NavigateUp());
    }

    [Fact]
    public void PrepareOpenLastReadingPositionRestoresFolderBookParentCollectionNavigation()
    {
        var rootPath = Path.Combine(_rootPath, "Root");
        var parentCollectionPath = Path.Combine(rootPath, "Series");
        var folderBookPath = Path.Combine(parentCollectionPath, "Volume");
        Directory.CreateDirectory(folderBookPath);
        var controller = CreateController();
        var book = new BookEntry(folderBookPath, "Volume", BookSourceKind.Folder, folderBookPath);

        controller.StartAtContentFolder(folderBookPath);
        controller.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 104,
            pageCount: 142,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: false);

        var restoredController = CreateController();
        var current = restoredController.PrepareOpenLastReadingPosition();

        Assert.NotNull(current);
        Assert.Equal(folderBookPath, current.Path);
        Assert.Equal(Path.GetFullPath(folderBookPath), restoredController.CurrentCollection?.Path);
        Assert.True(restoredController.CanNavigateUp);
        Assert.Equal(Path.GetFullPath(parentCollectionPath), restoredController.NavigateUp()?.Path);
    }

    [Fact]
    public void PrepareOpenLastReadingPositionKeepsParentCollectionUpNavigationAfterUserNavigatedUpBeforeClose()
    {
        var rootPath = Path.Combine(_rootPath, "Root");
        var parentCollectionPath = Path.Combine(rootPath, "Series");
        var folderBookPath = Path.Combine(parentCollectionPath, "Volume");
        Directory.CreateDirectory(folderBookPath);
        var controller = CreateController();
        var book = new BookEntry(folderBookPath, "Volume", BookSourceKind.Folder, folderBookPath);

        controller.StartAtContentFolder(folderBookPath);
        Assert.Equal(Path.GetFullPath(parentCollectionPath), controller.NavigateUp()?.Path);
        controller.SaveReadingState(
            book,
            hasPages: true,
            currentPageIndex: 104,
            pageCount: 142,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            deleteCompletedProgress: false);

        var restoredController = CreateController();
        var current = restoredController.PrepareOpenLastReadingPosition();

        Assert.NotNull(current);
        Assert.Equal(folderBookPath, current.Path);
        Assert.Equal(Path.GetFullPath(parentCollectionPath), restoredController.CurrentCollection?.Path);
        Assert.True(restoredController.CanNavigateUp);
        Assert.Equal(Path.GetFullPath(rootPath), restoredController.NavigateUp()?.Path);
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
        var path = Path.IsPathFullyQualified(fileName)
            ? fileName
            : Path.Combine(_rootPath, fileName);
        return new BookEntry(path, Path.GetFileName(path), BookSourceKind.Zip, path);
    }
}
