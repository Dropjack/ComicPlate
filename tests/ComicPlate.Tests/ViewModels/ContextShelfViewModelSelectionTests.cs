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
