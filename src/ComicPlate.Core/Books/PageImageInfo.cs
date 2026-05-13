namespace ComicPlate.Core.Books;

public sealed record PageImageInfo(
    int PixelWidth,
    int PixelHeight)
{
    public bool IsValid => PixelWidth > 0 && PixelHeight > 0;

    public double AspectRatio => IsValid
        ? (double)PixelWidth / PixelHeight
        : 1.0;

    public static PageImageInfo Unknown { get; } = new(0, 0);
}
