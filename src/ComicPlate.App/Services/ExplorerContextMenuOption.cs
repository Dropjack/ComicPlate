namespace ComicPlate.App.Services;

public sealed record ExplorerContextMenuOption(
    string Extension,
    string DisplayName,
    bool CanRegister,
    bool IsRegistered,
    string StatusText);
