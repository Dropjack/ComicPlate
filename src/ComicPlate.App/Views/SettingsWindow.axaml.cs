using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComicPlate.App.Input;
using ComicPlate.App.Services;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.Views;

public partial class SettingsWindow : Window
{
    private const double CompactContentWidth = 640;
    private readonly IPlatformLauncher _platformLauncher;
    private readonly SettingsService _settingsService;
    private AppSettings _settings = AppSettings.Default;
    private bool _isLoadingSettings;
    private ShortcutWindow? _shortcutWindow;

    public SettingsWindow()
        : this(new PlatformLauncher(), null)
    {
    }

    public SettingsWindow(IPlatformLauncher platformLauncher)
        : this(platformLauncher, null)
    {
    }

    public SettingsWindow(IPlatformLauncher platformLauncher, SettingsService? settingsService)
    {
        _platformLauncher = platformLauncher;
        _settingsService = settingsService ?? SettingsService.CreateDefault();
        InitializeComponent();
        var isMacOS = OperatingSystem.IsMacOS();
        Classes.Add(isMacOS ? "mac-shell" : "windows-shell");
        ApplyPlatformChrome(isMacOS);
        LoadSettingsIntoControls();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        Closed += OnClosed;
        ContentScrollViewer.SizeChanged += OnContentScrollViewerSizeChanged;
    }

    public string DataFolderPath => _settingsService.UserDataDirectory;

    private void ApplyPlatformChrome(bool isMacOS)
    {
        if (isMacOS)
        {
            Width = 780;
            Height = 600;
            MinWidth = 640;
            MinHeight = 480;
            SettingsRootGrid.ColumnDefinitions = new ColumnDefinitions("190,*");
            DataFolderOpenButton.Content = "在 Finder 中打开";
            CbzAssociationDescription.Text = "允许从 Finder 直接用 ComicPlate 打开 .cbz 文件。";
            ZipAssociationDescription.Text = "允许从 Finder 直接用 ComicPlate 打开 .zip 漫画压缩包。图片格式暂不进入文件关联设置。";
            return;
        }

        DataFolderOpenButton.Content = "在资源管理器中打开";
        CbzAssociationDescription.Text = "允许从资源管理器直接用 ComicPlate 打开 .cbz 文件。";
        ZipAssociationDescription.Text = "允许从资源管理器直接用 ComicPlate 打开 .zip 漫画压缩包。图片格式暂不进入文件关联设置。";
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
        UpdateResponsiveRows();
    }

    private void OnContentScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateResponsiveRows();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _shortcutWindow?.Close();
        _shortcutWindow = null;
    }

    private void OnStartupNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(StartupNavButton);
        ScrollToSection(StartupSection);
    }

    private void OnAppearanceNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(AppearanceNavButton);
        ScrollToSection(AppearanceSection);
    }

    private void OnDataNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(DataNavButton);
        ScrollToSection(DataSection);
    }

    private void OnAssociationNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(AssociationNavButton);
        ScrollToSection(AssociationSection);
    }

    private void OnShortcutsNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(ShortcutsNavButton);
        ScrollToSection(ShortcutsSection);
    }

    private void OnOpenShortcutsClick(object? sender, RoutedEventArgs e)
    {
        ShowShortcutWindow();
        e.Handled = true;
    }

    private void OnSettingToggleClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        SaveSettingsFromControls();
    }

    private void OnOpenDataFolderClick(object? sender, RoutedEventArgs e)
    {
        _platformLauncher.OpenFolder(DataFolderPath);
        e.Handled = true;
    }

    private void SelectNav(Button selectedButton)
    {
        var navButtons = new[]
        {
            StartupNavButton,
            AppearanceNavButton,
            DataNavButton,
            AssociationNavButton,
            ShortcutsNavButton,
        };

        foreach (var button in navButtons)
        {
            button.Classes.Remove("selected");
        }

        selectedButton.Classes.Add("selected");
    }

    private void ScrollToSection(Control section)
    {
        var point = section.TranslatePoint(new Point(0, 0), ContentStack);
        if (point is null)
        {
            return;
        }

        ContentScrollViewer.Offset = ContentScrollViewer.Offset.WithY(point.Value.Y);
    }

    private void UpdateResponsiveRows()
    {
        var contentWidth = Math.Max(
            0,
            ContentScrollViewer.Bounds.Width - ContentScrollViewer.Padding.Left - ContentScrollViewer.Padding.Right);

        ContentStack.Width = contentWidth;
        BottomScrollSpacer.Height = Math.Max(0, ContentScrollViewer.Bounds.Height - 140);

        var compact = contentWidth < CompactContentWidth;
        var rows = new[]
        {
            MultiWindowRow,
            RestoreWindowRow,
            PaletteRow,
            DataFolderRow,
            ThumbnailCacheRow,
            CbzAssociationRow,
            ZipAssociationRow,
            ShortcutsRow,
        };

        foreach (var row in rows)
        {
            ApplyResponsiveRow(row, compact);
        }
    }

    private static void ApplyResponsiveRow(Grid row, bool compact)
    {
        if (row.Children.Count < 2 || row.Children[1] is not Control control)
        {
            return;
        }

        if (compact)
        {
            row.ColumnDefinitions = new ColumnDefinitions("*");
            row.RowDefinitions = new RowDefinitions("Auto,Auto");
            row.ColumnSpacing = 0;
            row.RowSpacing = 10;
            Grid.SetColumn(control, 0);
            Grid.SetRow(control, 1);
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Center;
            return;
        }

        row.ColumnDefinitions = new ColumnDefinitions("*,Auto");
        row.RowDefinitions = new RowDefinitions("Auto");
        row.ColumnSpacing = 16;
        row.RowSpacing = 0;
        Grid.SetColumn(control, 1);
        Grid.SetRow(control, 0);
        control.HorizontalAlignment = HorizontalAlignment.Right;
        control.VerticalAlignment = VerticalAlignment.Center;
    }

    private void ShowShortcutWindow()
    {
        if (_shortcutWindow is not null)
        {
            _shortcutWindow.Activate();
            return;
        }

        _shortcutWindow = new ShortcutWindow();
        _shortcutWindow.Closed += OnShortcutWindowClosed;
        _shortcutWindow.Show();
    }

    private void OnShortcutWindowClosed(object? sender, EventArgs e)
    {
        if (_shortcutWindow is not null)
        {
            _shortcutWindow.Closed -= OnShortcutWindowClosed;
            _shortcutWindow = null;
        }
    }

    private void LoadSettingsIntoControls()
    {
        _isLoadingSettings = true;
        try
        {
            _settings = _settingsService.Load();
            MultiWindowToggle.IsChecked = _settings.AllowMultipleWindows;
            RestoreWindowToggle.IsChecked = _settings.RestoreWindowPlacement;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void SaveSettingsFromControls()
    {
        _settings = _settings with
        {
            AllowMultipleWindows = MultiWindowToggle.IsChecked == true,
            RestoreWindowPlacement = RestoreWindowToggle.IsChecked == true,
        };

        _settingsService.Save(_settings);
    }
}
