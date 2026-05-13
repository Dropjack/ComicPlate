using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ImagePageLoader
{
    public async Task<Bitmap> LoadAsync(
        PageEntry page,
        int targetPixelWidth,
        int targetPixelHeight,
        CancellationToken cancellationToken)
    {
        await using var stream = await page.OpenStreamAsync(cancellationToken);
        var width = Math.Max(1, targetPixelWidth);
        var height = Math.Max(1, targetPixelHeight);

        return width >= height
            ? Bitmap.DecodeToWidth(stream, width)
            : Bitmap.DecodeToHeight(stream, height);
    }
}
