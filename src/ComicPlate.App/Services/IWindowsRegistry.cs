namespace ComicPlate.App.Services;

public interface IWindowsRegistry
{
    string? ReadDefaultValue(string keyPath);

    string? ReadValue(string keyPath, string valueName);

    void WriteDefaultValue(string keyPath, string value);

    void WriteValue(string keyPath, string valueName, string value);

    void DeleteTree(string keyPath);
}

