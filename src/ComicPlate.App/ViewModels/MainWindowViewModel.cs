using System.ComponentModel;
using System.Windows.Input;
using ComicPlate.App.Controllers;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const string AppTitle = "ComicPlate";
    private const int HistoryBookLimit = 25;

    private readonly IFolderPickerService _folderPickerService;
    private readonly ContentOpenService _contentOpenService = new();
    private readonly ReaderImageCache _readerImageCache;
    private readonly ReadingSessionController _readingSession;
    private readonly SettingsService _settingsService;
    private IReadOnlyList<ShelfEntry> _collectionShelfEntries = Array.Empty<ShelfEntry>();
    private BookEntry? _currentBook;
    private string? _navigationHighlightPath;
    private string _collectionShelfRootPath = "";
    private NavigationPaneMode _navigationPaneMode = NavigationPaneMode.Shelf;
    private string _readerTitle = "";
    private string _shelfTitle = "";
    private bool _isNavigationPaneVisible = true;
    private bool _isReaderVisible;
    private bool _isStartVisible = true;
    private bool _isLoading;
    private string _statusMessage = "";

    public MainWindowViewModel(
        IFolderPickerService folderPickerService,
        ImagePageLoader imagePageLoader,
        JsonAppStateStore? stateStore = null,
        SettingsService? settingsService = null)
    {
        _folderPickerService = folderPickerService;
        _settingsService = settingsService ?? SettingsService.CreateDefault();
        var settings = _settingsService.Load();
        _readerImageCache = new ReaderImageCache(imagePageLoader);
        _readingSession = new ReadingSessionController(stateStore ?? JsonAppStateStore.CreateDefault());
        Reader = new ReaderSurfaceViewModel(
            _readerImageCache,
            settings.ReadingDirection,
            settings.ViewMode,
            settings.IsMagnifierEnabled);
        Reader.PropertyChanged += OnReaderPropertyChanged;
        Reader.ReadingStateChanged += OnReaderReadingStateChanged;
        ContextShelf = new ContextShelfViewModel(ActivateContentItemAsync);

        OpenContentCommand = new AsyncRelayCommand(OpenContentAsync, () => !IsLoading);
        OpenLastReadingPositionCommand = new RelayCommand(OpenLastReadingPosition, () => CanOpenLastReadingPosition);
        ShowStartCommand = new RelayCommand(ShowStart);
        ToggleNavigationPaneCommand = new RelayCommand(ToggleNavigationPane);
        NavigateUpCommand = new RelayCommand(NavigateUp, () => CanNavigateUp);
        ShowShelfCommand = new RelayCommand(ShowShelfPane);
        ShowHistoryCommand = new RelayCommand(ShowHistoryPane);
        LocateCurrentBookCommand = new AsyncRelayCommand(LocateCurrentBookInShelfAsync, () => CanLocateCurrentBookInShelf);
    }

    public ContextShelfViewModel ContextShelf { get; }

    public ReaderSurfaceViewModel Reader { get; }

    public ICommand OpenContentCommand { get; }

    public RelayCommand OpenLastReadingPositionCommand { get; }

    public ICommand ShowStartCommand { get; }

    public ICommand ToggleNavigationPaneCommand { get; }

    public RelayCommand NavigateUpCommand { get; }

    public ICommand ShowShelfCommand { get; }

    public ICommand ShowHistoryCommand { get; }

    public AsyncRelayCommand LocateCurrentBookCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) && Reader.ReaderStripItems.Count == 0;

    public string ReaderTitle
    {
        get => _readerTitle;
        private set
        {
            if (SetProperty(ref _readerTitle, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string WindowTitle
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ReaderTitle))
            {
                return AppTitle;
            }

            return Reader.HasPages && !string.IsNullOrWhiteSpace(Reader.PageText)
                ? $"{ReaderTitle} ({Reader.PageText}) - {AppTitle}"
                : $"{ReaderTitle} - {AppTitle}";
        }
    }

    public string ShelfTitle
    {
        get => _shelfTitle;
        private set => SetProperty(ref _shelfTitle, value);
    }

    public bool IsShelfPaneActive => _navigationPaneMode == NavigationPaneMode.Shelf;

    public bool IsHistoryPaneActive => _navigationPaneMode == NavigationPaneMode.History;

    public bool CanLocateCurrentBookInShelf => CurrentBook is not null && IsReaderVisible && !IsLoading;

    public bool IsStartVisible
    {
        get => _isStartVisible;
        private set => SetProperty(ref _isStartVisible, value);
    }

    public bool IsReaderVisible
    {
        get => _isReaderVisible;
        private set
        {
            if (SetProperty(ref _isReaderVisible, value))
            {
                OnPropertyChanged(nameof(IsReaderNavigationPaneVisible));
            }
        }
    }

    public bool IsNavigationPaneVisible
    {
        get => _isNavigationPaneVisible;
        private set
        {
            if (SetProperty(ref _isNavigationPaneVisible, value))
            {
                OnPropertyChanged(nameof(NavigationPaneToggleText));
                OnPropertyChanged(nameof(IsNavigationPaneHidden));
                OnPropertyChanged(nameof(IsReaderNavigationPaneVisible));
            }
        }
    }

    public string NavigationPaneToggleText => IsNavigationPaneVisible
        ? LocalizationService.Current.GetString("Shelf.Hide")
        : LocalizationService.Current.GetString("Shelf.Show");

    public bool IsNavigationPaneHidden => !IsNavigationPaneVisible;

    public bool IsReaderNavigationPaneVisible => IsReaderVisible && IsNavigationPaneVisible;

    public bool CanNavigateUp => IsShelfPaneActive && _readingSession.CanNavigateUp && !IsLoading;

    public bool CanOpenLastReadingPosition => _readingSession.CanOpenLastReadingPosition && !IsLoading;

    public string LastReadingPositionText => _readingSession.LastReadingPositionText;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public async Task OpenStartupPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        ShowReader();
        await OpenPathAsSessionStartAsync(path);
    }

    public bool MoveNavigationSelection(int delta, bool allowHiddenNavigationPane = false)
    {
        if (!IsReaderVisible || (!allowHiddenNavigationPane && !IsNavigationPaneVisible) || IsLoading)
        {
            return false;
        }

        return ContextShelf.MoveSelection(delta);
    }

    public void SetMagnifierEnabled(bool isEnabled)
    {
        Reader.SetMagnifierEnabled(isEnabled);
    }

    private BookEntry? CurrentBook
    {
        get => _currentBook;
        set
        {
            _currentBook = value;
            UpdateContextShelfVisualState();
            OnPropertyChanged(nameof(CanLocateCurrentBookInShelf));
            LocateCurrentBookCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task OpenContentAsync()
    {
        var folderPath = await _folderPickerService.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        IsLoading = true;
        ReaderTitle = "";
        ShelfTitle = Path.GetFileName(folderPath);
        ShowReader();
        SetMessageKey("Status.LoadingContents");

        try
        {
            await OpenPathAsSessionStartAsync(folderPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Reader.ClearPages();
            ClearCollectionShelfItems();
            SetMessageKey("Status.CannotReadFolder");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SaveCurrentState()
    {
        PersistCurrentReadingState(deleteCompletedProgress: true);
    }

    public void Dispose()
    {
        Reader.PropertyChanged -= OnReaderPropertyChanged;
        Reader.ReadingStateChanged -= OnReaderReadingStateChanged;
        Reader.Dispose();
        _readerImageCache.Dispose();
        ContextShelf.Dispose();
    }

    private void OnReaderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReaderSurfaceViewModel.ReaderStripItems))
        {
            OnPropertyChanged(nameof(HasMessage));
        }

        if (e.PropertyName is nameof(ReaderSurfaceViewModel.HasPages)
            or nameof(ReaderSurfaceViewModel.PageText))
        {
            OnPropertyChanged(nameof(WindowTitle));
        }

        if (e.PropertyName is nameof(ReaderSurfaceViewModel.ReadingDirection)
            or nameof(ReaderSurfaceViewModel.ViewMode))
        {
            PersistReaderPreferences();
        }
    }

    private void OnReaderReadingStateChanged()
    {
        SetMessage("");
        PersistCurrentReadingState(deleteCompletedProgress: false);
    }

    private void LoadCollectionShelfEntries(
        string rootPath,
        IReadOnlyList<ShelfEntry> entries,
        string? navigationHighlightPath)
    {
        _collectionShelfRootPath = rootPath;
        _collectionShelfEntries = entries;
        _navigationHighlightPath = navigationHighlightPath;
        if (IsShelfPaneActive)
        {
            RenderCollectionShelfPane();
        }
    }

    private async Task ActivateShelfEntryAsync(ShelfEntry entry)
    {
        if (entry.Kind == ShelfEntryKind.Collection)
        {
            await NavigateToContentFolderAsync(entry.Path);
            return;
        }

        await NavigateToBookAsync(entry.ToBookEntry());
    }

    private async Task ActivateContentItemAsync(ContentListItemViewModel item)
    {
        if (IsHistoryPaneActive)
        {
            await ActivateHistoryBookAsync(item.Entry.ToBookEntry());
            return;
        }

        await ActivateShelfEntryAsync(item.Entry);
    }

    private async Task ActivateHistoryBookAsync(BookEntry book)
    {
        ShowReader();
        PersistCurrentReadingState(deleteCompletedProgress: true);
        _readingSession.StartAtBook(book);
        RaiseCommandStates();
        await OpenBookAsync(book, persistBeforeOpen: false);
        await OpenCurrentCollectionShelfAsync();
    }

    private async Task LoadContentFolderAsync(
        string folderPath,
        bool updateReaderFromDirectPages,
        string? navigationHighlightPath = null)
    {
        if (updateReaderFromDirectPages)
        {
            PersistCurrentReadingState(deleteCompletedProgress: true);
        }

        var result = await _contentOpenService.OpenContentFolderAsync(folderPath, CancellationToken.None);
        LoadCollectionShelfEntries(result.FolderPath, result.ContextShelfEntries, navigationHighlightPath);

        if (updateReaderFromDirectPages)
        {
            var progress = _readingSession.FindProgress(result.DirectFolderBook.Path);
            CurrentBook = result.DirectPages.Count > 0 ? result.DirectFolderBook : null;
            ReaderTitle = CurrentBook?.DisplayName ?? "";
            await Reader.LoadPagesAsync(result.DirectPages, progress?.LastPageIndex ?? 0);
        }

        if (ContextShelf.IsEmpty && result.DirectPages.Count == 0 && Reader.ReaderStripItems.Count == 0)
        {
            SetMessageKey("Status.FolderNoReadableContents");
            return;
        }

        if (updateReaderFromDirectPages && result.DirectPages.Count == 0 && Reader.ReaderStripItems.Count == 0)
        {
            SetMessageKey("Status.SelectItemFromCurrentFolder");
        }

        _ = ContextShelf.LoadThumbnailsAsync();
    }

    private async Task OpenContentFolderAsync(string folderPath, bool updateReaderFromDirectPages = true)
    {
        IsLoading = true;
        if (updateReaderFromDirectPages)
        {
            ReaderTitle = "";
        }

        ShelfTitle = Path.GetFileName(folderPath);
        SetMessageKey("Status.LoadingContents");

        try
        {
            await LoadContentFolderAsync(folderPath, updateReaderFromDirectPages);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (updateReaderFromDirectPages)
            {
                Reader.ClearPages();
            }

            ClearCollectionShelfItems();
            SetMessageKey("Status.CannotReadFolder");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenBookAsync(
        BookEntry book,
        int? initialPageIndex = null,
        bool persistBeforeOpen = true)
    {
        if (persistBeforeOpen)
        {
            PersistCurrentReadingState(deleteCompletedProgress: true);
        }

        IsLoading = true;
        ReaderTitle = book.DisplayName;
        SetMessageKey("Status.LoadingPages");

        try
        {
            var result = await _contentOpenService.OpenBookAsync(book, CancellationToken.None);
            var pages = result.Pages;
            var progress = initialPageIndex.HasValue ? null : _readingSession.FindProgress(book.Path);
            CurrentBook = pages.Count > 0 ? result.Book : null;
            await Reader.LoadPagesAsync(pages, initialPageIndex ?? progress?.LastPageIndex ?? 0);

            if (pages.Count == 0)
            {
                SetMessageKey("Status.ComicNoReadableImages");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            CurrentBook = null;
            Reader.ClearPages();
            ReaderTitle = "";
            SetMessageKey("Status.CannotReadComic");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartAtContentFolderAsync(string folderPath)
    {
        _readingSession.StartAtContentFolder(folderPath);
        RaiseCommandStates();
        await OpenContentFolderAsync(folderPath, updateReaderFromDirectPages: true);
    }

    private async Task StartAtBookAsync(BookEntry book)
    {
        PersistCurrentReadingState(deleteCompletedProgress: true);
        _readingSession.StartAtBook(book);
        RaiseCommandStates();
        await OpenBookAsync(book, persistBeforeOpen: false);
        await OpenCurrentCollectionShelfAsync();
    }

    private async Task NavigateToContentFolderAsync(string folderPath)
    {
        _readingSession.NavigateToContentFolder(folderPath);
        RaiseCommandStates();
        await OpenCollectionShelfAsync(folderPath);
    }

    private async Task NavigateToBookAsync(BookEntry book)
    {
        _readingSession.NavigateToBook(book);
        RaiseCommandStates();
        await OpenBookAsync(book);
    }

    private async Task OpenPathAsSessionStartAsync(string path)
    {
        IsLoading = true;
        SetMessageKey("Status.LoadingContents");

        try
        {
            var result = _contentOpenService.ClassifyPath(path);
            if (result.Kind == OpenPathKind.ContentFolder)
            {
                await StartAtContentFolderAsync(result.Path);
                return;
            }

            if (result.Kind == OpenPathKind.Book && result.Book is not null)
            {
                await StartAtBookAsync(result.Book);
                return;
            }

            Reader.ClearPages();
            ClearCollectionShelfItems();
            SetMessageKey(result.Kind == OpenPathKind.Missing
                ? "Status.PathMissing"
                : "Status.FileTypeUnsupported");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Reader.ClearPages();
            ClearCollectionShelfItems();
            SetMessageKey("Status.CannotOpenPath");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowStart()
    {
        IsStartVisible = true;
        IsReaderVisible = false;
        ReaderTitle = "";
    }

    private void ShowReader()
    {
        IsStartVisible = false;
        IsReaderVisible = true;
    }

    private void ToggleNavigationPane()
    {
        IsNavigationPaneVisible = !IsNavigationPaneVisible;
    }

    private void NavigateUp()
    {
        var navigationHighlightPath = _readingSession.CurrentCollection?.Path;
        var entry = _readingSession.NavigateUp();
        if (entry is null)
        {
            return;
        }

        RaiseCommandStates();
        SetNavigationPaneMode(NavigationPaneMode.Shelf);
        _ = OpenCollectionShelfAsync(entry.Path, navigationHighlightPath);
    }

    private void OpenLastReadingPosition()
    {
        _ = OpenLastReadingPositionAsync();
    }

    private async Task OpenLastReadingPositionAsync()
    {
        var current = _readingSession.PrepareOpenLastReadingPosition();
        if (current is null)
        {
            return;
        }

        ShowReader();
        PersistCurrentReadingState(deleteCompletedProgress: true);
        CurrentBook = null;
        RaiseCommandStates();
        await OpenBookAsync(
            new BookEntry(current.Path, current.DisplayName, current.SourceKind, current.Path),
            current.LastPageIndex);
        await OpenCurrentCollectionShelfAsync();
    }

    private void PersistCurrentReadingState(bool deleteCompletedProgress)
    {
        _readingSession.SaveReadingState(
            CurrentBook,
            Reader.HasPages,
            Reader.CurrentPageIndex,
            Reader.PageCount,
            Reader.ReadingDirection,
            Reader.ViewMode,
            deleteCompletedProgress);

        OnPropertyChanged(nameof(LastReadingPositionText));
        RaiseCommandStates();
    }

    private void PersistReaderPreferences()
    {
        var settings = _settingsService.Load();
        _settingsService.Save(settings with
        {
            ReadingDirection = Reader.ReadingDirection,
            ViewMode = Reader.ViewMode,
        });
    }

    private void SetMessage(string message)
    {
        StatusMessage = message;
    }

    private void SetMessageKey(string key)
    {
        SetMessage(LocalizationService.Current.GetString(key));
    }

    private async Task OpenCurrentCollectionShelfAsync()
    {
        var collection = _readingSession.CurrentCollection;
        if (collection is null)
        {
            ClearCollectionShelfItems();
            return;
        }

        await OpenCollectionShelfAsync(collection.Path);
    }

    private async Task OpenCollectionShelfAsync(string folderPath, string? navigationHighlightPath = null)
    {
        IsLoading = true;
        ShelfTitle = Path.GetFileName(folderPath);

        try
        {
            await LoadContentFolderAsync(
                folderPath,
                updateReaderFromDirectPages: false,
                navigationHighlightPath);
            if (Reader.HasPages)
            {
                SetMessage("");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ClearCollectionShelfItems();
            if (!Reader.HasPages)
            {
                SetMessageKey("Status.CannotReadFolder");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearCollectionShelfItems()
    {
        _collectionShelfRootPath = "";
        _collectionShelfEntries = Array.Empty<ShelfEntry>();
        _navigationHighlightPath = null;
        if (IsShelfPaneActive)
        {
            ContextShelf.ReplaceItems(Array.Empty<ShelfEntry>());
            UpdateContextShelfVisualState();
        }
    }

    private void UpdateContextShelfVisualState()
    {
        ContextShelf.SetVisualState(CurrentBook?.Id, _navigationHighlightPath);
    }

    private async Task LocateCurrentBookInShelfAsync()
    {
        if (CurrentBook is null)
        {
            return;
        }

        SetNavigationPaneMode(NavigationPaneMode.Shelf);
        var collection = _readingSession.CurrentCollection;
        if (collection is not null && !CollectionShelfContains(CurrentBook))
        {
            await OpenCollectionShelfAsync(collection.Path);
        }
        else
        {
            RenderCollectionShelfPane();
        }

        ContextShelf.LocateBook(CurrentBook.Id);
    }

    private void ShowShelfPane()
    {
        SetNavigationPaneMode(NavigationPaneMode.Shelf);
        RenderCollectionShelfPane();
    }

    private void ShowHistoryPane()
    {
        SetNavigationPaneMode(NavigationPaneMode.History);
        RenderHistoryPane();
    }

    private void RenderCollectionShelfPane()
    {
        ShelfTitle = string.IsNullOrWhiteSpace(_collectionShelfRootPath)
            ? LocalizationService.Current.GetString("Shelf.Title")
            : Path.GetFileName(_collectionShelfRootPath);
        ContextShelf.ReplaceItems(_collectionShelfEntries);
        UpdateContextShelfVisualState();
        _ = ContextShelf.LoadThumbnailsAsync();
    }

    private void RenderHistoryPane()
    {
        ShelfTitle = LocalizationService.Current.GetString("Shelf.History");
        ContextShelf.ReplaceItems(_readingSession.GetRecentBooks(HistoryBookLimit).Select(ShelfEntry.FromBook));
        UpdateContextShelfVisualState();
        _ = ContextShelf.LoadThumbnailsAsync();
    }

    private void SetNavigationPaneMode(NavigationPaneMode mode)
    {
        if (_navigationPaneMode == mode)
        {
            return;
        }

        _navigationPaneMode = mode;
        OnPropertyChanged(nameof(IsShelfPaneActive));
        OnPropertyChanged(nameof(IsHistoryPaneActive));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        OnPropertyChanged(nameof(CanNavigateUp));
        OnPropertyChanged(nameof(CanOpenLastReadingPosition));

        if (OpenContentCommand is AsyncRelayCommand openContentCommand)
        {
            openContentCommand.RaiseCanExecuteChanged();
        }

        OpenLastReadingPositionCommand.RaiseCanExecuteChanged();
        NavigateUpCommand.RaiseCanExecuteChanged();
        LocateCurrentBookCommand.RaiseCanExecuteChanged();
    }

    private bool CollectionShelfContains(BookEntry book)
    {
        return _collectionShelfEntries.Any(entry =>
            entry.Kind == ShelfEntryKind.Book
            && PathsEqual(entry.Id, book.Id));
    }

    private static bool PathsEqual(string first, string second)
    {
        return string.Equals(
            Path.GetFullPath(first),
            Path.GetFullPath(second),
            StringComparison.OrdinalIgnoreCase);
    }

    private enum NavigationPaneMode
    {
        Shelf,
        History
    }
}
