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
    private readonly string? _startupPath;

    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(string? startupPath)
    {
        InitializeComponent();
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
        _viewModel.SaveCurrentState();
        _viewModel.Dispose();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Right || e.Key == Key.Space)
        {
            if (e.Key == Key.Right)
            {
                _viewModel.Reader.VisualRightCommand.Execute(null);
            }
            else
            {
                _viewModel.Reader.NextPageCommand.Execute(null);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Left || e.Key == Key.Back)
        {
            if (e.Key == Key.Left)
            {
                _viewModel.Reader.VisualLeftCommand.Execute(null);
            }
            else
            {
                _viewModel.Reader.PreviousPageCommand.Execute(null);
            }

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
        else if (e.Key == Key.O && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _viewModel.OpenContentCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y < 0)
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
}
