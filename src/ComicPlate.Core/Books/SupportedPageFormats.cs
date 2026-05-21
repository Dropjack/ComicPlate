namespace ComicPlate.Core.Books;

public static class SupportedPageFormats
{
    private static readonly string[] Extensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp",
        ".gif"
    ];

    private static readonly HashSet<string> ExtensionSet = new(Extensions, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> SupportedExtensions => Extensions;

    public static bool IsSupportedExtension(string extension)
    {
        return ExtensionSet.Contains(extension);
    }
}
