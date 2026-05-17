using System.Security.Cryptography;
using System.Text;
using Avalonia.Media.Imaging;

namespace ComicPlate.App.Services;

public sealed class ThumbnailCacheService
{
    public const long DefaultMaxCacheBytes = 300L * 1024 * 1024;

    public const string CacheFolderName = "thumbnail-cache";
    private readonly long _maxCacheBytes;

    public ThumbnailCacheService(string userDataDirectory, long maxCacheBytes = DefaultMaxCacheBytes)
    {
        UserDataDirectory = Path.GetFullPath(userDataDirectory);
        _maxCacheBytes = Math.Max(0, maxCacheBytes);
    }

    public string UserDataDirectory { get; }

    public string CacheDirectory => Path.Combine(UserDataDirectory, CacheFolderName);

    public Bitmap? TryLoad(string cacheKey)
    {
        var path = GetCacheFilePath(cacheKey);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new Bitmap(path);
            File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
            return bitmap;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save(string cacheKey, Bitmap thumbnail)
    {
        var path = GetCacheFilePath(cacheKey);
        var tempPath = $"{path}.tmp";

        try
        {
            Directory.CreateDirectory(CacheDirectory);
            using (var stream = File.Create(tempPath))
            {
                thumbnail.Save(stream);
            }

            File.Move(tempPath, path, overwrite: true);
            File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
            EnforceSizeLimit();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteFile(tempPath);
        }
    }

    public void Clear()
    {
        try
        {
            if (Directory.Exists(CacheDirectory))
            {
                Directory.Delete(CacheDirectory, recursive: true);
            }

            Directory.CreateDirectory(CacheDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cache cleanup should never block reading or settings.
        }
    }

    public void EnsureCacheDirectory()
    {
        Directory.CreateDirectory(CacheDirectory);
    }

    public void EnforceSizeLimit()
    {
        if (!Directory.Exists(CacheDirectory))
        {
            return;
        }

        var files = EnumerateCacheFiles()
            .OrderBy(file => file.LastAccessTimeUtc)
            .ThenBy(file => file.LastWriteTimeUtc)
            .ToArray();

        var totalBytes = files.Sum(file => file.Length);
        foreach (var file in files)
        {
            if (totalBytes <= _maxCacheBytes)
            {
                break;
            }

            var length = file.Length;
            TryDeleteFile(file.FullName);
            totalBytes -= length;
        }
    }

    private IEnumerable<FileInfo> EnumerateCacheFiles()
    {
        DirectoryInfo directory;

        try
        {
            directory = new DirectoryInfo(CacheDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or PathTooLongException)
        {
            yield break;
        }

        IEnumerable<FileInfo> files;
        try
        {
            files = directory.EnumerateFiles("*.png", SearchOption.TopDirectoryOnly);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            yield return file;
        }
    }

    private string GetCacheFilePath(string cacheKey)
    {
        return Path.Combine(CacheDirectory, $"{HashCacheKey(cacheKey)}.png");
    }

    private static string HashCacheKey(string cacheKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cache cleanup.
        }
    }
}
