using ComicPlate.Core.Books;
using PDFtoImage;
using SkiaSharp;

namespace ComicPlate.Infrastructure.FileSystem;

public sealed class PdfPageBookSource : IBookSource
{
    private const int RenderDpi = 144;
    private const int MaxRenderDimension = 4096;
    private const float PdfPointsPerInch = 72;

    private readonly string _pdfPath;

    public PdfPageBookSource(string pdfPath)
    {
        _pdfPath = Path.GetFullPath(pdfPath);
    }

    public string Id => _pdfPath;

    public string DisplayName => Path.GetFileName(_pdfPath);

    public BookSourceKind SourceKind => BookSourceKind.Pdf;

    public Task<IReadOnlyList<PageEntry>> LoadPagesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = File.OpenRead(_pdfPath);
#pragma warning disable CA1416
        var pageCount = Conversion.GetPageCount(stream, leaveOpen: false);
#pragma warning restore CA1416
        var pages = Enumerable.Range(0, pageCount)
            .Select(CreatePageEntry)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PageEntry>>(pages);
    }

    private PageEntry CreatePageEntry(int zeroBasedPageIndex)
    {
        var pageNumber = zeroBasedPageIndex + 1;
        var logicalPath = $"page-{pageNumber:D4}.png";

        return new PageEntry(
            $"Page {pageNumber}",
            logicalPath,
            PageSourceKind.PdfPage,
            cancellationToken => RenderPageAsync(zeroBasedPageIndex, cancellationToken));
    }

    private Task<Stream> RenderPageAsync(int zeroBasedPageIndex, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = CreateRenderOptions(zeroBasedPageIndex, cancellationToken);
        using var pdfStream = File.OpenRead(_pdfPath);
#pragma warning disable CA1416
        using var bitmap = Conversion.ToImage(
            pdfStream,
            zeroBasedPageIndex,
            leaveOpen: false,
            password: null,
            options: options);
#pragma warning restore CA1416

        cancellationToken.ThrowIfCancellationRequested();

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 95);
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }

    private RenderOptions CreateRenderOptions(int zeroBasedPageIndex, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var pdfStream = File.OpenRead(_pdfPath);
#pragma warning disable CA1416
        var pageSize = Conversion.GetPageSize(
            pdfStream,
            zeroBasedPageIndex,
            leaveOpen: false,
            password: null);
#pragma warning restore CA1416

        var width = Math.Max(1, (int)Math.Ceiling(pageSize.Width * RenderDpi / PdfPointsPerInch));
        var height = Math.Max(1, (int)Math.Ceiling(pageSize.Height * RenderDpi / PdfPointsPerInch));
        var scale = Math.Min(1, MaxRenderDimension / (double)Math.Max(width, height));

        return new RenderOptions(
            Dpi: RenderDpi,
            Width: Math.Max(1, (int)Math.Round(width * scale)),
            Height: Math.Max(1, (int)Math.Round(height * scale)),
            UseTiling: true);
    }
}
