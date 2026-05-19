namespace ComicPlate.Core.Books;

public sealed record ContextShelf(
    string RootPath,
    // Entries can be readable Books or child Collections. The current UI still
    // carries them as BookEntry while Book/Collection models are being split.
    IReadOnlyList<BookEntry> Entries);
