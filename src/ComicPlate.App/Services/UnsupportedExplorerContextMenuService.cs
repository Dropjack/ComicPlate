using ComicPlate.Core.Books;

namespace ComicPlate.App.Services;

public sealed class UnsupportedExplorerContextMenuService : IExplorerContextMenuService
{
    public ExplorerContextMenuState GetState()
    {
        return new ExplorerContextMenuState(
            false,
            false,
            LocalizationService.Current.GetString("ExplorerContextMenu.Status.PlatformUnsupported"));
    }

    public ExplorerContextMenuResult SetEnabled(bool isEnabled)
    {
        return new ExplorerContextMenuResult(
            false,
            LocalizationService.Current.GetString("ExplorerContextMenu.Status.PlatformUnsupported"));
    }
    public IReadOnlyList<ExplorerContextMenuOption> GetSupportedOptions()
    {
        return ComicArchiveFormats.SupportedFormats
            .Select(format => new ExplorerContextMenuOption(
                format.Extension,
                format.DisplayName,
                false,
                false,
                LocalizationService.Current.GetString("ExplorerContextMenu.Status.PlatformUnsupported")))
            .Append(new ExplorerContextMenuOption(
                PdfBookFormat.Extension,
                PdfBookFormat.Label,
                false,
                false,
                LocalizationService.Current.GetString("ExplorerContextMenu.Status.PlatformUnsupported")))
            .Append(new ExplorerContextMenuOption(
                EpubBookFormat.Extension,
                EpubBookFormat.Label,
                false,
                false,
                LocalizationService.Current.GetString("ExplorerContextMenu.Status.PlatformUnsupported")))
            .ToArray();
    }

    public ExplorerContextMenuResult SetEnabled(string extension, bool isEnabled)
    {
        return new ExplorerContextMenuResult(
            false,
            LocalizationService.Current.GetString("ExplorerContextMenu.Status.PlatformUnsupported"));
    }
}
