using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ComicPlate.App.ViewModels;

namespace ComicPlate.App.Views;

public partial class ReaderSurfaceView : UserControl
{
    private bool _isReaderDragging;
    private bool _isProgressDragging;
    private Point _readerDragStartPoint;

    public ReaderSurfaceView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnReaderViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ViewModel?.Reader.SetReaderViewportSize(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnVisualLeftClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Reader.VisualLeftCommand.Execute(null);
        Focus();
        e.Handled = true;
    }

    private void OnVisualRightClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Reader.VisualRightCommand.Execute(null);
        Focus();
        e.Handled = true;
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
        ViewModel?.Reader.BeginReaderStripDrag();
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
        ViewModel?.Reader.DragReaderStrip(position.X - _readerDragStartPoint.X);
        e.Handled = true;
    }

    private void OnReaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isReaderDragging || sender is not Control readerSurface)
        {
            return;
        }

        _isReaderDragging = false;
        ViewModel?.Reader.EndReaderStripDrag();
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
        ViewModel?.Reader.CancelReaderStripDrag();
    }

    private void OnProgressPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control progressTrack)
        {
            return;
        }

        var point = e.GetCurrentPoint(progressTrack);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isProgressDragging = true;
        e.Pointer.Capture(progressTrack);
        e.Handled = true;
    }

    private void OnProgressPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isProgressDragging || sender is not Control progressTrack)
        {
            return;
        }

        _isProgressDragging = false;
        CommitProgressNavigation(progressTrack, e.GetPosition(progressTrack));
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnProgressPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isProgressDragging = false;
    }

    private void CommitProgressNavigation(Control progressTrack, Point position)
    {
        var width = progressTrack.Bounds.Width;
        if (width <= 0)
        {
            return;
        }

        ViewModel?.Reader.GoToProgressRatio(position.X / width);
        Focus();
    }
}
