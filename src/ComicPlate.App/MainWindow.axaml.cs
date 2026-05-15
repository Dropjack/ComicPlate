using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ComicPlate.App.Input;
using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using ComicPlate.App.Views;

namespace ComicPlate.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly string? _startupPath;
    private SettingsWindow? _settingsWindow;

    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(string? startupPath)
    {
        InitializeComponent();
        var isMacOS = OperatingSystem.IsMacOS();
        Classes.Add(isMacOS ? "mac-shell" : "windows-shell");
        if (isMacOS)
        {
            Grid.SetColumn(ReaderLayoutGrid, 0);
            Grid.SetColumnSpan(ReaderLayoutGrid, 2);
            ReaderLayoutGrid.SetValue(Panel.ZIndexProperty, 0);
            CommandRail.SetValue(Panel.ZIndexProperty, 2);
            ContextShelf.SetValue(Panel.ZIndexProperty, 2);
            ReaderStageGrid.SetValue(Panel.ZIndexProperty, 0);
            ReaderStageGrid.RowDefinitions = new RowDefinitions("0,*");
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
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Right)
        {
            _viewModel.Reader.VisualRightCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            _viewModel.Reader.VisualLeftCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Home)
        {
            _viewModel.Reader.FirstPageCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            _viewModel.Reader.LastPageCommand.Execute(null);
            e.Handled = true;
        }
        else if (PlatformShortcuts.IsOpenContent(e))
        {
            _viewModel.OpenContentCommand.Execute(null);
            e.Handled = true;
        }
        else if (PlatformShortcuts.IsOpenSettings(e))
        {
            ShowSettingsWindow();
            e.Handled = true;
        }
        else if (PlatformShortcuts.IsCloseWindow(e))
        {
            CloseSettingsWindowsAndShowStart();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            _viewModel.ToggleNavigationPaneCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Q)
        {
            _viewModel.Reader.ToggleViewModeCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.R)
        {
            _viewModel.Reader.ToggleReadingDirectionCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F)
        {
            // Reserved for fullscreen. Fullscreen UI behavior will be implemented in its own step.
            e.Handled = true;
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

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow();
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
        }
    }
}
