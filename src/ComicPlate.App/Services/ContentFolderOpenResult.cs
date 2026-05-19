using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed record ContentFolderOpenResult(
    string FolderPath,
    IReadOnlyList<ShelfEntry> ContextShelfEntries,
    BookEntry DirectFolderBook,
    IReadOnlyList<PageEntry> DirectPages);
