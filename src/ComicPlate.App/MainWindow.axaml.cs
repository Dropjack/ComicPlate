using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;

namespace ComicPlate.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(
            new FolderPickerService(this),
            new ImagePageLoader());
        DataContext = _viewModel;
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
}
