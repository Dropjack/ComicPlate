namespace ComicPlate.Core.Books;

public sealed record ShelfEntry(
    string Id,
    string DisplayName,
    ShelfEntryKind Kind,
    string Path,
    BookSourceKind? BookSourceKind = null)
{
    public static ShelfEntry FromBook(BookEntry book)
    {
        return new ShelfEntry(
            book.Id,
            book.DisplayName,
            ShelfEntryKind.Book,
            book.Path,
            book.SourceKind);
    }

    public BookEntry ToBookEntry()
    {
        if (Kind != ShelfEntryKind.Book || BookSourceKind is null)
        {
            throw new InvalidOperationException("Only book shelf entries can be converted to BookEntry.");
        }

        return new BookEntry(Id, DisplayName, BookSourceKind.Value, Path);
    }
}
