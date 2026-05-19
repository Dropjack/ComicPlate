using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.Persistence;

public sealed class JsonAppStateStoreTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ComicPlatePersistenceTests-{Guid.NewGuid():N}");

    public JsonAppStateStoreTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void SavesAndLoadsSession()
    {
        var store = new JsonAppStateStore(_tempDirectory);
        var session = new SessionState(
            1,
            new ReadableUnitState(@"D:\Manga\A.cbz", "A.cbz", BookSourceKind.Zip, 100),
            new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection),
            new[] { new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection) },
            DateTimeOffset.Parse("2026-05-13T10:30:00Z"))
        {
            ReadingShelfCurrent = new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection),
            ReadingShelfBackStack = new[] { new NavigationEntry(@"D:\", "D:", BookSourceKind.Collection) },
            ReadingContainerCurrent = new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection),
            ReadingContainerBackStack = new[] { new NavigationEntry(@"D:\", "D:", BookSourceKind.Collection) },
            ReadingParentShelfCurrent = new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection),
            ReadingParentShelfBackStack = new[] { new NavigationEntry(@"D:\", "D:", BookSourceKind.Collection) },
            ReadingParentCollectionCurrent = new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection),
            ReadingParentCollectionBackStack = new[] { new NavigationEntry(@"D:\", "D:", BookSourceKind.Collection) }
        };

        store.SaveSession(session);
        var loaded = store.LoadSession();

        Assert.Equal("A.cbz", loaded.Current?.DisplayName);
        Assert.Equal(100, loaded.Current?.LastPageIndex);
        Assert.Equal("Manga", loaded.ReadingContainerCurrent?.DisplayName);
        Assert.Single(loaded.ReadingContainerBackStack);
        Assert.Equal("Manga", loaded.ReadingParentCollectionCurrent?.DisplayName);
        Assert.Single(loaded.ReadingParentCollectionBackStack);
        Assert.Equal("Manga", loaded.ShelfCurrent?.DisplayName);
        Assert.Single(loaded.BackStack);
        Assert.Equal("Manga", loaded.BackStack[0].DisplayName);
    }

    [Fact]
    public void LoadsSessionWithoutShelfCurrentForBackwardCompatibility()
    {
        var store = new JsonAppStateStore(_tempDirectory);
        File.WriteAllText(
            Path.Combine(_tempDirectory, "session.json"),
            """
            {
              "Version": 1,
              "Current": {
                "Path": "D:\\Manga\\A.cbz",
                "DisplayName": "A.cbz",
                "SourceKind": "Zip",
                "LastPageIndex": 12
              },
              "BackStack": [],
              "SavedAt": "2026-05-13T10:30:00Z"
            }
            """);

        var loaded = store.LoadSession();

        Assert.Equal("A.cbz", loaded.Current?.DisplayName);
        Assert.Equal(12, loaded.Current?.LastPageIndex);
        Assert.Null(loaded.ReadingShelfCurrent);
        Assert.Empty(loaded.ReadingShelfBackStack);
        Assert.Null(loaded.ReadingContainerCurrent);
        Assert.Empty(loaded.ReadingContainerBackStack);
        Assert.Null(loaded.ReadingParentShelfCurrent);
        Assert.Empty(loaded.ReadingParentShelfBackStack);
        Assert.Null(loaded.ReadingParentCollectionCurrent);
        Assert.Empty(loaded.ReadingParentCollectionBackStack);
        Assert.Null(loaded.ShelfCurrent);
    }

    [Fact]
    public void SavesProgressByNormalizedBookPath()
    {
        var store = new JsonAppStateStore(_tempDirectory);

        store.SaveProgress(CreateProgress(@"D:\Manga\A.cbz", 100, DateTimeOffset.UtcNow));

        var loaded = store.FindProgress(@"D:\Manga\A.cbz");

        Assert.NotNull(loaded);
        Assert.Equal(100, loaded.LastPageIndex);
    }

    [Fact]
    public void TrimsOldProgressEntries()
    {
        var store = new JsonAppStateStore(_tempDirectory, progressLimit: 2);

        store.SaveProgress(CreateProgress(@"D:\Manga\A.cbz", 1, DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
        store.SaveProgress(CreateProgress(@"D:\Manga\B.cbz", 2, DateTimeOffset.Parse("2026-05-13T11:00:00Z")));
        store.SaveProgress(CreateProgress(@"D:\Manga\C.cbz", 3, DateTimeOffset.Parse("2026-05-13T12:00:00Z")));

        var loaded = store.LoadProgress();

        Assert.Equal(2, loaded.Books.Count);
        Assert.Null(store.FindProgress(@"D:\Manga\A.cbz"));
        Assert.NotNull(store.FindProgress(@"D:\Manga\B.cbz"));
        Assert.NotNull(store.FindProgress(@"D:\Manga\C.cbz"));
    }

    [Fact]
    public void SaveReadingStateDeletesProgressAtLastPage()
    {
        var store = new JsonAppStateStore(_tempDirectory);
        var book = new BookEntry(@"D:\Manga\A.cbz", "A.cbz", BookSourceKind.Zip, @"D:\Manga\A.cbz");
        var history = new NavigationHistory();
        history.StartAt(new NavigationEntry(book.Path, book.DisplayName, book.SourceKind));
        store.SaveProgress(CreateProgress(book.Path, 50, DateTimeOffset.UtcNow));

        store.SaveReadingState(
            book,
            pageIndex: 99,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            history);

        Assert.Null(store.FindProgress(book.Path));
        Assert.Equal("A.cbz", store.LoadSession().Current?.DisplayName);
        Assert.Equal(@"D:\Manga", store.LoadSession().ReadingParentCollectionCurrent?.Path);
    }

    [Fact]
    public void SaveReadingStateCanKeepProgressAtLastPageUntilClose()
    {
        var store = new JsonAppStateStore(_tempDirectory);
        var book = new BookEntry(@"D:\Manga\A.cbz", "A.cbz", BookSourceKind.Zip, @"D:\Manga\A.cbz");
        var history = new NavigationHistory();
        history.StartAt(new NavigationEntry(book.Path, book.DisplayName, book.SourceKind));
        store.SaveProgress(CreateProgress(book.Path, 50, DateTimeOffset.UtcNow));

        store.SaveReadingState(
            book,
            pageIndex: 99,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            history,
            deleteCompletedProgress: false);

        Assert.Equal(50, store.FindProgress(book.Path)?.LastPageIndex);
        Assert.Equal(99, store.LoadSession().Current?.LastPageIndex);
        Assert.Equal(@"D:\Manga", store.LoadSession().ReadingParentCollectionCurrent?.Path);
    }

    [Fact]
    public void SaveReadingStateStoresFolderBookContainerSeparatelyFromParentCollection()
    {
        var store = new JsonAppStateStore(_tempDirectory);
        var rootPath = Path.Combine(_tempDirectory, "Manga");
        var bookPath = Path.Combine(rootPath, "Volume");
        Directory.CreateDirectory(bookPath);
        var book = new BookEntry(bookPath, "Volume", BookSourceKind.Folder, bookPath);
        var history = new NavigationHistory();
        history.StartAt(new NavigationEntry(rootPath, "Manga", BookSourceKind.Collection));
        history.NavigateTo(new NavigationEntry(bookPath, "Volume", BookSourceKind.Collection));

        store.SaveReadingState(
            book,
            pageIndex: 12,
            pageCount: 100,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            history,
            deleteCompletedProgress: false);

        var session = store.LoadSession();

        Assert.Equal(Path.GetFullPath(bookPath), session.ReadingContainerCurrent?.Path);
        Assert.Single(session.ReadingContainerBackStack);
        Assert.Equal(Path.GetFullPath(rootPath), session.ReadingContainerBackStack[0].Path);
        Assert.Equal(Path.GetFullPath(rootPath), session.ReadingParentCollectionCurrent?.Path);
        Assert.Empty(session.ReadingParentCollectionBackStack);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ProgressEntry CreateProgress(string path, int pageIndex, DateTimeOffset lastOpenedAt)
    {
        return new ProgressEntry(
            path,
            Path.GetFileName(path),
            BookSourceKind.Zip,
            pageIndex,
            200,
            ReadingDirection.RightToLeft,
            ViewMode.SinglePage,
            lastOpenedAt);
    }
}
