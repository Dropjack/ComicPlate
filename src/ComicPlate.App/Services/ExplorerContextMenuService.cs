namespace ComicPlate.App.Services;

public static class ExplorerContextMenuService
{
    public static IExplorerContextMenuService CreateDefault()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsExplorerContextMenuService.CreateDefault();
        }

        return new UnsupportedExplorerContextMenuService();
    }
}

