using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ComicPlate.App.Views;

public partial class SettingsWindow : Window
{
    private const double CompactContentWidth = 640;
    private ShortcutWindow? _shortcutWindow;

    public SettingsWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closed += OnClosed;
        ContentScrollViewer.SizeChanged += OnContentScrollViewerSizeChanged;
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
}
