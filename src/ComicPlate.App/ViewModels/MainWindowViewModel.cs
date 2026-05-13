using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.FileSystem;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const int NeighborPageLimit = 12;
    private const double ReaderStripItemHorizontalMargin = 8;
    private const double ReaderFrameHorizontalPadding = 28;
    private const double ReaderFrameVerticalPadding = 24;
    private const double WheelFreeMoveViewportRatio = 0.35;

    private readonly IFolderPickerService _folderPickerService;
    private readonly ImagePageLoader _imagePageLoader;
    private readonly PageImageInfoLoader _pageImageInfoLoader = new();
    private readonly SidebarThumbnailLoader _sidebarThumbnailLoader;
    private readonly JsonAppStateStore _stateStore;
    private readonly NavigationHistory _navigationHistory = new();
    private readonly ReaderState _readerState = new();
    private readonly ReaderFrameBuilder _readerFrameBuilder = new();
    private readonly VirtualizedReaderStrip _readerStrip = new(NeighborPageLimit);
    private readonly Dictionary<int, Bitmap> _imageCache = new();
    private IReadOnlyList<PageImageInfo> _pageImageInfos = Array.Empty<PageImageInfo>();
    private IReadOnlyList<ReaderFrame> _readerFrames = Array.Empty<ReaderFrame>();
    private IReadOnlyList<VirtualizedReaderStripSlot> _readerStripLayoutSlots = Array.Empty<VirtualizedReaderStripSlot>();
    private CancellationTokenSource _sidebarThumbnailCancellationTokenSource = new();
    private SessionState _lastSession;
    private BookEntry? _currentBook;
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
    private int _pageInfoLoadVersion;
    private int _readerStripRefreshVersion;
    private string _statusMessage = "";

    public MainWindowViewModel(
        IFolderPickerService folderPickerService,
        ImagePageLoader imagePageLoader,
        JsonAppStateStore? stateStore = null)
    {
        _folderPickerService = folderPickerService;
        _imagePageLoader = imagePageLoader;
        _stateStore = stateStore ?? JsonAppStateStore.CreateDefault();
        _lastSession = _stateStore.LoadSession();
        _sidebarThumbnailLoader = new SidebarThumbnailLoader();

        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync, () => !IsLoading);
        OpenLastReadingPositionCommand = new RelayCommand(OpenLastReadingPosition, () => CanOpenLastReadingPosition);
        ShowStartCommand = new RelayCommand(ShowStart);
        ToggleNavigationPaneCommand = new RelayCommand(ToggleNavigationPane);
        BackCommand = new RelayCommand(GoBack, () => CanGoBack);
        NextPageCommand = new RelayCommand(NextPage, CanGoNextFrame);
        PreviousPageCommand = new RelayCommand(PreviousPage, CanGoPreviousFrame);
        VisualLeftCommand = new RelayCommand(VisualLeft);
        VisualRightCommand = new RelayCommand(VisualRight);
        FirstPageCommand = new RelayCommand(FirstPage, () => _readerState.HasPages);
        LastPageCommand = new RelayCommand(LastPage, () => _readerState.HasPages);
        ToggleViewModeCommand = new RelayCommand(ToggleViewMode);
    }

    public ObservableCollection<BookListItemViewModel> BookItems { get; } = new();

    public ObservableCollection<PageListItemViewModel> PageItems { get; } = new();

    public ObservableCollection<ContentListItemViewModel> ContentItems { get; } = new();

    public ObservableCollection<ReaderStripItemViewModel> ReaderStripItems { get; private set; } = new();

    public ICommand OpenFolderCommand { get; }

    public RelayCommand OpenLastReadingPositionCommand { get; }

    public ICommand ShowStartCommand { get; }

    public ICommand ToggleNavigationPaneCommand { get; }

    public RelayCommand BackCommand { get; }

    public RelayCommand NextPageCommand { get; }

    public RelayCommand PreviousPageCommand { get; }

    public RelayCommand VisualLeftCommand { get; }

    public RelayCommand VisualRightCommand { get; }

    public RelayCommand FirstPageCommand { get; }

    public RelayCommand LastPageCommand { get; }

    public RelayCommand ToggleViewModeCommand { get; }

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

    public bool CanOpenLastReadingPosition => _lastSession.Current is not null && !IsLoading;

    public string LastReadingPositionText => _lastSession.Current is null
        ? "Continue Reading"
        : $"Continue Reading \"{_lastSession.Current.DisplayName}\"";

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

    public string ViewModeText => _readerState.ViewMode == ViewMode.DoublePage ? "Double" : "Single";

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

    public async Task OpenStartupPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        ShowReader();
        await OpenPathAsSessionStartAsync(path);
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
            await OpenPathAsSessionStartAsync(folderPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BookItems.Clear();
            ClearPages();
            ReplaceContentItems(Array.Empty<BookEntry>());
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
        await ActivateBookItemAsync(item.Book);
    }

    private async Task LoadContentFolderAsync(string folderPath, bool updateReaderFromDirectPages)
    {
        if (updateReaderFromDirectPages)
        {
            PersistCurrentReadingState(deleteCompletedProgress: true);
        }

        var bookshelfSource = new FileSystemBookshelfSource(folderPath);
        var directPageSource = new FolderBookSource(folderPath, recursive: false);

        var bookshelfTask = Task.Run(() => bookshelfSource.LoadAsync(CancellationToken.None));
        var pagesTask = Task.Run(() => directPageSource.LoadPagesAsync(CancellationToken.None));
        await Task.WhenAll(bookshelfTask, pagesTask);

        var bookshelf = await bookshelfTask;
        var pages = await pagesTask;

        LoadBookshelf(bookshelf);
        ReplaceContentItems(BookItems.Select(item => item.Book));

        if (updateReaderFromDirectPages)
        {
            var folderBook = CreateBookEntry(folderPath, BookSourceKind.Folder);
            var progress = _stateStore.FindProgress(folderBook.Path);
            _currentBook = pages.Count > 0 ? folderBook : null;
            await LoadPagesAsync(pages, progress?.LastPageIndex ?? 0);
        }

        if (ContentItems.Count == 0 && pages.Count == 0 && ReaderStripItems.Count == 0)
        {
            SetMessage("This folder has no readable contents.");
            return;
        }

        if (updateReaderFromDirectPages && pages.Count == 0 && ReaderStripItems.Count == 0)
        {
            SetMessage("Select an item from the current folder.");
        }

        _ = LoadSidebarThumbnailsAsync();
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
            BookItems.Clear();
            if (updateReaderFromDirectPages)
            {
                ClearPages();
            }

            ReplaceContentItems(Array.Empty<BookEntry>());
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
            IBookSource source = book.SourceKind switch
            {
                BookSourceKind.Image => new SingleImageBookSource(book.Path),
                BookSourceKind.Zip => new ZipBookSource(book.Path),
                _ => new FolderBookSource(book.Path, recursive: false)
            };

            var pages = await Task.Run(() => source.LoadPagesAsync(CancellationToken.None));
            var progress = initialPageIndex.HasValue ? null : _stateStore.FindProgress(book.Path);
            _currentBook = pages.Count > 0 ? NormalizeBookEntry(book) : null;
            await LoadPagesAsync(pages, initialPageIndex ?? progress?.LastPageIndex ?? 0);

            if (pages.Count == 0)
            {
                SetMessage("This comic has no readable images.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _currentBook = null;
            ClearPages();
            SetMessage("ComicPlate could not read this comic.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPagesAsync(IReadOnlyList<PageEntry> pages, int initialPageIndex = 0)
    {
        ClearImageCache();
        var pageInfoLoadVersion = ++_pageInfoLoadVersion;
        _pageImageInfos = CreateUnknownPageImageInfos(pages.Count);
        _readerState.LoadPages(pages, initialPageIndex);
        PageItems.Clear();
        ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());

        for (var index = 0; index < pages.Count; index++)
        {
            PageItems.Add(new PageListItemViewModel(index, pages[index]));
        }

        if (pages.Count == 0)
        {
            UpdatePageStatus();
            RaiseCommandStates();
            return;
        }

        await RefreshReaderStripAsync();
        _ = LoadPageImageInfosInBackgroundAsync(pages, pageInfoLoadVersion);
    }

    private void ClearPages()
    {
        ClearImageCache();
        _pageInfoLoadVersion++;
        _pageImageInfos = Array.Empty<PageImageInfo>();
        _readerFrames = Array.Empty<ReaderFrame>();
        _readerState.LoadPages(Array.Empty<PageEntry>());
        PageItems.Clear();
        ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
        UpdatePageStatus();
        RaiseCommandStates();
    }

    private static IReadOnlyList<PageImageInfo> CreateUnknownPageImageInfos(int pageCount)
    {
        var infos = new PageImageInfo[pageCount];
        Array.Fill(infos, PageImageInfo.Unknown);
        return infos;
    }

    private async Task LoadPageImageInfosInBackgroundAsync(
        IReadOnlyList<PageEntry> pages,
        int pageInfoLoadVersion)
    {
        var infos = await Task.Run(() => _pageImageInfoLoader.LoadAsync(pages, CancellationToken.None));
        if (pageInfoLoadVersion != _pageInfoLoadVersion || pages != _readerState.Pages)
        {
            return;
        }

        _pageImageInfos = infos;
        await RefreshReaderStripAsync(new ReaderStripPlacement(
            _readerState.CurrentPageIndex,
            GetPageScreenCenter(_readerState.CurrentPageIndex, ReaderStripTranslateX)));
    }

    private async Task StartAtContentFolderAsync(string folderPath)
    {
        _navigationHistory.StartAt(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
        RaiseCommandStates();
        await OpenContentFolderAsync(folderPath, updateReaderFromDirectPages: true);
    }

    private async Task StartAtBookAsync(BookEntry book)
    {
        _navigationHistory.StartAt(CreateNavigationEntry(book));
        ReplaceContentItems(Array.Empty<BookEntry>());
        RaiseCommandStates();
        await OpenBookAsync(book);
    }

    private async Task NavigateToContentFolderAsync(string folderPath)
    {
        _navigationHistory.NavigateTo(CreateNavigationEntry(folderPath, BookSourceKind.Collection));
        RaiseCommandStates();
        await OpenContentFolderAsync(folderPath, updateReaderFromDirectPages: false);
    }

    private async Task NavigateToBookAsync(BookEntry book)
    {
        var entry = CreateNavigationEntry(book);
        if (_navigationHistory.Current?.SourceKind == BookSourceKind.Collection)
        {
            _navigationHistory.NavigateTo(entry);
        }
        else
        {
            _navigationHistory.ReplaceCurrent(entry);
        }

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

    private async Task OpenPathAsSessionStartAsync(string path)
    {
        IsLoading = true;
        SetMessage("Loading contents...");

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                await StartAtContentFolderAsync(fullPath);
                return;
            }

            if (!File.Exists(fullPath))
            {
                ClearPages();
                ReplaceContentItems(Array.Empty<BookEntry>());
                SetMessage("ComicPlate could not find this path.");
                return;
            }

            if (IsSupportedArchivePath(fullPath))
            {
                await StartAtBookAsync(CreateBookEntry(fullPath, BookSourceKind.Zip));
                return;
            }

            if (SupportedPageFormats.IsSupportedExtension(Path.GetExtension(fullPath)))
            {
                await StartAtBookAsync(CreateBookEntry(fullPath, BookSourceKind.Image));
                return;
            }

            ClearPages();
            ReplaceContentItems(Array.Empty<BookEntry>());
            SetMessage("ComicPlate cannot open this file type yet.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ClearPages();
            ReplaceContentItems(Array.Empty<BookEntry>());
            SetMessage("ComicPlate could not open this path.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static BookEntry CreateBookEntry(string path, BookSourceKind sourceKind)
    {
        var fullPath = Path.GetFullPath(path);
        return new BookEntry(fullPath, Path.GetFileName(fullPath), sourceKind, fullPath);
    }

    private static BookEntry NormalizeBookEntry(BookEntry book)
    {
        var fullPath = Path.GetFullPath(book.Path);
        return book with { Id = fullPath, Path = fullPath };
    }

    private static bool IsSupportedArchivePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase);
    }

    private void ReplaceContentItems(IEnumerable<BookEntry> books)
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
        SetCurrentContentIndexSilently(-1);

        CurrentLogicalPath = page.LogicalPath;
        SetMessage("");

        _readerFrames = _readerFrameBuilder.Build(
            _readerState.Pages,
            _pageImageInfos,
            _readerState.CurrentPageIndex,
            _readerState.ViewMode,
            _readerState.ReadingDirection);
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        if (currentFrame is null)
        {
            ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
            UpdatePageStatus();
            return;
        }

        var currentGroupPageIndexes = currentFrame.PageIndexes;
        var currentGroupPageSet = currentGroupPageIndexes.ToHashSet();
        var windowFrames = CreateFrameWindow(currentFrame.FrameIndex);
        var activeIndexes = windowFrames
            .SelectMany(frame => frame.PageIndexes)
            .ToHashSet();
        var nextItems = new ObservableCollection<ReaderStripItemViewModel>();

        foreach (var frame in windowFrames)
        {
            var displaySizes = CalculateFrameDisplaySizes(frame);
            for (var framePageIndex = 0; framePageIndex < frame.Pages.Count; framePageIndex++)
            {
                var framePage = frame.Pages[framePageIndex];
                var slot = new ReaderStripSlot(
                    framePage.PageIndex,
                    framePage.DisplayIndex,
                    framePage.Page,
                    currentGroupPageSet.Contains(framePage.PageIndex));
                var item = new ReaderStripItemViewModel(slot);
                item.SetViewportSize(_readerViewportWidth, _readerViewportHeight);
                item.SetDisplaySize(displaySizes[framePageIndex].Width, displaySizes[framePageIndex].Height);

                try
                {
                    item.Image = await GetOrLoadImageAsync(item);
                }
                catch (Exception)
                {
                    item.StatusMessage = $"Could not display{Environment.NewLine}{slot.Page.DisplayName}";
                }

                nextItems.Add(item);
            }
        }

        if (refreshVersion != _readerStripRefreshVersion)
        {
            return;
        }

        ReplaceReaderStripItems(nextItems, placement);
        TrimImageCache(activeIndexes);
        UpdatePageStatus();
        PersistCurrentReadingState(deleteCompletedProgress: false);
        RaiseCommandStates();
    }

    private IReadOnlyList<ReaderFrame> CreateFrameWindow(int currentFrameIndex)
    {
        var indexes = _readerStrip.CreateWindow(
            _readerFrames.Count,
            currentFrameIndex,
            _readerState.ReadingDirection);
        return indexes
            .Select(index => _readerFrames[index])
            .ToArray();
    }

    private IReadOnlyList<PageDisplaySize> CalculateFrameDisplaySizes(ReaderFrame frame)
    {
        if (frame.Pages.Count == 0)
        {
            return Array.Empty<PageDisplaySize>();
        }

        var rawSizes = frame.Pages
            .Select(page => GetRawPageSize(page.ImageInfo))
            .ToArray();
        var availableWidth = Math.Max(160, _readerViewportWidth - ReaderFrameHorizontalPadding);
        var availableHeight = Math.Max(160, _readerViewportHeight - ReaderFrameVerticalPadding);
        var targetHeight = availableHeight;
        var totalWidthAtTargetHeight = rawSizes.Sum(size => size.Width * (targetHeight / size.Height));
        if (totalWidthAtTargetHeight > availableWidth)
        {
            targetHeight *= availableWidth / totalWidthAtTargetHeight;
        }

        return rawSizes
            .Select(size => new PageDisplaySize(size.Width * (targetHeight / size.Height), targetHeight))
            .ToArray();
    }

    private static PageDisplaySize GetRawPageSize(PageImageInfo imageInfo)
    {
        return imageInfo.IsValid
            ? new PageDisplaySize(imageInfo.PixelWidth, imageInfo.PixelHeight)
            : new PageDisplaySize(720, 1080);
    }

    private async Task<Bitmap> GetOrLoadImageAsync(ReaderStripItemViewModel item)
    {
        if (_imageCache.TryGetValue(item.PageIndex, out var cachedImage))
        {
            return cachedImage;
        }

        var image = await _imagePageLoader.LoadAsync(
            item.Slot.Page,
            item.DecodePixelWidth,
            item.DecodePixelHeight,
            CancellationToken.None);
        _imageCache[item.PageIndex] = image;
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
        MoveFrame(1);
    }

    private void PreviousPage()
    {
        MoveFrame(-1);
    }

    private bool CanGoNextFrame()
    {
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        return currentFrame is not null && currentFrame.FrameIndex < _readerFrames.Count - 1;
    }

    private bool CanGoPreviousFrame()
    {
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        return currentFrame is not null && currentFrame.FrameIndex > 0;
    }

    private void MoveFrame(int delta)
    {
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        if (currentFrame is null)
        {
            return;
        }

        var nextFrameIndex = currentFrame.FrameIndex + delta;
        if (nextFrameIndex < 0 || nextFrameIndex >= _readerFrames.Count)
        {
            return;
        }

        _readerState.GoToFrameStartPage(_readerFrames[nextFrameIndex].PageIndexes.Min());
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
        var lastFrame = _readerFrames.LastOrDefault();
        if (lastFrame is null)
        {
            _readerState.GoToLastPage();
        }
        else
        {
            _readerState.GoToFrameStartPage(lastFrame.PageIndexes.Min());
        }

        _ = RefreshReaderStripAsync();
    }

    private void ToggleViewMode()
    {
        var nextViewMode = _readerState.ViewMode == ViewMode.SinglePage
            ? ViewMode.DoublePage
            : ViewMode.SinglePage;
        _readerState.SetViewMode(nextViewMode);
        OnPropertyChanged(nameof(ViewModeText));
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

    private void OpenLastReadingPosition()
    {
        _ = OpenLastReadingPositionAsync();
    }

    private async Task OpenLastReadingPositionAsync()
    {
        var current = _lastSession.Current;
        if (current is null)
        {
            return;
        }

        ShowReader();
        PersistCurrentReadingState(deleteCompletedProgress: true);
        _currentBook = null;
        var entry = new NavigationEntry(current.Path, current.DisplayName, current.SourceKind);
        _navigationHistory.Restore(entry, _lastSession.BackStack);
        RaiseCommandStates();
        await OpenNavigationEntryAsync(entry, current.LastPageIndex);
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
        if (_currentBook is null || !_readerState.HasPages)
        {
            return;
        }

        _stateStore.SaveReadingState(
            _currentBook,
            _readerState.CurrentPageIndex,
            _readerState.PageCount,
            _readerState.ReadingDirection,
            _readerState.ViewMode,
            _navigationHistory,
            deleteCompletedProgress);

        _lastSession = _stateStore.LoadSession();
        OnPropertyChanged(nameof(LastReadingPositionText));
        RaiseCommandStates();
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
        var currentGroupPageIndexes = _readerFrames
            .FirstOrDefault(frame => frame.IsCurrent)
            ?.PageIndexes ?? _readerState.CurrentReadingGroupPageIndexes;
        _readerStripLayoutSlots = _readerStrip.CreateLayout(
            windowPageIndexes,
            currentGroupPageIndexes,
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
                currentGroupPageIndexes,
                _readerViewportWidth)
            : GetPreservedReaderStripOffset(placement);
        _readerStripBaseOffset = ClampReaderStripOffset(_readerStripBaseOffset);
        UpdateReaderStripTransform();
    }

    private double GetPreservedReaderStripOffset(ReaderStripPlacement placement)
    {
        var anchorSlot = _readerStripLayoutSlots
            .FirstOrDefault(slot => slot.PageIndex == placement.AnchorPageIndex);
        return anchorSlot is null
            ? _readerStrip.GetCenteredOffset(
                _readerStripLayoutSlots,
                _readerFrames.FirstOrDefault(frame => frame.IsCurrent)?.PageIndexes ?? _readerState.CurrentReadingGroupPageIndexes,
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
        _readerStripBaseOffset = ClampReaderStripOffset(_readerStripBaseOffset);
        _readerStripDragOffset = 0;
        UpdateReaderStripTransform();

        var targetFrame = _readerFrames.FirstOrDefault(frame => frame.PageIndexes.Contains(targetPageIndex));
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        if (targetFrame is null || targetFrame == currentFrame)
        {
            return;
        }

        _readerState.GoToFrameStartPage(targetFrame.PageIndexes.Min());
        _ = RefreshReaderStripAsync(new ReaderStripPlacement(targetPageIndex, anchorScreenX));
    }

    private double GetPageScreenCenter(int pageIndex, double stripOffset)
    {
        var slot = _readerStripLayoutSlots.FirstOrDefault(slot => slot.PageIndex == pageIndex);
        return slot is null
            ? _readerViewportWidth / 2
            : stripOffset + slot.CenterX;
    }

    private double ClampReaderStripOffset(double offset)
    {
        if (_readerStripLayoutSlots.Count == 0)
        {
            return 0;
        }

        var contentWidth = _readerStripLayoutSlots.Max(slot => slot.StartX + slot.Extent);
        if (contentWidth <= 0)
        {
            return 0;
        }

        if (contentWidth <= _readerViewportWidth)
        {
            return (_readerViewportWidth - contentWidth) / 2;
        }

        return Math.Clamp(offset, _readerViewportWidth - contentWidth, 0);
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
        OnPropertyChanged(nameof(CanOpenLastReadingPosition));

        if (OpenFolderCommand is AsyncRelayCommand openFolderCommand)
        {
            openFolderCommand.RaiseCanExecuteChanged();
        }

        OpenLastReadingPositionCommand.RaiseCanExecuteChanged();
        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
        BackCommand.RaiseCanExecuteChanged();
        VisualLeftCommand.RaiseCanExecuteChanged();
        VisualRightCommand.RaiseCanExecuteChanged();
        FirstPageCommand.RaiseCanExecuteChanged();
        LastPageCommand.RaiseCanExecuteChanged();
        ToggleViewModeCommand.RaiseCanExecuteChanged();
    }
}
