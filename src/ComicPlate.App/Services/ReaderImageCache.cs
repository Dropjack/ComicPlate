using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ReaderImageCache : IDisposable
{
    public const long DefaultBudgetBytes = 384L * 1024 * 1024;

    private readonly Dictionary<int, CacheEntry> _cache = new();
    private readonly ImagePageLoader _imagePageLoader;
    private readonly long _budgetBytes;
    private long _accessClock;
    private long _estimatedBytes;

    public ReaderImageCache(ImagePageLoader imagePageLoader, long budgetBytes = DefaultBudgetBytes)
    {
        _imagePageLoader = imagePageLoader;
        _budgetBytes = Math.Max(0, budgetBytes);
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

        var image = await _imagePageLoader.LoadAsync(
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
            oldEntry.Image.Dispose();
        }

        var nextEntry = new CacheEntry(image, request, request.EstimatedBytes, GetNextAccessOrder());
        _cache[pageIndex] = nextEntry;
        _estimatedBytes += nextEntry.EstimatedBytes;
        return image;
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
            entry.Image.Dispose();
        }

        _cache.Clear();
        _estimatedBytes = 0;
    }

    public void Dispose()
    {
        Clear();
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
        entry.Image.Dispose();
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
