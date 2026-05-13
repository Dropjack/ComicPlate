using Avalonia.Media.Imaging;
using ComicPlate.Core.Books;

namespace ComicPlate.App.ViewModels;

public sealed class ContentListItemViewModel : ViewModelBase
{
    private Bitmap? _thumbnail;
    private string _thumbnailStatus = "";

    private ContentListItemViewModel(
        ContentListItemKind kind,
        string displayName,
        string detail,
        BookEntry book)
    {
        Kind = kind;
        DisplayName = displayName;
        Detail = detail;
        Book = book;
    }

    public ContentListItemKind Kind { get; }

    public string DisplayName { get; }

    public string Detail { get; }

    public BookEntry Book { get; }

    public bool HasThumbnail => Thumbnail is not null;

    public string KindLabel => Kind switch
    {
        ContentListItemKind.Archive => "ZIP",
        ContentListItemKind.Folder => "Folder",
        _ => "Folder"
    };

    public bool HasKindOverlay => true;

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (SetProperty(ref _thumbnail, value))
            {
                OnPropertyChanged(nameof(HasThumbnail));
                OnPropertyChanged(nameof(HasPlaceholder));
            }
        }
    }

    public string ThumbnailStatus
    {
        get => _thumbnailStatus;
        set
        {
            if (SetProperty(ref _thumbnailStatus, value))
            {
                OnPropertyChanged(nameof(HasPlaceholder));
            }
        }
    }

    public bool HasPlaceholder => Thumbnail is null;

    public static ContentListItemViewModel FromBook(BookEntry book)
    {
        var kind = book.SourceKind == BookSourceKind.Zip
            ? ContentListItemKind.Archive
            : ContentListItemKind.Folder;
        var detail = book.SourceKind switch
        {
            BookSourceKind.Collection => "Folder",
            BookSourceKind.Image => "Image",
            BookSourceKind.Zip => "ZIP/CBZ",
            _ => "Comic folder"
        };

        return new ContentListItemViewModel(kind, book.DisplayName, detail, book);
    }
}
