using Avalonia.Media.Imaging;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;

namespace ComicPlate.App.ViewModels;

public sealed class ContentListItemViewModel : ViewModelBase
{
    private Bitmap? _thumbnail;
    private string _thumbnailStatus = "";
    private bool _isReading;
    private bool _isNavigationCurrent;
    private bool _isOpened;

    private ContentListItemViewModel(
        ContentListItemKind kind,
        string displayName,
        string detail,
        ShelfEntry entry)
    {
        Kind = kind;
        DisplayName = displayName;
        Detail = detail;
        Entry = entry;
    }

    public ContentListItemKind Kind { get; }

    public string DisplayName { get; }

    public string Detail { get; }

    public ShelfEntry Entry { get; }

    public bool HasThumbnail => Thumbnail is not null;

    public string KindLabel => Kind switch
    {
        ContentListItemKind.Archive => LocalizationService.Current.GetString("Shelf.Kind.Archive"),
        ContentListItemKind.Folder => LocalizationService.Current.GetString("Shelf.Kind.Folder"),
        _ => LocalizationService.Current.GetString("Shelf.Kind.Folder")
    };

    public bool HasKindOverlay => true;

    public bool IsReading
    {
        get => _isReading;
        set => SetProperty(ref _isReading, value);
    }

    public bool IsNavigationCurrent
    {
        get => _isNavigationCurrent;
        set => SetProperty(ref _isNavigationCurrent, value);
    }

    public bool IsOpened
    {
        get => _isOpened;
        set => SetProperty(ref _isOpened, value);
    }

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

    public static ContentListItemViewModel FromShelfEntry(ShelfEntry entry)
    {
        var kind = entry.BookSourceKind is BookSourceKind.Zip or BookSourceKind.Rar or BookSourceKind.Pdf or BookSourceKind.Epub
            ? ContentListItemKind.Archive
            : ContentListItemKind.Folder;
        var detail = entry.Kind == ShelfEntryKind.Collection
            ? LocalizationService.Current.GetString("Shelf.Kind.Folder")
            : entry.BookSourceKind switch
        {
            BookSourceKind.Image => LocalizationService.Current.GetString("Shelf.Kind.Image"),
            BookSourceKind.Zip => LocalizationService.Current.GetString("Shelf.Kind.ZipCbz"),
            BookSourceKind.Rar => LocalizationService.Current.GetString("Shelf.Kind.RarCbr"),
            BookSourceKind.Pdf => LocalizationService.Current.GetString("Shelf.Kind.Pdf"),
            BookSourceKind.Epub => LocalizationService.Current.GetString("Shelf.Kind.Epub"),
            _ => LocalizationService.Current.GetString("Shelf.Kind.ComicFolder")
        };

        return new ContentListItemViewModel(kind, entry.DisplayName, detail, entry);
    }
}
