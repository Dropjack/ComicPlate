using ComicPlate.App.Services;

namespace ComicPlate.Tests.Services;

public sealed class ExplorerContextMenuServiceTests
{
    [Fact]
    public void WindowsServiceReportsUnregisteredByDefault()
    {
        var service = CreateService(new InMemoryRegistry());

        var state = service.GetState();

        Assert.True(state.IsSupported);
        Assert.False(state.IsRegistered);
    }

    [Fact]
    public void WindowsServiceRegistersSupportedArchiveContextMenus()
    {
        var registry = new InMemoryRegistry();
        var service = CreateService(registry);

        var result = service.SetEnabled(true);

        Assert.True(result.Succeeded);
        Assert.True(service.GetState().IsRegistered);
        Assert.Equal(
            "在 ComicPlate 中打开",
            registry.ReadDefaultValue(@"Software\Classes\SystemFileAssociations\.cbz\shell\ComicPlate.Open"));
        Assert.Equal(
            "\"D:\\Tools\\ComicPlate\\ComicPlate.exe\" \"%1\"",
            registry.ReadDefaultValue(@"Software\Classes\SystemFileAssociations\.cbr\shell\ComicPlate.Open\command"));
        Assert.Equal(
            "\"D:\\Tools\\ComicPlate\\ComicPlate.exe\",0",
            registry.ReadValue(@"Software\Classes\SystemFileAssociations\.cbz\shell\ComicPlate.Open", "Icon"));
        Assert.False(registry.Values.ContainsKey(@"Software\Classes\SystemFileAssociations\.cb7\shell\ComicPlate.Open"));
    }

    [Fact]
    public void WindowsServiceRegistersSingleArchiveContextMenu()
    {
        var registry = new InMemoryRegistry();
        var service = CreateService(registry);

        var result = service.SetEnabled(".zip", true);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "\"D:\\Tools\\ComicPlate\\ComicPlate.exe\" \"%1\"",
            registry.ReadDefaultValue(@"Software\Classes\SystemFileAssociations\.zip\shell\ComicPlate.Open\command"));
        Assert.Null(registry.ReadDefaultValue(@"Software\Classes\SystemFileAssociations\.cbr\shell\ComicPlate.Open\command"));
    }

    [Fact]
    public void WindowsServiceUnregistersSingleArchiveContextMenu()
    {
        var registry = new InMemoryRegistry();
        var service = CreateService(registry);
        service.SetEnabled(true);

        var result = service.SetEnabled(".zip", false);

        Assert.True(result.Succeeded);
        Assert.Null(registry.ReadDefaultValue(@"Software\Classes\SystemFileAssociations\.zip\shell\ComicPlate.Open\command"));
        Assert.NotNull(registry.ReadDefaultValue(@"Software\Classes\SystemFileAssociations\.cbr\shell\ComicPlate.Open\command"));
    }

    [Fact]
    public void WindowsServiceUnregistersSupportedArchiveContextMenus()
    {
        var registry = new InMemoryRegistry();
        var service = CreateService(registry);
        service.SetEnabled(true);

        var result = service.SetEnabled(false);

        Assert.True(result.Succeeded);
        Assert.False(service.GetState().IsRegistered);
        Assert.DoesNotContain(registry.Values.Keys, key => key.Contains(@"\ComicPlate.Open", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnsupportedServiceIsUnavailable()
    {
        var service = new UnsupportedExplorerContextMenuService();

        var state = service.GetState();
        var result = service.SetEnabled(true);

        Assert.False(state.IsSupported);
        Assert.False(state.IsRegistered);
        Assert.False(result.Succeeded);
    }

    private static WindowsExplorerContextMenuService CreateService(InMemoryRegistry registry)
    {
        return new WindowsExplorerContextMenuService(registry, @"D:\Tools\ComicPlate\ComicPlate.exe");
    }

    private sealed class InMemoryRegistry : IWindowsRegistry
    {
        public Dictionary<string, Dictionary<string, string>> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? ReadDefaultValue(string keyPath)
        {
            return ReadValue(keyPath, "");
        }

        public string? ReadValue(string keyPath, string valueName)
        {
            return Values.TryGetValue(keyPath, out var values) && values.TryGetValue(valueName, out var value)
                ? value
                : null;
        }

        public void WriteDefaultValue(string keyPath, string value)
        {
            WriteValue(keyPath, "", value);
        }

        public void WriteValue(string keyPath, string valueName, string value)
        {
            if (!Values.TryGetValue(keyPath, out var values))
            {
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Values[keyPath] = values;
            }

            values[valueName] = value;
        }

        public void DeleteValue(string keyPath, string valueName)
        {
            if (!Values.TryGetValue(keyPath, out var values))
            {
                return;
            }

            values.Remove(valueName);
            if (values.Count == 0)
            {
                Values.Remove(keyPath);
            }
        }

        public void DeleteTree(string keyPath)
        {
            foreach (var key in Values.Keys.Where(key => IsKeyOrChild(key, keyPath)).ToArray())
            {
                Values.Remove(key);
            }
        }

        private static bool IsKeyOrChild(string key, string parent)
        {
            return key.Equals(parent, StringComparison.OrdinalIgnoreCase)
                || key.StartsWith(parent + @"\", StringComparison.OrdinalIgnoreCase);
        }
    }
}
