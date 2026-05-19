using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.Controllers;

public sealed class ReadingSessionController
{
    private readonly JsonAppStateStore _stateStore;
    private readonly NavigationHistory _navigationHistory = new();
    private SessionState _lastSession;

    public ReadingSessionController(JsonAppStateStore stateStore)
    {
        _stateStore = stateStore;
        _lastSession = _stateStore.LoadSession();
    }

    public bool CanNavigateUp => _navigationHistory.CanNavigateUp;

    public NavigationEntry? CurrentCollection => _navigationHistory.Current?.SourceKind == BookSourceKind.Collection
        ? _navigationHistory.Current
        : null;

    public bool CanOpenLastReadingPosition => _lastSession.Current is not null;

    public string LastReadingPositionText => _lastSession.Current is null
        ? "Continue Reading"
        : $"Continue Reading \"{_lastSession.Current.DisplayName}\"";

    public ProgressEntry? FindProgress(string bookPath)
    {
        return _stateStore.FindProgress(bookPath);
    }

    public IReadOnlyList<BookEntry> GetRecentBooks(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<BookEntry>();
        }

        var books = new List<BookEntry>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_lastSession.Current is not null)
        {
            AddBook(
                new BookEntry(
                    _lastSession.Current.Path,
                    _lastSession.Current.DisplayName,
                    _lastSession.Current.SourceKind,
                    _lastSession.Current.Path));
        }

        foreach (var entry in _stateStore.GetRecentProgressEntries(limit))
        {
            AddBook(new BookEntry(entry.Path, entry.DisplayName, entry.SourceKind, entry.Path));
            if (books.Count >= limit)
            {
                break;
            }
        }

        return books;

        void AddBook(BookEntry book)
        {
            var normalizedPath = Path.GetFullPath(book.Path);
            if (seenPaths.Add(normalizedPath))
            {
                books.Add(book with { Id = normalizedPath, Path = normalizedPath });
            }
        }
    }

    public void StartAtContentFolder(string folderPath)
    {
        _navigationHistory.StartAt(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
    }

    public void StartAtBook(BookEntry book)
    {
        var parent = CreateParentCollectionEntry(book.Path);
        if (parent is not null)
        {
            _navigationHistory.StartAt(parent);
        }
    }

    public void NavigateToContentFolder(string folderPath)
    {
        _navigationHistory.NavigateTo(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
    }

    public void NavigateToBook(BookEntry book)
    {
        if (_navigationHistory.Current is not null)
        {
            return;
        }

        StartAtBook(book);
    }

    public NavigationEntry? NavigateUp()
    {
        return _navigationHistory.NavigateUp();
    }

    public ReadableUnitState? PrepareOpenLastReadingPosition()
    {
        var current = _lastSession.Current;
        if (current is null)
        {
            return null;
        }

        var parentCollection = _lastSession.ReadingParentCollectionCurrent?.SourceKind == BookSourceKind.Collection
            ? _lastSession.ReadingParentCollectionCurrent
            : _lastSession.ReadingParentShelfCurrent?.SourceKind == BookSourceKind.Collection
                ? _lastSession.ReadingParentShelfCurrent
                : _lastSession.ReadingShelfCurrent?.SourceKind == BookSourceKind.Collection
                    ? _lastSession.ReadingShelfCurrent
                    : CreateParentCollectionEntry(current.Path);
        if (parentCollection is not null)
        {
            var backStack = _lastSession.ReadingParentCollectionBackStack.Count > 0
                ? _lastSession.ReadingParentCollectionBackStack
                : _lastSession.ReadingParentShelfBackStack.Count > 0
                    ? _lastSession.ReadingParentShelfBackStack
                    : _lastSession.ReadingShelfBackStack;
            _navigationHistory.Restore(parentCollection, backStack);
        }
        return current;
    }

    public void SaveReadingState(
        BookEntry? currentBook,
        bool hasPages,
        int currentPageIndex,
        int pageCount,
        ReadingDirection readingDirection,
        ViewMode viewMode,
        bool deleteCompletedProgress)
    {
        if (currentBook is null || !hasPages)
        {
            return;
        }

        _stateStore.SaveReadingState(
            currentBook,
            currentPageIndex,
            pageCount,
            readingDirection,
            viewMode,
            _navigationHistory,
            deleteCompletedProgress);

        _lastSession = _stateStore.LoadSession();
    }

    private static NavigationEntry CreateNavigationEntry(string path, BookSourceKind sourceKind)
    {
        var displayName = Path.GetFileName(path);
        return new NavigationEntry(path, string.IsNullOrWhiteSpace(displayName) ? path : displayName, sourceKind);
    }

    private static NavigationEntry? CreateParentCollectionEntry(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var parentPath = Directory.Exists(fullPath)
            ? Directory.GetParent(fullPath)?.FullName
            : Path.GetDirectoryName(fullPath);
        return string.IsNullOrWhiteSpace(parentPath)
            ? null
            : CreateNavigationEntry(parentPath, BookSourceKind.Collection);
    }
}
