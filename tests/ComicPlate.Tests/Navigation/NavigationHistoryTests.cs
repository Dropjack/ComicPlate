using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;

namespace ComicPlate.Tests.Navigation;

public sealed class NavigationHistoryTests
{
    [Fact]
    public void StartAtClearsBackStack()
    {
        var history = new NavigationHistory();

        history.StartAt(CreateEntry("root"));
        history.NavigateTo(CreateEntry("book"));
        history.StartAt(CreateEntry("other"));

        Assert.False(history.CanNavigateUp);
        Assert.Equal("other", history.Current?.Path);
    }

    [Fact]
    public void NavigateToPushesCurrentEntry()
    {
        var history = new NavigationHistory();

        history.StartAt(CreateEntry("root"));
        history.NavigateTo(CreateEntry("book"));

        Assert.True(history.CanNavigateUp);
        Assert.Equal("book", history.Current?.Path);
    }

    [Fact]
    public void NavigateUpReturnsPreviousEntry()
    {
        var history = new NavigationHistory();

        history.StartAt(CreateEntry("root"));
        history.NavigateTo(CreateEntry("series"));
        history.NavigateTo(CreateEntry("book"));

        var previous = history.NavigateUp();

        Assert.Equal("series", previous?.Path);
        Assert.Equal("series", history.Current?.Path);
        Assert.True(history.CanNavigateUp);
    }

    [Fact]
    public void NavigateToCurrentEntryDoesNotPushDuplicate()
    {
        var history = new NavigationHistory();
        var root = CreateEntry("root");

        history.StartAt(root);
        history.NavigateTo(root);

        Assert.False(history.CanNavigateUp);
    }

    [Fact]
    public void BackStackIsLimitedToEightEntries()
    {
        var history = new NavigationHistory();
        history.StartAt(CreateEntry("root"));

        for (var index = 1; index <= 10; index++)
        {
            history.NavigateTo(CreateEntry($"item-{index}"));
        }

        Assert.Equal(8, history.BackStack.Count);
        Assert.Equal("item-9", history.BackStack[0].Path);
        Assert.Equal("item-2", history.BackStack[^1].Path);
    }

    [Fact]
    public void RestoreKeepsBackOrder()
    {
        var history = new NavigationHistory();

        history.Restore(
            CreateEntry("book"),
            new[] { CreateEntry("series"), CreateEntry("root") });

        Assert.Equal("series", history.NavigateUp()?.Path);
        Assert.Equal("root", history.NavigateUp()?.Path);
        Assert.False(history.CanNavigateUp);
    }

    private static NavigationEntry CreateEntry(string path)
    {
        return new NavigationEntry(path, path, BookSourceKind.Collection);
    }
}
