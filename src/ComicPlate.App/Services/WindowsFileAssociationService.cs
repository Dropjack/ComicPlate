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
                ? "已关联到 ComicPlate。"
                : "未关联。");
    }

    public FileAssociationResult Associate(string extension)
    {
        if (!ComicArchiveFormats.TryGetByExtension(extension, out var format))
        {
            return new FileAssociationResult(false, "不支持的文件格式。");
        }

        try
        {
            var progId = GetProgId(format);
            WriteAssociation(format, progId);
            return IsAssociated(format.Extension)
                ? new FileAssociationResult(true, $"{format.DisplayName} 已关联到 ComicPlate。")
                : new FileAssociationResult(false, $"{format.DisplayName} 已注册；请在 Windows 默认应用中确认 ComicPlate。");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new FileAssociationResult(false, "文件关联失败，请检查系统权限。");
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
        var commandKey = $@"{progIdKey}\shell\open\command";

        _registry.WriteDefaultValue(extensionKey, progId);
        _registry.WriteDefaultValue(progIdKey, $"{ApplicationName} {format.DisplayName} File");
        _registry.WriteValue(progIdKey, "FriendlyTypeName", $"{format.DisplayName} 漫画压缩包");
        _registry.WriteDefaultValue(commandKey, CreateOpenCommand());
    }

    private string CreateOpenCommand()
    {
        return $"\"{_executablePath}\" \"%1\"";
    }

    private static string GetProgId(ComicArchiveFormat format)
    {
        return GetProgId(format.Extension);
    }

    private static string GetProgId(string extension)
    {
        return $"{ProgIdPrefix}{NormalizeExtension(extension)}";
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
