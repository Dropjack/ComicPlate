using Avalonia.Input;

namespace ComicPlate.App.Input;

internal static class PlatformShortcuts
{
    private static KeyModifiers PlatformCommandModifier =>
        OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;

    public static bool IsOpenContent(KeyEventArgs e)
    {
        return HasPlatformCommandModifier(e) && e.Key == Key.O;
    }

    public static bool IsOpenSettings(KeyEventArgs e)
    {
        return HasPlatformCommandModifier(e) && e.Key == Key.OemComma;
    }

    public static bool IsCloseWindow(KeyEventArgs e)
    {
        return HasPlatformCommandModifier(e) && e.Key == Key.W;
    }

    private static bool HasPlatformCommandModifier(KeyEventArgs e)
    {
        return e.KeyModifiers.HasFlag(PlatformCommandModifier);
    }
}
