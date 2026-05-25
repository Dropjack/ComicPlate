using System.Collections.ObjectModel;
using Avalonia.Threading;
using ComicPlate.App.Controllers;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderSurfaceViewModel : ViewModelBase, IDisposable
{
    private const int InitialMetadataPageRadius = 12;
    private const int NeighborPageLimit = 5;
    private const double ReaderViewportSizeEpsilon = 0.5;
    private static readonly TimeSpan ReaderViewportResizeCommitDelay = TimeSpan.FromMilliseconds(140);

    private readonly PageImageInfoLoader _pageImageInfoLoader;
    private readonly ReaderMotionSettings _motionSettings;
    private readonly ReaderFrameBuilder _readerFrameBuilder = new();
    private readonly ReaderImageCache _readerImageCache;
    private readonly ReaderMagnifierController _readerMagnifierController;
    private readonly ReaderState _readerState = new();
    private readonly ReaderStripController _readerStripController;
    private readonly ReaderStripItemBuilder _readerStripItemBuilder = new();
    private readonly ReaderStripRefreshCoordinator _readerStripRefreshCoordinator =
        new(ReaderViewportResizeCommitDelay);
    private IReadOnlyList<PageImageInfo> _pageImageInfos = Array.Empty<PageImageInfo>();
    private IReadOnlyList<ReaderFrame> _readerFrames = Array.Empty<ReaderFrame>();
    private int _currentPageIndex;
    private string _currentLogicalPath = "";
    private int _pageInfoLoadVersion;
    private string _pageText = "";
    private bool _hasReaderViewportSize;
    private int? _progressPreviewPageIndex;
    private string? _progressPreviewPageText;
    private readonly DispatcherTimer _bookOpenRevealTimer;
    private readonly DispatcherTimer _readerTransitionTimer;
    private double _bookOpenRevealStartOffset;
    private double _bookOpenRevealOffset;
    private double _bookOpenRevealOpacity = 1;
    private DateTimeOffset _bookOpenRevealStartedAt;
    private double _readerTransitionStartOffset;
    private double _readerTransitionOffset;
    private DateTimeOffset _readerTransitionStartedAt;

    public ReaderSurfaceViewModel(
        ReaderImageCache readerImageCache,
        ReadingDirection initialReadingDirection = ReadingDirection.RightToLeft,
        ViewMode initialViewMode = ViewMode.SinglePage,
        bool isMagnifierEnabled = true,
        PageImageInfoLoader? pageImageInfoLoader = null,
        ReaderMotionSettings? motionSettings = null)
    {
        _readerImageCache = readerImageCache;
        _pageImageInfoLoader = pageImageInfoLoader ?? new PageImageInfoLoader();
        _motionSettings = (motionSettings ?? ReaderMotionSettingsLoader.LoadEmbeddedOrDefault()).Normalize();
        _readerMagnifierController = new ReaderMagnifierController(_motionSettings.Magnifier);
        _readerStripController = new ReaderStripController(NeighborPageLimit, _motionSettings.ReaderInput);
        _readerMagnifierController.SetEnabled(isMagnifierEnabled);
        _readerState.SetReadingDirection(initialReadingDirection);
        _readerState.SetViewMode(initialViewMode);
        _bookOpenRevealTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_motionSettings.BookOpenReveal.FrameIntervalMs),
        };
        _bookOpenRevealTimer.Tick += OnBookOpenRevealTimerTick;
        _readerTransitionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_motionSettings.ReaderTransition.FrameIntervalMs),
        };
        _readerTransitionTimer.Tick += OnReaderTransitionTimerTick;

        NextPageCommand = new RelayCommand(NextPage, CanGoNextFrame);
        PreviousPageCommand = new RelayCommand(PreviousPage, CanGoPreviousFrame);
        VisualLeftCommand = new RelayCommand(VisualLeft);
        VisualRightCommand = new RelayCommand(VisualRight);
        FirstPageCommand = new RelayCommand(FirstPage, () => _readerState.HasPages);
        LastPageCommand = new RelayCommand(LastPage, () => _readerState.HasPages);
        ToggleViewModeCommand = new RelayCommand(ToggleViewMode);
        ToggleReadingDirectionCommand = new RelayCommand(ToggleReadingDirection);
    }

    public event Action? ReadingStateChanged;

    public ObservableCollection<PageListItemViewModel> PageItems { get; } = new();

    public ObservableCollection<ReaderStripItemViewModel> ReaderStripItems { get; private set; } = new();

    public RelayCommand NextPageCommand { get; }

    public RelayCommand PreviousPageCommand { get; }

    public RelayCommand VisualLeftCommand { get; }

    public RelayCommand VisualRightCommand { get; }

    public RelayCommand FirstPageCommand { get; }

    public RelayCommand LastPageCommand { get; }

    public RelayCommand ToggleViewModeCommand { get; }

    public RelayCommand ToggleReadingDirectionCommand { get; }

    public bool HasPages => _readerState.HasPages;

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

    public int DisplayPageProgressIndex => _progressPreviewPageIndex.HasValue
        ? PageIndexToVisualProgressIndex(_progressPreviewPageIndex.Value)
        : CurrentPageProgressIndex;

    public int LastPageProgressIndex => _readerState.HasPages ? Math.Max(_readerState.PageCount - 1, 0) : 0;

    public int PageCount => _readerState.PageCount;

    public string PageText
    {
        get => _progressPreviewPageText ?? _pageText;
        private set
        {
            if (SetProperty(ref _pageText, value))
            {
                OnPropertyChanged(nameof(PageText));
            }
        }
    }

    public string ViewModeText => _readerState.ViewMode == ViewMode.DoublePage
        ? LocalizationService.Current.GetString("Reader.DoublePage")
        : LocalizationService.Current.GetString("Reader.SinglePage");

    public bool IsSinglePageMode => _readerState.ViewMode == ViewMode.SinglePage;

    public bool IsDoublePageMode => _readerState.ViewMode == ViewMode.DoublePage;

    public string ReadingDirectionText => _readerState.ReadingDirection == ReadingDirection.RightToLeft
        ? LocalizationService.Current.GetString("Reader.DirectionRightToLeft")
        : LocalizationService.Current.GetString("Reader.DirectionLeftToRight");

    public bool IsLeftToRightReading => _readerState.ReadingDirection == ReadingDirection.LeftToRight;

    public bool IsRightToLeftReading => _readerState.ReadingDirection == ReadingDirection.RightToLeft;

    public bool IsMagnifierEnabled => _readerMagnifierController.IsEnabled;

    public bool IsMagnifierActive => _readerMagnifierController.IsActive;

    public double MagnifierScale => _readerMagnifierController.Scale;

    public string MagnifierScaleText => LocalizationService.Current.Format("Reader.ZoomScale", MagnifierScale);

    public double MagnifiedReaderStripTranslateX =>
        ReaderStripTranslateX + _readerMagnifierController.ContentTranslateX;

    public double MagnifierContentTranslateY => _readerMagnifierController.ContentTranslateY;

    public string CurrentLogicalPath
    {
        get => _currentLogicalPath;
        private set => SetProperty(ref _currentLogicalPath, value);
    }

    public double ReaderStripTranslateX =>
        _readerStripController.TranslateX + _readerTransitionOffset + _bookOpenRevealOffset;

    public double ReaderStripOpacity => _bookOpenRevealOpacity;

    public ReadingDirection ReadingDirection => _readerState.ReadingDirection;

    public ViewMode ViewMode => _readerState.ViewMode;

    public void SetMagnifierEnabled(bool isEnabled)
    {
        if (!_readerMagnifierController.SetEnabled(isEnabled))
        {
            return;
        }

        OnPropertyChanged(nameof(IsMagnifierEnabled));
        if (!isEnabled)
        {
            EndMagnifier();
        }
    }

    public void SetReaderViewportSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var hadReaderViewportSize = _hasReaderViewportSize;
        var isSizeChanged =
            Math.Abs(_readerStripController.ViewportWidth - width) > ReaderViewportSizeEpsilon
            || Math.Abs(_readerStripController.ViewportHeight - height) > ReaderViewportSizeEpsilon;
        if (!isSizeChanged && hadReaderViewportSize)
        {
            return;
        }

        var placement = _readerState.HasPages
            ? new ReaderStripPlacement(
                _readerState.CurrentPageIndex,
                _readerStripController.GetPageScreenCenter(_readerState.CurrentPageIndex, ReaderStripTranslateX))
            : null;

        _hasReaderViewportSize = true;
        _readerStripController.SetViewportSize(width, height);
        ClampMagnifier();

        if (!_readerState.HasPages)
        {
            UpdateReaderStripOffset();
            return;
        }

        if (!hadReaderViewportSize || ReaderStripItems.Count == 0)
        {
            _ = RefreshReaderStripAsync(placement);
            return;
        }

        if (!_readerStripItemBuilder.UpdateVisibleItemSizes(
            _readerFrames,
            ReaderStripItems,
            _readerStripController,
            _readerState.ReadingDirection))
        {
            _ = RefreshReaderStripAsync(placement);
            return;
        }

        UpdateReaderStripOffset(placement);
        QueueReaderViewportRefresh(placement);
    }

    public bool BeginMagnifier()
    {
        if (!_readerMagnifierController.Begin(_readerState.HasPages, out var activationChanged))
        {
            return false;
        }

        if (activationChanged)
        {
            OnPropertyChanged(nameof(IsMagnifierActive));
            OnPropertyChanged(nameof(MagnifierScale));
            OnPropertyChanged(nameof(MagnifierScaleText));
        }

        UpdateMagnifierTransform();
        return true;
    }

    public void EndMagnifier()
    {
        if (!_readerMagnifierController.End())
        {
            return;
        }

        OnPropertyChanged(nameof(IsMagnifierActive));
        OnPropertyChanged(nameof(MagnifierScale));
        OnPropertyChanged(nameof(MagnifierScaleText));
        OnPropertyChanged(nameof(MagnifiedReaderStripTranslateX));
        OnPropertyChanged(nameof(MagnifierContentTranslateY));
    }

    public void UpdateMagnifierPointer(double x, double y)
    {
        _readerMagnifierController.UpdatePointer(
            x,
            y,
            _readerStripController.ViewportWidth,
            _readerStripController.ViewportHeight);
        if (_readerMagnifierController.IsActive)
        {
            UpdateMagnifierTransform();
        }
    }

    public bool AdjustMagnifierScale(double wheelDelta)
    {
        if (!_readerMagnifierController.AdjustScale(wheelDelta, out var scaleChanged))
        {
            return false;
        }

        if (scaleChanged)
        {
            OnPropertyChanged(nameof(MagnifierScale));
            OnPropertyChanged(nameof(MagnifierScaleText));
            UpdateMagnifierTransform();
        }

        return true;
    }

    public void WheelNextReadingGroup(double inputDeltaMagnitude = 1)
    {
        MoveReaderStripFreely(GetNextReadingDirectionOffsetDelta(inputDeltaMagnitude));
    }

    public void WheelPreviousReadingGroup(double inputDeltaMagnitude = 1)
    {
        MoveReaderStripFreely(-GetNextReadingDirectionOffsetDelta(inputDeltaMagnitude));
    }

    public void TouchpadScrollVisualLeft(double inputDeltaMagnitude = 1)
    {
        MoveReaderStripFreely(_readerStripController.GetVisualLeftOffsetDelta(inputDeltaMagnitude));
    }

    public void TouchpadScrollVisualRight(double inputDeltaMagnitude = 1)
    {
        MoveReaderStripFreely(_readerStripController.GetVisualRightOffsetDelta(inputDeltaMagnitude));
    }

    public void GoToProgressRatio(double visualRatio)
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        var targetPageIndex = RatioToPageIndex(visualRatio);
        GoToProgressPage(targetPageIndex);
    }

    public void PreviewProgressRatio(double visualRatio)
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        var targetPageIndex = RatioToPageIndex(visualRatio);
        var landingPageIndex = GetProgressLandingPageIndex(targetPageIndex);
        _progressPreviewPageIndex = landingPageIndex;
        _progressPreviewPageText = FormatPageTextForPage(landingPageIndex);
        OnPropertyChanged(nameof(PageText));
        OnPropertyChanged(nameof(DisplayPageProgressIndex));
    }

    public void CommitProgressPreview(double visualRatio)
    {
        if (!_readerState.HasPages)
        {
            ClearProgressPreview();
            return;
        }

        var targetPageIndex = _progressPreviewPageIndex ?? RatioToPageIndex(visualRatio);
        GoToProgressPage(targetPageIndex);
        ClearProgressPreview();
        UpdatePageStatus();
    }

    public void CancelProgressPreview()
    {
        ClearProgressPreview();
    }

    public void BeginReaderStripDrag()
    {
        StopReaderTransition();
        _readerStripController.BeginDrag();
        UpdateReaderStripTransform();
    }

    public void DragReaderStrip(double horizontalDelta)
    {
        if (!_readerState.HasPages)
        {
            return;
        }

        _readerStripController.Drag(horizontalDelta);
        UpdateReaderStripTransform();
    }

    public void EndReaderStripDrag()
    {
        if (!_readerState.HasPages)
        {
            ResetReaderStripDrag();
            return;
        }

        CommitReaderStripFreeOffset(ReaderStripTranslateX);
    }

    public void CancelReaderStripDrag()
    {
        ResetReaderStripDrag();
    }

    public async Task LoadPagesAsync(IReadOnlyList<PageEntry> pages, int initialPageIndex = 0)
    {
        ClearProgressPreview();
        CancelReaderViewportRefresh();
        CancelReaderStripImageLoads();
        var pageInfoLoadVersion = ++_pageInfoLoadVersion;
        var initialPageImageInfos = CreateUnknownPageImageInfos(pages.Count);
        _pageImageInfos = initialPageImageInfos;
        _readerState.LoadPages(pages, initialPageIndex);
        PageItems.Clear();

        for (var index = 0; index < pages.Count; index++)
        {
            PageItems.Add(new PageListItemViewModel(index, pages[index]));
        }

        if (pages.Count == 0)
        {
            _readerImageCache.Clear();
            ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
            UpdatePageStatus();
            RaiseCommandStates();
            return;
        }

        ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
        UpdatePageStatus();
        RaiseCommandStates();

        await _pageImageInfoLoader.LoadAsync(
            pages,
            initialPageImageInfos,
            GetInitialMetadataPageIndexes(_readerState.CurrentPageIndex, pages.Count),
            CancellationToken.None);
        if (pageInfoLoadVersion != _pageInfoLoadVersion || pages != _readerState.Pages)
        {
            return;
        }

        _pageImageInfos = initialPageImageInfos;
        await RefreshReaderStripAsync(
            clearImageCacheBeforeReplace: true,
            startBookOpenReveal: true);
        _ = LoadPageImageInfosInBackgroundAsync(pages, pageInfoLoadVersion);
    }

    public void ClearPages()
    {
        ClearProgressPreview();
        CancelReaderViewportRefresh();
        CancelReaderStripImageLoads();
        StopBookOpenReveal();
        _readerImageCache.Clear();
        _pageInfoLoadVersion++;
        _pageImageInfos = Array.Empty<PageImageInfo>();
        _readerFrames = Array.Empty<ReaderFrame>();
        _readerState.LoadPages(Array.Empty<PageEntry>());
        PageItems.Clear();
        ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
        UpdatePageStatus();
        RaiseCommandStates();
    }

    private static PageImageInfo[] CreateUnknownPageImageInfos(int pageCount)
    {
        var infos = new PageImageInfo[pageCount];
        Array.Fill(infos, PageImageInfo.Unknown);
        return infos;
    }

    private static IReadOnlyList<int> GetInitialMetadataPageIndexes(int currentPageIndex, int pageCount)
    {
        if (pageCount <= 0)
        {
            return Array.Empty<int>();
        }

        var start = Math.Max(0, currentPageIndex - InitialMetadataPageRadius);
        var end = Math.Min(pageCount - 1, currentPageIndex + InitialMetadataPageRadius);
        return Enumerable.Range(start, end - start + 1).ToArray();
    }

    private async Task LoadPageImageInfosInBackgroundAsync(
        IReadOnlyList<PageEntry> pages,
        int pageInfoLoadVersion)
    {
        var seedInfos = _pageImageInfos;
        var infos = await Task.Run(() => _pageImageInfoLoader.LoadAsync(pages, seedInfos, CancellationToken.None));
        if (pageInfoLoadVersion != _pageInfoLoadVersion || pages != _readerState.Pages)
        {
            return;
        }

        _pageImageInfos = infos;
        await RefreshReaderStripAsync(new ReaderStripPlacement(
            _readerState.CurrentPageIndex,
            _readerStripController.GetPageScreenCenter(_readerState.CurrentPageIndex, ReaderStripTranslateX)),
            preserveBookOpenReveal: true);
    }

    private Task RefreshReaderStripAsync(
        ReaderStripPlacement? placement = null,
        int transitionDirection = 0,
        bool clearImageCacheBeforeReplace = false,
        bool startBookOpenReveal = false,
        bool preserveBookOpenReveal = false)
    {
        var refreshVersion = _readerStripRefreshCoordinator.BeginRefresh();
        StopReaderTransition();
        if (startBookOpenReveal || !preserveBookOpenReveal)
        {
            StopBookOpenReveal();
        }

        if (!_readerState.HasPages)
        {
            if (clearImageCacheBeforeReplace)
            {
                _readerImageCache.Clear();
            }

            ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
            UpdatePageStatus();
            return Task.CompletedTask;
        }

        var page = _readerState.Pages[_readerState.CurrentPageIndex];
        _currentPageIndex = _readerState.CurrentPageIndex;
        OnPropertyChanged(nameof(CurrentPageIndex));

        CurrentLogicalPath = page.LogicalPath;

        _readerFrames = _readerFrameBuilder.Build(
            _readerState.Pages,
            _pageImageInfos,
            _readerState.CurrentPageIndex,
            _readerState.ViewMode,
            _readerState.ReadingDirection);
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        if (currentFrame is null)
        {
            if (clearImageCacheBeforeReplace)
            {
                _readerImageCache.Clear();
            }

            ReplaceReaderStripItems(new ObservableCollection<ReaderStripItemViewModel>());
            UpdatePageStatus();
            return Task.CompletedTask;
        }

        var buildResult = _readerStripItemBuilder.BuildWindowItems(
            _readerFrames,
            currentFrame,
            _readerStripController,
            _readerState.ReadingDirection);

        if (!_readerStripRefreshCoordinator.IsCurrent(refreshVersion))
        {
            return Task.CompletedTask;
        }

        if (clearImageCacheBeforeReplace)
        {
            _readerImageCache.Clear();
        }

        ReplaceReaderStripItems(buildResult.Items, placement);
        _readerImageCache.TrimToBudget(buildResult.ActivePageIndexes, _readerState.CurrentPageIndex);
        _readerStripRefreshCoordinator.StartImageLoad(
            buildResult.Items.ToArray(),
            refreshVersion,
            _readerState.CurrentPageIndex,
            _readerImageCache,
            ReaderStripItems.Contains);
        UpdatePageStatus();
        ReadingStateChanged?.Invoke();
        RaiseCommandStates();
        StartReaderTransition(transitionDirection);
        if (startBookOpenReveal)
        {
            StartBookOpenReveal();
        }

        return Task.CompletedTask;
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

        var previousPageIndex = _readerState.CurrentPageIndex;
        _readerState.GoToFrameStartPage(_readerFrames[nextFrameIndex].PageIndexes.Min());
        _ = RefreshReaderStripAsync(transitionDirection: GetReaderTransitionDirection(
            previousPageIndex,
            _readerState.CurrentPageIndex));
    }

    private void GoToProgressPage(int pageIndex)
    {
        var landingPageIndex = GetProgressLandingPageIndex(pageIndex);
        if (landingPageIndex == _readerState.CurrentPageIndex)
        {
            return;
        }

        var previousPageIndex = _readerState.CurrentPageIndex;
        _readerState.GoToFrameStartPage(landingPageIndex);
        _ = RefreshReaderStripAsync(transitionDirection: GetReaderTransitionDirection(
            previousPageIndex,
            landingPageIndex));
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
        OnPropertyChanged(nameof(ViewMode));
        OnPropertyChanged(nameof(ViewModeText));
        OnPropertyChanged(nameof(IsSinglePageMode));
        OnPropertyChanged(nameof(IsDoublePageMode));
        _ = RefreshReaderStripAsync();
    }

    private void ToggleReadingDirection()
    {
        var nextReadingDirection = _readerState.ReadingDirection == ReadingDirection.RightToLeft
            ? ReadingDirection.LeftToRight
            : ReadingDirection.RightToLeft;
        _readerState.SetReadingDirection(nextReadingDirection);
        OnPropertyChanged(nameof(ReadingDirection));
        OnPropertyChanged(nameof(ReadingDirectionText));
        OnPropertyChanged(nameof(IsLeftToRightReading));
        OnPropertyChanged(nameof(IsRightToLeftReading));
        _ = RefreshReaderStripAsync();
    }

    private void ReplaceReaderStripItems(
        ObservableCollection<ReaderStripItemViewModel> items,
        ReaderStripPlacement? placement = null)
    {
        ReaderStripItems = items;
        OnPropertyChanged(nameof(ReaderStripItems));
        UpdateReaderStripOffset(placement);
    }

    private void QueueReaderViewportRefresh(ReaderStripPlacement? placement)
    {
        _readerStripRefreshCoordinator.QueueViewportRefresh(
            placement,
            nextPlacement => RefreshReaderStripAsync(nextPlacement));
    }

    private void CancelReaderStripImageLoads()
    {
        _readerStripRefreshCoordinator.CancelImageLoads();
    }

    private void CancelReaderViewportRefresh()
    {
        _readerStripRefreshCoordinator.CancelViewportRefresh();
    }

    public void Dispose()
    {
        StopBookOpenReveal();
        _bookOpenRevealTimer.Tick -= OnBookOpenRevealTimerTick;
        StopReaderTransition();
        _readerTransitionTimer.Tick -= OnReaderTransitionTimerTick;
        _readerStripRefreshCoordinator.Dispose();
    }

    private void UpdateReaderStripOffset(ReaderStripPlacement? placement = null)
    {
        if (ReaderStripItems.Count == 0)
        {
            _readerStripController.UpdateOffset(
                Array.Empty<int>(),
                new Dictionary<int, double>(),
                Array.Empty<int>());
            UpdateReaderStripTransform();
            return;
        }

        var windowPageIndexes = ReaderStripItems
            .Select(item => item.PageIndex)
            .ToArray();
        var pageExtents = ReaderStripItems.ToDictionary(
            item => item.PageIndex,
            item => item.DisplayWidth);
        var currentGroupPageIndexes = _readerFrames
            .FirstOrDefault(frame => frame.IsCurrent)
            ?.PageIndexes ?? _readerState.CurrentReadingGroupPageIndexes;
        _readerStripController.UpdateOffset(
            windowPageIndexes,
            pageExtents,
            currentGroupPageIndexes,
            placement);
        UpdateReaderStripTransform();
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

        var result = _readerStripController.CommitFreeOffset(
            targetOffset,
            _readerState.CurrentPageIndex,
            _readerFrames);
        UpdateReaderStripTransform();

        if (result is null || !result.CurrentFrameChanged)
        {
            return;
        }

        _readerState.GoToFrameStartPage(result.TargetFrameStartPageIndex);
        _ = RefreshReaderStripAsync(result.Placement);
    }

    private double GetNextReadingDirectionOffsetDelta(double inputDeltaMagnitude = 1)
    {
        return _readerStripController.GetNextReadingDirectionOffsetDelta(
            _readerState.ReadingDirection,
            inputDeltaMagnitude);
    }

    private void ResetReaderStripDrag()
    {
        _readerStripController.CancelDrag();
        UpdateReaderStripTransform();
    }

    private void UpdateReaderStripTransform()
    {
        OnPropertyChanged(nameof(ReaderStripTranslateX));
        if (_readerMagnifierController.IsActive)
        {
            UpdateMagnifierTransform();
        }
        else
        {
            OnPropertyChanged(nameof(MagnifiedReaderStripTranslateX));
        }
    }

    private void StartBookOpenReveal()
    {
        var motion = _motionSettings.BookOpenReveal;
        if (!motion.Enabled || !_readerState.HasPages || _readerStripController.ViewportWidth <= 0)
        {
            return;
        }

        _bookOpenRevealStartOffset = _readerState.ReadingDirection == ReadingDirection.RightToLeft
            ? motion.DistanceDip
            : -motion.DistanceDip;
        _bookOpenRevealOffset = _bookOpenRevealStartOffset;
        _bookOpenRevealOpacity = motion.OpacityFrom;
        _bookOpenRevealStartedAt = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(ReaderStripOpacity));
        UpdateReaderStripTransform();
        _bookOpenRevealTimer.Start();
    }

    private void StopBookOpenReveal()
    {
        var targetOpacity = _motionSettings.BookOpenReveal.OpacityTo;
        _bookOpenRevealTimer.Stop();
        if (Math.Abs(_bookOpenRevealOffset) <= 0.001
            && Math.Abs(_bookOpenRevealOpacity - targetOpacity) <= 0.001)
        {
            _bookOpenRevealOffset = 0;
            _bookOpenRevealOpacity = targetOpacity;
            return;
        }

        _bookOpenRevealOffset = 0;
        _bookOpenRevealOpacity = targetOpacity;
        OnPropertyChanged(nameof(ReaderStripOpacity));
        UpdateReaderStripTransform();
    }

    private void UpdateMagnifierTransform()
    {
        if (!_readerMagnifierController.IsActive)
        {
            return;
        }

        var normalLeft = ReaderStripTranslateX;
        _readerMagnifierController.UpdateTransform(
            normalLeft,
            _readerStripController.ViewportWidth,
            _readerStripController.ViewportHeight,
            GetReaderContentBounds());
        OnPropertyChanged(nameof(MagnifiedReaderStripTranslateX));
        OnPropertyChanged(nameof(MagnifierContentTranslateY));
    }

    private void ClampMagnifier()
    {
        if (_readerMagnifierController.IsActive)
        {
            UpdateMagnifierTransform();
        }
    }

    private ReaderMagnifierContentBounds GetReaderContentBounds()
    {
        var right = _readerStripController.LayoutSlots.Count == 0
            ? _readerStripController.ViewportWidth
            : _readerStripController.LayoutSlots.Max(slot => slot.StartX + slot.Extent);
        var bottom = ReaderStripItems.Count == 0
            ? _readerStripController.ViewportHeight
            : ReaderStripItems.Max(item => item.DisplayHeight);

        return new ReaderMagnifierContentBounds(
            0,
            0,
            Math.Max(1, right),
            Math.Max(1, bottom));
    }

    private int GetReaderTransitionDirection(int previousPageIndex, int targetPageIndex)
    {
        if (previousPageIndex == targetPageIndex)
        {
            return 0;
        }

        var logicalDirection = Math.Sign(targetPageIndex - previousPageIndex);
        return _readerState.ReadingDirection == ReadingDirection.RightToLeft
            ? -logicalDirection
            : logicalDirection;
    }

    private void StartReaderTransition(int direction)
    {
        var motion = _motionSettings.ReaderTransition;
        if (!motion.Enabled || direction == 0 || _readerStripController.ViewportWidth <= 0)
        {
            return;
        }

        var distance = Math.Clamp(
            _readerStripController.ViewportWidth * motion.DistanceViewportRatio,
            motion.MinDistanceDip,
            motion.MaxDistanceDip);
        _readerTransitionStartOffset = direction > 0 ? distance : -distance;
        _readerTransitionOffset = _readerTransitionStartOffset;
        _readerTransitionStartedAt = DateTimeOffset.UtcNow;
        UpdateReaderStripTransform();
        _readerTransitionTimer.Start();
    }

    private void StopReaderTransition()
    {
        _readerTransitionTimer.Stop();
        if (Math.Abs(_readerTransitionOffset) <= 0.001)
        {
            _readerTransitionOffset = 0;
            return;
        }

        _readerTransitionOffset = 0;
        UpdateReaderStripTransform();
    }

    private void OnReaderTransitionTimerTick(object? sender, EventArgs e)
    {
        var elapsed = DateTimeOffset.UtcNow - _readerTransitionStartedAt;
        var progress = Math.Clamp(
            elapsed.TotalMilliseconds / _motionSettings.ReaderTransition.DurationMs,
            0,
            1);
        if (progress >= 1)
        {
            StopReaderTransition();
            return;
        }

        var easedProgress = EaseOutCubic(progress);
        _readerTransitionOffset = _readerTransitionStartOffset * (1 - easedProgress);
        UpdateReaderStripTransform();
    }

    private void OnBookOpenRevealTimerTick(object? sender, EventArgs e)
    {
        var motion = _motionSettings.BookOpenReveal;
        var elapsed = DateTimeOffset.UtcNow - _bookOpenRevealStartedAt;
        var progress = Math.Clamp(
            elapsed.TotalMilliseconds / motion.DurationMs,
            0,
            1);
        if (progress >= 1)
        {
            StopBookOpenReveal();
            return;
        }

        var easedProgress = EaseOutCubic(progress);
        _bookOpenRevealOffset = _bookOpenRevealStartOffset * (1 - easedProgress);
        _bookOpenRevealOpacity = Lerp(motion.OpacityFrom, motion.OpacityTo, easedProgress);
        OnPropertyChanged(nameof(ReaderStripOpacity));
        UpdateReaderStripTransform();
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * progress);
    }

    private static double EaseOutCubic(double progress)
    {
        var inverse = 1 - progress;
        return 1 - (inverse * inverse * inverse);
    }

    private void UpdatePageStatus()
    {
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(CurrentPageProgressIndex));
        OnPropertyChanged(nameof(DisplayPageProgressIndex));
        OnPropertyChanged(nameof(LastPageProgressIndex));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(HasPages));

        PageText = FormatPageText(
            _readerFrames.FirstOrDefault(frame => frame.IsCurrent),
            _readerState.CurrentPageIndex);
    }

    private int RatioToPageIndex(double visualRatio)
    {
        return ReaderProgressMapper.RatioToPageIndex(
            visualRatio,
            _readerState.PageCount,
            _readerState.ReadingDirection);
    }

    private int PageIndexToVisualProgressIndex(int pageIndex)
    {
        if (!_readerState.HasPages)
        {
            return 0;
        }

        var clampedPageIndex = Math.Clamp(pageIndex, 0, _readerState.PageCount - 1);
        return _readerState.ReadingDirection == ReadingDirection.RightToLeft
            ? LastPageProgressIndex - clampedPageIndex
            : clampedPageIndex;
    }

    private int GetProgressLandingPageIndex(int pageIndex)
    {
        var clampedPageIndex = _readerState.HasPages
            ? Math.Clamp(pageIndex, 0, _readerState.PageCount - 1)
            : 0;
        var targetFrame = _readerFrames.FirstOrDefault(frame => frame.PageIndexes.Contains(clampedPageIndex));
        return targetFrame?.PageIndexes.Min() ?? clampedPageIndex;
    }

    private string FormatPageTextForPage(int pageIndex)
    {
        var targetFrame = _readerFrames.FirstOrDefault(frame => frame.PageIndexes.Contains(pageIndex));
        return FormatPageText(targetFrame, pageIndex);
    }

    private string FormatPageText(ReaderFrame? frame, int fallbackPageIndex)
    {
        return ReaderFramePageTextFormatter.Format(
            frame,
            fallbackPageIndex,
            _readerState.PageCount);
    }

    private void ClearProgressPreview()
    {
        if (!_progressPreviewPageIndex.HasValue && _progressPreviewPageText is null)
        {
            return;
        }

        _progressPreviewPageIndex = null;
        _progressPreviewPageText = null;
        OnPropertyChanged(nameof(PageText));
        OnPropertyChanged(nameof(DisplayPageProgressIndex));
    }

    private void RaiseCommandStates()
    {
        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
        VisualLeftCommand.RaiseCanExecuteChanged();
        VisualRightCommand.RaiseCanExecuteChanged();
        FirstPageCommand.RaiseCanExecuteChanged();
        LastPageCommand.RaiseCanExecuteChanged();
        ToggleViewModeCommand.RaiseCanExecuteChanged();
        ToggleReadingDirectionCommand.RaiseCanExecuteChanged();
    }
}
