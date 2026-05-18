using System.Diagnostics;
using System.Runtime.Versioning;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class WindowsExplorerContextMenuService : IExplorerContextMenuService
{
    private const string ClassesRoot = @"Software\Classes";
    private const string ShellVerbName = "ComicPlate.Open";
    private const string MenuText = "在 ComicPlate 中打开";

    private readonly IWindowsRegistry _registry;
    private readonly string _executablePath;

    public WindowsExplorerContextMenuService(IWindowsRegistry registry, string executablePath)
    {
        _registry = registry;
        _executablePath = executablePath;
    }

    [SupportedOSPlatform("windows")]
    public static WindowsExplorerContextMenuService CreateDefault()
    {
        return new WindowsExplorerContextMenuService(
            new WindowsRegistry(),
            GetCurrentExecutablePath());
    }

    public ExplorerContextMenuState GetState()
    {
        return new ExplorerContextMenuState(
            true,
            IsRegistered(),
            IsRegistered()
                ? "右键菜单已注册。"
                : "");
    }

    public IReadOnlyList<ExplorerContextMenuOption> GetSupportedOptions()
    {
        return ComicArchiveFormats.SupportedFormats
            .Select(format => new ExplorerContextMenuOption(
                format.Extension,
                format.DisplayName,
                true,
                IsRegistered(format.Extension),
                IsRegistered(format.Extension)
                    ? $"{format.DisplayName} 右键菜单已注册。"
                    : ""))
            .ToArray();
    }

    public ExplorerContextMenuResult SetEnabled(bool isEnabled)
    {
        try
        {
            if (isEnabled)
            {
                Register();
                return IsRegistered()
                    ? new ExplorerContextMenuResult(true, "右键菜单已注册。")
                    : new ExplorerContextMenuResult(false, "右键菜单注册失败。");
            }

            Unregister();
            return IsRegistered()
                ? new ExplorerContextMenuResult(false, "右键菜单移除失败。")
                : new ExplorerContextMenuResult(true, "右键菜单已移除。");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new ExplorerContextMenuResult(false, "右键菜单设置失败，请检查系统权限。");
        }
    }

    public ExplorerContextMenuResult SetEnabled(string extension, bool isEnabled)
    {
        if (!ComicArchiveFormats.TryGetByExtension(extension, out var format))
        {
            return new ExplorerContextMenuResult(false, "不支持的文件格式。");
        }

        try
        {
            if (isEnabled)
            {
                Register(format);
                return IsRegistered(format.Extension)
                    ? new ExplorerContextMenuResult(true, $"{format.DisplayName} 右键菜单已注册。")
                    : new ExplorerContextMenuResult(false, $"{format.DisplayName} 右键菜单注册失败。");
            }

            Unregister(format);
            return IsRegistered(format.Extension)
                ? new ExplorerContextMenuResult(false, $"{format.DisplayName} 右键菜单移除失败。")
                : new ExplorerContextMenuResult(true, $"{format.DisplayName} 右键菜单已移除。");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new ExplorerContextMenuResult(false, "右键菜单设置失败，请检查系统权限。");
        }
    }

    private bool IsRegistered()
    {
        return ComicArchiveFormats.SupportedFormats
            .All(format => IsRegistered(format.Extension));
    }

    private bool IsRegistered(string extension)
    {
        var command = _registry.ReadDefaultValue(GetCommandKeyPath(extension));
        return command is not null
            && command.Equals(CreateOpenCommand(), StringComparison.OrdinalIgnoreCase);
    }

    private void Register()
    {
        foreach (var format in ComicArchiveFormats.SupportedFormats)
        {
            Register(format);
        }
    }

    private void Register(ComicArchiveFormat format)
    {
        var shellKeyPath = GetShellKeyPath(format.Extension);
        var commandKeyPath = GetCommandKeyPath(format.Extension);
        _registry.WriteDefaultValue(shellKeyPath, MenuText);
        _registry.WriteValue(shellKeyPath, "MUIVerb", MenuText);
        _registry.WriteValue(shellKeyPath, "Icon", CreateIconReference());
        _registry.WriteDefaultValue(commandKeyPath, CreateOpenCommand());
    }

    private void Unregister()
    {
        foreach (var format in ComicArchiveFormats.SupportedFormats)
        {
            Unregister(format);
        }
    }

    private void Unregister(ComicArchiveFormat format)
    {
        _registry.DeleteTree(GetShellKeyPath(format.Extension));
    }

    private string CreateOpenCommand()
    {
        return $"\"{_executablePath}\" \"%1\"";
    }

    private string CreateIconReference()
    {
        return $"\"{_executablePath}\",0";
    }

    private static string GetShellKeyPath(string extension)
    {
        return $@"{ClassesRoot}\SystemFileAssociations\{NormalizeExtension(extension)}\shell\{ShellVerbName}";
    }

    private static string GetCommandKeyPath(string extension)
    {
        return $@"{GetShellKeyPath(extension)}\command";
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }

    private static string GetCurrentExecutablePath()
    {
        return Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot determine ComicPlate executable path.");
    }
}
