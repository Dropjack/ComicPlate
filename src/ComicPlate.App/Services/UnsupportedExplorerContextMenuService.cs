namespace ComicPlate.App.Services;

public sealed class UnsupportedExplorerContextMenuService : IExplorerContextMenuService
{
    public ExplorerContextMenuState GetState()
    {
        return new ExplorerContextMenuState(
            false,
            false,
            "当前平台不支持在 ComicPlate 内注册资源管理器右键菜单。");
    }

    public ExplorerContextMenuResult SetEnabled(bool isEnabled)
    {
        return new ExplorerContextMenuResult(
            false,
            "当前平台不支持在 ComicPlate 内注册资源管理器右键菜单。");
    }
}

