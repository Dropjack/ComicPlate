using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;

namespace ComicPlate.Tests.ViewModels;

public sealed class ReaderSurfaceViewModelProgressPreviewTests
{
    [Fact]
    public async Task PreviewProgressDoesNotChangeCurrentPageUntilCommitted()
    {
        using var viewModel = CreateViewModel();
        await viewModel.LoadPagesAsync(CreatePages(10));
        viewModel.ToggleReadingDirectionCommand.Execute(null);

        viewModel.PreviewProgressRatio(0.5);

        Assert.Equal(0, viewModel.CurrentPageIndex);
        Assert.Equal("6 / 10", viewModel.PageText);
        Assert.Equal(5, viewModel.DisplayPageProgressIndex);

        viewModel.CommitProgressPreview(0.5);

        Assert.Equal(5, viewModel.CurrentPageIndex);
        Assert.Equal("6 / 10", viewModel.PageText);
        Assert.Equal(5, viewModel.DisplayPageProgressIndex);
    }

    [Fact]
    public async Task DoublePagePreviewAndCommitUseFrameStartPage()
    {
        using var viewModel = CreateViewModel();
        await viewModel.LoadPagesAsync(CreatePages(6));
        viewModel.ToggleReadingDirectionCommand.Execute(null);
        viewModel.ToggleViewModeCommand.Execute(null);

        viewModel.PreviewProgressRatio(0.4);

        Assert.Equal(0, viewModel.CurrentPageIndex);
        Assert.Equal("2-3 / 6", viewModel.PageText);
        Assert.Equal(1, viewModel.DisplayPageProgressIndex);

        viewModel.CommitProgressPreview(0.4);

        Assert.Equal(1, viewModel.CurrentPageIndex);
        Assert.Equal("2-3 / 6", viewModel.PageText);
        Assert.Equal(1, viewModel.DisplayPageProgressIndex);
    }

    private static ReaderSurfaceViewModel CreateViewModel()
    {
        return new ReaderSurfaceViewModel(new ReaderImageCache(new ImagePageLoader()));
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
}
