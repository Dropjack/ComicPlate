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
        return item.Book.SourceKind switch
        {
            BookSourceKind.Zip => await LoadZipThumbnailAsync(item.Book, cancellationToken),
            BookSourceKind.Folder => await LoadFolderThumbnailAsync(item.Book.Path, cancellationToken),
            BookSourceKind.Collection => await LoadFolderThumbnailAsync(item.Book.Path, cancellationToken),
            _ => null
        };
    }

    private async Task<Bitmap?> LoadZipThumbnailAsync(BookEntry book, CancellationToken cancellationToken)
    {
        var source = new ZipBookSource(book.Path);
        var pages = await source.LoadPagesAsync(cancellationToken);
        var firstPage = pages.FirstOrDefault();

        return firstPage is null
            ? null
            : await LoadPageThumbnailAsync($"zip:{book.Path}:{firstPage.LogicalPath}", firstPage, cancellationToken);
    }

    private async Task<Bitmap?> LoadFolderThumbnailAsync(string folderPath, CancellationToken cancellationToken)
    {
        var source = new FolderBookSource(folderPath, recursive: false);
        var pages = await source.LoadPagesAsync(cancellationToken);
        var firstPage = pages.FirstOrDefault();

        return firstPage is null
            ? null
            : await LoadPageThumbnailAsync($"folder:{folderPath}:{firstPage.LogicalPath}", firstPage, cancellationToken);
    }

    private async Task<Bitmap> LoadPageThumbnailAsync(string cacheKey, PageEntry page, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        await using var stream = await page.OpenStreamAsync(cancellationToken);
        var thumbnail = Bitmap.DecodeToWidth(stream, DecodeWidth);
        _cache[cacheKey] = thumbnail;
        return thumbnail;
    }
}
