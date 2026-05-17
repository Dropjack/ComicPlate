using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ImagePageLoader
{
    public Task<Bitmap> LoadAsync(
        PageEntry page,
        ReaderImageDecodeRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            async () =>
            {
                await using var stream = await page.OpenStreamAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var width = Math.Max(1, request.PixelWidth);
                var height = Math.Max(1, request.PixelHeight);

                return width >= height
                    ? Bitmap.DecodeToWidth(stream, width)
                    : Bitmap.DecodeToHeight(stream, height);
            },
            cancellationToken);
    }
}
