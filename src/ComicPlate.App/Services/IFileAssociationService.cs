namespace ComicPlate.App.Services;

public interface IFileAssociationService
{
    IReadOnlyList<FileAssociationOption> GetSupportedAssociations();

    FileAssociationResult Associate(string extension);
}

