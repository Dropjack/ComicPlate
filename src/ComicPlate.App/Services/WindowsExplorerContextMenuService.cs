using System.Diagnostics;
using System.Runtime.Versioning;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class WindowsExplorerContextMenuService : IExplorerContextMenuService
{
    private const string ClassesRoot = @"Software\Classes";
    private const string ShellVerbName = "ComicPlate.Open";
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
                ? LocalizationService.Current.GetString("ExplorerContextMenu.Status.Registered")
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
                    ? LocalizationService.Current.Format(
                        "ExplorerContextMenu.Status.FormatRegistered",
                        format.DisplayName)
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
                    ? new ExplorerContextMenuResult(
                        true,
                        LocalizationService.Current.GetString("ExplorerContextMenu.Status.Registered"))
                    : new ExplorerContextMenuResult(
                        false,
                        LocalizationService.Current.GetString("ExplorerContextMenu.Error.RegistrationFailed"));
            }

            Unregister();
            return IsRegistered()
                ? new ExplorerContextMenuResult(
                    false,
                    LocalizationService.Current.GetString("ExplorerContextMenu.Error.RemovalFailed"))
                : new ExplorerContextMenuResult(
                    true,
                    LocalizationService.Current.GetString("ExplorerContextMenu.Status.Removed"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new ExplorerContextMenuResult(
                false,
                LocalizationService.Current.GetString("ExplorerContextMenu.Error.SettingFailed"));
        }
    }

    public ExplorerContextMenuResult SetEnabled(string extension, bool isEnabled)
    {
        if (!ComicArchiveFormats.TryGetByExtension(extension, out var format))
        {
            return new ExplorerContextMenuResult(
                false,
                LocalizationService.Current.GetString("ExplorerContextMenu.Error.UnsupportedFormat"));
        }

        try
        {
            if (isEnabled)
            {
                Register(format);
                return IsRegistered(format.Extension)
                    ? new ExplorerContextMenuResult(
                        true,
                        LocalizationService.Current.Format(
                            "ExplorerContextMenu.Status.FormatRegistered",
                            format.DisplayName))
                    : new ExplorerContextMenuResult(
                        false,
                        LocalizationService.Current.Format(
                            "ExplorerContextMenu.Error.FormatRegistrationFailed",
                            format.DisplayName));
            }

            Unregister(format);
            return IsRegistered(format.Extension)
                ? new ExplorerContextMenuResult(
                    false,
                    LocalizationService.Current.Format(
                        "ExplorerContextMenu.Error.FormatRemovalFailed",
                        format.DisplayName))
                : new ExplorerContextMenuResult(
                    true,
                    LocalizationService.Current.Format(
                        "ExplorerContextMenu.Status.FormatRemoved",
                        format.DisplayName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new ExplorerContextMenuResult(
                false,
                LocalizationService.Current.GetString("ExplorerContextMenu.Error.SettingFailed"));
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
        var menuText = LocalizationService.Current.GetString("ExplorerContextMenu.Verb.OpenInComicPlate");
        _registry.WriteDefaultValue(shellKeyPath, menuText);
        _registry.WriteValue(shellKeyPath, "MUIVerb", menuText);
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
