using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ComicPlate.App.Views;

public partial class ContextShelfView : UserControl
{
    public ContextShelfView()
    {
        InitializeComponent();
        Classes.Add(OperatingSystem.IsMacOS() ? "mac-shell" : "windows-shell");
        AddHandler(PointerPressedEvent, OnShelfPointerPressedTunnel, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnShelfPointerWheelChangedBubble, RoutingStrategies.Bubble);
    }

    private void OnShelfPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            e.Handled = true;
        }
    }

    private void OnShelfPointerWheelChangedBubble(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
    }
}
