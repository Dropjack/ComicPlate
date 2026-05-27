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

    public IReadOnlyList<ProgressEntry> GetRecentProgressEntries(int limit)
    {
        lock (FileGate)
        {
            return LoadProgressCore()
                .Books
                .Values
                .OrderByDescending(entry => entry.LastOpenedAt)
                .Take(Math.Max(0, limit))
                .ToArray();
        }
    }

    public IReadOnlySet<string> GetOpenedBookPaths()
    {
        lock (FileGate)
        {
            return LoadProgressCore()
                .OpenedBooks
                .Keys
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool IsBookOpened(string bookPath)
    {
        var key = NormalizePath(bookPath);
        lock (FileGate)
        {
            return LoadProgressCore().OpenedBooks.ContainsKey(key);
        }
    }

    public void MarkBookOpened(BookEntry book)
    {
        var normalizedPath = NormalizePath(book.Path);
        lock (FileGate)
        {
            var store = LoadProgressCore();
            store.OpenedBooks[normalizedPath] = new OpenedBookEntry(
                normalizedPath,
                book.DisplayName,
                book.SourceKind,
                DateTimeOffset.UtcNow);
            TrimOpenedBooks(store);
            WriteJson(ProgressPath, store);
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
        var readingContainer = CreateReadingContainerEntry(book, navigationHistory);
        var readingParentCollection = CreateReadingParentCollectionEntry(navigationHistory);
        var readingContainerBackStack = CreateNavigationBackStack(readingContainer, navigationHistory);
        var readingParentCollectionBackStack = CreateNavigationBackStack(readingParentCollection, navigationHistory);

        lock (FileGate)
        {
            WriteJson(SessionPath, new SessionState(
                Version,
                new ReadableUnitState(normalizedPath, book.DisplayName, book.SourceKind, pageIndex),
                navigationHistory.Current?.SourceKind == BookSourceKind.Collection
                    ? navigationHistory.Current
                    : null,
                navigationHistory.BackStack,
                savedAt)
            {
                ReadingShelfCurrent = readingParentCollection,
                ReadingShelfBackStack = readingParentCollectionBackStack,
                ReadingContainerCurrent = readingContainer,
                ReadingContainerBackStack = readingContainerBackStack,
                ReadingParentShelfCurrent = readingParentCollection,
                ReadingParentShelfBackStack = readingParentCollectionBackStack,
                ReadingParentCollectionCurrent = readingParentCollection,
                ReadingParentCollectionBackStack = readingParentCollectionBackStack
            });

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
        var readingContainer = CreateReadingContainerEntry(book, navigationHistory);
        var readingParentCollection = CreateReadingParentCollectionEntry(navigationHistory);
        var readingContainerBackStack = CreateNavigationBackStack(readingContainer, navigationHistory);
        var readingParentCollectionBackStack = CreateNavigationBackStack(readingParentCollection, navigationHistory);
        lock (FileGate)
        {
            WriteJson(SessionPath, new SessionState(
                Version,
                new ReadableUnitState(normalizedPath, book.DisplayName, book.SourceKind, pageIndex),
                navigationHistory.Current?.SourceKind == BookSourceKind.Collection
                    ? navigationHistory.Current
                    : null,
                navigationHistory.BackStack,
                DateTimeOffset.UtcNow)
            {
                ReadingShelfCurrent = readingParentCollection,
                ReadingShelfBackStack = readingParentCollectionBackStack,
                ReadingContainerCurrent = readingContainer,
                ReadingContainerBackStack = readingContainerBackStack,
                ReadingParentShelfCurrent = readingParentCollection,
                ReadingParentShelfBackStack = readingParentCollectionBackStack,
                ReadingParentCollectionCurrent = readingParentCollection,
                ReadingParentCollectionBackStack = readingParentCollectionBackStack
            });
        }
    }

    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static NavigationEntry? CreateReadingContainerEntry(BookEntry book, NavigationHistory navigationHistory)
    {
        var fullPath = Path.GetFullPath(book.Path);
        if (Directory.Exists(fullPath) && book.SourceKind == BookSourceKind.Folder)
        {
            return CreateCollectionEntry(fullPath);
        }

        return navigationHistory.Current?.SourceKind == BookSourceKind.Collection
            ? navigationHistory.Current
            : null;
    }

    private static NavigationEntry? CreateReadingParentCollectionEntry(NavigationHistory navigationHistory)
    {
        return navigationHistory.Current?.SourceKind == BookSourceKind.Collection
            ? navigationHistory.Current
            : null;
    }

    private static NavigationEntry CreateCollectionEntry(string path)
    {
        var displayName = Path.GetFileName(path);
        return new NavigationEntry(
            path,
            string.IsNullOrWhiteSpace(displayName) ? path : displayName,
            BookSourceKind.Collection);
    }

    private static IReadOnlyList<NavigationEntry> CreateNavigationBackStack(
        NavigationEntry? current,
        NavigationHistory navigationHistory)
    {
        if (current is null)
        {
            return Array.Empty<NavigationEntry>();
        }

        if (IsSameNavigationEntry(navigationHistory.Current, current))
        {
            return navigationHistory.BackStack;
        }

        var backStack = navigationHistory.BackStack;
        var collectionIndex = Array.FindIndex(
            backStack.ToArray(),
            entry => IsSameNavigationEntry(entry, current));
        return collectionIndex >= 0
            ? backStack.Skip(collectionIndex + 1).ToArray()
            : Array.Empty<NavigationEntry>();
    }

    private static bool IsSameNavigationEntry(NavigationEntry? first, NavigationEntry second)
    {
        return first is not null
            && first.SourceKind == second.SourceKind
            && NormalizePath(first.Path).Equals(NormalizePath(second.Path), StringComparison.OrdinalIgnoreCase);
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
        var normalizedStore = new ProgressStore(
            store.Version,
            new Dictionary<string, ProgressEntry>(store.Books ?? new Dictionary<string, ProgressEntry>(), StringComparer.OrdinalIgnoreCase))
        {
            OpenedBooks = new Dictionary<string, OpenedBookEntry>(
                store.OpenedBooks ?? new Dictionary<string, OpenedBookEntry>(),
                StringComparer.OrdinalIgnoreCase)
        };

        return normalizedStore;
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

    private void TrimOpenedBooks(ProgressStore store)
    {
        if (store.OpenedBooks.Count <= _progressLimit)
        {
            return;
        }

        var staleKeys = store.OpenedBooks
            .OrderBy(pair => pair.Value.LastOpenedAt)
            .Take(store.OpenedBooks.Count - _progressLimit)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in staleKeys)
        {
            store.OpenedBooks.Remove(key);
        }
    }
}
