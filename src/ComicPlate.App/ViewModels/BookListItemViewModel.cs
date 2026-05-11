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

    public string SourceLabel => Book.SourceKind == BookSourceKind.Zip ? "ZIP/CBZ" : "Folder";
}
