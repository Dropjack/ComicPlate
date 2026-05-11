using Avalonia.Media.Imaging;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderStripItemViewModel : ViewModelBase
{
    private Bitmap? _image;
    private string _statusMessage = "";

    public ReaderStripItemViewModel(ReaderStripSlot slot)
    {
        Slot = slot;
    }

    public ReaderStripSlot Slot { get; }

    public int PageIndex => Slot.PageIndex;

    public string PageLabel => Slot.DisplayIndex.ToString();

    public bool IsCurrent => Slot.IsCurrent;

    public double SlotWidth => IsCurrent ? 560 : 420;

    public double SlotOpacity => IsCurrent ? 1.0 : 0.72;

    public Bitmap? Image
    {
        get => _image;
        set
        {
            if (SetProperty(ref _image, value))
            {
                OnPropertyChanged(nameof(HasImage));
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasImage => Image is not null;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasMessage => !HasImage && !string.IsNullOrWhiteSpace(StatusMessage);
}
