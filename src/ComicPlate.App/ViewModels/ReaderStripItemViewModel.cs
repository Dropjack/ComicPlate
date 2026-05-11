using Avalonia.Media.Imaging;
using ComicPlate.Core.Reading;

namespace ComicPlate.App.ViewModels;

public sealed class ReaderStripItemViewModel : ViewModelBase
{
    private const double HorizontalPadding = 24;
    private const double VerticalPadding = 24;
    private const double MinimumDisplaySize = 160;

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
                RecalculateDisplaySize();
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
        RecalculateDisplaySize();
    }

    private void RecalculateDisplaySize()
    {
        var availableWidth = Math.Max(MinimumDisplaySize, _viewportWidth - HorizontalPadding);
        var availableHeight = Math.Max(MinimumDisplaySize, _viewportHeight - VerticalPadding);

        if (Image is null)
        {
            DisplayWidth = Math.Min(availableWidth, 320);
            DisplayHeight = Math.Min(availableHeight, 420);
            return;
        }

        var size = PageDisplaySizeCalculator.Calculate(
            Image.PixelSize.Width,
            Image.PixelSize.Height,
            availableWidth,
            availableHeight,
            FitMode.AutoFit);

        DisplayWidth = size.Width;
        DisplayHeight = size.Height;
    }
}
