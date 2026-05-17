namespace ComicPlate.Infrastructure.Persistence;

public sealed class WindowsUserDataPathProvider : IUserDataPathProvider
{
    public string GetUserDataDirectory()
    {
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppDataPath, "ComicPlate");
    }
}
