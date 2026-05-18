using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;

namespace ComicPlate.Tests.ViewModels;

public sealed class MainWindowViewModelWindowTitleTests
{
    [Fact]
    public async Task WindowTitleIncludesReaderTitleAndCurrentPage()
    {
        using var viewModel = CreateViewModel();

        Assert.Equal("ComicPlate", viewModel.WindowTitle);

        await viewModel.Reader.LoadPagesAsync(CreatePages(3));
        SetReaderTitle(viewModel, "Vol. 01.cbz");

        Assert.Equal("Vol. 01.cbz (1 / 3) - ComicPlate", viewModel.WindowTitle);

        viewModel.Reader.NextPageCommand.Execute(null);

        Assert.Equal("Vol. 01.cbz (2 / 3) - ComicPlate", viewModel.WindowTitle);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        return new MainWindowViewModel(new StubFolderPickerService(), new ImagePageLoader());
    }

    private static void SetReaderTitle(MainWindowViewModel viewModel, string title)
    {
        var property = typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.ReaderTitle))!;
        property.SetValue(viewModel, title);
    }

    private static IReadOnlyList<PageEntry> CreatePages(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new PageEntry(
                $"{index:D3}.jpg",
                $"{index:D3}.jpg",
                PageSourceKind.FileSystem,
                _ => Task.FromResult<Stream>(new MemoryStream())))
            .ToArray();
    }

    private sealed class StubFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync()
        {
            return Task.FromResult<string?>(null);
        }
    }
}
