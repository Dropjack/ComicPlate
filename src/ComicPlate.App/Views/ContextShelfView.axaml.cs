using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComicPlate.App.ViewModels;

namespace ComicPlate.App.Views;

public partial class ContextShelfView : UserControl
{
    private MainWindowViewModel? _viewModel;
    private int _lastLocateRequestVersion;

    public ContextShelfView()
    {
        InitializeComponent();
        Classes.Add(OperatingSystem.IsMacOS() ? "mac-shell" : "windows-shell");
        AddHandler(PointerPressedEvent, OnShelfPointerPressedTunnel, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnShelfPointerWheelChangedBubble, RoutingStrategies.Bubble);
        DataContextChanged += OnDataContextChanged;
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

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ContextShelf.PropertyChanged -= OnContextShelfPropertyChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _lastLocateRequestVersion = _viewModel.ContextShelf.LocateRequestVersion;
            _viewModel.ContextShelf.PropertyChanged += OnContextShelfPropertyChanged;
        }
    }

    private void OnContextShelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ContextShelfViewModel.LocateRequestVersion) || _viewModel is null)
        {
            return;
        }

        var requestVersion = _viewModel.ContextShelf.LocateRequestVersion;
        if (requestVersion == _lastLocateRequestVersion)
        {
            return;
        }

        _lastLocateRequestVersion = requestVersion;
        Dispatcher.UIThread.Post(ScrollSelectedItemToTop);
    }

    private void ScrollSelectedItemToTop()
    {
        if (ShelfList.SelectedIndex < 0)
        {
            return;
        }

        var scrollViewer = ShelfList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scrollViewer is null)
        {
            if (ShelfList.SelectedItem is not null)
            {
                ShelfList.ScrollIntoView(ShelfList.SelectedItem);
            }

            return;
        }

        const double estimatedItemHeight = 70;
        var offsetY = Math.Min(
            ShelfList.SelectedIndex * estimatedItemHeight,
            scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, offsetY));
    }
}
