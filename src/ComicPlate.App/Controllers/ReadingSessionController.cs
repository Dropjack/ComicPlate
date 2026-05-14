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

    public bool CanGoBack => _navigationHistory.CanGoBack;

    public bool CanOpenLastReadingPosition => _lastSession.Current is not null;

    public string LastReadingPositionText => _lastSession.Current is null
        ? "Continue Reading"
        : $"Continue Reading \"{_lastSession.Current.DisplayName}\"";

    public ProgressEntry? FindProgress(string bookPath)
    {
        return _stateStore.FindProgress(bookPath);
    }

    public void StartAtContentFolder(string folderPath)
    {
        _navigationHistory.StartAt(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
    }

    public void StartAtBook(BookEntry book)
    {
        _navigationHistory.StartAt(CreateNavigationEntry(book));
    }

    public void NavigateToContentFolder(string folderPath)
    {
        _navigationHistory.NavigateTo(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
    }

    public void NavigateToBook(BookEntry book)
    {
        var entry = CreateNavigationEntry(book);
        if (_navigationHistory.Current?.SourceKind == BookSourceKind.Collection)
        {
            _navigationHistory.NavigateTo(entry);
            return;
        }

        _navigationHistory.ReplaceCurrent(entry);
    }

    public NavigationEntry? Back()
    {
        return _navigationHistory.Back();
    }

    public ReadableUnitState? PrepareOpenLastReadingPosition()
    {
        var current = _lastSession.Current;
        if (current is null)
        {
            return null;
        }

        var entry = new NavigationEntry(current.Path, current.DisplayName, current.SourceKind);
        _navigationHistory.Restore(entry, _lastSession.BackStack);
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

    private static NavigationEntry CreateNavigationEntry(BookEntry book)
    {
        return new NavigationEntry(book.Path, book.DisplayName, book.SourceKind);
    }

    private static NavigationEntry CreateNavigationEntry(string path, BookSourceKind sourceKind)
    {
        var displayName = Path.GetFileName(path);
        return new NavigationEntry(path, string.IsNullOrWhiteSpace(displayName) ? path : displayName, sourceKind);
    }
}
