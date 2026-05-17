namespace ComicPlate.Core.Books;

public enum ComicArchiveKind
{
    Zip,
    Rar
}

public sealed record ComicArchiveFormat(
    string Extension,
    ComicArchiveKind ArchiveKind,
    BookSourceKind SourceKind,
    string DisplayName);

