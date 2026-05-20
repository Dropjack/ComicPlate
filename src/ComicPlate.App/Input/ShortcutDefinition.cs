using Avalonia.Input;

namespace ComicPlate.App.Input;

internal sealed record ShortcutDefinition(
    ShortcutActionId ActionId,
    string DisplayNameKey,
    string GroupNameKey,
    Key Key,
    KeyModifiers Modifiers = KeyModifiers.None,
    bool UsesPlatformCommandModifier = false)
{
    public bool Matches(KeyEventArgs e)
    {
        var expectedModifiers = UsesPlatformCommandModifier
            ? ShortcutRegistry.PlatformCommandModifier
            : Modifiers;

        return e.Key == Key && HasExactRelevantModifiers(e.KeyModifiers, expectedModifiers);
    }

    public string GetDisplayText()
    {
        var modifiers = UsesPlatformCommandModifier
            ? ShortcutRegistry.PlatformCommandModifier
            : Modifiers;

        var keyText = Key switch
        {
            Key.OemComma => ",",
            Key.Right => "Right",
            Key.Left => "Left",
            _ => Key.ToString(),
        };

        if (modifiers == KeyModifiers.None)
        {
            return keyText;
        }

        var modifierText = modifiers.HasFlag(KeyModifiers.Meta) ? "Cmd" : "Ctrl";
        return $"{modifierText} + {keyText}";
    }

    private static bool HasExactRelevantModifiers(KeyModifiers actual, KeyModifiers expected)
    {
        var relevant = KeyModifiers.Control | KeyModifiers.Meta | KeyModifiers.Alt | KeyModifiers.Shift;
        return (actual & relevant) == expected;
    }
}
