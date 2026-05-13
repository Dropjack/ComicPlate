namespace ComicPlate.Core.Navigation;

public sealed class NavigationHistory
{
    private readonly Stack<NavigationEntry> _backStack = new();

    public NavigationEntry? Current { get; private set; }

    public bool CanGoBack => _backStack.Count > 0;

    public void StartAt(NavigationEntry entry)
    {
        _backStack.Clear();
        Current = entry;
    }

    public void NavigateTo(NavigationEntry entry)
    {
        if (Current is null)
        {
            Current = entry;
            return;
        }

        if (Current == entry)
        {
            return;
        }

        _backStack.Push(Current);
        Current = entry;
    }

    public NavigationEntry? Back()
    {
        if (!CanGoBack)
        {
            return null;
        }

        Current = _backStack.Pop();
        return Current;
    }
}
