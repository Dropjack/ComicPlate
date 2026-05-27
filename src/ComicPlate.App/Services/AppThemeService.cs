using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.Services;

public static class AppThemeService
{
    private static readonly IReadOnlyDictionary<AppColorTheme, ThemePalette> Palettes =
        new Dictionary<AppColorTheme, ThemePalette>
        {
            [AppColorTheme.MistGreen] = new(
                BackgroundBase: "#EEF4F7",
                SurfacePanel: "#F3F5F2",
                SurfaceMuted: "#E4EAE7",
                SurfaceElevated: "#FFFFFF",
                SurfaceInput: "#DCE5E1",
                ReaderStage: "#EEF4F7",
                ReaderPageSurface: "#FFFFFF",
                TextPrimary: "#1F2A2A",
                TextSecondary: "#667371",
                TextDisabled: "#A7B1AE",
                TextOnAccent: "#FFFFFF",
                TextInverse: "#FFFFFF",
                BorderSubtle: "#C9D4D0",
                Accent: "#4F7F6A",
                AccentHover: "#5F927B",
                ShelfHoverHighlight: "#E8EFEC",
                ShelfReadingHighlight: "#DCE6E2",
                ShelfNavigationHighlight: "#CAD8D3",
                OverlayDark: "#CC1F1F1F",
                MacFullscreenChrome: "#80F9F7F5",
                MacFullscreenChromeBorder: "#66FFFFFF",
                MacSidebarGlassTint: "#F3F5F2",
                MacSidebarGlassFallback: "#CCF3F5F2",
                MacSidebarGlassOverlay: "#D9F3F5F2",
                MacSidebarGlassBorder: "#99C9D4D0"),
            [AppColorTheme.SlateBlue] = new(
                BackgroundBase: "#EEF3F8",
                SurfacePanel: "#F6F8FA",
                SurfaceMuted: "#E3EAF1",
                SurfaceElevated: "#FFFFFF",
                SurfaceInput: "#D9E3EC",
                ReaderStage: "#EDF1F5",
                ReaderPageSurface: "#FFFFFF",
                TextPrimary: "#1C2733",
                TextSecondary: "#607080",
                TextDisabled: "#A3AFBA",
                TextOnAccent: "#FFFFFF",
                TextInverse: "#FFFFFF",
                BorderSubtle: "#C8D3DE",
                Accent: "#3F6F95",
                AccentHover: "#4E82AC",
                ShelfHoverHighlight: "#E7EEF5",
                ShelfReadingHighlight: "#DCE8F2",
                ShelfNavigationHighlight: "#C8D8E6",
                OverlayDark: "#CC1C2228",
                MacFullscreenChrome: "#80F7FAFC",
                MacFullscreenChromeBorder: "#66FFFFFF",
                MacSidebarGlassTint: "#F6F8FA",
                MacSidebarGlassFallback: "#CCF6F8FA",
                MacSidebarGlassOverlay: "#D9F6F8FA",
                MacSidebarGlassBorder: "#99C8D3DE"),
            [AppColorTheme.WarmPaper] = new(
                BackgroundBase: "#F4EFE7",
                SurfacePanel: "#FAF7F1",
                SurfaceMuted: "#E9E1D4",
                SurfaceElevated: "#FFFDF8",
                SurfaceInput: "#E3D9CA",
                ReaderStage: "#F0EAE1",
                ReaderPageSurface: "#FFFDF8",
                TextPrimary: "#2D2924",
                TextSecondary: "#756D62",
                TextDisabled: "#B1A89B",
                TextOnAccent: "#FFFFFF",
                TextInverse: "#FFFFFF",
                BorderSubtle: "#D5CABB",
                Accent: "#7A6A45",
                AccentHover: "#8B7A52",
                ShelfHoverHighlight: "#EFE8DD",
                ShelfReadingHighlight: "#E5DACB",
                ShelfNavigationHighlight: "#D8C8B4",
                OverlayDark: "#CC1E1A15",
                MacFullscreenChrome: "#80FFF9EF",
                MacFullscreenChromeBorder: "#66FFFFFF",
                MacSidebarGlassTint: "#FAF7F1",
                MacSidebarGlassFallback: "#CCFAF7F1",
                MacSidebarGlassOverlay: "#D9FAF7F1",
                MacSidebarGlassBorder: "#99D5CABB"),
            [AppColorTheme.NightGraphite] = new(
                BackgroundBase: "#171B1F",
                SurfacePanel: "#1F252A",
                SurfaceMuted: "#273037",
                SurfaceElevated: "#2D353C",
                SurfaceInput: "#222B32",
                ReaderStage: "#121518",
                ReaderPageSurface: "#1A1D20",
                TextPrimary: "#E6ECEF",
                TextSecondary: "#A4B0B7",
                TextDisabled: "#657178",
                TextOnAccent: "#FFFFFF",
                TextInverse: "#FFFFFF",
                BorderSubtle: "#35414A",
                Accent: "#6FA6B8",
                AccentHover: "#82B8CA",
                ShelfHoverHighlight: "#263038",
                ShelfReadingHighlight: "#2C3A43",
                ShelfNavigationHighlight: "#354A56",
                OverlayDark: "#CC000000",
                MacFullscreenChrome: "#80222A30",
                MacFullscreenChromeBorder: "#335F6A72",
                MacSidebarGlassTint: "#1F252A",
                MacSidebarGlassFallback: "#CC1F252A",
                MacSidebarGlassOverlay: "#D91F252A",
                MacSidebarGlassBorder: "#8035414A"),
        };

