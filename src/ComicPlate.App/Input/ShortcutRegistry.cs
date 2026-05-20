using Avalonia.Input;

namespace ComicPlate.App.Input;

internal static class ShortcutRegistry
{
    public static KeyModifiers PlatformCommandModifier =>
        OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;

    public static string PlatformName => OperatingSystem.IsMacOS() ? "macOS" : "Windows";

    public static IReadOnlyList<ShortcutDefinition> Definitions { get; } = new[]
    {
        new ShortcutDefinition(ShortcutActionId.NextPage, "Shortcuts.NextPage", "Shortcuts.Group.Navigation", Key.Right),
        new ShortcutDefinition(ShortcutActionId.PreviousPage, "Shortcuts.PreviousPage", "Shortcuts.Group.Navigation", Key.Left),
        new ShortcutDefinition(ShortcutActionId.FirstPage, "Shortcuts.FirstPage", "Shortcuts.Group.Navigation", Key.Home),
        new ShortcutDefinition(ShortcutActionId.LastPage, "Shortcuts.LastPage", "Shortcuts.Group.Navigation", Key.End),
        new ShortcutDefinition(ShortcutActionId.OpenContent, "Shortcuts.OpenComic", "Shortcuts.Group.Actions", Key.O, UsesPlatformCommandModifier: true),
        new ShortcutDefinition(ShortcutActionId.NewWindow, "Shortcuts.NewWindow", "Shortcuts.Group.Actions", Key.N, UsesPlatformCommandModifier: true),
        new ShortcutDefinition(ShortcutActionId.ToggleNavigationPane, "Shortcuts.ToggleShelf", "Shortcuts.Group.Actions", Key.Tab),
        new ShortcutDefinition(ShortcutActionId.ToggleViewMode, "Shortcuts.ToggleViewMode", "Shortcuts.Group.Actions", Key.Q),
        new ShortcutDefinition(ShortcutActionId.ToggleReadingDirection, "Shortcuts.ToggleReadingDirection", "Shortcuts.Group.Actions", Key.R),
        new ShortcutDefinition(ShortcutActionId.ToggleFullscreen, "Shortcuts.Fullscreen", "Shortcuts.Group.Actions", Key.F),
        new ShortcutDefinition(ShortcutActionId.OpenSettings, "Shortcuts.Settings", "Shortcuts.Group.Actions", Key.OemComma, UsesPlatformCommandModifier: true),
        new ShortcutDefinition(ShortcutActionId.CloseWindow, "Shortcuts.BackToStart", "Shortcuts.Group.Actions", Key.W, UsesPlatformCommandModifier: true),
    };

    public static ShortcutActionId? GetAction(KeyEventArgs e)
    {
        return Definitions.FirstOrDefault(definition => definition.Matches(e))?.ActionId;
    }

    public static string GetDisplayText(ShortcutActionId actionId)
    {
        return Definitions.Single(definition => definition.ActionId == actionId).GetDisplayText();
    }
}
