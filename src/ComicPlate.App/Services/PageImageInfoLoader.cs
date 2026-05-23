using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.App.Services;

public sealed class PageImageInfoLoader
{
    public async Task<IReadOnlyList<PageImageInfo>> LoadAsync(
        IReadOnlyList<PageEntry> pages,
        CancellationToken cancellationToken)
    {
        var infos = new PageImageInfo[pages.Count];

        for (var index = 0; index < pages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            infos[index] = await LoadAsync(pages[index], cancellationToken);
        }

        return infos;
    }

    public async Task<IReadOnlyList<PageImageInfo>> LoadAsync(
        IReadOnlyList<PageEntry> pages,
        IReadOnlyList<PageImageInfo> seedInfos,
        CancellationToken cancellationToken)
    {
        var infos = new PageImageInfo[pages.Count];

        for (var index = 0; index < pages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var seedInfo = index < seedInfos.Count ? seedInfos[index] : PageImageInfo.Unknown;
            infos[index] = seedInfo.IsValid
                ? seedInfo
                : await LoadAsync(pages[index], cancellationToken);
        }

        return infos;
    }

    public async Task LoadAsync(
        IReadOnlyList<PageEntry> pages,
        PageImageInfo[] infos,
        IEnumerable<int> pageIndexes,
        CancellationToken cancellationToken)
    {
        foreach (var pageIndex in pageIndexes.Distinct().Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageIndex < 0 || pageIndex >= pages.Count || infos[pageIndex].IsValid)
            {
                continue;
            }

            infos[pageIndex] = await LoadAsync(pages[pageIndex], cancellationToken);
        }
    }

    public static async Task<PageImageInfo> LoadAsync(PageEntry page, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await page.OpenStreamAsync(cancellationToken);
            return ImageMetadataReader.Read(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return PageImageInfo.Unknown;
        }
    }
}
