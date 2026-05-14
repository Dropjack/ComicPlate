using System.Collections.ObjectModel;
using ComicPlate.App.Controllers;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderSurfaceViewModel : ViewModelBase, IDisposable
{
    private const int NeighborPageLimit = 5;
    private const int ReaderViewportResizeCommitDelayMilliseconds = 140;
    private const double ReaderStripItemHorizontalMargin = 8;
    private const double ReaderFrameHorizontalPadding = 28;
    private const double ReaderFrameVerticalPadding = 24;
    private const double ReaderViewportSizeEpsilon = 0.5;

    private readonly PageImageInfoLoader _pageImageInfoLoader = new();
    private readonly ReaderFrameBuilder _readerFrameBuilder = new();
    private readonly ReaderImageCache _readerImageCache;
    private readonly ReaderState _readerState = new();
    private readonly ReaderStripController _readerStripController = new(NeighborPageLimit);
    private IReadOnlyList<PageImageInfo> _pageImageInfos = Array.Empty<PageImageInfo>();
    private IReadOnlyList<ReaderFrame> _readerFrames = Array.Empty<ReaderFrame>();
    private int _currentPageIndex;
    private string _currentLogicalPath = "";
    private int _pageInfoLoadVersion;
    private string _pageText = "";
    private bool _hasReaderViewportSize;
    private CancellationTokenSource? _readerStripImageLoadCts;
    private int _readerStripRefreshVersion;
    private CancellationTokenSource? _readerViewportRefreshCts;

    public ReaderSurfaceViewModel(ReaderImageCache readerImageCache)
    {
        _readerImageCache = readerImageCache;

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

    public int LastPageProgressIndex => _readerState.HasPages ? Math.Max(_readerState.PageCount - 1, 0) : 0;

    public int PageCount => _readerState.PageCount;

    public string PageText
    {
        get => _pageText;
        private set => SetProperty(ref _pageText, value);
    }

    public string ViewModeText => _readerState.ViewMode == ViewMode.DoublePage ? "Double" : "Single";

    public string ReadingDirectionText => _readerState.ReadingDirection == ReadingDirection.RightToLeft ? "RTL" : "LTR";

    public string CurrentLogicalPath
    {
        get => _currentLogicalPath;
        private set => SetProperty(ref _currentLogicalPath, value);
    }

    public double ReaderStripTranslateX => _readerStripController.TranslateX;

    public ReadingDirection ReadingDirection => _readerState.ReadingDirection;

    public ViewMode ViewMode => _readerState.ViewMode;

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
        CancelReaderViewportRefresh();
        CancelReaderStripImageLoads();
        _readerImageCache.Clear();
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

    public void ClearPages()
    {
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

    private Task RefreshReaderStripAsync(ReaderStripPlacement? placement = null)
    {
        var refreshVersion = ++_readerStripRefreshVersion;
        CancelReaderStripImageLoads();

        if (!_readerState.HasPages)
        {
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

        if (refreshVersion != _readerStripRefreshVersion)
        {
            return Task.CompletedTask;
        }

        ReplaceReaderStripItems(nextItems, placement);
        _readerImageCache.TrimTo(activeIndexes);
        StartReaderStripImageLoad(nextItems, refreshVersion);
        UpdatePageStatus();
        ReadingStateChanged?.Invoke();
        RaiseCommandStates();
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
        var availableWidth = Math.Max(160, _readerStripController.ViewportWidth - ReaderFrameHorizontalPadding);
        var availableHeight = Math.Max(160, _readerStripController.ViewportHeight - ReaderFrameVerticalPadding);
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

    private void ToggleReadingDirection()
    {
        var nextReadingDirection = _readerState.ReadingDirection == ReadingDirection.RightToLeft
            ? ReadingDirection.LeftToRight
            : ReadingDirection.RightToLeft;
        _readerState.SetReadingDirection(nextReadingDirection);
        OnPropertyChanged(nameof(ReadingDirection));
        OnPropertyChanged(nameof(ReadingDirectionText));
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

    private void StartReaderStripImageLoad(
        ObservableCollection<ReaderStripItemViewModel> items,
        int refreshVersion)
    {
        CancelReaderStripImageLoads();
        var cancellationTokenSource = new CancellationTokenSource();
        _readerStripImageLoadCts = cancellationTokenSource;
        _ = LoadReaderStripImagesAsync(items.ToArray(), refreshVersion, cancellationTokenSource.Token);
    }

    private void QueueReaderViewportRefresh(ReaderStripPlacement? placement)
    {
        CancelReaderViewportRefresh();
        var cancellationTokenSource = new CancellationTokenSource();
        _readerViewportRefreshCts = cancellationTokenSource;
        _ = CommitReaderViewportRefreshAsync(placement, cancellationTokenSource.Token);
    }

    private async Task CommitReaderViewportRefreshAsync(
        ReaderStripPlacement? placement,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(ReaderViewportResizeCommitDelayMilliseconds, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await RefreshReaderStripAsync(placement);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadReaderStripImagesAsync(
        IReadOnlyList<ReaderStripItemViewModel> items,
        int refreshVersion,
        CancellationToken cancellationToken)
    {
        var currentPageIndex = _readerState.CurrentPageIndex;
        var orderedItems = items
            .OrderBy(item => item.IsCurrent ? 0 : 1)
            .ThenBy(item => Math.Abs(item.PageIndex - currentPageIndex))
            .ToArray();

        foreach (var item in orderedItems)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var image = await _readerImageCache.GetOrLoadAsync(
                    item.PageIndex,
                    item.Slot.Page,
                    item.DecodeRequest,
                    cancellationToken);

                if (refreshVersion != _readerStripRefreshVersion
                    || cancellationToken.IsCancellationRequested
                    || !ReaderStripItems.Contains(item))
                {
                    return;
                }

                item.Image = image;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                if (refreshVersion == _readerStripRefreshVersion
                    && !cancellationToken.IsCancellationRequested
                    && ReaderStripItems.Contains(item))
                {
                    item.StatusMessage = $"Could not display{Environment.NewLine}{item.Slot.Page.DisplayName}";
                }
            }
        }
    }

    private void CancelReaderStripImageLoads()
    {
        _readerStripImageLoadCts?.Cancel();
        _readerStripImageLoadCts?.Dispose();
        _readerStripImageLoadCts = null;
    }

    private void CancelReaderViewportRefresh()
    {
        _readerViewportRefreshCts?.Cancel();
        _readerViewportRefreshCts?.Dispose();
        _readerViewportRefreshCts = null;
    }

    public void Dispose()
    {
        CancelReaderViewportRefresh();
        CancelReaderStripImageLoads();
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
            item => item.DisplayWidth + (ReaderStripItemHorizontalMargin * 2));
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
    }

    private void UpdatePageStatus()
    {
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(CurrentPageProgressIndex));
        OnPropertyChanged(nameof(LastPageProgressIndex));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(HasPages));

        PageText = _readerState.HasPages
            ? $"{CurrentPageNumber} / {PageCount}"
            : "0 / 0";
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
