using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly ImagePageLoader _imagePageLoader;
    private readonly ReaderState _readerState = new();
    private Bitmap? _currentImage;
    private string _currentLogicalPath = "";
    private int _currentPageIndex;
    private string _headerTitle = "ComicPlate";
    private bool _isReaderVisible;
    private bool _isStartVisible = true;
    private bool _isLoading;
    private string _pageText = "";
    private string _statusMessage = "No recent books yet.";

    public MainWindowViewModel(IFolderPickerService folderPickerService, ImagePageLoader imagePageLoader)
    {
        _folderPickerService = folderPickerService;
        _imagePageLoader = imagePageLoader;

        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync, () => !IsLoading);
        ShowStartCommand = new RelayCommand(ShowStart);
        NextPageCommand = new RelayCommand(NextPage, () => _readerState.CanGoNext);
        PreviousPageCommand = new RelayCommand(PreviousPage, () => _readerState.CanGoPrevious);
        FirstPageCommand = new RelayCommand(FirstPage, () => _readerState.HasPages);
        LastPageCommand = new RelayCommand(LastPage, () => _readerState.HasPages);
    }

    public ObservableCollection<PageListItemViewModel> PageItems { get; } = new();

    public ICommand OpenFolderCommand { get; }

    public ICommand ShowStartCommand { get; }

    public RelayCommand NextPageCommand { get; }

    public RelayCommand PreviousPageCommand { get; }

    public RelayCommand FirstPageCommand { get; }

    public RelayCommand LastPageCommand { get; }

    public Bitmap? CurrentImage
    {
        get => _currentImage;
        private set
        {
            if (SetProperty(ref _currentImage, value))
            {
                OnPropertyChanged(nameof(HasImage));
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasImage => CurrentImage is not null;

    public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) && CurrentImage is null;

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public string HeaderTitle
    {
        get => _headerTitle;
        private set => SetProperty(ref _headerTitle, value);
    }

    public bool IsStartVisible
    {
        get => _isStartVisible;
        private set => SetProperty(ref _isStartVisible, value);
    }

    public bool IsReaderVisible
    {
        get => _isReaderVisible;
        private set => SetProperty(ref _isReaderVisible, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            if (value == _currentPageIndex || value < 0 || value >= _readerState.PageCount)
            {
                return;
            }

            _readerState.GoToPage(value);
            _ = RefreshCurrentPageAsync();
        }
    }

    public int CurrentPageNumber => _readerState.HasPages ? _readerState.CurrentPageIndex + 1 : 0;

    public int PageCount => _readerState.PageCount;

    public string PageText
    {
        get => _pageText;
        private set => SetProperty(ref _pageText, value);
    }

    public string CurrentLogicalPath
    {
        get => _currentLogicalPath;
        private set => SetProperty(ref _currentLogicalPath, value);
    }

    private async Task OpenFolderAsync()
    {
        var folderPath = await _folderPickerService.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        IsLoading = true;
        HeaderTitle = Path.GetFileName(folderPath);
        ShowReader();
        SetMessage("Loading pages...");

        try
        {
            var source = new FolderBookSource(folderPath, recursive: true);
            var pages = await Task.Run(() => source.LoadPagesAsync(CancellationToken.None));
            LoadPages(pages);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LoadPages(Array.Empty<PageEntry>());
            SetMessage("ComicPlate could not read this folder.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadPages(IReadOnlyList<PageEntry> pages)
    {
        _readerState.LoadPages(pages);
        PageItems.Clear();

        for (var index = 0; index < pages.Count; index++)
        {
            PageItems.Add(new PageListItemViewModel(index, pages[index]));
        }

        if (pages.Count == 0)
        {
            CurrentImage = null;
            SetMessage("This folder has no readable images.");
            UpdatePageStatus();
            RaiseCommandStates();
            return;
        }

        _ = RefreshCurrentPageAsync();
    }

    private async Task RefreshCurrentPageAsync()
    {
        if (!_readerState.HasPages)
        {
            CurrentImage = null;
            SetMessage("This folder has no readable images.");
            UpdatePageStatus();
            return;
        }

        var page = _readerState.Pages[_readerState.CurrentPageIndex];
        _currentPageIndex = _readerState.CurrentPageIndex;
        OnPropertyChanged(nameof(CurrentPageIndex));

        CurrentLogicalPath = page.LogicalPath;
        SetMessage("");

        try
        {
            CurrentImage?.Dispose();
            CurrentImage = await _imagePageLoader.LoadAsync(page, CancellationToken.None);
        }
        catch (Exception)
        {
            CurrentImage = null;
            SetMessage($"Could not display this image:{Environment.NewLine}{page.DisplayName}");
        }

        UpdatePageStatus();
        RaiseCommandStates();
    }

    private void NextPage()
    {
        _readerState.NextPage();
        _ = RefreshCurrentPageAsync();
    }

    private void PreviousPage()
    {
        _readerState.PreviousPage();
        _ = RefreshCurrentPageAsync();
    }

    private void FirstPage()
    {
        _readerState.GoToFirstPage();
        _ = RefreshCurrentPageAsync();
    }

    private void LastPage()
    {
        _readerState.GoToLastPage();
        _ = RefreshCurrentPageAsync();
    }

    private void ShowStart()
    {
        IsStartVisible = true;
        IsReaderVisible = false;
        HeaderTitle = "ComicPlate";
    }

    private void ShowReader()
    {
        IsStartVisible = false;
        IsReaderVisible = true;
    }

    private void SetMessage(string message)
    {
        StatusMessage = message;
    }

    private void UpdatePageStatus()
    {
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(PageCount));

        PageText = _readerState.HasPages
            ? $"{CurrentPageNumber} / {PageCount}"
            : "0 / 0";
    }

    private void RaiseCommandStates()
    {
        if (OpenFolderCommand is AsyncRelayCommand openFolderCommand)
        {
            openFolderCommand.RaiseCanExecuteChanged();
        }

        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
        FirstPageCommand.RaiseCanExecuteChanged();
        LastPageCommand.RaiseCanExecuteChanged();
    }
}
