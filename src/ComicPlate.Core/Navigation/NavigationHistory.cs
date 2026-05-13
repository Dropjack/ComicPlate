namespace ComicPlate.Core.Navigation;

public sealed class NavigationHistory
{
    public const int MaxBackStackEntries = 8;

    private readonly List<NavigationEntry> _backStack = new();

    public NavigationEntry? Current { get; private set; }

    public bool CanGoBack => _backStack.Count > 0;

    public IReadOnlyList<NavigationEntry> BackStack => _backStack
        .AsEnumerable()
        .Reverse()
        .ToArray();

    public void StartAt(NavigationEntry entry)
    {
        _backStack.Clear();
        Current = entry;
    }

    public void Restore(NavigationEntry current, IEnumerable<NavigationEntry> backStack)
    {
        Current = current;
        _backStack.Clear();

        foreach (var entry in backStack.Take(MaxBackStackEntries).Reverse())
        {
            _backStack.Add(entry);
        }
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

        _backStack.Add(Current);
        TrimBackStack();
        Current = entry;
    }

    public void ReplaceCurrent(NavigationEntry entry)
    {
        Current = entry;
    }

    public NavigationEntry? Back()
    {
        if (!CanGoBack)
        {
            return null;
        }

        var lastIndex = _backStack.Count - 1;
        Current = _backStack[lastIndex];
        _backStack.RemoveAt(lastIndex);
        return Current;
    }

    private void TrimBackStack()
    {
        if (_backStack.Count <= MaxBackStackEntries)
        {
            return;
        }

        _backStack.RemoveRange(0, _backStack.Count - MaxBackStackEntries);
    }
}
