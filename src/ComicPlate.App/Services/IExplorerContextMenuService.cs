namespace ComicPlate.App.Services;

public interface IExplorerContextMenuService
{
    ExplorerContextMenuState GetState();

    IReadOnlyList<ExplorerContextMenuOption> GetSupportedOptions();

    ExplorerContextMenuResult SetEnabled(bool isEnabled);

    ExplorerContextMenuResult SetEnabled(string extension, bool isEnabled);
}
