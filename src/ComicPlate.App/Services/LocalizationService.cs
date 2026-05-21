using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.App.Services;

public sealed class LocalizationService
{
    public const string EnglishTag = "en";
    public const string SimplifiedChineseTag = "zh-Hans";
    public const string JapaneseTag = "ja";

    private static readonly Lazy<LocalizationService> LazyCurrent = new(() => Create(AppLanguage.System));

    private readonly IReadOnlyDictionary<string, string> _fallbackStrings;
    private readonly IReadOnlyDictionary<string, string> _strings;

    private LocalizationService(
        string languageTag,
        IReadOnlyDictionary<string, string> fallbackStrings,
        IReadOnlyDictionary<string, string> strings)
    {
        LanguageTag = languageTag;
        _fallbackStrings = fallbackStrings;
        _strings = strings;
    }

    public static LocalizationService Current { get; private set; } = LazyCurrent.Value;

    public string LanguageTag { get; }

    public static void Initialize(AppLanguage language)
    {
        Current = Create(language);
    }

    public static LocalizationService Create(
        AppLanguage language,
        CultureInfo? systemCulture = null,
        string? localizationDirectory = null)
    {
        var directory = localizationDirectory ?? Path.Combine(AppContext.BaseDirectory, "Localization");
        var fallbackStrings = LoadLanguageFile(directory, EnglishTag);
        var languageTag = ResolveLanguageTag(language, systemCulture ?? CultureInfo.CurrentUICulture);
        var strings = languageTag == EnglishTag
            ? fallbackStrings
            : LoadLanguageFile(directory, languageTag);

        return new LocalizationService(languageTag, fallbackStrings, strings);
    }

    public static string ResolveLanguageTag(AppLanguage language, CultureInfo systemCulture)
    {
        return language switch
        {
            AppLanguage.English => EnglishTag,
            AppLanguage.SimplifiedChinese => SimplifiedChineseTag,
            AppLanguage.Japanese => JapaneseTag,
            AppLanguage.System => ResolveSystemLanguageTag(systemCulture),
            _ => EnglishTag,
        };
    }

    public string GetString(string key)
    {
        if (_strings.TryGetValue(key, out var value))
        {
            return value;
        }

        return _fallbackStrings.TryGetValue(key, out var fallbackValue)
            ? fallbackValue
            : key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString(key), args);
    }

    private static string ResolveSystemLanguageTag(CultureInfo culture)
    {
        var name = culture.Name;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return SimplifiedChineseTag;
        }

        if (name.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
        {
            return JapaneseTag;
        }

        return EnglishTag;
    }

    private static IReadOnlyDictionary<string, string> LoadLanguageFile(string directory, string languageTag)
    {
        var path = Path.Combine(directory, $"{languageTag}.json");
        if (!File.Exists(path))
        {
            return LoadEmbeddedLanguageFile(languageTag);
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static IReadOnlyDictionary<string, string> LoadEmbeddedLanguageFile(string languageTag)
    {
        var resourceName = $"ComicPlate.Localization.{languageTag}.json";
        var assembly = typeof(LocalizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return new Dictionary<string, string>();
        }

        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
