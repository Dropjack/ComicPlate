using System.Text.Json;
using System.Text.Json.Serialization;
using ComicPlate.Core.Books;
using ComicPlate.Core.Navigation;
using ComicPlate.Core.Reading;

namespace ComicPlate.Infrastructure.Persistence;

public sealed class JsonAppStateStore
{
    private const int Version = 1;
    private static readonly object FileGate = new();

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
        get => new DefaultUserDataPathProvider().GetUserDataDirectory();
    }

    public static JsonAppStateStore CreateDefault()
    {
        return new JsonAppStateStore(DefaultDirectoryPath);
    }

    public SessionState LoadSession()
    {
        lock (FileGate)
        {
            return ReadJson(SessionPath, SessionState.Empty);
        }
    }

    public void SaveSession(SessionState session)
    {
        lock (FileGate)
        {
            WriteJson(SessionPath, session);
        }
    }

    public ProgressStore LoadProgress()
    {
        lock (FileGate)
        {
            return LoadProgressCore();
        }
    }

    public ProgressEntry? FindProgress(string bookPath)
    {
        var key = NormalizePath(bookPath);
        lock (FileGate)
        {
            var store = LoadProgressCore();
            return store.Books.TryGetValue(key, out var entry) ? entry : null;
        }
    }

    public void SaveProgress(ProgressEntry entry)
    {
        lock (FileGate)
        {
            var store = LoadProgressCore();
            var key = NormalizePath(entry.Path);
            store.Books[key] = entry with { Path = key };
            TrimProgress(store);
            WriteJson(ProgressPath, store);
        }
    }

    public void DeleteProgress(string bookPath)
    {
        lock (FileGate)
        {
            var store = LoadProgressCore();
            if (store.Books.Remove(NormalizePath(bookPath)))
            {
                WriteJson(ProgressPath, store);
            }
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

        lock (FileGate)
        {
            WriteJson(SessionPath, new SessionState(
                Version,
                new ReadableUnitState(normalizedPath, book.DisplayName, book.SourceKind, pageIndex),
                navigationHistory.Current?.SourceKind == BookSourceKind.Collection
                    ? navigationHistory.Current
                    : null,
                navigationHistory.BackStack,
                savedAt));

            if (pageCount > 0 && pageIndex >= pageCount - 1)
            {
                if (deleteCompletedProgress)
                {
                    var store = LoadProgressCore();
                    if (store.Books.Remove(normalizedPath))
                    {
                        WriteJson(ProgressPath, store);
                    }
                }

                return;
            }

            var progressStore = LoadProgressCore();
            progressStore.Books[normalizedPath] = new ProgressEntry(
                normalizedPath,
                book.DisplayName,
                book.SourceKind,
                pageIndex,
                pageCount,
                readingDirection,
                viewMode,
                savedAt);
            TrimProgress(progressStore);
            WriteJson(ProgressPath, progressStore);
        }
    }

    public void SaveSessionOnly(
        BookEntry book,
        int pageIndex,
        NavigationHistory navigationHistory)
    {
        var normalizedPath = NormalizePath(book.Path);
        lock (FileGate)
        {
            WriteJson(SessionPath, new SessionState(
                Version,
                new ReadableUnitState(normalizedPath, book.DisplayName, book.SourceKind, pageIndex),
                navigationHistory.Current?.SourceKind == BookSourceKind.Collection
                    ? navigationHistory.Current
                    : null,
                navigationHistory.BackStack,
                DateTimeOffset.UtcNow));
        }
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

    private ProgressStore LoadProgressCore()
    {
        var store = ReadJson(ProgressPath, ProgressStore.Empty);
        return new ProgressStore(
            store.Version,
            new Dictionary<string, ProgressEntry>(store.Books, StringComparer.OrdinalIgnoreCase));
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
