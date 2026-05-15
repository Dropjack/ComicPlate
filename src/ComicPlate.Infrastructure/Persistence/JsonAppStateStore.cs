using System.Text.Json;
using System.Text.Json.Serialization;
using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;

namespace ComicPlate.Infrastructure.Persistence;

public sealed class JsonAppStateStore
{
    private const int Version = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    static JsonAppStateStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly string _directoryPath;
    private readonly int _progressLimit;

    public JsonAppStateStore(string directoryPath, int progressLimit = 500)
    {
        _directoryPath = directoryPath;
        _progressLimit = progressLimit;
    }

    private string SessionPath => Path.Combine(_directoryPath, "session.json");

    private string ProgressPath => Path.Combine(_directoryPath, "progress.json");

    public static string DefaultDirectoryPath
    {
        get
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "ComicPlate");
        }
    }

    public static JsonAppStateStore CreateDefault()
    {
        return new JsonAppStateStore(DefaultDirectoryPath);
    }

    public SessionState LoadSession()
    {
        return ReadJson(SessionPath, SessionState.Empty);
    }

    public void SaveSession(SessionState session)
    {
        WriteJson(SessionPath, session);
    }

    public ProgressStore LoadProgress()
    {
        var store = ReadJson(ProgressPath, ProgressStore.Empty);
        return new ProgressStore(
            store.Version,
            new Dictionary<string, ProgressEntry>(store.Books, StringComparer.OrdinalIgnoreCase));
    }

    public ProgressEntry? FindProgress(string bookPath)
    {
        var key = NormalizePath(bookPath);
        var store = LoadProgress();
        return store.Books.TryGetValue(key, out var entry) ? entry : null;
    }

    public void SaveProgress(ProgressEntry entry)
    {
        var store = LoadProgress();
        var key = NormalizePath(entry.Path);
        store.Books[key] = entry with { Path = key };
        TrimProgress(store);
        WriteJson(ProgressPath, store);
    }

    public void DeleteProgress(string bookPath)
    {
        var store = LoadProgress();
        if (store.Books.Remove(NormalizePath(bookPath)))
        {
            WriteJson(ProgressPath, store);
        }
    }

    public void SaveReadingState(
        BookEntry book,
        int pageIndex,
        int pageCount,
        ReadingDirection readingDirection,
        ViewMode viewMode,
        NavigationHistory navigationHistory,
        bool deleteCompletedProgress = true)
    {
        var normalizedPath = NormalizePath(book.Path);
        var savedAt = DateTimeOffset.UtcNow;

        SaveSession(new SessionState(
            Version,
            new ReadableUnitState(normalizedPath, book.DisplayName, book.SourceKind, pageIndex),
            navigationHistory.BackStack,
            savedAt));

        if (pageCount > 0 && pageIndex >= pageCount - 1)
        {
            if (deleteCompletedProgress)
            {
                DeleteProgress(normalizedPath);
            }

            return;
        }

        SaveProgress(new ProgressEntry(
            normalizedPath,
            book.DisplayName,
            book.SourceKind,
            pageIndex,
            pageCount,
            readingDirection,
            viewMode,
            savedAt));
    }

    public void SaveSessionOnly(
        BookEntry book,
        int pageIndex,
        NavigationHistory navigationHistory)
    {
        var normalizedPath = NormalizePath(book.Path);
        SaveSession(new SessionState(
            Version,
            new ReadableUnitState(normalizedPath, book.DisplayName, book.SourceKind, pageIndex),
            navigationHistory.BackStack,
            DateTimeOffset.UtcNow));
    }

    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static T ReadJson<T>(string path, T fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return fallback;
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json);
    }

    private void TrimProgress(ProgressStore store)
    {
        if (store.Books.Count <= _progressLimit)
        {
            return;
        }

        var staleKeys = store.Books
            .OrderBy(pair => pair.Value.LastOpenedAt)
            .Take(store.Books.Count - _progressLimit)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in staleKeys)
        {
            store.Books.Remove(key);
        }
    }
}
