namespace ComicPlate.Core.Books;

public static class EpubBookFormat
{
    public const string Extension = ".epub";
    public const string Label = "EPUB";

    public static bool IsSupportedPath(string path)
    {
        return IsSupportedExtension(Path.GetExtension(path));
    }

    public static bool IsSupportedExtension(string extension)
    {
        return Extension.Equals(extension, StringComparison.OrdinalIgnoreCase);
    }
}
