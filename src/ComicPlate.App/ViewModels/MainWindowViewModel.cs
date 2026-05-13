using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const int NeighborPageLimit = 3;
    private const double ReaderStripItemHorizontalMargin = 8;
    private const double WheelFreeMoveViewportRatio = 0.35;

    private readonly IFolderPickerService _folderPickerService;
    private readonly ImagePageLoader _imagePageLoader;
    private readonly SidebarThumbnailLoader _sidebarThumbnailLoader;
    private readonly NavigationHistory _navigationHistory = new();
    private readonly ReaderState _readerState = new();
    private readonly VirtualizedReaderStrip _readerStrip = new(NeighborPageLimit);
    private readonly Dictionary<int, Bitmap> _imageCache = new();
    private IReadOnlyList<VirtualizedReaderStripSlot> _readerStripLayoutSlots = Array.Empty<VirtualizedReaderStripSlot>();
    private CancellationTokenSource _sidebarThumbnailCancellationTokenSource = new();
    private string _currentLogicalPath = "";
    private int _currentBookIndex = -1;
    private int _currentContentIndex = -1;
    private int _currentPageIndex;
    private string _headerTitle = "ComicPlate";
    private bool _isNavigationPaneVisible = true;
    private bool _isReaderVisible;
    private bool _isStartVisible = true;
    private bool _isLoading;
    private string _pageText = "";
    private double _readerViewportHeight = 600;
    private double _readerViewportWidth = 800;
    private double _readerStripBaseOffset;
    private double _readerStripDragOffset;
    private double _readerStripTranslateX;
    private int _readerStripRefreshVersion;
    private string _statusMessage = "No recent books yet.";

    public MainWindowViewModel(IFolderPickerService folderPickerService, ImagePageLoader imagePageLoader)
    {
        _folderPickerService = folderPickerService;
        _imagePageLoader = imagePageLoader;
        _sidebarThumbnailLoader = new SidebarThumbnailLoader();

        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync, () => !IsLoading);
        ShowStartCommand = new RelayCommand(ShowStart);
        ToggleNavigationPaneCommand = new RelayCommand(ToggleNavigationPane);
        BackCommand = new RelayCommand(GoBack, () => CanGoBack);
        NextPageCommand = new RelayCommand(NextPage, () => _readerState.CanGoNext);
        PreviousPageCommand = new RelayCommand(PreviousPage, () => _readerState.CanGoPrevious);
        VisualLeftCommand = new RelayCommand(VisualLeft);
        VisualRightCommand = new RelayCommand(VisualRight);
        FirstPageCommand = new RelayCommand(FirstPage, () => _readerState.HasPages);
        LastPageCommand = new RelayCommand(LastPage, () => _readerState.HasPages);
    }

    public ObservableCollection<BookListItemViewModel> BookItems { get; } = new();

    public ObservableCollection<PageListItemViewModel> PageItems { get; } = new();

    public ObservableCollection<ContentListItemViewModel> ContentItems { get; } = new();

    public ObservableCollection<ReaderStripItemViewModel> ReaderStripItems { get; private set; } = new();

    public ICommand OpenFolderCommand { get; }

    public ICommand ShowStartCommand { get; }

    public ICommand ToggleNavigationPaneCommand { get; }

    public RelayCommand BackCommand { get; }

    public RelayCommand NextPageCommand { get; }

    public RelayCommand PreviousPageCommand { get; }

    public RelayCommand VisualLeftCommand { get; }

    public RelayCommand VisualRightCommand { get; }

    public RelayCommand FirstPageCommand { get; }

    public RelayCommand LastPageCommand { get; }

    public int CurrentBookIndex
    {
        get => _currentBookIndex;
        set
        {
            if (value == _currentBookIndex || value < 0 || value >= BookItems.Count)
            {
                return;
            }

            _currentBookIndex = value;
            OnPropertyChanged(nameof(CurrentBookIndex));
            _ = ActivateBookItemAsync(BookItems[value].Book);
        }
    }

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

    public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) && ReaderStripItems.Count == 0;

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

    public bool CanGoBack => _navigationHistory.CanGoBack && !IsLoading;

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

    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            if (value == _currentPageIndex || value < 0 || value >= _readerState.PageCount)
            {
                return;
            }

            _readerState.GoToPage(value);
            _ = RefreshReaderStripAsync();
        }
    }

    public int CurrentPageNumber => _readerState.HasPages ? _readerState.CurrentPageIndex + 1 : 0;

    public int CurrentPageProgressIndex
    {
        get
        {
            if (!_readerState.HasPages)
            {
                return 0;
            }

            return _readerState.ReadingDirection == ReadingDirection.RightToLeft
                ? LastPageProgressIndex - _readerState.CurrentPageIndex
                : _readerState.CurrentPageIndex;
        }
    }

    public int LastPageProgressIndex => _readerState.HasPages ? Math.Max(_readerState.PageCount - 1, 0) : 0;

    public int PageCount => _readerState.PageCount;

    public string PageText
    {
        get => _pageText;
        private set => SetProperty(ref _pageText, value);
    }

    public string CurrentLogicalPath
    {
        get => _currentLogicalPath;
        private set => SetProperty(ref _currentLogicalPath, value);
    }

    public double ReaderStripTranslateX
    {
        get => _readerStripTranslateX;
        private set => SetProperty(ref _readerStripTranslateX, value);
    }

    public int CurrentContentIndex
    {
        get => _currentContentIndex;
        set
        {
            if (value == _currentContentIndex || value < 0 || value >= ContentItems.Count)
            {
                return;
            }

            _currentContentIndex = value;
            OnPropertyChanged(nameof(CurrentContentIndex));
            _ = ActivateContentItemAsync(ContentItems[value]);
        }
    }

    public void SetReaderViewportSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _readerViewportWidth = width;
        _readerViewportHeight = height;

        foreach (var item in ReaderStripItems)
        {
            item.SetViewportSize(width, height);
        }

        UpdateReaderStripOffset();
    }

    public void WheelNextReadingGroup()
    {
        MoveReaderStripFreely(GetNextReadingDirectionOffsetDelta());
    }

    public void WheelPreviousReadingGroup()
    {
        MoveReaderStripFreely(-GetNextReadingDirectionOffsetDelta());
    }

    public void BeginReaderStripDrag()
    {
        _readerStripDragOffset = 0;
        UpdateReaderStripTransform();
    }

    public void DragReaderStrip(double horizontalDelta)
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        _readerStripDragOffset = horizontalDelta;
        UpdateReaderStripTransform();
    }

    public void EndReaderStripDrag(double horizontalDelta)
    {
        if (!_readerState.HasPages)
        {
            ResetReaderStripDrag();
            return;
        }

        CommitReaderStripFreeOffset(_readerStripBaseOffset + horizontalDelta);
    }

    public void CancelReaderStripDrag()
    {
        ResetReaderStripDrag();
    }

    private async Task OpenFolderAsync()
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
            await StartAtContentFolderAsync(folderPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BookItems.Clear();
            LoadPages(Array.Empty<PageEntry>(), rebuildContentItems: true);
            SetMessage("ComicPlate could not read this folder.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadBookshelf(Bookshelf bookshelf)
    {
        BookItems.Clear();

        foreach (var book in bookshelf.Books)
        {
            BookItems.Add(new BookListItemViewModel(book));
        }

        _currentBookIndex = -1;
        OnPropertyChanged(nameof(CurrentBookIndex));
        HeaderTitle = Path.GetFileName(bookshelf.RootPath);
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
        if (item.Page is not null)
        {
            CurrentPageIndex = item.Page.Index;
            return;
        }

        if (item.Book is not null)
        {
            await ActivateBookItemAsync(item.Book);
        }
    }

    private async Task LoadContentFolderAsync(string folderPath)
    {
        var bookshelfSource = new FileSystemBookshelfSource(folderPath);
        var directPageSource = new FolderBookSource(folderPath, recursive: false);

        var bookshelfTask = Task.Run(() => bookshelfSource.LoadAsync(CancellationToken.None));
        var pagesTask = Task.Run(() => directPageSource.LoadPagesAsync(CancellationToken.None));
        await Task.WhenAll(bookshelfTask, pagesTask);

        var bookshelf = await bookshelfTask;
        var pages = await pagesTask;

        LoadBookshelf(bookshelf);
        LoadPages(pages, rebuildContentItems: false);
        ReplaceContentItems(BookItems.Select(item => item.Book), PageItems);

        if (ContentItems.Count == 0)
        {
            SetMessage("This folder has no readable contents.");
            return;
        }

        if (pages.Count == 0)
        {
            SetMessage("Select an item from the current folder.");
        }

        _ = LoadSidebarThumbnailsAsync();
    }

    private async Task OpenContentFolderAsync(string folderPath)
    {
        IsLoading = true;
        HeaderTitle = Path.GetFileName(folderPath);
        SetMessage("Loading contents...");

        try
        {
            await LoadContentFolderAsync(folderPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BookItems.Clear();
            LoadPages(Array.Empty<PageEntry>(), rebuildContentItems: true);
            SetMessage("ComicPlate could not read this folder.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenBookAsync(BookEntry book)
    {
        IsLoading = true;
        HeaderTitle = book.DisplayName;
        SetMessage("Loading pages...");

        try
        {
            IBookSource source = book.SourceKind == BookSourceKind.Zip
                ? new ZipBookSource(book.Path)
                : new FolderBookSource(book.Path, recursive: false);

            var pages = await Task.Run(() => source.LoadPagesAsync(CancellationToken.None));
            LoadPages(pages, rebuildContentItems: true);

            if (pages.Count == 0)
            {
                SetMessage("This comic has no readable images.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            LoadPages(Array.Empty<PageEntry>(), rebuildContentItems: true);
            SetMessage("ComicPlate could not read this comic.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadPages(IReadOnlyList<PageEntry> pages, bool rebuildContentItems)
    {
        ClearImageCache();
        _readerState.LoadPages(pages);
        PageItems.Clear();
        ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());

        for (var index = 0; index < pages.Count; index++)
        {
            PageItems.Add(new PageListItemViewModel(index, pages[index]));
        }

        if (rebuildContentItems)
        {
            ReplaceContentItems(Array.Empty<BookEntry>(), PageItems);
            _ = LoadSidebarThumbnailsAsync();
        }

        if (pages.Count == 0)
        {
            UpdatePageStatus();
            RaiseCommandStates();
            return;
        }

        _ = RefreshReaderStripAsync();
    }

    private async Task StartAtContentFolderAsync(string folderPath)
    {
        _navigationHistory.StartAt(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
        RaiseCommandStates();
        await OpenContentFolderAsync(folderPath);
    }

    private async Task NavigateToContentFolderAsync(string folderPath)
    {
        _navigationHistory.NavigateTo(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
        RaiseCommandStates();
        await OpenContentFolderAsync(folderPath);
    }

    private async Task NavigateToBookAsync(BookEntry book)
    {
        _navigationHistory.NavigateTo(CreateNavigationEntry(book));
        RaiseCommandStates();
        await OpenBookAsync(book);
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

    private void ReplaceContentItems(IEnumerable<BookEntry> books, IEnumerable<PageListItemViewModel> pages)
    {
        _sidebarThumbnailCancellationTokenSource.Cancel();
        _sidebarThumbnailCancellationTokenSource.Dispose();
        _sidebarThumbnailCancellationTokenSource = new CancellationTokenSource();
        _sidebarThumbnailLoader.Clear();
        ContentItems.Clear();

        foreach (var book in books)
        {
            ContentItems.Add(ContentListItemViewModel.FromBook(book));
        }

        foreach (var page in pages)
        {
            ContentItems.Add(ContentListItemViewModel.FromPage(page));
        }

        SetCurrentContentIndexSilently(-1);
    }

    private async Task LoadSidebarThumbnailsAsync()
    {
        var cancellationToken = _sidebarThumbnailCancellationTokenSource.Token;

        try
        {
            await _sidebarThumbnailLoader.LoadInitialThumbnailsAsync(ContentItems.ToArray(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SetCurrentContentIndexSilently(int index)
    {
        if (_currentContentIndex == index)
        {
            return;
        }

        _currentContentIndex = index;
        OnPropertyChanged(nameof(CurrentContentIndex));
    }

    private async Task RefreshReaderStripAsync(ReaderStripPlacement? placement = null)
    {
        var refreshVersion = ++_readerStripRefreshVersion;

        if (!_readerState.HasPages)
        {
            ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
            UpdatePageStatus();
            return;
        }

        var page = _readerState.Pages[_readerState.CurrentPageIndex];
        _currentPageIndex = _readerState.CurrentPageIndex;
        OnPropertyChanged(nameof(CurrentPageIndex));
        var contentIndex = ContentItems
            .Select((item, index) => new { item, index })
            .FirstOrDefault(pair => pair.item.Page?.Index == _readerState.CurrentPageIndex)
            ?.index ?? -1;
        SetCurrentContentIndexSilently(contentIndex);

        CurrentLogicalPath = page.LogicalPath;
        SetMessage("");

        var windowPageIndexes = _readerStrip.CreateWindow(
            _readerState.PageCount,
            _readerState.CurrentPageIndex,
            _readerState.ReadingDirection);
        var activeIndexes = windowPageIndexes.ToHashSet();
        var nextItems = new ObservableCollection<ReaderStripItemViewModel>();

        foreach (var pageIndex in windowPageIndexes)
        {
            var slot = new ReaderStripSlot(
                pageIndex,
                pageIndex + 1,
                _readerState.Pages[pageIndex],
                pageIndex == _readerState.CurrentPageIndex);
            var item = new ReaderStripItemViewModel(slot);
            item.SetViewportSize(_readerViewportWidth, _readerViewportHeight);

            try
            {
                item.Image = await GetOrLoadImageAsync(slot);
            }
            catch (Exception)
            {
                item.StatusMessage = $"Could not display{Environment.NewLine}{slot.Page.DisplayName}";
            }

            nextItems.Add(item);
        }

        if (refreshVersion != _readerStripRefreshVersion)
        {
            return;
        }

        ReplaceReaderStripItems(nextItems, placement);
        TrimImageCache(activeIndexes);
        UpdatePageStatus();
        RaiseCommandStates();
    }

    private async Task<Bitmap> GetOrLoadImageAsync(ReaderStripSlot slot)
    {
        if (_imageCache.TryGetValue(slot.PageIndex, out var cachedImage))
        {
            return cachedImage;
        }

        var image = await _imagePageLoader.LoadAsync(slot.Page, CancellationToken.None);
        _imageCache[slot.PageIndex] = image;
        return image;
    }

    private void TrimImageCache(HashSet<int> activeIndexes)
    {
        var staleIndexes = _imageCache.Keys
            .Where(index => !activeIndexes.Contains(index))
            .ToArray();

        foreach (var index in staleIndexes)
        {
            _imageCache[index].Dispose();
            _imageCache.Remove(index);
        }
    }

    private void ClearImageCache()
    {
        foreach (var image in _imageCache.Values)
        {
            image.Dispose();
        }

        _imageCache.Clear();
    }

    private void NextPage()
    {
        _readerState.NextPage();
        _ = RefreshReaderStripAsync();
    }

    private void PreviousPage()
    {
        _readerState.PreviousPage();
        _ = RefreshReaderStripAsync();
    }

    private void VisualLeft()
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        if (_readerState.ReadingDirection == ReadingDirection.RightToLeft)
        {
            NextPage();
        }
        else
        {
            PreviousPage();
        }
    }

    private void VisualRight()
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        if (_readerState.ReadingDirection == ReadingDirection.RightToLeft)
        {
            PreviousPage();
        }
        else
        {
            NextPage();
        }
    }

    private void FirstPage()
    {
        _readerState.GoToFirstPage();
        _ = RefreshReaderStripAsync();
    }

    private void LastPage()
    {
        _readerState.GoToLastPage();
        _ = RefreshReaderStripAsync();
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
        var entry = _navigationHistory.Back();
        if (entry is null)
        {
            return;
        }

        RaiseCommandStates();
        _ = OpenNavigationEntryAsync(entry);
    }

    private async Task OpenNavigationEntryAsync(NavigationEntry entry)
    {
        if (entry.SourceKind == BookSourceKind.Collection)
        {
            await OpenContentFolderAsync(entry.Path);
            return;
        }

        await OpenBookAsync(new BookEntry(entry.Path, entry.DisplayName, entry.SourceKind, entry.Path));
    }

    private void SetMessage(string message)
    {
        StatusMessage = message;
    }

    private void ReplaceReaderStripItems(
        ObservableCollection<ReaderStripItemViewModel> items,
        ReaderStripPlacement? placement = null)
    {
        ReaderStripItems = items;
        OnPropertyChanged(nameof(ReaderStripItems));
        OnPropertyChanged(nameof(HasMessage));
        UpdateReaderStripOffset(placement);
    }

    private void UpdateReaderStripOffset(ReaderStripPlacement? placement = null)
    {
        if (ReaderStripItems.Count == 0)
        {
            _readerStripBaseOffset = 0;
            _readerStripLayoutSlots = Array.Empty<VirtualizedReaderStripSlot>();
            UpdateReaderStripTransform();
            return;
        }

        var windowPageIndexes = ReaderStripItems
            .Select(item => item.PageIndex)
            .ToArray();
        var pageExtents = ReaderStripItems.ToDictionary(
            item => item.PageIndex,
            item => item.DisplayWidth + (ReaderStripItemHorizontalMargin * 2));
        _readerStripLayoutSlots = _readerStrip.CreateLayout(
            windowPageIndexes,
            _readerState.CurrentPageIndex,
            pageExtents);

        if (_readerStripLayoutSlots.Count == 0)
        {
            _readerStripBaseOffset = 0;
            UpdateReaderStripTransform();
            return;
        }

        _readerStripBaseOffset = placement is null
            ? _readerStrip.GetCenteredOffset(
                _readerStripLayoutSlots,
                _readerState.CurrentPageIndex,
                _readerViewportWidth)
            : GetPreservedReaderStripOffset(placement);
        UpdateReaderStripTransform();
    }

    private double GetPreservedReaderStripOffset(ReaderStripPlacement placement)
    {
        var anchorSlot = _readerStripLayoutSlots
            .FirstOrDefault(slot => slot.PageIndex == placement.AnchorPageIndex);
        return anchorSlot is null
            ? _readerStrip.GetCenteredOffset(
                _readerStripLayoutSlots,
                _readerState.CurrentPageIndex,
                _readerViewportWidth)
            : placement.AnchorScreenX - anchorSlot.CenterX;
    }

    private void MoveReaderStripFreely(double horizontalDelta)
    {
        CommitReaderStripFreeOffset(ReaderStripTranslateX + horizontalDelta);
    }

    private void CommitReaderStripFreeOffset(double targetOffset)
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        var targetPageIndex = _readerStrip.FindNearestPageIndex(
            _readerStripLayoutSlots,
            _readerViewportWidth,
            targetOffset,
            _readerState.CurrentPageIndex);
        var anchorScreenX = GetPageScreenCenter(targetPageIndex, targetOffset);

        _readerStripBaseOffset = targetOffset;
        _readerStripDragOffset = 0;
        UpdateReaderStripTransform();

        if (targetPageIndex == _readerState.CurrentPageIndex)
        {
            return;
        }

        _readerState.GoToPage(targetPageIndex);
        _ = RefreshReaderStripAsync(new ReaderStripPlacement(targetPageIndex, anchorScreenX));
    }

    private double GetPageScreenCenter(int pageIndex, double stripOffset)
    {
        var slot = _readerStripLayoutSlots.FirstOrDefault(slot => slot.PageIndex == pageIndex);
        return slot is null
            ? _readerViewportWidth / 2
            : stripOffset + slot.CenterX;
    }

    private double GetNextReadingDirectionOffsetDelta()
    {
        var magnitude = Math.Max(120, _readerViewportWidth * WheelFreeMoveViewportRatio);
        return _readerState.ReadingDirection == ReadingDirection.RightToLeft
            ? magnitude
            : -magnitude;
    }

    private void ResetReaderStripDrag()
    {
        _readerStripDragOffset = 0;
        UpdateReaderStripTransform();
    }

    private void UpdateReaderStripTransform()
    {
        ReaderStripTranslateX = _readerStripBaseOffset + _readerStripDragOffset;
    }

    private sealed record ReaderStripPlacement(int AnchorPageIndex, double AnchorScreenX);

    private void UpdatePageStatus()
    {
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(CurrentPageProgressIndex));
        OnPropertyChanged(nameof(LastPageProgressIndex));
        OnPropertyChanged(nameof(PageCount));

        PageText = _readerState.HasPages
            ? $"{CurrentPageNumber} / {PageCount}"
            : "0 / 0";
    }

    private void RaiseCommandStates()
    {
        OnPropertyChanged(nameof(CanGoBack));

        if (OpenFolderCommand is AsyncRelayCommand openFolderCommand)
        {
            openFolderCommand.RaiseCanExecuteChanged();
        }

        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
        BackCommand.RaiseCanExecuteChanged();
        VisualLeftCommand.RaiseCanExecuteChanged();
        VisualRightCommand.RaiseCanExecuteChanged();
        FirstPageCommand.RaiseCanExecuteChanged();
        LastPageCommand.RaiseCanExecuteChanged();
    }
}
