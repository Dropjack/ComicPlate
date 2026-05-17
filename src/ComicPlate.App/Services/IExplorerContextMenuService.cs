namespace ComicPlate.App.Services;

public interface IExplorerContextMenuService
{
    ExplorerContextMenuState GetState();

    ExplorerContextMenuResult SetEnabled(bool isEnabled);
}

