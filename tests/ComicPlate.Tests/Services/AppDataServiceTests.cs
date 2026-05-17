using ComicPlate.App.Services;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.Services;

public sealed class AppDataServiceTests
{
    [Fact]
    public void ExposesThumbnailCacheAsChildOfUserDataDirectory()
    {
        var launcher = new RecordingPlatformLauncher();
        var service = new AppDataService(new FixedUserDataPathProvider(@"D:\ComicPlateData"), launcher);

        Assert.Equal(Path.GetFullPath(@"D:\ComicPlateData"), service.UserDataDirectory);
        Assert.Equal(
            Path.Combine(Path.GetFullPath(@"D:\ComicPlateData"), ThumbnailCacheService.CacheFolderName),
            service.ThumbnailCacheDirectory);
    }

    [Fact]
    public void OpensUserDataDirectoryAsSinglePublicFolderEntry()
    {
        var launcher = new RecordingPlatformLauncher();
        var service = new AppDataService(new FixedUserDataPathProvider(@"D:\ComicPlateData"), launcher);

        service.OpenUserDataDirectory();

        Assert.Equal(service.UserDataDirectory, launcher.LastOpenedPath);
        Assert.NotEqual(service.ThumbnailCacheDirectory, launcher.LastOpenedPath);
    }

    private sealed class FixedUserDataPathProvider : IUserDataPathProvider
    {
        private readonly string _path;

        public FixedUserDataPathProvider(string path)
        {
            _path = path;
        }

        public string GetUserDataDirectory()
        {
            return _path;
        }
    }

    private sealed class RecordingPlatformLauncher : IPlatformLauncher
    {
        public string? LastOpenedPath { get; private set; }

        public void OpenFolder(string path)
        {
            LastOpenedPath = path;
        }
    }
}
