using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.Services;

public sealed class AppDataService : IAppDataService
{
    private readonly IPlatformLauncher _platformLauncher;

    public AppDataService(IUserDataPathProvider userDataPathProvider, IPlatformLauncher platformLauncher)
        : this(userDataPathProvider.GetUserDataDirectory(), platformLauncher)
    {
    }

    public AppDataService(string userDataDirectory, IPlatformLauncher platformLauncher)
    {
        UserDataDirectory = Path.GetFullPath(userDataDirectory);
        ThumbnailCacheDirectory = Path.Combine(UserDataDirectory, ThumbnailCacheService.CacheFolderName);
        _platformLauncher = platformLauncher;
    }

    public string UserDataDirectory { get; }

    public string ThumbnailCacheDirectory { get; }

    public static AppDataService CreateDefault(IPlatformLauncher? platformLauncher = null)
    {
        return new AppDataService(
            DefaultUserDataPathProvider.CreateForCurrentPlatform(),
            platformLauncher ?? new PlatformLauncher());
    }

    public void OpenUserDataDirectory()
    {
        _platformLauncher.OpenFolder(UserDataDirectory);
    }
}
