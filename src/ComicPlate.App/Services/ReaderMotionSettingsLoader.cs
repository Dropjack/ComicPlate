using System.Reflection;
using System.Text.Json;

namespace ComicPlate.App.Services;

public static class ReaderMotionSettingsLoader
{
    private const string ResourceName = "ComicPlate.Config.reader-motion.jsonc";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ReaderMotionSettings LoadEmbeddedOrDefault()
    {
        var assembly = typeof(ReaderMotionSettingsLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return ReaderMotionSettings.Default;
        }

        return LoadOrDefault(stream);
    }

    public static ReaderMotionSettings LoadOrDefault(Stream stream)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<ReaderMotionSettings>(stream, JsonOptions);
            return (settings ?? ReaderMotionSettings.Default).Normalize();
        }
        catch (JsonException)
        {
            return ReaderMotionSettings.Default;
        }
    }
}
