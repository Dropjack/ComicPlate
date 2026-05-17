namespace ComicPlate.Infrastructure.Persistence;

public sealed class DefaultUserDataPathProvider : IUserDataPathProvider
{
    private readonly IUserDataPathProvider _platformProvider;

    public DefaultUserDataPathProvider()
        : this(CreateForCurrentPlatform())
    {
    }

    private DefaultUserDataPathProvider(IUserDataPathProvider platformProvider)
    {
        _platformProvider = platformProvider;
    }

    public static IUserDataPathProvider CreateForCurrentPlatform()
    {
        return OperatingSystem.IsMacOS()
            ? new MacOSUserDataPathProvider()
            : new WindowsUserDataPathProvider();
    }

    public string GetUserDataDirectory()
    {
        return _platformProvider.GetUserDataDirectory();
    }
}
