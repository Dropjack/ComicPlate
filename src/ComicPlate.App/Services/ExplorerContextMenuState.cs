namespace ComicPlate.App.Services;

public sealed record ExplorerContextMenuState(
    bool IsSupported,
    bool IsRegistered,
    string StatusText);

