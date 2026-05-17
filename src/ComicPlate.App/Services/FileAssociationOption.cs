namespace ComicPlate.App.Services;

public sealed record FileAssociationOption(
    string Extension,
    string DisplayName,
    bool CanAssociate,
    bool IsAssociated,
    string StatusText);
