using ComicPlate.App.Services;

namespace ComicPlate.Tests.Services;

public sealed class ThumbnailCacheServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ComicPlateThumbnailCacheTests-{Guid.NewGuid():N}");

    public ThumbnailCacheServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void ClearOnlyRemovesThumbnailCacheDirectory()
    {
        var service = new ThumbnailCacheService(_tempDirectory, maxCacheBytes: 1024);
        Directory.CreateDirectory(service.CacheDirectory);

        File.WriteAllText(Path.Combine(_tempDirectory, "settings.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDirectory, "session.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDirectory, "progress.json"), "{}");
        File.WriteAllBytes(Path.Combine(service.CacheDirectory, "cached.png"), new byte[] { 1, 2, 3 });

        service.Clear();

        Assert.True(File.Exists(Path.Combine(_tempDirectory, "settings.json")));
        Assert.True(File.Exists(Path.Combine(_tempDirectory, "session.json")));
        Assert.True(File.Exists(Path.Combine(_tempDirectory, "progress.json")));
        Assert.True(Directory.Exists(service.CacheDirectory));
        Assert.Empty(Directory.EnumerateFiles(service.CacheDirectory));
    }

    [Fact]
    public void EnforceSizeLimitDeletesOldestThumbnailFilesFirst()
    {
        var service = new ThumbnailCacheService(_tempDirectory, maxCacheBytes: 5);
        Directory.CreateDirectory(service.CacheDirectory);

        var oldestPath = Path.Combine(service.CacheDirectory, "oldest.png");
        var newestPath = Path.Combine(service.CacheDirectory, "newest.png");
        File.WriteAllBytes(oldestPath, new byte[] { 1, 2, 3, 4 });
        File.WriteAllBytes(newestPath, new byte[] { 5, 6, 7, 8 });
        File.SetLastAccessTimeUtc(oldestPath, DateTime.UtcNow.AddMinutes(-10));
        File.SetLastAccessTimeUtc(newestPath, DateTime.UtcNow);

        service.EnforceSizeLimit();

        Assert.False(File.Exists(oldestPath));
        Assert.True(File.Exists(newestPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
