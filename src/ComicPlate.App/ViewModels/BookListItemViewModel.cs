using ComicPlate.Core.Books;

namespace ComicPlate.App.ViewModels;

public sealed class BookListItemViewModel
{
    public BookListItemViewModel(BookEntry book)
    {
        Book = book;
    }

    public BookEntry Book { get; }

    public string DisplayName => Book.DisplayName;

    public string SourceLabel => Book.SourceKind switch
    {
        BookSourceKind.Collection => "Folder",
        BookSourceKind.Zip => "ZIP/CBZ",
        _ => "Comic"
    };
}
