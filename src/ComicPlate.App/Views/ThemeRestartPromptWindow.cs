using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComicPlate.App.Services;

namespace ComicPlate.App.Views;

public sealed class ThemeRestartPromptWindow : Window
{
    public ThemeRestartPromptWindow()
        : this(
            LocalizationService.Current.GetString("Restart.Theme.Title"),
            LocalizationService.Current.GetString("Restart.Theme.Message"))
    {
    }

    public ThemeRestartPromptWindow(string titleText, string messageText)
    {
        var localization = LocalizationService.Current;

        Title = titleText;
        Width = 380;
        Height = 190;
        MinWidth = 340;
        MinHeight = 170;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("SurfacePanel", Brushes.White);

        var title = new TextBlock
        {
            Text = titleText,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("TextPrimary", Brushes.Black),
        };

        var message = new TextBlock
        {
            Text = messageText,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("TextSecondary", Brushes.DimGray),
            Margin = new Thickness(0, 8, 0, 0),
        };

        var restartButton = new Button
        {
            Content = localization.GetString("Common.Restart"),
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        restartButton.Click += (_, _) => Close(true);

        var laterButton = new Button
        {
            Content = localization.GetString("Common.Later"),
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        laterButton.Click += (_, _) => Close(false);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children =
            {
                laterButton,
                restartButton,
            }
        };

        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(22),
            Children =
            {
                new StackPanel
                {
                    Children =
                    {
                        title,
                        message,
                    }
                },
                buttons,
            }
        };
        Grid.SetRow(buttons, 1);
    }

    private static IBrush Brush(string key, IBrush fallback)
    {
        return Application.Current?.Resources.TryGetResource(key, null, out var value) == true
            && value is IBrush brush
                ? brush
                : fallback;
    }
}
