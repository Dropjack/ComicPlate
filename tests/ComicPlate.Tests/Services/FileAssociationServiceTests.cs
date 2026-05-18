using ComicPlate.App.Services;

namespace ComicPlate.Tests.Services;

public sealed class FileAssociationServiceTests
{
    [Fact]
    public void WindowsServiceExposesOnlySupportedArchiveFormats()
    {
        var service = new WindowsFileAssociationService(
            new InMemoryFileAssociationRegistry(),
            @"D:\Tools\ComicPlate\ComicPlate.exe");

        var options = service.GetSupportedAssociations();

        Assert.Equal(new[] { ".cbz", ".zip", ".cbr", ".rar" }, options.Select(option => option.Extension));
        Assert.All(options, option => Assert.True(option.CanAssociate));
    }

    [Fact]
    public void WindowsServiceAssociatesSupportedExtensionWithoutTouchingRealRegistry()
    {
        var registry = new InMemoryFileAssociationRegistry();
        var service = new WindowsFileAssociationService(
            registry,
            @"D:\Tools\ComicPlate\ComicPlate.exe");

        var result = service.Associate(".cbr");

        Assert.True(result.Succeeded);
        Assert.Equal(
            "ComicPlate.cbr",
            registry.ReadDefaultValue(@"Software\Classes\.cbr"));
        Assert.Equal(
            "\"D:\\Tools\\ComicPlate\\ComicPlate.exe\" \"%1\"",
            registry.ReadDefaultValue(@"Software\Classes\ComicPlate.cbr\shell\open\command"));
        Assert.Equal(
            "\"D:\\Tools\\ComicPlate\\ComicPlate.exe\",0",
            registry.ReadDefaultValue(@"Software\Classes\ComicPlate.cbr\DefaultIcon"));
    }

    [Fact]
    public void WindowsServiceRejectsUnsupportedExtensions()
    {
        var registry = new InMemoryFileAssociationRegistry();
        var service = new WindowsFileAssociationService(
            registry,
            @"D:\Tools\ComicPlate\ComicPlate.exe");

        var result = service.Associate(".cb7");

        Assert.False(result.Succeeded);
        Assert.Empty(registry.Values);
    }

    [Fact]
    public void WindowsServiceDisassociatesComicPlateExtension()
    {
        var registry = new InMemoryFileAssociationRegistry();
        var service = new WindowsFileAssociationService(
            registry,
            @"D:\Tools\ComicPlate\ComicPlate.exe");

        service.Associate(".cbr");
        var result = service.Disassociate(".cbr");

        Assert.True(result.Succeeded);
        Assert.Null(registry.ReadDefaultValue(@"Software\Classes\.cbr"));
        Assert.False(registry.Values.ContainsKey(@"Software\Classes\ComicPlate.cbr"));
        Assert.DoesNotContain(registry.Values.Keys, key => key.StartsWith(@"Software\Classes\ComicPlate.cbr\", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WindowsServiceDisassociateDoesNotRemoveAnotherDefaultHandler()
    {
        var registry = new InMemoryFileAssociationRegistry();
        registry.WriteDefaultValue(@"Software\Classes\.cbr", "OtherApp.cbr");
        var service = new WindowsFileAssociationService(
            registry,
            @"D:\Tools\ComicPlate\ComicPlate.exe");

        var result = service.Disassociate(".cbr");

        Assert.True(result.Succeeded);
        Assert.Equal("OtherApp.cbr", registry.ReadDefaultValue(@"Software\Classes\.cbr"));
    }

    [Fact]
    public void WindowsServiceDoesNotClaimSuccessWhenWindowsUserChoicePointsElsewhere()
    {
        var registry = new InMemoryFileAssociationRegistry();
        registry.WriteValue(
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.cbr\UserChoice",
            "ProgId",
            "OtherApp.cbr");
        var service = new WindowsFileAssociationService(
            registry,
            @"D:\Tools\ComicPlate\ComicPlate.exe");

        var result = service.Associate(".cbr");

        Assert.False(result.Succeeded);
        Assert.Contains("Windows 默认应用", result.Message);
        Assert.Equal(
            "ComicPlate.cbr",
            registry.ReadDefaultValue(@"Software\Classes\.cbr"));
    }

    [Fact]
    public void MacOSServiceExposesSupportedFormatsAsUnavailable()
    {
        var service = new MacOSFileAssociationService();

        var options = service.GetSupportedAssociations();

        Assert.Equal(new[] { ".cbz", ".zip", ".cbr", ".rar" }, options.Select(option => option.Extension));
        Assert.All(options, option => Assert.False(option.CanAssociate));
    }

    private sealed class InMemoryFileAssociationRegistry : IWindowsRegistry
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
