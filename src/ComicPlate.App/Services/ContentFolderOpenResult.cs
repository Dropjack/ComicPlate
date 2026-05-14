using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed record ContentFolderOpenResult(
    string FolderPath,
    IReadOnlyList<BookEntry> ShelfEntries,
    BookEntry DirectFolderBook,
    IReadOnlyList<PageEntry> DirectPages);
