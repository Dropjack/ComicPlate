using System.Collections.ObjectModel;
using Avalonia.Threading;
using ComicPlate.App.Controllers;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderSurfaceViewModel : ViewModelBase, IDisposable
{
    private const int NeighborPageLimit = 5;
    private const double ReaderFrameVerticalPadding = 0;
    private const double ReaderViewportSizeEpsilon = 0.5;
    private const double ReaderTransitionDistanceRatio = 0.32;
    private static readonly TimeSpan ReaderViewportResizeCommitDelay = TimeSpan.FromMilliseconds(140);
    private static readonly TimeSpan ReaderTransitionDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ReaderTransitionFrameInterval = TimeSpan.FromMilliseconds(16);

    private readonly PageImageInfoLoader _pageImageInfoLoader = new();
    private readonly ReaderFrameBuilder _readerFrameBuilder = new();
    private readonly ReaderImageCache _readerImageCache;
    private readonly ReaderMagnifierController _readerMagnifierController = new();
    private readonly ReaderState _readerState = new();
    private readonly ReaderStripController _readerStripController = new(NeighborPageLimit);
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
    private readonly DispatcherTimer _readerTransitionTimer;
    private double _readerTransitionStartOffset;
    private double _readerTransitionOffset;
    private DateTimeOffset _readerTransitionStartedAt;

    public ReaderSurfaceViewModel(
        ReaderImageCache readerImageCache,
        ReadingDirection initialReadingDirection = ReadingDirection.RightToLeft,
        ViewMode initialViewMode = ViewMode.SinglePage,
        bool isMagnifierEnabled = true)
    {
        _readerImageCache = readerImageCache;
        _readerMagnifierController.SetEnabled(isMagnifierEnabled);
        _readerState.SetReadingDirection(initialReadingDirection);
        _readerState.SetViewMode(initialViewMode);
        _readerTransitionTimer = new DispatcherTimer
        {
            Interval = ReaderTransitionFrameInterval,
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

    public string ViewModeText => _readerState.ViewMode == ViewMode.DoublePage ? "双页" : "单页";

    public bool IsSinglePageMode => _readerState.ViewMode == ViewMode.SinglePage;

    public bool IsDoublePageMode => _readerState.ViewMode == ViewMode.DoublePage;

    public string ReadingDirectionText => _readerState.ReadingDirection == ReadingDirection.RightToLeft ? "RTL" : "LTR";

    public bool IsLeftToRightReading => _readerState.ReadingDirection == ReadingDirection.LeftToRight;

    public bool IsRightToLeftReading => _readerState.ReadingDirection == ReadingDirection.RightToLeft;

    public bool IsMagnifierEnabled => _readerMagnifierController.IsEnabled;

    public bool IsMagnifierActive => _readerMagnifierController.IsActive;

    public double MagnifierScale => _readerMagnifierController.Scale;

    public string MagnifierScaleText => $"{MagnifierScale:0.0}x";

    public double MagnifiedReaderStripTranslateX =>
        ReaderStripTranslateX + _readerMagnifierController.ContentTranslateX;

    public double MagnifierContentTranslateY => _readerMagnifierController.ContentTranslateY;

    public string CurrentLogicalPath
    {
        get => _currentLogicalPath;
        private set => SetProperty(ref _currentLogicalPath, value);
    }

    public double ReaderStripTranslateX => _readerStripController.TranslateX + _readerTransitionOffset;

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

        if (!UpdateVisibleReaderStripItemSizes())
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

    public void WheelNextReadingGroup()
    {
        MoveReaderStripFreely(GetNextReadingDirectionOffsetDelta());
    }

    public void WheelPreviousReadingGroup()
    {
        MoveReaderStripFreely(-GetNextReadingDirectionOffsetDelta());
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
        _pageImageInfos = CreateUnknownPageImageInfos(pages.Count);
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

        await RefreshReaderStripAsync(clearImageCacheBeforeReplace: true);
        _ = LoadPageImageInfosInBackgroundAsync(pages, pageInfoLoadVersion);
    }

    public void ClearPages()
    {
        ClearProgressPreview();
        CancelReaderViewportRefresh();
        CancelReaderStripImageLoads();
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
            _readerStripController.GetPageScreenCenter(_readerState.CurrentPageIndex, ReaderStripTranslateX)));
    }

    private Task RefreshReaderStripAsync(
        ReaderStripPlacement? placement = null,
        int transitionDirection = 0,
        bool clearImageCacheBeforeReplace = false)
    {
        var refreshVersion = _readerStripRefreshCoordinator.BeginRefresh();
        StopReaderTransition();

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
                var item = new ReaderStripItemViewModel(slot, framePage.ImageInfo);
                item.SetViewportSize(_readerStripController.ViewportWidth, _readerStripController.ViewportHeight);
                item.SetDisplaySize(displaySizes[framePageIndex].Width, displaySizes[framePageIndex].Height);

                nextItems.Add(item);
            }
        }

        if (!_readerStripRefreshCoordinator.IsCurrent(refreshVersion))
        {
            return Task.CompletedTask;
        }

        if (clearImageCacheBeforeReplace)
        {
            _readerImageCache.Clear();
        }

        ReplaceReaderStripItems(nextItems, placement);
        _readerImageCache.TrimToBudget(activeIndexes, _readerState.CurrentPageIndex);
        _readerStripRefreshCoordinator.StartImageLoad(
            nextItems.ToArray(),
            refreshVersion,
            _readerState.CurrentPageIndex,
            _readerImageCache,
            ReaderStripItems.Contains);
        UpdatePageStatus();
        ReadingStateChanged?.Invoke();
        RaiseCommandStates();
        StartReaderTransition(transitionDirection);
        return Task.CompletedTask;
    }

    private IReadOnlyList<ReaderFrame> CreateFrameWindow(int currentFrameIndex)
    {
        return _readerStripController.CreateFrameWindow(
            _readerFrames,
            currentFrameIndex,
            _readerState.ReadingDirection);
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
        var availableHeight = Math.Max(160, _readerStripController.ViewportHeight - ReaderFrameVerticalPadding);
        var targetHeight = availableHeight;

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

    private bool UpdateVisibleReaderStripItemSizes()
    {
        var currentFrame = _readerFrames.FirstOrDefault(frame => frame.IsCurrent);
        if (currentFrame is null || ReaderStripItems.Count == 0)
        {
            return false;
        }

        var visibleItems = ReaderStripItems.ToDictionary(item => item.PageIndex);
        foreach (var frame in CreateFrameWindow(currentFrame.FrameIndex))
        {
            var displaySizes = CalculateFrameDisplaySizes(frame);
            for (var framePageIndex = 0; framePageIndex < frame.Pages.Count; framePageIndex++)
            {
                var framePage = frame.Pages[framePageIndex];
                if (!visibleItems.TryGetValue(framePage.PageIndex, out var item))
                {
                    return false;
                }

                item.SetViewportSize(_readerStripController.ViewportWidth, _readerStripController.ViewportHeight);
                item.SetDisplaySize(displaySizes[framePageIndex].Width, displaySizes[framePageIndex].Height);
            }
        }

        return true;
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

    private double GetNextReadingDirectionOffsetDelta()
    {
        return _readerStripController.GetNextReadingDirectionOffsetDelta(_readerState.ReadingDirection);
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
        if (direction == 0 || _readerStripController.ViewportWidth <= 0)
        {
            return;
        }

        var distance = Math.Clamp(
            _readerStripController.ViewportWidth * ReaderTransitionDistanceRatio,
            120,
            360);
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
            elapsed.TotalMilliseconds / ReaderTransitionDuration.TotalMilliseconds,
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
