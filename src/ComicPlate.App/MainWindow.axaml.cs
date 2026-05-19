using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ComicPlate.App.Input;
using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using ComicPlate.App.Views;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App;

public partial class MainWindow : Window
{
    public static readonly StyledProperty<bool> CanCreateNewWindowProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(CanCreateNewWindow), defaultValue: true);

    private readonly MainWindowViewModel _viewModel;
    private readonly SettingsService _settingsService;
    private readonly IReaderWindowService _readerWindowService;
    private AppSettings _appSettings;
    private readonly string? _startupPath;
    private SettingsWindow? _settingsWindow;
    private WindowState _windowStateBeforeFullscreen = WindowState.Normal;
    private bool _isFullscreen;

    public MainWindow()
        : this(null, null)
    {
    }

    public MainWindow(string? startupPath)
        : this(startupPath, null)
    {
    }

    public MainWindow(string? startupPath, SettingsService? settingsService)
        : this(startupPath, settingsService, null)
    {
    }

    public MainWindow(
        string? startupPath,
        SettingsService? settingsService,
        IReaderWindowService? readerWindowService)
    {
        InitializeComponent();
        _settingsService = settingsService ?? SettingsService.CreateDefault();
        _readerWindowService = readerWindowService ?? new ReaderWindowService(_settingsService);
        _appSettings = _settingsService.Load();
        CanCreateNewWindow = _appSettings.AllowMultipleWindows;
        if (_appSettings.RestoreWindowPlacement)
        {
            ApplyWindowPlacement(_appSettings.MainWindow);
        }

        var isMacOS = OperatingSystem.IsMacOS();
        Classes.Add(isMacOS ? "mac-shell" : "windows-shell");
        if (isMacOS)
        {
            MainShell.ColumnDefinitions = new ColumnDefinitions("Auto,*");
            ReaderLayoutGrid.ColumnDefinitions = new ColumnDefinitions("*");
            Grid.SetColumn(ReaderStageGrid, 0);
            WindowsShelfHost.IsVisible = false;
            ReaderStageGrid.RowDefinitions = new RowDefinitions("0,*");
        }
        else
        {
            MacShelfDivider.IsVisible = false;
            MacContextShelf.IsVisible = false;
        }
        _startupPath = startupPath;
        _viewModel = new MainWindowViewModel(
            new FolderPickerService(this),
            new ImagePageLoader());
        DataContext = _viewModel;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        Closed += OnClosed;
    }

    public bool CanCreateNewWindow
    {
        get => GetValue(CanCreateNewWindowProperty);
        private set => SetValue(CanCreateNewWindowProperty, value);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Focus());

        if (!string.IsNullOrWhiteSpace(_startupPath))
        {
            Dispatcher.UIThread.Post(async () => await _viewModel.OpenStartupPathAsync(_startupPath));
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _settingsWindow?.Close();
        _settingsWindow = null;
        _viewModel.SaveCurrentState();
        _viewModel.Dispose();
        _appSettings = _settingsService.Load();
        _settingsService.Save(_appSettings with { MainWindow = CaptureWindowPlacement() });
    }

    private void ApplyWindowPlacement(WindowPlacementSettings placement)
    {
        Width = Math.Max(MinWidth, placement.Width);
        Height = Math.Max(MinHeight, placement.Height);

        if (TryGetValidPosition(placement, Width, Height, out var position))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = position;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private WindowPlacementSettings CaptureWindowPlacement()
    {
        return new WindowPlacementSettings
        {
            Width = Width,
            Height = Height,
            X = Position.X,
            Y = Position.Y,
        };
    }

    private bool TryGetValidPosition(
        WindowPlacementSettings placement,
        double width,
        double height,
        out PixelPoint position)
    {
        position = default;

        if (!placement.HasPosition)
        {
            return false;
        }

        var x = placement.X!.Value;
        var y = placement.Y!.Value;
        var windowWidth = Math.Max(1, (int)Math.Ceiling(width));
        var windowHeight = Math.Max(1, (int)Math.Ceiling(height));

        foreach (var screen in Screens.All)
        {
            var area = screen.WorkingArea;
            var hasVisibleTopLeft =
                x >= area.X
                && y >= area.Y
                && x < area.X + area.Width - Math.Min(80, windowWidth)
                && y < area.Y + area.Height - Math.Min(80, windowHeight);

            if (hasVisibleTopLeft)
            {
                position = new PixelPoint(x, y);
                return true;
            }
        }

        return false;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isFullscreen && e.Key == Key.Escape)
        {
            ExitFullscreen();
            e.Handled = true;
            return;
        }

        var canMoveNavigationSelection = !_isFullscreen || ReaderSurface.IsFullscreenShelfOverlayVisible;
        if (e.KeyModifiers == KeyModifiers.None
            && e.Key is Key.Up or Key.Down
            && canMoveNavigationSelection
            && _viewModel.MoveNavigationSelection(
                e.Key == Key.Down ? 1 : -1,
                allowHiddenNavigationPane: _isFullscreen))
        {
            e.Handled = true;
            return;
        }

        switch (ShortcutRegistry.GetAction(e))
        {
            case ShortcutActionId.NextPage:
                _viewModel.Reader.VisualRightCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.PreviousPage:
                _viewModel.Reader.VisualLeftCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.FirstPage:
                _viewModel.Reader.FirstPageCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.LastPage:
                _viewModel.Reader.LastPageCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.OpenContent:
                _viewModel.OpenContentCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.NewWindow:
                TryShowNewWindow();
                e.Handled = true;
                break;
            case ShortcutActionId.OpenSettings:
                ShowSettingsWindow();
                e.Handled = true;
                break;
            case ShortcutActionId.CloseWindow:
                CloseSettingsWindowsAndShowStart();
                e.Handled = true;
                break;
            case ShortcutActionId.ToggleNavigationPane:
                if (_isFullscreen)
                {
                    ReaderSurface.ToggleFullscreenShelfOverlay();
                }
                else
                {
                    _viewModel.ToggleNavigationPaneCommand.Execute(null);
                }

                e.Handled = true;
                break;
            case ShortcutActionId.ToggleViewMode:
                _viewModel.Reader.ToggleViewModeCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.ToggleReadingDirection:
                _viewModel.Reader.ToggleReadingDirectionCommand.Execute(null);
                e.Handled = true;
                break;
            case ShortcutActionId.ToggleFullscreen:
                ToggleFullscreen();
                e.Handled = true;
                break;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (OperatingSystem.IsMacOS() && Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y))
        {
            if (e.Delta.X > 0)
            {
                _viewModel.Reader.VisualLeftCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Delta.X < 0)
            {
                _viewModel.Reader.VisualRightCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Delta.Y < 0)
        {
            _viewModel.Reader.WheelNextReadingGroup();
            e.Handled = true;
        }
        else if (e.Delta.Y > 0)
        {
            _viewModel.Reader.WheelPreviousReadingGroup();
            e.Handled = true;
        }
    }

    private void OnSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ShowSettingsWindow();
        e.Handled = true;
    }

    private void OnNewWindowClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryShowNewWindow();
        e.Handled = true;
    }

    private void TryShowNewWindow()
    {
        RefreshSettings();
        if (!CanCreateNewWindow)
        {
            return;
        }

        _readerWindowService.ShowEmptyWindow();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(new PlatformLauncher(), _settingsService);
        _settingsWindow.Closed += OnSettingsWindowClosed;
        _settingsWindow.Show();
    }

    private void CloseSettingsWindowsAndShowStart()
    {
        _settingsWindow?.Close();
        _settingsWindow = null;
        _viewModel.ShowStartCommand.Execute(null);
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= OnSettingsWindowClosed;
            _settingsWindow = null;
            RefreshSettings();
        }
    }

    private void RefreshSettings()
    {
        _appSettings = _settingsService.Load();
        CanCreateNewWindow = _appSettings.AllowMultipleWindows;
    }

    private void ToggleFullscreen()
    {
        if (_isFullscreen)
        {
            ExitFullscreen();
            return;
        }

        EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        if (_isFullscreen)
        {
            return;
        }

        _isFullscreen = true;
        _windowStateBeforeFullscreen = WindowState;
        WindowState = WindowState.FullScreen;
        ApplyFullscreenChrome();
    }

    private void ExitFullscreen()
    {
        if (!_isFullscreen)
        {
            return;
        }

        _isFullscreen = false;
        WindowState = _windowStateBeforeFullscreen == WindowState.FullScreen
            ? WindowState.Normal
            : _windowStateBeforeFullscreen;
        ApplyFullscreenChrome();
    }

    private void ApplyFullscreenChrome()
    {
        var isMacOS = OperatingSystem.IsMacOS();

        LeftFloatingPanel.IsVisible = !_isFullscreen;
        MainShell.ColumnDefinitions = _isFullscreen
            ? new ColumnDefinitions("0,*")
            : isMacOS
                ? new ColumnDefinitions("Auto,*")
                : new ColumnDefinitions("64,*");
        ReaderLayoutGrid.ColumnDefinitions = _isFullscreen
            ? isMacOS
                ? new ColumnDefinitions("*")
                : new ColumnDefinitions("0,*")
            : isMacOS
                ? new ColumnDefinitions("*")
                : new ColumnDefinitions("Auto,*");
        ReaderStageGrid.RowDefinitions = _isFullscreen
            ? new RowDefinitions("0,*")
            : isMacOS
                ? new RowDefinitions("0,*")
                : new RowDefinitions("40,*");
        Grid.SetColumn(ReaderStageGrid, isMacOS ? 0 : 1);
        WindowsShelfHost.IsVisible = !_isFullscreen && !isMacOS;
        ReaderSurface.SetFullscreenChromeHidden(_isFullscreen);
    }
}
