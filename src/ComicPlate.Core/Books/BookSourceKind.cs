namespace ComicPlate.Core.Books;

public enum BookSourceKind
{
    // Compatibility value for persisted NavigationEntry/session data. Shelf
    // rows now use ShelfEntryKind.Collection instead.
    Collection,
    Folder,
    Image,
    Zip,
    Rar,
    Pdf
}
