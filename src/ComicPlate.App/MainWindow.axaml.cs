using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;

namespace ComicPlate.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private bool _isReaderDragging;
    private Point _readerDragStartPoint;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(
            new FolderPickerService(this),
            new ImagePageLoader());
        DataContext = _viewModel;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Focus());
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Right || e.Key == Key.Space)
        {
            if (e.Key == Key.Right)
            {
                _viewModel.VisualRightCommand.Execute(null);
            }
            else
            {
                _viewModel.NextPageCommand.Execute(null);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Left || e.Key == Key.Back)
        {
            if (e.Key == Key.Left)
            {
                _viewModel.VisualLeftCommand.Execute(null);
            }
            else
            {
                _viewModel.PreviousPageCommand.Execute(null);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Home)
        {
            _viewModel.FirstPageCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            _viewModel.LastPageCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.O && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _viewModel.OpenFolderCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnReaderViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _viewModel.SetReaderViewportSize(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnVisualLeftClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.VisualLeftCommand.Execute(null);
        Focus();
        e.Handled = true;
    }

    private void OnVisualRightClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.VisualRightCommand.Execute(null);
        Focus();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y < 0)
        {
            _viewModel.WheelNextReadingGroup();
            e.Handled = true;
        }
        else if (e.Delta.Y > 0)
        {
            _viewModel.WheelPreviousReadingGroup();
            e.Handled = true;
        }
    }

    private void OnReaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control readerSurface)
        {
            return;
        }

        var point = e.GetCurrentPoint(readerSurface);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isReaderDragging = true;
        _readerDragStartPoint = point.Position;
        _viewModel.BeginReaderStripDrag();
        e.Pointer.Capture(readerSurface);
        e.Handled = true;
    }

    private void OnReaderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isReaderDragging || sender is not Control readerSurface)
        {
            return;
        }

        var position = e.GetPosition(readerSurface);
        _viewModel.DragReaderStrip(position.X - _readerDragStartPoint.X);
        e.Handled = true;
    }

    private void OnReaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isReaderDragging || sender is not Control readerSurface)
        {
            return;
        }

        _isReaderDragging = false;
        var position = e.GetPosition(readerSurface);
        _viewModel.EndReaderStripDrag(position.X - _readerDragStartPoint.X);
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnReaderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isReaderDragging)
        {
            return;
        }

        _isReaderDragging = false;
        _viewModel.CancelReaderStripDrag();
    }
}
