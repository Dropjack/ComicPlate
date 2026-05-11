namespace ComicPlate.Core.Books;

public static class SupportedPageFormats
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp",
        ".gif"
    };

    public static bool IsSupportedExtension(string extension)
    {
        return Extensions.Contains(extension);
    }
}
