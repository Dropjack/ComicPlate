namespace ComicPlate.Core.Reading;

public sealed record ReaderFrame(
    int FrameIndex,
    ReaderFrameKind Kind,
    IReadOnlyList<ReaderFramePage> Pages,
    bool IsCurrent)
{
    public IReadOnlyList<int> PageIndexes => Pages
        .Select(page => page.PageIndex)
        .ToArray();
}