    private static readonly IReadOnlyDictionary<string, string> CompatibilityBrushes =
        new Dictionary<string, string>
        {
            ["PanelBrush"] = "SurfacePanel",
            ["SubtlePanelBrush"] = "SurfaceMuted",
            ["BorderBrushSoft"] = "BorderSubtle",
            ["TextBrush"] = "TextPrimary",
            ["MutedTextBrush"] = "TextSecondary",
            ["DisabledTextBrush"] = "TextDisabled",
            ["AccentBrush"] = "Accent",
            ["SettingsNavBrush"] = "SurfaceMuted",
            ["SettingsContentBrush"] = "BackgroundBase",
            ["SettingsCardBrush"] = "SurfacePanel",
            ["SettingsSelectedBrush"] = "SurfaceInput",
            ["SettingsBorderBrush"] = "BorderSubtle",
            ["SettingsTextBrush"] = "TextPrimary",
            ["SettingsMutedTextBrush"] = "TextSecondary",
            ["ShortcutCardBrush"] = "SurfacePanel",
            ["ShortcutBorderBrush"] = "BorderSubtle",
            ["ShortcutTextBrush"] = "TextPrimary",
            ["ShortcutMutedTextBrush"] = "TextSecondary",
        };

    public static void Apply(AppColorTheme theme)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        var palette = Palettes.TryGetValue(theme, out var selected)
            ? selected
            : Palettes[AppColorTheme.MistGreen];

        application.RequestedThemeVariant = theme == AppColorTheme.NightGraphite
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        foreach (var (token, color) in palette.Colors)
        {
            SetColorResource(application, $"{token}Color", color);
            SetBrushResource(application, token, color);
        }

        SetBrushResource(application, "MacFullscreenChromeBrush", palette.Colors[nameof(ThemePalette.MacFullscreenChrome)]);
        SetBrushResource(
            application,
            "MacFullscreenChromeBorderBrush",
            palette.Colors[nameof(ThemePalette.MacFullscreenChromeBorder)]);
        SetBrushResource(
            application,
            "MacSidebarGlassOverlayBrush",
            palette.Colors[nameof(ThemePalette.MacSidebarGlassOverlay)]);
        SetBrushResource(
            application,
            "MacSidebarGlassBorderBrush",
            palette.Colors[nameof(ThemePalette.MacSidebarGlassBorder)]);

