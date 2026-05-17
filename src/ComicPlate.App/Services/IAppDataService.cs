namespace ComicPlate.App.Services;

public interface IAppDataService
{
    string UserDataDirectory { get; }

    string ThumbnailCacheDirectory { get; }

    void OpenUserDataDirectory();
}
