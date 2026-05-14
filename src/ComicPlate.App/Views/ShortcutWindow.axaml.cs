using Avalonia;
using Avalonia.Controls;

namespace ComicPlate.App.Views;

public partial class ShortcutWindow : Window
{
    public ShortcutWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        ShortcutScrollViewer.SizeChanged += OnShortcutScrollViewerSizeChanged;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        UpdateBottomScrollSpacer();
    }

    private void OnShortcutScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateBottomScrollSpacer();
    }

    private void UpdateBottomScrollSpacer()
    {
        BottomScrollSpacer.Height = Math.Max(0, ShortcutScrollViewer.Bounds.Height - 140);
    }
}