        foreach (var (brushKey, token) in CompatibilityBrushes)
        {
            SetBrushResource(application, brushKey, palette.Colors[token]);
        }
    }

    private static void SetColorResource(Application application, string key, Color color)
    {
        application.Resources[key] = color;
    }

    private static void SetBrushResource(Application application, string key, Color color)
    {
        if (application.Resources.TryGetResource(key, null, out var existing)
            && existing is SolidColorBrush brush)
        {
            brush.Color = color;
            return;
        }

        application.Resources[key] = new SolidColorBrush(color);
    }

    private sealed record ThemePalette(
        string BackgroundBase,
        string SurfacePanel,
        string SurfaceMuted,
        string SurfaceElevated,
        string SurfaceInput,
        string ReaderStage,
        string ReaderPageSurface,
        string TextPrimary,
        string TextSecondary,
        string TextDisabled,
        string TextOnAccent,
        string TextInverse,
        string BorderSubtle,
        string Accent,
        string AccentHover,
        string ShelfHoverHighlight,
        string ShelfReadingHighlight,
        string ShelfNavigationHighlight,
        string OverlayDark,
        string MacFullscreenChrome,
        string MacFullscreenChromeBorder,
        string MacSidebarGlassTint,
        string MacSidebarGlassFallback,
        string MacSidebarGlassOverlay,
        string MacSidebarGlassBorder)
    {
        public IReadOnlyDictionary<string, Color> Colors { get; } = new Dictionary<string, Color>
        {
            [nameof(BackgroundBase)] = Color.Parse(BackgroundBase),
            [nameof(SurfacePanel)] = Color.Parse(SurfacePanel),
            [nameof(SurfaceMuted)] = Color.Parse(SurfaceMuted),
            [nameof(SurfaceElevated)] = Color.Parse(SurfaceElevated),
            [nameof(SurfaceInput)] = Color.Parse(SurfaceInput),
            [nameof(ReaderStage)] = Color.Parse(ReaderStage),
            [nameof(ReaderPageSurface)] = Color.Parse(ReaderPageSurface),
            [nameof(TextPrimary)] = Color.Parse(TextPrimary),
            [nameof(TextSecondary)] = Color.Parse(TextSecondary),
            [nameof(TextDisabled)] = Color.Parse(TextDisabled),
            [nameof(TextOnAccent)] = Color.Parse(TextOnAccent),
            [nameof(TextInverse)] = Color.Parse(TextInverse),
            [nameof(BorderSubtle)] = Color.Parse(BorderSubtle),
            [nameof(Accent)] = Color.Parse(Accent),
            [nameof(AccentHover)] = Color.Parse(AccentHover),
            [nameof(ShelfHoverHighlight)] = Color.Parse(ShelfHoverHighlight),
            [nameof(ShelfReadingHighlight)] = Color.Parse(ShelfReadingHighlight),
            [nameof(ShelfNavigationHighlight)] = Color.Parse(ShelfNavigationHighlight),
            [nameof(OverlayDark)] = Color.Parse(OverlayDark),
            [nameof(MacFullscreenChrome)] = Color.Parse(MacFullscreenChrome),
            [nameof(MacFullscreenChromeBorder)] = Color.Parse(MacFullscreenChromeBorder),
            [nameof(MacSidebarGlassTint)] = Color.Parse(MacSidebarGlassTint),
            [nameof(MacSidebarGlassFallback)] = Color.Parse(MacSidebarGlassFallback),
            [nameof(MacSidebarGlassOverlay)] = Color.Parse(MacSidebarGlassOverlay),
            [nameof(MacSidebarGlassBorder)] = Color.Parse(MacSidebarGlassBorder),
        };
    }
}
