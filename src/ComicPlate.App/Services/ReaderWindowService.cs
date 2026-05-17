using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.Services;

public sealed class ReaderWindowService : IReaderWindowService
{
    private readonly SettingsService _settingsService;

    public ReaderWindowService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public static ReaderWindowService CreateDefault()
    {
        return new ReaderWindowService(SettingsService.CreateDefault());
    }

    public void ShowEmptyWindow()
    {
        ShowWindow(null);
    }

    public void ShowPathInNewWindow(string path)
    {
        ShowWindow(path);
    }

    private void ShowWindow(string? startupPath)
    {
        var window = new MainWindow(startupPath, _settingsService, this);
        window.Show();
        window.Activate();
    }
}
