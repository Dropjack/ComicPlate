namespace ComicPlate.App.Services;

public interface IWindowsRegistry
{
    string? ReadDefaultValue(string keyPath);

    string? ReadValue(string keyPath, string valueName);

    void WriteDefaultValue(string keyPath, string value);

    void WriteValue(string keyPath, string valueName, string value);

    void DeleteValue(string keyPath, string valueName);

    void DeleteTree(string keyPath);
}
