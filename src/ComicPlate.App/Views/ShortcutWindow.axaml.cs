using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ComicPlate.App.Input;

namespace ComicPlate.App.Views;

public partial class ShortcutWindow : Window
{
    public ShortcutWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        ShortcutScrollViewer.SizeChanged += OnShortcutScrollViewerSizeChanged;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (PlatformShortcuts.IsCloseWindow(e))
        {
            Close();
            e.Handled = true;
        }
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
