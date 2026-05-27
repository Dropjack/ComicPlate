using ComicPlate.App.ViewModels;
using ComicPlate.Core.Books;

namespace ComicPlate.Tests.ViewModels;

public sealed class ContextShelfViewModelSelectionTests
{
    [Fact]
    public void MoveSelection_StartsFromReadingItemWhenSelectionIsEmpty()
    {
        var activated = new List<string>();
        var viewModel = new ContextShelfViewModel(item =>
        {
            activated.Add(item.Entry.DisplayName);
            return Task.CompletedTask;
        });
        viewModel.ReplaceItems(new[]
        {
            CreateBook("Book 1"),
            CreateBook("Book 2"),
            CreateBook("Book 3"),
        });
        viewModel.Items[1].IsReading = true;

        var moved = viewModel.MoveSelection(1);

        Assert.True(moved);
        Assert.Equal(2, viewModel.CurrentIndex);
        Assert.Equal(new[] { "Book 3" }, activated);
    }

    [Fact]
    public void MoveSelection_DoesNotWrapPastListEdges()
    {
        var activated = new List<string>();
        var viewModel = new ContextShelfViewModel(item =>
        {
            activated.Add(item.Entry.DisplayName);
            return Task.CompletedTask;
        });
        viewModel.ReplaceItems(new[]
        {
            CreateBook("Book 1"),
        });

        Assert.True(viewModel.MoveSelection(1));
        Assert.False(viewModel.MoveSelection(1));
        Assert.Equal(0, viewModel.CurrentIndex);
        Assert.Equal(new[] { "Book 1" }, activated);
    }

    [Fact]
    public void SetVisualState_MarksOpenedBookItems()
    {
        var viewModel = new ContextShelfViewModel(_ => Task.CompletedTask);
        var openedBook = CreateBook("Book 1");
        var unopenedBook = CreateBook("Book 2");
        viewModel.ReplaceItems(new[]
        {
            openedBook,
            unopenedBook,
            new ShelfEntry(
                Path.Combine("C:\\Comics", "Folder"),
                "Folder",
                ShelfEntryKind.Collection,
                Path.Combine("C:\\Comics", "Folder")),
        });

        viewModel.SetVisualState(
            readingBookId: null,
            navigationCollectionPath: null,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Path.GetFullPath(openedBook.Path)
            });

        Assert.True(viewModel.Items[0].IsOpened);
        Assert.False(viewModel.Items[1].IsOpened);
        Assert.False(viewModel.Items[2].IsOpened);
    }

    private static ShelfEntry CreateBook(string name)
    {
        var path = Path.Combine("C:\\Comics", $"{name}.cbz");
        return new ShelfEntry(
            path,
            name,
            ShelfEntryKind.Book,
            path,
            BookSourceKind.Zip);
    }
}
