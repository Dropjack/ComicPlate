namespace ComicPlate.Infrastructure.Persistence;

public sealed class DefaultUserDataPathProvider : IUserDataPathProvider
{
    public string GetUserDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfilePath, "Library", "Application Support", "ComicPlate");
        }

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppDataPath, "ComicPlate");
    }
}
