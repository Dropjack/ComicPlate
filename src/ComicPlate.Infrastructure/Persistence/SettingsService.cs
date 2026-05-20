using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComicPlate.Infrastructure.Persistence;

public sealed class SettingsService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _userDataDirectory;

    public SettingsService(IUserDataPathProvider userDataPathProvider)
        : this(userDataPathProvider.GetUserDataDirectory())
    {
    }

    public SettingsService(string userDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(userDataDirectory))
        {
            throw new ArgumentException("User data directory cannot be empty.", nameof(userDataDirectory));
        }

        _userDataDirectory = userDataDirectory;
    }

    public static SettingsService CreateDefault()
    {
        return new SettingsService(new DefaultUserDataPathProvider());
    }

    public string SettingsPath => Path.Combine(_userDataDirectory, "settings.json");

    public string UserDataDirectory => _userDataDirectory;

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return AppSettings.Default;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath, Utf8NoBom);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return Normalize(settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        var normalized = Normalize(settings);
        var directory = Path.GetDirectoryName(SettingsPath)!;
        var tempPath = Path.Combine(directory, $"{Path.GetFileName(SettingsPath)}.tmp");

        try
        {
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(normalized, JsonOptions);
            File.WriteAllText(tempPath, json, Utf8NoBom);
            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteTempFile(tempPath);
        }
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        if (settings is null || settings.Version != AppSettings.CurrentVersion)
        {
            return AppSettings.Default;
        }

        var defaults = AppSettings.Default;
        var mainWindow = settings.MainWindow ?? WindowPlacementSettings.Default;

        return settings with
        {
            Version = AppSettings.CurrentVersion,
            ProgressLimit = settings.ProgressLimit > 0 ? settings.ProgressLimit : defaults.ProgressLimit,
            DefaultFitMode = string.IsNullOrWhiteSpace(settings.DefaultFitMode)
                ? defaults.DefaultFitMode
                : settings.DefaultFitMode,
            MainWindow = mainWindow with
            {
                Width = IsReasonableSize(mainWindow.Width) ? mainWindow.Width : defaults.MainWindow.Width,
                Height = IsReasonableSize(mainWindow.Height) ? mainWindow.Height : defaults.MainWindow.Height,
            },
            SidebarWidth = settings.SidebarWidth is > 0 ? settings.SidebarWidth : null
        };
    }

    private static bool IsReasonableSize(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 320 && value <= 10000;
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // Settings writes should not make shutdown fail.
        }
    }
}
