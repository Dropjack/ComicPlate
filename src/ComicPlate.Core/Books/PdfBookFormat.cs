namespace ComicPlate.Core.Books;

public static class PdfBookFormat
{
    public const string Extension = ".pdf";
    public const string Label = "PDF";

    public static bool IsSupportedPath(string path)
    {
        return IsSupportedExtension(Path.GetExtension(path));
    }

    public static bool IsSupportedExtension(string extension)
    {
        return Extension.Equals(extension, StringComparison.OrdinalIgnoreCase);
    }
}
