namespace ComicPlate.Core.Books;

public enum BookSourceKind
{
    // Navigation-pane entry for a child Collection. It is kept in this enum
    // while Shelf entries still share BookEntry as their transport shape.
    Collection,
    Folder,
    Image,
    Zip,
    Rar
}
