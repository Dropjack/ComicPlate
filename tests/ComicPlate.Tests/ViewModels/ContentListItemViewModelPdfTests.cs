using ComicPlate.App.Services;
using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.ViewModels;

public sealed class ContentListItemViewModelPdfTests
{
    public ContentListItemViewModelPdfTests()
    {
        LocalizationService.Initialize(AppLanguage.English);
    }

    [Fact]
    public void PdfShelfEntryUsesPdfDetailLabel()
    {
        var entry = new ShelfEntry(
            @"D:\Books\comic.pdf",
            "comic.pdf",
            ShelfEntryKind.Book,
            @"D:\Books\comic.pdf",
            BookSourceKind.Pdf);

        var item = ContentListItemViewModel.FromShelfEntry(entry);

        Assert.Equal("PDF", item.Detail);
        Assert.Equal(ContentListItemKind.Archive, item.Kind);
    }
}
