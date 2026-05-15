using Avalonia.Input;

namespace ComicPlate.App.Input;

internal static class PlatformShortcuts
{
    public static bool IsOpenContent(KeyEventArgs e)
    {
        return ShortcutRegistry.GetAction(e) == ShortcutActionId.OpenContent;
    }

    public static bool IsOpenSettings(KeyEventArgs e)
    {
        return ShortcutRegistry.GetAction(e) == ShortcutActionId.OpenSettings;
    }

    public static bool IsCloseWindow(KeyEventArgs e)
    {
        return ShortcutRegistry.GetAction(e) == ShortcutActionId.CloseWindow;
    }
}
