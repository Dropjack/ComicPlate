using Microsoft.Win32;
using System.Runtime.Versioning;

namespace ComicPlate.App.Services;

[SupportedOSPlatform("windows")]
public sealed class WindowsRegistry : IWindowsRegistry
{
    public string? ReadDefaultValue(string keyPath)
    {
        return ReadValue(keyPath, "");
    }

    public string? ReadValue(string keyPath, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: false);
        return key?.GetValue(string.IsNullOrEmpty(valueName) ? null : valueName) as string;
    }

    public void WriteDefaultValue(string keyPath, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(null, value);
    }

    public void WriteValue(string keyPath, string valueName, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(valueName, value);
    }

    public void DeleteValue(string keyPath, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    public void DeleteTree(string keyPath)
    {
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
    }
}
