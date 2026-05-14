using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ReaderImageCache : IDisposable
{
    private readonly Dictionary<int, CacheEntry> _cache = new();
    private readonly ImagePageLoader _imagePageLoader;

    public ReaderImageCache(ImagePageLoader imagePageLoader)
    {
        _imagePageLoader = imagePageLoader;
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
            oldEntry.Image.Dispose();
        }

        _cache[pageIndex] = new CacheEntry(image, request);
        return image;
    }

    public void TrimTo(IReadOnlySet<int> activeIndexes)
    {
        var staleIndexes = _cache.Keys
            .Where(index => !activeIndexes.Contains(index))
            .ToArray();

        foreach (var index in staleIndexes)
        {
            _cache[index].Image.Dispose();
            _cache.Remove(index);
        }
    }

    public void Clear()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Image.Dispose();
        }

        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }

    private sealed record CacheEntry(Bitmap Image, ReaderImageDecodeRequest Request);
}
