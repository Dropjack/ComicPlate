namespace ComicPlate.Infrastructure.Persistence;

public sealed class MacOSUserDataPathProvider : IUserDataPathProvider
{
    public string GetUserDataDirectory()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfilePath, "Library", "Application Support", "ComicPlate");
    }
}
