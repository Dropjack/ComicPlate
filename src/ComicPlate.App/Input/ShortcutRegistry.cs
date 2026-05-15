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
        new ShortcutDefinition(ShortcutActionId.NextPage, "下一页", "翻页与定位", Key.Right),
        new ShortcutDefinition(ShortcutActionId.PreviousPage, "上一页", "翻页与定位", Key.Left),
        new ShortcutDefinition(ShortcutActionId.FirstPage, "第一页", "翻页与定位", Key.Home),
        new ShortcutDefinition(ShortcutActionId.LastPage, "最后一页", "翻页与定位", Key.End),
        new ShortcutDefinition(ShortcutActionId.OpenContent, "打开漫画", "功能操作", Key.O, UsesPlatformCommandModifier: true),
        new ShortcutDefinition(ShortcutActionId.ToggleNavigationPane, "显示/隐藏 Shelf", "功能操作", Key.Tab),
        new ShortcutDefinition(ShortcutActionId.ToggleViewMode, "单页/双页切换", "功能操作", Key.Q),
        new ShortcutDefinition(ShortcutActionId.ToggleReadingDirection, "阅读方向", "功能操作", Key.R),
        new ShortcutDefinition(ShortcutActionId.ToggleFullscreen, "全屏", "功能操作", Key.F),
        new ShortcutDefinition(ShortcutActionId.OpenSettings, "设置", "功能操作", Key.OemComma, UsesPlatformCommandModifier: true),
        new ShortcutDefinition(ShortcutActionId.CloseWindow, "回到起始页", "功能操作", Key.W, UsesPlatformCommandModifier: true),
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
