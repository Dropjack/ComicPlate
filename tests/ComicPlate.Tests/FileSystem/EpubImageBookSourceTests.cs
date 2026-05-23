using System.IO.Compression;
using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.FileSystem;

namespace ComicPlate.Tests.FileSystem;

public sealed class EpubImageBookSourceTests : IDisposable
{
    private static readonly byte[] PngBytes =
    [
        137, 80, 78, 71, 13, 10, 26, 10,
        0, 0, 0, 13, 73, 72, 68, 82,
        0, 0, 0, 1, 0, 0, 0, 1,
        8, 6, 0, 0, 0, 31, 21, 196,
        137, 0, 0, 0, 13, 73, 68, 65,
        84, 120, 156, 99, 248, 15, 4,
        0, 9, 251, 3, 253, 167, 80, 120,
        156, 0, 0, 0, 0, 73, 69, 78,
        68, 174, 66, 96, 130
    ];

    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"ComicPlateEpubTests-{Guid.NewGuid():N}");

    public EpubImageBookSourceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadsImagesInSpineHtmlOrder()
    {
        var epubPath = Path.Combine(_tempDirectory, "comic.epub");
        WriteEpub(
            epubPath,
            [
                new EpubItem("chapter2", "Text/chapter2.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><img src=\"../Images/002.png\" /></body></html>"),
                new EpubItem("chapter1", "Text/chapter1.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><img src=\"../Images/001.png\" /></body></html>"),
                new EpubItem("image1", "Images/001.png", "image/png", null),
                new EpubItem("image2", "Images/002.png", "image/png", null)
            ],
            ["chapter2", "chapter1"]);

        var source = new EpubImageBookSource(epubPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Equal(BookSourceKind.Epub, source.SourceKind);
        Assert.Equal(new[] { "OEBPS/Images/002.png", "OEBPS/Images/001.png" }, pages.Select(page => page.LogicalPath));
        Assert.All(pages, page => Assert.Equal(PageSourceKind.EpubImage, page.SourceKind));
    }

    [Fact]
    public async Task ReadsMultipleImagesAndSkipsDuplicates()
    {
        var epubPath = Path.Combine(_tempDirectory, "multi.epub");
        WriteEpub(
            epubPath,
            [
                new EpubItem("chapter1", "Text/chapter1.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><img src=\"../Images/cover.png\" /><img src=\"../Images/page.png\" /></body></html>"),
                new EpubItem("chapter2", "Text/chapter2.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><img src=\"../Images/page.png\" /></body></html>"),
                new EpubItem("cover", "Images/cover.png", "image/png", null, "cover-image"),
                new EpubItem("page", "Images/page.png", "image/png", null)
            ],
            ["chapter1", "chapter2"]);

        var source = new EpubImageBookSource(epubPath);
        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Equal(new[] { "OEBPS/Images/cover.png", "OEBPS/Images/page.png" }, pages.Select(page => page.LogicalPath));
    }

    [Fact]
    public async Task ThumbnailPagePrefersManifestCoverOutsideSpine()
    {
        var epubPath = Path.Combine(_tempDirectory, "cover.epub");
        WriteEpub(
            epubPath,
            [
                new EpubItem("chapter", "Text/chapter.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><img src=\"../Images/page.png\" /></body></html>"),
                new EpubItem("cover", "Images/cover.png", "image/png", null, "cover-image"),
                new EpubItem("page", "Images/page.png", "image/png", null)
            ],
            ["chapter"]);
        var source = new EpubImageBookSource(epubPath);

        var pages = await source.LoadPagesAsync(CancellationToken.None);
        var thumbnailPage = await source.LoadCoverOrFirstPageAsync(CancellationToken.None);

        Assert.Equal(new[] { "OEBPS/Images/page.png" }, pages.Select(page => page.LogicalPath));
        Assert.NotNull(thumbnailPage);
        Assert.Equal("OEBPS/Images/cover.png", thumbnailPage.LogicalPath);
    }

    [Fact]
    public async Task OpensImageStreamFromEpubEntry()
    {
        var epubPath = Path.Combine(_tempDirectory, "comic.epub");
        WriteEpub(
            epubPath,
            [
                new EpubItem("chapter", "chapter.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><img src=\"image.png\" /></body></html>"),
                new EpubItem("image", "image.png", "image/png", null)
            ],
            ["chapter"]);
        var source = new EpubImageBookSource(epubPath);
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
    public async Task EmptyAndTextOnlyChaptersLoadNoPages()
    {
        var epubPath = Path.Combine(_tempDirectory, "text.epub");
        WriteEpub(
            epubPath,
            [
                new EpubItem("chapter1", "chapter1.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><p>Text only.</p></body></html>"),
                new EpubItem("chapter2", "chapter2.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body></body></html>")
            ],
            ["chapter1", "chapter2"]);
        var source = new EpubImageBookSource(epubPath);

        var pages = await source.LoadPagesAsync(CancellationToken.None);

        Assert.Empty(pages);
    }

    [Fact]
    public async Task EncryptedEpubReportsOpenFailure()
    {
        var epubPath = Path.Combine(_tempDirectory, "encrypted.epub");
        WriteEpub(
            epubPath,
            [new EpubItem("chapter", "chapter.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body /></html>")],
            ["chapter"],
            encrypted: true);
        var source = new EpubImageBookSource(epubPath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => source.LoadPagesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task BrokenEpubReportsOpenFailure()
    {
        var epubPath = Path.Combine(_tempDirectory, "broken.epub");
        await File.WriteAllTextAsync(epubPath, "not an epub");
        var source = new EpubImageBookSource(epubPath);

        await Assert.ThrowsAnyAsync<Exception>(
            () => source.LoadPagesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task HonorsCancellationBeforeLoadingPages()
    {
        var epubPath = Path.Combine(_tempDirectory, "comic.epub");
        WriteEpub(
            epubPath,
            [new EpubItem("chapter", "chapter.xhtml", "application/xhtml+xml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body /></html>")],
            ["chapter"]);
        var source = new EpubImageBookSource(epubPath);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => source.LoadPagesAsync(cancellation.Token));
    }

    private static void WriteEpub(
        string path,
        IReadOnlyList<EpubItem> items,
        IReadOnlyList<string> spine,
        bool encrypted = false)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteEntry(archive, "mimetype", "application/epub+zip");
        WriteEntry(
            archive,
            "META-INF/container.xml",
            """
            <?xml version="1.0" encoding="utf-8"?>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
              <rootfiles>
                <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml" />
              </rootfiles>
            </container>
            """);

        if (encrypted)
        {
            WriteEntry(archive, "META-INF/encryption.xml", "<encryption />");
        }

        var manifest = string.Join(
            Environment.NewLine,
            items.Select(item =>
            {
                var properties = string.IsNullOrWhiteSpace(item.Properties)
                    ? ""
                    : $" properties=\"{item.Properties}\"";
                return $"    <item id=\"{item.Id}\" href=\"{item.Href}\" media-type=\"{item.MediaType}\"{properties} />";
            }));
        var spineItems = string.Join(
            Environment.NewLine,
            spine.Select(id => $"    <itemref idref=\"{id}\" />"));
        WriteEntry(
            archive,
            "OEBPS/content.opf",
            $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="3.0">
              <manifest>
            {{manifest}}
              </manifest>
              <spine>
            {{spineItems}}
              </spine>
            </package>
            """);

        foreach (var item in items)
        {
            if (item.Content is null)
            {
                WriteEntry(archive, $"OEBPS/{item.Href}", PngBytes);
            }
            else
            {
                WriteEntry(archive, $"OEBPS/{item.Href}", item.Content);
            }
        }
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] content)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        stream.Write(content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed record EpubItem(
        string Id,
        string Href,
        string MediaType,
        string? Content,
        string Properties = "");
}
