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
            new[] { new NavigationEntry(@"D:\Manga", "Manga", BookSourceKind.Collection) },
            DateTimeOffset.Parse("2026-05-13T10:30:00Z"));

        store.SaveSession(session);
        var loaded = store.LoadSession();

        Assert.Equal("A.cbz", loaded.Current?.DisplayName);
        Assert.Equal(100, loaded.Current?.LastPageIndex);
        Assert.Single(loaded.BackStack);
        Assert.Equal("Manga", loaded.BackStack[0].DisplayName);
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
