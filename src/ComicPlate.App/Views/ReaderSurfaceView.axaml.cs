using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComicPlate.App.ViewModels;

namespace ComicPlate.App.Views;

public partial class ReaderSurfaceView : UserControl
{
    private const double FullscreenShelfHotZoneWidth = 80;
    private const double FullscreenBottomHotZoneHeight = 80;
    private static readonly TimeSpan FullscreenOverlayHideDelay = TimeSpan.FromSeconds(3);

    private bool _isReaderDragging;
    private bool _isProgressDragging;
    private Point _readerDragStartPoint;
    private bool _isFullscreenChromeHidden;
    private readonly DispatcherTimer _fullscreenOverlayHideTimer;

    public ReaderSurfaceView()
    {
        InitializeComponent();
        Classes.Add(OperatingSystem.IsMacOS() ? "mac-shell" : "windows-shell");
        _fullscreenOverlayHideTimer = new DispatcherTimer
        {
            Interval = FullscreenOverlayHideDelay,
        };
        _fullscreenOverlayHideTimer.Tick += OnFullscreenOverlayHideTimerTick;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public void SetFullscreenChromeHidden(bool isHidden)
    {
        if (_isFullscreenChromeHidden == isHidden)
        {
            return;
        }

        _isFullscreenChromeHidden = isHidden;
        ReaderSurfaceRoot.RowDefinitions = isHidden
            ? new RowDefinitions("*,0,0")
            : new RowDefinitions("*,34,34");
        BottomBubble.IsVisible = !isHidden && OperatingSystem.IsMacOS();
        BottomButtonRow.IsVisible = !isHidden;
        BottomProgressRow.IsVisible = !isHidden;
        FullscreenBottomOverlay.IsVisible = false;
        FullscreenShelfOverlay.IsVisible = false;
        _fullscreenOverlayHideTimer.Stop();
    }

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

        if (IsPointerFromFullscreenOverlay(e.Source))
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
        if (sender is not Control readerSurface)
        {
            return;
        }

        if (!_isReaderDragging)
        {
            var position = e.GetPosition(readerSurface);
            UpdateFullscreenBottomOverlay(readerSurface, position);
            UpdateFullscreenShelfOverlay(position);
            return;
        }

        var dragPosition = e.GetPosition(readerSurface);
        ViewModel?.Reader.DragReaderStrip(dragPosition.X - _readerDragStartPoint.X);
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
        FullscreenBottomOverlay.IsVisible = ShouldUseFullscreenBottomOverlay();
        ResetFullscreenOverlayHideTimer();
        CommitProgressNavigation(progressTrack, point.Position);
        e.Pointer.Capture(progressTrack);
        e.Handled = true;
    }

    private void OnProgressPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isProgressDragging || sender is not Control progressTrack)
        {
            return;
        }

        CommitProgressNavigation(progressTrack, e.GetPosition(progressTrack));
        ResetFullscreenOverlayHideTimer();
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
        ResetFullscreenOverlayHideTimer();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnProgressPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isProgressDragging = false;
        ResetFullscreenOverlayHideTimer();
    }

    private void OnFullscreenOverlayHideTimerTick(object? sender, EventArgs e)
    {
        _fullscreenOverlayHideTimer.Stop();
        if (_isProgressDragging)
        {
            ResetFullscreenOverlayHideTimer();
            return;
        }

        FullscreenBottomOverlay.IsVisible = false;
        FullscreenShelfOverlay.IsVisible = false;
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

    private void UpdateFullscreenBottomOverlay(Control readerSurface, Point position)
    {
        if (!ShouldUseFullscreenBottomOverlay())
        {
            FullscreenBottomOverlay.IsVisible = false;
            return;
        }

        if (_isProgressDragging)
        {
            return;
        }

        if (readerSurface.Bounds.Height - position.Y <= FullscreenBottomHotZoneHeight)
        {
            FullscreenBottomOverlay.IsVisible = true;
            ResetFullscreenOverlayHideTimer();
        }
    }

    private void UpdateFullscreenShelfOverlay(Point position)
    {
        if (!ShouldUseFullscreenShelfOverlay())
        {
            FullscreenShelfOverlay.IsVisible = false;
            return;
        }

        if (_isProgressDragging)
        {
            return;
        }

        if (position.X <= FullscreenShelfHotZoneWidth)
        {
            FullscreenShelfOverlay.IsVisible = true;
            ResetFullscreenOverlayHideTimer();
        }
    }

    private bool ShouldUseFullscreenBottomOverlay()
    {
        return _isFullscreenChromeHidden && !OperatingSystem.IsMacOS();
    }

    private bool ShouldUseFullscreenShelfOverlay()
    {
        return ShouldUseFullscreenBottomOverlay();
    }

    private void ResetFullscreenOverlayHideTimer()
    {
        if (!ShouldUseFullscreenBottomOverlay())
        {
            _fullscreenOverlayHideTimer.Stop();
            return;
        }

        _fullscreenOverlayHideTimer.Stop();
        _fullscreenOverlayHideTimer.Start();
    }

    private bool IsPointerFromFullscreenOverlay(object? source)
    {
        if (source is not Visual visual)
        {
            return false;
        }

        return FullscreenBottomOverlay.IsVisualAncestorOf(visual)
            || FullscreenShelfOverlay.IsVisualAncestorOf(visual);
    }
}
