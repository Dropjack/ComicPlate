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

        Assert.False(history.CanGoBack);
        Assert.Equal("other", history.Current?.Path);
    }

    [Fact]
    public void NavigateToPushesCurrentEntry()
    {
        var history = new NavigationHistory();

        history.StartAt(CreateEntry("root"));
        history.NavigateTo(CreateEntry("book"));

        Assert.True(history.CanGoBack);
        Assert.Equal("book", history.Current?.Path);
    }

    [Fact]
    public void BackReturnsPreviousEntry()
    {
        var history = new NavigationHistory();

        history.StartAt(CreateEntry("root"));
        history.NavigateTo(CreateEntry("series"));
        history.NavigateTo(CreateEntry("book"));

        var previous = history.Back();

        Assert.Equal("series", previous?.Path);
        Assert.Equal("series", history.Current?.Path);
        Assert.True(history.CanGoBack);
    }

    [Fact]
    public void NavigateToCurrentEntryDoesNotPushDuplicate()
    {
        var history = new NavigationHistory();
        var root = CreateEntry("root");

        history.StartAt(root);
        history.NavigateTo(root);

        Assert.False(history.CanGoBack);
    }

    private static NavigationEntry CreateEntry(string path)
    {
        return new NavigationEntry(path, path, BookSourceKind.Collection);
    }
}
