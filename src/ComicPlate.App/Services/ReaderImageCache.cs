using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ReaderImageCache : IDisposable
{
    public const long DefaultBudgetBytes = 384L * 1024 * 1024;
    private static readonly TimeSpan RetiredImageDisposeDelay = TimeSpan.FromSeconds(2);

    private readonly Dictionary<int, CacheEntry> _cache = new();
    private readonly Func<PageEntry, ReaderImageDecodeRequest, CancellationToken, Task<Bitmap>> _loadImageAsync;
    private readonly long _budgetBytes;
    private readonly List<Bitmap> _retiredImages = new();
    private readonly DispatcherTimer _retiredImageDisposeTimer;
    private long _accessClock;
    private long _estimatedBytes;
    private bool _isDisposed;

    public ReaderImageCache(ImagePageLoader imagePageLoader, long budgetBytes = DefaultBudgetBytes)
        : this(imagePageLoader.LoadAsync, budgetBytes)
    {
    }

    public ReaderImageCache(
        Func<PageEntry, ReaderImageDecodeRequest, CancellationToken, Task<Bitmap>> loadImageAsync,
        long budgetBytes = DefaultBudgetBytes)
    {
        _loadImageAsync = loadImageAsync;
        _budgetBytes = Math.Max(0, budgetBytes);
        _retiredImageDisposeTimer = new DispatcherTimer
        {
            Interval = RetiredImageDisposeDelay,
        };
        _retiredImageDisposeTimer.Tick += OnRetiredImageDisposeTimerTick;
    }

    public async Task<Bitmap> GetOrLoadAsync(
        int pageIndex,
        PageEntry page,
        ReaderImageDecodeRequest request,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(pageIndex, out var cachedEntry)
            && cachedEntry.Request.CanReuseFor(request))
        {
            cachedEntry.MarkAccessed(GetNextAccessOrder());
            return cachedEntry.Image;
        }

        var image = await _loadImageAsync(
            page,
            request,
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            image.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (_cache.TryGetValue(pageIndex, out var oldEntry))
        {
            _estimatedBytes -= oldEntry.EstimatedBytes;
            RetireImage(oldEntry.Image);
        }

        var nextEntry = new CacheEntry(image, request, request.EstimatedBytes, GetNextAccessOrder());
        _cache[pageIndex] = nextEntry;
        _estimatedBytes += nextEntry.EstimatedBytes;
        return image;
    }

    public Bitmap? TryGetPreview(int pageIndex)
    {
        if (!_cache.TryGetValue(pageIndex, out var cachedEntry))
        {
            return null;
        }

        cachedEntry.MarkAccessed(GetNextAccessOrder());
        return cachedEntry.Image;
    }

    public Bitmap? TryGetReusable(int pageIndex, ReaderImageDecodeRequest request)
    {
        if (!_cache.TryGetValue(pageIndex, out var cachedEntry)
            || !cachedEntry.Request.CanReuseFor(request))
        {
            return null;
        }

        cachedEntry.MarkAccessed(GetNextAccessOrder());
        return cachedEntry.Image;
    }

    public void TrimToBudget(IReadOnlySet<int> activeIndexes, int currentPageIndex)
    {
        if (_estimatedBytes <= _budgetBytes)
        {
            return;
        }

        var removableIndexes = ReaderImageCacheBudgetPolicy.SelectRemovalOrder(
            _cache.Select(entry => new ReaderImageCacheBudgetCandidate(
                entry.Key,
                entry.Value.LastAccessOrder)),
            activeIndexes,
            currentPageIndex);

        foreach (var index in removableIndexes)
        {
            if (_estimatedBytes <= _budgetBytes)
            {
                return;
            }

            Remove(index);
        }
    }

    public void Clear()
    {
        foreach (var entry in _cache.Values)
        {
            RetireImage(entry.Image);
        }

        _cache.Clear();
        _estimatedBytes = 0;
    }

    public void Dispose()
    {
        _isDisposed = true;
        _retiredImageDisposeTimer.Stop();
        Clear();
        DisposeRetiredImages();
    }

    private long GetNextAccessOrder()
    {
        return ++_accessClock;
    }

    private void Remove(int pageIndex)
    {
        if (!_cache.Remove(pageIndex, out var entry))
        {
            return;
        }

        _estimatedBytes -= entry.EstimatedBytes;
        RetireImage(entry.Image);
    }

    private void RetireImage(Bitmap image)
    {
        if (_isDisposed)
        {
            image.Dispose();
            return;
        }

        _retiredImages.Add(image);
        _retiredImageDisposeTimer.Stop();
        _retiredImageDisposeTimer.Start();
    }

    private void OnRetiredImageDisposeTimerTick(object? sender, EventArgs e)
    {
        _retiredImageDisposeTimer.Stop();
        DisposeRetiredImages();
    }

    private void DisposeRetiredImages()
    {
        foreach (var image in _retiredImages)
        {
            image.Dispose();
        }

        _retiredImages.Clear();
    }

    private sealed class CacheEntry
    {
        public CacheEntry(
            Bitmap image,
            ReaderImageDecodeRequest request,
            long estimatedBytes,
            long lastAccessOrder)
        {
            Image = image;
            Request = request;
            EstimatedBytes = estimatedBytes;
            LastAccessOrder = lastAccessOrder;
        }

        public Bitmap Image { get; }

        public ReaderImageDecodeRequest Request { get; }

        public long EstimatedBytes { get; }

        public long LastAccessOrder { get; private set; }

        public void MarkAccessed(long accessOrder)
        {
            LastAccessOrder = accessOrder;
        }
    }
}
