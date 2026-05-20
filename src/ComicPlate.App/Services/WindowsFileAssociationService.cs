using System.Diagnostics;
using System.Runtime.Versioning;
using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class WindowsFileAssociationService : IFileAssociationService
{
    private const string ClassesRoot = @"Software\Classes";
    private const string FileExtsRoot = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";
    private const string ApplicationName = "ComicPlate";
    private const string ProgIdPrefix = "ComicPlate";

    private readonly IWindowsRegistry _registry;
    private readonly string _executablePath;

    public WindowsFileAssociationService(IWindowsRegistry registry, string executablePath)
    {
        _registry = registry;
        _executablePath = executablePath;
    }

    [SupportedOSPlatform("windows")]
    public static WindowsFileAssociationService CreateDefault()
    {
        return new WindowsFileAssociationService(
            new WindowsRegistry(),
            GetCurrentExecutablePath());
    }

    public IReadOnlyList<FileAssociationOption> GetSupportedAssociations()
    {
        return FileAssociationService.CreateOptions(
            _ => true,
            IsAssociated,
            extension => IsAssociated(extension)
                ? LocalizationService.Current.GetString("FileAssociation.Status.Associated")
                : LocalizationService.Current.GetString("FileAssociation.Status.NotAssociated"));
    }

    public FileAssociationResult Associate(string extension)
    {
        if (!ComicArchiveFormats.TryGetByExtension(extension, out var format))
        {
            return new FileAssociationResult(
                false,
                LocalizationService.Current.GetString("FileAssociation.Error.UnsupportedFormat"));
        }

        try
        {
            var progId = GetProgId(format);
            WriteAssociation(format, progId);
            return IsAssociated(format.Extension)
                ? new FileAssociationResult(
                    true,
                    LocalizationService.Current.Format("FileAssociation.Result.Associated", format.DisplayName))
                : new FileAssociationResult(
                    false,
                    LocalizationService.Current.Format(
                        "FileAssociation.Result.RegisteredNeedsWindowsConfirmation",
                        format.DisplayName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new FileAssociationResult(
                false,
                LocalizationService.Current.GetString("FileAssociation.Error.AssociationFailed"));
        }
    }

    public FileAssociationResult Disassociate(string extension)
    {
        if (!ComicArchiveFormats.TryGetByExtension(extension, out var format))
        {
            return new FileAssociationResult(
                false,
                LocalizationService.Current.GetString("FileAssociation.Error.UnsupportedFormat"));
        }

        try
        {
            RemoveAssociation(format);
            return IsAssociated(format.Extension)
                ? new FileAssociationResult(
                    false,
                    LocalizationService.Current.Format(
                        "FileAssociation.Result.StillAssociatedByWindows",
                        format.DisplayName))
                : new FileAssociationResult(
                    true,
                    LocalizationService.Current.Format("FileAssociation.Result.Disassociated", format.DisplayName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new FileAssociationResult(
                false,
                LocalizationService.Current.GetString("FileAssociation.Error.DisassociationFailed"));
        }
    }

    private bool IsAssociated(string extension)
    {
        var userChoice = _registry.ReadValue($@"{FileExtsRoot}\{NormalizeExtension(extension)}\UserChoice", "ProgId");
        if (!string.IsNullOrWhiteSpace(userChoice))
        {
            return userChoice.Equals(GetProgId(extension), StringComparison.OrdinalIgnoreCase);
        }

        var current = _registry.ReadDefaultValue($@"{ClassesRoot}\{NormalizeExtension(extension)}");
        return current is not null
            && current.Equals(GetProgId(extension), StringComparison.OrdinalIgnoreCase);
    }

    private void WriteAssociation(ComicArchiveFormat format, string progId)
    {
        var extension = NormalizeExtension(format.Extension);
        var extensionKey = $@"{ClassesRoot}\{extension}";
        var progIdKey = $@"{ClassesRoot}\{progId}";
        var defaultIconKey = $@"{progIdKey}\DefaultIcon";
        var commandKey = $@"{progIdKey}\shell\open\command";

        _registry.WriteDefaultValue(extensionKey, progId);
        _registry.WriteDefaultValue(
            progIdKey,
            LocalizationService.Current.Format("FileAssociation.Windows.ProgIdDescription", format.DisplayName));
        _registry.WriteValue(
            progIdKey,
            "FriendlyTypeName",
            LocalizationService.Current.Format("FileAssociation.Windows.FriendlyTypeName", format.DisplayName));
        _registry.WriteDefaultValue(defaultIconKey, CreateIconReference());
        _registry.WriteDefaultValue(commandKey, CreateOpenCommand());
    }

    private void RemoveAssociation(ComicArchiveFormat format)
    {
        var extension = NormalizeExtension(format.Extension);
        var progId = GetProgId(format);
        var extensionKey = $@"{ClassesRoot}\{extension}";
        var userChoiceKey = $@"{FileExtsRoot}\{extension}\UserChoice";

        if (IsRegistryValue(progId, _registry.ReadValue(userChoiceKey, "ProgId")))
        {
            _registry.DeleteTree(userChoiceKey);
        }

        if (IsRegistryValue(progId, _registry.ReadDefaultValue(extensionKey)))
        {
            _registry.DeleteValue(extensionKey, "");
        }

        _registry.DeleteTree($@"{ClassesRoot}\{progId}");
    }

    private string CreateOpenCommand()
    {
        return $"\"{_executablePath}\" \"%1\"";
    }

    private string CreateIconReference()
    {
        return $"\"{_executablePath}\",0";
    }

    private static string GetProgId(ComicArchiveFormat format)
    {
        return GetProgId(format.Extension);
    }

    private static string GetProgId(string extension)
    {
        return $"{ProgIdPrefix}{NormalizeExtension(extension)}";
    }

    private static bool IsRegistryValue(string expected, string? actual)
    {
        return actual is not null
            && actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
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
