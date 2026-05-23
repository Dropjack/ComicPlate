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

    [Fact]
    public async Task MagnifierIsHoldOnlyAndRemembersScaleForCurrentRun()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetReaderViewportSize(1000, 800);
        await viewModel.LoadPagesAsync(CreatePages(3));

        Assert.Equal("1.0x", viewModel.MagnifierScaleText);

        Assert.True(viewModel.BeginMagnifier());
        Assert.Equal(1.5, viewModel.MagnifierScale);

        Assert.True(viewModel.AdjustMagnifierScale(2));
        Assert.Equal(1.6, viewModel.MagnifierScale, precision: 3);

        viewModel.EndMagnifier();

        Assert.Equal(1.0, viewModel.MagnifierScale);
        Assert.Equal("1.0x", viewModel.MagnifierScaleText);

        Assert.True(viewModel.BeginMagnifier());

        Assert.Equal(1.6, viewModel.MagnifierScale, precision: 3);
    }

    [Fact]
    public async Task DisabledMagnifierDoesNotStartOrConsumeWheel()
    {
        using var viewModel = CreateViewModel();
        await viewModel.LoadPagesAsync(CreatePages(3));
        viewModel.SetMagnifierEnabled(false);

        Assert.False(viewModel.BeginMagnifier());
        Assert.False(viewModel.AdjustMagnifierScale(1));
        Assert.Equal("1.0x", viewModel.MagnifierScaleText);
    }

    [Fact]
    public async Task MagnifierScaleIsClamped()
    {
        using var viewModel = CreateViewModel();
        await viewModel.LoadPagesAsync(CreatePages(3));

        viewModel.BeginMagnifier();
        viewModel.AdjustMagnifierScale(100);

        Assert.Equal(2.5, viewModel.MagnifierScale);

        viewModel.AdjustMagnifierScale(-100);

        Assert.Equal(1.5, viewModel.MagnifierScale);
    }

    [Fact]
    public async Task MagnifierPointerUsesSlightlyHigherSensitivity()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetReaderViewportSize(1000, 800);
        await viewModel.LoadPagesAsync(CreatePages(10), initialPageIndex: 5);

        viewModel.UpdateMagnifierPointer(600, 400);
        Assert.True(viewModel.BeginMagnifier());

        var projectedPointerX = 615.0;
        var contentXUnderPointer = projectedPointerX - viewModel.ReaderStripTranslateX;
        var scaledPointerX = viewModel.MagnifiedReaderStripTranslateX
            + (contentXUnderPointer * viewModel.MagnifierScale);

        Assert.Equal(projectedPointerX, scaledPointerX, precision: 3);
    }

    [Fact]
    public async Task MagnifierKeepsPointerAnchoredWhenContentBoundsAllowIt()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetReaderViewportSize(1000, 800);
        await viewModel.LoadPagesAsync(CreatePages(10), initialPageIndex: 5);

        viewModel.UpdateMagnifierPointer(500, 400);
        Assert.True(viewModel.BeginMagnifier());

        var contentXUnderPointer = 500 - viewModel.ReaderStripTranslateX;
        var contentYUnderPointer = 400;
        var scaledPointerX = viewModel.MagnifiedReaderStripTranslateX
            + (contentXUnderPointer * viewModel.MagnifierScale);
        var scaledPointerY = viewModel.MagnifierContentTranslateY
            + (contentYUnderPointer * viewModel.MagnifierScale);

        Assert.Equal(500, scaledPointerX, precision: 3);
        Assert.Equal(400, scaledPointerY, precision: 3);
    }

    [Fact]
    public async Task MagnifierOffsetIsClampedToScaledContentBounds()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetReaderViewportSize(1000, 800);
        await viewModel.LoadPagesAsync(CreatePages(3), initialPageIndex: 0);

        viewModel.UpdateMagnifierPointer(500, 400);
        Assert.True(viewModel.BeginMagnifier());

        var scaledContentWidth = viewModel.ReaderStripItems.Sum(item => item.DisplayWidth)
            * viewModel.MagnifierScale;
        var scaledContentHeight = viewModel.ReaderStripItems.Max(item => item.DisplayHeight)
            * viewModel.MagnifierScale;

        Assert.True(viewModel.MagnifiedReaderStripTranslateX <= 0);
        Assert.True(viewModel.MagnifiedReaderStripTranslateX + scaledContentWidth >= 1000);
        Assert.True(viewModel.MagnifierContentTranslateY <= 0);
        Assert.True(viewModel.MagnifierContentTranslateY + scaledContentHeight >= 800);
    }

    [Fact]
    public async Task LoadPagesUsesInitialMetadataBeforeFirstReaderStrip()
    {
        using var viewModel = CreateViewModel();
        viewModel.SetReaderViewportSize(1000, 800);

        await viewModel.LoadPagesAsync([CreatePngPage("wide.png", 1600, 800)]);

        var item = Assert.Single(viewModel.ReaderStripItems);
        Assert.Equal(800, item.DisplayHeight);
        Assert.Equal(1600, item.DisplayWidth);
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

    private static PageEntry CreatePngPage(string name, int width, int height)
    {
        return new PageEntry(
            name,
            name,
            PageSourceKind.FileSystem,
            _ => Task.FromResult<Stream>(new MemoryStream(CreatePngHeader(width, height))));
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        var header = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(header, 0);
        WriteBigEndian(header, 16, width);
        WriteBigEndian(header, 20, height);
        return header;
    }

    private static void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 3] = (byte)(value & 0xFF);
    }
}
