using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.ViewModels;

public sealed class MainWindowViewModelWindowTitleTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ComicPlateMainWindowViewModelTests-{Guid.NewGuid():N}");

    public MainWindowViewModelWindowTitleTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

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

    [Fact]
    public void ReaderPreferencesAreLoadedAndSavedThroughSettings()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"ComicPlateMainWindowViewModelTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var settingsService = new SettingsService(tempDirectory);
            settingsService.Save(AppSettings.Default with
            {
                ReadingDirection = ReadingDirection.LeftToRight,
                ViewMode = ViewMode.DoublePage,
            });

            using var viewModel = new MainWindowViewModel(
                new StubFolderPickerService(),
                new ImagePageLoader(),
                new JsonAppStateStore(tempDirectory),
                settingsService: settingsService);

            Assert.Equal(ReadingDirection.LeftToRight, viewModel.Reader.ReadingDirection);
            Assert.Equal(ViewMode.DoublePage, viewModel.Reader.ViewMode);

            viewModel.Reader.ToggleReadingDirectionCommand.Execute(null);
            viewModel.Reader.ToggleViewModeCommand.Execute(null);

            var saved = settingsService.Load();
            Assert.Equal(ReadingDirection.RightToLeft, saved.ReadingDirection);
            Assert.Equal(ViewMode.SinglePage, saved.ViewMode);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private MainWindowViewModel CreateViewModel()
    {
        return new MainWindowViewModel(
            new StubFolderPickerService(),
            new ImagePageLoader(),
            new JsonAppStateStore(_tempDirectory),
            new SettingsService(_tempDirectory));
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
