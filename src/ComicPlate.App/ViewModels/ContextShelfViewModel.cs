using System.Collections.ObjectModel;
using ComicPlate.App.Services;
using ComicPlate.Core.Books;

namespace ComicPlate.App.ViewModels;

public sealed class ContextShelfViewModel : ViewModelBase, IDisposable
{
    private readonly Func<ContentListItemViewModel, Task> _activateItemAsync;
    private readonly SidebarThumbnailLoader _thumbnailLoader = new();
    private CancellationTokenSource _thumbnailCancellationTokenSource = new();
    private int _currentIndex = -1;

    public ContextShelfViewModel(Func<ContentListItemViewModel, Task> activateItemAsync)
    {
        _activateItemAsync = activateItemAsync;
    }

    public ObservableCollection<ContentListItemViewModel> Items { get; } = new();

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (value == _currentIndex || value < 0 || value >= Items.Count)
            {
                return;
            }

            _currentIndex = value;
            OnPropertyChanged(nameof(CurrentIndex));
            _ = _activateItemAsync(Items[value]);
        }
    }

    public bool IsEmpty => Items.Count == 0;

    public int ItemCount => Items.Count;

    public void ReplaceItems(IEnumerable<BookEntry> books)
    {
        _thumbnailCancellationTokenSource.Cancel();
        _thumbnailCancellationTokenSource.Dispose();
        _thumbnailCancellationTokenSource = new CancellationTokenSource();
        _thumbnailLoader.Clear();
        Items.Clear();

        foreach (var book in books)
        {
            Items.Add(ContentListItemViewModel.FromBook(book));
        }

        SetCurrentIndexSilently(-1);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ItemCount));
    }

    public void SetVisualState(string? readingBookId, string? navigationCollectionPath)
    {
        var normalizedReadingBookId = NormalizePath(readingBookId);
        var normalizedNavigationCollectionPath = NormalizePath(navigationCollectionPath);

        foreach (var item in Items)
        {
            item.IsReading = PathsEqual(item.Book.Id, normalizedReadingBookId);
            item.IsNavigationCurrent =
                item.Book.SourceKind == BookSourceKind.Collection
                && PathsEqual(item.Book.Path, normalizedNavigationCollectionPath);
        }
    }

    public async Task LoadThumbnailsAsync()
    {
        var cancellationToken = _thumbnailCancellationTokenSource.Token;

        try
        {
            await _thumbnailLoader.LoadInitialThumbnailsAsync(Items.ToArray(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void SetCurrentIndexSilently(int index)
    {
        if (_currentIndex == index)
        {
            return;
        }

        _currentIndex = index;
        OnPropertyChanged(nameof(CurrentIndex));
    }

    public void Dispose()
    {
        _thumbnailCancellationTokenSource.Cancel();
        _thumbnailCancellationTokenSource.Dispose();
        _thumbnailLoader.Dispose();
    }

    private static bool PathsEqual(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
        {
            return false;
        }

        return string.Equals(
            NormalizePath(first),
            NormalizePath(second),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
    }
}
