using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ReaderImageCache : IDisposable
{
    private readonly Dictionary<int, Bitmap> _cache = new();
    private readonly ImagePageLoader _imagePageLoader;

    public ReaderImageCache(ImagePageLoader imagePageLoader)
    {
        _imagePageLoader = imagePageLoader;
    }

    public async Task<Bitmap> GetOrLoadAsync(
        int pageIndex,
        PageEntry page,
        int decodePixelWidth,
        int decodePixelHeight,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(pageIndex, out var cachedImage))
        {
            return cachedImage;
        }

        var image = await _imagePageLoader.LoadAsync(
            page,
            decodePixelWidth,
            decodePixelHeight,
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            image.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

        _cache[pageIndex] = image;
        return image;
    }

    public void TrimTo(IReadOnlySet<int> activeIndexes)
    {
        var staleIndexes = _cache.Keys
            .Where(index => !activeIndexes.Contains(index))
            .ToArray();

        foreach (var index in staleIndexes)
        {
            _cache[index].Dispose();
            _cache.Remove(index);
        }
    }

    public void Clear()
    {
        foreach (var image in _cache.Values)
        {
            image.Dispose();
        }

        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}
