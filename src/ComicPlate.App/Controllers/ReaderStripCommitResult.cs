namespace ComicPlate.App.Controllers;

public sealed record ReaderStripCommitResult(
    bool CurrentFrameChanged,
    int TargetFrameStartPageIndex,
    ReaderStripPlacement Placement);
