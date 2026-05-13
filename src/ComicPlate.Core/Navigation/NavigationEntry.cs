using ComicPlate.Core.Books;

namespace ComicPlate.Core.Navigation;

public sealed record NavigationEntry(
    string Path,
    string DisplayName,
    BookSourceKind SourceKind);
