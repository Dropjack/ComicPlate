namespace ComicPlate.Core.Books;

public static class ComicArchiveFormats
{
    private static readonly ComicArchiveFormat[] Formats =
    [
        new(".zip", ComicArchiveKind.Zip, BookSourceKind.Zip, "ZIP"),
        new(".cbz", ComicArchiveKind.Zip, BookSourceKind.Zip, "CBZ"),
        new(".rar", ComicArchiveKind.Rar, BookSourceKind.Rar, "RAR"),
        new(".cbr", ComicArchiveKind.Rar, BookSourceKind.Rar, "CBR")
    ];

    public static IReadOnlyList<ComicArchiveFormat> SupportedFormats => Formats;

    public static bool IsSupportedArchivePath(string path)
    {
        return TryGetByPath(path, out _);
    }

    public static bool TryGetByPath(string path, out ComicArchiveFormat format)
    {
        return TryGetByExtension(Path.GetExtension(path), out format);
    }

    public static bool TryGetByExtension(string extension, out ComicArchiveFormat format)
    {
        foreach (var candidate in Formats)
        {
            if (candidate.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                format = candidate;
                return true;
            }
        }

        format = default!;
        return false;
    }
}

