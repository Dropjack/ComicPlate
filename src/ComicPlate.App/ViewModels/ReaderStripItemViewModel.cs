using Avalonia.Media.Imaging;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderStripItemViewModel : ViewModelBase
{
    private const double DecodeScale = 2.0;

    private double _displayHeight = 320;
    private double _displayWidth = 240;
    private Bitmap? _image;
    private string _statusMessage = "";
    private double _viewportHeight = 600;
    private double _viewportWidth = 800;

    public ReaderStripItemViewModel(ReaderStripSlot slot)
    {
        Slot = slot;
    }

    public ReaderStripSlot Slot { get; }

    public int PageIndex => Slot.PageIndex;

    public string PageLabel => Slot.DisplayIndex.ToString();

    public bool IsCurrent => Slot.IsCurrent;

    public int DecodePixelWidth => Math.Max(1, (int)Math.Ceiling(DisplayWidth * DecodeScale));

    public int DecodePixelHeight => Math.Max(1, (int)Math.Ceiling(DisplayHeight * DecodeScale));

    public double DisplayWidth
    {
        get => _displayWidth;
        private set => SetProperty(ref _displayWidth, value);
    }

    public double DisplayHeight
    {
        get => _displayHeight;
        private set => SetProperty(ref _displayHeight, value);
    }

    public double SlotOpacity => IsCurrent ? 1.0 : 0.92;

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

    public void SetViewportSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _viewportWidth = width;
        _viewportHeight = height;
    }

    public void SetDisplaySize(double width, double height)
    {
        DisplayWidth = Math.Max(1, width);
        DisplayHeight = Math.Max(1, height);
        OnPropertyChanged(nameof(DecodePixelWidth));
        OnPropertyChanged(nameof(DecodePixelHeight));
    }
}
