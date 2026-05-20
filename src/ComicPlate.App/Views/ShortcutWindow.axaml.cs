using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ComicPlate.App.Input;
using ComicPlate.App.Services;

namespace ComicPlate.App.Views;

public partial class ShortcutWindow : Window
{
    public ShortcutWindow()
    {
        InitializeComponent();
        ApplyPlatformShortcutText();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        ShortcutScrollViewer.SizeChanged += OnShortcutScrollViewerSizeChanged;
    }

    private void ApplyPlatformShortcutText()
    {
        ShortcutIntroText.Text = LocalizationService.Current.GetString("Shortcuts.Intro");
        NextPageShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.NextPage);
        PreviousPageShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.PreviousPage);
        FirstPageShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.FirstPage);
        LastPageShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.LastPage);
        OpenContentShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.OpenContent);
        ToggleShelfShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.ToggleNavigationPane);
        ToggleViewModeShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.ToggleViewMode);
        ToggleReadingDirectionShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.ToggleReadingDirection);
        FullscreenShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.ToggleFullscreen);
        SettingsShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.OpenSettings);
        CloseShortcutText.Text = ShortcutRegistry.GetDisplayText(ShortcutActionId.CloseWindow);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (PlatformShortcuts.IsCloseWindow(e))
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        UpdateBottomScrollSpacer();
    }

    private void OnShortcutScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateBottomScrollSpacer();
    }

    private void UpdateBottomScrollSpacer()
    {
        BottomScrollSpacer.Height = Math.Max(0, ShortcutScrollViewer.Bounds.Height - 140);
    }
}
