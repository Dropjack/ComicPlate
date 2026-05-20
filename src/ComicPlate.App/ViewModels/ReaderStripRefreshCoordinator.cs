using ComicPlate.App.Controllers;
using ComicPlate.App.Services;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderStripRefreshCoordinator : IDisposable
{
    private readonly TimeSpan _viewportRefreshDelay;
    private CancellationTokenSource? _imageLoadCts;
    private CancellationTokenSource? _viewportRefreshCts;

    public ReaderStripRefreshCoordinator(TimeSpan viewportRefreshDelay)
    {
        _viewportRefreshDelay = viewportRefreshDelay;
    }

    public int Version { get; private set; }

    public int BeginRefresh()
    {
        Version++;
        CancelImageLoads();
        return Version;
    }

    public bool IsCurrent(int refreshVersion)
    {
        return refreshVersion == Version;
    }

    public void QueueViewportRefresh(
        ReaderStripPlacement? placement,
        Func<ReaderStripPlacement?, Task> refreshAsync)
    {
        CancelViewportRefresh();
        var cancellationTokenSource = new CancellationTokenSource();
        _viewportRefreshCts = cancellationTokenSource;
        _ = CommitViewportRefreshAsync(placement, refreshAsync, cancellationTokenSource.Token);
    }

    public void StartImageLoad(
        IReadOnlyList<ReaderStripItemViewModel> items,
        int refreshVersion,
        int currentPageIndex,
        ReaderImageCache imageCache,
        Func<ReaderStripItemViewModel, bool> isItemVisible)
    {
        CancelImageLoads();
        var cancellationTokenSource = new CancellationTokenSource();
        _imageLoadCts = cancellationTokenSource;
        _ = LoadImagesAsync(
            items,
            refreshVersion,
            currentPageIndex,
            imageCache,
            isItemVisible,
            cancellationTokenSource.Token);
    }

    public void CancelImageLoads()
    {
        _imageLoadCts?.Cancel();
        _imageLoadCts?.Dispose();
        _imageLoadCts = null;
    }

    public void CancelViewportRefresh()
    {
        _viewportRefreshCts?.Cancel();
        _viewportRefreshCts?.Dispose();
        _viewportRefreshCts = null;
    }

    public void Dispose()
    {
        CancelViewportRefresh();
        CancelImageLoads();
    }

    private async Task CommitViewportRefreshAsync(
        ReaderStripPlacement? placement,
        Func<ReaderStripPlacement?, Task> refreshAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_viewportRefreshDelay, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await refreshAsync(placement);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadImagesAsync(
        IReadOnlyList<ReaderStripItemViewModel> items,
        int refreshVersion,
        int currentPageIndex,
        ReaderImageCache imageCache,
        Func<ReaderStripItemViewModel, bool> isItemVisible,
        CancellationToken cancellationToken)
    {
        var orderedItems = items
            .OrderBy(item => item.IsCurrent ? 0 : 1)
            .ThenBy(item => Math.Abs(item.PageIndex - currentPageIndex))
            .ToArray();

        foreach (var item in orderedItems)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var image = await imageCache.GetOrLoadAsync(
                    item.PageIndex,
                    item.Slot.Page,
                    item.DecodeRequest,
                    cancellationToken);

                if (!IsCurrent(refreshVersion)
                    || cancellationToken.IsCancellationRequested
                    || !isItemVisible(item))
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
                if (IsCurrent(refreshVersion)
                    && !cancellationToken.IsCancellationRequested
                    && isItemVisible(item))
                {
                    item.StatusMessage = $"Could not display{Environment.NewLine}{item.Slot.Page.DisplayName}";
                }
            }
        }
    }
}
