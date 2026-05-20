using System.Text;
using ComicPlate.Core.Reading;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.Persistence;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ComicPlateSettingsTests-{Guid.NewGuid():N}");

    public SettingsServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void LoadReturnsDefaultWhenSettingsFileIsMissing()
    {
        var service = CreateService();

        var settings = service.Load();

        Assert.Equal(AppSettings.Default, settings);
        Assert.True(settings.AllowMultipleWindows);
    }

    [Fact]
    public void SaveWritesUtf8NoBomJsonUnderUserDataDirectory()
    {
        var service = CreateService();
        var settings = AppSettings.Default with
        {
            MainWindow = new WindowPlacementSettings
            {
                Width = 1440,
                Height = 900,
                X = 40,
                Y = 50,
            }
        };

        service.Save(settings);

        Assert.True(File.Exists(Path.Combine(_tempDirectory, "settings.json")));
        var bytes = File.ReadAllBytes(Path.Combine(_tempDirectory, "settings.json"));
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Contains("\"Width\": 1440", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void LoadReadsSavedWindowPlacement()
    {
        var service = CreateService();
        service.Save(AppSettings.Default with
        {
            MainWindow = new WindowPlacementSettings
            {
                Width = 1000,
                Height = 700,
                X = 120,
                Y = 80,
            }
        });

        var loaded = service.Load();

        Assert.Equal(1000, loaded.MainWindow.Width);
        Assert.Equal(700, loaded.MainWindow.Height);
        Assert.Equal(120, loaded.MainWindow.X);
        Assert.Equal(80, loaded.MainWindow.Y);
    }

    [Fact]
    public void LoadReadsSavedBasicSettings()
    {
        var service = CreateService();
        service.Save(AppSettings.Default with
        {
            AllowMultipleWindows = true,
            RestoreWindowPlacement = false,
            ReadingDirection = ReadingDirection.LeftToRight,
            ViewMode = ViewMode.DoublePage,
            ColorTheme = AppColorTheme.NightGraphite,
            IsMagnifierEnabled = false,
        });

        var loaded = service.Load();

        Assert.True(loaded.AllowMultipleWindows);
        Assert.False(loaded.RestoreWindowPlacement);
        Assert.Equal(ReadingDirection.LeftToRight, loaded.ReadingDirection);
        Assert.Equal(ViewMode.DoublePage, loaded.ViewMode);
        Assert.Equal(AppColorTheme.NightGraphite, loaded.ColorTheme);
        Assert.False(loaded.IsMagnifierEnabled);
    }

    [Fact]
    public void LoadFallsBackWhenSettingsFileIsCorrupt()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "settings.json"), "{ nope", Encoding.UTF8);

        var settings = CreateService().Load();

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public void LoadFallsBackForOldVersionSettings()
    {
        File.WriteAllText(
            Path.Combine(_tempDirectory, "settings.json"),
            """
            {
              "Version": 0,
              "MainWindow": {
                "Width": 1600,
                "Height": 1000
              }
            }
            """,
            Encoding.UTF8);

        var settings = CreateService().Load();

        Assert.Equal(AppSettings.Default, settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private SettingsService CreateService()
    {
        return new SettingsService(_tempDirectory);
    }
}
