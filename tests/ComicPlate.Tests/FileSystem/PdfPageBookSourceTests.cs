using ComicPlate.Core.Books;
using ComicPlate.App.Services;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class PdfPageBookSourceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlatePdfTests-{Guid.NewGuid():N}");

    public PdfPageBookSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsPdfPagesInPageOrder()
    {
        var pdfPath = Path.Combine(_tempDirectory, "comic.pdf");
        TestPdfFactory.WriteTwoPagePdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);

        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Equal(BookSourceKind.Pdf, source.SourceKind);
        Assert.Equal(new[] { "page-0001.png", "page-0002.png" }, pages.Select(page => page.LogicalPath));
        Assert.All(pages, page => Assert.Equal(PageSourceKind.PdfPage, page.SourceKind));
    }

    [Fact]
    public async Task RendersPageAsPngStream()
    {
        var pdfPath = Path.Combine(_tempDirectory, "comic.pdf");
        TestPdfFactory.WriteTwoPagePdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);

        await using var stream = await pages[0].OpenStreamAsync(CancellationToken.None);

        Assert.True(stream.CanSeek);
        Assert.Equal(0, stream.Position);

        var signature = new byte[8];
        var read = await stream.ReadAsync(signature, CancellationToken.None);
        Assert.Equal(8, read);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, signature);
    }

    [Fact]
    public async Task CapsRenderedPageDimensionsForLargePdfPages()
    {
        var pdfPath = Path.Combine(_tempDirectory, "large.pdf");
        TestPdfFactory.WriteTwoPagePdf(pdfPath, width: 10000, height: 5000);
        var source = new PdfPageBookSource(pdfPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);

        await using var stream = await pages[0].OpenStreamAsync(CancellationToken.None);
        var imageInfo = ImageMetadataReader.Read(stream);

        Assert.True(imageInfo.PixelWidth <= ReaderImageDecodeRequest.MaximumDecodeDimension);
        Assert.True(imageInfo.PixelHeight <= ReaderImageDecodeRequest.MaximumDecodeDimension);
    }

    [Fact]
    public async Task HonorsCancellationBeforeLoadingPages()
    {
        var pdfPath = Path.Combine(_tempDirectory, "comic.pdf");
        TestPdfFactory.WriteTwoPagePdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => source.LoadPagesAsync(cancellation.Token));
    }

    [Fact]
    public async Task HonorsCancellationBeforeRenderingPage()
    {
        var pdfPath = Path.Combine(_tempDirectory, "comic.pdf");
        TestPdfFactory.WriteTwoPagePdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => pages[0].OpenStreamAsync(cancellation.Token));
    }

    [Fact]
    public async Task BrokenPdfReportsOpenFailure()
    {
        var pdfPath = Path.Combine(_tempDirectory, "broken.pdf");
        await File.WriteAllTextAsync(pdfPath, "not a pdf");
        var source = new PdfPageBookSource(pdfPath);

        await Assert.ThrowsAnyAsync<Exception>(
            () => source.LoadPagesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EmptyPdfLoadsNoPages()
    {
        var pdfPath = Path.Combine(_tempDirectory, "empty.pdf");
        TestPdfFactory.WriteEmptyPdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);

        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Empty(pages);
    }

    [Fact]
    public async Task EncryptedPdfReportsOpenFailure()
    {
        var pdfPath = Path.Combine(_tempDirectory, "encrypted.pdf");
        TestPdfFactory.WriteEncryptedPlaceholderPdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);

        await Assert.ThrowsAnyAsync<Exception>(
            () => source.LoadPagesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MissingPdfDuringRenderReportsRenderFailure()
    {
        var pdfPath = Path.Combine(_tempDirectory, "comic.pdf");
        TestPdfFactory.WriteTwoPagePdf(pdfPath);
        var source = new PdfPageBookSource(pdfPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);
        File.Delete(pdfPath);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => pages[0].OpenStreamAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
