using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ComicPlate.App.Views;

public partial class ContextShelfView : UserControl
{
    public ContextShelfView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnShelfPointerPressedTunnel, RoutingStrategies.Tunnel);
    }

    private void OnShelfPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            e.Handled = true;
        }
    }
}
