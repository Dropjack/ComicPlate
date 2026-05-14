using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed record ReaderImageDecodeRequest(
    int PixelWidth,
    int PixelHeight)
{
    public const double DefaultScale = 1.5;
    public const int MaximumDecodeDimension = 4096;
    private const double ReuseOversizeLimit = 1.75;

    public long EstimatedBytes => (long)PixelWidth * PixelHeight * 4;

    public bool CanReuseFor(ReaderImageDecodeRequest requested)
    {
        if (PixelWidth < requested.PixelWidth || PixelHeight < requested.PixelHeight)
        {
            return false;
        }

        return PixelWidth <= requested.PixelWidth * ReuseOversizeLimit
            && PixelHeight <= requested.PixelHeight * ReuseOversizeLimit;
    }

    public static ReaderImageDecodeRequest Create(
        double displayWidth,
        double displayHeight,
        PageImageInfo sourceInfo,
        double scale = DefaultScale)
    {
        var width = Math.Max(1, (int)Math.Ceiling(displayWidth * scale));
        var height = Math.Max(1, (int)Math.Ceiling(displayHeight * scale));

        if (sourceInfo.IsValid)
        {
            width = Math.Min(width, sourceInfo.PixelWidth);
            height = Math.Min(height, sourceInfo.PixelHeight);
        }

        width = Math.Min(width, MaximumDecodeDimension);
        height = Math.Min(height, MaximumDecodeDimension);

        return new ReaderImageDecodeRequest(width, height);
    }
}
