using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class ImagePageLoader
{
    public async Task<Bitmap> LoadAsync(PageEntry page, CancellationToken cancellationToken)
    {
        await using var stream = await page.OpenStreamAsync(cancellationToken);
        return new Bitmap(stream);
    }
}
