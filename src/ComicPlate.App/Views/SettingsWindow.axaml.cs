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
    private readonly IAppDataService _appDataService;
    private readonly IExplorerContextMenuService _explorerContextMenuService;
    private readonly IFileAssociationService _fileAssociationService;
    private readonly SettingsService _settingsService;
    private readonly ThumbnailCacheService _thumbnailCacheService;
    private AppSettings _settings = AppSettings.Default;
    private bool _hideFileIntegrationSettings;
    private bool _isLoadingSettings;
    private bool _isShowingRestartPrompt;
    private ShortcutWindow? _shortcutWindow;

    public event EventHandler? RestartRequested;

    public SettingsWindow()
        : this(AppDataService.CreateDefault(), null, null)
    {
    }

    public SettingsWindow(IPlatformLauncher platformLauncher)
        : this(AppDataService.CreateDefault(platformLauncher), null, null)
    {
    }

    public SettingsWindow(IPlatformLauncher platformLauncher, SettingsService? settingsService)
        : this(
            settingsService is null
                ? AppDataService.CreateDefault(platformLauncher)
                : new AppDataService(settingsService.UserDataDirectory, platformLauncher),
            settingsService,
            null)
    {
    }

    public SettingsWindow(IAppDataService appDataService, SettingsService? settingsService)
        : this(appDataService, settingsService, null)
    {
    }

    public SettingsWindow(
        IAppDataService appDataService,
        SettingsService? settingsService,
        IFileAssociationService? fileAssociationService,
        IExplorerContextMenuService? explorerContextMenuService = null)
    {
        _appDataService = appDataService;
        _explorerContextMenuService = explorerContextMenuService ?? ExplorerContextMenuService.CreateDefault();
        _fileAssociationService = fileAssociationService ?? FileAssociationService.CreateDefault();
        _settingsService = settingsService ?? SettingsService.CreateDefault();
        _thumbnailCacheService = new ThumbnailCacheService(_appDataService.UserDataDirectory);
        InitializeComponent();
        var isMacOS = OperatingSystem.IsMacOS();
        _hideFileIntegrationSettings = isMacOS;
        Classes.Add(isMacOS ? "mac-shell" : "windows-shell");
        ApplyPlatformChrome(isMacOS);
        ApplyLocalizedText();
        LoadSettingsIntoControls();
        ColorThemeComboBox.SelectionChanged += OnColorThemeSelectionChanged;
        LanguageComboBox.SelectionChanged += OnLanguageSelectionChanged;
        ApplyFileIntegrationVisibility();
        if (!_hideFileIntegrationSettings)
        {
            LoadFileAssociationOptions();
            LoadExplorerContextMenuState();
        }

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        Closed += OnClosed;
        ContentScrollViewer.SizeChanged += OnContentScrollViewerSizeChanged;
    }

    private void ApplyPlatformChrome(bool isMacOS)
    {
        if (isMacOS)
        {
            Width = 780;
            Height = 600;
            MinWidth = 640;
            MinHeight = 480;
            SettingsRootGrid.ColumnDefinitions = new ColumnDefinitions("190,*");
            DataFolderOpenButton.Content = LocalizationService.Current.GetString("Settings.DataFolder.OpenInFinder");
            return;
        }

        DataFolderOpenButton.Content = LocalizationService.Current.GetString("Settings.DataFolder.OpenInExplorer");
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

    private void OnReadingNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(ReadingNavButton);
        ScrollToSection(ReadingSection);
    }

    private void OnDataNavClick(object? sender, RoutedEventArgs e)
    {
        SelectNav(DataNavButton);
        ScrollToSection(DataSection);
    }

    private void OnAssociationNavClick(object? sender, RoutedEventArgs e)
    {
        if (_hideFileIntegrationSettings)
        {
            return;
        }

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

    private async void OnColorThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || _isShowingRestartPrompt)
        {
            return;
        }

        var selectedTheme = GetSelectedColorTheme();
        if (selectedTheme == _settings.ColorTheme)
        {
            return;
        }

        SaveSettingsFromControls();
        await PromptRestartForThemeChangeAsync();
    }

    private async void OnLanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || _isShowingRestartPrompt)
        {
            return;
        }

        var selectedLanguage = GetSelectedLanguage();
        if (selectedLanguage == _settings.Language)
        {
            return;
        }

        SaveSettingsFromControls();
        await PromptRestartForLanguageChangeAsync();
    }

    private void OnOpenDataFolderClick(object? sender, RoutedEventArgs e)
    {
        _appDataService.OpenUserDataDirectory();
        e.Handled = true;
    }

    private void OnClearThumbnailCacheClick(object? sender, RoutedEventArgs e)
    {
        _thumbnailCacheService.Clear();
        ThumbnailCacheStatusText.Text = LocalizationService.Current.GetString("Settings.ThumbnailCache.Cleared");
        e.Handled = true;
    }

    private void OnAssociateFileClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control || control.Tag is not string extension)
        {
            return;
        }

        var shouldAssociate = sender is CheckBox { IsChecked: true };
        var result = shouldAssociate
            ? _fileAssociationService.Associate(extension)
            : _fileAssociationService.Disassociate(extension);
        LoadFileAssociationOptions();
        SetAssociationStatus(extension, result.Message);
        e.Handled = true;
    }

    private void OnExplorerContextMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control || control.Tag is not string extension)
        {
            return;
        }

        var shouldRegister = sender is CheckBox { IsChecked: true };
        var result = _explorerContextMenuService.SetEnabled(extension, shouldRegister);
        LoadExplorerContextMenuState();
        ExplorerContextMenuStatusText.Text = result.Message;
        e.Handled = true;
    }

    private void SelectNav(Button selectedButton)
    {
        var navButtons = new[]
        {
            StartupNavButton,
            AppearanceNavButton,
            ReadingNavButton,
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

    private void ApplyFileIntegrationVisibility()
    {
        AssociationNavButton.IsVisible = !_hideFileIntegrationSettings;
        AssociationSection.IsVisible = !_hideFileIntegrationSettings;
        ExplorerContextMenuSection.IsVisible = !_hideFileIntegrationSettings;
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
            LanguageRow,
            MagnifierRow,
            DataFolderRow,
            ThumbnailCacheRow,
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
            MagnifierToggle.IsChecked = _settings.IsMagnifierEnabled;
            ColorThemeComboBox.SelectedIndex = GetColorThemeIndex(_settings.ColorTheme);
            LanguageComboBox.SelectedIndex = GetLanguageIndex(_settings.Language);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void ApplyLocalizedText()
    {
        var localization = LocalizationService.Current;
        LanguageLabelText.Text = localization.GetString("Settings.Language.Label");
        LanguageDescriptionText.Text = localization.GetString("Settings.Language.Description");

        SetComboBoxItemContent(LanguageComboBox, 0, localization.GetString("Settings.Language.System"));
        SetComboBoxItemContent(LanguageComboBox, 1, localization.GetString("Settings.Language.English"));
        SetComboBoxItemContent(LanguageComboBox, 2, localization.GetString("Settings.Language.SimplifiedChinese"));
        SetComboBoxItemContent(LanguageComboBox, 3, localization.GetString("Settings.Language.Japanese"));
    }

    private void LoadFileAssociationOptions()
    {
        var options = _fileAssociationService.GetSupportedAssociations();
        foreach (var option in options)
        {
            SetAssociationOption(option.Extension, option.CanAssociate, option.IsAssociated);
        }

        AssociationStatusText.Text = options.Any(option => !option.CanAssociate)
            ? options.First(option => !option.CanAssociate).StatusText
            : "";
    }

    private void LoadExplorerContextMenuState()
    {
        var state = _explorerContextMenuService.GetState();
        ExplorerContextMenuSection.IsVisible = state.IsSupported;
        var options = _explorerContextMenuService.GetSupportedOptions();
        foreach (var option in options)
        {
            SetExplorerContextMenuOption(option.Extension, option.CanRegister, option.IsRegistered);
        }

        ExplorerContextMenuStatusText.Text = state.IsSupported ? "" : state.StatusText;
    }

    private void SetAssociationOption(string extension, bool canAssociate, bool isAssociated)
    {
        var checkBox = GetAssociationCheckBox(extension);
        if (checkBox is null)
        {
            return;
        }

        checkBox.IsEnabled = canAssociate;
        checkBox.IsChecked = isAssociated;
    }

    private void SetAssociationStatus(string extension, string status)
    {
        AssociationStatusText.Text = status;
    }

    private void SetExplorerContextMenuOption(string extension, bool canRegister, bool isRegistered)
    {
        var checkBox = GetExplorerContextMenuCheckBox(extension);
        if (checkBox is null)
        {
            return;
        }

        checkBox.IsEnabled = canRegister;
        checkBox.IsChecked = isRegistered;
    }

    private CheckBox? GetAssociationCheckBox(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cbz" => CbzAssociationCheckBox,
            ".cbr" => CbrAssociationCheckBox,
            _ => null
        };
    }

    private CheckBox? GetExplorerContextMenuCheckBox(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cbz" => ExplorerContextMenuCbzCheckBox,
            ".cbr" => ExplorerContextMenuCbrCheckBox,
            ".zip" => ExplorerContextMenuZipCheckBox,
            ".rar" => ExplorerContextMenuRarCheckBox,
            ".pdf" => ExplorerContextMenuPdfCheckBox,
            ".epub" => ExplorerContextMenuEpubCheckBox,
            _ => null
        };
    }

    private void SaveSettingsFromControls()
    {
        _settings = _settings with
        {
            AllowMultipleWindows = MultiWindowToggle.IsChecked == true,
            RestoreWindowPlacement = RestoreWindowToggle.IsChecked == true,
            IsMagnifierEnabled = MagnifierToggle.IsChecked == true,
            ColorTheme = GetSelectedColorTheme(),
            Language = GetSelectedLanguage(),
        };

        _settingsService.Save(_settings);
    }

    private async Task PromptRestartForThemeChangeAsync()
    {
        _isShowingRestartPrompt = true;
        try
        {
            var prompt = new ThemeRestartPromptWindow();
            var shouldRestart = await prompt.ShowDialog<bool?>(this);
            if (shouldRestart == true)
            {
                RestartRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            _isShowingRestartPrompt = false;
        }
    }

    private async Task PromptRestartForLanguageChangeAsync()
    {
        _isShowingRestartPrompt = true;
        try
        {
            var localization = LocalizationService.Current;
            var prompt = new ThemeRestartPromptWindow(
                localization.GetString("Restart.Language.Title"),
                localization.GetString("Restart.Language.Message"));
            var shouldRestart = await prompt.ShowDialog<bool?>(this);
            if (shouldRestart == true)
            {
                RestartRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            _isShowingRestartPrompt = false;
        }
    }

    private AppColorTheme GetSelectedColorTheme()
    {
        return ColorThemeComboBox.SelectedIndex switch
        {
            1 => AppColorTheme.SlateBlue,
            2 => AppColorTheme.WarmPaper,
            3 => AppColorTheme.NightGraphite,
            _ => AppColorTheme.MistGreen,
        };
    }

    private static int GetColorThemeIndex(AppColorTheme theme)
    {
        return theme switch
        {
            AppColorTheme.SlateBlue => 1,
            AppColorTheme.WarmPaper => 2,
            AppColorTheme.NightGraphite => 3,
            _ => 0,
        };
    }

    private AppLanguage GetSelectedLanguage()
    {
        return LanguageComboBox.SelectedIndex switch
        {
            1 => AppLanguage.English,
            2 => AppLanguage.SimplifiedChinese,
            3 => AppLanguage.Japanese,
            _ => AppLanguage.System,
        };
    }

    private static int GetLanguageIndex(AppLanguage language)
    {
        return language switch
        {
            AppLanguage.English => 1,
            AppLanguage.SimplifiedChinese => 2,
            AppLanguage.Japanese => 3,
            _ => 0,
        };
    }

    private static void SetComboBoxItemContent(ComboBox comboBox, int index, string content)
    {
        if (comboBox.Items[index] is ComboBoxItem item)
        {
            item.Content = content;
        }
    }
}
