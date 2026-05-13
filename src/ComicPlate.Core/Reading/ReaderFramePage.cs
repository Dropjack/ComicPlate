using ComicPlate.Core.Books;

namespace ComicPlate.Core.Reading;

public sealed record ReaderFramePage(
    int PageIndex,
    int DisplayIndex,
    PageEntry Page,
    PageImageInfo ImageInfo);
