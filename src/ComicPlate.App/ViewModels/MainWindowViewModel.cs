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
    private readonly IFolderPickerService _folderPickerService;
    private readonly ContentOpenService _contentOpenService = new();
    private readonly ReaderImageCache _readerImageCache;
    private readonly ReadingSessionController _readingSession;
    private BookEntry? _currentBook;
    private string _headerTitle = "ComicPlate";
    private bool _isNavigationPaneVisible = true;
    private bool _isReaderVisible;
    private bool _isStartVisible = true;
    private bool _isLoading;
    private string _statusMessage = "";

    public MainWindowViewModel(
        IFolderPickerService folderPickerService,
        ImagePageLoader imagePageLoader,
        JsonAppStateStore? stateStore = null)
    {
        _folderPickerService = folderPickerService;
        _readerImageCache = new ReaderImageCache(imagePageLoader);
        _readingSession = new ReadingSessionController(stateStore ?? JsonAppStateStore.CreateDefault());
        Reader = new ReaderSurfaceViewModel(_readerImageCache);
        Reader.PropertyChanged += OnReaderPropertyChanged;
        Reader.ReadingStateChanged += OnReaderReadingStateChanged;
        ContextShelf = new ContextShelfViewModel(ActivateContentItemAsync);

        OpenContentCommand = new AsyncRelayCommand(OpenContentAsync, () => !IsLoading);
        OpenLastReadingPositionCommand = new RelayCommand(OpenLastReadingPosition, () => CanOpenLastReadingPosition);
        ShowStartCommand = new RelayCommand(ShowStart);
        ToggleNavigationPaneCommand = new RelayCommand(ToggleNavigationPane);
        BackCommand = new RelayCommand(GoBack, () => CanGoBack);
    }

    public ContextShelfViewModel ContextShelf { get; }

    public ReaderSurfaceViewModel Reader { get; }

    public ICommand OpenContentCommand { get; }

    public RelayCommand OpenLastReadingPositionCommand { get; }

    public ICommand ShowStartCommand { get; }

    public ICommand ToggleNavigationPaneCommand { get; }

    public RelayCommand BackCommand { get; }

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

    public string HeaderTitle
    {
        get => _headerTitle;
        private set => SetProperty(ref _headerTitle, value);
    }

    public bool IsStartVisible
    {
        get => _isStartVisible;
        private set => SetProperty(ref _isStartVisible, value);
    }

    public bool IsReaderVisible
    {
        get => _isReaderVisible;
        private set => SetProperty(ref _isReaderVisible, value);
    }

    public bool IsNavigationPaneVisible
    {
        get => _isNavigationPaneVisible;
        private set
        {
            if (SetProperty(ref _isNavigationPaneVisible, value))
            {
                OnPropertyChanged(nameof(NavigationPaneToggleText));
            }
        }
    }

    public string NavigationPaneToggleText => IsNavigationPaneVisible ? "Hide Panels" : "Show Panels";

    public bool CanGoBack => _readingSession.CanGoBack && !IsLoading;

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

    private async Task OpenContentAsync()
    {
        var folderPath = await _folderPickerService.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        IsLoading = true;
        HeaderTitle = Path.GetFileName(folderPath);
        ShowReader();
        SetMessage("Loading contents...");

        try
        {
            await OpenPathAsSessionStartAsync(folderPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Reader.ClearPages();
            ContextShelf.ReplaceItems(Array.Empty<BookEntry>());
            SetMessage("ComicPlate could not read this folder.");
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
    }

    private void OnReaderReadingStateChanged()
    {
        ContextShelf.SetCurrentIndexSilently(-1);
        SetMessage("");
        PersistCurrentReadingState(deleteCompletedProgress: false);
    }

    private void LoadContextShelfEntries(string rootPath, IReadOnlyList<BookEntry> books)
    {
        HeaderTitle = Path.GetFileName(rootPath);
        ContextShelf.ReplaceItems(books);
    }

    private async Task ActivateBookItemAsync(BookEntry book)
    {
        if (book.SourceKind == BookSourceKind.Collection)
        {
            await NavigateToContentFolderAsync(book.Path);
            return;
        }

        await NavigateToBookAsync(book);
    }

    private async Task ActivateContentItemAsync(ContentListItemViewModel item)
    {
        await ActivateBookItemAsync(item.Book);
    }

    private async Task LoadContentFolderAsync(string folderPath, bool updateReaderFromDirectPages)
    {
        if (updateReaderFromDirectPages)
        {
            PersistCurrentReadingState(deleteCompletedProgress: true);
        }

        var result = await _contentOpenService.OpenContentFolderAsync(folderPath, CancellationToken.None);
        LoadContextShelfEntries(result.FolderPath, result.ContextShelfEntries);

        if (updateReaderFromDirectPages)
        {
            var progress = _readingSession.FindProgress(result.DirectFolderBook.Path);
            _currentBook = result.DirectPages.Count > 0 ? result.DirectFolderBook : null;
            await Reader.LoadPagesAsync(result.DirectPages, progress?.LastPageIndex ?? 0);
        }

        if (ContextShelf.IsEmpty && result.DirectPages.Count == 0 && Reader.ReaderStripItems.Count == 0)
        {
            SetMessage("This folder has no readable contents.");
            return;
        }

        if (updateReaderFromDirectPages && result.DirectPages.Count == 0 && Reader.ReaderStripItems.Count == 0)
        {
            SetMessage("Select an item from the current folder.");
        }

        _ = ContextShelf.LoadThumbnailsAsync();
    }

    private async Task OpenContentFolderAsync(string folderPath, bool updateReaderFromDirectPages = true)
    {
        IsLoading = true;
        HeaderTitle = Path.GetFileName(folderPath);
        SetMessage("Loading contents...");

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

            ContextShelf.ReplaceItems(Array.Empty<BookEntry>());
            SetMessage("ComicPlate could not read this folder.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenBookAsync(BookEntry book, int? initialPageIndex = null)
    {
        PersistCurrentReadingState(deleteCompletedProgress: true);

        IsLoading = true;
        HeaderTitle = book.DisplayName;
        SetMessage("Loading pages...");

        try
        {
            var result = await _contentOpenService.OpenBookAsync(book, CancellationToken.None);
            var pages = result.Pages;
            var progress = initialPageIndex.HasValue ? null : _readingSession.FindProgress(book.Path);
            _currentBook = pages.Count > 0 ? result.Book : null;
            await Reader.LoadPagesAsync(pages, initialPageIndex ?? progress?.LastPageIndex ?? 0);

            if (pages.Count == 0)
            {
                SetMessage("This comic has no readable images.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _currentBook = null;
            Reader.ClearPages();
            SetMessage("ComicPlate could not read this comic.");
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
        _readingSession.StartAtBook(book);
        ContextShelf.ReplaceItems(Array.Empty<BookEntry>());
        RaiseCommandStates();
        await OpenBookAsync(book);
    }

    private async Task NavigateToContentFolderAsync(string folderPath)
    {
        _readingSession.NavigateToContentFolder(folderPath);
        RaiseCommandStates();
        await OpenContentFolderAsync(folderPath, updateReaderFromDirectPages: false);
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
        SetMessage("Loading contents...");

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
            ContextShelf.ReplaceItems(Array.Empty<BookEntry>());
            SetMessage(result.Kind == OpenPathKind.Missing
                ? "ComicPlate could not find this path."
                : "ComicPlate cannot open this file type yet.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Reader.ClearPages();
            ContextShelf.ReplaceItems(Array.Empty<BookEntry>());
            SetMessage("ComicPlate could not open this path.");
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
        HeaderTitle = "ComicPlate";
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

    private void GoBack()
    {
        var entry = _readingSession.Back();
        if (entry is null)
        {
            return;
        }

        RaiseCommandStates();
        _ = OpenNavigationEntryAsync(entry);
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
        _currentBook = null;
        RaiseCommandStates();
        await OpenNavigationEntryAsync(
            new NavigationEntry(current.Path, current.DisplayName, current.SourceKind),
            current.LastPageIndex);
    }

    private async Task OpenNavigationEntryAsync(NavigationEntry entry, int? initialPageIndex = null)
    {
        if (entry.SourceKind == BookSourceKind.Collection)
        {
            await OpenContentFolderAsync(entry.Path, updateReaderFromDirectPages: false);
            return;
        }

        await OpenBookAsync(new BookEntry(entry.Path, entry.DisplayName, entry.SourceKind, entry.Path), initialPageIndex);
    }

    private void PersistCurrentReadingState(bool deleteCompletedProgress)
    {
        _readingSession.SaveReadingState(
            _currentBook,
            Reader.HasPages,
            Reader.CurrentPageIndex,
            Reader.PageCount,
            Reader.ReadingDirection,
            Reader.ViewMode,
            deleteCompletedProgress);

        OnPropertyChanged(nameof(LastReadingPositionText));
        RaiseCommandStates();
    }

    private void SetMessage(string message)
    {
        StatusMessage = message;
    }

    private void RaiseCommandStates()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanOpenLastReadingPosition));

        if (OpenContentCommand is AsyncRelayCommand openContentCommand)
        {
            openContentCommand.RaiseCanExecuteChanged();
        }

        OpenLastReadingPositionCommand.RaiseCanExecuteChanged();
        BackCommand.RaiseCanExecuteChanged();
    }
}
