using ComicPlate.Core.Books;

namespace ComicPlate.Core.Reading;

public sealed record ReaderStripSlot(
    int PageIndex,
    int DisplayIndex,
    PageEntry Page,
    bool IsCurrent);
