using Avalonia.Media.Imaging;
using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.App.Services;

public sealed class SidebarThumbnailLoader : IDisposable
{
    private const int DecodeWidth = 96;
    private const int MaxLoadedThumbnails = 96;

    private readonly Dictionary<string, Bitmap> _cache = new();
    private readonly ThumbnailCacheService _diskCache;

    public SidebarThumbnailLoader()
        : this(new ThumbnailCacheService(AppDataService.CreateDefault().UserDataDirectory))
    {
    }

    public SidebarThumbnailLoader(ThumbnailCacheService diskCache)
    {
        _diskCache = diskCache;
    }

    public async Task LoadInitialThumbnailsAsync(
        IReadOnlyList<ContentListItemViewModel> items,
        CancellationToken cancellationToken)
    {
        foreach (var item in items.Take(MaxLoadedThumbnails))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                item.Thumbnail = await LoadThumbnailAsync(item, cancellationToken);
            }
            catch (Exception)
            {
                item.ThumbnailStatus = "No preview";
            }
        }
    }

    public void Clear()
    {
        foreach (var bitmap in _cache.Values)
        {
            bitmap.Dispose();
        }

        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }

    private async Task<Bitmap?> LoadThumbnailAsync(ContentListItemViewModel item, CancellationToken cancellationToken)
    {
        return item.Entry.Kind == ShelfEntryKind.Collection
            ? await LoadFolderThumbnailAsync(item.Entry.Path, cancellationToken)
            : item.Entry.BookSourceKind switch
        {
            BookSourceKind.Zip => await LoadArchiveThumbnailAsync(item.Entry.ToBookEntry(), cancellationToken),
            BookSourceKind.Rar => await LoadArchiveThumbnailAsync(item.Entry.ToBookEntry(), cancellationToken),
            BookSourceKind.Folder => await LoadFolderThumbnailAsync(item.Entry.Path, cancellationToken),
            _ => null
        };
    }

    private async Task<Bitmap?> LoadArchiveThumbnailAsync(BookEntry book, CancellationToken cancellationToken)
    {
        var source = CreateArchiveSource(book);
        var pages = await source.LoadPagesAsync(cancellationToken);
        var firstPage = pages.FirstOrDefault();

        return firstPage is null
            ? null
            : await LoadPageThumbnailAsync(
                $"archive:{book.SourceKind}:{book.Path}:{GetLastWriteTicks(book.Path)}:{firstPage.LogicalPath}",
                firstPage,
                cancellationToken);
    }

    private static IBookSource CreateArchiveSource(BookEntry book)
    {
        return book.SourceKind switch
        {
            BookSourceKind.Zip => new ZipBookSource(book.Path),
            BookSourceKind.Rar => new RarBookSource(book.Path),
            _ => throw new InvalidOperationException($"Unsupported archive source kind: {book.SourceKind}.")
        };
    }

    private async Task<Bitmap?> LoadFolderThumbnailAsync(string folderPath, CancellationToken cancellationToken)
    {
        var source = new FolderBookSource(folderPath, recursive: false);
        var pages = await source.LoadPagesAsync(cancellationToken);
        var firstPage = pages.FirstOrDefault();

        return firstPage is null
            ? null
            : await LoadPageThumbnailAsync(
                $"folder:{folderPath}:{firstPage.LogicalPath}:{GetLastWriteTicks(Path.Combine(folderPath, firstPage.LogicalPath))}",
                firstPage,
                cancellationToken);
    }

    private async Task<Bitmap> LoadPageThumbnailAsync(string cacheKey, PageEntry page, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var diskCached = _diskCache.TryLoad(cacheKey);
        if (diskCached is not null)
        {
            _cache[cacheKey] = diskCached;
            return diskCached;
        }

        await using var stream = await page.OpenStreamAsync(cancellationToken);
        var thumbnail = Bitmap.DecodeToWidth(stream, DecodeWidth);
        _cache[cacheKey] = thumbnail;
        _diskCache.Save(cacheKey, thumbnail);
        return thumbnail;
    }

    private static long GetLastWriteTicks(string path)
    {
        try
        {
            return File.Exists(path)
                ? File.GetLastWriteTimeUtc(path).Ticks
                : 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
