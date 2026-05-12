namespace ComicPlate.Core.Reading;

public sealed record VirtualizedReaderStripSlot(
    int PageIndex,
    bool IsCurrent,
    double StartX,
    double Extent)
{
    public double CenterX => StartX + (Extent / 2);
}
